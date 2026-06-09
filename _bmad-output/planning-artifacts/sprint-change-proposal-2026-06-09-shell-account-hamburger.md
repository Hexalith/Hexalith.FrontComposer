# Sprint Change Proposal — Shell header account avatar + always-visible navigation hamburger

_Workflow: bmad-correct-course · Date: 2026-06-09 · Mode: Incremental · Author: Administrator_

> Companion to `sprint-change-proposal-2026-06-09-fluent-v5-domain-ui.md`. That change made the
> Tenants page **bodies** Fluent v5; **this** change fixes the **shell header chrome** (account
> affordance + hamburger) and closes that proposal's documented *"live visual check under Aspire"*
> residual. Triggers selected by the user: **submodule divergence**, **avatar + hamburger scope**,
> **live-visual residual**, **D9 reversal**.

---

## Section 1 — Issue Summary

**Problem.** The Tenants UI at `/tenants` did not read as a Fluent UI Blazor site in two specific ways:

1. **Sign in/out was a bare `<a>` link in the content area**, not the header. The Tenants
   `MainLayout.razor` rendered a bespoke `.tenants-auth-bar` as the first child of `@Body` — a plain
   hyperlink under the header, with no avatar.
2. **No hamburger was visible at desktop width.** `FcHamburgerToggle` was deliberately hidden at the
   `Desktop` viewport tier (`IsVisible => CurrentViewport != Desktop`, decision **"D9"**), so a
   wide-desktop user never saw a navigation toggle — only the per-category collapse chevron.

**Discovery.** Direct UX request during manual review of the running stack
(*"https://localhost:62445/tenants does not look and feel like Blazor Fluent UI sites … sign in/out
should be on the header bar with an avatar … the navigation menu should have a hamburger"*).

**Issue type:** new UX requirement + a framework enhancement + a deliberate **design-decision reversal (D9)**.

**Two process issues surfaced during execution:**
- **Submodule divergence.** The app the user was viewing (port 62445) was served by
  **`Hexalith.Parties.AppHost`** (in `/home/administrator/projects/hexalith/parties/`), which builds
  from its **own** submodule checkouts `/parties/Hexalith.FrontComposer` + `/parties/Hexalith.Tenants`
  — **separate working trees** from where the edits were made (`/frontcomposer/…`). The running app
  would never have reflected the changes. The host that *does* consume the edited source is the
  **`Hexalith.FrontComposer.AppHost`** (its `tenants-ui` resource).
- **Live-visual residual** (carried from the Fluent-v5 proposal): the app was stopped at verification
  time, so the final screenshot check is still pending.

**Evidence.** Screenshot of the bare content-area "Sign in" link; `FcHamburgerToggle.razor.cs`
`IsVisible => CurrentViewport != Desktop`; `ps` output proving the running AppHost was Parties (DAPR
sidecars for eventstore/tenants/parties) building from `/parties/*` submodule copies; green local
test runs (below).

---

## Section 2 — Impact Analysis

### Epic Impact — none
FrontComposer framework **Epics 1–7 are DONE** and already built on Fluent v5. This change touches
Shell territory (**FR9** shell frame, **FR15** settings/preferences, **NFR6** a11y) but **modifies,
adds, removes, or resequences no epic**. Domain-UI conformance lives in the `Hexalith.Tenants`
submodule and is tracked via sprint-change-proposals, not `epics.md`.

### Story Impact
- No formal story (direct UX request). Recorded here + as a **D9-superseding note** in
  `2-2-registry-driven-navigation-and-home-directory.md` (where D9 is documented).

### Artifact Conflicts
| Artifact | Impact | Action |
|---|---|---|
| **PRD** | N/A — none exists; `epics.md` FRs (FR9 / NFR6) *reinforced*, not altered | none |
| **Architecture** (`_bmad-output/project-docs/architecture.md`) | Consistent with §4; needed a governance line for the new shell-owned account affordance + the D9 reversal | ✅ **edited** (§4 paragraph) |
| **Story 2-2** | Documents D9 ("hidden at Desktop") — now superseded | ✅ **edited** (superseding note) |
| **UI/UX spec** | No standalone UX doc exists | N/A (folded into architecture note) |
| **Tests** | bUnit hamburger/account-menu, test base auth context, Tenants composition, e2e sidebar | ✅ updated |
| **Localization** | New shell strings (EN + FR) | ✅ added |

### Technical Impact
- **Framework Shell** gains an always-present account control and an always-visible hamburger that
  reuses existing `SidebarToggledAction` machinery (width swap + collapsed-rail swap + persistence
  already wired). Reverses **D9**.
- **bUnit blast radius (resolved):** the shell now always reads `AuthenticationStateProvider`; bUnit's
  placeholder provider throws unless a test opts into auth. Fixed once in `LayoutComponentTestBase`
  (default **anonymous** bUnit auth context → "Sign in" state), keeping all shell-render tests green.
- **Cross-repo:** changes span the FrontComposer repo, the Tenants submodule, and (per decision) the
  Parties repo's submodule pointers.

---

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment (Hybrid): ✅ Selected.** Effort **Low**, risk **Low–Medium**.
The code is implemented and tested; the remainder is documentation (done), cross-submodule
propagation, and the live-visual check.

- **Option 2 — Rollback: rejected.** Changes are wanted and green; nothing to revert.
- **Option 3 — PRD/MVP review: N/A.** MVP and epics are untouched.

**Rationale:** Lowest-effort path that satisfies the UX request, keeps the framework consistent for
all adopters (auth affordance is framework-owned, not duplicated per domain), and uses existing
sidebar-collapse state rather than new machinery. The D9 reversal is intentional and recorded.

---

## Section 4 — Detailed Change Proposals

### 4a. Code (already implemented — change record)

**`Hexalith.FrontComposer.Shell` (FrontComposer repo):**
- `Components/Layout/FcAccountMenu.razor` + `.razor.cs` *(new)* — `FluentAvatar` trigger → `FluentMenu`
  with Sign in / Sign out; reads `AuthenticationStateProvider`; navigates to
  `IOptions<FrontComposerAuthenticationOptions>.Redirect.{LoginPath,LogoutPath}` with `forceLoad:true`.
- `Components/Layout/FrontComposerShell.razor` — renders `<FcAccountMenu />` **always**, right-most in
  the header stack (survives adopter `HeaderEnd` customization).
- `Components/Layout/FcHamburgerToggle.razor` + `.razor.cs` — **always visible**; at Desktop renders a
  subtle `FluentButton` whose click dispatches `new SidebarToggledAction(UlidFactory.NewUlid())`; at
  other tiers keeps `FluentLayoutHamburger` (drawer). Removed `IsVisibleForTest` (CA1822); added
  `IsDesktop`.
- `Components/Icons/FcFluentIcons.cs` — added `Navigation20()` hamburger glyph + `TryCreate` arm.
- `Resources/FcShellResources.resx` + `.fr.resx` — `AccountMenuAriaLabel`, `SignInLabel`,
  `SignOutLabel`, `SignedInAsLabel` (EN + FR, parity-test compliant).

**`Hexalith.Tenants.UI` (Tenants submodule — edit pre-approved in the plan):**
- `Components/Layout/MainLayout.razor` — collapsed to `<FrontComposerShell>@Body</FrontComposerShell>`;
  bespoke `.tenants-auth-bar` removed (auth now framework-owned).

**Tests:**
- `FcHamburgerToggleTests.cs` — Desktop assertions flipped (visible + click toggles `SidebarCollapsed`).
- `FcAccountMenuTests.cs` *(new)* — anonymous → Sign in affordance present.
- `LayoutComponentTestBase.cs` — added default-anonymous bUnit `Authorization` context.
- `FrontComposerNavigationNavEntryTests.cs` — reuse the base's `Authorization` (no double-registration).
- `tests/e2e/specs/sidebar-responsive.spec.ts` — Desktop hamburger now `toBeVisible()`.
- `Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` (submodule) — auth-bar assertion →
  `ShouldNotContain("tenants-auth-bar")`.

### 4b. Documentation (applied in this workflow)
- `architecture.md` §4 — governance line (account control + always-visible hamburger + D9 superseded).
- `2-2-registry-driven-navigation-and-home-directory.md` — D9-superseded note.

### 4c. Verification evidence
- **Shell:** all affected tests pass (targeted run: **111 passed**). Full Shell suite's only failures
  are **17 pre-existing** (CI-governance fixtures, Counter snapshots, an auth-boundary architecture
  test, a navigation-effects test) — **confirmed pre-existing** by re-running them with these changes
  stashed → **zero new regressions**.
- **Tenants.UI: 670/670 pass.**
- **`Hexalith.FrontComposer.AppHost` builds end-to-end** with the changes.
- **Live-visual: OPEN** (app was stopped; see handoff).

---

## Section 5 — Implementation Handoff

**Scope classification: Moderate** — spans framework Shell + a submodule + a second consuming repo
(Parties) + a documented-decision reversal + cross-repo submodule-pointer coordination. (Not Major:
no epic/PRD/architecture-pattern change, MVP unaffected.)

**Action plan (sequenced):**

1. **Tenants submodule** (`/frontcomposer/Hexalith.Tenants`) — commit `MainLayout.razor` +
   `TenantsUiCompositionTests.cs` on a `feat/` branch → PR → merge. Conventional commit, e.g.
   `feat(ui): move sign in/out into framework shell account menu`. (Tenants tests: solution build for
   restore, per-project `dotnet test`; run with `DiffEngine_Disabled=true`.)
2. **FrontComposer repo** (`/frontcomposer`) — commit Shell + tests + the two `_bmad-output` doc edits
   on a `feat/` branch → PR → merge. E.g.
   `feat(shell): add header account avatar menu and always-visible nav hamburger (supersedes D9)`.
   Bump the `/frontcomposer` `Hexalith.Tenants` submodule pointer to step 1's merged commit.
   Verify Release build clean (`TreatWarningsAsErrors`) + default test lane green before push.
3. **Propagate to Parties** (`/parties`, per the user's decision) — after steps 1–2 merge, bump the
   `/parties/Hexalith.FrontComposer` **and** `/parties/Hexalith.Tenants` submodule pointers to the new
   commits; commit the pointer bump on a `feat/`/`chore/` branch → PR.
4. **Live-visual verification** — start the **`Hexalith.FrontComposer.AppHost`** (dashboard
   `https://localhost:17217`; its `tenants-ui` resource uses the edited source). Confirm: header
   **avatar** opens Sign in / Sign out; **hamburger** visible at desktop and toggles full nav ↔ 48px
   rail; the old content-area "Sign in" link is gone; narrow-width drawer hamburger still works.
   Then re-run the Parties AppHost to confirm propagation. (AppHost changes require restarting
   `aspire run`.)

**Recipients:**
- **Developer** — steps 1–2 (commits/PRs in FrontComposer + Tenants), step 4 (verification).
- **Developer / repo owner of `/parties`** — step 3 (submodule-pointer propagation).

**Constraints (from project-context):** no direct commits to `main` (feature branch + PR);
Conventional Commits (don't `feat:` a refactor); never `--init --recursive` submodules; submodule
edits are pre-approved here; `.slnx` only.

**Success criteria:**
- All configured tests green in each repo's CI lane (FrontComposer solution-level + traits; Tenants
  per-project Tier 1/2).
- Live `/tenants` shows the header avatar + working hamburger under **both** the FrontComposer AppHost
  and (after propagation) the Parties AppHost.
- `architecture.md` + Story 2-2 reflect the D9 reversal (done).

---

## Checklist Status (Change Navigation)
- **§1 Trigger & Context:** ✅ Done (1.1–1.3)
- **§2 Epic Impact:** ✅ N/A (no epic changes)
- **§3 Artifact Conflicts:** 3.1 N/A · 3.2 ✅ edited · 3.3 N/A (no UX doc) · 3.4 ✅ tests/resx done, propagation = handoff
- **§4 Path Forward:** ✅ Option 1 (Hybrid) selected
- **§5 Proposal Components:** ✅ this document
- **§6 Final Review/Handoff:** pending user approval (below)
