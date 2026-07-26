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

    private static string GetGeneratedSource(string source)
    {
        var result = TestDriver.Run(TestCompilation.Create(source));

        return result
            .GeneratedTrees.Single(t => t.FilePath.EndsWith("EntityLengths.g.cs"))
            .GetText()
            .ToString();
    }
}
