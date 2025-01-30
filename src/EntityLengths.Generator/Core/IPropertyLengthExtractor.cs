using EntityLengths.Generator.Models;
using Microsoft.CodeAnalysis;

namespace EntityLengths.Generator.Core;

internal interface IPropertyLengthExtractor
{
    EntityTypeInfo? ExtractPropertyLengths(GeneratorSyntaxContext context);
}
