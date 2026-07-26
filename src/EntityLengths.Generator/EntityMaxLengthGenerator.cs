using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using EntityLengths.Generator.Configuration;
using EntityLengths.Generator.Core;
using EntityLengths.Generator.Diagnostics;
using EntityLengths.Generator.Emit;
using EntityLengths.Generator.Extensions;
using EntityLengths.Generator.Extractors;
using EntityLengths.Generator.Models;
using EntityLengths.Generator.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityLengths.Generator;

[Generator]
public class EntityMaxLengthGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Options depend on the compilation (assembly attribute) and on MSBuild properties. Kept out
        // of the syntax transform on purpose: the transform must not see the Compilation, which is not
        // equatable and would defeat caching.
        var configuration = context
            .CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider)
            .Select(
                (pair, _) =>
                    new GeneratorConfiguration(
                        new EntityLengthsOptionsProvider().GetOptions(
                            pair.Left,
                            pair.Right.GlobalOptions
                        ),
                        pair.Left.AssemblyName
                    )
            );

        var typeDeclarations = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: IsSyntaxTargetForGeneration,
                transform: static (syntaxContext, _) => Transform(syntaxContext)
            )
            .SelectMany(static (entities, _) => entities.AsImmutableArray())
            .Collect();

        context.RegisterSourceOutput(
            configuration.Combine(typeDeclarations),
            static (spc, source) => GenerateOutput(spc, source.Left, source.Right)
        );
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken _) =>
        // Only classes and records can carry lengths, and only through a base type (configuration or
        // DbContext) or through attributes on their members.
        node is TypeDeclarationSyntax typeDeclaration
        && typeDeclaration is ClassDeclarationSyntax or RecordDeclarationSyntax
        && (
            typeDeclaration.BaseList is not null
            || typeDeclaration.Members.Any(static m => m.AttributeLists.Count > 0)
        );

    private static EquatableArray<EntityTypeInfo> Transform(GeneratorSyntaxContext syntaxContext)
    {
        var typeNode = (TypeDeclarationSyntax)syntaxContext.Node;

        // Check for IEntityTypeConfiguration
        if (typeNode.IsEntityConfigurationClass())
        {
            return new FluentConfigurationExtractor().ExtractPropertyLengths(syntaxContext);
        }

        // Check for DbContext
        if (typeNode.IsDbContextClass())
        {
            return new DbContextConfigurationExtractor().ExtractPropertyLengths(syntaxContext);
        }

        // Check for attributes
        return new AttributeExtractor().ExtractPropertyLengths(syntaxContext);
    }

    private static void GenerateOutput(
        SourceProductionContext context,
        GeneratorConfiguration configuration,
        ImmutableArray<EntityTypeInfo> types
    )
    {
        var options = configuration.Options;
        var filter = new NamespaceFilter(options.ScanningOptions);

        var entities = types
            .Where(t => filter.ShouldInclude(t.DeclaringNamespace, t.DeclaringTypeName))
            .GroupBy(t => t.EntityName)
            .OrderBy(g => g.Key)
            .Select(g => Resolve(context, g.Key, [.. g]))
            .ToList();

        if (options.EmitInProjectOutput)
        {
            var ns = options.Namespace ?? configuration.AssemblyName ?? Constants.ClassName;
            context.AddSource(
                $"{options.GeneratedClassName}.g.cs",
                ConstantsSourceBuilder.Build(ns, entities, options)
            );
        }

        if (options.OutputPath is null)
        {
            return;
        }

        var outputNamespace = options.OutputNamespace ?? options.Namespace;
        if (outputNamespace is null)
        {
            outputNamespace = configuration.AssemblyName ?? Constants.ClassName;
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.OutputNamespaceMissing,
                    Location.None,
                    outputNamespace
                )
            );
        }

        var content = ConstantsSourceBuilder.Build(outputNamespace, entities, options);
        if (!GeneratedFileWriter.TryWrite(options.OutputPath, content, out var error))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.OutputWriteFailed,
                    Location.None,
                    options.OutputPath,
                    error
                )
            );
        }
    }

    /// <summary>
    /// Collapses everything found for one entity name into a single nested class, reporting the
    /// ambiguities instead of silently picking a value.
    /// </summary>
    private static EntityConstants Resolve(
        SourceProductionContext context,
        string entityName,
        IReadOnlyList<EntityTypeInfo> group
    )
    {
        var fullNames = group.Select(t => t.EntityFullName).Distinct().OrderBy(n => n).ToList();
        if (fullNames.Count > 1)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.DuplicateEntityName,
                    Location.None,
                    string.Join(", ", fullNames),
                    entityName
                )
            );
        }

        var properties = new List<PropertyMaxLength>();
        foreach (
            var propertyGroup in group
                .SelectMany(t => t.StringProperties)
                .GroupBy(p => p.PropertyName)
                .OrderBy(g => g.Key)
        )
        {
            var lengths = propertyGroup.Select(p => p.MaxLength).Distinct().ToList();
            var resolved = propertyGroup.First();

            if (lengths.Count > 1)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ConflictingPropertyLength,
                        Location.None,
                        entityName,
                        propertyGroup.Key,
                        string.Join(", ", lengths.OrderBy(l => l)),
                        resolved.MaxLength
                    )
                );
            }

            properties.Add(resolved);
        }

        return new EntityConstants(entityName, properties);
    }
}

/// <summary>
/// Everything the emit stage needs from the compilation, in an equatable shape.
/// </summary>
internal sealed record GeneratorConfiguration(
    EntityLengthsGeneratorOptions Options,
    string? AssemblyName
);
