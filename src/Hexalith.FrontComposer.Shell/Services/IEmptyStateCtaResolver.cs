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
public sealed record EmptyStateCta {
    public EmptyStateCta(
        string CommandFqn,
        string CommandDisplayName,
        string CommandRoute,
        string? AuthorizationPolicy = null,
        string? BoundedContext = null) {
        // P-18: reject whitespace-only AuthorizationPolicy. The component branches on
        // `policy is { Length: > 0 }` and would otherwise pass "   " into <AuthorizeView Policy=>
        // which throws InvalidOperationException at runtime.
        if (AuthorizationPolicy is not null && string.IsNullOrWhiteSpace(AuthorizationPolicy)) {
            throw new ArgumentException(
                "AuthorizationPolicy must be either null or a non-whitespace policy name.",
                nameof(AuthorizationPolicy));
        }

        this.CommandFqn = CommandFqn;
        this.CommandDisplayName = CommandDisplayName;
        this.CommandRoute = CommandRoute;
        this.AuthorizationPolicy = AuthorizationPolicy;
        this.BoundedContext = BoundedContext;
    }

    public string CommandFqn { get; init; }
    public string CommandDisplayName { get; init; }
    public string CommandRoute { get; init; }
    public string? AuthorizationPolicy { get; init; }

    /// <summary>
    /// Bounded context of the manifest that owns the resolved command. Threaded through to
    /// <c>FcAuthorizedCommandRegion</c> so the evaluator's <c>CommandAuthorizationResource</c>
    /// shape stays symmetric across CTA, palette, dispatch, and form surfaces (Story 7-3 Pass 4
    /// resolves AA-08: DN5 was supposed to eliminate resource-shape divergence but had dropped
    /// this field).
    /// </summary>
    public string? BoundedContext { get; init; }
}
