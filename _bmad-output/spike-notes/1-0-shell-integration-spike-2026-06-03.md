---
title: 'Story 1.0 — Shell-integration spike note'
date: '2026-06-03'
story: '1.0'
status: 'complete'   # every 🔴 AR5 API question resolved-or-escalated
spike_owner: 'Tenants dev (FrontComposer supports)'
---

# Shell-integration spike — findings

> Time-boxed SPIKE. The deliverable is **this note**; the throwaway host was discarded and
> **no `src/` files changed** (see Cleanup). The Shell, generator, registry, manifests,
> projection routing and `FC-TBL` DataGrid already exist in the brownfield repo — this spike
> *exercised and confirmed* them against a from-scratch consuming host.

## Host setup

- **Location (outside `src/`):** `artifacts/spike-1-0/` — a minimal Blazor-Server host, gitignored
  (`artifacts/*`), never added to `Hexalith.FrontComposer.slnx`, now deleted.
- **Marker domain:** reused `Counter.Domain` (`[BoundedContext("Counter")]` marker `CounterDomain`,
  `[Projection] CounterProjection`, three `[Command]` types) — the fastest path per Dev Notes.
- **Bootstrap ordering used (AC#1, verbatim from `samples/Counter/Counter.Web/Program.cs`):**
  `AddRazorComponents().AddInteractiveServerComponents()` → `AddFluentUIComponents()` →
  `AddHexalithFrontComposerQuickstart(...)` → `AddHexalithDomain<CounterDomain>()` →
  stub `AddHexalithEventStore(o => { BaseAddress = http://localhost:9/; RequireAccessToken = false; })`.
- **Boots?** ✅ Yes. `HTTP 200` on `/`, `/home`, and the generated FullPage command route.
- **`ValidateScopes` clean?** ✅ Yes — host built with
  `UseDefaultServiceProvider(o => { o.ValidateScopes = true; o.ValidateOnBuild = true; })`;
  no scoped-capture / build-validation errors at boot (ADR-030 `IStorageService` Scoped lifetime holds).

## API questions

| # | Question | Status | Answer / Escalation (owner) | Evidence (file:line / observation) |
|---|----------|--------|-----------------------------|-------------------------------------|
| 1 | `AddHexalithFrontComposer*` registration path boots an empty shell | **Resolved** | Quickstart → Domain → stub EventStore boots and renders the empty shell. `Quickstart` = `AddLocalization` + `AddAuthorizationCore` + `AddHexalithShellLocalization` + `AddHexalithFrontComposer` in one call. Order matters: EventStore `TryAdd`s the registry, so Quickstart must run first to install the authoritative `FrontComposerRegistry`. | `ServiceCollectionExtensions.cs:476` (Quickstart), `:165` (FrontComposer), `EventStoreServiceExtensions.cs:32`; live host `GET / → HTTP 200` rendering `fc-shell` |
| 2 | Manifest discovery → `GetManifests()` | **Resolved** | `AddHexalithDomain<T>` reflects `typeof(T).Assembly`, finds every `*Registration` type exposing a static `Manifest : DomainManifest` **and** static `RegisterDomain(IFrontComposerRegistry)`, and registers a `DomainRegistrationAction` per type. `FrontComposerRegistry`'s constructor applies all actions, so `GetManifests()` is populated by construction. Empirically returned **2 manifests** (≥1 required). `DomainManifest` shape = `(Name, BoundedContext, IReadOnlyList<string> Projections (FQN), IReadOnlyList<string> Commands (FQN), IReadOnlyDictionary<string,string> CommandPolicies)`. | `ServiceCollectionExtensions.cs:72-136`; `FrontComposerRegistry.cs:17-35,74`; generated `Counter.Domain.CounterProjectionRegistration.g.cs`; `DomainManifest.cs:12`; runtime `registry-evidence.txt` (count=2) |
| 3 | Projection-route reachability (+ companion opt-in?) | **Resolved** | Generated **projections** are embeddable components (`CounterProjectionView : ComponentBase`, **no `[Route]`**) — an adopter mounts them in their own page **or** reaches them through the registry-driven Shell home (`FcHomeRouteView` at `/` and `/home`, `GET /home → HTTP 200`, lists `Counter`/`CounterProjection`). Generated **FullPage commands** *do* emit a route: `[Route("/commands/{BoundedContext}/{CommandName}")]` (`GET /commands/Counter/ConfigureCounterCommand → HTTP 200`, renders a `<form>`). **Companion opt-in:** the default `FrontComposerRegistry` **already implements** `IFrontComposerFullPageRouteRegistry` (+ write-access + policy companions — all `True` at runtime), and its `HasFullPageRoute` is an inert placeholder returning `true` for every command in a manifest (Story 3-4 D21 / DN6). So with the stock registry, no extra companion wiring is needed; a *custom* `IFrontComposerRegistry` would need to add the companion to get non-permissive route filtering (Story 9-4 will enforce). | `FcProjectionRoutes.cs` (status-filter click-through only), `CommandRouteBuilder.cs:77` (`/domain/{kebab}` helper — see F2), `IFrontComposerRegistry.cs:8-19`, `IFrontComposerFullPageRouteRegistry.cs`, `FrontComposerRegistry.cs:12,96-111`; generated `...ConfigureCounterCommand.CommandPage.g.razor.cs:16`; runtime route probes |
| 4 | `FC-TBL` column/filter/expand surface | **Resolved (with escalation to Story 2.8)** | Adopter entry point = the generated projection `*View` (driven by `[Projection]` + column attributes `[Display(Name=…)]`, `[RelativeTime]`, …), built on FluentUI v5 `FluentDataGrid`. It composes **12 `public ComponentBase`** sub-components under `Shell/Components/DataGrid/`: filtering — `FcColumnFilterCell` (`ColumnHeader`, `InitialValue`), `FcFilterSummary` (`ViewKey`, `HumanisedColumnHeaders`, `SortColumn`, `SortDescending`), `FcFilterResetButton` (`ActiveFilterCount`), `FcFilterEmptyState`, `FcStatusFilterChips`, `FcProjectionGlobalSearch` (cross-column search box); expand-in-row — `FcExpandInRowDetail` (`PanelId`, `SuppressedAnnouncement`), `FcExpandedRowHiddenBanner` (WCAG 4.1.2 live-region); wide-grid — `FcColumnPrioritizer` (`HiddenColumns`, `MaxVisibleColumns`=10; >15 cols → HFC1028/HFC1029); status notices — `FcSlowQueryNotice`, `FcMaxItemsCapNotice`, `FcNewItemIndicator`. **Gap (→ Story 2.8, owner FrontComposer + Product/UX):** these are public CLR types but are **not pinned in any `PublicAPI.Shipped.txt`** (only `Hexalith.FrontComposer.Testing` has one). Story 2.8 must mark FC-TBL confirmed-stable and either freeze the surface in a Shell `PublicAPI.Shipped.txt` or make the sub-components `internal`. | `Shell/Components/DataGrid/*.razor.cs` (12 `public partial class … : ComponentBase`); `find PublicAPI.Shipped.txt` → only `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt` |

**All four 🔴 Shell-integration-spike API questions (AR5 row "Shell-integration spike", owner _Tenants dev / FC supports_) are RESOLVED.** AC#4 satisfied.

### Cross-check vs the other AR5 🔴 rows
The remaining 🔴 readiness-request rows are owned by their own Epic-1 stories, **not** this spike, and no *new* API-shaped blocker surfaced for them while spiking:
- **FC-LYT** (Story 1.2): `<FrontComposerShell>@Body</FrontComposerShell>` layout body renders cleanly — page-layout contract is confirmed by Story 1.2.
- **FC-A11Y / FC-L10N / FC-DOC** (Stories 1.3–1.5): localization wired through `AddHexalithShellLocalization` inside Quickstart (`FcShellResources.resx`, `ServiceCollectionExtensions.cs:437`); a11y patterns observed in `FcExpandedRowHiddenBanner` (WCAG 4.1.2). No API question to escalate from this spike.

## Additional findings to escalate (record-only — spike does NOT fix)

- **F1 — Duplicate manifest from per-command registration default bounded context.** `GetManifests()`
  returned a second manifest `Name='Domain', BoundedContext='Domain', Commands=[Counter.Domain.IncrementCommand]`.
  Root cause: `IncrementCommand` has `[Command]` but **no `[BoundedContext]`**, so the generated
  `IncrementCommandRegistration` defaults `BoundedContext` to the namespace tail (`Counter.Domain` →
  `"Domain"`), while `AddHexalithDomain`'s reflection fallback (`CollectCommandRegistration`) files the
  same command under the marker's `"Counter"`. Net: `IncrementCommand` appears in **both** a `Counter`
  and a `Domain` manifest. **Owner: FrontComposer (SourceTools/generator).** Impact for Story 1.1: a
  command without `[BoundedContext]` produces a stray nav group; bootstrap docs should require
  `[BoundedContext]` on every command, or the generator should inherit the marker's context.
  Evidence: `Counter.Domain.IncrementCommand.CommandRegistration.g.cs` (`BoundedContext: "Domain"`);
  `ServiceCollectionExtensions.cs:578-606`.
- **F2 — Two command-route conventions.** Generated FullPage pages route at
  `/commands/{BoundedContext}/{CommandName}` (PascalCase, e.g. `/commands/Counter/ConfigureCounterCommand`),
  but `CommandRouteBuilder.BuildRoute` yields `/domain/{kebab-bc}/{kebab-cmd}`. Confirm which is canonical
  before Story 1.1 documents routing. **Owner: FrontComposer (Shell routing).**
  Evidence: `...ConfigureCounterCommand.CommandPage.g.razor.cs:16` vs `CommandRouteBuilder.cs:77-82`.
- **F3 — FC-TBL public surface not frozen.** See Q4 gap. **Owner: FrontComposer + Product/UX, via Story 2.8.**
- **F4 — `HFC1001` on a host with no annotated types.** The host project itself raised
  `warning HFC1001: No [Command] or [Projection] types found in compilation` because the projections/commands
  live in the referenced `Counter.Domain` assembly, not the host. Benign for the layered-host pattern, but
  Story 1.1 bootstrap docs should call it out so adopters don't treat it as an error. **Owner: Story 1.1 author.**
- **F5 — Additional-assembly registration is required in TWO places.** To reach generated command pages and
  the Shell's `/home`, the adopter must add the domain assembly **and** the Shell assembly to BOTH the
  `<Router AdditionalAssemblies=…>` (Routes.razor) AND `MapRazorComponents<App>().AddAdditionalAssemblies(…)`
  (endpoint routing). Omitting the endpoint-level call returned `HTTP 404` for every generated route during
  the spike until corrected. **Owner: Story 1.1 author** — this is the single most important bootstrap-wiring
  detail for 1.1. Reference: `samples/Counter/Counter.Web/Program.cs:125-134` + `Components/Routes.razor`.

## Hand-off to Story 1.1 (bootstrap)

**Confirmed assumptions (build 1.1 on these):**
1. The canonical 3-call ordering (Quickstart → `AddHexalithDomain<TMarker>` → stub/real `AddHexalithEventStore`)
   boots a working empty shell with `ValidateScopes = true`.
2. Generated `*Registration` types flow into `IFrontComposerRegistry.GetManifests()` automatically via
   `AddHexalithDomain<T>` reflection — no manual registry calls needed.
3. Projections render as embeddable `*View` components reachable through the registry-driven home
   (`FcHomeRouteView` at `/` + `/home`); FullPage commands get generated routes; the default registry
   already satisfies the route-reachability companion.
4. A valid stub `EventStoreOptions` (absolute `BaseAddress`) passes `ValidateOnStart` without a live backend.

**Open / escalated items (owners above):** F1 (generator bounded-context default — FrontComposer),
F2 (route-convention canonicalization — FrontComposer), F3 (FC-TBL freeze — FrontComposer + Product/UX via
Story 2.8), F4 (HFC1001 docs — Story 1.1), F5 (dual additional-assembly registration — Story 1.1).
None block bootstrap; all are recorded with named owners.

## Cleanup

- Throwaway host removed: **yes** (`rm -rf artifacts/spike-1-0`).
- `git status --porcelain src/` empty: **yes** (verified — see Story 1.0 AC#5).
- `Hexalith.FrontComposer.slnx` unmodified: **yes**.
- Net-new tracked artifacts: (1) this spike note under `_bmad-output/spike-notes/`, and (2) a **permanent regression suite** `tests/Hexalith.FrontComposer.Shell.Tests/Spike/Story10ShellIntegrationSpikeTests.cs` (8 tests, all passing) pinning the four 🔴 answers against live `src/`. The regression suite is **not** a discarded throwaway host-driving test — it lives under `tests/` (not `src/`), so AC#5's "no `src/` changes" still holds.
