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

/// <summary>Marks that the foundational <c>AddHexalithFrontComposer()</c> call ran.</summary>
internal sealed record QuickstartBootstrapMarker : IFrontComposerBootstrapMarker {
    /// <inheritdoc />
    public FrontComposerBootstrapStage Stage => FrontComposerBootstrapStage.Quickstart;
}

/// <summary>Marks that an <c>AddHexalithDomain&lt;TMarker&gt;()</c> call ran.</summary>
internal sealed record DomainBootstrapMarker : IFrontComposerBootstrapMarker {
    /// <inheritdoc />
    public FrontComposerBootstrapStage Stage => FrontComposerBootstrapStage.Domain;
}

/// <summary>Marks that an <c>AddHexalithEventStore(...)</c> call ran.</summary>
internal sealed record EventStoreBootstrapMarker : IFrontComposerBootstrapMarker {
    /// <inheritdoc />
    public FrontComposerBootstrapStage Stage => FrontComposerBootstrapStage.EventStore;
}
