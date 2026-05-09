using System.Text;

namespace Hexalith.FrontComposer.Cli;

public static class OutputSanitizer
{
    public static string SanitizeMultiLine(string? value, int maxLength = 8_000)
    {
        if (string.IsNullOrEmpty(value)) {
            return string.Empty;
        }

        StringBuilder builder = new(Math.Min(value.Length, maxLength) + 32);
        int emitted = 0;
        foreach (char ch in value) {
            string replacement = ch switch {
                '\r' or '\n' or '\t' => ch.ToString(),
                < ' ' or '' => "\\u" + ((int)ch).ToString("X4", System.Globalization.CultureInfo.InvariantCulture),
                _ => ch.ToString(),
            };

            if (emitted + replacement.Length > maxLength) {
                break;
            }

            _ = builder.Append(replacement);
            emitted += replacement.Length;
        }

        if (builder.Length < value.Length) {
            _ = builder.Append(" [truncated:");
            _ = builder.Append(Math.Max(0, value.Length - emitted).ToString(System.Globalization.CultureInfo.InvariantCulture));
            _ = builder.Append(']');
        }

        return builder.ToString();
    }

    public static string Sanitize(string? value, int maxLength = 240)
    {
        if (string.IsNullOrEmpty(value)) {
            return string.Empty;
        }

        StringBuilder builder = new(Math.Min(value.Length, maxLength) + 32);
        int emitted = 0;
        foreach (char ch in value) {
            string replacement = ch switch {
                '\r' => "\\r",
                '\n' => "\\n",
                '\t' => "\\t",
                < ' ' or '\u007f' => "\\u" + ((int)ch).ToString("X4", System.Globalization.CultureInfo.InvariantCulture),
                _ => ch.ToString(),
            };

            if (emitted + replacement.Length > maxLength) {
                break;
            }

            _ = builder.Append(replacement);
            emitted += replacement.Length;
        }

        if (builder.Length < value.Length) {
            _ = builder.Append(" [truncated:");
            _ = builder.Append(Math.Max(0, value.Length - emitted).ToString(System.Globalization.CultureInfo.InvariantCulture));
            _ = builder.Append(']');
        }

        return builder.ToString();
    }
}
