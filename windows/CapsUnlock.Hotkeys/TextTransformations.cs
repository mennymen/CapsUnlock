using System.Text;
using System.Text.RegularExpressions;

namespace CapsUnlock.Hotkeys;

internal static class TextTransformations
{
    private static readonly Regex Words = new(
        @"[\p{L}\p{N}][\p{L}\p{M}\p{N}]*(?:['’][\p{L}\p{M}\p{N}]+)*",
        RegexOptions.CultureInvariant);

    public static string Transform(string operation, string text)
    {
        return operation switch
        {
            "uppercase" => text.ToUpperInvariant(),
            "lowercase" => text.ToLowerInvariant(),
            "invert" => InvertCase(text),
            "title" => ToTitleCase(text),
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
