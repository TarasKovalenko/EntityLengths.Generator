namespace EntityLengths.Generator.Sample;

public abstract class Program
{
    public static void Main()
    {
        Console.WriteLine(EntityLengths.FluentUser.NameLength);

        Console.WriteLine(EntityLengths.DataAnnotationUser.NameLength);

        Console.WriteLine(EntityLengths.DbContextUser.NameLength);

        Console.WriteLine(EntityLengths.ColumnTypeDefinitionUser.NameLength);

        Console.WriteLine(EntityLengths.ColumnTypeDefinitionUser.Name1Length);

        Console.WriteLine(EntityLengths.ColumnTypeDefinitionUser.Name2Length);
    }
}
