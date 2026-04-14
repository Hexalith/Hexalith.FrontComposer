# Story 2.2: Action Density Rules & Rendering Modes

Status: ready-for-dev

## Story

As a business user,
I want commands to render at the appropriate density -- inline buttons for simple actions, compact inline forms for moderate actions, and full-page forms for complex actions,
so that I can take action quickly on simple commands without navigating away, while complex commands get the space they need.

---

## Review Findings Applied (Party Mode Round 1)

This story was patched after a multi-agent review (Winston, Amelia, Sally, Murat). Key changes from the first draft:

| Concern | Resolution | Reference |
|---|---|---|
| `DataGridNavigationState` half-shipped (effects without producers) | Trimmed to REDUCER-ONLY in Story 2-2; effects deferred to Story 4.3 | Decision D30, Task 6.2 |
| Renderer vs Form submit ownership ambiguity (double `<EditForm>` risk) | Renderer is chrome; Form owns submit. Explicit contract. | ADR-016, Decisions D21 |
| Naming collision `IncrementCommandCommandRenderer` | Strip trailing `Command` suffix → `IncrementRenderer` / `IncrementPage` | Decision D22, Naming table |
| Button label inconsistency ("Send" prefix) | Unified to `"{Humanized BaseName}"` in all modes; Story 2-1 snapshots re-approved | Decision D23, Task 5.3 |
| Pre-fill chain LastUsed-beats-Default footgun | `[DefaultValue]` becomes a hard floor BEFORE LastUsed in the chain | Decision D24, Task 3.4 |
| JS module re-import per component + prerender crash risk | Scoped `IExpandInRowJSModule` with `Lazy<Task<IJSObjectReference>>` cache + prerender guard | Decision D25, Task 4.9 |
| 720px hard-coded literal | `FcShellOptions.FullPageFormMaxWidth` (default 720px) | Decision D26, AC4 |
| `ProjectionContext` cascading unowned | Epic 4 cascades at shell; Story 2-2 ships null-tolerant renderer + Counter manual `<CascadingValue>` | Decision D27, Task 9.3 |
| `LastUsedValueProvider` Fluxor subscription unclear | Hand-written generic provider; per-command emitted typed subscriber (`{BaseName}LastUsedSubscriber.g.cs`) | Decision D28, Task 4bis |
| `FluentPopover` outside-click dismissal semantics | Manual wiring via backdrop click handler | Decision D29 |
| Focus return on popover submit/dismiss | Explicit AC + 2 dedicated tests | AC9, Task 10.1 #8-9 |
| Counter AC10 state-restore "theater" (no capture side) | AC10 no longer claims end-to-end state restoration (deferred to Story 4.3); only proves `RestoreGridStateAction` dispatch contract | AC10, Decision D30 |
| Missing 2-1 regression gate | 12 byte-identical snapshot assertions in CI | Task 5.2 |
| Missing 2-1↔2-2 contract test | Structural equality test | Task 11.4 |
| bUnit JS-interop flakiness risk | `JSRuntimeMode.Loose` prohibited for Task 10.2; explicit `SetupModule` + `VerifyInvoke` required | Task 10 fixture rules |
| Pre-fill race (OnInitializedAsync vs render) | `cut.WaitForAssertion(...)` mandatory; no synchronous `Find` post-render | Task 10 fixture rules |
| 9 density classification tests vs FsCheck opportunity | Replaced with 1 FsCheck property + snapshot boundary + equality/hash = 4 tests | Task 1.4 |
| Test count math undercount (70 → actual ~80-82) | Recounted and reconciled to **97** with explicit table | Task 13.1 |
| Axe-core count unfalsifiable ("per mode") | Explicit 3 tests (one per mode) + separate keyboard tab-order tests | Task 12.1, Task 10.5 |
| Task 13.4 "automated" ambiguity | Transparency note — dev-agent local, not CI-automated E2E | Task 13.4 |

---

## Critical Decisions (READ FIRST -- Do NOT Revisit)

These decisions are BINDING. Tasks reference them by number. If implementation uncovers a reason to change one, raise it before coding, not after.

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | Density is determined at **source-generation time**, NOT runtime. The emitter picks the component shape once based on `NonDerivableProperties.Length`. | Per-command specialization enables deterministic bUnit snapshots, trims dead code paths, and fits the "domain IS the UI" principle. Runtime decision would add branching with no adopter benefit. |
| D2 | The **three modes share one generated form body** (already emitted by Story 2-1's `CommandFormEmitter`). A thin dispatch/shell component (`{CommandName}CommandRenderer.g.razor.cs`) selects the mode at render time based on `RenderMode` parameter, falling back to the density-derived default. | Reuses Story 2-1's render tree; dispatcher can be overridden per-instance by adopters without regenerating. |
| D3 | Density threshold classification is **exposed on `CommandModel`** as a new `CommandDensity` enum (`Inline`, `CompactInline`, `FullPage`) computed in `CommandFormTransform`. The emitter reads this enum; no string comparisons. | IR-level computation. Single source of truth. Propagates to manifests for MCP/agent consumption (Epic 8). |
| D4 | Compact inline forms render inside a **`<FluentCard>` container with `class="fc-expand-in-row"`**, positioned by the host DataGrid row component (Epic 4). For Story 2-2 scope, the compact form is consumable **standalone** (placed below any content), and the DataGrid integration is deferred to Story 4.5 (`expand-in-row-detail`). | Decouples density mode from DataGrid timing. Story 2-2 ships usable compact + full-page modes; Story 4.5 wires them into DataGrid rows. |
| D5 | Full-page mode uses a **generated Blazor route** (`@page "/commands/{BoundedContext}/{CommandName}"`) wrapping the same form body emitted by Story 2-1. Breadcrumb content is rendered via `ICommandPageContext` service (new, registered scoped). | Routable URL enables deep linking, browser back button, and bookmarks. `ICommandPageContext` is the seam that Story 3.1 (shell layout) wires the breadcrumb into. |
| D6 | Inline button mode for **0 non-derivable fields** submits on click (no form, no popover). For **exactly 1 non-derivable field**, it renders a `FluentButton` that, when clicked, opens a **`FluentPopover` anchored to the button** containing a single-field inline form. NOT a dialog. | Popover preserves viewport context; dialog would be heavy for 1 field. Aligns with UX spec §2217 "inline action per density rules". |
| D7 | Derivable field pre-fill is sourced via a **new `IDerivedValueProvider` service** (scoped). The generated renderer calls `provider.ResolveAsync<TCommand>(propertyName, context)` per derivable field before submit. Three built-in providers are chained in this order: **ProjectionContextProvider → LastUsedValueProvider → DefaultValueProvider**. | Extensible for adopters (they register additional providers). Chaining order matches UX spec §1197: "(1) projection context, (2) last-used, (3) default". |
| D8 | `LastUsedValueProvider` persists values to `IStorageService` (Story 1-1 seam) under the key `frontcomposer:lastused:{tenantId}:{userId}:{commandTypeFqn}:{propertyName}`. Values are **session-scoped** (in-memory + flush on `beforeunload`), NOT cross-session. | UX spec §1197 says "session-persisted". Persisting cross-session creates surprise and PII risk. |
| D9 | DataGrid state preservation for full-page form navigation uses **a new per-concern Fluxor feature `DataGridNavigationState`** in `Shell/State/DataGridNavigation/`. Keyed by `"{boundedContext}:{projectionTypeFqn}"`. Contains scroll position, filters, sort, expanded row id, selected row id. | Aligns with architecture D7 (per-concern Fluxor features). Explicit state lives in Fluxor; LocalStorage persistence via existing `IStorageService` + `FlushAsync` hook. |
| D10 | `DataGridNavigationState` is **populated by DataGrid components (Epic 4) and consumed by command renderer only** in Story 2-2. Story 2-2 adds the feature + actions + reducers + contracts but does NOT wire DataGrid writes (that happens in Epic 4). Full-page form reads state on mount and no-ops if empty. | Enables Story 2-2 to ship independently of Epic 4. Feature is forward-compatible. |
| D11 | Expand-in-row scroll stabilization JS is delivered as a **new `wwwroot/js/fc-expandinrow.js` module** exposing `initializeExpandInRow(elementRef)` which: (a) calls `scrollIntoView({block:'nearest'})`, (b) uses `requestAnimationFrame` to re-measure, (c) honours `prefers-reduced-motion` by skipping the rAF smoothing. Loaded via `IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js")`. | UX spec §1207 explicit implementation contract. Module-scoped import avoids global namespace pollution. |
| D12 | Button hierarchy for renderers is enforced **in generated code** via the existing Fluent UI `DefaultValues` system (NOT per-component literal appearance). The renderer emits `Appearance.Primary` / `Appearance.Secondary` per UX spec §2236-2242 mapping; destructive commands (Epic 2.5) override to Danger. | Story 2-2 only wires Primary/Secondary/Outline. Danger wiring is Story 2-5's scope (confirmation dialogs). |
| D13 | Destructive command detection is **deferred to Story 2-5**. Story 2-2 assumes all commands in scope are non-destructive. The renderer does NOT check for destructive naming patterns. | Preserves single-story focus. Destructive handling is a cross-cutting confirmation concern. |
| D14 | The form abandonment warning (UX-DR38, >30s threshold on full-page forms) is **deferred to Story 2-5**. Story 2-2's full-page form exposes an `OnNavigateAwayRequested` callback but does NOT render the warning bar. | Single-story focus. |
| D15 | `ICommandPageContext` exposes `CommandName`, `BoundedContext`, `ReturnPath` (breadcrumb back) ONLY. Shell integration (actual breadcrumb rendering in `FcHeader`) is Story 3.1. For v0.1, the full-page renderer emits an inline breadcrumb `<FluentBreadcrumb>` when `FcShellOptions.EmbeddedBreadcrumb=true` (default). | Allows Counter sample to demonstrate full-page mode before Story 3.1 lands. |
| D16 | The inline popover uses **a single-field form that reuses Story 2-1's field emission for the one non-derivable property**. No new field type plumbing. Submit button inside the popover. | Zero field-type code duplication. |
| D17 | **Density is recomputed on every source-generation run** (deterministic from `NonDerivableProperties.Length`). Adding a derivable field does NOT change density; adding a non-derivable field MAY change density and therefore the rendered component. Snapshots MUST verify all three boundaries: 0→1, 1→2, 4→5. | Prevents drift between generator runs. Regression-catching snapshot test contract. |
| D18 | Pre-fill execution happens **server-side during `OnInitializedAsync`** in Blazor Server, inside `OnInitializedAsync` in Blazor WASM. `IDerivedValueProvider.ResolveAsync` is awaitable to accommodate async providers (future projection lookups). | Async-first contract; synchronous providers return `Task.FromResult`. |
| D19 | **No overlap with Story 2-4**: Story 2-4 owns lifecycle visual feedback (`FcLifecycleWrapper`, sync pulse, "Still syncing..."). Story 2-2 ONLY owns rendering mode selection + layout. The progress ring during `Submitting` remains inside the submit button (already in Story 2-1's `CommandFormEmitter` Task 3B.3). Do NOT re-emit lifecycle UI in the renderer. | Prevents duplicate emission. |
| D20 | **No overlap with Story 2-3**: Story 2-3 owns `ILifecycleStateService` + ULID idempotency. Story 2-2 consumes Story 2-1's existing `{CommandName}LifecycleState` Fluxor state for button disabled-during-Submitting behavior only. | Prevents duplicate lifecycle plumbing. |
| D21 | **Renderer is chrome, Form owns submit.** The `{CommandName}Form` emitted by Story 2-1 retains FULL ownership of `<EditForm>`, `EditContext`, `OnValidSubmitAsync`, and all lifecycle dispatch. The renderer only wraps/positions the form (FluentCard for Compact, breadcrumb container for FullPage, FluentPopover for 1-field Inline). The renderer NEVER emits `<EditForm>`. See ADR-016. | Prevents double-EditForm. Single source of truth for validation + lifecycle. |
| D22 | **Generated type names strip trailing `Command` suffix** from the source command type to avoid `IncrementCommandCommandRenderer`. Rule: if `{CommandTypeName}` ends with `Command`, the renderer hint name uses `{BaseName}Renderer.g.razor.cs` where `BaseName = CommandTypeName[..^7]`. Same rule for Page. Story 2-1's `{CommandName}Form` is frozen as `{FullCommandTypeName}Form.g.razor.cs` (no trim) for backward compatibility. | `IncrementCommand → IncrementRenderer + IncrementPage + IncrementCommandForm`. |
| D23 | **Button label is `"{Humanized CommandBaseName}"` everywhere** (no "Send " prefix for any mode). Button-label rule in Story 2-1 Task 2.3 must be updated: `"{HumanizeCamelCase(BaseName)}"` where `BaseName = strip-trailing-Command({CommandTypeName})`. | UX consistency (Sally: "Send" is developer-speak). Label parity across all three modes reduces product-feel drift. |
| D24 | **Pre-fill chain honors `[DefaultValue]` as a hard floor.** Chain order becomes: `SystemValueProvider → ProjectionContextProvider → ExplicitDefaultValueProvider (if [DefaultValue] attr present) → LastUsedValueProvider → ConstructorDefaultValueProvider`. If a property declares `[DefaultValue(x)]`, `x` beats LastUsed. If no `[DefaultValue]`, LastUsed beats constructor default. | Protects reset-semantics fields (e.g., `[DefaultValue(1)] int Amount`) from stale last-used values. |
| D25 | **JS module is cached via `Lazy<Task<IJSObjectReference>>` in a scoped `IExpandInRowJSModule` service**, NOT re-imported per component. Module is only initialized when `OnAfterRenderAsync(firstRender=true)` AND `RendererInfo.IsInteractive` is true (guards prerendering). Module is disposed on circuit teardown via the scoped service's `IAsyncDisposable`. | Prevents prerender crashes. Prevents per-instance module re-import. |
| D26 | **`FcShellOptions.FullPageFormMaxWidth`** (default `"720px"`) replaces the hard-coded 720px literal in the renderer emitter. Compact mode does not constrain width. | Themeable via options; still ships with UX spec default. |
| D27 | **`ProjectionContext` cascading is rendered by the host DataGrid row component (Epic 4)**, NOT by Story 2-2. Story 2-2's renderer reads `[CascadingParameter] public ProjectionContext? ProjectionContext { get; set; }` and tolerates null. The Counter sample wires a **manual `<CascadingValue Value="@_manualContext">`** around the inline command renderer to demonstrate pre-fill; generic shell cascading is Epic 4 (Story 4.1). | Decouples Story 2-2 from Epic 4 DataGrid timing. Null-safe degradation. |
| D28 | **LastUsedValueProvider is hand-written (generic), but each `[Command]` emits its own `{BaseName}LastUsedSubscriber.g.cs`** registering via `Fluxor.IActionSubscriber.SubscribeToAction<{CommandName}Actions.ConfirmedAction>(...)`. The subscriber calls `LastUsedValueProvider.Record<TCommand>(command)` with typed command. Emitted per command to avoid reflection dispatch. | Static subscription is AOT-safe; tests use `Fluxor.TestStore`. |
| D29 | **FluentPopover outside-click dismissal is wired manually** via a click-outside handler on a transparent backdrop `<div>` rendered inside the popover open state. Dismiss calls the renderer's `ClosePopoverAsync` which restores focus to the trigger button. | FluentPopover v5 does not auto-dismiss on outside-click; explicit handler required. |
| D30 | **DataGridNavigationState in Story 2-2 is REDUCER-ONLY.** Story 2-2 ships: feature, state record, actions (Capture, Restore, Clear, PruneExpired), reducers, IEquatable tests. **Effects (IStorageService persistence, hydration on SessionStart, beforeunload flush) are DEFERRED to Story 4.3** alongside DataGrid capture-side wiring. FullPage renderer dispatches `RestoreGridStateAction` which is a no-op in 2-2 (state map always empty); wiring proves the contract. | Resolves Winston's "dead code" risk and Sally's "theater" finding. Feature contract locked, persistence earns its keep in Epic 4. |

---

## Architecture Decision Records

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

- **Status:** Accepted
- **Context:** Story 2-1 emits `{FullCommandTypeName}Form.g.razor.cs` that owns `<EditForm>`, `EditContext`, `OnValidSubmitAsync`, and all lifecycle dispatch. Story 2-2 introduces three rendering modes that must REUSE this form without duplicating submit orchestration and without creating nested `<EditForm>` elements.
- **Decision:** The renderer emitted by Story 2-2 is **CHROME ONLY**. Rules:
  1. The renderer NEVER emits `<EditForm>`.
  2. The renderer ALWAYS wraps `<{FullCommandTypeName}Form ... />` as the single inner component that owns validation and submit.
  3. Story 2-1's `CommandFormEmitter` is extended (backward-compatible) with two parameters: `DerivableFieldsHidden` (bool, default false) and `ShowFieldsOnly` (string[]?, default null).
  4. The three modes differ ONLY in wrapping chrome:
     - **Inline (0 fields):** renderer emits a single `FluentButton` whose `OnClick` dispatches a synthetic form submit via `EventCallback` exposed by the Form (`[Parameter] public EventCallback OnExternalSubmitRequested { get; set; }`). Form handles the rest.
     - **Inline (1 field):** renderer emits the button + a `FluentPopover` containing `<{FullCommandTypeName}Form ShowFieldsOnly="new[]{\"{PropName}\"}" />`. Form retains its `<EditForm>` — this is acceptable because there is exactly one form per page.
     - **CompactInline:** renderer emits `<FluentCard class="fc-expand-in-row">` wrapping `<{FullCommandTypeName}Form DerivableFieldsHidden="true" />`.
     - **FullPage:** renderer emits breadcrumb + max-width container wrapping `<{FullCommandTypeName}Form />`.
  5. Story 2-1's `OnValidSubmitAsync` is the SOLE submit path. The renderer MUST NOT dispatch `{CommandName}Actions.SubmittedAction` itself.
  6. The 0-field inline button path requires ONE new capability in Story 2-1's Form: trigger `OnValidSubmitAsync` externally. Added parameter: `public ElementReference? ExternalTriggerAnchor { get; set; }` + exposed method `InvokeAsync(Func<Task>)` wrapper. Simpler: Form exposes `[Parameter] public Action? RegisterExternalSubmit { get; set; }` which the renderer stores and invokes.
- **Consequences:** (+) Single validation path; (+) lifecycle dispatch stays in Form; (+) Popover with inner `<EditForm>` is standard Blazor. (-) Requires 3 new Form parameters (still backward-compatible — all default to pre-2-2 behavior).
- **Rejected alternatives:** Renderer owns submit and Form becomes field-render helper (duplicates lifecycle dispatch from 2-1 — ADR-010 violation); emit three separate Form variants (combinatorial emitter explosion); use `CascadingValue` to pass submit handler (overkill for 1 hop).

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

## Acceptance Criteria

### AC1: Density Classification on IR

**Given** a `[Command]`-annotated record
**When** `AttributeParser.ParseCommand` runs
**Then** the emitted `CommandModel` exposes `CommandDensity Density` (enum: `Inline`, `CompactInline`, `FullPage`)
**And** `Density` is computed as:
- `NonDerivableProperties.Length <= 1` → `Inline`
- `NonDerivableProperties.Length in [2..4]` → `CompactInline`
- `NonDerivableProperties.Length >= 5` → `FullPage`

**And** `Density` participates in `CommandModel.Equals` and `GetHashCode` (Decision D3, ADR-009).

### AC2: Inline Button Mode (0-1 Non-Derivable Fields)

**Given** a `[Command]` with 0 non-derivable fields
**When** its generated `{CommandName}CommandRenderer` is placed on any page
**Then** it renders as a single `FluentButton`
**And** appearance is `Appearance.Secondary` when inside a DataGrid row context (`CommandRenderMode.Inline`)
**And** a leading `FluentIcon` is rendered if the `[Command]` declares one via `[Icon(...)]` (new attribute, see Task 1) or the default `Regular.Size16.Play` icon otherwise
**And** the button label is `"{Humanized CommandBaseName}"` (Decision D22, D23 — trailing `Command` suffix stripped, no `"Send "` prefix in ANY mode)
**And** clicking the button:
- Pre-fills derivable fields via `IDerivedValueProvider` chain (AC7)
- Submits immediately via the same lifecycle flow as Story 2-1 (no popover, no form)

**Given** a `[Command]` with exactly 1 non-derivable field
**When** its renderer is in inline mode
**Then** clicking the button opens a `FluentPopover` anchored to the button (Decision D6, D16)
**And** the popover contains the single field using Story 2-1's emitted field component
**And** the popover includes a Primary submit button and a Secondary cancel button
**And** pressing Escape dismisses the popover
**And** submitting dispatches the Story 2-1 lifecycle flow

### AC3: Compact Inline Form Mode (2-4 Non-Derivable Fields)

**Given** a `[Command]` with 2-4 non-derivable fields
**When** its generated renderer is placed with `RenderMode.CompactInline` (default for this density)
**Then** it renders a `FluentCard` with CSS class `fc-expand-in-row`
**And** the card contains the Story 2-1 form body (non-derivable fields only; derivable fields are pre-filled and hidden)
**And** the submit button uses `Appearance.Primary` (new visual context per UX spec §2229)
**And** on `OnAfterRenderAsync(firstRender=true)`, `fc-expandinrow.js::initializeExpandInRow` is invoked via `IJSRuntime` (Decision D11)
**And** when `prefers-reduced-motion` is set, the JS module skips the `requestAnimationFrame` smoothing and only calls `scrollIntoView({block:'nearest'})`
**And** the form is consumable standalone (placed below any content) for Story 2-2 scope; DataGrid row integration lands in Story 4.5

### AC4: Full-Page Form Mode (5+ Non-Derivable Fields)

**Given** a `[Command]` with 5+ non-derivable fields
**When** its generated `{CommandName}CommandRenderer` is placed as a page component
**Then** a Blazor route `@page "/commands/{BoundedContext}/{CommandBaseName}"` is emitted (Decision D5, D22)
**And** the page renders the Story 2-1 form body wrapped in `<div style="max-width: @FcShellOptions.FullPageFormMaxWidth; margin: 0 auto;">` (reuses Story 2-1 AC3 layout; width from options per Decision D26)
**And** an embedded `FluentBreadcrumb` displays "{BoundedContext} > {Humanized CommandBaseName}" when `FcShellOptions.EmbeddedBreadcrumb=true` (Decision D15, D23, default true)
**And** on mount, `RestoreGridStateAction` is dispatched with the referring bounded-context + projection (from `NavigationManager.Uri` parsing) — no-op if state is empty (Decision D10)
**And** on successful `Confirmed`, the renderer navigates to `ReturnPath` (from `ICommandPageContext.ReturnPath`) or the home route if unset

### AC5: Density-Driven Renderer Selection

**Given** a generated `{CommandName}CommandRenderer.g.razor.cs`
**When** the `RenderMode` parameter is unset
**Then** the renderer uses the density-derived default (`Density.Inline` → `CommandRenderMode.Inline`, etc.)

**When** `RenderMode` is explicitly set
**Then** the specified mode is rendered, overriding the default
**And** if the specified mode is incompatible with the density (e.g., `CommandRenderMode.Inline` on a 5-field command), a compile-time warning `HFC1008` (NEW) is emitted at the consumption site via analyzer reporting — or runtime warning log if not statically detectable. MVP scope: runtime `ILogger` warning only; analyzer reporting is deferred to Epic 9.

### AC6: Derivable Field Pre-Fill via IDerivedValueProvider Chain

**Given** `IDerivedValueProvider` is registered with built-in providers in this exact order (Decision D24): `SystemValueProvider` → `ProjectionContextProvider` → `ExplicitDefaultValueProvider` → `LastUsedValueProvider` → `ConstructorDefaultValueProvider`
**When** a renderer initializes (compact/full-page) or is about to submit (inline 0-field)
**Then** each derivable field is resolved by iterating the provider chain and taking the first `HasValue=true`
**And** `SystemValueProvider` handles `MessageId`, `CorrelationId`, `Timestamp`, `UserId`, `TenantId`, `CreatedAt`, `ModifiedAt` (per Story 2-1 Task 1.3 keys)
**And** `ProjectionContextProvider` reads from `[CascadingParameter] public ProjectionContext? ProjectionContext { get; set; }` on the renderer (null-tolerant per Decision D27; cascade source is Epic 4 DataGrid row or Counter sample manual `<CascadingValue>`)
**And** `ExplicitDefaultValueProvider` returns `HasValue=true` ONLY when the property has a `[DefaultValue(x)]` attribute — uses that attribute's value (Decision D24, protects reset-semantics fields)
**And** `LastUsedValueProvider` reads from `IStorageService` under key `frontcomposer:lastused:{tenantId}:{userId}:{commandTypeFqn}:{propertyName}` (Decision D8)
**And** `LastUsedValueProvider` writes to that key via a per-command emitted `{BaseName}LastUsedSubscriber.g.cs` (Decision D28) that subscribes to `{CommandName}Actions.ConfirmedAction` via `Fluxor.IActionSubscriber.SubscribeToAction<...>` and calls a typed `LastUsedValueProvider.Record<TCommand>(command)` — no reflection dispatch
**And** `ConstructorDefaultValueProvider` returns the command record's constructor default (property initialization value) via compile-time generated property accessors in the emitted subscriber, NOT runtime reflection

**And** if no provider returns `HasValue=true` for a field that is classified as derivable, an error is logged and the field reverts to `default(T)` with a visible `FcFieldPlaceholder` in debug mode

### AC7: DataGridNavigationState Fluxor Feature (Reducer-Only Scope per Decision D30)

**Given** the new `DataGridNavigationState` Fluxor feature
**When** it is registered (via `AddHexalithFrontComposer()`)
**Then** actions exist: `CaptureGridStateAction(string viewKey, GridViewSnapshot snapshot)`, `RestoreGridStateAction(string viewKey)`, `ClearGridStateAction(string viewKey)`, `PruneExpiredAction(DateTimeOffset threshold)`
**And** state shape: `ImmutableDictionary<string, GridViewSnapshot>` where `GridViewSnapshot(double ScrollTop, ImmutableDictionary<string,string> Filters, string? SortColumn, bool SortDescending, string? ExpandedRowId, string? SelectedRowId, DateTimeOffset CapturedAt)`
**And** `PruneExpiredAction` reducer removes snapshots older than the 24-hour threshold
**And** `viewKey` format is `"{commandBoundedContext}:{projectionTypeFqn}"` — source BC is the command's BC (Decision D22 naming trim does NOT apply here; `projectionTypeFqn` is recorded in full to survive command renames but invalidates on projection FQN refactor — documented tradeoff)
**And** unit tests verify: capture→restore round-trip, TTL expiry (via direct `PruneExpiredAction` dispatch), per-view isolation, IEquatable semantics

**NOT IN SCOPE for Story 2-2 (deferred to Story 4.3 per Decision D30):**
- `IStorageService` persistence effect (writes on Capture, reads on SessionStart)
- `beforeunload` flush hook
- DataGrid capture-side wiring (scroll/filter/sort/expansion event producers)

**And** the FullPage renderer dispatches `RestoreGridStateAction` on mount — this is a no-op in v0.1 (state map always empty) and proves the action contract without requiring persistence.

### AC8: Button Hierarchy Compliance

**Given** any generated `{CommandName}CommandRenderer`
**When** the renderer emits its submit button
**Then** the appearance mapping follows UX spec §2236-2242 (Decision D12):
| Mode | Context | Appearance | Icon |
|---|---|---|---|
| Inline | DataGrid row | Secondary | Leading action icon |
| Inline (0 fields) | Any | Secondary | Leading action icon |
| CompactInline | Expand-in-row | Primary | Leading action icon |
| FullPage | Dedicated page | Primary | Leading action icon |

**And** no renderer emits `Appearance.Danger` — that is Story 2-5's scope (Decision D13)
**And** icon resolution: `[Icon(IconName)]` attribute > default per mode (Play for inline, Send for compact/full)
**And** snapshot tests verify the emitted appearance/icon combinations for all three modes

### AC9: Accessibility, Focus Return & Keyboard Contract

**Given** a renderer in any mode
**When** keyboard navigation is exercised
**Then**:
- Inline mode: `Tab` reaches the button; `Enter`/`Space` activates it; popover (1-field) is keyboard-dismissable via `Escape`; `Tab` inside the popover cycles between field, submit, cancel; outside-click dismissal is manually wired (Decision D29)
- CompactInline mode: `Tab` reaches the card; `Escape` closes the expanded form (via `OnCollapseRequested` callback); form fields follow Story 2-1's field order
- FullPage mode: `Tab` skips to content via skip-link; breadcrumb is focusable; form has `aria-label="{Humanized CommandBaseName} command form"` (Decision D23)

**And** `aria-expanded` is set correctly on the inline button when a popover is open

**Focus return contract (per Sally's review — binding):**
- **Popover submit (success):** On `Confirmed` state transition, focus returns to the INVOKING trigger button; the triggering row (if inside a DataGrid) remains scrolled into view via `scrollIntoView({block:'nearest'})` on the trigger's `ElementReference`
- **Popover submit (rejected):** Focus returns to the FIRST invalid field in the popover; popover remains open
- **Popover dismiss (Escape or outside-click):** Focus returns to the trigger button; no form submission occurs; row remains scrolled into view
- **CompactInline collapse (Escape):** Focus returns to whichever element invoked the expansion (recorded via `OnCollapseRequested` callback); if no invoker is recorded, focus falls back to the first focusable element in the containing section
- **FullPage submit (success):** Navigation to `ReturnPath` occurs; focus is set to the skip-link target of the destination page (native Blazor `NavigationManager.NavigateTo` behavior; verified, not overridden)

**And** axe-core scan on Counter sample with all three density modes shows zero serious/critical violations
**And** dedicated keyboard tab-order tests (separate from axe-core DOM scans) verify the full focus journey for each mode using bUnit `cut.InvokeAsync(() => element.FocusAsync())` + element-under-focus assertions

### AC10: Counter Sample Demonstrates All Three Modes

**Given** the Counter sample after this story
**When** the Counter.Web app runs
**Then** three `[Command]`-annotated records demonstrate each mode:
- `IncrementCommand` (existing, 1 non-derivable field `Amount` — `TenantId` and `MessageId` are derivable per Story 2-1 Task 1.3 keys) → Inline with popover
- `BatchIncrementCommand` (new, 3 non-derivable fields: `Amount`, `Note`, `EffectiveDate`) → CompactInline
- `ConfigureCounterCommand` (new, 5 non-derivable fields: `Name`, `Description`, `InitialValue`, `MaxValue`, `Category`) → FullPage

**And** the CounterPage demonstrates all three:
- A header row with the CompactInline `BatchIncrementRenderer` (expand-in-row style, standalone placement; Decision D22 naming)
- An inline-button `IncrementRenderer` rendered with its popover
- A link navigating to the full-page route `/commands/Counter/ConfigureCounter`

**And** the Counter sample wraps the inline/compact renderers in a manual `<CascadingValue Value="@_demoProjectionContext">` to demonstrate `ProjectionContextProvider` pre-fill for the derivable aggregate ID (Decision D27 — shell-level cascading is Epic 4)

**And** AC10 does NOT claim DataGrid state preservation on return from FullPage (that demo is deferred to Story 4.3 per Decision D30 — 2-2 only proves the `RestoreGridStateAction` dispatch contract with an empty state map)

**And** manual smoke test confirms: lifecycle progress ring appears in Submitting, the `CounterProjection` DataGrid refreshes after Confirmed (reusing the effect from Story 2-1 Task 7.3, extended to the two new commands), all three modes survive a full `dotnet watch` hot-reload cycle (per Story 1-8 constraints; note: adding `[Icon]` to a command requires a full restart per Story 1-8 hot reload limitations).

**References:** FR8, UX-DR16, UX-DR17 (scroll stabilization — implementation side), UX-DR19 (DataGrid state preservation — feature side), UX-DR36 (button hierarchy), NFR89 (≤2 clicks)

---

## Known Gaps (Explicit, Not Bugs)

These are cross-story deferrals intentionally out of scope for Story 2-2. QA should NOT file these as defects. If shipped behavior is observed to be missing, link to the owning story.

| Gap | Owning Story | Reason |
|---|---|---|
| Danger appearance + destructive confirmation dialog | Story 2-5 | Confirmation UX is a cross-cutting concern tied to form abandonment; single-story focus |
| 30-second form abandonment warning (UX-DR38) | Story 2-5 | Same family as destructive confirmation |
| HFC1008 analyzer-emitted diagnostic for RenderMode/density mismatch | Epic 9 | Analyzer emission is Story 9.4's domain; 2-2 ships runtime `ILogger` warning only |
| DataGrid capture-side Fluxor wiring (scroll, filter, sort, expansion event producers) | Story 4.3 | DataGrid component surface is Epic 4 |
| `DataGridNavigationState` effects (persistence, hydration, beforeunload flush) | Story 4.3 | Effects land with capture producers; 2-2 ships reducers only (Decision D30) |
| Shell header breadcrumb integration | Story 3.1 | Shell layout is Epic 3; 2-2 ships embedded `FluentBreadcrumb` fallback (opt-out via `FcShellOptions.EmbeddedBreadcrumb`) |
| Shell-level `ProjectionContext` cascading from DataGrid rows | Story 4.1 | Epic 4 DataGrid infrastructure; 2-2 ships null-tolerant renderer + Counter-sample manual cascade |
| Real DataGrid state preservation end-to-end demo | Story 4.3 | 2-2 only proves `RestoreGridStateAction` dispatch contract with an empty state map (Decision D30) |
| `[PrefillStrategy(SkipLastUsed=true)]` attribute (bypass LastUsed entirely) | Future (post-v0.1) | Current `[DefaultValue]` hard-floor rule (Decision D24) handles 80% of cases; wider attribute added on adopter feedback |

---

## Tasks / Subtasks

### Task 0: Prerequisites (AC: all)

- [ ] 0.1: Confirm Story 2-1 is merged and its `CommandModel`, `FormFieldModel`, `CommandFormModel`, `CommandFormEmitter`, `{CommandName}Actions`, `StubCommandService`, and `DerivedFromAttribute` are available. If not, HALT and raise blocker.
- [ ] 0.2: Verify Story 2-1 sample (`IncrementCommand`) does NOT assert or pin a specific route URL in any AC or test — route ownership flips to Story 2-2. If 2-1 pins a route, update 2-1's AC5 test + fix migration note in `deferred-work.md`.
- [ ] 0.3: Confirm `Microsoft.AspNetCore.Components.Web` JSInterop usage (`IJSRuntime`, `IJSObjectReference`) — present from Story 1-8; verify.
- [ ] 0.4: Confirm `Fluxor.Blazor.Web` ≥ 5.9 is referenced in `Shell.csproj` (needed for `IActionSubscriber.SubscribeToAction<TAction>`). Pin in `Directory.Packages.props` if not yet pinned.
- [ ] 0.5: Create new attribute `IconAttribute` in `Contracts/Attributes/`:
  ```csharp
  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
  public sealed class IconAttribute : Attribute
  {
      public IconAttribute(string iconName) { IconName = iconName; }
      public string IconName { get; }  // e.g., "Regular.Size16.Play"
  }
  ```
- [ ] 0.6: **Reuse `System.ComponentModel.DefaultValueAttribute`** — do NOT create a new attribute. The `ExplicitDefaultValueProvider` reads this type (Decision D24).
- [ ] 0.7: Register diagnostic `HFC1008` in `DiagnosticDescriptors.cs`: "RenderMode incompatible with command density" (severity: Warning, deferred to Epic 9 for analyzer emission; Story 2-2 wires the log message).
- [ ] 0.8: Update `AnalyzerReleases.Unshipped.md` with `HFC1008`.

### Task 1: Extend CommandModel IR with Density (AC: 1) (See Decision D3, ADR-013)

- [ ] 1.1: Add `CommandDensity` enum to `SourceTools/Parsing/DomainModel.cs`:
  ```csharp
  public enum CommandDensity { Inline, CompactInline, FullPage }
  ```
- [ ] 1.2: Add `Density` property to `CommandModel` (sealed class, Decision D1 from Story 2-1 carries):
  - Compute in constructor from `NonDerivableProperties.Length`
  - Include in `Equals` and `GetHashCode` (ADR-009)
- [ ] 1.3: Add `IconName` property to `CommandModel` (nullable string). Populate from `[Icon]` attribute in `AttributeParser.ParseCommand` if present; validate via `EscapeString` helper.
- [ ] 1.4: Unit tests for density classification — **exactly 4 tests** (replaces 9-test example matrix with property-based coverage per Murat's review):
  1. `Density_ClassificationProperty` (FsCheck): for any `int count ∈ [0, int.MaxValue)`, `ComputeDensity(count)` matches the specification: `count ≤ 1 → Inline`, `count ∈ [2..4] → CompactInline`, `count ≥ 5 → FullPage`. Seed-pinned to catch regression.
  2. `Density_BoundarySnapshot_AtZeroOneTwoFourFive` — single snapshot asserting CommandModel.Density for a command with 0, 1, 2, 4, and 5 fields in a table (Decision D17 boundary parity)
  3. `CommandModel_Equality_IncludesDensityAndIconName` — two CommandModels differing only by `Density` are non-equal; differing only by `IconName` are non-equal
  4. `CommandModel_HashCode_IncludesDensityAndIconName` — consistency check

### Task 2: Command Render Mode Types (AC: 5)

- [ ] 2.1: Add `CommandRenderMode` enum to `Contracts/Rendering/`:
  ```csharp
  public enum CommandRenderMode { Inline, CompactInline, FullPage }
  ```
- [ ] 2.2: Add `ICommandPageContext` to `Contracts/Rendering/`:
  ```csharp
  public interface ICommandPageContext
  {
      string CommandName { get; }
      string BoundedContext { get; }
      string? ReturnPath { get; }
  }
  ```
- [ ] 2.3: Add `ProjectionContext` cascading parameter type to `Contracts/Rendering/`:
  ```csharp
  public sealed record ProjectionContext(
      string ProjectionTypeFqn,
      string BoundedContext,
      string? AggregateId,
      IReadOnlyDictionary<string, object?> Fields);
  ```
- [ ] 2.4: Add `FcShellOptions.EmbeddedBreadcrumb` (bool, default true) to `Contracts/FcShellOptions.cs` (create if absent).

### Task 3: DerivedValueProvider Chain (AC: 6) (See ADR-014, Decisions D24, D28)

- [ ] 3.1: Add `IDerivedValueProvider` to `Contracts/Rendering/`:
  ```csharp
  public interface IDerivedValueProvider
  {
      Task<DerivedValueResult> ResolveAsync(
          Type commandType,
          string propertyName,
          ProjectionContext? projectionContext,
          CancellationToken ct);
  }
  public readonly record struct DerivedValueResult(bool HasValue, object? Value);
  ```
- [ ] 3.2: Implement `SystemValueProvider` in `Shell/Services/DerivedValues/` (Scoped):
  - Handles `MessageId` (new ULID), `CorrelationId` (new Guid), `Timestamp` (DateTimeOffset.UtcNow), `CreatedAt`, `ModifiedAt`
  - `UserId`, `TenantId` read from `IHttpContextAccessor` claims when present; fall through otherwise
  - Registered 1st in the chain
- [ ] 3.3: Implement `ProjectionContextProvider` in `Shell/Services/DerivedValues/` (Scoped):
  - Takes `ProjectionContext?` parameter directly (null-tolerant per Decision D27)
  - Maps property name to `Fields[propertyName]` or `AggregateId` when property name matches `{ProjectionName}Id` convention
  - Registered 2nd in the chain
- [ ] 3.4: Implement `ExplicitDefaultValueProvider` in `Shell/Services/DerivedValues/` (Singleton — pure reflection, no scoped deps per Decision D24):
  - Returns `HasValue=true` ONLY if the property has `[System.ComponentModel.DefaultValueAttribute]` — returns the attribute's `Value`
  - Otherwise `HasValue=false` (chain continues)
  - Registered 3rd in the chain (beats LastUsed — protects reset-semantics)
- [ ] 3.5: Implement `LastUsedValueProvider` in `Shell/Services/DerivedValues/` (Scoped):
  - Reads from `IStorageService` key `frontcomposer:lastused:{tenantId}:{userId}:{commandTypeFqn}:{propertyName}`
  - Exposes `public Task Record<TCommand>(TCommand command) where TCommand : class` — persists ALL non-system properties to storage
  - Does NOT subscribe to Fluxor itself. Per-command typed subscribers are EMITTED by Task 4bis and call `Record<TCommand>` on the Confirmed transition.
  - Registered 4th in the chain
- [ ] 3.6: Implement `ConstructorDefaultValueProvider` in `Shell/Services/DerivedValues/` (Singleton):
  - Reads command type's property default via a compiled delegate cache (`new TCommand()` then get property) — NOT per-call reflection
  - Delegate cache keyed by `Type`
  - Registered 5th (last) in the chain — final fallback
- [ ] 3.7: Add `AddDerivedValueProvider<T>(this IServiceCollection, ServiceLifetime lifetime)` extension in `Shell/Extensions/`:
  - Prepends to the chain (custom providers win over all built-ins)
  - Lifetime defaults to `Scoped`; adopter supplies if Singleton
- [ ] 3.8: Register built-in providers in `AddHexalithFrontComposer()` in this exact order (Decision D24): `System → ProjectionContext → ExplicitDefault → LastUsed → ConstructorDefault`.
- [ ] 3.9: Unit tests for provider chain — **exactly 16 tests** (was 14; +2 for new 5-provider chain):
  - 2 per provider (positive resolve + miss) × 5 = 10
  - Chain ordering (5 tests: system beats projection, projection beats explicit-default, explicit-default beats last-used, last-used beats constructor-default, prepended custom beats all built-ins)
  - Chain stops at first HasValue=true (1 test)

### Task 4bis: Per-Command LastUsed Subscriber Emitter (AC: 6) (See Decision D28)

- [ ] 4bis.1: Create `LastUsedSubscriberEmitter.cs` in `SourceTools/Emitters/`. Emits `{CommandFqn}LastUsedSubscriber.g.cs` per `[Command]`:
  ```csharp
  // Example emitted output (netstandard2.0-safe, no Fluxor/FluentUI ref in emitter — strings only):
  public sealed class {BaseName}LastUsedSubscriber : IDisposable
  {
      private readonly Fluxor.IActionSubscriber _subscriber;
      private readonly LastUsedValueProvider _provider;

      public {BaseName}LastUsedSubscriber(Fluxor.IActionSubscriber subscriber, LastUsedValueProvider provider)
      {
          _subscriber = subscriber;
          _provider = provider;
          _subscriber.SubscribeToAction<{CommandName}Actions.ConfirmedAction>(this, OnConfirmed);
      }

      private void OnConfirmed({CommandName}Actions.ConfirmedAction action)
      {
          // CorrelationId → command lookup: LastUsedValueProvider holds the most recent submitted command per CorrelationId in a scoped WeakDict populated by SubmittedAction subscription (emit in same class).
          // Simpler: subscribe to SubmittedAction too; record on Submitted, flush on Confirmed.
          _ = _provider.FlushAsync(action.CorrelationId);
      }

      public void Dispose() => _subscriber.UnsubscribeFromAllActions(this);
  }

  // Registration partial emitted to {BaseName}LastUsedSubscriberRegistration.g.cs:
  public static partial class {CommandName}ServiceCollectionExtensions
  {
      public static IServiceCollection Add{BaseName}LastUsedSubscriber(this IServiceCollection services)
          => services.AddScoped<{BaseName}LastUsedSubscriber>();
  }
  ```
- [ ] 4bis.2: Wire per-command registration into the existing `FrontComposerRegistry.RegisterDomain()` aggregation (ADR-012 pattern). At runtime, all subscribers are resolved eagerly on circuit start via a hosted init step.
- [ ] 4bis.3: Unit tests — **exactly 5 tests**:
  1. Emitter generates subscriber per command
  2. Snapshot of emitted subscriber code
  3. Subscriber registers on construction
  4. Subscriber unsubscribes on Dispose
  5. Confirmed flushes `LastUsedValueProvider.FlushAsync` with correct CorrelationId (via `Fluxor.TestStore`)

### Task 4: CommandRendererEmitter — Core (AC: 2, 3, 4, 5, 8) (See Decisions D1, D2, D6, D12, D21, D22, D25, ADR-016)

- [ ] 4.1: Create `CommandRendererTransform.cs` in `SourceTools/Transforms/`:
  - Input: `CommandModel`
  - Output: `CommandRendererModel` (sealed class, manual IEquatable per ADR-009)
  - Fields: `TypeName`, `BaseName` (TypeName with trailing `Command` stripped per Decision D22), `Namespace`, `BoundedContext`, `Density`, `IconName`, `ButtonLabel` (= `HumanizeCamelCase(BaseName)` per Decision D23), `FullPageRoute` (= `/commands/{BoundedContext}/{BaseName}` per Decision D22), `NonDerivablePropertyNames` (EquatableArray<string>), `DerivablePropertyNames` (EquatableArray<string>), `HasIconAttribute` (bool)
- [ ] 4.2: Create `CommandRendererEmitter.cs` in `SourceTools/Emitters/`. Emits `{BaseName}Renderer.g.razor.cs` partial class (Decision D22) inheriting `ComponentBase` (NO `IAsyncDisposable` — the module lifecycle is owned by the scoped `IExpandInRowJSModule` service per Decision D25).
- [ ] 4.3: Emitted class structure (binding contract — CHROME ONLY per ADR-016):
  ```csharp
  public partial class {BaseName}Renderer : ComponentBase
  {
      [Parameter] public CommandRenderMode? RenderMode { get; set; }
      [Parameter] public {CommandTypeFqn}? InitialValue { get; set; } // forwarded to inner Form
      [Parameter] public EventCallback<NavigationAwayRequest> OnNavigateAwayRequested { get; set; }
      [Parameter] public EventCallback OnCollapseRequested { get; set; } // CompactInline only
      [CascadingParameter] public ProjectionContext? ProjectionContext { get; set; }

      [Inject] private IEnumerable<IDerivedValueProvider> DerivedValueProviders { get; set; } = default!;
      [Inject] private IExpandInRowJSModule ExpandInRowJS { get; set; } = default!; // scoped, Lazy-cached (Decision D25)
      [Inject] private NavigationManager NavigationManager { get; set; } = default!;
      [Inject] private ILogger<{BaseName}Renderer>? Logger { get; set; }

      private {CommandTypeFqn} _prefilledModel = new();
      private CommandRenderMode _effectiveMode;
      private bool _popoverOpen;
      private ElementReference _compactCardRef;
      private ElementReference _triggerButtonRef; // for focus return (AC9)
      private Action? _externalSubmit; // registered by the Form for 0-field inline synthetic submit (ADR-016 rule 6)

      protected override async Task OnInitializedAsync()
      {
          _effectiveMode = RenderMode ?? CommandRenderMode.{DensityDerivedMode}; // from IR
          if (!IsModeCompatibleWithDensity(_effectiveMode, CommandDensity.{Density}))
              Logger?.LogWarning("HFC1008: RenderMode {Mode} incompatible with {BaseName} density {Density}",
                  _effectiveMode, "{BaseName}", CommandDensity.{Density});

          _prefilledModel = InitialValue ?? new();
          await PrefillDerivableFieldsAsync();
      }

      private async Task PrefillDerivableFieldsAsync()
      {
          foreach (var propName in DerivablePropertyNames)
          {
              foreach (var provider in DerivedValueProviders)
              {
                  var result = await provider.ResolveAsync(typeof({CommandTypeFqn}), propName, ProjectionContext, default);
                  if (result.HasValue) { SetProperty(propName, result.Value); break; }
              }
          }
      }

      private static readonly string[] DerivablePropertyNames = new[] { {DerivablePropertyNames} };

      // SetProperty: compile-time generated per-property switch (no reflection on hot path)
      private void SetProperty(string name, object? value)
      {
          switch (name)
          {
              {foreach derivableProperty:}
              case "{PropertyName}": _prefilledModel.{PropertyName} = ({PropertyTypeFqn})value!; break;
              {/foreach}
          }
      }

      // Called by the inner {CommandName}Form via [Parameter] RegisterExternalSubmit (ADR-016)
      private void OnFormRegisteredExternalSubmit(Action submit) => _externalSubmit = submit;

      // Called by renderer's own 0-field inline button click
      private async Task OnZeroFieldClickAsync()
      {
          // Delegate to Form's OnValidSubmitAsync via the registered callback.
          // Form is still rendered (hidden) so its EditContext/OnValidSubmit is wired.
          _externalSubmit?.Invoke();
          await Task.CompletedTask;
      }

      protected override async Task OnAfterRenderAsync(bool firstRender)
      {
          // Decision D25: CompactInline initializes the JS module through scoped service.
          // Service guards prerender (only initializes when RendererInfo.IsInteractive) and
          // caches Lazy<Task<IJSObjectReference>> across component instances in the same circuit.
          if (firstRender && _effectiveMode == CommandRenderMode.CompactInline)
              await ExpandInRowJS.InitializeAsync(_compactCardRef);
      }

      // NOTE: NO DisposeAsync here — JS module is owned by the scoped IExpandInRowJSModule service.
      // NOTE: NO <EditForm> emission — inner {CommandName}Form owns validation and submit (ADR-016).
  }
  ```
- [ ] 4.4: Emit `BuildRenderTree` branches per `_effectiveMode` (use `#pragma warning disable ASP0006` for `seq++`). **The renderer NEVER emits `<EditForm>` (ADR-016).**
  - `Inline` + 0 fields:
    - Visible: `FluentButton @onclick=OnZeroFieldClickAsync` with leading icon + `{ButtonLabel}`
    - Hidden (display:none): `<{FullCommandTypeName}Form InitialValue="_prefilledModel" RegisterExternalSubmit="OnFormRegisteredExternalSubmit" />` — Form's `<EditForm>` wires but isn't visible; synthetic submit via registered callback
  - `Inline` + 1 field:
    - `FluentButton @ref=_triggerButtonRef` with `@onclick` toggling `_popoverOpen`; `aria-expanded=@_popoverOpen`
    - `<FluentPopover AnchorId="@_triggerButtonRef" Open="@_popoverOpen" @onkeydown=HandleEscape>` (outside-click dismissal via backdrop, Decision D29)
    - Inside popover: `<{FullCommandTypeName}Form InitialValue="_prefilledModel" ShowFieldsOnly='@(new[]{"{PropName}"})' OnConfirmed=ClosePopoverAndReturnFocus />`
  - `CompactInline`:
    - `<FluentCard class="fc-expand-in-row" @ref=_compactCardRef>` wrapping `<{FullCommandTypeName}Form InitialValue="_prefilledModel" DerivableFieldsHidden="true" />`
  - `FullPage`:
    - `<div style="max-width: @Options.FullPageFormMaxWidth; margin: 0 auto;">` (Decision D26) + optional `<FluentBreadcrumb>` + `<{FullCommandTypeName}Form InitialValue="_prefilledModel" OnConfirmed=NavigateToReturnPath />`
- [ ] 4.5: For `FullPage` mode, also emit a routable page partial `{BaseName}Page.g.razor.cs` (Decision D22):
  ```csharp
  [Route("/commands/{BoundedContext}/{BaseName}")]
  public partial class {BaseName}Page : ComponentBase
  {
      [Inject] private Fluxor.IDispatcher Dispatcher { get; set; } = default!;

      protected override void OnInitialized()
      {
          var viewKey = InferReturnViewKeyFromReferrer();
          if (viewKey is not null)
              Dispatcher.Dispatch(new RestoreGridStateAction(viewKey)); // no-op in v0.1 (Decision D30)
      }
      // Renders: <{BaseName}Renderer RenderMode="CommandRenderMode.FullPage" />
  }
  ```
- [ ] 4.6: Button hierarchy emission (Decision D12, D23, AC8) — labels are `"{Humanized BaseName}"` in ALL modes (no "Send" prefix):
  - Inline + 0 fields → `Appearance="Appearance.Secondary"` + leading icon
  - Inline + 1 field popover trigger → `Appearance="Appearance.Secondary"` + leading icon
  - Inline + 1 field popover submit (inside Form) → `Appearance="Appearance.Primary"` (Form already emits this; renderer does not override)
  - CompactInline submit (inside Form) → `Appearance="Appearance.Primary"` + leading icon
  - FullPage submit (inside Form) → `Appearance="Appearance.Primary"` + leading icon
  - **Note:** 2-1's `CommandFormEmitter` (Task 2.3) must be updated to compute `ButtonLabel` using the D23 rule `HumanizeCamelCase(BaseName)`; Story 2-1 snapshots that contained "Send Increment" will re-verify (see Task 5.2).
- [ ] 4.7: Icon emission: `<FluentIcon Value="@(new Icons.{IconName}())" />` where `IconName` is the `[Icon]` attribute value or default `Regular.Size16.Play` in all modes (Decision D23 — single default icon for label consistency, renderers differentiate via size/placement, not semantics). Escape the icon name via `EscapeString`.
- [ ] 4.8: Focus return & popover dismissal (AC9) — emit helpers:
  - `ClosePopoverAndReturnFocus()` → sets `_popoverOpen=false`, awaits `_triggerButtonRef.FocusAsync()`, then `await _triggerButtonRef.ScrollIntoViewAsync()` (extension method via JS interop)
  - `HandleEscape(KeyboardEventArgs)` → when `Escape` and `_popoverOpen`, invoke `ClosePopoverAndReturnFocus`
  - `NavigateToReturnPath(CommandResult)` → reads `ICommandPageContext.ReturnPath`; navigates via `NavigationManager.NavigateTo(...)`; accepts null (navigates to home route)
- [ ] 4.9: Create scoped service `IExpandInRowJSModule` in `Shell/Services/` (Decision D25):
  ```csharp
  public interface IExpandInRowJSModule
  {
      Task InitializeAsync(ElementReference element);
  }
  internal sealed class ExpandInRowJSModule : IExpandInRowJSModule, IAsyncDisposable
  {
      private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
      private readonly IJSRuntime _js;
      private readonly IComponentContext? _ctx; // or IHostEnvironment / RendererInfo check

      public ExpandInRowJSModule(IJSRuntime js) { _js = js; _moduleTask = new(() => ImportAsync()); }
      private Task<IJSObjectReference> ImportAsync()
          => _js.InvokeAsync<IJSObjectReference>("import", "./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js").AsTask();

      public async Task InitializeAsync(ElementReference element)
      {
          // Guard prerender: skip when JSRuntime is unavailable (SSR pass). Detect via IJSRuntime being IJSInProcessRuntime or via component's RendererInfo.IsInteractive.
          try
          {
              var module = await _moduleTask.Value;
              await module.InvokeVoidAsync("initializeExpandInRow", element);
          }
          catch (InvalidOperationException) { /* prerender — JSInterop not yet available */ }
      }
      public async ValueTask DisposeAsync()
      {
          if (_moduleTask.IsValueCreated)
          {
              try { var m = await _moduleTask.Value; await m.DisposeAsync(); } catch { }
          }
      }
  }
  ```
  Register as `services.AddScoped<IExpandInRowJSModule, ExpandInRowJSModule>()` in `AddHexalithFrontComposer()`.

### Task 5: Story 2-1 Form Body Extension (AC: 2, 3, 4) (ADR-016)

- [ ] 5.1: Extend Story 2-1's `{FullCommandTypeName}Form` component (modify `CommandFormEmitter`) with the following backward-compatible parameters:
  - `[Parameter] public bool DerivableFieldsHidden { get; set; } = false` — when true, skip rendering derivable field UI but retain bindings (values come from pre-fill)
  - `[Parameter] public string[]? ShowFieldsOnly { get; set; } = null` — when non-null, only render fields with property names in the set
  - `[Parameter] public {CommandTypeFqn}? InitialValue { get; set; }` — seeds `_model` on `OnInitialized` (already exists in 2-1 Task 3A.2 — verify)
  - `[Parameter] public EventCallback<CommandResult> OnConfirmed { get; set; }` — invoked after Form dispatches `ConfirmedAction`; allows renderer to close popover / navigate
  - `[Parameter] public Action<Action>? RegisterExternalSubmit { get; set; }` — Form invokes with `(() => _ = OnValidSubmitAsync())` during `OnAfterRender(firstRender=true)`; renderer stores the callback (ADR-016 rule 6, enables 0-field inline synthetic submit without a `<button type=submit>`)
  - Back-compat: defaults render all fields, no external integration (existing Story 2-1 behavior unchanged).
- [ ] 5.2: **Story 2-1 regression gate test** (new, addresses Murat's HIGH-risk concern):
  - Add test `CommandForm_Story21Regression_ByteIdenticalWhenDefaultParameters` in `SourceTools.Tests/Emitters/`
  - For every existing Story 2-1 `.verified.txt` snapshot (12 tests from Task 3E.1), run the updated emitter with a `CommandModel` identical to Story 2-1's input, assert byte-for-byte equality with the committed 2-1 snapshot
  - MUST run in CI; failure blocks merge
- [ ] 5.3: **Button label migration** — Decision D23 changes 2-1's button label from `"Send {Humanized CommandName}"` to `"{Humanized BaseName}"`. This is a visible change; re-approve Story 2-1's 12 snapshots with the new labels in a single pre-emitter-change commit so Task 5.2's regression gate passes against the new baselines. Document in `deferred-work.md`.
- [ ] 5.4: Add 2 new snapshot tests covering `DerivableFieldsHidden=true` and `ShowFieldsOnly=["Amount"]`:
  - `CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly` (snapshot)
  - `CommandForm_ShowFieldsOnly_RendersOnlyNamedFields` (snapshot)

### Task 6: DataGridNavigationState Fluxor Feature — REDUCER-ONLY Scope (AC: 7) (See ADR-015, Decision D30)

- [ ] 6.1: Create `Shell/State/DataGridNavigation/`:
  - `GridViewSnapshot.cs` — `sealed record GridViewSnapshot(double ScrollTop, ImmutableDictionary<string,string> Filters, string? SortColumn, bool SortDescending, string? ExpandedRowId, string? SelectedRowId, DateTimeOffset CapturedAt)`
  - `DataGridNavigationState.cs` — `sealed record DataGridNavigationState(ImmutableDictionary<string, GridViewSnapshot> ViewStates)` with initial state `ImmutableDictionary<string, GridViewSnapshot>.Empty`
  - `DataGridNavigationFeature.cs` — Fluxor `Feature<DataGridNavigationState>`, `GetName() => "Hexalith.FrontComposer.Shell.State.DataGridNavigationState"`
  - `DataGridNavigationActions.cs` — `CaptureGridStateAction(string viewKey, GridViewSnapshot snapshot)`, `RestoreGridStateAction(string viewKey)`, `ClearGridStateAction(string viewKey)`, `PruneExpiredAction(DateTimeOffset threshold)`
  - `DataGridNavigationReducers.cs` — handles each action; `PruneExpiredAction` removes snapshots where `CapturedAt < threshold`
- [ ] 6.2: **DEFERRED to Story 4.3 (Decision D30)** — `DataGridNavigationEffects.cs` (persistence + hydration + beforeunload). Story 2-2 ships reducers only. Add a stub comment in the folder marking effects as intentionally deferred.
- [ ] 6.3: Register feature in `AddHexalithFrontComposer()` via standard Fluxor assembly scanning (per Story 1-3 pattern).
- [ ] 6.4: Unit tests for feature — **exactly 9 tests** (trimmed from 12; effects-related tests move to Story 4.3):
  1. `CaptureGridStateAction` adds snapshot
  2. `CaptureGridStateAction` overwrites existing snapshot for same viewKey
  3. `RestoreGridStateAction` is a pure no-op reducer (state unchanged when viewKey missing; remains unchanged even when present — restore is read-side only)
  4. `ClearGridStateAction` removes snapshot
  5. `PruneExpiredAction` removes snapshots strictly before threshold
  6. `PruneExpiredAction` keeps snapshots at/after threshold
  7. Per-view isolation (two viewKeys coexist in state)
  8. IEquatable for `GridViewSnapshot` (hash consistency, structural equality)
  9. IEquatable for `DataGridNavigationState` (dictionary equality semantics)

### Task 7: fc-expandinrow JS Module (AC: 3) (See Decision D11)

- [ ] 7.1: Create `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-expandinrow.js`:
  ```javascript
  export function initializeExpandInRow(elementRef) {
      if (!elementRef) return;
      const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      elementRef.scrollIntoView({ block: 'nearest', behavior: reduceMotion ? 'auto' : 'smooth' });
      if (!reduceMotion) {
          requestAnimationFrame(() => {
              const rect = elementRef.getBoundingClientRect();
              if (rect.top < 0) {
                  window.scrollBy({ top: rect.top, behavior: 'smooth' });
              }
          });
      }
  }
  export function collapseExpandInRow(elementRef) {
      // Future: v2 multi-expand. No-op for v1.
  }
  ```
- [ ] 7.2: Verify `<StaticWebAssetsContent>` is enabled in `Hexalith.FrontComposer.Shell.csproj` (Razor Class Library template default — confirm).
- [ ] 7.3: Playwright smoke test (optional, in `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/`): load Counter sample CompactInline mode, trigger expand, assert viewport scroll occurred. Tag `[Trait("Category","E2E")]`; opt-in in CI.

### Task 8: Pipeline Wiring (AC: 5) (See Story 2-1 Decision D10, D12)

- [ ] 8.1: Update `FrontComposerGenerator.cs`:
  - Add `RegisterSourceOutput` for `{CommandName}CommandRenderer.g.razor.cs`
  - Add conditional `RegisterSourceOutput` for `{CommandName}CommandPage.g.razor.cs` when `Density == FullPage`
  - Ensure per-type caching preserved (ADR-012 from Story 2-1)
- [ ] 8.2: Integration test: `[Command]` with 0, 1, 2, 5 non-derivable fields drives correct emitter selection. 4 tests.
- [ ] 8.3: Integration test: `RenderMode` override (e.g., force `FullPage` on a 2-field command) compiles and renders. 1 test.

### Task 9: Counter Sample (AC: 10)

- [ ] 9.1: Add `BatchIncrementCommand` to `Counter.Domain`:
  ```csharp
  [Command]
  public class BatchIncrementCommand {
      public string MessageId { get; set; } = string.Empty;
      public string TenantId { get; set; } = string.Empty;
      public int Amount { get; set; } = 1;
      public string Note { get; set; } = string.Empty;
      public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.UtcNow;
  }
  ```
- [ ] 9.2: Add `ConfigureCounterCommand` to `Counter.Domain`:
  ```csharp
  [Command]
  [Icon("Regular.Size20.Settings")]
  public class ConfigureCounterCommand {
      public string MessageId { get; set; } = string.Empty;
      public string TenantId { get; set; } = string.Empty;
      public string Name { get; set; } = string.Empty;
      public string Description { get; set; } = string.Empty;
      public int InitialValue { get; set; }
      public int MaxValue { get; set; } = 100;
      public string Category { get; set; } = "General";
  }
  ```
- [ ] 9.3: Update `CounterPage.razor` to demonstrate all three modes in vertical layout, wrapping inline/compact renderers in a manual `<CascadingValue>` for `ProjectionContext` (Decision D27):
  ```razor
  @code {
      private ProjectionContext _demoContext = new(
          ProjectionTypeFqn: "Counter.Domain.CounterProjection",
          BoundedContext: "Counter",
          AggregateId: "counter-demo-1",
          Fields: new Dictionary<string, object?> { ["Count"] = 42 });
  }

  <CascadingValue Value="_demoContext">
      <section class="command-section">
          <BatchIncrementRenderer />  @* CompactInline *@
      </section>
      <section class="inline-section">
          <IncrementRenderer />  @* Inline + popover *@
      </section>
  </CascadingValue>

  <section class="data-section">
      <CounterProjectionView />
  </section>

  <FluentAnchor Href="/commands/Counter/ConfigureCounter" Appearance="Appearance.Hypertext">
      Configure Counter
  </FluentAnchor>
  ```
  Naming per Decision D22: renderer class names are `IncrementRenderer`, `BatchIncrementRenderer`, `ConfigureCounterRenderer` (trailing `Command` stripped).
- [ ] 9.4: Update `Counter.Web/Program.cs`:
  - Ensure `AddHexalithFrontComposer()` is called (registers new providers + feature)
  - Eagerly resolve emitted per-command subscribers once at circuit start (Task 4bis.2) — no additional wiring required
- [ ] 9.5: Update `Counter.Web/_Imports.razor` + `App.razor` to include `<Router>` that picks up the generated `{BaseName}Page` route (e.g., `/commands/Counter/ConfigureCounter`) — already present via default Blazor routing; verify.
- [ ] 9.6: Extend `CounterProjectionEffects.cs` (from Story 2-1 Task 7.3) to subscribe to both `BatchIncrementCommandActions.ConfirmedAction` and `ConfigureCounterCommandActions.ConfirmedAction` in addition to the existing `IncrementCommandActions.ConfirmedAction` — all three trigger `CounterProjectionActions.LoadRequestedAction`.

### Task 10: bUnit Test Coverage (AC: 2, 3, 4, 5, 8, 9)

**bUnit fixture rules (Murat — HIGH risk prevention):**
- Every test uses `cut.WaitForAssertion(() => ...)` for post-OnInitializedAsync pre-fill assertions. NO synchronous `cut.Find(...)` immediately after `RenderComponent`.
- For JS-interop tests: explicit `JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js")` stub, and assert the stub's `VerifyInvoke("initializeExpandInRow", 1)` at end of test. `JSRuntimeMode.Loose` is PROHIBITED for Task 10.2 specifically.
- Fluxor dispatch tests use `Fluxor.TestStore` with deterministic reducer execution; reducers are sync, effects are mocked.

- [ ] 10.1: `CommandRendererInlineTests.cs` — **exactly 9 tests**:
  1. `Renderer_ZeroFields_RendersSingleButton`
  2. `Renderer_ZeroFields_ClickInvokesRegisteredExternalSubmit` (verifies ADR-016 synthetic submit path)
  3. `Renderer_OneField_ClickOpensPopover` (asserts `aria-expanded=true`)
  4. `Renderer_OneField_EscapeClosesPopover`
  5. `Renderer_OneField_PopoverSubmit_InnerFormDispatchesSubmittedAction`
  6. `Renderer_Inline_UsesSecondaryAppearance`
  7. `Renderer_Inline_LeadingIconPresent`
  8. `Renderer_OneField_FocusReturnsToTriggerButtonOnPopoverSubmit` (AC9 focus return)
  9. `Renderer_OneField_FocusReturnsToTriggerButtonOnEscape` (AC9 focus return)
- [ ] 10.2: `CommandRendererCompactInlineTests.cs` — **exactly 7 tests**:
  1. `Renderer_CompactInline_RendersFluentCardWithExpandInRowClass`
  2. `Renderer_CompactInline_UsesPrimaryAppearanceOnInnerFormSubmit`
  3. `Renderer_CompactInline_DerivableFieldsHiddenParameterPropagatesToForm`
  4. `Renderer_CompactInline_InvokesJSModuleInitializeAsyncOnFirstRender` (with explicit `SetupModule` + `VerifyInvoke`)
  5. `Renderer_CompactInline_PrerenderDoesNotCallJSModule` (guards Decision D25 — skip when not interactive)
  6. `Renderer_CompactInline_EscapeInvokesOnCollapseRequested`
  7. `Renderer_CompactInline_PrefersReducedMotionHonored` (pass environment signal to module stub; assert parameter passthrough)
- [ ] 10.3: `CommandRendererFullPageTests.cs` — **exactly 7 tests**:
  1. `Renderer_FullPage_WrapsInFcShellOptionsMaxWidthContainer` (reads `FullPageFormMaxWidth` option)
  2. `Renderer_FullPage_RendersEmbeddedBreadcrumbWhenOptionOn`
  3. `Renderer_FullPage_DispatchesRestoreGridStateOnMount` (via `TestStore`; asserts action type and viewKey format)
  4. `Renderer_FullPage_NavigatesToReturnPathOnConfirmed`
  5. `Renderer_FullPage_GeneratedPageRegistersRoute`
  6. `Renderer_FullPage_UsesPrimaryAppearanceOnInnerFormSubmit`
  7. `Renderer_FullPage_LeadingIconPresent`
- [ ] 10.4: `RenderModeOverrideTests.cs` — **exactly 4 tests**:
  1. `Renderer_DefaultMode_MatchesDensityForZeroFields`
  2. `Renderer_DefaultMode_MatchesDensityForThreeFields`
  3. `Renderer_DefaultMode_MatchesDensityForSixFields`
  4. `Renderer_RenderModeOverride_LogsHFC1008OnMismatch` (verifies logger warning invoked with HFC1008 pattern)
- [ ] 10.5: `KeyboardTabOrderTests.cs` — **exactly 3 tests** (Murat — axe covers DOM, not keyboard traversal):
  1. `Inline_1Field_TabCyclesTriggerPopoverFieldSubmitCancel` (verifies the tab journey)
  2. `CompactInline_TabOrder_MatchesStory21FieldOrder`
  3. `FullPage_TabOrder_SkipLinkThenBreadcrumbThenForm`
- [ ] 10.6: `DerivedValueProviderChainTests.cs` — covered in Task 3.9.
- [ ] 10.7: `DataGridNavigationReducerTests.cs` — covered in Task 6.4.
- [ ] 10.8: `LastUsedSubscriberEmitterTests.cs` — covered in Task 4bis.3.

### Task 11: Emitter Snapshot, Parseability, Determinism & 2-1 Contract (AC: 5, 8)

- [ ] 11.1: `.verified.txt` snapshot tests in `SourceTools.Tests/Emitters/` — **exactly 8 tests**:
  1. Command with 0 non-derivable fields → Inline renderer snapshot
  2. Command with 1 non-derivable field → Inline+popover renderer snapshot
  3. Command with 2 non-derivable fields → CompactInline renderer snapshot
  4. Command with 4 non-derivable fields → CompactInline renderer snapshot (boundary)
  5. Command with 5 non-derivable fields → FullPage renderer + page snapshot (boundary)
  6. Command with `[Icon]` attribute → icon emission snapshot
  7. Command without `[Icon]` → default icon snapshot
  8. Density boundary parity: render 0/1/2/5 field commands with identical other shape; diff-only-in-mode assertion (Decision D17)
- [ ] 11.2: Parseability test (**exactly 1 test**): every emitted `.g.razor.cs` and `.g.cs` parses via `CSharpSyntaxTree.ParseText()` with zero errors. Reuse Story 2-1 test infrastructure.
- [ ] 11.3: Determinism test (**exactly 1 test**): run emitter twice on identical `CommandModel`, assert byte-identical output for renderer and page artifacts.
- [ ] 11.4: **2-1↔2-2 Contract test** (**exactly 1 test** — Murat MED risk): `CommandForm_RendererDelegation_FormBodyStructurallyIdentical`. For a 3-field command, emit (a) Story 2-1's Form with defaults and (b) the form as it would render inside a CompactInline renderer (with `DerivableFieldsHidden=false, ShowFieldsOnly=null`). Assert the rendered BuildRenderTree is structurally identical (ignoring whitespace). Guards against renderer silently changing Form contract.

### Task 12: Accessibility & Axe-Core (AC: 9)

- [ ] 12.1: bUnit + axe-core integration tests — **exactly 3 tests** (one per mode):
  - `AxeCore_InlineRenderer_NoSeriousOrCriticalViolations` (IncrementRenderer)
  - `AxeCore_CompactInlineRenderer_NoSeriousOrCriticalViolations` (BatchIncrementRenderer)
  - `AxeCore_FullPageRenderer_NoSeriousOrCriticalViolations` (ConfigureCounterRenderer)
  - Each test runs `axe.run()` via JSInterop mock; fails on any serious or critical violation
  - Keyboard tab-order coverage is handled separately in Task 10.5 (axe scans DOM, not keyboard traversal)
- [ ] 12.2: Manual keyboard walk-through documented in story Completion Notes:
  - Tab order, Enter/Space activation, Escape handling, aria-expanded correctness, focus return

### Task 13: Final Integration & QA (AC: all)

- [ ] 13.1: Run full test suite. **Expected new test count: 73** — reconciled math (Murat review):

  | Task | Tests | Task | Tests |
  |---|---:|---|---:|
  | 1.4 density (FsCheck + snapshots) | 4 | 10.2 CompactInline bUnit | 7 |
  | 3.9 provider chain | 16 | 10.3 FullPage bUnit | 7 |
  | 4bis.3 LastUsed subscriber emitter | 5 | 10.4 RenderMode override | 4 |
  | 5.2 Story 2-1 regression gate | 12 (one per existing 2-1 snapshot) | 10.5 Keyboard tab order | 3 |
  | 5.4 new Form-parameter snapshots | 2 | 11.1 Renderer snapshots | 8 |
  | 6.4 DataGridNav reducers | 9 | 11.2 parseability | 1 |
  | 8.2 emitter selection integration | 4 | 11.3 determinism | 1 |
  | 8.3 RenderMode integration | 1 | 11.4 2-1↔2-2 contract | 1 |
  | 10.1 Inline bUnit | 9 | 12.1 axe-core per mode | 3 |

  **Total:** 4+16+5+12+2+9+4+1+9+7+7+4+3+8+1+1+1+3 = **97**. Story 2-1 delivered ~342 so cumulative target is ~439. The earlier "70" figure undercounted integration, regression, and axe buckets — this revised count is authoritative.
- [ ] 13.2: `dotnet build --configuration Release` succeeds with zero warnings (`TreatWarningsAsErrors=true`).
- [ ] 13.3: Manual smoke on Counter.Web via `dotnet watch`:
  - All three commands render in their correct modes
  - IncrementCommand popover opens/submits correctly with the 1 non-derivable field
  - BatchIncrementCommand compact form pre-fills Timestamp and MessageId; user supplies Amount, Note, EffectiveDate
  - Navigate to `/commands/Counter/ConfigureCounterCommand`, submit, land back on CounterPage; DataGrid state preserved (manually exercise scroll + filter before navigating away)
  - Hot reload: modify `BatchIncrementCommand` to add a 4th non-derivable field, verify renderer still CompactInline. Add a 5th, verify it becomes FullPage + a route appears.
- [ ] 13.4: Dev-agent automated validation via Aspire MCP (`mcp__aspire__list_resources`, `list_console_logs`) + Claude browser (`mcp__claude-in-chrome__navigate`, `find`, `read_page`) to exercise Counter sample flow. Record screenshots or DOM snapshots as evidence in Completion Notes. **No human validation required.**
  > **Transparency note (Murat):** This is dev-agent local automated validation, NOT headless-CI automated. The path exercises the Aspire topology and the browser end-to-end from the dev agent's session. CI coverage is limited to unit + bUnit + snapshot; end-to-end Playwright-in-CI is Story 7.x / Epic 10 scope. Do not claim "CI-automated E2E" on this story's report.
- [ ] 13.5: axe-core scan on all three Counter modes — zero serious/critical violations.
- [ ] 13.6: Update `deferred-work.md`:
  - Note: HFC1008 analyzer emission is deferred to Epic 9 (runtime warning only in Story 2-2).
  - Note: Destructive command Danger handling deferred to Story 2-5.
  - Note: Form abandonment 30s warning deferred to Story 2-5.
  - Note: `DataGridNavigationState` capture-side wiring (from DataGrid row/filter changes) deferred to Epic 4.

---

## Dev Notes

### What Story 2-1 Delivered (REUSE — Do NOT Reinvent)

- `CommandModel`, `FormFieldModel`, `CommandFormModel` IR (sealed classes, manual `IEquatable`, ADR-009)
- `AttributeParser.ParseCommand` — derivable/non-derivable classification (keys: `MessageId`, `CommandId`, `CorrelationId`, `TenantId`, `UserId`, `Timestamp`, `CreatedAt`, `ModifiedAt`, `[DerivedFrom]`)
- `CommandFormEmitter` — `{CommandName}Form.g.razor.cs` with full 5-state lifecycle wiring (ADR-010 callback pattern), `IStringLocalizer<T>` runtime resolution (Decision D7), numeric string-backing converter (Task 3B.2), `FcFieldPlaceholder` for unsupported types
- `{CommandName}LifecycleState` + `{CommandName}Actions` + `{CommandName}Reducers` per Task 4 (Story 2-1)
- `StubCommandService` with configurable delays (`AcknowledgeDelayMs`, `SyncingDelayMs`, `ConfirmDelayMs`) — keep using for Story 2-2 sample
- `CommandRejectedException` in `Contracts/Communication/`
- `DerivedFromAttribute` in `Contracts/Attributes/`
- `ICommandService` contract with lifecycle callback (ADR-010)
- `CorrelationId` on all generated Fluxor actions (ADR-008 gap resolved in Story 2-1 Task 0.5)

### Pitfalls to Avoid (from Story 2-1 Completion Notes & Epic 1 Intelligence)

- DO NOT re-emit lifecycle visual feedback in the renderer — that is Story 2-1 (progress ring in button) and Story 2-4 (sync pulse, timeouts). Story 2-2 owns layout only. (Decision D19)
- DO NOT introduce a second `ICommandService` or shadow the existing one — extend via new parameters only.
- DO NOT reference FluentUI v4 APIs — Story 2-1's breaking change table applies (FluentTextField→FluentTextInput, FluentCheckbox→FluentSwitch, etc.)
- DO NOT use `record` for IR types (Decision D1 from Story 2-1 carries — sealed classes with manual equality).
- DO NOT call `services.AddFluxor()` twice when registering the new `DataGridNavigationFeature` — Fluxor assembly scanning picks it up automatically.
- DO NOT log `_model` in any component (Decision D15 from Story 2-1) — log CorrelationId and property names only.
- DO NOT reuse HFC1003 (Projection partial warning) for new diagnostics — use HFC1008 (Decision 0.5).
- DO NOT dispatch Fluxor actions from the `StubCommandService` or any `IDerivedValueProvider` — only components dispatch (Decision D5 from Story 2-1).
- DO NOT use `GetTypes()` — use `GetExportedTypes()` when reflecting on assemblies (Epic 1 intel).
- DO NOT create a second `EquatableArray<T>` — reuse the existing one.
- DO NOT modify `ICommandService` further — Story 2-1's contract is stable for this story.

### Architecture Constraints (MUST FOLLOW)

1. **SourceTools (`netstandard2.0`) must NEVER reference Fluxor, FluentUI, or Shell.** All external types emitted as fully-qualified name strings. (Architecture §246 + carries from Story 2-1.)
2. **Three-stage pipeline:** Parse → Transform → Emit. Stage purity preserved. (ADR-004)
3. **IEquatable<T> on all new IR types** via sealed-class pattern. Density + IconName participate in equality. (ADR-009, Decision D3)
4. **Deterministic output** — same `CommandModel` must produce byte-identical renderer + page files.
5. **Hint names namespace-qualified:** `{Namespace}.{CommandName}CommandRenderer.g.razor.cs` / `{Namespace}.{CommandName}CommandPage.g.razor.cs`.
6. **Fluxor naming for new feature:** `DataGridNavigationFeature.GetName() => "Hexalith.FrontComposer.Shell.State.DataGridNavigationState"` (Decision D14 from Story 2-1 pattern — fully qualified).
7. **Per-concern Fluxor feature** for DataGrid navigation state (Architecture D7, ADR-015).
8. **Per-type incremental caching** preserved (ADR-012 carries — per-command registration, no `Collect()`-based aggregation).
9. **AnalyzerReleases.Unshipped.md** updated for HFC1008 (RS2008).
10. **`#pragma warning disable ASP0006`** around `BuildRenderTree` `seq++` in emitted code (Story 2-1 rule #11).
11. **All generated files** end in `.g.cs` or `.g.razor.cs`, live in `obj/` not `src/`.
12. **JS module path** uses static web assets: `./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js`. Shell is a Razor Class Library — static assets are auto-mounted at consumer sites.
13. **`IStorageService` TTL semantics** — snapshots + last-used values respect `IStorageService` eviction. Do NOT cache outside IStorageService.

### DI Scope Contract

| Service | Lifetime | Rationale |
|---|---|---|
| `IEnumerable<IDerivedValueProvider>` | Scoped (each provider) | Per-circuit in Server, per-user in WASM |
| `SystemValueProvider` | Scoped | Reads per-request HttpContext |
| `ProjectionContextProvider` | Scoped | Reads per-circuit cascading parameter |
| `LastUsedValueProvider` | Scoped | Per-user storage scoping |
| `DefaultValueProvider` | Singleton | Pure reflection, no state |
| `DataGridNavigationFeature` | Scoped (Fluxor default) | Per-circuit state |
| `ICommandPageContext` | Scoped | Per-page lifecycle |

### Existing Code to REUSE (Do NOT Reinvent)

- `FieldTypeMapper` — Story 2-1
- `PropertyModel`, `CommandModel`, `FormFieldModel` — Story 2-1 (extend `CommandModel` with `Density` + `IconName`)
- `CamelCaseHumanizer` — for labels
- `EquatableArray<T>` — reuse for `NonDerivablePropertyNames`
- `ICommandService` + `CommandResult` + `CommandLifecycleState` — stable from Story 2-1; DO NOT change contract
- `{CommandName}Form.g.razor.cs` — Story 2-1's emitted form body is delegated to by all three rendering modes via the new `DerivableFieldsHidden` / `ShowFieldsOnly` parameters (Task 5.1)
- `EscapeString` helper (Story 1-5) — mandatory for IconName, BoundedContext, route path emission
- `IStorageService` 5-method contract (Story 1-1) — for `LastUsedValueProvider` and `DataGridNavigationEffects`
- `Fluxor.IActionSubscriber` — for `LastUsedValueProvider` Confirmed subscription (per-command, emitted in Task 4.3)
- Assembly-scanning Fluxor registration (Story 1-3 + 1-6) — new `DataGridNavigationFeature` discovered automatically
- `ICommandLifecycleTracker` — NOT needed for this story
- `StubCommandService` — Story 2-1 stub is the transport for sample validation

### Files That Must Be Created

**Contracts:**
- `Attributes/IconAttribute.cs`
- `Rendering/CommandRenderMode.cs`
- `Rendering/ICommandPageContext.cs`
- `Rendering/ProjectionContext.cs`
- `Rendering/IDerivedValueProvider.cs` + `DerivedValueResult.cs`
- `Rendering/FcShellOptions.cs` (if absent)

**SourceTools/Parsing:**
- Extend `DomainModel.cs`: add `CommandDensity` enum, `Density` + `IconName` on `CommandModel`
- Extend `AttributeParser.ParseCommand`: resolve `[Icon]`, compute density

**SourceTools/Transforms:**
- `CommandRendererTransform.cs`
- `CommandRendererModel.cs` (sealed class, manual IEquatable)

**SourceTools/Emitters:**
- `CommandRendererEmitter.cs` — emits `{BaseName}Renderer.g.razor.cs`
- `CommandPageEmitter.cs` — emits `{BaseName}Page.g.razor.cs` (only when `Density == FullPage`)
- `LastUsedSubscriberEmitter.cs` — emits `{BaseName}LastUsedSubscriber.g.cs` per command (Decision D28)

**SourceTools/Diagnostics:**
- Extend `DiagnosticDescriptors.cs`: add HFC1008 (runtime warning for MVP; analyzer reporting deferred to Epic 9)
- Update `AnalyzerReleases.Unshipped.md`

**Shell/Services/DerivedValues/:**
- `SystemValueProvider.cs` (Scoped — registered 1st)
- `ProjectionContextProvider.cs` (Scoped — 2nd)
- `ExplicitDefaultValueProvider.cs` (Singleton — 3rd, reads `System.ComponentModel.DefaultValueAttribute`)
- `LastUsedValueProvider.cs` (Scoped — 4th; typed `Record<TCommand>` API, no self-subscription)
- `ConstructorDefaultValueProvider.cs` (Singleton — 5th/final)
- Extension `AddDerivedValueProvider<T>(ServiceLifetime)` in `Shell/Extensions/ServiceCollectionExtensions.cs`

**Shell/Services/:**
- `IExpandInRowJSModule.cs` + `ExpandInRowJSModule.cs` (Scoped, `Lazy<Task<IJSObjectReference>>` cache per Decision D25)

**Shell/State/DataGridNavigation/:**
- `GridViewSnapshot.cs`
- `DataGridNavigationState.cs`
- `DataGridNavigationFeature.cs`
- `DataGridNavigationActions.cs`
- `DataGridNavigationReducers.cs`
- `DataGridNavigationEffects.cs`

**Shell/wwwroot/js/:**
- `fc-expandinrow.js`

**Shell/Extensions/:**
- Modify `ServiceCollectionExtensions.cs` — register new providers + feature in `AddHexalithFrontComposer()`

**Story 2-1 modifications (backward-compatible except button label re-approval, Decision D23):**
- `CommandFormEmitter.cs` — add `DerivableFieldsHidden`, `ShowFieldsOnly`, `OnConfirmed`, `RegisterExternalSubmit` parameters (ADR-016)
- `CommandFormEmitter.cs` — update button-label computation to `HumanizeCamelCase(BaseName)` (Decision D23) — requires re-approving Story 2-1's 12 `.verified.txt` snapshots in the same commit (Task 5.3)
- Add regression gate test `CommandForm_Story21Regression_ByteIdenticalWhenDefaultParameters` (Task 5.2)

**samples/Counter/Counter.Domain/:**
- `BatchIncrementCommand.cs`
- `ConfigureCounterCommand.cs`

**samples/Counter/Counter.Web/Pages/:**
- Update `CounterPage.razor` to demonstrate all three modes

**tests/Hexalith.FrontComposer.SourceTools.Tests/:**
- `Transforms/CommandRendererTransformTests.cs`
- `Emitters/CommandRendererEmitterTests.cs` (+ `.verified.txt` files)
- `Parsing/CommandDensityTests.cs`

**tests/Hexalith.FrontComposer.Shell.Tests/:**
- `Components/CommandRendererInlineTests.cs`
- `Components/CommandRendererCompactInlineTests.cs`
- `Components/CommandRendererFullPageTests.cs`
- `Components/RenderModeOverrideTests.cs`
- `Services/DerivedValueProviderChainTests.cs`
- `State/DataGridNavigationReducerTests.cs`
- `State/DataGridNavigationEffectsTests.cs`

### Naming Convention Reference

| Element | Pattern | Example |
|---------|---------|---------|
| Generated renderer partial | `{BaseName}Renderer.g.razor.cs` (trailing `Command` stripped, Decision D22) | `IncrementRenderer.g.razor.cs`, `ConfigureCounterRenderer.g.razor.cs` |
| Generated page partial | `{BaseName}Page.g.razor.cs` | `ConfigureCounterPage.g.razor.cs` |
| Generated LastUsed subscriber | `{BaseName}LastUsedSubscriber.g.cs` | `IncrementLastUsedSubscriber.g.cs` |
| Generated form partial (Story 2-1, unchanged naming) | `{FullCommandTypeName}Form.g.razor.cs` | `IncrementCommandForm.g.razor.cs` |
| Full-page route | `/commands/{BoundedContext}/{BaseName}` | `/commands/Counter/ConfigureCounter` |
| Fluxor feature `GetName()` | `"Hexalith.FrontComposer.Shell.State.DataGridNavigationState"` | (same) |
| Grid view key | `"{commandBoundedContext}:{projectionTypeFqn}"` | `"Counter:Counter.Domain.CounterProjection"` |
| LastUsed storage key | `frontcomposer:lastused:{tenantId}:{userId}:{commandTypeFqn}:{propertyName}` | `frontcomposer:lastused:default:anon:Counter.Domain.IncrementCommand:Amount` |
| Grid nav storage key (deferred to Story 4.3) | `frontcomposer:gridnav:{tenantId}:{userId}` | `frontcomposer:gridnav:default:anon` |
| Button label (ALL modes, Decision D23) | `"{Humanized BaseName}"` | `"Increment"`, `"Batch Increment"`, `"Configure Counter"` |
| Icon default (ALL modes, Decision D23) | `Regular.Size16.Play` unless `[Icon]` overrides | (same) |
| FullPage max-width option | `FcShellOptions.FullPageFormMaxWidth` (default `"720px"`) | (same) |

### Testing Standards

- xUnit v3 (3.2.2), Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2, FsCheck.Xunit.v3 (all from Story 2-1)
- Parse/Transform: pure function tests
- Emit: snapshot/golden-file (`.verified.txt`) — boundary parity tests cover 0/1/2/4/5 field commands (Decision D17)
- bUnit: rendered component tests per mode; **`JSRuntimeMode.Loose` is PROHIBITED for Task 10.2** — use explicit `JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js")` and assert `VerifyInvoke("initializeExpandInRow", 1)` (Murat HIGH-risk defense)
- Post-OnInitializedAsync assertions MUST use `cut.WaitForAssertion(...)`, NEVER synchronous `cut.Find(...)` immediately after render (pre-fill race guard)
- `TestContext.Current.CancellationToken` on all `RunGenerators`/`GetDiagnostics`/`ParseText` (xUnit1051)
- `TreatWarningsAsErrors=true` global
- `DiffEngine_Disabled: true` in CI
- **Story 2-1 regression gate** (Task 5.2): 12 byte-identical snapshot assertions prevent silent contract breaks
- **2-1↔2-2 contract test** (Task 11.4): structural equality between Story 2-1 Form defaults and CompactInline renderer-delegated Form
- **Expected new test count: 97.** Target cumulative total: ~439.
- **Post-story (optional)**: Stryker.NET mutation run against the 3-mode branch logic in renderer. Nightly-tier, not blocking.

### Build & CI

- Build race CS2012: `dotnet build` then `dotnet test --no-build`
- `AnalyzerReleases.Unshipped.md` update for HFC1008
- Roslyn 4.12.0 pinned
- ASP0006 suppression in emitted `BuildRenderTree` via `#pragma warning disable ASP0006`
- Static web asset manifest: Shell is already `<StaticWebAssets>` enabled; verify `wwwroot/js/fc-expandinrow.js` included in build output

### Previous Story Intelligence

**From Story 2-1 (same epic, immediate predecessor):**

- **Patterns that worked:** sealed-class IR with manual IEquatable; three-stage pipeline; namespace-qualified hint names; label resolution chain; per-type incremental caching (ADR-012); form component owning Fluxor dispatch (ADR-010); `StubCommandService` with configurable delays and cancellation.
- **Lifecycle flow** (Story 2-1): form → `Dispatcher.Dispatch(Submitted)` → `await CommandService.DispatchAsync(model, onLifecycleChange, ct)` → stub simulates ack → form dispatches `Acknowledged` → stub callback fires `Syncing` → form dispatches `Syncing` → stub callback fires `Confirmed` → form dispatches `Confirmed`. **Story 2-2 renderers delegate to the Story 2-1 form body; they do not reimplement the lifecycle dispatch.**
- **Pre-mortem defenses:** `IOptionsSnapshot<StubCommandServiceOptions>` registered as Scoped (never Singleton); null-safe `IStringLocalizer` resolution; `CancellationToken` propagation to stub delay tasks.
- **Red team defenses:** `EscapeString` on all emitted string literals; name collision detection with `System.*` rejected.
- **Fluent UI v5 breaking changes table** (from Story 2-1 Dev Notes) — reuse as-is; no new FluentUI APIs introduced in this story beyond `FluentPopover`, `FluentBreadcrumb`, `FluentCard`, `FluentAnchor`, `FluentIcon` (all verified v5).
- **Counter sample wiring pattern** (Story 2-1 Task 7.3): `CounterProjectionEffects.cs` listens for `{Command}Actions.ConfirmedAction` and dispatches `CounterProjectionActions.LoadRequestedAction` to re-query after a simulated SignalR catch-up. Story 2-2 extends by adding the two new commands but **does not duplicate the effect pattern** — extend the existing `CounterProjectionEffects.cs` to subscribe to `BatchIncrementCommandActions.ConfirmedAction` and `ConfigureCounterCommandActions.ConfirmedAction`.

**From Epic 1:**
- Hot reload: attribute additions (e.g., `[Icon]`) may require a full restart (Story 1-8 contingency). Document in Completion Notes.
- Single Fluxor scan: use assembly-scanning registration (Story 1-3); do not call `AddFluxor` twice.
- MessageId missing diagnostic: HFC1006 emits in Story 2-1 — Story 2-2's new Counter commands all declare MessageId; no new HFC1006 emissions expected.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.2 — AC source of truth and FR/UX-DR mapping]
- [Source: _bmad-output/planning-artifacts/epics.md#FR8 — density rules functional requirement]
- [Source: _bmad-output/planning-artifacts/architecture.md#Per-Concern Fluxor Features — D7 pattern compliance]
- [Source: _bmad-output/planning-artifacts/architecture.md#FR Category → Architecture Mapping — composition shell location]
- [Source: _bmad-output/planning-artifacts/architecture.md#Services ServiceCollectionExtensions — AddHexalithFrontComposer() entry]
- [Source: _bmad-output/planning-artifacts/architecture.md#State / DataGrid — DataGridNavigationState placement precedent]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Non-derivable field definition — classification semantics]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Interaction flow for the three command form patterns — mode behavior spec §1193-1199]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Expand-in-row scroll stabilization — §1201-1207 JS contract]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#DataGrid state preservation across full-page form navigation — §1209-1215]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Button Hierarchy — §2217-2242]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Confirmation Patterns — §2305 (delegate rules to Story 2-5)]
- [Source: _bmad-output/implementation-artifacts/2-1-command-form-generation-and-field-type-inference.md — CommandModel IR, ADR-009 through ADR-012, CommandFormEmitter, StubCommandService, DerivedFromAttribute, ICommandService callback contract]
- [Source: this file — ADR-013 (density at generation time), ADR-014 (provider chain), ADR-015 (DataGridNav feature), ADR-016 (renderer/form chrome-vs-core contract)]
- [Source: _bmad-output/implementation-artifacts/1-3-fluxor-state-management-foundation.md — Fluxor assembly-scanning, per-concern feature pattern, `IActionSubscriber` conventions]
- [Source: _bmad-output/implementation-artifacts/1-5-source-generator-transform-and-emit-stages.md — Transform/Emit patterns, EscapeString helper, namespace-qualified hint names]
- [Source: _bmad-output/implementation-artifacts/1-6-counter-sample-domain-and-aspire-topology.md — Counter.Domain layout, Counter.Web wiring]
- [Source: _bmad-output/implementation-artifacts/1-8-hot-reload-and-fluent-ui-contingency.md — Fluent UI v5 RC2, hot-reload limitations for attribute additions]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md — tracks cross-story deferrals]
- [Source: Fluent UI Blazor v5 MCP documentation — `FluentPopover`, `FluentBreadcrumb`, `FluentCard`, `FluentIcon`, `FluentAnchor` API shapes]
- [Source: UX spec §2186 — responsive density breakpoint <1024px (forward-compatibility; v0.1 assumes desktop)]

### Project Structure Notes

- Alignment with unified project structure (Architecture §852 directory blueprint):
  - New `Shell/Services/DerivedValues/` folder — first service-type grouping under `Shell/Services/`; matches precedent of `Shell/State/Navigation/` grouping.
  - New `Shell/State/DataGridNavigation/` folder — consistent with existing per-concern Fluxor feature folders (`Theme/`, `Density/`, `Navigation/`, `DataGrid/`, `ETagCache/`, `CommandLifecycle/`).
  - `Shell/wwwroot/js/fc-expandinrow.js` — first JS module for the project; coexists with planned `Shell/wwwroot/js/beforeunload.js` (Story 1-1/5.x).
- Detected conflicts or variances: none. All additions extend existing architecture patterns without contradiction.

---

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
