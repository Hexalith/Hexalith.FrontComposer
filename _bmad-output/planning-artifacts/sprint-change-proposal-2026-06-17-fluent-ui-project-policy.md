# Sprint Change Proposal — Project-wide policy: every UI page uses FrontComposer or Fluent UI v5

_Workflow: bmad-correct-course · Date: 2026-06-17 · Mode: Incremental · Author: Administrator · Status: **APPROVED + IMPLEMENTED — all touched test lanes GREEN (FrontComposer FluentConformanceTests 2/2; EventStore Admin.UI 22/22; Tenants DomainUiFluentConformanceTests 1/1), clean Debug builds (TWAE). Changes uncommitted across 3 repos. Residuals: full Release solution build, live-visual check under the restarted Aspire stack. See "Implementation complete" at the bottom.**_

> Trigger (Administrator): _"each UI page should use FrontComposer or Blazor Fluent UI V5 components."_
>
> Generalizes the per-module rule established for `Hexalith.Tenants.UI` into a **project-wide governance
> policy** spanning every FrontComposer UI surface, extends enforcement beyond Tenants.UI, and closes the
> last known residual (EventStore Admin.UI ×5 raw buttons).
>
> Builds directly on three prior correct-course passes:
> - `sprint-change-proposal-2026-06-09-fluent-v5-domain-ui.md` — Tenants.UI form controls → Fluent v5 + the
>   `DomainUiFluentConformanceTests` governance guard (Tenants.UI-scoped).
> - `sprint-change-proposal-2026-06-14-shell-security-helper.md` — shared security wiring → Shell helpers.
> - `sprint-change-proposal-2026-06-16-tenants-grid-fluent.md` — Tenants grid cell content → Fluent.
>
> **Submodule approval:** Administrator authorized editing the `Hexalith.EventStore` submodule for the
> Admin.UI conversion in scope here (2026-06-17).

---

## Section 1 — Issue Summary

**Problem.** FrontComposer's "build on Fluent UI v5" rule (ADR-003) is **written and enforced only for
`Hexalith.Tenants.UI`**. There is no project-wide, written policy that *every* UI page/component — across
the framework Shell, the samples, and all domain consumers — must render through **FrontComposer
components or Fluent UI Blazor v5**, and the only enforcement (`DomainUiFluentConformanceTests`) is scoped
to the Tenants submodule. As a result the Shell, the Counter sample, and the EventStore Admin.UI can drift
back to raw interactive HTML controls without any guard failing.

In Fluent UI v5 the design system only styles its own custom elements (`<fluent-button>`,
`<fluent-text-input>`, `<fluent-select>`, …); a native `<button>`/`<input>`/`<select>`/`<textarea>` is
never upgraded — it falls back to **unstyled browser rendering** *and* drops the `aria`/`role`/focus
affordances that the Fluent components guarantee (**NFR6**).

**Issue type:** New requirement / governance standardization discovered via direct stakeholder directive
(not a defect). It **reinforces** existing requirements (FR9 shell frame, FR11 Fluent DataGrid, UX-DR1/DR2
Fluent tokens/badges, NFR6 accessibility) rather than changing any of them.

**Discovery.** Stakeholder directive on 2026-06-17, followed by a full `.razor` source audit across every
UI surface.

**Evidence — forbidden interactive controls (`<button>/<input>/<select>/<textarea>`) by surface:**

| Surface | Raw controls | Notes |
|---|---|---|
| `Hexalith.Tenants.UI` | **0** | ✅ Clean + guarded (`DomainUiFluentConformanceTests`) |
| `Hexalith.FrontComposer.Shell` | **1** | `FcHomeCard.razor` — intentional framework-chrome carve-out (`role="link"`, scoped CSS, custom keyboard activation) |
| `Counter.Specimens` (sample) | **7** | `FrontComposerTypeSpecimen.razor` — the raw controls **are** the visual-test specimen fixtures (intentional) |
| `Hexalith.EventStore.Admin.UI` | **5** | `Streams`, `Breadcrumb` ×2, `ActivityChart`, `JsonViewer` — long-standing residual (submodule) |

`<a>` navigation anchors are intentionally **not** in scope (the 2026-06-16 pass kept inline detail links;
`FluentAnchor` restyles them as buttons). Admin.UI already uses Fluent v5 heavily (per the 06-09 audit:
390 `FluentButton`, 62 Fluent inputs, 0 raw inputs) — only the 5 buttons remain.

**Admin.UI residual — the 5 stragglers split into two kinds:**

| File | Control | Kind | Disposition |
|---|---|---|---|
| `Components/Shared/JsonViewer.razor` | "Show full payload (N lines)" text button | plain action | **Convert** → `FluentButton` |
| `Layout/Breadcrumb.razor` (copy) | copy-URL icon button (already wraps `<FluentIcon Copy>`) | icon button | **Convert** → `FluentButton` `IconStart` |
| `Layout/Breadcrumb.razor` (truncation) | "…" path-truncation toggle | inline toggle | **Convert** → `FluentButton Appearance="Lightweight"` |
| `Components/ActivityChart.razor` | clickable bar-chart bar (`<button>` wrapping a height-scaled `<div>`) | data-viz element | **Carve-out** (a `FluentButton` destroys the bar) |
| `Pages/Streams.razor` | inline monospace click-to-copy aggregate-ID cell | grid-cell affordance | **Carve-out** (a `FluentButton` breaks the cell layout) |

Both carve-out candidates already carry full `aria-label`/`role`/`data-testid` — i.e. they are **not** the
unstyled-control defect the rule targets; they are styled, accessible, custom interactive elements, exactly
the `FcHomeCard` rationale.

---

## Section 2 — Impact Analysis

### Epic Impact — none
FrontComposer framework **Epics 1–7 are Done** and already built correctly on Fluent v5. No epic is
modified, added, removed, resequenced, or invalidated. This is cross-cutting **conformance + governance**,
anchored to NFR6 (a11y) and UX-DR1/DR2 (Fluent tokens/badges). Domain-consumer conformance (Tenants.UI,
Admin.UI) is not tracked in FrontComposer's `epics.md`.

### Story Impact — none
No formal story. Recorded as a governance/conformance change (same handling as the prior three passes).

### Artifact Conflicts

| Artifact | Impact | Action |
|---|---|---|
| **PRD** | N/A — no authored PRD exists; `epics.md` FR/NFR unchanged | none |
| **Architecture** (`_bmad-output/project-docs/architecture.md`) | §4 has only ADR-003 + a scattered per-module note; needs a first-class project-wide governance principle + a carve-out registry | ✏️ edit |
| **AI rules** (`_bmad-output/project-context.md`) | Blazor Shell rules section lacks the Fluent-only UI rule | ✏️ edit |
| **UI/UX** | No authored UX spec to conflict; the change *reinforces* the implemented Fluent token/badge/a11y contracts | positive |
| **Tests** | Enforcement is Tenants-only; need per-surface guards (Shell, Counter, Admin.UI) with carve-out allowlists. Admin.UI conversions break DOM-shape assertions (now `<fluent-*>`); `data-testid` preserved so most `[data-testid]` queries survive | ➕ add / ✏️ update |
| **Public API** | No public surface change (internal markup + test-only additions) | none |
| **`docs/` (FC-DOC, Gate 2d)** | No new public component; policy is governance doc only | none |
| **Memory** | Add a project memory recording the project-wide policy + carve-out registry | ✏️ after impl |
| **CI / IaC / deployment / observability** | none | none |

### Technical Impact
- **FrontComposer repo:** 1 architecture doc edit, 1 project-context edit, 1 new Shell governance guard test.
- **Counter sample:** 1 new governance guard test (scans `Counter.Web`, excludes `Counter.Specimens`).
- **EventStore Admin.UI (submodule):** 3 raw buttons → `FluentButton`; 1 new governance guard test with a
  2-entry carve-out allowlist; affected Admin.UI bUnit DOM-shape assertions updated (`<fluent-button>` node
  shape; `JSInterop.Mode = Loose` where components import JS on first render). Mechanical-but-careful.
- **Tenants submodule:** the existing guard stays; only its doc comment is updated to reference the
  project-wide policy (no behavioral change).

---

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment (selected).** Codify the policy (architecture + project-context), extend
enforcement with per-surface guards carrying explicit carve-out allowlists, and close the Admin.UI residual
(convert the 3 clean buttons; document the 2 viz/cell carve-outs). No epic restructuring; additive to the
landed Tenants work.

- **Option 2 — Rollback:** N/A. Nothing to revert — the policy does not yet exist; the residual *is* the
  original build.
- **Option 3 — MVP review:** N/A. MVP is not threatened; no scope reduction.

**Effort:** Medium · **Risk:** Low–Medium (Admin.UI test DOM churn; the 2 carve-out judgments are settled)
· **Timeline:** no epic/milestone slip; contained.

**Carve-out principle (decided this pass).** The rule is *not* "zero raw controls" — it is **"use
FrontComposer or Fluent v5 components, with documented, accessibility-complete carve-outs for custom-styled
interactive elements where a Fluent control would cause visual regression."** Each carve-out is recorded in
the registry **and** in the relevant guard's allowlist, so the exception is self-documenting and cannot be
silently widened.

**Decisions captured (Administrator, 2026-06-17):**
1. Mode = **Incremental**.
2. Admin.UI = **in scope**; editing the `Hexalith.EventStore` submodule is **authorized**.
3. Exceptions = **codify both** FcHomeCard and Counter.Specimens as documented carve-outs.
4. Admin.UI custom elements = **carve out both** the ActivityChart bar and the aggregate-id-copy cell;
   convert only the 3 clean buttons.

---

## Section 4 — Detailed Change Proposals

### 4.A — Architecture governance principle + carve-out registry (`architecture.md` §4)

Add a first-class governance principle:

> **UI component policy (project-wide).** Every UI page and component across all FrontComposer surfaces —
> the framework Shell, the samples, and domain consumers (`Hexalith.Tenants.UI`,
> `Hexalith.EventStore.Admin.UI`) — renders through **FrontComposer components or Fluent UI Blazor v5**.
> Raw interactive HTML controls (`<button>`, `<input>`, `<select>`, `<textarea>`) are **forbidden**: Fluent
> v5 leaves them unstyled and they drop the NFR6 accessibility affordances Fluent components provide. Raw
> `<a>` navigation links are permitted. The rule is enforced per surface by `…FluentConformanceTests`
> governance guards (`[Trait("Category","Governance")]`). **Documented carve-outs** — custom-styled,
> fully-accessible interactive elements where a Fluent control would cause visual regression — are listed
> below and mirrored in each guard's allowlist.

**Carve-out registry (initial):**

| Surface | File | Element | Justification |
|---|---|---|---|
| Shell | `Components/Home/FcHomeCard.razor` | full-card link button | framework chrome; `role="link"` + custom keyboard activation; scoped `.fc-home-card-button` CSS; hosts `<h2>` + projection `<ul>` a `FluentButton` cannot contain without regression |
| Counter sample | `Counter.Specimens/FrontComposerTypeSpecimen.razor` | raw control specimens | the raw controls **are** the visual-test fixtures (a11y/visual specimen gate); not a shipped UI page |
| EventStore Admin.UI | `Components/ActivityChart.razor` | clickable bar-chart bar | data-visualization element (height-scaled `<div>`); `aria-label` present; `FluentButton` destroys the bar |
| EventStore Admin.UI | `Pages/Streams.razor` | inline monospace click-to-copy aggregate-ID cell | grid-cell affordance; `aria-label`/`data-testid`/`stopPropagation` present; `FluentButton` breaks the cell layout |

### 4.B — AI-agent rule (`_bmad-output/project-context.md`, "Blazor Shell & Fluxor Rules")

Add:

> - **Fluent-only UI (project-wide):** every `.razor` page/component uses **FrontComposer or Fluent v5
>   components** — never raw `<button>/<input>/<select>/<textarea>` (Fluent v5 leaves them unstyled and
>   drops NFR6 a11y). Raw `<a>` nav links are allowed. Enforced by per-surface
>   `…FluentConformanceTests` Governance guards; documented carve-outs (FcHomeCard, Counter.Specimens,
>   Admin.UI ActivityChart bar + Streams aggregate-id-copy cell) are listed in `architecture.md` §4 and
>   each guard's allowlist.

### 4.C — Extend enforcement: per-surface governance guards

Mirror the existing `DomainUiFluentConformanceTests` regex (`<(button|input|select|textarea)(\s|/|>)`),
each with an explicit, justified allowlist. All carry `[Trait("Category","Governance")]` (blocking lane).

1. **`tests/Hexalith.FrontComposer.Shell.Tests` → `ShellFluentConformanceTests`** — scans
   `src/Hexalith.FrontComposer.Shell/Components/**/*.razor`; **allowlist:** `FcHomeCard.razor`.
2. **Counter sample tests → `CounterWebFluentConformanceTests`** — scans `samples/Counter/Counter.Web/**`;
   **excludes** `samples/Counter/Counter.Specimens/**` entirely (visual-test fixtures).
3. **EventStore Admin.UI test suite → `AdminUiFluentConformanceTests`** — scans
   `src/Hexalith.EventStore.Admin.UI/{Pages,Layout,Components}/**`; **allowlist:** `ActivityChart.razor`,
   `Streams.razor` (the two carve-outs). *(If Admin.UI has no existing test project, add the guard to the
   nearest Admin UI test assembly or a minimal new one — confirm during implementation.)*
4. **Tenants `DomainUiFluentConformanceTests`** — unchanged behavior; update its XML-doc comment to point at
   the project-wide policy in `architecture.md` §4.

Guard shape (representative):

```csharp
[Fact]
[Trait("Category", "Governance")]
public void Shell_components_use_fluent_v5_only_except_documented_carveouts()
{
    string root = Path.Combine(ProjectRoot(), "src", "Hexalith.FrontComposer.Shell", "Components");
    string[] razor = Directory.GetFiles(root, "*.razor", SearchOption.AllDirectories);
    razor.ShouldNotBeEmpty(); // guard against a broken path silently passing

    // Documented carve-outs (see architecture.md §4): custom-styled, fully-accessible interactive
    // elements where a Fluent control would cause visual regression.
    string[] carveOuts = ["FcHomeCard.razor"];

    List<string> offenders = razor
        .Where(f => !carveOuts.Contains(Path.GetFileName(f), StringComparer.Ordinal))
        .Where(f => RawInteractiveControl.IsMatch(File.ReadAllText(f)))
        .Select(Path.GetFileName)
        .ToList();

    offenders.ShouldBeEmpty(
        "Shell .razor must use FrontComposer/Fluent v5 only (no raw <button>/<input>/<select>/<textarea>); "
        + $"carve-outs are allowlisted. Offenders: {string.Join(", ", offenders)}");
}
```

### 4.D — Close the Admin.UI residual (`Hexalith.EventStore` submodule)

**Convert (3) — invariants: preserve `data-testid`, `aria-label`/`title`, `@onclick` (and
`stopPropagation`/`onkeydown` semantics), localized text; no behavior change.**

`Components/Shared/JsonViewer.razor`:
```razor
OLD:
<button class="json-viewer__show-all" @onclick="ShowFullPayload">
    Show full payload (@(_totalLines) lines)
</button>

NEW:
<FluentButton Appearance="ButtonAppearance.Lightweight" Class="json-viewer__show-all" OnClick="ShowFullPayload">
    Show full payload (@(_totalLines) lines)
</FluentButton>
```

`Layout/Breadcrumb.razor` (copy — already wraps `<FluentIcon>`):
```razor
OLD:
<button class="breadcrumb-copy-btn" aria-label="Copy page URL to clipboard" title="Copy link" @onclick="CopyUrlAsync">
    <FluentIcon Value="@(new Icons.Regular.Size16.Copy())" />
</button>

NEW:
<FluentButton Appearance="ButtonAppearance.Lightweight" Class="breadcrumb-copy-btn"
              aria-label="Copy page URL to clipboard" Title="Copy link" OnClick="CopyUrlAsync"
              IconStart="@(new Icons.Regular.Size16.Copy())" />
```

`Layout/Breadcrumb.razor` (truncation "…" toggle):
```razor
OLD:
<button class="breadcrumb-truncation-btn" aria-label="Show full breadcrumb path" @onclick="ToggleFullPath">...</button>

NEW:
<FluentButton Appearance="ButtonAppearance.Lightweight" Class="breadcrumb-truncation-btn"
              aria-label="Show full breadcrumb path" OnClick="ToggleFullPath">...</FluentButton>
```

> Note: the exact Fluent v5 RC `ButtonAppearance` member names and `Class`/`IconStart`/`Title` parameter
> surface must be confirmed against the pinned `5.0.0-rc.3-26138.1` during implementation (the 06-16 pass
> showed RC enums differ from docs); scoped CSS class names are preserved so existing styling/tests survive.

**Carve out (2) — no markup change; document + allowlist only:**
- `Components/ActivityChart.razor` — clickable bar-chart bar.
- `Pages/Streams.razor` — inline monospace click-to-copy aggregate-ID cell.

**Tests (Admin.UI):** migrate DOM-shape assertions on the 3 converted buttons to the Fluent node shape
(`FLUENT-BUTTON`, lookup by preserved `data-testid`), add `JSInterop.Mode = Loose` where the converted
components now import JS on first render, and add `AdminUiFluentConformanceTests` (§4.C-3).

---

## Section 5 — Implementation Handoff

**Scope classification: Moderate** — multi-file across the FrontComposer repo + 1 submodule, with test
additions/updates and two doc edits; **no** epic/PRD/architecture-pattern replan, MVP unaffected. (Matches
the prior three passes.)

**Routing → Developer agent** (with PO awareness for the submodule backlog entry).

**Suggested sequencing:**
1. **FrontComposer repo (governance):** edit `architecture.md` §4 (principle + registry) and
   `project-context.md`; add `ShellFluentConformanceTests` + `CounterWebFluentConformanceTests`. Build
   Release clean (TWAE); run the default + Governance lanes green
   (`dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`,
   `DiffEngine_Disabled=true`).
2. **EventStore submodule (Admin.UI):** convert the 3 clean buttons; add `AdminUiFluentConformanceTests`
   with the 2-entry allowlist; update affected Admin.UI bUnit assertions. Build Admin.UI Release clean;
   run the Admin.UI test suite green. (Note EventStore's **per-project** `dotnet test` rule — opposite of
   FrontComposer's solution-level rule.)
3. **Tenants submodule:** update the `DomainUiFluentConformanceTests` doc comment to reference the
   project-wide policy (no behavior change); re-run Tenants.UI suite green (670/670).
4. **Live visual check** under `Hexalith.FrontComposer.AppHost` (Aspire): confirm no native grey controls
   remain on any page, the 3 converted Admin.UI buttons are Fluent-styled, and the 2 carve-outs render and
   behave as before. (Carries forward the still-open live-visual residual from the prior passes.)
5. **Memory:** add a `project`-type memory recording the project-wide Fluent UI policy + carve-out registry
   (cross-links the three prior proposals).

**Constraints (project-context + CLAUDE.md):** no direct commits to `main` (feature branch + PR);
Conventional Commits — the Admin.UI conversion is `refactor`/`fix`, **not** `feat` (don't trigger a false
minor bump for a control swap); the governance docs/tests are `docs`/`test`; submodule edits are authorized
for `Hexalith.EventStore` (this proposal) but **not** for any nested submodule; `.slnx` only;
`TreatWarningsAsErrors`; **do NOT commit** unless explicitly requested (submodule changes propagate
ecosystem-wide).

**Success criteria:**
- ✅ `architecture.md` §4 carries the project-wide UI component policy + carve-out registry; `project-context.md` carries the matching AI rule.
- ✅ Per-surface Governance guards exist for Shell, Counter (`Counter.Web`, specimens excluded), and Admin.UI, each green with its documented allowlist; Tenants guard still green.
- ✅ Admin.UI raw interactive controls reduced to the 2 documented carve-outs; the 3 converted buttons render as `FluentButton` with `data-testid`/a11y preserved.
- ✅ All configured tests green in each repo's lane (FrontComposer solution-level default+Governance; EventStore per-project Admin.UI; Tenants 670/670), `DiffEngine_Disabled=true`.
- ⏳ Live visual check under Aspire (carried-forward residual).

---

## Checklist Status (Change Navigation)
- **§1 Trigger & Context:** ✅ Done (1.1 no formal story — stakeholder governance directive · 1.2 new-requirement/standardization, not a defect · 1.3 full `.razor` audit evidence + per-control Admin.UI disposition)
- **§2 Epic Impact:** ✅ N/A (Epics 1–7 Done; no epic add/remove/resequence)
- **§3 Artifact Conflicts:** 3.1 N/A (no PRD) · 3.2 ✅ architecture §4 edit · 3.3 ✅ positive (no UX spec) · 3.4 ✅ tests (guards) + project-context + memory; no public-API/CI/IaC impact
- **§4 Path Forward:** ✅ Option 1 (Direct Adjustment)
- **§5 Proposal Components:** ✅ this document
- **§6 Final Review/Handoff:** ✅ approved (Administrator: "proceed") + implemented this pass. 6.4 sprint-status.yaml: **N/A** (no epic add/remove/renumber).

---

## Implementation complete (2026-06-17)

**Result:** all touched test lanes GREEN; clean Debug builds (TWAE, 0 warnings) across the three repos.
Changes left **uncommitted** (no commit requested). Implemented in **Incremental** mode with the carve-out
decision confirmed live.

### What changed

**FrontComposer repo (governance + enforcement):**
- `_bmad-output/project-docs/architecture.md` — new **§4.1 "UI component policy (project-wide governance)"**
  with the rule + the 4-row carve-out registry.
- `_bmad-output/project-context.md` — new **"Fluent-only UI (project-wide)"** rule under Blazor Shell rules.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` — **new** guard, 2 facts:
  Shell (allowlist `FcHomeCard.razor`) + Counter.Web (excludes `Counter.Specimens`). **2/2 pass.**

**EventStore submodule (Admin.UI — conversion + enforcement):**
- Converted the **3 clean** raw buttons → `FluentButton` using the established `ButtonAppearance.Transparent`
  idiom, preserving `Class`/`aria-label`/`title`/`OnClick`/child content:
  `Components/Shared/JsonViewer.razor` (show-all), `Layout/Breadcrumb.razor` (copy + truncation).
- `tests/Hexalith.EventStore.Admin.UI.Tests/Governance/AdminUiFluentConformanceTests.cs` — **new** guard,
  allowlist `ActivityChart.razor` + `Streams.razor` (the 2 carve-outs).
- `Layout/BreadcrumbTests.cs` — **1 behaviour-preserving test fix**: `<fluent-button>` wraps child content
  with template whitespace, so the truncation-ellipsis assertion now `.Trim()`s before the exact-match
  (the control still renders "..." and is found by `aria-label`). **Admin.UI lane 22/22 pass** (new guard +
  `JsonViewerTests` + `BreadcrumbTests`).

**Tenants submodule:**
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` — doc-comment now references the
  project-wide policy + sibling guards (no behaviour change). **1/1 pass.**

### Carve-outs (NOT converted — documented in architecture.md §4.1 + guard allowlists)
Shell `FcHomeCard` · `Counter.Specimens/FrontComposerTypeSpecimen` (fixtures) · Admin.UI `ActivityChart`
bar-chart bar · Admin.UI `Streams` aggregate-id-copy cell.

### Verification (2026-06-17)
- FrontComposer `FluentConformanceTests`: **2/2** (`-c Debug`, `DiffEngine_Disabled=true`).
- EventStore `Hexalith.EventStore.Admin.UI.Tests` (filtered to the 3 affected classes): **22/22**.
- Tenants `DomainUiFluentConformanceTests`: **1/1**.
- Post-edit source audit: the only raw interactive controls remaining anywhere are the 4 documented
  carve-outs; the 3 converted Admin.UI buttons are gone from the raw-control set.
- Each test project compiled clean under TWAE in Debug (full test assemblies compile together).

### Residuals
1. **Full Release solution build** (`-c Release`) — not run this pass (only the touched test projects, Debug).
2. **Live-visual check** under Aspire — the stack was stopped to release build locks, then **restarted on the
   new DLLs** (`aspire run` on `Hexalith.FrontComposer.AppHost`). Confirm in-browser: the 3 converted Admin.UI
   buttons are Fluent-styled (esp. the full-width `json-viewer__show-all` bar — the highest visual risk of the
   three), and the carve-outs render/behave unchanged.
3. **Commit/PR** — not done (not requested). Per Conventional Commits: governance docs/tests = `docs`/`test`;
   Admin.UI control swap = `refactor` (NOT `feat`). Submodule edits propagate ecosystem-wide — branch + PR per repo.
