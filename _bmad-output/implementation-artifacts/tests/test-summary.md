# Test Automation Summary — Story 1.0 (Shell-integration spike)

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-03
**Story:** `1-0-shell-integration-spike-verify-the-bootstrap-table-apis.md`
**Spike note:** `_bmad-output/spike-notes/1-0-shell-integration-spike-2026-06-03.md`

## Context

Story 1.0 is a **time-boxed spike** — it produced no `src/` artifact (AC#5). What it *did* produce
is four empirically-confirmed answers about **already-shipped** Shell features (bootstrap registration,
manifest discovery, projection-route reachability, and the `FC-TBL` DataGrid surface). Those answers
are the facts Story 1.1 (bootstrap) builds on.

This QA run locks the four confirmed answers in as **regression tests against the live `src/` code**, so
they cannot silently drift before 1.1 consumes them. The throwaway host stayed discarded; no `src/`
files were touched (`git status --porcelain src/` is empty).

## Framework

Project's existing stack — **xUnit v3 + Shouldly** (per `project-context.md` testing rules). No new
framework introduced. Tests live in the existing `Hexalith.FrontComposer.Shell.Tests` project and run
through the solution-level lane.

## Generated Tests

### Integration / contract tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Spike/Story10ShellIntegrationSpikeTests.cs` — 8 tests, all passing.

| Spike question (🔴 AR5) | Test(s) | What it pins |
|---|---|---|
| **Q1** — registration path boots empty shell (AC#1/#2) | `Bootstrap_QuickstartThenDomainThenStubEventStore_BuildsWithScopeValidation`, `Bootstrap_EventStoreRegisteredAfterQuickstart_PreservesAuthoritativeRegistry` | Canonical 3-call ordering (Quickstart → `AddHexalithDomain<CounterDomain>` → stub `AddHexalithEventStore`) builds with `ValidateScopes=true` (ADR-030 guard); EventStore's `TryAdd` does not drop the Quickstart registry. |
| **Q2** — manifest discovery → `GetManifests()` (AC#2) | `ManifestDiscovery_ThroughQuickstart_SurfacesGeneratedCounterRegistration` | Generated `*Registration` flows into the registry through the full Quickstart entry point; Counter manifest carries `CounterProjection` + `IncrementCommand` FQNs. |
| **Q3** — projection-route reachability + companion opt-in (Task 3) | `DefaultRegistry_ImplementsRouteReachabilityCompanions`, `DefaultRegistry_HasFullPageRoute_TrueForRegisteredCommand_FalseForUnknown` | Default registry already implements `IFrontComposerFullPageRouteRegistry` + `IFrontComposerCommandWriteAccessRegistry` (no extra wiring needed); `HasFullPageRoute` is permissive for registered commands, false for unknown. |
| **Q4** — `FC-TBL` column/filter/expand surface (Task 4 → Story 2.8) | `FcTbl_DocumentedSurface_IsPublicComponentBase`, `FcColumnPrioritizer_MaxVisibleColumns_DefaultIsTen`, `FcExpandInRowDetail_ExposesDocumentedParameters` | The 12 adopter-facing DataGrid components stay `public ComponentBase` (compile-time freeze for finding F3); `FcColumnPrioritizer.MaxVisibleColumns` defaults to 10; `FcExpandInRowDetail` exposes `PanelId` + `SuppressedAnnouncement` `[Parameter]`s (WCAG 4.1.2). |

### E2E tests
- None added. UI/a11y E2E is owned by the existing Playwright workspace in `tests/e2e` (specimen host),
  and the spike's UI surface (`FC-TBL`) is pinned here at the contract level. A full DataGrid filter/expand
  E2E belongs to Story 2.8 when the surface is formally frozen.

### API tests
- N/A — FrontComposer ships no deployed HTTP service; the "API" exercised by the spike is the
  in-process registration/registry/registry-companion surface, covered above.

## Discovered gaps — auto-applied

The spike's four confirmed answers were checked against existing coverage. Three were genuine gaps and
are now filled (the fourth was partially covered and extended):

1. **Full Quickstart→Domain→EventStore boot ordering** — existing tests covered `AddHexalithFrontComposer`
   (granular) and Quickstart sugar in isolation, but not the end-to-end 3-call ordering invariant the spike
   booted. **Added.**
2. **Route-reachability companion opt-in** — no test asserted the *default* registry implements
   `IFrontComposerFullPageRouteRegistry` / `IFrontComposerCommandWriteAccessRegistry` or the `HasFullPageRoute`
   true/false contract. **Added** (the headline Q3 finding).
3. **`FC-TBL` public surface freeze** — no test pinned the 12-component adopter surface (finding F3: not in
   any `PublicAPI.Shipped.txt`). **Added** as a compile-time + reflection contract for Story 2.8.
4. **Manifest discovery via Quickstart** — `FrontComposerRegistryTests` covered the granular path; **extended**
   to the Quickstart entry point the spike actually used.

## Coverage

- Spike 🔴 API questions pinned: **4 / 4**
- Spike findings referenced (record-only, not "fixed" here): F1 (duplicate `Domain` manifest), F2 (route-convention),
  F3 (`FC-TBL` not frozen — now pinned by Q4 tests), F4 (`HFC1001`), F5 (dual additional-assembly registration).
  F1/F2/F4/F5 are bootstrap/generator concerns escalated to their owners; not converted to assertions (would pin
  behaviour the spike explicitly chose to escalate, not lock).

## Validation

- Command: `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/... --filter "FullyQualifiedName~Story10ShellIntegrationSpikeTests"`
- Result: **Passed — 8/8, 0 failed, 0 skipped.**
- `git status --porcelain src/` — empty (spike AC#5 preserved).

## Next steps

- Run inside the full solution lane in CI:
  `dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` with `DiffEngine_Disabled=true`.
- **Story 2.8** consumes the Q4 surface tests: when it freezes `FC-TBL`, mirror the 12-component set into a Shell
  `PublicAPI.Shipped.txt` (resolving finding F3) — the Q4 tests then become the executable counterpart of that baseline.
- **Story 1.1** consumes Q1–Q3 as confirmed assumptions; the escalated findings (F1, F2, F4, F5) remain owner-tracked
  in the spike note.
