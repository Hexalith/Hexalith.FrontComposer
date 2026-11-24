using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Builds canonicalized <c>IStorageService</c> keys for Story 2-2's <c>LastUsedValueProvider</c> and
/// (forward) Story 4.3's <c>DataGridNavigation</c> effects. Decision D39 + fail-closed D31:
/// <list type="bullet">
///   <item><c>tenantId</c> — trim → NFC-normalize → URL-encode.</item>
///   <item><c>userId</c> — trim → NFC-normalize → lowercase when email-shaped → URL-encode.</item>
///   <item><c>commandTypeFqn</c> + <c>propertyName</c> — verbatim (already C#-legal identifiers).</item>
/// </list>
/// Throws <see cref="InvalidOperationException"/> when <c>tenantId</c> or <c>userId</c> is null or
/// whitespace — NEVER substitutes <c>"anonymous"</c> or empty segments (prevents cross-tenant PII leak).
/// <para>
/// Keys never exceed <see cref="MaxKeyLength"/> characters. A key that fits keeps the original
/// <c>frontcomposer:lastused:{tenant}:{user}:{command}:{property}</c> format. A longer key uses the
/// <see cref="BoundedFormatVersion"/> format, <c>frontcomposer:lastused:v2:{tenant}:{user}:{command}:{property}</c>,
/// where each segment that is longer than 96 characters or starts with <c>!</c> is replaced by <c>!</c>
/// followed by the lowercase hexadecimal SHA-256 digest of its UTF-8 bytes. The 512-character maximum,
/// the 96-character segment limit, the <c>!</c> marker and the <c>v2</c> segment are part of the stored
/// format: changing any of them orphans values already stored under bounded keys.
/// </para>
/// </summary>
public static class FrontComposerStorageKey {
    /// <summary>Key-segment separator. Inside-segment <c>:</c> characters are URL-encoded to <c>%3A</c>.</summary>
    public const string Separator = ":";

    /// <summary>Single-character form of <see cref="Separator"/> used for allocation-free appends.</summary>
    private const char SeparatorChar = ':';

    /// <summary>Feature prefix for LastUsed storage keys.</summary>
    public const string LastUsedPrefix = "frontcomposer:lastused";

    /// <summary>
    /// Maximum length, in UTF-16 characters, of a key returned by <see cref="Build"/>. Part of the stored
    /// key format: keys longer than this are stored under the bounded format.
    /// </summary>
    public const int MaxKeyLength = 512;

    /// <summary>Version segment that follows <see cref="LastUsedPrefix"/> in keys longer than the legacy format allows.</summary>
    public const string BoundedFormatVersion = "v2";

    /// <summary>Longest segment the bounded format keeps verbatim.</summary>
    private const int BoundedSegmentMaxLength = 96;

    /// <summary>Marks a bounded-format segment as a SHA-256 digest of the original segment.</summary>
    private const char HashedSegmentMarker = '!';

    /// <summary>
    /// Builds a canonicalized LastUsed key.
    /// </summary>
    /// <param name="tenantId">Tenant identifier. Must be non-null, non-whitespace.</param>
    /// <param name="userId">User identifier (typically email). Must be non-null, non-whitespace.</param>
    /// <param name="commandTypeFqn">Fully-qualified command type name (verbatim, C#-legal).</param>
    /// <param name="propertyName">Property name (verbatim, C#-legal).</param>
    /// <returns>A colon-separated canonical key of at most <see cref="MaxKeyLength"/> characters.</returns>
    /// <exception cref="InvalidOperationException">When tenant or user is null/whitespace (D31).</exception>
    /// <exception cref="ArgumentException">When <paramref name="commandTypeFqn"/> or <paramref name="propertyName"/> is null/whitespace or contains <see cref="Separator"/>.</exception>
    public static string Build(string? tenantId, string? userId, string commandTypeFqn, string propertyName)
        => BuildWithLegacyFallback(tenantId, userId, commandTypeFqn, propertyName).Key;

    /// <summary>
    /// Builds a canonicalized LastUsed key and, when that key uses the bounded format, the legacy
    /// unbounded key that earlier versions wrote for the same inputs.
    /// </summary>
    /// <param name="tenantId">Tenant identifier. Must be non-null, non-whitespace.</param>
    /// <param name="userId">User identifier (typically email). Must be non-null, non-whitespace.</param>
    /// <param name="commandTypeFqn">Fully-qualified command type name (verbatim, C#-legal).</param>
    /// <param name="propertyName">Property name (verbatim, C#-legal).</param>
    /// <returns>
    /// The key <see cref="Build"/> returns, and the legacy key to read as a fallback. The legacy key is
    /// <see langword="null"/> when the legacy key fits within <see cref="MaxKeyLength"/>, because the
    /// returned key is then the legacy key itself.
    /// </returns>
    /// <exception cref="InvalidOperationException">When tenant or user is null/whitespace (D31).</exception>
    /// <exception cref="ArgumentException">When <paramref name="commandTypeFqn"/> or <paramref name="propertyName"/> is null/whitespace or contains <see cref="Separator"/>.</exception>
    internal static (string Key, string? LegacyKey) BuildWithLegacyFallback(string? tenantId, string? userId, string commandTypeFqn, string propertyName) {
        if (string.IsNullOrWhiteSpace(commandTypeFqn)) {
            throw new ArgumentException("Command type FQN must be non-empty.", nameof(commandTypeFqn));
        }

        if (string.IsNullOrWhiteSpace(propertyName)) {
            throw new ArgumentException("Property name must be non-empty.", nameof(propertyName));
        }

        // Verbatim segments must not carry the separator, or two inputs could share one key.
        if (commandTypeFqn.Contains(SeparatorChar, StringComparison.Ordinal)) {
            throw new ArgumentException("Command type FQN must not contain the key separator.", nameof(commandTypeFqn));
        }

        if (propertyName.Contains(SeparatorChar, StringComparison.Ordinal)) {
            throw new ArgumentException("Property name must not contain the key separator.", nameof(propertyName));
        }

        if (string.IsNullOrWhiteSpace(tenantId)) {
            throw new InvalidOperationException("TenantId is required — cannot build a LastUsed key without it (Decision D31).");
        }

        if (string.IsNullOrWhiteSpace(userId)) {
            throw new InvalidOperationException("UserId is required — cannot build a LastUsed key without it (Decision D31).");
        }

        string tenantCanon = CanonicalizeTenant(tenantId!);
        string userCanon = CanonicalizeUser(userId!);

        StringBuilder sb = new();
        _ = sb.Append(LastUsedPrefix);
        _ = sb.Append(SeparatorChar).Append(tenantCanon);
        _ = sb.Append(SeparatorChar).Append(userCanon);
        _ = sb.Append(SeparatorChar).Append(commandTypeFqn);
        _ = sb.Append(SeparatorChar).Append(propertyName);
        string legacyKey = sb.ToString();
        if (legacyKey.Length <= MaxKeyLength) {
            return (legacyKey, null);
        }

        _ = sb.Clear();
        _ = sb.Append(LastUsedPrefix);
        _ = sb.Append(SeparatorChar).Append(BoundedFormatVersion);
        _ = sb.Append(SeparatorChar).Append(BoundSegment(tenantCanon));
        _ = sb.Append(SeparatorChar).Append(BoundSegment(userCanon));
        _ = sb.Append(SeparatorChar).Append(BoundSegment(commandTypeFqn));
        _ = sb.Append(SeparatorChar).Append(BoundSegment(propertyName));
        return (sb.ToString(), legacyKey);
    }

    /// <summary>
    /// Canonicalizes a tenant segment: trim → NFC → URL-encode.
    /// </summary>
    public static string CanonicalizeTenant(string raw) {
        ArgumentNullException.ThrowIfNull(raw);
        string trimmed = raw.Trim();
        string normalized = trimmed.Normalize(NormalizationForm.FormC);
        return Uri.EscapeDataString(normalized);
    }

    /// <summary>
    /// Canonicalizes a user segment: trim → NFC → lowercase only when email-shaped → URL-encode.
    /// </summary>
    public static string CanonicalizeUser(string raw) {
        ArgumentNullException.ThrowIfNull(raw);
        string trimmed = raw.Trim();
        string normalized = trimmed.Normalize(NormalizationForm.FormC);
        string canonical = normalized.Contains('@')
            ? normalized.ToLower(CultureInfo.InvariantCulture)
            : normalized;
        return Uri.EscapeDataString(canonical);
    }

    /// <summary>
    /// Parses a canonical LastUsed key back into its segments. Round-trip property for keys that keep
    /// the legacy format: <c>Parse(Build(t, u, c, p)) == (Canon(t), Canon(u), c, p)</c>. For a
    /// bounded-format key, each hashed segment is returned as its <c>!</c>-prefixed digest.
    /// </summary>
    /// <param name="key">The key to parse.</param>
    /// <returns>A tuple of the canonicalized parts, or <see langword="null"/> on shape mismatch.</returns>
    public static (string TenantCanon, string UserCanon, string CommandTypeFqn, string PropertyName)? TryParse(string key) {
        if (string.IsNullOrEmpty(key) || !key.StartsWith(LastUsedPrefix + Separator, StringComparison.Ordinal)) {
            return null;
        }

        // Skip the prefix and split the remainder. Build rejects ':' in the verbatim command and property
        // segments and URL-encodes it in tenant/user, so a legacy key has exactly 4 segments and a bounded
        // key has 5, the first being the version.
        string remainder = key[(LastUsedPrefix.Length + Separator.Length)..];
        string[] parts = remainder.Split(':');
        if (parts.Length == 4) {
            return (parts[0], parts[1], parts[2], parts[3]);
        }

        if (parts.Length == 5 && string.Equals(parts[0], BoundedFormatVersion, StringComparison.Ordinal)) {
            return (parts[1], parts[2], parts[3], parts[4]);
        }

        return null;
    }

    private static string BoundSegment(string segment) {
        // A verbatim segment that starts with the marker is hashed too, so it cannot pose as a digest.
        if (segment.Length <= BoundedSegmentMaxLength && segment[0] != HashedSegmentMarker) {
            return segment;
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(segment));
        return HashedSegmentMarker + Convert.ToHexStringLower(digest);
    }
}
