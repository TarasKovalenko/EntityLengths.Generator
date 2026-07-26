# EntityLengths.Generator

[![Made in Ukraine](https://img.shields.io/badge/made_in-ukraine-ffd700.svg?labelColor=0057b7)](https://taraskovalenko.github.io/)
[![build](https://github.com/TarasKovalenko/EntityLengths.Generator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/TarasKovalenko/EntityLengths.Generator/actions)
[![EntityLengths.Generator NuGet current](https://img.shields.io/nuget/v/EntityLengths.Generator?label=EntityLengths.Generator)](https://www.nuget.org/packages/EntityLengths.Generator/)

## Goals
This library is a C# source generator designed to automatically generate string length constants from Entity Framework configurations and data annotations. 
By analyzing your model configurations, it eliminates the need for manual constant maintenance and reduces the risk of hardcoded length values across your application.

## Terms of use

By using this project or its source code, for any purpose and in any shape or form, you grant your **implicit agreement** to all of the following statements:

- You unequivocally condemn Russia and its military aggression against Ukraine
- You recognize that Russia is an occupant that unlawfully invaded a sovereign state
- You agree that [Russia is a terrorist state](https://www.europarl.europa.eu/doceo/document/RC-9-2022-0482_EN.html)
- You fully support Ukraine's territorial integrity, including its claims over [temporarily occupied territories](https://en.wikipedia.org/wiki/Russian-occupied_territories_of_Ukraine)
- You reject false narratives perpetuated by Russian state propaganda

To learn more about the war and how you can help, [click here](https://war.ukraine.ua/). Glory to Ukraine! 🇺🇦

## Benefits

- **Automatic Constant Generation**: Automatically create string length constants based on your existing model configurations
- **Reduced Redundancy**: Eliminate manual maintenance of string length constants
- **Compile-Time Safety**: Generate constants at compile-time, ensuring type safety and preventing runtime errors
- **Flexible Configuration**: Supports multiple ways of defining string lengths across different .NET and Entity Framework patterns

## Features

- Extracts string length configurations from:
    - EF Core Fluent API configurations (`HasMaxLength`)
    - Data Annotations
      - `[MaxLength]`
      - `[StringLength]`
    - Column type definitions 
      - `[Column(TypeName = "varchar(200)")]`
      - `[Column(TypeName = "nvarchar(200)")]`
      - `[Column(TypeName = "char(200)")]`
    - DbContext configurations (`OnModelCreating`)
- Works on `class` and `record` entities
- Can write the constants into another project, for Clean Architecture and similar layouts
  (see [Writing the constants into another project](#writing-the-constants-into-another-project-clean-architecture))

## Samples

- [`samples/CleanArchitecture`](samples/CleanArchitecture/README.md) - three projects
  (Domain / Infrastructure / Api) showing the constants generated into a different layer than the one
  holding the EF configurations.
- [`src/EntityLengths.Generator.Sample`](src/EntityLengths.Generator.Sample) - single project showing
  every supported way to declare a length.

## Supported frameworks

The generator ships as a `netstandard2.0` analyzer, which is what Roslyn loads, and works in projects
targeting **.NET 8, .NET 9 and .NET 10**. Tests and samples are built and run against all three, each
with the matching EF Core line (8.0.x, 9.0.x, 10.0.x).

## Installation

Install the library via NuGet Package Manager:

```bash
dotnet add package EntityLengths.Generator
```

Version 1.1.0 and earlier declared `Microsoft.CodeAnalysis.CSharp [4.8.0, 4.12.0]` as a package
dependency, which clashes with anything else that pulls in a newer Roslyn - for example
`Microsoft.EntityFrameworkCore.Design` 10.x, which requires 5.0.0 or newer:

```
error NU1107: Version conflict detected for Microsoft.CodeAnalysis.CSharp
```

From 1.1.1 the package declares no dependencies at all, because the compiler already supplies Roslyn
to analyzers. Upgrade to fix the restore. If you are stuck on an older version, reference Roslyn
directly as the error suggests:

```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="5.0.0" PrivateAssets="all"/>
```

## Usage

The generator supports a few ways to define string lengths, and combines everything it finds for one
entity into a single nested class:

```csharp
public class User
{
    // Using MaxLength attribute
    [MaxLength(50)]
    public required string Name { get; set; }

    // Using StringLength attribute
    [StringLength(150)]
    public required string Surname { get; set; }

    // Using Column attribute
    [Column(TypeName = "varchar(200)")]
    public required string Url { get; set; }

    // Configured by the Fluent API below
    public required string Description { get; set; }

    // Configured in OnModelCreating below
    public required string Code { get; set; }
}

// Using Fluent API
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(p => p.Description)
            .HasMaxLength(500);
    }
}

// DbContext configuration
public class UserDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // fluent API style
        modelBuilder.Entity<User>().Property(b => b.Code).HasMaxLength(20).IsRequired();

        // or lambda API
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
        });
    }
}
```

Generated output (entities and properties are sorted by name):

```csharp
public static partial class EntityLengths 
{
    public static partial class User
    {
        public const int CodeLength = 20;
        public const int DescriptionLength = 500;
        public const int NameLength = 50;
        public const int SurnameLength = 150;
        public const int UrlLength = 200;
    }
}
```

Configuring the same property twice with different lengths, for example `[MaxLength(50)]` plus a
`HasMaxLength(100)` in the Fluent API, is reported as `ELG0004` instead of being silently resolved.

## Configuration
There are ways to configure EntityLengths.Generator. Configuration values are needed during compile-time since this is a source generator:

- Assembly level attribute for configuration: `EntityLengthsGeneratorAttribute`

```csharp
[assembly: EntityLengthsGenerator(
    GenerateDocumentation = true,
    GeneratedClassName = "Constants",
    LengthSuffix = "Length",
    IncludeNamespaces = ["EntityLengths.Generator.Sample.Entities"],
    ExcludeNamespaces = ["EntityLengths.Generator.Sample.Entities.Exclude"],
    ScanNestedNamespaces = true,
    ScanEntitySuffix = "User",
    Namespace = "EntityLengths.Generator.Sample"
)]
```

- `GenerateDocumentation` - Generates XML documentation for the generated class. Default is `false`.
- `GeneratedClassName` - The name of the generated class. Default is `EntityLengths`.
- `LengthSuffix` - The suffix for the generated length constants. Default is `Length`.
- `IncludeNamespaces` - The namespaces to include in the generation process. Default is `null`.
- `ExcludeNamespaces` - The namespaces to exclude from the generation process. Default is `null`.
- `ScanNestedNamespaces` - Scans nested namespaces for entities. Default is `true`.
- `ScanEntitySuffix` - The suffix for the entity classes to scan. Default is `null`.
- `Namespace` - The namespace for the generated class. Default is `null`.

### Writing the constants into another project (Clean Architecture)

A source generator can only add code to the project it runs in. When the EF configurations live in
`Infrastructure` but the constants belong in `Domain` (which must not reference `Infrastructure`),
the generator can additionally write the constants to a file in that other project. The file is a
normal source file: commit it, and the other project compiles it like any other code.

Configure it with MSBuild properties in the project that contains the EF configurations:

```xml
<!-- MyApp.Infrastructure.csproj -->
<PropertyGroup>
    <!-- Relative to this project directory, or an absolute path -->
    <EntityLengthsOutputPath>../MyApp.Domain/Generated/EntityLengths.cs</EntityLengthsOutputPath>
    <!-- Namespace of the project that compiles the file -->
    <EntityLengthsOutputNamespace>MyApp.Domain</EntityLengthsOutputNamespace>
</PropertyGroup>
```

- `EntityLengthsOutputPath` - File the constants are written to. Relative paths resolve against the
  project directory. Default is empty, meaning nothing is written to disk.
- `EntityLengthsOutputNamespace` - Namespace used in the written file. Falls back to the
  `Namespace` attribute value and then to the assembly name (reported as `ELG0002`).
- `EntityLengthsEmitInProjectOutput` - Whether the constants are also compiled into the current
  project. Default is `true`, and `false` as soon as `EntityLengthsOutputPath` is set, because
  otherwise the same type would exist in both assemblies. Set it to `true` explicitly if the two
  namespaces differ and you want both.

Notes:

- The file is written during compilation, so `Domain` picks up changes on the **next** build. Commit
  the generated file and treat it as checked-in generated code.
- A runnable end-to-end example is in
  [`samples/CleanArchitecture`](samples/CleanArchitecture/README.md) (Domain / Infrastructure / Api).
- The file is only touched when its content actually changes, so editing in an IDE does not churn it.
- Fluent API and `OnModelCreating` lengths are read from source, so the generator must run in the
  project that contains those configurations - it cannot read them from a referenced assembly.

### Diagnostics

| ID | Severity | Meaning |
|----|----------|---------|
| `ELG0001` | Warning | The constants file could not be written to `EntityLengthsOutputPath` |
| `ELG0002` | Warning | `EntityLengthsOutputPath` is set without a namespace, so the assembly name is used |
| `ELG0003` | Warning | Two entity types share a simple name, so their constants are merged into one nested class |
| `ELG0004` | Warning | A property has conflicting configured lengths; the first one found is used |

Generated output:

```csharp
// <auto-generated/>
namespace EntityLengths.Generator.Sample;

/// <summary>
/// Contains generated string length constants for entity properties
/// </summary>
public static partial class Constants 
{
    /// <summary>
    /// Length constants for ColumnTypeDefinitionUser
    /// </summary>
    public static partial class ColumnTypeDefinitionUser
    {
        /// <summary>
        /// Maximum length for Name
        /// </summary>
        public const int NameLength = 200;
        /// <summary>
        /// Maximum length for Name1
        /// </summary>
        public const int Name1Length = 300;
        /// <summary>
        /// Maximum length for Name2
        /// </summary>
        public const int Name2Length = 400;
    }
    
    /// <summary>
    /// Length constants for DataAnnotationUser
    /// </summary>
    public static partial class DataAnnotationUser
    {
        /// <summary>
        /// Maximum length for Name
        /// </summary>
        public const int NameLength = 50;
        /// <summary>
        /// Maximum length for Surname
        /// </summary>
        public const int SurnameLength = 150;
    }
    
    /// <summary>
    /// Length constants for DbContextUser
    /// </summary>
    public static partial class DbContextUser
    {
        /// <summary>
        /// Maximum length for Description
        /// </summary>
        public const int DescriptionLength = 500;
        /// <summary>
        /// Maximum length for Description2
        /// </summary>
        public const int Description2Length = 500;
        /// <summary>
        /// Maximum length for Name
        /// </summary>
        public const int NameLength = 50;
        /// <summary>
        /// Maximum length for Name2
        /// </summary>
        public const int Name2Length = 50;
    }
    
    /// <summary>
    /// Length constants for FluentUser
    /// </summary>
    public static partial class FluentUser
    {
        /// <summary>
        /// Maximum length for Description
        /// </summary>
        public const int DescriptionLength = 500;
        /// <summary>
        /// Maximum length for Name
        /// </summary>
        public const int NameLength = 50;
    }
}
```

## Building this repository

```bash
dotnet build          # generator, tests and both samples, for net8.0 / net9.0 / net10.0
dotnet test
```

Package versions are managed centrally in `Directory.Packages.props`, so a `PackageReference` in a
project file must not carry a `Version` attribute. The target frameworks come from
`$(SupportedTargetFrameworks)` in `Directory.Build.props`.
