namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Scoped context consumed by generated FullPage command renderers — carries the command's
/// display identity plus an optional return path for post-submit navigation (Story 2-2 Decision D15).
/// Shell integration (actual <c>FluentBreadcrumb</c> rendering in <c>FcHeader</c>) is Story 3.1.
/// </summary>
public interface ICommandPageContext {
    /// <summary>Gets the command's TypeName (e.g. <c>ConfigureCounterCommand</c>).</summary>
    string CommandName { get; }

    /// <summary>Gets the bounded context the command belongs to (e.g. <c>Counter</c>).</summary>
    string BoundedContext { get; }

    /// <summary>
    /// Gets the relative URL the renderer should navigate to on successful <c>Confirmed</c>.
    /// Validated via <c>Uri.IsWellFormedUriString(UriKind.Relative)</c> before navigation
    /// (Story 2-2 Decision D32 — open-redirect defense).
    /// </summary>
    string? ReturnPath { get; }
}
