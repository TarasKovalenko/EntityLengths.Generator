using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLengths.Generator.Sample.Entities;

/// <summary>
///  Represents a user entity with column type definition attributes.
/// </summary>
public class ColumnTypeDefinitionUser
{
    [Column(TypeName = "varchar(200)")]
    public required string Name { get; set; }

    [Column(TypeName = "nvarchar(300)")]
    public required string Name1 { get; set; }

    [Column(TypeName = "char(400)")]
    public required string Name2 { get; set; }
}
