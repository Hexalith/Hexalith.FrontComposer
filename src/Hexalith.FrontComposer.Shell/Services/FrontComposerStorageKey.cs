using System.Globalization;
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
/// </summary>
public static class FrontComposerStorageKey {
    /// <summary>Key-segment separator. Inside-segment <c>:</c> characters are URL-encoded to <c>%3A</c>.</summary>
    public const string Separator = ":";

    /// <summary>Feature prefix for LastUsed storage keys.</summary>
    public const string LastUsedPrefix = "frontcomposer:lastused";

    /// <summary>
    /// Builds a canonicalized LastUsed key.
    /// </summary>
    /// <param name="tenantId">Tenant identifier. Must be non-null, non-whitespace.</param>
    /// <param name="userId">User identifier (typically email). Must be non-null, non-whitespace.</param>
    /// <param name="commandTypeFqn">Fully-qualified command type name (verbatim, C#-legal).</param>
    /// <param name="propertyName">Property name (verbatim, C#-legal).</param>
    /// <returns>A colon-separated canonical key.</returns>
    /// <exception cref="InvalidOperationException">When tenant or user is null/whitespace (D31).</exception>
    /// <exception cref="ArgumentException">When <paramref name="commandTypeFqn"/> or <paramref name="propertyName"/> is null/whitespace.</exception>
    public static string Build(string? tenantId, string? userId, string commandTypeFqn, string propertyName) {
        if (string.IsNullOrWhiteSpace(commandTypeFqn)) {
            throw new ArgumentException("Command type FQN must be non-empty.", nameof(commandTypeFqn));
        }

        if (string.IsNullOrWhiteSpace(propertyName)) {
            throw new ArgumentException("Property name must be non-empty.", nameof(propertyName));
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
        _ = sb.Append(Separator).Append(tenantCanon);
        _ = sb.Append(Separator).Append(userCanon);
        _ = sb.Append(Separator).Append(commandTypeFqn);
        _ = sb.Append(Separator).Append(propertyName);
        return sb.ToString();
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
    /// Parses a canonical LastUsed key back into its segments. Round-trip property:
    /// <c>Parse(Build(t, u, c, p)) == (Canon(t), Canon(u), c, p)</c>.
    /// </summary>
    /// <returns>A tuple of the canonicalized parts, or <see langword="null"/> on shape mismatch.</returns>
    public static (string TenantCanon, string UserCanon, string CommandTypeFqn, string PropertyName)? TryParse(string key) {
        if (string.IsNullOrEmpty(key) || !key.StartsWith(LastUsedPrefix + Separator, StringComparison.Ordinal)) {
            return null;
        }

        // Skip prefix; split the remainder into 4 segments. commandTypeFqn / propertyName are verbatim so they
        // cannot contain ':' — but tenant/user can (URL-encoded). The shape is deterministic: 4 trailing ':'-separated segments.
        string remainder = key.Substring(LastUsedPrefix.Length + Separator.Length);
        string[] parts = remainder.Split(':');
        if (parts.Length != 4) {
            return null;
        }

        return (parts[0], parts[1], parts[2], parts[3]);
    }
}
