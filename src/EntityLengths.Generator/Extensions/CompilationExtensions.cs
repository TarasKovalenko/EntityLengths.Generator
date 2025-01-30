using EntityLengths.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityLengths.Generator.Extensions;

internal static class CompilationExtensions
{
    private const string EntityTypeConfigurationInterface = "IEntityTypeConfiguration";
    private const string HasMaxLengthMethod = "HasMaxLength";

    public static ITypeSymbol? GetEntityTypeConfigurationBase(
        this ClassDeclarationSyntax classSyntax,
        SemanticModel semanticModel
    )
    {
        return classSyntax
            .BaseList?.Types.Select(t => semanticModel.GetTypeInfo(t.Type).Type)
            .Where(t => t != null)
            .FirstOrDefault(t => t!.Name.StartsWith(EntityTypeConfigurationInterface));
    }

    public static List<PropertyMaxLength> FindMaxLengthProperties(
        this ClassDeclarationSyntax classSyntax
    )
    {
        var maxLengthProperties = new List<PropertyMaxLength>();

        var maxLengthInvocations = classSyntax
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(inv => inv.Expression.ToString().EndsWith(HasMaxLengthMethod));

        foreach (var invocation in maxLengthInvocations)
        {
            if (TryGetPropertyMaxLength(invocation, out var propertyMaxLength))
            {
                maxLengthProperties.Add(propertyMaxLength);
            }
        }

        return maxLengthProperties;
    }

    private static bool TryGetPropertyMaxLength(
        InvocationExpressionSyntax invocation,
        out PropertyMaxLength propertyMaxLength
    )
    {
        propertyMaxLength = default!;

        var lambdaExpr = invocation
            .DescendantNodes()
            .OfType<SimpleLambdaExpressionSyntax>()
            .FirstOrDefault();

        var propertyAccess = lambdaExpr?.Body as MemberAccessExpressionSyntax;
        var propertyName = propertyAccess?.Name.Identifier.Text ?? string.Empty;

        if (string.IsNullOrEmpty(propertyName))
        {
            return false;
        }

        var maxLengthArg = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (!int.TryParse(maxLengthArg?.ToString(), out var maxLength))
        {
            return false;
        }

        propertyMaxLength = new PropertyMaxLength(propertyName, maxLength);
        return true;
    }
}
