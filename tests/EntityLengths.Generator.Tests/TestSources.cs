﻿namespace EntityLengths.Generator.Tests;

public static class TestSources
{
    public const string FluentApiEntity =
        @"
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestNamespace;

public class User
{
    public required string Name { get; set; }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
    }
}";

    public const string DataAnnotationsEntity =
        @"
using System.ComponentModel.DataAnnotations;

namespace TestNamespace;

public class User
{
    [MaxLength(50)]
    public required string Name { get; set; }
}";

    public const string DataAnnotationsStringLengthEntity =
        @"
using System.ComponentModel.DataAnnotations;

namespace TestNamespace;

public class User
{
    [StringLength(50)]
    public required string Name { get; set; }
}";

    public const string ColumnTypeDefinitionEntity =
        @"
using System.ComponentModel.DataAnnotations.Schema;

namespace TestNamespace;

public class User
{
    [Column(TypeName = ""varchar(50)"")]
    public required string Name { get; set; }
}";

    public const string DbContextEntity =
        @"
using Microsoft.EntityFrameworkCore;

namespace TestNamespace;

public class User
{
    public required string Name { get; set; }
}

public class SampleDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(b => b.Name).HasMaxLength(50).IsRequired();\
    }
}";

    public const string DbContextLambdaEntity =
        @"
using Microsoft.EntityFrameworkCore;

namespace TestNamespace;

public class User
{
    public required string Name { get; set; }
}

public class SampleDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
        });
    }
}";

    public const string ExpectedOutput =
        @"// <auto-generated/>
namespace EntityMaxLengthGeneratorTests;

public static partial class EntityLengths 
{
	public static partial class User
	{
		public const int NameLength = 50;
	}
}
";
}
