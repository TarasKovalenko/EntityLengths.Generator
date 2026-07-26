using System;
using System.Linq;
using EntityLengths.Generator.Options;

namespace EntityLengths.Generator.Core;

internal class NamespaceFilter(EntityLengthsScanningOptions options)
{
    /// <summary>
    /// Decides whether the type that declared the lengths passes the configured namespace and suffix
    /// filters. Applied after extraction so the syntax transform stays independent of the options.
    /// </summary>
    public bool ShouldInclude(string declaringNamespace, string declaringTypeName)
    {
        if (!ShouldProcessNamespace(declaringNamespace))
        {
            return false;
        }

        // Check entity suffix if configured
        if (!string.IsNullOrEmpty(options.EntitySuffix))
        {
            return declaringTypeName.EndsWith(options.EntitySuffix, StringComparison.Ordinal);
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
