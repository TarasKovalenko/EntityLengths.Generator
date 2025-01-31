namespace EntityLengths.Generator.Configuration;

[AttributeUsage(AttributeTargets.Assembly)]
public class EntityLengthsGeneratorAttribute : Attribute
{
    /// <summary>
    /// The name of the generated static class. Default is "EntityLengths"
    /// </summary>
    public string GeneratedClassName { get; set; } = Constants.ClassName;

    /// <summary>
    /// Optional suffix for the generated constant fields. Default is "Length"
    /// </summary>
    public string LengthSuffix { get; set; } = Constants.LengthSuffix;

    /// <summary>
    /// If true, will add XML documentation to generated code. Default is false
    /// </summary>
    public bool GenerateDocumentation { get; set; }

    /// <summary>
    /// Optional namespace override. If null, uses the project's default namespace
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    ///  The namespace to scan for entities.
    ///  If not specified, the namespace of the assembly is used.
    /// </summary>
    public string[]? IncludeNamespaces { get; set; }

    /// <summary>
    ///  The namespaces to exclude from scanning for entities.
    ///  If not specified, no namespaces are excluded.
    /// </summary>
    public string[]? ExcludeNamespaces { get; set; }

    /// <summary>
    /// If true, will also scan nested namespaces of included namespaces. Default is true.
    /// </summary>
    public bool ScanNestedNamespaces { get; set; } = true;

    /// <summary>
    /// Optional type name suffix to identify entity classes in the scanning process. E.g., "Entity" would match "UserEntity".
    /// </summary>
    public string? ScanEntitySuffix { get; set; }
}
