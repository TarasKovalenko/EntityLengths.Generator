# EntityLengths.Generator

[![Made in Ukraine](https://img.shields.io/badge/made_in-ukraine-ffd700.svg?labelColor=0057b7)](https://taraskovalenko.github.io/)

A C# source generator that automatically generates string length constants from Entity Framework configurations and data annotations.

## Terms of use

By using this project or its source code, for any purpose and in any shape or form, you grant your **implicit agreement** to all the following statements:

- You **condemn Russia and its military aggression against Ukraine**
- You **recognize that Russia is an occupant that unlawfully invaded a sovereign state**
- You **support Ukraine's territorial integrity, including its claims over temporarily occupied territories of Crimea and Donbas**
- You **reject false narratives perpetuated by Russian state propaganda**

## Features

- Extracts string length configurations from:
    - EF Core Fluent API configurations (`HasMaxLength`)
    - Data Annotations (`[MaxLength]`)
    - Column type definitions 
      - `[Column(TypeName = "varchar(200)")]`
      - `[Column(TypeName = "nvarchar(200)")]`
      - `[Column(TypeName = "char(200)")]`
    - DbContext configurations (`OnModelCreating`)

## Usage

The generator supports three ways to define string lengths:

```csharp
// Using MaxLength attribute
public class User
{
    [MaxLength(50)]
    public string Name { get; set; }
}

// Using Column attribute
public class User
{
    [Column(TypeName = "varchar(200)")]
    public string Url { get; set; }
}

// Using Fluent API
public class UserConfiguration : IEntityTypeConfiguration
{
    public void Configure(EntityTypeBuilder builder)
    {
        builder.Property(p => p.Name)
            .HasMaxLength(50);
    }
}

// DbContext configuration
public class User
{
    public required string Surname { get; set; }
}

public class UserDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(b => b.Surname).HasMaxLength(150).IsRequired();
    }
}
```

Generated output:

```csharp
public static partial class EntityLengths 
{
    public static partial class User
    {
        public const int NameLength = 50;
        public const int UrlLength = 200;
        public const int SurnameLength = 200;
    }
}
```