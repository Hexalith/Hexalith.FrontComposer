# Sprint Change Proposal — Sidebar shows a single active menu item (Fluent v5 verified)

_Workflow: bmad-correct-course · Date: 2026-06-19 · Mode: Incremental · Author: Administrator_

> Trigger: at `/tenants/users` the left navigation lit **two** items ("All tenants" **and** "User
> lookup"). The user also asked to confirm FrontComposer uses Blazor **Fluent UI v5** Menu and
> Hamburger components.

---

## Section 1 — Issue Summary

**Problem.** The framework-owned sidebar highlighted more than one item at once. On the User-lookup
page (`/tenants/users`) both **All tenants** and **User lookup** rendered the active indicator bar.

**Root cause.** `FluentNavItem.Match` defaults to **`NavLinkMatch.Prefix`**. The Tenants module
registers three sibling nav entries (`Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`):

| Label | Href |
|---|---|
| All tenants | `/tenants` |
| My tenants | `/tenants/my` |
| User lookup | `/tenants/users` |

Because `/tenants` is a **segment-prefix of every** `/tenants/*` route, at `/tenants/users` it
prefix-matched (active) **and** `/tenants/users` exact-matched (active) → two bars. The same
double-highlight occurred on `/tenants/my` and on every tenant detail page.

**Fluent v5 verification — ✅ passes.** This is **not** a wrong-component defect. FrontComposer uses
Fluent UI v5 throughout the shell chrome:
- Sidebar menu → `FluentNav` / `FluentNavCategory` / `FluentNavItem`
- Desktop hamburger → `FluentButton` + `Navigation20` icon (toggles `SidebarCollapsed`)
- Narrow-width hamburger → `FluentLayoutHamburger` (drawer)
- Header account menu → `FluentMenu` (`FcAccountMenu`)

Package pinned at `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1`; `FluentNavItem.Match`
(`NavLinkMatch` Prefix/All) confirmed present in that assembly.

**Discovery.** Direct UX report with a screenshot of `/tenants/users` showing two active bars.

**Issue type:** framework bug fix (Shell-owned active-state rendering). No new requirement.

---

## Section 2 — Impact Analysis

### Epic Impact — none
FrontComposer framework Epics 1–7 are DONE. This change touches only Shell active-state rendering
(FR9 shell frame, NFR6 a11y are *reinforced*, not altered). No epic added, removed, or resequenced.

### Story Impact — none
No formal story (direct UX bug). Recorded here.

### Artifact Conflicts
| Artifact | Impact | Action |
|---|---|---|
| **PRD** | N/A — none exists | none |
| **Architecture** (`_bmad-output/project-docs/architecture.md`) | §4 documented the shell chrome but not the single-active rule | ✅ **edited** (one sentence in the §4 shell paragraph) |
| **Epics / Stories** | unaffected | none |
| **UI/UX spec** | no standalone doc | N/A |
| **Hexalith.Tenants submodule** | **none** — the registration already delegates `active state` to the shell; routes are unchanged | none |
| **Tests** | new active-state regression pins | ✅ added |

### Technical Impact
- Confined to **`Hexalith.FrontComposer.Shell` → `Components/Layout/FrontComposerNavigation.razor(.cs)`**.
- The collapsed 48px rail (`FcCollapsedNavRail`) is **unaffected**: it uses `FluentButton` + `OnClick`
  (no `Href`/NavLink), so it never had an active-state concern.
- No contract change (`FrontComposerNavEntry` untouched), no submodule edit, no new dependency.

---

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment (most-specific-prefix-wins): ✅ Selected.** Effort **Low**, risk **Low**.

The shell computes the registered nav href that is the **longest segment-prefix** of the current URL
and renders **that** item with `NavLinkMatch.Prefix`; **every other** item gets `NavLinkMatch.All`.
Because `NavLinkMatch.All` strips the query string before comparing (default framework switch), this:
- guarantees **at most one** active item;
- keeps **query-string pages** lit (`/tenants/users?userId=…` → still "User lookup");
- keeps **detail pages** lit at their section ancestor (`/tenants/{id}` → "All tenants").

A `NavigationManager.LocationChanged` subscription re-renders the component on client-side navigation
so the `Match` assignment re-evaluates (without it, the previous active item would stay lit after a
SPA nav — the original bug). This aligns with the Tenants registration's own note that *"the
FrontComposer shell owns all rendering (icons, grouping, active state, responsive collapse)."*

- **Option 2 — Minimal `Match=All` everywhere: rejected.** Equally single-active, but a detail/child
  page that isn't a nav entry would show **nothing** lit. Identical behavior to Option 1 for today's
  Tenants routes, but worse for any future parent→detail domain. (User chose Option 1.)
- **Option 3 — Rollback / MVP review: N/A.** Nothing to revert; MVP untouched.

---

## Section 4 — Detailed Change Proposals (implemented)

### 4a. Code — `Hexalith.FrontComposer.Shell`

**`Components/Layout/FrontComposerNavigation.razor`**
- Top of the full-nav branch: `RecomputeActiveNavHref(discovery, navEntries);` (resolves `_activeNavHref`).
- Projection `FluentNavItem`: capture `route` once; add `Match="@MatchFor(route)"`.
- Nav-entry `FluentNavItem`: add `Match="@MatchFor(entry.Enabled ? entry.Href : null)"`.

**`Components/Layout/FrontComposerNavigation.razor.cs`**
- `using Microsoft.AspNetCore.Components.Routing;`; class now `: FluxorComponent, IAsyncDisposable`.
- Inject `NavigationManager`; `_activeNavHref` field.
- `OnInitialized` subscribes to `LocationChanged`; `HandleLocationChanged` re-renders with the
  disposed-circuit guard mirrored from `FrontComposerShell`; `DisposeAsync` unsubscribes.
- `RecomputeActiveNavHref(...)` — collects the same hrefs the sidebar renders (visible projection
  routes + enabled nav-entry hrefs) and picks the longest segment-prefix of the current URL.
- `MatchFor(href)` → `Prefix` for the active href, else `All`.
- `NormalizeHref` (strip query/fragment, single leading slash, trim trailing slash, lowercase) and
  `LongestNavPrefix` (segment-aware, `internal static` for unit tests).

### 4b. Tests — `Hexalith.FrontComposer.Shell.Tests`
- `FrontComposerNavigationTests.cs` — `[Theory]` for `NormalizeHref` (6 cases) and `LongestNavPrefix`
  (6 cases incl. query-string and detail-page).
- `FrontComposerNavigationNavEntryTests.cs` — three render pins over the real Tenants entries:
  `/tenants/users` → exactly one `Prefix` item (`/tenants/users`) and `/tenants` is `All`; detail page
  `/tenants/{id}` → only `/tenants` is `Prefix`; unrelated route → nothing active.

### 4c. Documentation
- `architecture.md` §4 — one sentence on the single-active / longest-prefix rule.
- This Sprint Change Proposal.

### 4d. Verification evidence
- `dotnet build` of `Hexalith.FrontComposer.Shell.Tests` (Debug): **0 warnings / 0 errors** (TWAE clean).
- Navigation suite: **48 passed / 0 failed** (incl. the 15 new active-state cases), `DiffEngine_Disabled=true`.
- Release-clean build + full default lane: see handoff.

---

## Section 5 — Implementation Handoff

**Scope classification: Minor** — one framework Shell component + tests + a one-line doc note. No epic,
PRD, architecture-pattern, contract, or submodule change.

**Recipient: Developer (direct implementation — done in this workflow).**

**Remaining steps:**
1. Commit on a `feat/`… **no — `fix/`** branch (this is a bug fix, not a feature; do not `feat:` it).
   Suggested message: `fix(shell): keep a single active sidebar item via longest-prefix nav match`.
2. Before push: `dotnet build -c Release` clean (TWAE) + default test lane green
   (`dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`, `DiffEngine_Disabled=true`).
3. Optional live check under `Hexalith.FrontComposer.AppHost`: `/tenants/users` shows only "User
   lookup" lit; `/tenants/{id}` keeps "All tenants" lit; `/tenants/users?userId=…` stays lit.

**Constraints (project-context):** no direct commits to `main` (branch + PR); Conventional Commits
(`fix:`); never `--init --recursive` submodules; `.slnx` only; `_bmad-output/` for generated docs.

**Success criteria:**
- At most one active sidebar item at any URL; correct item on query-string and detail pages.
- FrontComposer continues using Fluent v5 nav + hamburger components (verified — unchanged).
- Release build clean + default lane green.

---

## Checklist Status (Change Navigation)
- **§1 Trigger & Context:** ✅ Done — double-active reproduced; Fluent v5 usage verified.
- **§2 Epic Impact:** ✅ N/A (no epic changes).
- **§3 Artifact Conflicts:** PRD N/A · Architecture ✅ edited · Stories N/A · Submodule N/A (shell owns active state) · Tests ✅ added.
- **§4 Path Forward:** ✅ Option 1 (most-specific-prefix-wins) selected & implemented.
- **§5 Proposal Components:** ✅ this document.
- **§6 Final Review/Handoff:** Minor → Developer (implemented + tests green); pending Release-lane confirmation + commit.
