#nullable enable

namespace Hexalith.FrontComposer.SourceTools.Transforms;

using System.Text;

/// <summary>
/// Converts CamelCase identifiers to human-readable labels.
/// </summary>
public static class CamelCaseHumanizer
{
    /// <summary>
    /// Inserts spaces before uppercase transitions in a CamelCase string.
    /// </summary>
    /// <param name="camelCase">The CamelCase input string.</param>
    /// <returns>A humanized string with spaces, or null if input is null.</returns>
    public static string? Humanize(string? camelCase)
    {
        if (camelCase is null)
        {
            return null;
        }

        if (camelCase.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder(camelCase.Length + 10);

        for (int i = 0; i < camelCase.Length; i++)
        {
            char c = camelCase[i];

            if (i == 0)
            {
                // Capitalize first character
                sb.Append(char.ToUpperInvariant(c));
                continue;
            }

            bool isUpper = c >= 'A' && c <= 'Z';
            bool prevIsUpper = camelCase[i - 1] >= 'A' && camelCase[i - 1] <= 'Z';
            bool prevIsLower = camelCase[i - 1] >= 'a' && camelCase[i - 1] <= 'z';
            bool prevIsDigit = camelCase[i - 1] >= '0' && camelCase[i - 1] <= '9';
            bool isLower = c >= 'a' && c <= 'z';

            if (isUpper && prevIsLower)
            {
                // Transition: lowercase -> uppercase ("orderDate" -> "order Date")
                sb.Append(' ');
                sb.Append(c);
            }
            else if (isUpper && prevIsUpper && i + 1 < camelCase.Length)
            {
                char next = camelCase[i + 1];
                bool nextIsLower = next >= 'a' && next <= 'z';
                if (nextIsLower)
                {
                    // End of acronym -- only split if the uppercase run is 3+ chars.
                    // "XMLParser" (X,M,L,P -> 4 uppers) -> "XML Parser"
                    // "OrderIDs" (I,D -> 2 uppers) -> "IDs" stays together
                    int upperRunLength = CountConsecutiveUppersBefore(camelCase, i) + 1;
                    if (upperRunLength >= 3)
                    {
                        sb.Append(' ');
                    }

                    sb.Append(c);
                }
                else
                {
                    sb.Append(c);
                }
            }
            else if (isUpper && prevIsDigit)
            {
                // Transition: digit -> uppercase ("Order2Name" -> "Order2 Name")
                sb.Append(' ');
                sb.Append(c);
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static int CountConsecutiveUppersBefore(string s, int index)
    {
        int count = 0;
        for (int j = index - 1; j >= 0; j--)
        {
            if (s[j] >= 'A' && s[j] <= 'Z')
            {
                count++;
            }
            else
            {
                break;
            }
        }

        return count;
    }
}
