
using System.Text;

namespace Hexalith.FrontComposer.SourceTools.Transforms;
/// <summary>
/// Converts CamelCase identifiers to human-readable labels.
/// </summary>
public static class CamelCaseHumanizer {
    /// <summary>
    /// Inserts spaces before uppercase transitions in a CamelCase string.
    /// </summary>
    /// <param name="camelCase">The CamelCase input string.</param>
    /// <returns>A humanized string with spaces, or null if input is null.</returns>
    public static string? Humanize(string? camelCase) {
        if (camelCase is null) {
            return null;
        }

        if (camelCase.Length == 0) {
            return string.Empty;
        }

        var sb = new StringBuilder(camelCase.Length + 10);

        for (int i = 0; i < camelCase.Length; i++) {
            char c = camelCase[i];

            if (i == 0) {
                // Capitalize first character
                _ = sb.Append(char.ToUpperInvariant(c));
                continue;
            }

            bool isUpper = c is >= 'A' and <= 'Z';
            bool prevIsUpper = camelCase[i - 1] is >= 'A' and <= 'Z';
            bool prevIsLower = camelCase[i - 1] is >= 'a' and <= 'z';
            bool prevIsDigit = camelCase[i - 1] is >= '0' and <= '9';
            _ = c is >= 'a' and <= 'z';

            if (isUpper && prevIsLower) {
                // Transition: lowercase -> uppercase ("orderDate" -> "order Date")
                _ = sb.Append(' ');
                _ = sb.Append(c);
            }
            else if (isUpper && prevIsUpper && i + 1 < camelCase.Length) {
                char next = camelCase[i + 1];
                bool nextIsLower = next is >= 'a' and <= 'z';
                if (nextIsLower) {
                    // End of acronym -- only split if the uppercase run is 3+ chars.
                    // "XMLParser" (X,M,L,P -> 4 uppers) -> "XML Parser"
                    // "OrderIDs" (I,D -> 2 uppers) -> "IDs" stays together
                    int upperRunLength = CountConsecutiveUppersBefore(camelCase, i) + 1;
                    if (upperRunLength >= 3) {
                        _ = sb.Append(' ');
                    }

                    _ = sb.Append(c);
                }
                else {
                    _ = sb.Append(c);
                }
            }
            else if (isUpper && prevIsDigit) {
                // Transition: digit -> uppercase ("Order2Name" -> "Order2 Name")
                _ = sb.Append(' ');
                _ = sb.Append(c);
            }
            else {
                _ = sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static int CountConsecutiveUppersBefore(string s, int index) {
        int count = 0;
        for (int j = index - 1; j >= 0; j--) {
            if (s[j] is >= 'A' and <= 'Z') {
                count++;
            }
            else {
                break;
            }
        }

        return count;
    }
}
