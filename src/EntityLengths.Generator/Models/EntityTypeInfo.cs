using EntityLengths.Generator.Core;

namespace EntityLengths.Generator.Models;

/// <summary>
/// A fully value-equatable description of the length constants found for a single entity type.
/// Deliberately holds no Roslyn symbols: symbols root the <see cref="Microsoft.CodeAnalysis.Compilation"/>
/// and are not equatable, which would disable incremental caching.
/// </summary>
/// <param name="EntityName">Simple name of the entity type, used as the generated nested class name.</param>
/// <param name="EntityNamespace">Namespace of the entity type, empty for the global namespace.</param>
/// <param name="DeclaringNamespace">Namespace of the type that declared the lengths (entity, configuration or DbContext).</param>
/// <param name="DeclaringTypeName">Simple name of the type that declared the lengths.</param>
/// <param name="StringProperties">The discovered property lengths.</param>
public sealed record EntityTypeInfo(
    string EntityName,
    string EntityNamespace,
    string DeclaringNamespace,
    string DeclaringTypeName,
    EquatableArray<PropertyMaxLength> StringProperties
)
{
    /// <summary>
    /// Namespace-qualified entity name, used to detect distinct entities that share a simple name.
    /// </summary>
    public string EntityFullName =>
        EntityNamespace.Length == 0 ? EntityName : $"{EntityNamespace}.{EntityName}";
}
