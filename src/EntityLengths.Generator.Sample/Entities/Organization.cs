using System.ComponentModel.DataAnnotations;

namespace EntityLengths.Generator.Sample.Entities;

public class Organization
{
    [MaxLength(123)]
    public required string Name { get; set; }
}
