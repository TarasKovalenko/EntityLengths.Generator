using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace EntityLengths.Generator.Models;

public sealed record EntityTypeInfo(
    ITypeSymbol EntityType,
    List<PropertyMaxLength> StringProperties
)
{
    public ITypeSymbol EntityType { get; } = EntityType;
    public List<PropertyMaxLength> StringProperties { get; } = StringProperties;
}
