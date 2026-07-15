namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Story 1.1 AC2 — immutable ordering marker appended to the <c>IServiceCollection</c> by a
/// FrontComposer bootstrap entry point. Registered as a singleton via <c>TryAddEnumerable</c> so a
/// duplicate entry-point call (e.g. calling Quickstart twice) does not double-register or change the
/// observed insertion order (D33 — no mutable process-static).
/// </summary>
internal interface IFrontComposerBootstrapMarker {
    /// <summary>Gets the bootstrap stage this marker represents.</summary>
    FrontComposerBootstrapStage Stage { get; }
}
