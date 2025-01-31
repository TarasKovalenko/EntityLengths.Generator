namespace EntityLengths.Generator.Options;

/// <summary>
/// Scanning options for the EntityLengths generator.
/// </summary>
public class EntityLengthsScanningOptions
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
}
