# Clean Architecture sample

Shows `EntityLengthsOutputPath`: the EF configurations live in the Infrastructure layer, but the
generated constants end up in the Domain layer, which does not reference Infrastructure.

```
MyApp.Domain            no EF Core, no generator, no Infrastructure reference
├── Entities/           plain POCOs (Customer, Order)
├── Validation/         uses EntityLengths.Customer.NameLength
└── Generated/          <- EntityLengths.cs is written here and committed

MyApp.Infrastructure    references Domain + EF Core + the generator
└── Persistence/        AppDbContext and IEntityTypeConfiguration<T> classes (the lengths)

MyApp.Api               composition root, references Domain + Infrastructure
```

The relevant part of `MyApp.Infrastructure.csproj`:

```xml
<PropertyGroup>
    <EntityLengthsOutputPath>../MyApp.Domain/Generated/EntityLengths.cs</EntityLengthsOutputPath>
    <EntityLengthsOutputNamespace>MyApp.Domain</EntityLengthsOutputNamespace>
</PropertyGroup>
```

Because an output path is set, the generator does **not** add the constants to
`MyApp.Infrastructure` as well - otherwise `MyApp.Domain.EntityLengths` would exist in two
assemblies. Infrastructure sees them through its Domain reference.

## Running it

```bash
dotnet run --project samples/CleanArchitecture/MyApp.Api --framework net10.0
```

Output:

```
Customer.Name      max 100
Customer.Email     max 256
Customer.Notes     max 2000
Order.Reference    max 32
Order.ShippingAddr max 400
invalid: Name must be 1 to 100 characters.
```

## Changing a length

Edit `CustomerConfiguration.Configure`, for example `HasMaxLength(100)` to `HasMaxLength(120)`, then
build twice:

1. The first build writes the new `Generated/EntityLengths.cs` while Infrastructure compiles - too
   late for the Domain compilation that already ran in the same build.
2. The second build compiles Domain against the new value.

That is why the file is committed: a clean clone builds in one pass, and a length change shows up in
a reviewable diff. Treat it as checked-in generated code and never edit it by hand.

## Multi-targeting

All three projects build for `net10.0` and `net11.0` (see `$(SupportedTargetFrameworks)` in
`Directory.Build.props`), with the EF Core version following the target framework. The inner builds
run in parallel and all write the same constants file; the generator only rewrites it when the
content actually changes.
