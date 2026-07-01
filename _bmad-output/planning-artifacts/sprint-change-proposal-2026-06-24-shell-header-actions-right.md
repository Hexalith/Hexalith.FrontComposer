# Sprint Change Proposal — Shell header actions flush-right + user name relocated to account menu

- **Date:** 2026-06-24
- **Author:** Administrator (via Correct Course workflow)
- **Trigger type:** UI defect + minor UX refinement (direct UX request, running app `localhost:62445`)
- **Change scope:** **Minor** — direct implementation by Developer agent
- **Path forward:** Option 1 — Direct Adjustment (Effort **Low**, Risk **Low**)
- **Status:** **APPROVED** by Administrator on 2026-07-01. Implementation evidence is present in the current FrontComposer source tree.

---

## Section 1 — Issue Summary

While running the app, two header-bar issues were observed (see attached screenshot of the Tenants
/ "Locataires" page):

1. **Header action icons sit mid-bar, not flush-right.** The right-side action cluster (theme
   toggle "Clair", command-palette search, settings gear, account avatar "Admin User / AU") renders
   roughly centered in the header rather than aligned to the right edge.

2. **The connected user's name is duplicated in the header.** "Admin User" renders inline beside the
   avatar in the header bar. The desired end state is for the user name to appear **only inside the
   avatar menu**, as a disabled entry immediately above the sign-out ("disconnect") item.

### Evidence / root cause

- **Issue 1 (layout):** In `FrontComposerShell.razor`, the header is a `FluentStack` with
  `HorizontalAlignment.SpaceBetween` wrapping two child `FluentStack`s (left: hamburger + title +
  breadcrumb center; right: theme/dev/palette/settings/account). The right child has **no explicit
  `Width`**, so it inherits Fluent's `Width="100%"` default. Under `SpaceBetween`, two
  100%-width flex children split the bar ~50/50, and the right child left-aligns its content
  (FluentStack default `HorizontalAlignment=Start`) — landing the icons in the **middle** of the
  bar. This is the documented FluentStack pitfall (architecture §4.3 / project note
  `fluent-layout-components-43`): a stack replacing an `inline-flex` cluster must be set to
  `Width="fit-content"`.

- **Issue 2 (duplication):** `FcAccountMenu.razor` renders the user name **twice** when
  authenticated — once inline in the header (`<span class="fc-account-menu__name"
  data-testid="fc-account-name">`, line 23) and once as a disabled item inside the `FluentMenu`
  (`data-testid="fc-account-user"`, line 34, already followed by a `FluentDivider` and the sign-out
  item). The requested menu structure **already exists**; the change is to **remove the inline
  header copy** so the name lives solely in the menu.

---

## Section 2 — Impact Analysis

| Artifact | Impact | Action |
|---|---|---|
| **Epics** (`epics.md`) | None — Framework Epics 1–7 DONE; touches only Shell chrome under existing **UX-DR8** (account control) | **N/A** |
| **PRD** | No PRD exists; `epics.md` FR/NFR unchanged. NFR6 (a11y) preserved — name stays accessible via the disabled menu item + avatar `Title` | **N/A** |
| **Architecture** (`architecture.md` §4, §4.3) | Already documents a header "right cluster" + always-rendered `FcAccountMenu`, and mandates the `Width="fit-content"` FluentStack idiom. Fix **applies** the documented pattern | **Reinforced, not altered** |
| **UI/UX** | `FrontComposerShell.razor` right cluster + `FcAccountMenu.razor`/`.css` | **Edited** (Edits 1–3) |
| **Tests** | `FcAccountMenuAuthenticatedTests.cs` asserts the inline `fc-account-name` is present | **Edited** (Edit 4 — flip to regression guard) |
| **CI / IaC / docs site** | None | **N/A** |

**Technical impact:** Layout-only + dead-CSS removal + one test-assertion flip. No state, no
contracts, no server-security wiring, no generated code. No Governance/Contract guard touches header
action alignment or the inline-name span, so no guard allowlist changes.

---

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment.** Modify the two Shell components in place, prune the dead CSS rule,
and update the one regression test. No new story or epic is required (consistent with the
2026-06-09 shell-account-hamburger precedent, which recorded shell UX tweaks directly in a
sprint-change-proposal rather than `epics.md`).

- **Effort:** Low (≈4 small edits, 1 component build + targeted test run)
- **Risk:** Low (no behavior beyond layout/visibility; a11y preserved)
- **Option 2 (Rollback):** N/A — nothing to revert.
- **Option 3 (MVP review):** N/A — MVP and epics untouched.

---

## Section 4 — Detailed Change Proposals

### Edit 1 — `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` (line ~71)

Right-side header action cluster: shrink to content so `SpaceBetween` pushes it to the right edge.

```razor
OLD:
    <FluentStack Horizontal="true" VerticalAlignment="VerticalAlignment.Center">
        <FcThemeToggle />

NEW:
    <FluentStack Horizontal="true" VerticalAlignment="VerticalAlignment.Center" Width="fit-content">
        <FcThemeToggle />
```

> Note: target the **second** identical `<FluentStack Horizontal="true" …>` (the right cluster,
> containing `<FcThemeToggle />`), not the first (left cluster: hamburger + app title). The left
> stack stays `Width="100%"` so the title/breadcrumb center keep their room.

### Edit 2 — `src/Hexalith.FrontComposer.Shell/Components/Layout/FcAccountMenu.razor` (line 23)

Remove the inline header name; it remains in the disabled menu item (line 34) + avatar `Title`.

```razor
REMOVE:
    <span class="fc-account-menu__name" data-testid="fc-account-name" title="@SignedInAsText">@UserName</span>
```

### Edit 3 — `src/Hexalith.FrontComposer.Shell/Components/Layout/FcAccountMenu.razor.css`

Remove the now-orphaned `.fc-account-menu__name` rule and its narrow-viewport `@media` block
(lines ~5–20). Keep `.fc-account-menu__menu-user` and the wrapper comment.

```css
REMOVE:
    .fc-account-menu__name { font-size: 0.875rem; font-weight: 500; max-inline-size: 14rem;
        overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    @media (max-width: 36rem) { .fc-account-menu__name { display: none; } }

KEEP:
    .fc-account-menu__menu-user { font-weight: 600; }
```

### Edit 4 — `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcAccountMenuAuthenticatedTests.cs` (line 43)

Flip the inline-name assertion into a regression guard; lines 44 & 47 (name present in the
`fc-account-user` menu item) still pass unchanged.

```csharp
OLD:  cut.Markup.ShouldContain("data-testid=\"fc-account-name\"");
NEW:  cut.Markup.ShouldNotContain("data-testid=\"fc-account-name\"");
```

Also update the class XML-doc summary (lines ~15–20): the display name now surfaces "as the avatar
initials and menu title" — drop "the header label".

---

## Section 5 — Implementation Handoff

- **Scope classification:** **Minor** → Developer agent, direct implementation.
- **Recipient:** `bmad-dev-story` / Developer (Amelia).
- **Definition of Done:**
  1. Edits 1–4 applied.
  2. `dotnet build -c Release` clean (TreatWarningsAsErrors).
  3. Targeted test run green:
     `dotnet test Hexalith.FrontComposer.slnx --filter "FullyQualifiedName~FcAccountMenu" -e DiffEngine_Disabled=true`
     plus the default lane (`Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined`).
  4. Visual confirmation in the running app: action icons flush-right; header shows avatar only
     (no inline name); avatar menu shows disabled name → divider → Sign out.
  5. `/bmad-code-review` before flipping to Done (per project workflow rule).
- **Commit:** `fix(shell): right-align header actions and move user name into the account menu`
  (`fix` → patch; not `feat`).
- **Branch:** `fix/shell-header-actions-right` (no direct commit to `main`).

### Optional follow-up (not blocking)
- Consider a lightweight bUnit assertion in `FrontComposerShellTests` that the header right cluster
  renders `width: fit-content`, to lock Issue-1 against regression. Deferred to the dev's judgment —
  no existing guard covers header action alignment today.

---

## Success Criteria

- Header action icons (theme, palette, settings, avatar) sit flush against the header's right edge.
- The connected user's name no longer renders inline in the header bar.
- The avatar menu shows: **{user name} (disabled)** → divider → **Sign out**, unchanged.
- All configured tests pass; Release build clean.

---

## Approval and Handoff Log

- **2026-07-01 — Approved by Administrator.** User directive: `$bmad-correct-course approve change proposals`.
- **Scope classification:** Minor.
- **Routed to:** Developer agent for direct implementation.
- **Implementation evidence observed in this approval pass:** the Shell header right action cluster has `Width="fit-content"`, the inline `fc-account-name` header label is absent from `FcAccountMenu`, and `FcAccountMenuAuthenticatedTests` now guards against reintroducing `data-testid="fc-account-name"`.
- **Sprint-status:** N/A; no epic/story add, remove, or renumber is required.
- **Validation note:** approval was documented from source inspection; build/test commands were not re-run in this approval pass.
