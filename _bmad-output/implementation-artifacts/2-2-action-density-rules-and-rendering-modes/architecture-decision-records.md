# Architecture Decision Records

### ADR-013: Density Computed at Generation Time

- **Status:** Accepted
- **Context:** Action density (inline/compact/full-page) can be decided at runtime (single renderer branches on field count) or at generation time (three specialized emitted components). Runtime costs a branch + dead render trees but allows dynamic override. Generation time costs compile surface but enables snapshots and MCP introspection.
- **Decision:** Density is computed on `CommandModel` during parse (`NonDerivableProperties.Length`), stored as `CommandDensity` enum on the IR, and drives emitter selection. The emitter emits a single `{CommandName}CommandRenderer.g.razor.cs` with a `[Parameter] public CommandRenderMode? RenderMode` that defaults to the density-derived mode but can be overridden.
- **Consequences:** (+) Snapshots prove density classification. (+) MCP (Epic 8) can expose density in command manifests. (+) Adopters can override render mode per-instance. (-) Shallow branching still exists in renderer but is small and testable.
- **Rejected alternatives:** Pure runtime branching (no snapshot proof, harder to review); three separate component types per command (explosion in hint names, breaks single-command-single-renderer mental model).

### ADR-014: DerivedValueProvider Chain-of-Responsibility

- **Status:** Accepted
- **Context:** Derivable field pre-fill has three sources per UX spec: projection context, last-used value, command default. A single provider would hard-code all three; chained providers let adopters inject custom providers (e.g., URL-param-derived) between them.
- **Decision:** `IDerivedValueProvider` is registered as `IEnumerable<IDerivedValueProvider>` in DI, resolved in order. Each returns `Task<DerivedValueResult>` with `HasValue` + `Value`. First `HasValue=true` wins. Built-in providers register in this order at `AddHexalithFrontComposer()`: `ProjectionContextProvider`, `LastUsedValueProvider`, `DefaultValueProvider`. Adopters use `services.AddDerivedValueProvider<T>()` which prepends (custom providers win over built-ins).
- **Consequences:** (+) Extensible without touching framework code. (+) Clear precedence. (-) Adopter must understand the order.
- **Rejected alternatives:** Single provider with visitor pattern (harder to extend); reflection-based attribute-driven resolution (AOT-hostile).

### ADR-015: DataGridNavigationState as Separate Fluxor Feature

- **Status:** Accepted
- **Context:** DataGrid scroll/filter/sort state preservation across navigation is needed by Story 2-2 (full-page form restore) and Story 4.x (DataGrid itself). It is per-view (bounded-context + projection type).
- **Decision:** New Fluxor feature `DataGridNavigationState` in `Shell/State/DataGridNavigation/`, keyed by `"{boundedContext}:{projectionTypeFqn}"` with a `Dictionary<string, GridViewSnapshot>` payload. Actions: `CaptureGridStateAction` (Epic 4 dispatches), `RestoreGridStateAction` (renderer dispatches on nav-back). Persisted via `IStorageService` with a 24-hour TTL per snapshot.
- **Consequences:** (+) Aligns with architecture D7 per-concern Fluxor pattern. (+) Forward-compatible with Epic 4. (-) Small empty-state branch in Story 2-2.
- **Rejected alternatives:** In-memory service outside Fluxor (breaks state dispatch audit trail); reuse `NavigationState` (conflates shell nav with grid state).

### ADR-016: Renderer/Form Contract — Chrome vs Core

> **TL;DR (for new adopters):** Form = engine. Renderer = shape. One form, three possible shapes.

- **Status:** Accepted
- **Context:** Story 2-1 emits `{CommandTypeName}Form.g.razor.cs` that owns `<EditForm>`, `EditContext`, `OnValidSubmitAsync`, and all lifecycle dispatch. Story 2-2 introduces three rendering modes that must REUSE this form without duplicating submit orchestration and without creating nested `<EditForm>` elements.
- **Decision:** The renderer emitted by Story 2-2 is **CHROME ONLY**. Rules:
  1. The renderer NEVER emits `<EditForm>`.
  2. The renderer ALWAYS wraps `<{CommandTypeName}Form ... />` as the single inner component that owns validation and submit.
  3. Story 2-1's `CommandFormEmitter` is extended (backward-compatible) with two parameters: `DerivableFieldsHidden` (bool, default false) and `ShowFieldsOnly` (string[]?, default null).
  4. The three modes differ ONLY in wrapping chrome:
     - **Inline (0 fields):** renderer emits a single `FluentButton` whose `OnClick` dispatches a synthetic form submit via `EventCallback` exposed by the Form (`[Parameter] public EventCallback OnExternalSubmitRequested { get; set; }`). Form handles the rest.
     - **Inline (1 field):** renderer emits the button + a `FluentPopover` containing `<{CommandTypeName}Form ShowFieldsOnly="new[]{\"{PropName}\"}" />`. Form retains its `<EditForm>` — this is acceptable because there is exactly one form per page.
     - **CompactInline:** renderer emits `<FluentCard class="fc-expand-in-row">` wrapping `<{CommandTypeName}Form DerivableFieldsHidden="true" />`.
     - **FullPage:** renderer emits breadcrumb + max-width container wrapping `<{CommandTypeName}Form />`.
  5. Story 2-1's `OnValidSubmitAsync` is the SOLE submit path. The renderer MUST NOT dispatch `{CommandName}Actions.SubmittedAction` itself.
  6. The 0-field inline button path requires ONE new capability in Story 2-1's Form: trigger `OnValidSubmitAsync` externally. Added parameter: `public ElementReference? ExternalTriggerAnchor { get; set; }` + exposed method `InvokeAsync(Func<Task>)` wrapper. Simpler: Form exposes `[Parameter] public Action? RegisterExternalSubmit { get; set; }` which the renderer stores and invokes.
- **Consequences:** (+) Single validation path; (+) lifecycle dispatch stays in Form; (+) Popover with inner `<EditForm>` is standard Blazor. (-) Requires 3 new Form parameters (still backward-compatible — all default to pre-2-2 behavior).
- **Rejected alternatives:**
  - **Renderer owns submit and Form becomes field-render helper** — duplicates lifecycle dispatch from 2-1 (ADR-010 violation).
  - **Emit three separate Form variants** — combinatorial emitter explosion.
  - **Use `CascadingValue` to pass submit handler** — overkill for 1 hop.
  - **Fold all three rendering modes into `CommandFormEmitter`** (first-principles alternative, evaluated during advanced elicitation): add `RenderMode?` parameter to `{CommandName}Form` directly; emit all three mode branches inside the form body. **Rejected because:** (a) bloats form emission to ~600 lines with three mode branches + popover + card + breadcrumb, harming readability and snapshot diff signal; (b) mixes validation concern (form's core) with layout concern (renderer's core); (c) breaks MCP introspection symmetry with Epic 8 — separate `{CommandTypeName}Renderer` artifact gives the agent registry a cleaner surface per density mode; (d) makes adopter overrides awkward (per-instance `RenderMode` override on a form tied to validation lifetime). **Trade-off accepted:** the split creates two emitted files per command (Form + Renderer) which future devs must understand together. Documented via this ADR so the question doesn't need re-litigation.

```
# INLINE MODE (0-1 non-derivable fields) ---------------------------
USER          FcCommandRenderer(Inline)   DerivedValueProvider   Story 2-1 CommandFormEmitter
 |                   |                           |                           |
 |-- click button -->|                           |                           |
 |                   |-- ResolveAsync(all) ----> |                           |
 |                   |<-- values --------------- |                           |
 |                   |-- [1 field]: show FluentPopover w/ single field   ->  |
 |                   |-- [0 fields]: invoke OnValidSubmitAsync immediately ->|
 |                                                                           |
 |                                      [Story 2-1 lifecycle flow takes over from here]

# COMPACT INLINE MODE (2-4 non-derivable fields) -------------------
USER          FcCommandRenderer(Compact)   JSInterop(fc-expandinrow)   Form body (Story 2-1)
 |                   |                           |                           |
 |-- click action -->|                           |                           |
 |                   |-- initializeExpandInRow(elementRef) ------>           |
 |                   |<-- stabilized scroll --   |                           |
 |                   |-- ResolveAsync(derivables) (initialize model)         |
 |                   |-- render form body (Story 2-1) ---------------------->|
 |                                                                           |
 |                                      [Story 2-1 lifecycle flow]

# FULL-PAGE MODE (5+ non-derivable fields) -------------------------
USER          /commands/{bc}/{cmd} page   DataGridNavigationState   Form body
 |                   |                           |                           |
 |-- navigate ------>|                           |                           |
 |                   |-- CaptureGridStateAction (if came from grid) --------->|
 |                   |-- ResolveAsync(derivables) (initialize model)         |
 |                   |-- render form body (Story 2-1) ---------------------->|
 |                                                                           |
 |                                      [Story 2-1 lifecycle flow]
 |
 |-- click breadcrumb 'Back' -->|
 |                   |-- RestoreGridStateAction ------------------------------>
 |                   |-- NavigationManager.NavigateTo(returnPath)            |
```

---
