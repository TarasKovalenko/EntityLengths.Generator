using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using EntityLengths.Generator.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EntityLengths.Generator.Configuration;

internal sealed class EntityLengthsOptionsProvider(EntityLengthsGeneratorOptions? options = null)
{
    private readonly EntityLengthsGeneratorOptions _defaultOptions =
        options ?? EntityLengthsGeneratorOptions.Default;

    public EntityLengthsGeneratorOptions GetOptions(
        Compilation compilation,
        AnalyzerConfigOptions? globalOptions = null
    )
    {
        var result = GetAttributeOptions(compilation);
        ApplyMsBuildOptions(result, globalOptions);
        return result;
    }

    private EntityLengthsGeneratorOptions GetAttributeOptions(Compilation compilation)
    {
        var assemblyAttributes = compilation.Assembly.GetAttributes();
        var configAttribute = assemblyAttributes.FirstOrDefault(attr =>
            string.Equals(
                attr.AttributeClass?.Name,
                nameof(EntityLengthsGeneratorAttribute),
                StringComparison.Ordinal
            )
        );

        if (configAttribute == null)
        {
            return Clone(_defaultOptions);
        }

        // Get attribute values
        var className = GetNamedArgumentValue<string>(
            configAttribute,
            nameof(EntityLengthsGeneratorAttribute.GeneratedClassName)
        );
        var lengthSuffix = GetNamedArgumentValue<string>(
            configAttribute,
            nameof(EntityLengthsGeneratorAttribute.LengthSuffix)
        );
        var generateDocs = GetNamedArgumentValue<bool>(
            configAttribute,
            nameof(EntityLengthsGeneratorAttribute.GenerateDocumentation)
        );
        var ns = GetNamedArgumentValue<string>(
            configAttribute,
            nameof(EntityLengthsGeneratorAttribute.Namespace)
        );

        var includeNs = GetNamedArgumentValue<ImmutableArray<string>>(
            configAttribute,
            nameof(EntityLengthsGeneratorAttribute.IncludeNamespaces)
        );
        var excludeNs = GetNamedArgumentValue<ImmutableArray<string>>(
            configAttribute,
            nameof(EntityLengthsGeneratorAttribute.ExcludeNamespaces)
        );

        var scanNested =
            GetNamedArgumentValue<bool?>(
                configAttribute,
                nameof(EntityLengthsGeneratorAttribute.ScanNestedNamespaces)
            ) ?? true;
        var entitySuffix = GetNamedArgumentValue<string>(
            configAttribute,
            nameof(EntityLengthsGeneratorAttribute.ScanEntitySuffix)
        );

        return new EntityLengthsGeneratorOptions
        {
            GeneratedClassName = className ?? _defaultOptions.GeneratedClassName,
            LengthSuffix = lengthSuffix ?? _defaultOptions.LengthSuffix,
            GenerateDocumentation = generateDocs,
            Namespace = ns,
            OutputPath = _defaultOptions.OutputPath,
            OutputNamespace = _defaultOptions.OutputNamespace,
            EmitInProjectOutput = _defaultOptions.EmitInProjectOutput,
            ScanningOptions = new EntityLengthsScanningOptions
            {
                IncludeNamespaces = includeNs.IsDefault
                    ? [.. _defaultOptions.ScanningOptions.IncludeNamespaces]
                    : [.. includeNs],
                ExcludeNamespaces = excludeNs.IsDefault
                    ? [.. _defaultOptions.ScanningOptions.ExcludeNamespaces]
                    : [.. excludeNs],
                ScanNestedNamespaces = scanNested,
                EntitySuffix = entitySuffix ?? _defaultOptions.ScanningOptions.EntitySuffix,
            },
        };
    }

    /// <summary>
    /// Overlays the MSBuild properties on top of the assembly attribute values. MSBuild wins because
    /// the output location is a build concern, not a source concern.
    /// </summary>
    private static void ApplyMsBuildOptions(
        EntityLengthsGeneratorOptions target,
        AnalyzerConfigOptions? globalOptions
    )
    {
        if (globalOptions is null)
        {
            return;
        }

        var outputPath =
            GetProperty(globalOptions, MsBuildPropertyNames.ResolvedOutputPath)
            ?? Resolve(
                GetProperty(globalOptions, MsBuildPropertyNames.OutputPath),
                GetProperty(globalOptions, MsBuildPropertyNames.ProjectDir)
            );

        if (outputPath is not null)
        {
            target.OutputPath = outputPath;
            // Writing to another project means that project compiles the constants, so emitting them
            // here as well would produce the same type in two assemblies.
            target.EmitInProjectOutput = false;
        }

        var outputNamespace = GetProperty(globalOptions, MsBuildPropertyNames.OutputNamespace);
        if (outputNamespace is not null)
        {
            target.OutputNamespace = outputNamespace;
        }

        var emitInProject = GetProperty(globalOptions, MsBuildPropertyNames.EmitInProjectOutput);
        if (emitInProject is not null && bool.TryParse(emitInProject, out var emit))
        {
            target.EmitInProjectOutput = emit;
        }
    }

    private static string? Resolve(string? path, string? projectDir)
    {
        if (path is null)
        {
            return null;
        }

        if (Path.IsPathRooted(path) || string.IsNullOrWhiteSpace(projectDir))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(projectDir, path));
    }

    private static string? GetProperty(AnalyzerConfigOptions globalOptions, string key) =>
        globalOptions.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;

    private static EntityLengthsGeneratorOptions Clone(EntityLengthsGeneratorOptions source) =>
        new()
        {
            GeneratedClassName = source.GeneratedClassName,
            LengthSuffix = source.LengthSuffix,
            GenerateDocumentation = source.GenerateDocumentation,
            Namespace = source.Namespace,
            OutputPath = source.OutputPath,
            OutputNamespace = source.OutputNamespace,
            EmitInProjectOutput = source.EmitInProjectOutput,
            ScanningOptions = new EntityLengthsScanningOptions
            {
                IncludeNamespaces = [.. source.ScanningOptions.IncludeNamespaces],
                ExcludeNamespaces = [.. source.ScanningOptions.ExcludeNamespaces],
                ScanNestedNamespaces = source.ScanningOptions.ScanNestedNamespaces,
                EntitySuffix = source.ScanningOptions.EntitySuffix,
            },
        };

    private static T? GetNamedArgumentValue<T>(AttributeData attribute, string name)
    {
        var argument = attribute.NamedArguments.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, name, StringComparison.Ordinal)
        );

        var value = argument.Value;

        if (value.IsNull)
        {
            return default;
        }

        if (typeof(T) == typeof(ImmutableArray<string>) && value.Kind == TypedConstantKind.Array)
        {
            var arrayValues = value.Values.Select(v => v.Value?.ToString()).Where(v => v != null);
            return (T)(object)arrayValues.ToImmutableArray();
        }

        return (T?)value.Value;
    }
}
