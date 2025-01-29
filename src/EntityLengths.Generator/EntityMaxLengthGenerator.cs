using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityLengths.Generator;

[Generator]
public class EntityMaxLengthGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find entity type configuration classes
        var providerConfigTypes = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: (s, _) => IsEntityConfigurationClass(s),
                transform: (syntaxContext, _) => GetEntityTypeConfigurationInfo(syntaxContext)
            )
            .Where(x => x != null);

        // Find entity classes with string properties that have MaxLength attribute
        var entityTypesWithAttributes = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: (s, _) => s is ClassDeclarationSyntax,
                transform: (syntaxContext, _) => GetEntityTypeInfoWithAttributes(syntaxContext)
            )
            .Where(x => x != null);

        // Combine the found configurations and attribute-based configurations
        var compilationAndConfigurations = context
            .CompilationProvider.Combine(providerConfigTypes.Collect())
            .Combine(entityTypesWithAttributes.Collect());

        // Generate the output
        context.RegisterSourceOutput(
            compilationAndConfigurations,
            (spc, source) =>
                GenerateEntityLengthsFile(spc, source.Left.Left, source.Left.Right, source.Right)
        );
    }

    private static bool IsEntityConfigurationClass(SyntaxNode syntaxNode)
    {
        return syntaxNode is ClassDeclarationSyntax classSyntax
            && classSyntax.BaseList?.Types.Any(t =>
                t.Type.ToString().StartsWith("IEntityTypeConfiguration")
            ) == true;
    }

    private static EntityConfigurationInfo? GetEntityTypeConfigurationInfo(
        GeneratorSyntaxContext context
    )
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Find the generic type parameter of IEntityTypeConfiguration<T>
        var baseType = classSyntax
            .BaseList?.Types.Select(t => semanticModel.GetTypeInfo(t.Type).Type)
            .FirstOrDefault(t => t?.Name.StartsWith("IEntityTypeConfiguration") == true);

        if (baseType is null)
        {
            return null;
        }

        // Get the entity type
        var entityType = (baseType as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();
        if (entityType is null)
        {
            return null;
        }

        // Find properties with HasMaxLength configuration
        var maxLengthProperties = FindMaxLengthProperties(classSyntax);

        return new EntityConfigurationInfo(entityType, maxLengthProperties);
    }

    private static EntityTypeInfo? GetEntityTypeInfoWithAttributes(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax);

        if (classSymbol is null)
        {
            return null;
        }

        // Get string properties with MaxLength attribute
        var stringPropertiesWithMaxLength = new List<PropertyMaxLength>();

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.Type.SpecialType != SpecialType.System_String)
            {
                continue;
            }

            var maxLengthAttribute = member
                .GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "MaxLengthAttribute");

            if (
                maxLengthAttribute != null
                && maxLengthAttribute.ConstructorArguments.Length > 0
                && maxLengthAttribute.ConstructorArguments[0].Value is int maxLength
            )
            {
                stringPropertiesWithMaxLength.Add(new PropertyMaxLength(member.Name, maxLength));
            }
        }

        return stringPropertiesWithMaxLength.Any()
            ? new EntityTypeInfo(classSymbol, stringPropertiesWithMaxLength)
            : null;
    }

    private static List<PropertyMaxLength> FindMaxLengthProperties(
        ClassDeclarationSyntax classSyntax
    )
    {
        var maxLengthProperties = new List<PropertyMaxLength>();

        // Find HasMaxLength invocations
        var maxLengthInvocations = classSyntax
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(inv => inv.Expression.ToString().EndsWith("HasMaxLength"));

        foreach (var invocation in maxLengthInvocations)
        {
            // Extract property name from lambda expression
            var lambdaExpr = invocation
                .DescendantNodes()
                .OfType<SimpleLambdaExpressionSyntax>()
                .FirstOrDefault();

            var propertyAccess = lambdaExpr?.Body as MemberAccessExpressionSyntax;
            var propertyName = propertyAccess?.Name.Identifier.Text ?? string.Empty;

            if (string.IsNullOrEmpty(propertyName))
            {
                continue;
            }

            // Extract max length
            var maxLengthArg = invocation.ArgumentList.Arguments.FirstOrDefault();
            if (int.TryParse(maxLengthArg?.ToString(), out var maxLength))
            {
                maxLengthProperties.Add(new PropertyMaxLength(propertyName, maxLength));
            }
        }

        return maxLengthProperties;
    }

    private static void GenerateEntityLengthsFile(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<EntityConfigurationInfo?> fluentConfigurations,
        ImmutableArray<EntityTypeInfo?> attributeConfigurations
    )
    {
        var sourceBuilder = new System.Text.StringBuilder();
        sourceBuilder.AppendLine("// <auto-generated/>");

        var projectNamespace = compilation.AssemblyName;
        sourceBuilder.AppendLine($"namespace {projectNamespace};");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("public static partial class EntityLengths \r\n{");

        // Process fluent configurations
        for (var index = 0; index < fluentConfigurations.Length; index++)
        {
            var configuration = fluentConfigurations[index];
            if (configuration is null)
            {
                continue;
            }

            GenerateEntityClass(
                sourceBuilder,
                configuration.EntityType.Name,
                configuration.MaxLengthProperties
            );

            if (index < fluentConfigurations.Length - 1)
            {
                sourceBuilder.AppendLine();
            }
        }

        if (attributeConfigurations.Any() && fluentConfigurations.Any())
        {
            sourceBuilder.AppendLine();
        }

        // Process attribute configurations
        for (var index = 0; index < attributeConfigurations.Length; index++)
        {
            var configuration = attributeConfigurations[index];
            if (configuration is null)
            {
                continue;
            }

            GenerateEntityClass(
                sourceBuilder,
                configuration.EntityType.Name,
                configuration.StringProperties
            );

            if (index < attributeConfigurations.Length - 1)
            {
                sourceBuilder.AppendLine();
            }
        }

        sourceBuilder.AppendLine("}");

        context.AddSource("EntityLengths.g.cs", sourceBuilder.ToString());
    }

    private static void GenerateEntityClass(
        System.Text.StringBuilder sourceBuilder,
        string entityName,
        List<PropertyMaxLength> properties
    )
    {
        sourceBuilder.AppendLine($"\tpublic static partial class {entityName}");
        sourceBuilder.AppendLine("\t{");

        foreach (var prop in properties)
        {
            sourceBuilder.AppendLine(
                $"\t\tpublic const int {prop.PropertyName}Length = {prop.MaxLength};"
            );
        }

        sourceBuilder.AppendLine("\t}");
    }

    private sealed record EntityConfigurationInfo(
        ITypeSymbol EntityType,
        List<PropertyMaxLength> MaxLengthProperties
    )
    {
        public ITypeSymbol EntityType { get; } = EntityType;
        public List<PropertyMaxLength> MaxLengthProperties { get; } = MaxLengthProperties;
    }

    private sealed record EntityTypeInfo(
        ITypeSymbol EntityType,
        List<PropertyMaxLength> StringProperties
    )
    {
        public ITypeSymbol EntityType { get; } = EntityType;
        public List<PropertyMaxLength> StringProperties { get; } = StringProperties;
    }

    private sealed record PropertyMaxLength(string PropertyName, int MaxLength)
    {
        public string PropertyName { get; } = PropertyName;
        public int MaxLength { get; } = MaxLength;
    }
}
