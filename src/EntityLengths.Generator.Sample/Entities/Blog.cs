using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLengths.Generator.Sample.Entities;

public class Blog
{
    [Column(TypeName = "varchar(200)")]
    public string Url { get; set; }

    [Column(TypeName = "nvarchar(300)")]
    public string Description { get; set; }

    [Column(TypeName = "char(10)")]
    public string Code { get; set; }
}