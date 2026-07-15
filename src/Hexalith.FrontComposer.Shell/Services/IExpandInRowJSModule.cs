using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Scoped service caching the <c>fc-expandinrow.js</c> module reference (Story 2-2 Decision D25).
/// Prevents per-component module re-import and guards against prerender-time JSRuntime unavailability.
/// </summary>
public interface IExpandInRowJSModule {
    /// <summary>
    /// Initializes the expand-in-row scroll stabilization for <paramref name="element"/>.
    /// Safe to call during prerender — the module import is skipped when JSInterop is unavailable.
    /// </summary>
    Task InitializeAsync(ElementReference element);
}
