using System.Collections.Generic;
using System.Linq;
using EntityLengths.Generator.Core;
using EntityLengths.Generator.Extensions;
using EntityLengths.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityLengths.Generator.Extractors;

internal class AttributeExtractor : IPropertyLengthExtractor
{
    public EquatableArray<EntityTypeInfo> ExtractPropertyLengths(GeneratorSyntaxContext context)
    {
        var typeSyntax = (TypeDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(typeSyntax) as INamedTypeSymbol;

        if (classSymbol is null)
        {
            return EquatableArray<EntityTypeInfo>.Empty;
        }

        var stringPropertiesWithMaxLength = new List<PropertyMaxLength>();

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.Type.SpecialType != SpecialType.System_String)
            {
                continue;
            }

            if (member.TryGetMaxLengthFromAttribute(out var maxLength))
            {
                stringPropertiesWithMaxLength.Add(new PropertyMaxLength(member.Name, maxLength));
                continue;
            }

            if (member.TryGetMaxLengthFromColumnType(out maxLength))
            {
                stringPropertiesWithMaxLength.Add(new PropertyMaxLength(member.Name, maxLength));
                continue;
            }

            if (member.TryGetStringLengthFromAttribute(out maxLength))
            {
                stringPropertiesWithMaxLength.Add(new PropertyMaxLength(member.Name, maxLength));
            }
        }

        return EntityTypeInfoFactory.Create(
            classSymbol,
            typeSyntax,
            semanticModel,
            stringPropertiesWithMaxLength
        );
    }
}
