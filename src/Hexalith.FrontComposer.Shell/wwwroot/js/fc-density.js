// Story 3-3 Task 4.1 (D9, ADR-041) — ES module that mutates the <body> data-fc-density attribute
// on demand. Write-only — no subscribe/unsubscribe path; all state lives in Fluxor and the DOM
// attribute is the projection. FcDensityApplier.razor calls setDensity() on every EffectiveDensity
// change and on first render.

/**
 * Write the canonical density attribute onto the document body.
 * @param {string | null | undefined} level Density level name (case-insensitive).
 */
export function setDensity(level) {
    if (!level) {
        return;
    }

    document.body.dataset.fcDensity = String(level).toLowerCase();
}
