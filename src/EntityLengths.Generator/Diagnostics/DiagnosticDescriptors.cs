using Microsoft.CodeAnalysis;

namespace EntityLengths.Generator.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string Category = "EntityLengths";

    /// <summary>
    /// The constants file could not be written to the configured output path.
    /// </summary>
    public static readonly DiagnosticDescriptor OutputWriteFailed = new(
        id: "ELG0001",
        title: "Could not write the generated constants file",
        messageFormat: "Could not write the generated constants to '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// An output path was configured without a namespace, so the current assembly name is used, which
    /// is almost never right for a file compiled by another project.
    /// </summary>
    public static readonly DiagnosticDescriptor OutputNamespaceMissing = new(
        id: "ELG0002",
        title: "No namespace configured for the generated constants file",
        messageFormat: "EntityLengthsOutputPath is set but no namespace is configured, so '{0}' is used. Set the EntityLengthsOutputNamespace MSBuild property to the namespace of the project that compiles the file.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Two entity types share a simple name, so their constants end up in one nested class.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateEntityName = new(
        id: "ELG0003",
        title: "Entity types share a simple name",
        messageFormat: "Entity types {0} share the simple name '{1}', so their length constants are merged into a single nested class",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// The same property has conflicting lengths from different sources.
    /// </summary>
    public static readonly DiagnosticDescriptor ConflictingPropertyLength = new(
        id: "ELG0004",
        title: "Conflicting lengths for a property",
        messageFormat: "Property '{0}.{1}' has conflicting configured lengths ({2}); using {3}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
}
