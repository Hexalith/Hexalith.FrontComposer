# Acceptance Criteria

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
**And** the button label is `{DisplayLabel}` (Decision D23 — display-only trailing " Command" strip, no `"Send "` prefix in ANY mode; class/hint/route names remain full `{CommandTypeName}` per D22)
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

**And** opening a popover on one renderer closes any other open Inline popover in the same circuit first (Decision D37 — at-most-one constraint); no transitional state where two popovers are simultaneously open

**And** the 0-field Inline button is disabled (`[disabled]`) until the inner Form invokes `RegisterExternalSubmit` on first render; this prevents silent click-drop during SSR-to-interactive transition (Decision D36)

**And** when the popover form is shown AND `LastUsedValueProvider` reports no stored entry for this `(tenantId, userId, commandType)` triple (first-session case — Sally Journey 1), a subtle muted-caption line renders below the single field: `"Your last value will be remembered after your first submission."` Rendered only when pre-fill chain returned no `HasValue=true` result AND the field has no `[DefaultValue]`. Removes the "why didn't it remember me?" antipattern. Styling: `<FluentBodyText Typo="Typography.Caption" Color="Color.Neutral">`.

### AC3: Compact Inline Form Mode (2-4 Non-Derivable Fields)

**Given** a `[Command]` with 2-4 non-derivable fields
**When** its generated renderer is placed with `RenderMode.CompactInline` (default for this density)
**Then** it renders a `FluentCard` with CSS class `fc-expand-in-row`
**And** the card contains the Story 2-1 form body (non-derivable fields only; derivable fields are pre-filled and hidden)
**And** the submit button uses `Appearance.Primary` (new visual context per UX spec §2229)
**And** on `OnAfterRenderAsync(firstRender=true)`, `fc-expandinrow.js::initializeExpandInRow` is invoked via `IJSRuntime` (Decision D11)
**And** when `prefers-reduced-motion` is set, the JS module skips the `requestAnimationFrame` smoothing and only calls `scrollIntoView({block:'nearest'})`
**And** the form is consumable standalone (placed below any content) for Story 2-2 scope; DataGrid row integration lands in Story 4.5

**And** Story 2-2 does NOT constrain multiple CompactInline renderers on the same page — the UX-spec invariant "one row expanded at a time (v1)" is the **DataGrid container's responsibility** (enforced in Story 4.5). Standalone CompactInline placement (e.g., the Counter sample `BatchIncrementCommandRenderer`) is legitimately unconstrained in 2-2 and may coexist with other CompactInline instances without enforcement.

### AC4: Full-Page Form Mode (5+ Non-Derivable Fields)

**Given** a `[Command]` with 5+ non-derivable fields
**When** its generated `{CommandName}CommandRenderer` is placed as a page component
**Then** a Blazor route `@page "/commands/{BoundedContext}/{CommandTypeName}"` is emitted (Decision D5, D22 — full TypeName, no stripping)
**And** the page renders the Story 2-1 form body wrapped in `<div style="max-width: @FcShellOptions.FullPageFormMaxWidth; margin: 0 auto;">` (reuses Story 2-1 AC3 layout; width from options per Decision D26)
**And** an embedded `FluentBreadcrumb` displays "{BoundedContext} > {DisplayLabel}" when `FcShellOptions.EmbeddedBreadcrumb=true` (Decision D15, D23, default true)
**And** on mount, `RestoreGridStateAction` is dispatched with the referring bounded-context + projection (from `NavigationManager.Uri` parsing) — no-op if state is empty (Decision D10)
**And** on successful `Confirmed`, the renderer navigates to `ReturnPath` (from `ICommandPageContext.ReturnPath`) or the home route if unset

**And** `ReturnPath` is validated via `Uri.IsWellFormedUriString(path, UriKind.Relative) && !path.StartsWith("//")` before navigation (Decision D32); on failure, navigate to `/` and log `ILogger.LogError` with `CorrelationId` (open-redirect CVE defense)

### AC5: Density-Driven Renderer Selection

**Given** a generated `{CommandName}CommandRenderer.g.razor.cs`
**When** the `RenderMode` parameter is unset
**Then** the renderer uses the density-derived default (`Density.Inline` → `CommandRenderMode.Inline`, etc.)

**When** `RenderMode` is explicitly set
**Then** the specified mode is rendered, overriding the default
**And** if the specified mode is incompatible with the density (e.g., `CommandRenderMode.Inline` on a 5-field command), a compile-time warning `HFC1015` (NEW — renumbered from originally-proposed HFC1008 which collides with Story 2-1's Flags-enum diagnostic) is emitted at the consumption site via analyzer reporting — or runtime warning log if not statically detectable. MVP scope: runtime `ILogger` warning only; analyzer reporting is deferred to Epic 9.

### AC6: Derivable Field Pre-Fill via IDerivedValueProvider Chain

**Given** `IDerivedValueProvider` is registered with built-in providers in this exact order (Decision D24): `SystemValueProvider` → `ProjectionContextProvider` → `ExplicitDefaultValueProvider` → `LastUsedValueProvider` → `ConstructorDefaultValueProvider`
**When** a renderer initializes (compact/full-page) or is about to submit (inline 0-field)
**Then** each derivable field is resolved by iterating the provider chain and taking the first `HasValue=true`
**And** `SystemValueProvider` handles `MessageId`, `CorrelationId`, `Timestamp`, `UserId`, `TenantId`, `CreatedAt`, `ModifiedAt` (per Story 2-1 Task 1.3 keys)
**And** `ProjectionContextProvider` reads from `[CascadingParameter] public ProjectionContext? ProjectionContext { get; set; }` on the renderer (null-tolerant per Decision D27; cascade source is Epic 4 DataGrid row or Counter sample manual `<CascadingValue>`)
**And** `ExplicitDefaultValueProvider` returns `HasValue=true` ONLY when the property has a `[DefaultValue(x)]` attribute — uses that attribute's value (Decision D24, protects reset-semantics fields)
**And** `LastUsedValueProvider` reads from `IStorageService` under key `frontcomposer:lastused:{tenantId}:{userId}:{commandTypeFqn}:{propertyName}` (Decision D8)
**And** `LastUsedValueProvider` writes to that key via a per-command emitted `{CommandTypeName}LastUsedSubscriber.g.cs` (Decision D28) that subscribes to `{CommandName}Actions.ConfirmedAction` via `Fluxor.IActionSubscriber.SubscribeToAction<...>` and calls a typed `LastUsedValueProvider.Record<TCommand>(command)` — no reflection dispatch
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
- FullPage mode: `Tab` skips to content via skip-link; breadcrumb is focusable; form has `aria-label="{DisplayLabel} command form"` (Decision D23)

**And** `aria-expanded` is set correctly on the inline button when a popover is open

**Focus return contract (per Sally's review — binding):**
- **Popover submit (success):** On `Confirmed` state transition, the trigger button is first scrolled into view via `scrollIntoView({block:'nearest'})` (handles the case where `Confirmed` arrives ~2s later and the user has scrolled — Hindsight #3), THEN focus is restored via `ElementReference.FocusAsync()`. Both operations must complete in order — scroll-then-focus, never focus-then-scroll.
- **Popover submit (rejected):** Focus returns to the FIRST invalid field in the popover; popover remains open
- **Popover dismiss (Escape or outside-click):** Focus returns to the trigger button; no form submission occurs; row remains scrolled into view (same scroll-then-focus order)
- **Circuit reconnect mid-popover (Pre-mortem PM-2, Chaos CM-6):** On `CircuitHandler.OnConnectionUpAsync` after a disconnect, if `_popoverOpen == true`, the popover is closed silently, `_popoverOpen = false`, and a warning is logged: `Logger?.LogWarning("Popover state lost on circuit reconnect for {CommandType}. Full draft preservation is Story 2-5 scope.", "{CommandTypeFqn}")`. Full draft preservation is 2-5's concern; Story 2-2 ensures the fail-closed path doesn't leak ghost state or raise exceptions.
- **Popover + FluentDialog z-index conflict (Pre-mortem PM-6):** Known cross-story risk deferred to Story 2-5 scope — 2-5's dialog-opening path will need to close any open Story 2-2 popover before rendering a destructive confirmation. The coordination contract (interface + registry) lands with 2-5 rather than being speculatively built here. Story 2-2 surfaces this in Known Gaps and its popover components expose a public `ClosePopoverAsync()` method so 2-5 can integrate without needing a new contract.
- **CompactInline collapse (Escape):** Focus returns to whichever element invoked the expansion (recorded via `OnCollapseRequested` callback); if no invoker is recorded, focus falls back to the first focusable element in the containing section
- **FullPage submit (success):** Navigation to `ReturnPath` occurs (after Decision D32 validation); focus is set to the skip-link target of the destination page (native Blazor `NavigationManager.NavigateTo` behavior; verified, not overridden)

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
- A header row with the CompactInline `BatchIncrementCommandRenderer` (expand-in-row style, standalone placement; Decision D22 uses full TypeName — no stripping)
- An inline-button `IncrementCommandRenderer` rendered with its popover
- A link navigating to the full-page route `/commands/Counter/ConfigureCounterCommand`

**And** the Counter sample wraps the inline/compact renderers in a manual `<CascadingValue Value="@_demoProjectionContext">` to demonstrate `ProjectionContextProvider` pre-fill for the derivable aggregate ID (Decision D27 — shell-level cascading is Epic 4)

**And** AC10 does NOT claim DataGrid state preservation on return from FullPage (that demo is deferred to Story 4.3 per Decision D30 — 2-2 only proves the `RestoreGridStateAction` dispatch contract with an empty state map)

**And** manual smoke test confirms: lifecycle progress ring appears in Submitting, the `CounterProjection` DataGrid refreshes after Confirmed (reusing the effect from Story 2-1 Task 7.3, extended to the two new commands), all three modes survive a full `dotnet watch` hot-reload cycle (per Story 1-8 constraints; note: adding `[Icon]` to a command requires a full restart per Story 1-8 hot reload limitations).

**References:** FR8, UX-DR16, UX-DR17 (scroll stabilization — implementation side), UX-DR19 (DataGrid state preservation — feature side), UX-DR36 (button hierarchy), NFR89 (≤2 clicks)

---
