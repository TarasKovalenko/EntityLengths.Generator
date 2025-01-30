using EntityLengths.Generator.Core;
using EntityLengths.Generator.Extensions;
using EntityLengths.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityLengths.Generator.Configuration;

internal class FluentConfigurationExtractor : IPropertyLengthExtractor
{
    public EntityTypeInfo? ExtractPropertyLengths(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var baseType = classSyntax.GetEntityTypeConfigurationBase(semanticModel);
        if (baseType is null)
        {
            return null;
        }

        var entityType = (baseType as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();
        if (entityType is null)
        {
            return null;
        }

        var maxLengthProperties = classSyntax.FindMaxLengthProperties();

        return maxLengthProperties.Any()
            ? new EntityTypeInfo(entityType, maxLengthProperties)
            : null;
    }
}
