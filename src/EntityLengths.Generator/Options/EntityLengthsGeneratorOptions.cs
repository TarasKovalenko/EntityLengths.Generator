using System;

namespace EntityLengths.Generator.Options;

/// <summary>
/// Generator options for the EntityLengths generator.
/// </summary>
public class EntityLengthsGeneratorOptions : IEquatable<EntityLengthsGeneratorOptions>
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
    /// Optional absolute file path the constants are additionally written to, so another project
    /// (for example the Domain layer of a Clean Architecture solution) can compile them as a normal
    /// checked-in file. Configured through the <c>EntityLengthsOutputPath</c> MSBuild property.
    /// If null, the constants are only added to the current compilation.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Namespace used for the file written to <see cref="OutputPath"/>. Configured through the
    /// <c>EntityLengthsOutputNamespace</c> MSBuild property. Falls back to <see cref="Namespace"/>
    /// and then to the current assembly name.
    /// </summary>
    public string? OutputNamespace { get; set; }

    /// <summary>
    /// If true, the constants are also added to the current compilation. Defaults to true, and to
    /// false when <see cref="OutputPath"/> is set - otherwise the same type would exist both in this
    /// assembly and in the project that compiles the written file. Configured through the
    /// <c>EntityLengthsEmitInProjectOutput</c> MSBuild property.
    /// </summary>
    public bool EmitInProjectOutput { get; set; } = true;

    /// <summary>
    /// Scanning options for the EntityLengths generator.
    /// </summary>
    public EntityLengthsScanningOptions ScanningOptions { get; set; } =
        EntityLengthsScanningOptions.Default;

    public static EntityLengthsGeneratorOptions Default => new();

    public bool Equals(EntityLengthsGeneratorOptions? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(GeneratedClassName, other.GeneratedClassName, StringComparison.Ordinal)
            && string.Equals(LengthSuffix, other.LengthSuffix, StringComparison.Ordinal)
            && GenerateDocumentation == other.GenerateDocumentation
            && string.Equals(Namespace, other.Namespace, StringComparison.Ordinal)
            && string.Equals(OutputPath, other.OutputPath, StringComparison.Ordinal)
            && string.Equals(OutputNamespace, other.OutputNamespace, StringComparison.Ordinal)
            && EmitInProjectOutput == other.EmitInProjectOutput
            && ScanningOptions.Equals(other.ScanningOptions);
    }

    public override bool Equals(object? obj) => Equals(obj as EntityLengthsGeneratorOptions);

    public override int GetHashCode()
    {
        var hash = 17;
        hash = (hash * 31) + GeneratedClassName.GetHashCode();
        hash = (hash * 31) + LengthSuffix.GetHashCode();
        hash = (hash * 31) + GenerateDocumentation.GetHashCode();
        hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
        hash = (hash * 31) + (OutputPath?.GetHashCode() ?? 0);
        hash = (hash * 31) + (OutputNamespace?.GetHashCode() ?? 0);
        hash = (hash * 31) + EmitInProjectOutput.GetHashCode();
        hash = (hash * 31) + ScanningOptions.GetHashCode();
        return hash;
    }
}
