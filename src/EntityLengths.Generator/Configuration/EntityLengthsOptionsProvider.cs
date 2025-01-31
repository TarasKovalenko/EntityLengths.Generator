using System.Collections.Immutable;
using EntityLengths.Generator.Options;
using Microsoft.CodeAnalysis;

namespace EntityLengths.Generator.Configuration;

internal sealed class EntityLengthsOptionsProvider(EntityLengthsGeneratorOptions? options = null)
{
    private readonly EntityLengthsGeneratorOptions _defaultOptions =
        options ?? EntityLengthsGeneratorOptions.Default;

    public EntityLengthsGeneratorOptions GetOptions(Compilation compilation)
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
            return _defaultOptions;
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
            ScanningOptions = new EntityLengthsScanningOptions
            {
                IncludeNamespaces = includeNs.IsDefault
                    ? _defaultOptions.ScanningOptions.IncludeNamespaces
                    : [.. includeNs],
                ExcludeNamespaces = excludeNs.IsDefault
                    ? _defaultOptions.ScanningOptions.ExcludeNamespaces
                    : [.. excludeNs],
                ScanNestedNamespaces = scanNested,
                EntitySuffix = entitySuffix ?? _defaultOptions.ScanningOptions.EntitySuffix,
            },
        };
    }

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
