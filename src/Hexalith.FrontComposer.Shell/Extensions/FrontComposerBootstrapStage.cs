namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Story 1.1 AC2 — the three FrontComposer bootstrap entry points, in their required call order.
/// Each entry point appends exactly one immutable marker to the <c>IServiceCollection</c>; the
/// <see cref="FrontComposerBootstrapValidator"/> reads the markers back (DI preserves registration
/// order) to verify presence and ordering at host start, before first render.
/// </summary>
internal enum FrontComposerBootstrapStage {
    /// <summary>
    /// The foundational <c>AddHexalithFrontComposer()</c> call (reached directly via the granular
    /// 3-call path or via <c>AddHexalithFrontComposerQuickstart()</c>). Establishes the authoritative
    /// Fluxor store, <c>IStorageService</c>, and <c>IFrontComposerRegistry</c>. Required for any
    /// valid bootstrap.
    /// </summary>
    Quickstart = 0,

    /// <summary>
    /// An <c>AddHexalithDomain&lt;TMarker&gt;()</c> call. Optional — an empty shell with no domain
    /// types registered is valid (Story 1.1 AC3).
    /// </summary>
    Domain = 1,

    /// <summary>
    /// An <c>AddHexalithEventStore(...)</c> call. Optional — must run last when present so it only
    /// swaps the stub command/query clients for the real EventStore-backed ones.
    /// </summary>
    EventStore = 2,
}
