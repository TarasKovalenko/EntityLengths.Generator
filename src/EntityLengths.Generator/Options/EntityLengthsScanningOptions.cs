using System;
using System.Collections.Generic;

namespace EntityLengths.Generator.Options;

/// <summary>
/// Scanning options for the EntityLengths generator.
/// </summary>
public class EntityLengthsScanningOptions : IEquatable<EntityLengthsScanningOptions>
{
    /// <summary>
    /// List of namespaces to include in scanning. If empty, all namespaces will be scanned.
    /// </summary>
    public HashSet<string> IncludeNamespaces { get; set; } = new();

    /// <summary>
    /// List of namespaces to exclude from scanning.
    /// </summary>
    public HashSet<string> ExcludeNamespaces { get; set; } = new();

    /// <summary>
    /// If true, will also scan nested namespaces of included namespaces.
    /// </summary>
    public bool ScanNestedNamespaces { get; set; } = true;

    /// <summary>
    /// Optional type name suffix to identify entity classes. E.g., "Entity" would match "UserEntity".
    /// </summary>
    public string? EntitySuffix { get; set; }

    public static EntityLengthsScanningOptions Default => new();

    public bool Equals(EntityLengthsScanningOptions? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ScanNestedNamespaces == other.ScanNestedNamespaces
            && string.Equals(EntitySuffix, other.EntitySuffix, StringComparison.Ordinal)
            && IncludeNamespaces.SetEquals(other.IncludeNamespaces)
            && ExcludeNamespaces.SetEquals(other.ExcludeNamespaces);
    }

    public override bool Equals(object? obj) => Equals(obj as EntityLengthsScanningOptions);

    public override int GetHashCode()
    {
        var hash = 17;
        hash = (hash * 31) + ScanNestedNamespaces.GetHashCode();
        hash = (hash * 31) + (EntitySuffix?.GetHashCode() ?? 0);
        hash = (hash * 31) + IncludeNamespaces.Count;
        hash = (hash * 31) + ExcludeNamespaces.Count;
        return hash;
    }
}
