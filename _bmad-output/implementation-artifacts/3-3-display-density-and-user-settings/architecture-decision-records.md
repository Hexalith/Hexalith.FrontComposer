# Architecture Decision Records

## ADR-039: Four-tier density precedence resolved by a centralised pure function in `FrontComposerDensityFeature`

**Status:** Accepted (Story 3-3)

**Context:** UX spec §197-204 defines a four-tier density precedence rule: (1) user preference in LocalStorage → (2) deployment-wide default via config → (3) factory hybrid defaults (Compact for DataGrids/dev-mode, Comfortable elsewhere) → (4) per-component default (lowest priority). The choice is where to encode this rule: inline in each consuming component (every DataGrid, every form, every nav item reads density from N sources and applies its own fallback), inside the `DensityEffects` side-effect layer (resolve on every action, dispatch an `EffectiveDensityRecomputedAction`), as a public utility class that components call from their code-behind (deferred resolution), or a pure function invoked by action producers before dispatch (reducer consumes pre-resolved density).

**Decision:** A `public static class DensityPrecedence` with a single pure function `Resolve(DensityLevel? userPreference, DensityLevel? deploymentDefault, DensitySurface surface, ViewportTier tier) : DensityLevel` is the single source of truth for the four-tier rule. The function is called by ACTION PRODUCERS (effects + components dispatching state changes) BEFORE the dispatch, and the computed `EffectiveDensity` is carried in the action payload. Reducers stay pure static methods that assign `state with { EffectiveDensity = action.NewEffective }`. The resolver encodes: **(1)** tier-force override at `tier <= ViewportTier.Tablet` → `Comfortable` (ADR-040); **(2)** user preference when non-null; **(3)** deployment default when non-null; **(4)** factory hybrid default via `GetFactoryDefault(surface)` switch. Per-component tier 4 is not reachable through the resolver — custom components wanting a locked density hardcode `data-fc-density="compact"` on their root DOM element and do not consult `EffectiveDensity`.

**Rejected alternatives:**
- **Inline precedence in each consumer.** N components × N rules = O(N²) places to keep in sync. The rule IS the spec; centralising it enforces conformance.
- **Resolver inside the reducer.** Fluxor reducers are pure static — injecting `IOptions<FcShellOptions>` + `IState<FrontComposerNavigationState>` into a reducer violates the purity contract and the Fluxor static analyser would flag it.
- **Lazy selector (compute on every render via `IStateSelection` projection).** Multiplies the resolver cost by component count per render pass. Works correctly but wastes CPU; the reducer-once path amortises the compute to one call per triggering action.
- **`IOptions<DensityPrecedenceOptions>` adopter-configurable resolver.** Adopters have never asked for "a different four-tier rule" — the rule is the UX spec. Exposing it as a configuration surface inflates the public API without a use case.

**Consequences:**
- `DensityPrecedence.cs` is ~30 lines of pure C# with no Fluxor dependency — testable in isolation (`DensityPrecedenceTests.cs`, 6 `[Theory]` cases covering the precedence matrix).
- Reducers stay pure static; every reducer test constructs `state` + `action` and asserts the new state.
- Action producers (3 effect handlers + `FcSettingsDialog` radio callback + `FcLayoutBreakpointWatcher`-derived cross-feature handler) share one line: `var newEffective = DensityPrecedence.Resolve(...)`.
- Future rule refinements (e.g., "Roomy must not force-downgrade to Comfortable even at Tablet if the user has a visual-impairment flag") are a single-function change in one file, not a cross-codebase grep.
- The `DensitySurface` enum carries the surface semantic — additive by appending values, never breaking existing callers.

**Verification:** `DensityPrecedenceTests.cs` parameterised theory covers: `(null, null, Default, Desktop) → Comfortable`; `(Compact, null, Default, Desktop) → Compact`; `(null, Roomy, Default, Desktop) → Roomy`; `(Compact, Roomy, Default, Tablet) → Comfortable` (tier forces over both); `(null, null, DataGrid, Desktop) → Compact` (factory hybrid); `(null, Compact, NavigationSidebar, Desktop) → Compact` (deployment beats factory). Task 10.1.

---

## ADR-040: Viewport-driven forced-comfortable override at `ViewportTier <= Tablet` supersedes user preference

**Status:** Accepted (Story 3-3 — directly satisfies Story 3-2 Known Gap G2)

**Context:** UX spec §26-28 + §50 requires a 44 × 44 px minimum touch target at viewports < 1024 px. The density levels map to row heights roughly: Compact ≈ 32 px, Comfortable ≈ 44 px, Roomy ≈ 52 px. A user whose `UserPreference = Compact` who is handed the app on a tablet would see a 32 px DataGrid row with < 44 px inline action buttons — accessibility floor violated. The choice: accept the violation (rely on component-level responsive CSS to pad touch targets), force Comfortable regardless of preference (global override — the simplest rule), or negotiate per-component (DataGrid forces at Tablet, CommandForm does not).

**Decision:** When `ViewportTier <= ViewportTier.Tablet` (i.e., `tier is Tablet or Phone`), the density precedence resolver returns `DensityLevel.Comfortable` regardless of `userPreference` and `deploymentDefault` values. This is the **first** tier in the precedence ordering (not the last), codified as an early-return branch in `DensityPrecedence.Resolve`. The user's `UserPreference` is preserved in state — it is NOT cleared — so when the viewport expands back to Desktop, the user's choice re-applies without a re-selection action. The Settings dialog's density radio group continues to show the user's selected value (not Comfortable) at Tablet so the user isn't confused by the dialog disagreeing with their choice; the dialog body adds an inline note: "Your device size is forcing Comfortable density. Your preference will re-apply at larger screen sizes."

**Rejected alternatives:**
- **Responsive CSS per component (Compact rows grow to 44 px via `@media (max-width: 1024px)`).** UX spec §62 explicitly rules this out: "The density auto-switch at <1024px is the primary mechanism." Responsive padding works for components that adopt it; density forcing is the uniform rule. Mixing the two creates per-component inconsistency.
- **Force Comfortable only on DataGrid surfaces.** Partial fix — inline buttons inside detail views, form fields, and nav items all need touch targets too. Per-surface override multiplies the cognitive load and the test matrix.
- **Force Roomy at Tablet for maximum touch-target safety.** Overshoots — Comfortable already hits 44 px per the UX spec; Roomy would waste vertical space on tablets, reducing visible content. UX spec §193 explicitly says Roomy is "user-activated only," never a factory default.
- **Clear `UserPreference` when forcing activates.** Destructive — the user's preference is preserved across viewport changes. Clearing would require them to re-select when they next use a desktop.

**Consequences:**
- The Settings dialog's density radio group shows `UserPreference ?? EffectiveDensity` so the user sees their stored choice at Tablet, not the forced Comfortable value.
- The inline note at Tablet tier is conditional: rendered only when `EffectiveDensity != (UserPreference ?? DeploymentDefault ?? FactoryDefault)` — i.e., only when the forcing is actually active. At Desktop with Compact preference the note is absent (correct).
- `DensityReducerTests.ViewportTierChanged_AtTablet_ForcesComfortable_PreservesUserPreference` asserts both invariants (Task 10.4).
- Story 3-2 Known Gap G2 is closed by this ADR. 3-2 shipped the `ViewportTier` enum + the `FrontComposerNavigationState.CurrentViewport` observation signal; 3-3's `DensityEffects.HandleViewportTierChanged` subscribes and re-resolves.
- Accessibility contract: the framework guarantees 44 px touch targets at tablet/phone via this rule; custom components that hardcode their density (tier 5 per D1) are outside this guarantee — the custom-component accessibility contract (UX spec §154) flags this in Epic 6 reviews.

**Verification:** `DensityReducerTests.ViewportTierChanged_AtTablet_ForcesComfortable_PreservesUserPreference`, `DensityPrecedenceTests.Resolve_AtTablet_IgnoresUserPreference`, `FcSettingsDialogTests.RendersForcedDensityNoteAtTabletWhenUserPrefersCompact` (Tasks 10.1, 10.4, 10.8). Playwright resize test `SidebarResponsiveE2ETests` (inherited from Story 3-2) extended with a density transition assertion (Task 10.12).

---

## ADR-041: `<body data-fc-density>` is the default density source; local override via nested `data-fc-density` is a first-class escape hatch

**Status:** Accepted (Story 3-3) — reworded post-review (Winston 2026-04-19) from "single source of truth" to "default source + local override" to close Story 4-1 virtualization-scoped density ambiguity.

**Context:** The density rule affects spacing in generated forms, DataGrids, nav items, cards, and dev-mode overlays — i.e., it touches almost every visible component. The choice: each component subscribes to `IState<FrontComposerDensityState>` and mutates its own CSS custom properties at render time; generated Razor components read density from a context parameter; one global CSS-custom-property cascade from `<body>` that every component consumes; or a Razor `CascadingValue<DensityLevel>` from the shell. A separate sub-choice: whether local-scope overrides (a component wanting to render at a different density than the body) are first-class or forbidden.

**Decision:** `<body>` carries a `data-fc-density="compact|comfortable|roomy"` attribute maintained by the `FcDensityApplier` component (mounted inside `FrontComposerShell.Content`). **This is the DEFAULT source — NOT an exclusive single source.** `FrontComposerShell.razor.css` declares three scoped CSS rules binding `body[data-fc-density="X"]` to a `--fc-spacing-unit: Npx` CSS custom property, PLUS three mirror rules binding `[data-fc-density="X"]` (attribute-on-any-element) to the same unit values — so nested wrappers can override the cascade. **Every generated or hand-authored scoped CSS emits `padding: calc(var(--fc-spacing-unit) * N)`** for its spacing values — never a hardcoded px value. Fluent UI tokens (`spacingS`, `spacingM`, etc.) are UNCHANGED — we compose our unit on TOP of Fluent UI's 4 px grid and inject density through our own variable.

**Local override is first-class:** Any component that needs to render at a density different from the body's applied density sets `data-fc-density="X"` on a wrapper element of its choice. CSS cascade resolves `--fc-spacing-unit` to the nearest ancestor attribute. Consumers today: `FcDensityPreviewPanel` (shows preview at the radio-selected density — may differ from the forced-Comfortable body density at Tablet). Consumers anticipated: Story 4-1 `FluentDataGrid` virtualized-row container wrapping rows at `DataGrid`-factory Compact when the body is at Comfortable; Story 4-5 `DetailView` expanded-row overlay; Story 6-5 `DevModeOverlay` scope. The invariant is not "body is the only source"; the invariant is "density is expressed through the `data-fc-density` attribute via CSS cascade — not via Razor state subscriptions or inline styles."

**Rejected alternatives:**
- **Per-component Fluxor subscription.** N components × 1 subscription each = N re-render chains per density change. CSS cascade is free.
- **Razor `CascadingValue<DensityLevel>`.** Razor cascading values are component-tree specific — they don't reach the source-generated scoped CSS emitted into `.razor.css` files. A generator that emits CSS can't read a Razor cascade at render time.
- **Inline `style="--fc-density: Npx"` on `<body>`.** Fights Fluent UI's own inline styles and breaks when the adopter also sets body styles. `data-*` attributes are the standard HTML5 idiom for "configuration state" and are untouched by Fluent UI.
- **Per-component `data-density` on every scoped root (unconditional).** Would require every component to subscribe to `IState<FrontComposerDensityState>` and render the attribute — defeats the "zero per-component density logic" goal. The reworded decision draws the line differently: components that need a DIFFERENT density than the body's applied value opt into a local wrapper; components that are content with the body's value stay silent.
- **CSS `@media (min-width: Npx)` instead of `data-fc-density`.** Viewport-driven only; user preference + deployment default can't express themselves through `@media` queries.
- **"Single source of truth — body only, no overrides."** Considered at spec time; rejected post-review because (a) `FcDensityPreviewPanel` already needs the override to show the selected-radio density while body is forced to Comfortable at Tablet, and (b) Story 4-1 virtualization-scoped DataGrid density will need it next. Pretending the invariant is "body only" while the codebase already contains a legitimate override sets up readers to fight the framework.

**Consequences:**
- Every future component that emits scoped CSS (generated DataGrid renderers, FcLifecycleWrapper, FcSettingsDialog itself) uses `var(--fc-spacing-unit)` or a direct px multiplier of it. The convention is documented in `DensityPrecedence.cs` XML + the scoped-CSS style guide (`docs/style-guide.md` landing in Story 9-5 — v1.x).
- Override is cheap and documented: a component wraps its scope in `<div data-fc-density="@Density">` and the cascade resolves children to the wrapper's density, not the body's. `FcDensityPreviewPanel` is the canonical example.
- `FcDensityApplier.OnAfterRenderAsync(firstRender)` fires `setDensity(initialValue)` so the first-render paint reflects the hydrated preference — no FOUC between initial paint and density applied.
- The scoped-CSS bundle filename contract (Story 3-1 ADR-034) applies unchanged — `--fc-spacing-unit` is declared inside `FrontComposerShell.razor.css`, which ships via the existing `_content/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.styles.css` link.
- `DensityNoPerComponentLogicLintTest` remains a tripwire against rogue `var(--fc-density)` / inline-style density writes; it does NOT fail on legitimate `data-fc-density` wrapper usage inside `FcDensityPreviewPanel` + future Story 4-1/4-5/6-5 consumers. The allow-list is maintained in the test itself (see Task 10.6a). **The grep-based tripwire is a v1 placeholder** — replacement by Roslyn analyzer + PostCSS AST walker tracked in Known Gap G15 (Story 9-x).
- Custom CSS the adopter writes against Fluent UI internals does NOT participate in this system — adopters who override Fluent UI CSS are outside the zero-override contract and own their density compliance.

**Verification:** `FcDensityApplierTests.InvokesSetDensityOnInitialRenderAndOnStateChange` (bUnit + `BunitJSInterop.SetupVoid("setDensity")` expectation, plus invocation-count assertion per Murat review — Task 10.6); `FcDensityPreviewPanelTests.RendersLocalDensityAttribute` asserts the wrapper element has the correct `data-fc-density` value; `FcDensityPreviewPanelTests.CascadesSpacingUnitThroughLocalWrapper` asserts CSS `--fc-spacing-unit` resolves to the wrapper's tier at computed-style time (bUnit + `Document.QuerySelector + getComputedStyle` via JS interop). Manual verification in Task 11 — Counter.Web at Roomy shows visibly taller DataGrid rows. Playwright screenshot diff across densities is deferred to Story 10-2 (density parity specimen).

---
