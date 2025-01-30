using System.Text.RegularExpressions;

namespace EntityLengths.Generator.Utils;

internal static class RegexPatterns
{
    public static readonly Regex VarCharLength = new(
        @"(?:var)?char\s*\((\d+)\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
}
