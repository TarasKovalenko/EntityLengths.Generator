namespace EntityLengths.Generator.Models;

public sealed record PropertyMaxLength(string PropertyName, int MaxLength)
{
    public string PropertyName { get; } = PropertyName;
    public int MaxLength { get; } = MaxLength;
}