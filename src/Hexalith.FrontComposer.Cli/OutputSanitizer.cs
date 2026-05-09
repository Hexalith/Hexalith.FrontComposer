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
        int emittedChars = 0;
        int processedInputChars = 0;
        foreach (char ch in value) {
            string replacement = ch switch {
                '\r' or '\n' or '\t' => ch.ToString(),
                < ' ' or '\u007f' => "\\u" + ((int)ch).ToString("X4", System.Globalization.CultureInfo.InvariantCulture),
                _ => ch.ToString(),
            };

            if (emittedChars + replacement.Length > maxLength) {
                break;
            }

            _ = builder.Append(replacement);
            emittedChars += replacement.Length;
            processedInputChars++;
        }

        if (processedInputChars < value.Length) {
            _ = builder.Append(" [truncated:");
            _ = builder.Append((value.Length - processedInputChars).ToString(System.Globalization.CultureInfo.InvariantCulture));
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
        int emittedChars = 0;
        int processedInputChars = 0;
        foreach (char ch in value) {
            string replacement = ch switch {
                '\r' => "\\r",
                '\n' => "\\n",
                '\t' => "\\t",
                < ' ' or '\u007f' => "\\u" + ((int)ch).ToString("X4", System.Globalization.CultureInfo.InvariantCulture),
                _ => ch.ToString(),
            };

            if (emittedChars + replacement.Length > maxLength) {
                break;
            }

            _ = builder.Append(replacement);
            emittedChars += replacement.Length;
            processedInputChars++;
        }

        if (processedInputChars < value.Length) {
            _ = builder.Append(" [truncated:");
            _ = builder.Append((value.Length - processedInputChars).ToString(System.Globalization.CultureInfo.InvariantCulture));
            _ = builder.Append(']');
        }

        return builder.ToString();
    }
}
