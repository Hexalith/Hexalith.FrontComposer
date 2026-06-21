namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Domain-agnostic surface state for an aggregate detail page (FC-DTL contract, cc-2026-06-21).
/// A consuming domain maps its own detail snapshot kind onto these states so the shared
/// <c>FcAggregateDetailPage&lt;TItem&gt;</c> wrapper keeps the states distinct (non-collapsing) without
/// owning domain semantics, copy, or query behaviour.
/// </summary>
/// <remarks>
/// The wrapper renders the ready body only for <see cref="Ready"/>, <see cref="Stale"/>, and
/// <see cref="Degraded"/> (the projection-proven states); <see cref="Stale"/> and <see cref="Degraded"/>
/// additionally surface a caller-supplied banner above the ready body. Every other value renders the
/// caller-supplied content for that single state and never the ready body, so a non-ready surface can
/// never be dressed as success.
/// </remarks>
public enum FcAggregateDetailState {
    /// <summary>The detail projection is still loading. The wrapper renders the loading content only.</summary>
    Loading,

    /// <summary>The projection is current and authoritative. The wrapper renders the ready body.</summary>
    Ready,

    /// <summary>The projection is known-stale. The wrapper renders the stale banner above the ready body.</summary>
    Stale,

    /// <summary>The projection is degraded. The wrapper renders the degraded banner above the ready body.</summary>
    Degraded,

    /// <summary>The operator is not authorized. The wrapper renders the unauthorized content only.</summary>
    Unauthorized,

    /// <summary>The aggregate was not found. The wrapper renders the not-found content only.</summary>
    NotFound,

    /// <summary>The read path is unavailable or returned no payload. The wrapper renders the unavailable content only (the fail-closed default).</summary>
    Unavailable,
}
