namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Resolves the optional empty-state CTA for a projection type.
/// </summary>
public interface IEmptyStateCtaResolver {
    /// <summary>
    /// Resolves a command CTA for the projection, or <see langword="null"/> when no suitable
    /// command is registered.
    /// </summary>
    EmptyStateCta? Resolve(Type projectionType, string? explicitCommandName = null);
}

/// <summary>
/// Runtime CTA metadata rendered by <c>FcProjectionEmptyPlaceholder</c>.
/// </summary>
public sealed record EmptyStateCta(string CommandTypeName, string CommandDisplayName, string CommandRoute);
