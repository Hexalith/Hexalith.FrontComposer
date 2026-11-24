namespace Hexalith.FrontComposer.Mcp;

/// <summary>Identifies Unicode bidirectional controls in MCP text.</summary>
internal static class McpUnicodeSafety
{
    /// <summary>Finds control characters in URI text before URI parsing can normalize them.</summary>
    /// <param name="value">The original URI text.</param>
    /// <returns>Whether the URI contains a literal or percent-encoded control.</returns>
    public static bool ContainsUnsafeUriCharacters(string value)
    {
        foreach (char character in Uri.UnescapeDataString(value))
        {
            if (char.IsControl(character) || IsBidirectionalControl(character))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns whether the character has the Unicode Bidi_Control property.</summary>
    /// <param name="value">The character to classify.</param>
    /// <returns>Whether the character controls bidirectional text ordering.</returns>
    public static bool IsBidirectionalControl(char value)
        => value is '\u061C' or '\u200E' or '\u200F'
            or (>= '\u202A' and <= '\u202E')
            or (>= '\u2066' and <= '\u2069');
}
