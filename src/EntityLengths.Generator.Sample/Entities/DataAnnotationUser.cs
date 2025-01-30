using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace EntityLengths.Generator.Sample.Entities;

/// <summary>
///  Represents a user entity with data annotation attributes.
/// </summary>
public class DataAnnotationUser
{
    [MaxLength(50)]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>"
    )]
    public required string Name { get; set; }

    [StringLength(150)]
    public required string Surname { get; set; }
}
