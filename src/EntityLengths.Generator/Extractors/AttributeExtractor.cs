using EntityLengths.Generator.Core;
using EntityLengths.Generator.Extensions;
using EntityLengths.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityLengths.Generator.Extractors;

internal class AttributeExtractor : IPropertyLengthExtractor
{
    public EntityTypeInfo? ExtractPropertyLengths(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax);

        if (classSymbol is null)
        {
            return null;
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
            }
        }

        return stringPropertiesWithMaxLength.Any()
            ? new EntityTypeInfo(classSymbol, stringPropertiesWithMaxLength)
            : null;
    }
}
