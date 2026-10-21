using System.Globalization;

namespace Ttw.Core.Exporting;

/// <summary>
/// Formats numbers the way the Java implementation does, so the exported files
/// stay comparable with the ones of the original tool.
/// </summary>
internal static class JavaNumber
{
    /// <summary>
    /// Formats a double like <c>Double.toString</c>: the shortest representation
    /// that reads back as the same value, always with a decimal point.
    /// </summary>
    public static string ToString(double value)
    {
        if (double.IsNaN(value))
        {
            return "NaN";
        }

        if (double.IsPositiveInfinity(value))
        {
            return "Infinity";
        }

        if (double.IsNegativeInfinity(value))
        {
            return "-Infinity";
        }

        string text = value.ToString("R", CultureInfo.InvariantCulture);
        int exponent = text.IndexOfAny(['E', 'e']);

        if (exponent >= 0)
        {
            string mantissa = text[..exponent];
            string digits = text[(exponent + 1)..];

            if (!mantissa.Contains('.'))
            {
                mantissa += ".0";
            }

            bool negative = digits.StartsWith('-');
            digits = digits.TrimStart('+', '-').TrimStart('0');

            if (digits.Length == 0)
            {
                digits = "0";
            }

            return mantissa + "E" + (negative ? "-" : string.Empty) + digits;
        }

        if (!text.Contains('.'))
        {
            text += ".0";
        }

        return text;
    }

    /// <summary>
    /// Formats a float like <c>Float.toString</c>.
    /// </summary>
    public static string ToString(float value)
    {
        if (float.IsNaN(value))
        {
            return "NaN";
        }

        if (float.IsPositiveInfinity(value))
        {
            return "Infinity";
        }

        if (float.IsNegativeInfinity(value))
        {
            return "-Infinity";
        }

        string text = value.ToString("R", CultureInfo.InvariantCulture);

        if (!text.Contains('.') && !text.Contains('E') && !text.Contains('e'))
        {
            text += ".0";
        }

        return text;
    }
}
