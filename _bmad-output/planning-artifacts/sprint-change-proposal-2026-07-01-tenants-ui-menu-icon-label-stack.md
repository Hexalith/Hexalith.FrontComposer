---
workflow: bmad-correct-course
date: 2026-07-01
mode: Batch
status: Approved
change_trigger: Tenants UI navigation rail label rendered beside the icon instead of below it
scope_classification: Minor
approval: Approved by Administrator on 2026-07-01
source_artifact: _bmad-output/implementation-artifacts/spec-tenants-ui-menu-icon-label-stack.md
---

# Sprint Change Proposal: Tenants UI Menu Icon/Label Stack

## 1. Issue Summary

The Tenants UI navigation rail inherited the unified `FrontComposerNavigation` tile layout from
Story 8.5, but the visible label could render beside the icon instead of below it. That weakened the
Aspire-style rail pattern Story 8.5 introduced: a compact rail where each context tile reads as an
icon-over-label app-bar item, not a horizontal button row.

Evidence:

- Implementation artifact: `_bmad-output/implementation-artifacts/spec-tenants-ui-menu-icon-label-stack.md`.
- Source evidence: `FrontComposerNavigation.razor` now wraps each bounded-context tile's icon and
  label in a vertical `FluentStack`, and applies the same structure to orphan navigation contexts.
- Badge evidence: count and "New" badges remain in a separate overlay row, so they do not become part
  of the icon/label stack.
- Test evidence: `FrontComposerNavigationTests` now pins the vertical stack and verifies badges remain
  outside the icon/label stack.
- Verification run: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter
  "FullyQualifiedName~FrontComposerNavigationTests"` passed `37` Shell tests with `0` failures. The
  run emitted one transient MSBuild copy retry warning for a SourceTools `.pdb`, then completed
  successfully.

## 2. Impact Analysis

Epic impact:

- Current epic: **Epic 8, Story 8.5: Icon+label navigation rail + projection flyout**.
- Epic 8 remains valid. This is a minor clarification and implementation correction inside Story 8.5,
  not a new epic and not a resequencing event.
- Epics 1-7 are unaffected.

Story impact:

- Story 8.5 should be tightened to state that the 72px labeled rail stacks the icon above the visible
  label through a Fluent layout primitive.
- Story 8.5 should also state that aggregate count and "New" badges are indicator overlays outside the
  icon/label content stack.

Artifact impact:

- PRD: no authored PRD exists. `epics.md` is the reverse-engineered requirements inventory and PRD
  proxy for this brownfield plan; no MVP scope change is needed.
- Epics: Story 8.5 acceptance wording should be amended for the icon-over-label layout and badge
  placement.
- Architecture: the Epic 8 navigation note can be clarified from "icon+label rail" to
  "icon-over-label rail" to remove ambiguity.
- UI/UX docs: the navigation reference can clarify the labeled rail tile structure.
- Code/tests: implementation is already present in `FrontComposerNavigation.razor`,
  `FrontComposerNavigation.razor.css`, and `FrontComposerNavigationTests.cs`.

Technical impact:

- No state, routing, EventStore, MCP, SourceTools, schema fingerprint, or package/deployment changes.
- The change remains inside Shell navigation markup/CSS and focused bUnit tests.
- It follows Hexalith UI rules: Fluent v5 components first, layout via `FluentStack`, no legacy Fluent
  v4/FAST tokens, and CSS limited to layout/positioning constraints.

## 3. Recommended Approach

Selected path: **Direct Adjustment**.

Rationale:

- The issue is a bounded UX regression caused by underspecified tile layout details in Story 8.5.
- The existing Epic 8 direction remains correct; the fix clarifies the intended Aspire-style visual
  contract rather than changing scope.
- Rollback is not useful because the unified rail and flyout remain the desired design.
- MVP review is not required because this is post-MVP chrome parity polish.

Effort estimate: **Low**.

Risk level: **Low**. The primary risk is future regression if "icon+label" remains ambiguous; the
story and docs wording plus focused tests close that gap.

Timeline impact: **None** to current epics. This is direct Developer-agent work already implemented,
with optional planning/documentation wording follow-through.

## 4. Detailed Change Proposals

### 4.1 `epics.md` - Tighten Story 8.5 Acceptance Criteria

Section: `Story 8.5: Icon+label navigation rail + projection flyout`

OLD:

```md
**Given** a bounded-context tile,
**Then** it shows a `FluentIcon` (rest = outline, active = filled) + (labeled) short name + aggregate-count
badge; the active context shows an accent left-bar + `aria-current`.
```

NEW:

```md
**Given** a bounded-context tile in the 72px labeled rail,
**Then** the tile content stacks the `FluentIcon` above the short label through a Fluent layout
primitive, while aggregate count and "New" badges render outside that icon/label stack as an overlay
indicator row; the active context uses the filled icon, accent left-bar, and `aria-current`.

**Given** a bounded-context tile in the 48px icon-only rail,
**Then** the icon remains centered and the tile keeps an accessible name through `aria-label`/tooltip;
badges remain outside the icon content stack.
```

Rationale: removes the ambiguity that allowed icon and label to render horizontally while preserving
the existing Story 8.5 rail-width and active-state contract.

### 4.2 `architecture.md` - Clarify Epic 8 Navigation Contract

Section: `4.1 UI component policy`, Epic 8 paragraph

OLD:

```md
Epic 8 also restyles the navigation into an
**icon+label rail with a projection flyout** (Story 8.5, refining UX-DR3), tightens **default density** +
grid (Story 8.4), adds a reusable **`FcPageToolbar`** (Story 8.6), and **amends UX-DR2** ...
```

NEW:

```md
Epic 8 also restyles the navigation into an
**icon-over-label rail with a projection flyout** (Story 8.5, refining UX-DR3): labeled rail tiles stack
the icon above the visible label through a Fluent layout primitive, while count and "New" badges stay
as separate overlay indicators outside that icon/label stack. Epic 8 also tightens **default density** +
grid (Story 8.4), adds a reusable **`FcPageToolbar`** (Story 8.6), and **amends UX-DR2** ...
```

Rationale: keeps the architecture source of record aligned with the corrected Story 8.5 behavior.

### 4.3 `docs/reference/components/navigation.md` - Clarify Tile Layout

Section: `Overview`

OLD:

```md
At Desktop it renders as a 72 px labelled rail or a 48 px icon-only rail, controlled by the shell's
hamburger and navigation state; CompactDesktop also uses the 48 px rail.
```

NEW:

```md
At Desktop it renders as a 72 px labelled rail or a 48 px icon-only rail, controlled by the shell's
hamburger and navigation state; CompactDesktop also uses the 48 px rail. In the 72 px rail, each
context tile stacks the icon above the visible label, while count and "New" badges stay as separate
overlay indicators.
```

Rationale: adopter-facing docs should describe the visible rail contract now that it is regression-tested.

### 4.4 Implementation Handoff Evidence

Already implemented:

- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor`
  - bounded-context tiles use a vertical `FluentStack` for icon and label.
  - orphan navigation context tiles use the same vertical stack.
  - badge indicators render in a sibling `FluentStack`, separate from tile content.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.css`
  - CSS remains layout-only: tile positioning, badge overlay placement, active accent thread, and label
    text alignment.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs`
  - desktop rail test verifies the tile content stack is vertical.
  - badge regression test verifies count/New badges are not inside the icon/label content stack.

No rollback or additional implementation story is recommended.

## 5. Checklist Outcome

- [x] 1.1 Trigger identified: `_bmad-output/implementation-artifacts/spec-tenants-ui-menu-icon-label-stack.md`.
- [x] 1.2 Core problem defined: Story 8.5 under-specified icon/label tile orientation; issue type is a
  misunderstanding/underspecification of the original UX requirement.
- [x] 1.3 Evidence gathered: source, CSS, tests, implementation artifact, and targeted test run.
- [x] 2.1 Current epic assessed: Epic 8 remains viable.
- [x] 2.2 Epic-level changes assessed: no new epic; amend Story 8.5 wording only.
- [x] 2.3 Remaining epics reviewed: no impact to Epics 1-7 or other Epic 8 stories.
- [x] 2.4 Future epic invalidation checked: none.
- [x] 2.5 Priority/order checked: no resequencing needed.
- [!] 3.1 PRD impact assessed: no authored PRD exists; `epics.md` remains the PRD proxy and should be
  clarified.
- [x] 3.2 Architecture impact assessed: minor wording clarification recommended.
- [x] 3.3 UI/UX impact assessed: navigation reference should state icon-over-label tile structure.
- [x] 3.4 Secondary artifacts assessed: no CI/deployment/IaC impact; tests already cover the regression.
- [x] 4.1 Direct adjustment viable: effort Low, risk Low.
- [N/A] 4.2 Rollback path: not useful.
- [N/A] 4.3 MVP review: not required.
- [x] 4.4 Recommended path selected: Direct Adjustment.
- [x] 5.1-5.5 Proposal, artifact impacts, action plan, and handoff captured here.
- [x] 6.3 User approval: approved by Administrator on 2026-07-01.
- [N/A] 6.4 Sprint-status update: no epic/story status changes required; approved wording updates were
  applied to `epics.md`, `architecture.md`, and `docs/reference/components/navigation.md`.
- [x] 6.5 Handoff plan defined.

## 6. Implementation Handoff

Scope classification: **Minor**.

Route to: **Developer agent**.

Responsibilities:

- Treat the code/test implementation referenced above as complete and approved for handoff.
- Approved wording updates were applied to `epics.md`, `architecture.md`, and
  `docs/reference/components/navigation.md`.
- Keep changes within FrontComposer-owned files; do not modify Tenants submodule files.
- Focused navigation tests passed; full docs validation is blocked by existing DocFX metadata loading
  errors unrelated to the navigation Markdown edit.

Success criteria:

- Labeled rail tiles render icon above label in the Tenants host and any FrontComposer adopter host.
- Count and "New" badges remain outside the icon/label stack.
- The regression remains covered by `FrontComposerNavigationTests`.
- Story 8.5 and reference docs no longer allow horizontal icon/label interpretation.
