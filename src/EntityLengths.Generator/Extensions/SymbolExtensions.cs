using System;
using System.Linq;
using EntityLengths.Generator.Utils;
using Microsoft.CodeAnalysis;

namespace EntityLengths.Generator.Extensions;

internal static class SymbolExtensions
{
    public static bool TryGetMaxLengthFromAttribute(
        this IPropertySymbol property,
        out int maxLength
    )
    {
        maxLength = 0;

        var maxLengthAttribute = property
            .GetAttributes()
            .FirstOrDefault(a =>
                string.Equals(
                    a.AttributeClass?.Name,
                    Constants.MaxLengthAttribute,
                    StringComparison.Ordinal
                )
            );

        if (
            maxLengthAttribute is { ConstructorArguments.Length: > 0 }
            && maxLengthAttribute.ConstructorArguments[0].Value is int length
        )
        {
            maxLength = length;
            return true;
        }

        return false;
    }

    public static bool TryGetMaxLengthFromColumnType(
        this IPropertySymbol property,
        out int maxLength
    )
    {
        maxLength = 0;

        var columnAttribute = property
            .GetAttributes()
            .FirstOrDefault(a =>
                string.Equals(
                    a.AttributeClass?.Name,
                    Constants.ColumnAttribute,
                    StringComparison.Ordinal
                )
            );

        if (columnAttribute == null)
        {
            return false;
        }

        var typeNameArg = columnAttribute
            .NamedArguments.FirstOrDefault(arg =>
                string.Equals(arg.Key, Constants.TypeName, StringComparison.Ordinal)
            )
            .Value;

        if (typeNameArg.IsNull)
        {
            return false;
        }

        var typeNameValue = typeNameArg.Value?.ToString();
        if (string.IsNullOrEmpty(typeNameValue))
        {
            return false;
        }

        var match = RegexPatterns.VarCharLength.Match(typeNameValue);
        return match.Success && int.TryParse(match.Groups[1].Value, out maxLength);
    }

    public static bool TryGetStringLengthFromAttribute(
        this IPropertySymbol property,
        out int maxLength
    )
    {
        maxLength = 0;

        var attribute = property
            .GetAttributes()
            .FirstOrDefault(a =>
                string.Equals(
                    a.AttributeClass?.Name,
                    Constants.StringLengthAttribute,
                    StringComparison.Ordinal
                )
            );

        if (attribute == null)
        {
            return false;
        }

        if (
            attribute.ConstructorArguments.Length > 0
            && attribute.ConstructorArguments[0].Value is int max
        )
        {
            maxLength = max;
            return true;
        }

        return false;
    }
}
