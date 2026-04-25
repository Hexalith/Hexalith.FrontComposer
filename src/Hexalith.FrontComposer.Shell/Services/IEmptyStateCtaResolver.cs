namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Resolves the optional empty-state CTA for a projection type (Story 4-6 D4/D5).
/// </summary>
public interface IEmptyStateCtaResolver {
    /// <summary>
    /// Resolves a command CTA for the projection, or <see langword="null"/> when no suitable
    /// command is registered. The resolver internally discovers
    /// <c>[ProjectionEmptyStateCta]</c> on <paramref name="projectionType"/> per Story 4-6 D5.
    /// </summary>
    /// <param name="projectionType">The projection's CLR type.</param>
    EmptyStateCta? Resolve(Type projectionType);

    /// <summary>
    /// Resolves a CTA for the projection using an explicit command-name override (caller-supplied
    /// programmatic override; bypasses <c>[ProjectionEmptyStateCta]</c> attribute discovery but
    /// still applies the writable + reachable + bounded-context filters).
    /// </summary>
    EmptyStateCta? ResolveExplicit(Type projectionType, string commandName);
}

/// <summary>
/// Runtime CTA metadata rendered by <c>FcProjectionEmptyPlaceholder</c>.
/// </summary>
/// <param name="CommandFqn">Fully qualified command type name.</param>
/// <param name="CommandDisplayName">Humanized display label (e.g., "Create Order").</param>
/// <param name="CommandRoute">Routable URL for the command's full page.</param>
/// <param name="AuthorizationPolicy">Optional <c>[Authorize(Policy=…)]</c> name discovered on the
/// resolved command type. <see langword="null"/> when the command carries no explicit policy
/// (callers wrap with a default <c>&lt;AuthorizeView&gt;</c> for the authenticated-user check).</param>
public sealed record EmptyStateCta(
    string CommandFqn,
    string CommandDisplayName,
    string CommandRoute,
    string? AuthorizationPolicy = null);
