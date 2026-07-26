using System.Linq;
using EntityLengths.Generator.Tests.Infrastructure;
using Xunit;

namespace EntityLengths.Generator.Tests;

public class GeneratorBehaviorTests
{
    [Fact]
    public void Generates_Constants_For_Record_Entities()
    {
        const string source =
            @"
using System.ComponentModel.DataAnnotations;

namespace TestNamespace;

public record User
{
    [MaxLength(50)]
    public string Name { get; init; } = string.Empty;
}";

        var output = GetGeneratedSource(source);

        Assert.Contains("public static partial class User", output);
        Assert.Contains("public const int NameLength = 50;", output);
    }

    [Fact]
    public void Generates_Constants_For_Every_Entity_Configured_By_A_DbContext()
    {
        const string source =
            @"
using Microsoft.EntityFrameworkCore;

namespace TestNamespace;

public class User
{
    public string Name { get; set; } = string.Empty;
}

public class Order
{
    public string Reference { get; set; } = string.Empty;
}

public class SampleDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(u => u.Name).HasMaxLength(50);
        modelBuilder.Entity<Order>().Property(o => o.Reference).HasMaxLength(30);
    }
}";

        var output = GetGeneratedSource(source);

        Assert.Contains("public const int NameLength = 50;", output);
        Assert.Contains("public const int ReferenceLength = 30;", output);
    }

    [Fact]
    public void Reports_ELG0003_When_Two_Entities_Share_A_Simple_Name()
    {
        const string source =
            @"
using System.ComponentModel.DataAnnotations;

namespace Billing
{
    public class Invoice
    {
        [MaxLength(50)]
        public string Number { get; set; } = string.Empty;
    }
}

namespace Sales
{
    public class Invoice
    {
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
    }
}";

        var result = TestDriver.Run(TestCompilation.Create(source));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("ELG0003", diagnostic.Id);
        Assert.Contains("Billing.Invoice, Sales.Invoice", diagnostic.GetMessage());
    }

    [Fact]
    public void Reports_ELG0004_When_A_Property_Has_Conflicting_Lengths()
    {
        const string source =
            @"
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestNamespace;

public class User
{
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Name).HasMaxLength(100);
    }
}";

        var result = TestDriver.Run(TestCompilation.Create(source));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("ELG0004", diagnostic.Id);
        Assert.Contains("50, 100", diagnostic.GetMessage());
    }

    [Fact]
    public void Excluded_Namespaces_Are_Not_Scanned()
    {
        const string source =
            @"
using System.ComponentModel.DataAnnotations;
using EntityLengths.Generator.Configuration;

[assembly: EntityLengthsGenerator(ExcludeNamespaces = new[] { ""TestNamespace.Ignored"" })]

namespace TestNamespace.Kept
{
    public class User
    {
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
    }
}

namespace TestNamespace.Ignored
{
    public class Secret
    {
        [MaxLength(10)]
        public string Token { get; set; } = string.Empty;
    }
}";

        var output = GetGeneratedSource(source);

        Assert.Contains("public static partial class User", output);
        Assert.DoesNotContain("Secret", output);
    }

    [Fact]
    public void Entity_Suffix_Filter_Is_Applied()
    {
        const string source =
            @"
using System.ComponentModel.DataAnnotations;
using EntityLengths.Generator.Configuration;

[assembly: EntityLengthsGenerator(ScanEntitySuffix = ""Entity"")]

namespace TestNamespace;

public class UserEntity
{
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}

public class Helper
{
    [MaxLength(10)]
    public string Token { get; set; } = string.Empty;
}";

        var output = GetGeneratedSource(source);

        Assert.Contains("public static partial class UserEntity", output);
        Assert.DoesNotContain("Helper", output);
    }

    [Fact]
    public void Generates_The_Output_Documented_In_The_Readme()
    {
        const string source =
            @"
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestNamespace;

public class User
{
    [MaxLength(50)]
    public required string Name { get; set; }

    [StringLength(150)]
    public required string Surname { get; set; }

    [Column(TypeName = ""varchar(200)"")]
    public required string Url { get; set; }

    public required string Description { get; set; }

    public required string Code { get; set; }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(p => p.Description).HasMaxLength(500);
    }
}

public class UserDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(b => b.Code).HasMaxLength(20).IsRequired();

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
        });
    }
}";

        var result = TestDriver.Run(TestCompilation.Create(source));
        var output = result
            .GeneratedTrees.Single(t => t.FilePath.EndsWith("EntityLengths.g.cs"))
            .GetText()
            .ToString();

        // Keeps the README example honest, including the alphabetical ordering it claims.
        const string expected =
            "public static partial class EntityLengths \r\n"
            + "{\r\n"
            + "\tpublic static partial class User\r\n"
            + "\t{\r\n"
            + "\t\tpublic const int CodeLength = 20;\r\n"
            + "\t\tpublic const int DescriptionLength = 500;\r\n"
            + "\t\tpublic const int NameLength = 50;\r\n"
            + "\t\tpublic const int SurnameLength = 150;\r\n"
            + "\t\tpublic const int UrlLength = 200;\r\n"
            + "\t}\r\n"
            + "}\r\n";

        Assert.Empty(result.Diagnostics);
        Assert.Contains(expected.ReplaceLineEndings(), output.ReplaceLineEndings());
    }

    private static string GetGeneratedSource(string source)
    {
        var result = TestDriver.Run(TestCompilation.Create(source));

        return result
            .GeneratedTrees.Single(t => t.FilePath.EndsWith("EntityLengths.g.cs"))
            .GetText()
            .ToString();
    }
}
