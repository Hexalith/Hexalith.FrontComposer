namespace Hexalith.FrontComposer.Mcp;

/// <summary>
/// Validates exact canonical ULIDs at MCP identity boundaries without normalizing input.
/// </summary>
internal static class FrontComposerMcpUlid {
    /// <summary>
    /// Determines whether an identifier is an uppercase canonical encoding of a 128-bit ULID.
    /// </summary>
    /// <param name="value">The identifier to validate.</param>
    /// <returns><see langword="true"/> only for an exact canonical ULID.</returns>
    internal static bool IsCanonical(string? value) {
        if (value is null || value.Length != 26 || value[0] is < '0' or > '7') {
            return false;
        }

        if (NUlid.Ulid.TryParse(value, out NUlid.Ulid parsed)) {
            return string.Equals(value, parsed.ToString(), StringComparison.Ordinal);
        }

        // NUlid 1.7.3 models the 48-bit timestamp as DateTimeOffset and therefore cannot parse
        // otherwise valid ULIDs whose timestamp exceeds DateTimeOffset.MaxValue. Clamp only the
        // highest timestamp digit for the validation probe; the original identifier is never
        // returned normalized. Exact probe round-trip still validates every Crockford character,
        // while the 0-7 check above enforces the ULID 128-bit upper bound.
        Span<char> validationProbe = stackalloc char[26];
        value.AsSpan().CopyTo(validationProbe);
        validationProbe[0] = '0';
        return NUlid.Ulid.TryParse(validationProbe, out parsed)
            && validationProbe.SequenceEqual(parsed.ToString());
    }
}
