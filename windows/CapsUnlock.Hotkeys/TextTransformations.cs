using System.Text;
using System.Text.RegularExpressions;

namespace CapsUnlock.Hotkeys;

internal static class TextTransformations
{
    private static readonly Regex Words = new(
        @"[\p{L}\p{N}][\p{L}\p{M}\p{N}]*(?:['’][\p{L}\p{M}\p{N}]+)*",
        RegexOptions.CultureInvariant);
    private static readonly Regex NamingWords = new(
        @"[\p{L}\p{N}][\p{L}\p{M}\p{N}]*",
        RegexOptions.CultureInvariant);
    private static readonly Regex LowerToUpperBoundary = new(
        @"([\p{Ll}\p{N}])(\p{Lu})",
        RegexOptions.CultureInvariant);
    private static readonly Regex AcronymBoundary = new(
        @"(\p{Lu})(\p{Lu}\p{Ll})",
        RegexOptions.CultureInvariant);
    private static readonly Regex SentenceBoundary = new(
        @"(^|[.!?]\s+|[\r\n\u2028\u2029]+)(\p{L})",
        RegexOptions.CultureInvariant);
    private static readonly Regex BlankLine = new(
        @"(?<=\n|^)[^\S\r\n]*\r?\n",
        RegexOptions.CultureInvariant);

    public static string Transform(string operation, string text)
    {
        return operation switch
        {
            "uppercase" => text.ToUpperInvariant(),
            "lowercase" => text.ToLowerInvariant(),
            "invert" => InvertCase(text),
            "title" => ToTitleCase(text),
            "sentence" => ToSentenceCase(text),
            "camel" => ToCamelCase(text),
            "pascal" => ToPascalCase(text),
            "snake" => ToSeparatedCase(text, "_"),
            "kebab" => ToSeparatedCase(text, "-"),
            "trim" => text.Trim(),
            "collapse-whitespace" => Regex.Replace(text, @"\s+", " ", RegexOptions.CultureInvariant),
            "remove-blank-lines" => BlankLine.Replace(text, ""),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown transformation."),
        };
    }

    private static string InvertCase(string text)
    {
        var result = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            var inverted = Rune.ToLowerInvariant(rune) == rune
                ? Rune.ToUpperInvariant(rune)
                : Rune.ToLowerInvariant(rune);
            result.Append(inverted.ToString());
        }

        return result.ToString();
    }

    private static string ToTitleCase(string text)
    {
        return Words.Replace(text, match => CapitalizeFirstLetter(match.Value));
    }

    private static string ToSentenceCase(string text)
    {
        return SentenceBoundary.Replace(text.ToLowerInvariant(), match =>
            match.Groups[1].Value + match.Groups[2].Value.ToUpperInvariant());
    }

    private static string ToCamelCase(string text)
    {
        var words = GetNamingWords(text);
        if (words.Count == 0)
        {
            return "";
        }

        return words[0].ToLowerInvariant()
            + string.Concat(words.Skip(1).Select(CapitalizeFirstLetter));
    }

    private static string ToPascalCase(string text)
    {
        return string.Concat(GetNamingWords(text).Select(CapitalizeFirstLetter));
    }

    private static string ToSeparatedCase(string text, string separator)
    {
        return string.Join(separator, GetNamingWords(text).Select(word => word.ToLowerInvariant()));
    }

    private static List<string> GetNamingWords(string text)
    {
        var separated = LowerToUpperBoundary.Replace(text, "$1 $2");
        separated = AcronymBoundary.Replace(separated, "$1 $2");
        return NamingWords.Matches(separated)
            .Select(match => match.Value)
            .ToList();
    }

    private static string CapitalizeFirstLetter(string word)
    {
        var lowered = word.ToLowerInvariant();
        var result = new StringBuilder(lowered.Length);
        var firstCasedRuneFound = false;

        foreach (var rune in lowered.EnumerateRunes())
        {
            if (!firstCasedRuneFound && Rune.ToLowerInvariant(rune) != Rune.ToUpperInvariant(rune))
            {
                result.Append(Rune.ToUpperInvariant(rune).ToString());
                firstCasedRuneFound = true;
            }
            else
            {
                result.Append(rune.ToString());
            }
        }

        return result.ToString();
    }
}
