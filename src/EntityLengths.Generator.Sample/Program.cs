namespace EntityLengths.Generator.Sample;

public abstract class Program
{
    public static void Main()
    {
        Console.WriteLine(EntityLengths.User.NameLength);

        Console.WriteLine(EntityLengths.Organization.NameLength);

        Console.WriteLine(EntityLengths.Blog.UrlLength);

        Console.WriteLine(EntityLengths.Blog.DescriptionLength);

        Console.WriteLine(EntityLengths.Blog.CodeLength);
    }
}
