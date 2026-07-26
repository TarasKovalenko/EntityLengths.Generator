using EntityLengths.Generator.Models;
using Microsoft.CodeAnalysis;

namespace EntityLengths.Generator.Core;

internal interface IPropertyLengthExtractor
{
    /// <summary>
    /// Extracts the lengths declared by the current syntax node. One node can configure several
    /// entities - a DbContext usually configures all of them - so this returns a collection.
    /// </summary>
    EquatableArray<EntityTypeInfo> ExtractPropertyLengths(GeneratorSyntaxContext context);
}
