using Microsoft.CodeAnalysis;

namespace EntityLengths.Generator.Models;

public sealed record EntityConfigurationInfo(
    ITypeSymbol EntityType,
    List<PropertyMaxLength> MaxLengthProperties
)
{
    public ITypeSymbol EntityType { get; } = EntityType;
    public List<PropertyMaxLength> MaxLengthProperties { get; } = MaxLengthProperties;
}
