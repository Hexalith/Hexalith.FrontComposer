# Sprint Change Proposal — Convert FrontComposer domain UIs to Fluent UI v5 (no raw HTML controls)

_Workflow: bmad-correct-course · Date: 2026-06-09 (completed) · Mode: Batch · Status: **COMPLETE — Tenants.UI fully Fluent v5 (0 raw interactive controls) and its test suite is GREEN: 670/670 (`-c Release`, was 644/25/669), including a new domain-UI governance guard. Open decisions resolved (audit date-filter → native datetime-local; FcHomeCard kept as a documented styled-button exception). Shell stragglers 6/7 converted + green. NFR6 focus wiring verified (dispatch test-covered; runtime landing folded into the live-visual residual). Remaining out-of-band: live visual check under Aspire, and EventStore Admin.UI ×5 (submodule — needs approval). See Section 7.** (Administrator, 2026-06-09)_

> Companion to `sprint-change-proposal-2026-06-09.md` (nav auto-registration, already implemented).
> That change made the Tenants nav render through the Fluent shell; **this** change fixes the page
> *bodies*, which are still hand-rolled raw HTML.

## Section 1 — Issue Summary

**Problem.** `Hexalith.Tenants.UI` (a FrontComposer **domain consumer**, in the `Hexalith.Tenants`
submodule) is built almost entirely from **raw HTML form controls** instead of Fluent UI Blazor v5
components. In Fluent UI v5 the design system only styles its own custom elements (`<fluent-button>`,
`<fluent-text-input>`, `<fluent-select>`/`<fluent-dropdown>`, `<fluent-field>`, …); a native
`<button>` / `<input>` / `<select>` is never upgraded, so it falls back to **unstyled browser
rendering** — the flat grey, `outset`-bordered buttons observed on the Tenants pages.

This violates the project rule **"FrontComposer should only use Fluent UI v5"**, the documented
**ADR-003** (build on Fluent UI v5 RC), the framework's intended consumption pattern
(`architecture.md` §4 — consumers reduce to `<FrontComposerShell>` and render through Fluent), and
**NFR6** (accessibility: `aria`/`role`/`data-testid`/focus on every interactive element — guarantees
that Fluent components provide and bare HTML controls do not).

**Discovery.** Found during manual verification after the 2026-06-09 nav auto-registration change:
running the stack under Aspire and inspecting the Tenants UI in the browser. The header and (now) the
left nav are correct Fluent v5; the page bodies are not.

**Evidence.**

- **Element audit** (`.razor` source across the three FrontComposer UI surfaces):

  | UI | `FluentButton` | raw `<button>` | Fluent inputs | raw input/select/textarea |
  |---|---|---|---|---|
  | EventStore Admin.UI | 390 | 5 | 62 | 0 |
  | FrontComposer.Shell | 31 | 5 | 4 | 2 |
  | **Tenants.UI** | **0** | **70** | **0** | **27** |

- **Runtime (browser).** On Tenants.UI the "Actualiser" button renders as
  `<button type="button" data-testid="tenants-list-refresh" b-up3esfkpr0>` with computed
  `background-color: rgb(240,240,240)`, `border: outset`, `border-radius: 0`, Arial — i.e. a native
  default. By contrast, Admin.UI's Commands page renders real `<fluent-button>` (computed
  `border-radius: 4px`) and brand-blue `fluent-layout-item` (`rgb(0,102,204)` = `#0066CC`).
- **Not a setup/asset failure.** `bundle.scp.css`, `lib.module.js`, `default-fuib.css` all return
  HTTP 200 on Tenants.UI; `AddFluentUIComponents()` is called; `FluentProviders` is present via the
  shell; `<FluentDataGrid>` (used 3×) renders correctly. The pipeline works — **only the page markup
  bypasses Fluent.**

## Section 2 — Impact Analysis

### Epic Impact — none to the FrontComposer framework
FrontComposer framework Epics 1–7 are **done** and already built correctly on Fluent v5. No framework
epic is modified, added, removed, resequenced, or invalidated. The defect is **domain-consumer
conformance** in the `Hexalith.Tenants` submodule, which is not tracked in FrontComposer's `epics.md`.

### Artifact Conflicts
- **PRD** — N/A (no standalone PRD; `epics.md` FRs unchanged — this is conformance, not a requirement
  change). The relevant requirements (**FR9** shell frame, **FR11** Fluent DataGrid, **UX-DR1/DR2**
  Fluent tokens/badges, **NFR6** a11y) are *reinforced*, not altered.
- **Architecture** (`_bmad-output/project-docs/architecture.md`) — **no conflict**; the change makes
  the Tenants UI match §4. *Optional:* add one governance line — "domain UIs use Fluent v5 components
  only; no raw HTML form controls."
- **UI/UX** — **positive impact.** Restores Fluent styling, design tokens, and NFR6 accessibility
  affordances across all Tenants pages.
- **Tests / other artifacts** — `Hexalith.Tenants.UI.Tests` (21 files use DOM queries; ~669 tests)
  must be updated for the `<fluent-*>` DOM shape; **all `data-testid` values are preserved** so most
  `[data-testid=…]` queries keep working, but tag-name/`MarkupMatches` assertions and Playwright E2E
  selectors need review. No CI/CD, IaC, deployment, or observability impact.

### Technical Impact
~**97 controls across 23 `.razor` files** in Tenants.UI, plus **~10 raw-`<button>` stragglers** in
FrontComposer.Shell (5) and EventStore Admin.UI (5). Conversions are mostly mechanical but carry
**binding-pattern changes** — notably `<select value @onchange>` → `<FluentSelect>` with
`@bind-Value`/`ValueChanged` + `<FluentOption>` children — that require per-control care to preserve
behavior and validation.

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment** (selected). Convert raw HTML controls to Fluent v5 components in the
domain UIs and update the affected tests. No epic restructuring; additive to the just-landed nav work.

- **Option 2 — Rollback:** *Not viable.* There is nothing to revert to — the raw HTML *is* the
  original (flawed) build; reverting would remove functionality.
- **Option 3 — MVP Review:** *N/A.* MVP is not threatened; no scope reduction needed.

**Effort:** Medium-High · **Risk:** Medium (test churn + select/input binding parity) · **Timeline:**
no epic/milestone slip; contained, mechanical-but-careful work.

**Scope (per decision: all FrontComposer surfaces):**
1. **Tenants.UI** — full conversion (primary, ~97 controls / 23 files).
2. **FrontComposer.Shell** — 5 raw-`<button>` stragglers.
3. **EventStore Admin.UI** — 5 raw-`<button>` stragglers (*cross-repo:* EventStore submodule has its
   own BMAD backlog; track there but execute in the same sweep).

## Section 4 — Detailed Change Proposals

### Control → Fluent v5 mapping (canonical)

| Raw HTML | Fluent v5 | Binding / notes |
|---|---|---|
| `<button @onclick>` | `<FluentButton OnClick Appearance="ButtonAppearance.Primary/Neutral">` | preserve `data-testid`, `disabled` → `Disabled` |
| `<input type="search">` | `<FluentSearch>` (or `<FluentTextInput>`) | `value @onchange` → `@bind-Value` / `ValueChanged` |
| `<input type="text">` | `<FluentTextInput>` / `<FluentTextField>` | same binding swap |
| `<input type="number">` | `<FluentNumberField TValue="…">` | typed value binding |
| `<select>` + `<option>` | `<FluentSelect>` + `<FluentOption>` | `value @onchange` → `@bind-Value`/`ValueChanged`; option `Value`/text preserved |
| `<textarea>` | `<FluentTextArea>` | `@bind-Value` |
| `<label><span>…</span></label>` wrappers | `Label="…"` param or `<FluentField>` | keep `@Localizer[...]` keys |

**Invariants for every conversion:** keep `data-testid`; keep `@onclick`/`@onchange` semantics
(remapped to the Fluent event/`@bind`); keep `@Localizer["…"]` keys; preserve `disabled` logic; no
behavior change.

### Representative before/after — `Components/Pages/TenantsWorkspace.razor`

```razor
OLD:
<select value="@_statusFilter" data-testid="tenants-list-status-filter" @onchange="OnStatusFilterChanged">
    <option value="">@Localizer["Tenants.List.StatusFilter.All"]</option>
    <option value="@TenantStatus.Active">@Localizer["Tenants.List.StatusFilter.Active"]</option>
    <option value="@TenantStatus.Disabled">@Localizer["Tenants.List.StatusFilter.Disabled"]</option>
    <option value="@TenantStatus.Unknown">@Localizer["Tenants.List.StatusFilter.Unknown"]</option>
</select>

NEW:
<FluentSelect TOption="string" Value="@_statusFilter" ValueChanged="OnStatusFilterChanged"
              data-testid="tenants-list-status-filter" aria-label="@Localizer["Tenants.List.StatusFilterLabel"]">
    <FluentOption Value="">@Localizer["Tenants.List.StatusFilter.All"]</FluentOption>
    <FluentOption Value="@TenantStatus.Active.ToString()">@Localizer["Tenants.List.StatusFilter.Active"]</FluentOption>
    <FluentOption Value="@TenantStatus.Disabled.ToString()">@Localizer["Tenants.List.StatusFilter.Disabled"]</FluentOption>
    <FluentOption Value="@TenantStatus.Unknown.ToString()">@Localizer["Tenants.List.StatusFilter.Unknown"]</FluentOption>
</FluentSelect>
```

```razor
OLD:
<button type="button" data-testid="tenants-list-refresh" @onclick="RefreshAsync">
    @Localizer["Tenants.List.Refresh"]
</button>

NEW:
<FluentButton Appearance="ButtonAppearance.Primary" data-testid="tenants-list-refresh" OnClick="RefreshAsync">
    @Localizer["Tenants.List.Refresh"]
</FluentButton>
```

```razor
OLD:
<input type="search" value="@_search" placeholder="@Localizer["Tenants.List.SearchPlaceholder"]"
       data-testid="tenants-list-search" @onchange="OnSearchChanged" />

NEW:
<FluentSearch Value="@_search" Placeholder="@Localizer["Tenants.List.SearchPlaceholder"]"
              data-testid="tenants-list-search" ValueChanged="OnSearchChanged"
              aria-label="@Localizer["Tenants.List.SearchLabel"]" />
```

### File coverage (Tenants.UI — by raw-control count)
`GlobalAdministratorsPage` (11) · `TenantsWorkspace` (8) · `SetTenantConfigurationFlow` (7) ·
`UserMembershipLookupPage` (7) · `TenantAuditPage` (7) · `EditTenantMetadataFlow` (6) ·
`CorrectionStartPanel` (6) · `CreateTenantFlow` (5) · `RemoveTenantMemberFlow` (4) ·
`ChangeTenantMemberRoleFlow` (4) · `AddTenantMemberFlow` (4) · `TenantLifecycleCommandFlow` (4) ·
`RemoveTenantConfigurationFlow` (4) · `TenantConfigurationView` (3) · `AuditDataGrid` (3) ·
`MyTenantsPage` (3) · `MemberAccessReview` (2) · `TenantLifecycleActionAvailability` (2) ·
`AuditEvidenceReceipt` (2) · `ListSurfaceStates` (2) · `MyTenantsState` (1) ·
`AuditAvailabilityState` (1) · `SupportSafeCopyButton` (1). **Stragglers:** FrontComposer.Shell ×5,
EventStore Admin.UI ×5.

### Optional follow-up (recommended, not blocking)
Add a **governance guard** so this can't regress: a bUnit/governance test or Roslyn analyzer that
fails when a domain UI `.razor` emits a raw interactive HTML control (`<button>`, `<input>`,
`<select>`, `<textarea>`). Naturally extends the HFC accessibility-analyzer family.

## Section 5 — Implementation Handoff

**Scope classification: Moderate** — multi-file implementation across submodules with test updates; no
epic replan, no architecture/PRD change.

- **Primary → Developer agent:** convert Tenants.UI (23 files) using the mapping above; update
  `Hexalith.Tenants.UI.Tests` for the `<fluent-*>` DOM shape (preserve `data-testid`); review
  Playwright E2E selectors. Acceptance below.
- **Same sweep → Developer agent:** FrontComposer.Shell ×5 and Admin.UI ×5 stragglers (the Admin.UI
  five are in the EventStore submodule — coordinate with that repo's PO/backlog).
- **PO awareness:** record the Tenants.UI conversion as a maintenance/conformance item in the Tenants
  submodule backlog (FrontComposer `epics.md` and `sprint-status.yaml` need **no** change).
- **Suggested sequencing:** (1) `TenantsWorkspace` end-to-end as the proven pattern + green tests →
  (2) roll the pattern across the remaining 22 files → (3) stragglers → (4) optional governance guard.

**Success criteria — verified status (2026-06-09 re-verification).**
- ✅ **0 raw interactive HTML controls in Tenants.UI `.razor`** — audited; all 23 files clean.
  ❌ Shell (×5) / Admin.UI (×5) stragglers still present.
- ✅ **Tenants.UI builds clean** — `Hexalith.Tenants.UI.csproj -c Release` → 0 warnings / 0 errors (TWAE).
  ⏳ Full-solution `Hexalith.FrontComposer.slnx -c Release` not re-run this pass.
- ❌ **`Hexalith.Tenants.UI.Tests` NOT green — 644 passed / 25 failed / 669 total** (`DiffEngine_Disabled=true`,
  Release). Root cause: stale DOM-shape assertions not migrated to the `<fluent-*>` shape (see Section 6).
- ⏳ **Visual** check still deferred — Aspire stack not running this pass.
- ⏳ **NFR6 focus** — `@ref`/`FocusAsync` conversions compile, but runtime focus-return correctness is
  not proven by the build; needs the per-control re-verification flagged in Section 6.
- ✅ `data-testid` hooks and localized labels preserved on converted controls (source-audited).

---

## Section 6 — Implementation progress (2026-06-09)

### Status — production conversion COMPLETE; tests RED (re-verified 2026-06-09)
**This supersedes the earlier "flagship-only / remaining paused" snapshot, which is now obsolete.**
Since that snapshot the rollout was carried across **all 23 Tenants.UI `.razor` files** in the working
tree (uncommitted). Re-verification this pass found:

- ✅ **Production markup converted — 0 raw interactive controls** (`<button>/<input>/<select>/<textarea>`)
  across all 23 files (source audit). Search→`FluentTextInput`, `<select>`→`FluentSelect`+`FluentOption`
  (enum values `.ToString()`), buttons→`FluentButton`, `<textarea>`→`FluentTextArea`; change handlers
  re-typed `ChangeEventArgs`→value type; `data-testid`/`@Localizer` preserved.
- ✅ **Build clean** — `Hexalith.Tenants.UI.csproj -c Release` → **0 warnings / 0 errors** (TWAE).
- ❌ **Tests NOT green** — `Hexalith.Tenants.UI.Tests` = **644 passed / 25 failed / 669 total**
  (`DiffEngine_Disabled=true`, Release). The production code outran the test updates.

#### The 25 failing tests (by class)
`SupportSafeCopyButtonTests` ×12 · `TenantDetailSurfaceTests` ×2 · `TenantAuditPageTests` ×2 ·
`AuditEvidenceReceiptTests` ×2 · `AuditAvailabilityStateTests` ×1 · `ChangeTenantMemberRoleFlowTests` ×1 ·
`CorrectionStartPanelTests` ×1 · `MyTenantsSurfaceTests` ×1 · `TenantLifecycleActionAvailabilityTests` ×1 ·
`TenantListSurfaceTests` ×1.

**Root cause — two buckets:**
- **24 = stale DOM-shape assertions** (mechanical, per the recipe below). Tests still assert
  `GetAttribute("type") == "button"` or tag-name `BUTTON`/`INPUT` on what are now `<fluent-*>` elements
  (AngleSharp `HtmlUnknownElement` / `FLUENT-BUTTON`). The components are behaviorally faithful
  (`data-testid`, `aria-label`, `OnClick`, binding preserved — e.g. `SupportSafeCopyButton`'s classify /
  JS-interop logic is unchanged). **Two test files need attention:** `TenantDetailSurfaceTests.cs` was
  **never updated** (not in the working-tree diff), and `SupportSafeCopyButtonTests.cs` *was* edited but
  its update is **incomplete** (still 12 red). Fix = apply the recipe; **confirm behavior, don't just
  silence the assertion.**
- **1 = genuine design decision (audit date filters).** `<input type="datetime-local">`
  (`tenants-audit-filter-from`/`-to`) was converted to a **plain `FluentTextInput` (text)**, losing the
  native datetime-picker affordance; users now type the value as free text (parsed in
  `OnFromChanged`/`OnToChanged`). **Decide:** accept text entry (update the test to the new shape) **or**
  use `TextInputType="TextInputType.DateTimeLocal"` to keep native datetime-local semantics in a Fluent
  control. Not a pure test fix.

### Reusable recipe (validated — use for the remaining files)
- **Markup:** `<button @onclick disabled>`→`<FluentButton Appearance="ButtonAppearance.Primary|Outline"
  OnClick Disabled>`; `<input type=search>`→`<FluentTextInput TextInputType="TextInputType.Search"
  Value ValueChanged Label Placeholder>`; `<select value @onchange>`+`<option>`→`<FluentSelect
  TOption="string" TValue="string" Value ValueChanged Label>`+`<FluentOption TValue="string" Value>`
  (enum option values need `.ToString()`); `<textarea>`→`<FluentTextArea>`. Always keep `data-testid`,
  `@Localizer[...]`, and disabled/binding semantics; use the component's `Label` instead of a wrapping
  `<label><span>`.
- **Handlers:** `ChangeEventArgs args` → the value type (`string?`); read `value` directly.
- **bUnit tests:** add `JSInterop.Mode = JSRuntimeMode.Loose` to any class/test that renders converted
  components (they import JS modules on first render); replace `GetAttribute("type")` assertions with
  `NodeName` (`FLUENT-BUTTON` / `FLUENT-TEXT-INPUT`) — this also guards against raw-HTML regression;
  `.Change()` works on `FluentTextInput`, `.Click()` works on `FluentButton`, but **`FluentSelect`**
  must be driven via the component callback (`ondropdownchange`'s `DropdownEventArgs` is internal) —
  see the `ChangeSelectAsync` helper added to `TenantListSurfaceTests` (find the `FluentSelect<string,
  string>` by `data-testid` in `AdditionalAttributes`, `await cut.InvokeAsync(() =>
  select.ValueChanged.InvokeAsync(value))`).

### Accessibility focus management (NFR6) — resolved structurally; runtime still unverified
The ~11 flow/panel files that attach `@ref` + call `.FocusAsync()` for WCAG focus-return/focus-on-error
were handled two ways (both compile clean): Fluent **input** refs are typed as the component and use
`ref.Element.FocusAsync()` (e.g. `RemoveTenantMemberFlow._confirmationElement`,
`CreateTenantFlow._tenantIdElement`, `EditTenantMetadataFlow._nameElement`); **buttons/selects** focus
via a wrapping `<span tabindex="-1" @ref="…ElementReference…">` + `ref.FocusAsync()` (e.g.
`TenantLifecycleActionAvailability`, `MemberAccessReview`, `TenantConfigurationView`). ⚠ **The build
proves it compiles, not that focus lands correctly at runtime** — per-control NFR6 focus-return behavior
is still unverified (no live/visual run this pass). This remains the top residual risk.

### Remaining work (rescoped after verification — 2026-06-09)
Production `.razor` conversion is **done**; the open items are now:
1. **Make the test suite green (25 failures).** 24 are mechanical assertion-shape updates (Loose
   JSInterop / `NodeName` instead of `GetAttribute("type")` / `ChangeSelectAsync`); fully update the
   missed `TenantDetailSurfaceTests.cs` and finish `SupportSafeCopyButtonTests.cs`. **Confirm each
   component still behaves — don't just match the new DOM.**
2. **Resolve the audit date-filter decision** (text `FluentTextInput` vs `TextInputType.DateTimeLocal`),
   then align `TenantAuditPageTests`.
3. **Re-verify NFR6 focus-return at runtime** for the ~11 focus-entangled flows (build-green ≠ focus
   correct).
4. **Live visual check** under Aspire — confirm Fluent styling (no native grey controls) on every page.
5. **Stragglers:** FrontComposer.Shell ×5 raw `<button>` (`DevMode/*` + `FcHomeCard` — *confirm these
   dev-mode controls are even in scope for the "domain UI only" rule*); EventStore Admin.UI ×5
   (**EventStore submodule — needs explicit approval before any edit**).
6. **Optional governance guard** against raw interactive HTML in domain UIs (bUnit/Roslyn).

### Files changed (working tree — uncommitted, Tenants submodule)
- **23 `.razor`** under `src/Hexalith.Tenants.UI/Components/**` (pages, shared, audit, members,
  configuration, lifecycle, metadata, users surfaces — 0 raw interactive controls remain).
- **18 test files** under `tests/Hexalith.Tenants.UI.Tests/**` + **1 new** `Components/FluentBunitContext.cs`.
- Not yet green: `TenantDetailSurfaceTests.cs` (untouched) plus 9 other test classes still asserting the
  old DOM shape — the 25 failures above. *(All resolved this pass — see Section 7.)*

---

## Section 7 — Final verification & completion (2026-06-09, follow-up correct-course pass)

**This supersedes the RED status in Sections 5–6.** A follow-up `bmad-correct-course` run re-verified
ground truth, resolved the open decisions, and drove the remaining work to green.

### Status — COMPLETE for the domain UI; residuals are out-of-band
- ✅ **Tenants.UI test suite GREEN — 670 / 670** (`-c Release`, `DiffEngine_Disabled=true`): the prior
  669 + **1 new governance guard**. (Was 644 passed / 25 failed / 669.)
- ✅ **Tenants.UI raw interactive controls = 0** (re-audited live).
- ✅ **Shell stragglers 6 / 7 converted + green** — Shell `DevMode`/`Home` tests 54/54, Release build clean.
- ✅ **Governance guard added** (`DomainUiFluentConformanceTests`, Tenants.UI scope) — fails the build if a
  raw `<button>/<input>/<select>/<textarea>` regresses into a domain-UI `.razor`.
- ⏳ **Live visual check** — still pending (no Aspire run this pass).
- ⛔ **EventStore Admin.UI ×5** — untouched (submodule; needs explicit approval).

### How the 25 failures were resolved (behaviour-confirmed, not silenced)
- **24 stale DOM-shape assertions** → migrated to the Fluent shape: `GetAttribute("type")=="button"` /
  `TagName=="BUTTON"` → `NodeName=="FLUENT-BUTTON"`; `cut.Find("button")` → find by the preserved
  `data-testid` + `.Click()`. The never-migrated `TenantDetailSurfaceTests.cs` and the incomplete
  `SupportSafeCopyButtonTests.cs` (12) were finished.
- **FluentSelect interaction** (`ChangeTenantMemberRoleFlowTests`) — `.Change()` threw
  `MissingEventHandlerException` (`onchange` vs Fluent's internal `ondropdownchange`); now driven via the
  component `ValueChanged` callback (`FluentSelectInterop.ChangeFluentSelect`).
- **Fluent submit button** (`TenantLifecycleActionAvailabilityTests`) — bUnit does not treat a
  `<fluent-button type=submit>.Click()` as a form submitter, so `SubmitAsync`/validation never ran; switched
  to the established `cut.Find("form").Submit()` idiom (matches every other flow test). Runtime submit is
  unaffected — Fluent's submit button is form-associated.

### Open decision RESOLVED — audit date filters → native datetime-local
The `tenants-audit-filter-from/-to` filters keep **native datetime-local semantics**. ⚠️ The proposal's
literal `TextInputType="TextInputType.DateTimeLocal"` is **not possible** in the pinned Fluent v5 RC
(`5.0.0-rc.3-26138.1`): the `TextInputType` enum has **no DateTimeLocal member** (Text/Email/Password/
Telephone/Url/Color/Search/Number only). The chosen outcome was delivered via a **splatted
`type="datetime-local"` attribute** on `FluentTextInput` (the string `Value`/`ValueChanged` binding and UTC
`ParseDate` logic are unchanged); the test's `GetAttribute("type")=="datetime-local"` passes. Runtime
native-picker rendering depends on the web component forwarding `type` ("relies on browser support" per
Fluent docs) — folded into the live-visual residual.

### NFR6 focus-return — verified to the extent CI allows
Both focus patterns are wired correctly and compile clean: Fluent input refs use
`ref.Element.FocusAsync()`; plain element / `<span tabindex="-1">` refs use `ref.FocusAsync()`. The focus
**dispatch path is test-covered** (e.g. `Configuration_view_returns_focus_to_launching_remove_control_on_cancel`
asserts the JS focus-invocation count increases — green). True browser focus-*landing* is inherently
unverifiable in bUnit (Loose JSInterop → `FocusAsync` is a mocked no-op) and is folded into the live-visual
residual.

### Shell stragglers — 6/7 done; FcHomeCard is a documented exception
Converted to Fluent v5 (preserving classes, `data-testid`, handlers, dev-only gating): `FcDevModeToggleButton`
and `FcDevModeAnnotation` buttons → `FluentButton`; `FcDevModeOverlay` close + copy buttons → `FluentButton`,
the before/after checkbox → `FluentCheckbox`, and the source `<textarea>` → `FluentTextArea`. **`FcHomeCard`
intentionally NOT converted** (decision 2026-06-09): it is framework chrome (the Shell is not a domain UI),
already fully styled via `.fc-home-card-button` scoped CSS with `role="link"` + custom keyboard activation —
i.e. *not* the unstyled-control defect the rule targets — and it is a full-card-content button (`<h2>` +
projection `<ul>`) that `FluentButton` cannot host without visual regression. Recorded as a justified
carve-out; the governance guard scopes to Tenants.UI, so it does not flag the Shell.

### Residuals (carried forward, out of this pass's scope)
1. **Live visual check** under Aspire — confirm Fluent styling on every page, the datetime-local picker,
   and runtime focus-return.
2. **EventStore Admin.UI ×5** raw `<button>` — EventStore submodule; needs explicit approval; track in that
   repo's backlog.

### Files changed this pass (working tree — uncommitted)
- **Tenants submodule:** `Components/Pages/TenantAuditPage.razor` (datetime-local); **10 test files** migrated
  to the Fluent shape (`SupportSafeCopyButtonTests`, `TenantDetailSurfaceTests`, `TenantAuditPageTests`,
  `AuditEvidenceReceiptTests`, `AuditAvailabilityStateTests`, `CorrectionStartPanelTests`,
  `MyTenantsSurfaceTests`, `TenantListSurfaceTests`, `ChangeTenantMemberRoleFlowTests`,
  `TenantLifecycleActionAvailabilityTests`) + **1 new** `DomainUiFluentConformanceTests.cs` (governance guard).
- **FrontComposer repo:** `Shell/Components/DevMode/{FcDevModeToggleButton,FcDevModeAnnotation,FcDevModeOverlay}.razor`.

---

_No commit made (not requested). Tenants submodule changes are working-tree only; the FrontComposer Shell
edits are in the FrontComposer repo. Per project rules, the EventStore submodule (Admin.UI stragglers) must
not be modified without explicit approval._
