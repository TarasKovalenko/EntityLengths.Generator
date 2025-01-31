namespace EntityLengths.Generator.Options;

/// <summary>
/// Generator options for the EntityLengths generator.
/// </summary>
public class EntityLengthsGeneratorOptions
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
    /// Scanning options for the EntityLengths generator.
    /// </summary>
    public EntityLengthsScanningOptions ScanningOptions { get; set; } =
        EntityLengthsScanningOptions.Default;

    public static EntityLengthsGeneratorOptions Default => new();
}
