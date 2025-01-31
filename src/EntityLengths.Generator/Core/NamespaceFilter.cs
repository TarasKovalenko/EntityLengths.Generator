using EntityLengths.Generator.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityLengths.Generator.Core;

internal class NamespaceFilter(EntityLengthsScanningOptions options)
{
    public bool ShouldProcessNode(SyntaxNode node, SemanticModel semanticModel)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }

        // Get containing namespace
        var symbol = semanticModel.GetDeclaredSymbol(classDeclaration);
        if (symbol == null)
        {
            return false;
        }

        var namespaceName = symbol.ContainingNamespace.ToString();

        // Check if we should process this namespace
        if (!ShouldProcessNamespace(namespaceName))
        {
            return false;
        }

        // Check entity suffix if configured
        if (!string.IsNullOrEmpty(options.EntitySuffix))
        {
            return classDeclaration.Identifier.Text.EndsWith(options.EntitySuffix);
        }

        return true;
    }

    private bool ShouldProcessNamespace(string namespaceName)
    {
        // Check excluded namespaces first
        if (
            options.ExcludeNamespaces.Any(excluded =>
                namespaceName.Equals(excluded)
                || (options.ScanNestedNamespaces && namespaceName.StartsWith($"{excluded}."))
            )
        )
        {
            return false;
        }

        // If no included namespaces are specified, process all non-excluded namespaces
        if (options.IncludeNamespaces.Count == 0)
        {
            return true;
        }

        // Check if namespace is included
        return options.IncludeNamespaces.Any(included =>
            namespaceName.Equals(included)
            || (options.ScanNestedNamespaces && namespaceName.StartsWith($"{included}."))
        );
    }
}
