using System.Linq;
using EntityLengths.Generator.Core;
using EntityLengths.Generator.Extensions;
using EntityLengths.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityLengths.Generator.Extractors;

internal class FluentConfigurationExtractor : IPropertyLengthExtractor
{
    public EquatableArray<EntityTypeInfo> ExtractPropertyLengths(GeneratorSyntaxContext context)
    {
        var typeSyntax = (TypeDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var baseType = typeSyntax.GetEntityTypeConfigurationBase(semanticModel);
        if (baseType is null)
        {
            return EquatableArray<EntityTypeInfo>.Empty;
        }

        var entityType = (baseType as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();
        if (entityType is null)
        {
            return EquatableArray<EntityTypeInfo>.Empty;
        }

        return EntityTypeInfoFactory.Create(
            entityType,
            typeSyntax,
            semanticModel,
            typeSyntax.FindMaxLengthProperties()
        );
    }
}
