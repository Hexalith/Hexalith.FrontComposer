using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Story 4-4 T2.1 / D20 — density-to-row-height mapping consumed by the generator-emitted
/// <c>FluentDataGrid.ItemSize</c> binding. Resolution is runtime (not emit-time) because
/// <see cref="DensityLevel"/> is user-scoped and can toggle via <c>FcSettingsDialog</c>;
/// paired with <c>@key="@RenderContext.DensityLevel"</c> on the grid so a density change
/// forces remount (Fluent v5's <c>Virtualize</c> reads <c>ItemSize</c> at initialisation).
/// </summary>
/// <remarks>
/// Pixel values (32 / 44 / 56) track Story 3-3's Compact / Comfortable / Roomy density
/// tokens and preserve the Fluent-recommended row heights for Virtualize correctness.
/// </remarks>
public static class DataGridDensityMetrics {
    /// <summary>
    /// Resolves the row height (in CSS pixels) for the current density level. Used by the
    /// generator-emitted grid to bind <c>FluentDataGrid&lt;T&gt;.ItemSize</c>.
    /// </summary>
    /// <param name="density">Resolved <see cref="DensityLevel"/> from the cascading
    /// <c>RenderContext</c>.</param>
    /// <returns>Row height in CSS pixels.</returns>
    public static float ResolveRowHeightPx(DensityLevel density) => density switch {
        DensityLevel.Compact => 32f,
        DensityLevel.Comfortable => 44f,
        DensityLevel.Roomy => 56f,
        _ => 44f,
    };
}
