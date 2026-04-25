using System.Globalization;

namespace Hexalith.FrontComposer.Shell.State.ETagCache;

/// <summary>
/// Story 5-2 D3 / T3 — framework-allowlisted discriminator builder for the opportunistic ETag
/// cache. The story forbids raw user-entered values, search text, free-text filters, PII,
/// hashed user input, or arbitrary serialized query payloads from the cache key. This builder
/// validates compile-time projection / query identity plus framework-generated page and count
/// lane identifiers, returning <see langword="null"/> for anything that does not pass.
/// </summary>
/// <remarks>
/// Discriminator shapes Story 5-2 caches on day one (T3 documentation):
/// <list type="bullet">
///   <item><description><c>"projection-page:{TypeFqn}:s{Skip}-t{Take}"</c> — projection snapshot / page lane.</description></item>
///   <item><description><c>"action-queue-count:{TypeFqn}"</c> — Story 3-5 ActionQueue badge count lane.</description></item>
/// </list>
/// Adopter-supplied discriminators are deliberately not supported in v1 because there is no
/// way to verify they are framework-controlled. Future stories may expose an opt-in registry
/// for additional safe discriminator shapes.
/// </remarks>
public static class ETagCacheDiscriminator {
    /// <summary>The lane prefix used for projection page / snapshot caches.</summary>
    public const string ProjectionPageLanePrefix = "projection-page";

    /// <summary>The lane prefix used for action-queue badge count caches.</summary>
    public const string ActionQueueCountLanePrefix = "action-queue-count";

    /// <summary>Maximum length of a discriminator string. Anything longer is rejected.</summary>
    public const int MaxLength = 256;

    /// <summary>
    /// Builds a discriminator for a projection page / snapshot lane. Returns
    /// <see langword="null"/> when any input fails the framework allowlist (null / blank /
    /// colon-containing / unsafe-character type FQN, or non-positive <paramref name="take"/>,
    /// or negative <paramref name="skip"/>).
    /// </summary>
    /// <param name="projectionTypeFqn">Fully-qualified projection type name (compile-time identity).</param>
    /// <param name="skip">Non-negative skip offset (framework-generated).</param>
    /// <param name="take">Positive page size (framework-generated).</param>
    /// <returns>The safe discriminator, or <see langword="null"/> when the inputs cannot be allowlisted.</returns>
    public static string? ForProjectionPage(string? projectionTypeFqn, int skip, int take) {
        if (skip < 0 || take <= 0) {
            return null;
        }

        if (!IsSafeTypeFqn(projectionTypeFqn)) {
            return null;
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{ProjectionPageLanePrefix}:{projectionTypeFqn}:s{skip}-t{take}");
    }

    /// <summary>
    /// Builds a discriminator for an action-queue count lane. Returns <see langword="null"/>
    /// when the projection type FQN fails the framework allowlist.
    /// </summary>
    /// <param name="projectionTypeFqn">Fully-qualified projection type name (compile-time identity).</param>
    /// <returns>The safe discriminator, or <see langword="null"/> when the type FQN cannot be allowlisted.</returns>
    public static string? ForActionQueueCount(string? projectionTypeFqn) {
        return IsSafeTypeFqn(projectionTypeFqn)
            ? string.Concat(ActionQueueCountLanePrefix, ":", projectionTypeFqn)
            : null;
    }

    /// <summary>
    /// Validates a discriminator returned by this builder, or supplied externally, against the
    /// framework allowlist. Returns <see langword="false"/> for null / blank / oversized
    /// strings, anything containing whitespace or control characters, anything that does not
    /// start with a known safe lane prefix, or anything containing a colon followed by an
    /// empty segment (which would let two distinct logical keys collide).
    /// </summary>
    public static bool IsAllowlisted(string? discriminator) {
        if (string.IsNullOrWhiteSpace(discriminator)) {
            return false;
        }

        if (discriminator!.Length > MaxLength) {
            return false;
        }

        if (!discriminator.StartsWith(ProjectionPageLanePrefix + ":", System.StringComparison.Ordinal)
            && !discriminator.StartsWith(ActionQueueCountLanePrefix + ":", System.StringComparison.Ordinal)) {
            return false;
        }

        for (int i = 0; i < discriminator.Length; i++) {
            char ch = discriminator[i];
            if (char.IsControl(ch) || char.IsWhiteSpace(ch)) {
                return false;
            }
        }

        return discriminator.StartsWith(ProjectionPageLanePrefix + ":", System.StringComparison.Ordinal)
            ? IsProjectionPageShape(discriminator)
            : IsActionQueueCountShape(discriminator);
    }

    private static bool IsProjectionPageShape(string discriminator) {
        string[] parts = discriminator.Split(':');
        if (parts.Length != 3
            || !string.Equals(parts[0], ProjectionPageLanePrefix, System.StringComparison.Ordinal)
            || !IsSafeTypeFqn(parts[1])) {
            return false;
        }

        string paging = parts[2];
        int dash = paging.IndexOf("-t", System.StringComparison.Ordinal);
        if (!paging.StartsWith('s')
            || dash <= 1
            || dash + 2 >= paging.Length) {
            return false;
        }

        return int.TryParse(paging[1..dash], NumberStyles.None, CultureInfo.InvariantCulture, out int skip)
            && int.TryParse(paging[(dash + 2)..], NumberStyles.None, CultureInfo.InvariantCulture, out int take)
            && skip >= 0
            && take > 0;
    }

    private static bool IsActionQueueCountShape(string discriminator) {
        string[] parts = discriminator.Split(':');
        return parts.Length == 2
            && string.Equals(parts[0], ActionQueueCountLanePrefix, System.StringComparison.Ordinal)
            && IsSafeTypeFqn(parts[1]);
    }

    private static bool IsSafeTypeFqn(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        if (value!.Length > 200) {
            return false;
        }

        for (int i = 0; i < value.Length; i++) {
            char ch = value[i];
            if (!IsTypeFqnChar(ch)) {
                return false;
            }
        }

        return true;
    }

    private static bool IsTypeFqnChar(char ch) {
        // Allow CLR-name characters, generics markers, and common namespace separators.
        // Whitespace, ':', '/', '\\', '?', '#', and other path-traversal / key-separator
        // characters are explicitly excluded.
        if (char.IsLetterOrDigit(ch)) {
            return true;
        }

        return ch is '.' or '_' or '+' or '<' or '>' or ',' or '`';
    }
}
