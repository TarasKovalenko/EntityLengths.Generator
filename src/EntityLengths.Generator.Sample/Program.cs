using EntityLengths.Generator.Configuration;
using EntityLengths.Generator.Sample;

[assembly: EntityLengthsGenerator(
    GenerateDocumentation = true,
    GeneratedClassName = "Constants",
    IncludeNamespaces = ["EntityLengths.Generator.Sample.Entities"],
    ExcludeNamespaces = ["EntityLengths.Generator.Sample.Entities.Exclude"],
    ScanNestedNamespaces = true,
    ScanEntitySuffix = "User",
    Namespace = "EntityLengths.Generator.Sample",
    LengthSuffix = "Length"
)]

Console.WriteLine(Constants.FluentUser.NameLength);

Console.WriteLine(Constants.DataAnnotationUser.NameLength);

Console.WriteLine(Constants.DataAnnotationUser.SurnameLength);

Console.WriteLine(Constants.DbContextUser.NameLength);

Console.WriteLine(Constants.ColumnTypeDefinitionUser.NameLength);

Console.WriteLine(Constants.ColumnTypeDefinitionUser.Name1Length);

Console.WriteLine(Constants.ColumnTypeDefinitionUser.Name2Length);
