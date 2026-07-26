namespace EntityLengths.Generator.Configuration;

/// <summary>
/// MSBuild property keys read from the analyzer config global options. They are made visible to the
/// compiler by the <c>build/EntityLengths.Generator.targets</c> file shipped in the NuGet package.
/// </summary>
internal static class MsBuildPropertyNames
{
    private const string Prefix = "build_property.";

    /// <summary>Absolute output path, pre-resolved by the shipped targets file.</summary>
    public const string ResolvedOutputPath = Prefix + "EntityLengthsResolvedOutputPath";

    /// <summary>Raw output path as written by the user, may be relative to the project directory.</summary>
    public const string OutputPath = Prefix + "EntityLengthsOutputPath";

    /// <summary>Namespace used for the file written to disk.</summary>
    public const string OutputNamespace = Prefix + "EntityLengthsOutputNamespace";

    /// <summary>Whether the constants are also added to the current compilation.</summary>
    public const string EmitInProjectOutput = Prefix + "EntityLengthsEmitInProjectOutput";

    /// <summary>Project directory, made compiler visible by the .NET SDK.</summary>
    public const string ProjectDir = Prefix + "ProjectDir";
}
