namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Viewport size tier derived at runtime by <c>FcLayoutBreakpointWatcher</c> (Story 3-2 D4 / ADR-036).
/// Integer values are pinned so the <c>fc-layout-breakpoints.js</c> module can send the tier as an
/// <see cref="int"/> verbatim and C# casts with <c>(ViewportTier)int</c>.
/// </summary>
/// <remarks>
/// Boundaries per UX spec §22-37 (inclusive): Desktop ≥ 1366, CompactDesktop 1024–1365, Tablet 768–1023,
/// Phone &lt; 768. The ordering lets downstream consumers (Story 3-3 density override) express
/// tier predicates as <c>tier &gt;= ViewportTier.Tablet</c>. Tier is NEVER persisted (ADR-037).
/// </remarks>
public enum ViewportTier : byte
{
    /// <summary>Phone viewport: &lt; 768 px.</summary>
    Phone = 0,

    /// <summary>Tablet viewport: 768–1023 px.</summary>
    Tablet = 1,

    /// <summary>Compact desktop viewport: 1024–1365 px.</summary>
    CompactDesktop = 2,

    /// <summary>Full desktop viewport: ≥ 1366 px (feature default).</summary>
    Desktop = 3,
}
