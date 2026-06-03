---
baseline_commit: 0db0fb04f95dddc2b273d086418256e74a89f20a
---

# Story 1.1: Bootstrap a minimal, bootable shell

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield reality — read this first.** The Shell, the three-call DI bootstrap, the
> `FluentLayout` frame, skip links, `FluentProviders`, the `Ctrl+,` / `Ctrl+K` shortcuts, and the
> empty-state home directory **already exist and already boot** — Story 1.0's spike proved it
> end-to-end (host booted with `ValidateScopes=true`, `GET /` and `GET /home` → 200, registry
> populated). This story is therefore **mostly confirm-and-pin** (AC#1, AC#3) **plus one genuinely
> new guard** (AC#2): a startup fail-fast that NAMES a missing/mis-ordered `AddHexalith*` call instead
> of dying with an opaque DI error at first render. Do **not** re-implement the shell. Do **not**
> "fix" working wiring. Add the AC#2 guard, then lock AC#1/AC#3 behaviour with tests so it cannot
> silently regress.

## Story

As an adopter developer,
I want my app's `MainLayout` to reduce to `<FrontComposerShell>@Body</FrontComposerShell>` with the three-call DI bootstrap,
so that I get the complete Header/Navigation/Content/Footer frame with zero hand-written layout.

## Acceptance Criteria

**AC1 — Three-call bootstrap registers the full service graph and renders the frame. *(FR9, FR10)***
**Given** an app calling `AddHexalithFrontComposerQuickstart()` → `AddHexalithDomain<TMarker>()` → `AddHexalithEventStore(...)` in that order,
**When** the app starts,
**Then** Fluxor (with the `<StoreInitializer />` placed by `FrontComposerShell`), `IStorageService`, `IFrontComposerRegistry`, the command/query stubs, and the badge / lifecycle / slot / template / view registries are all registered,
**And** the shell renders `FluentLayout` with skip links, `<FluentProviders />`, and the global shortcuts `Ctrl+,` (settings) and `Ctrl+K` (palette) active.

**AC2 — Out-of-order or missing registration fails fast with a NAMING message. *(NEW WORK)***
**Given** the registration calls are made out of order or a required one is missing,
**When** the app starts,
**Then** startup fails fast at host start (before first render) with a message that **names the missing or mis-ordered registration** (e.g. "`AddHexalithFrontComposerQuickstart()` must be called before `AddHexalithEventStore(...)`") rather than failing later with an opaque DI resolution error at first render.

**AC3 — Empty shell (no domain types) renders the home empty state without throwing.**
**Given** the empty shell (no domain types registered yet),
**When** it renders,
**Then** the content area shows the home directory in its empty state (`FcHomeDirectory` → `data-testid="fc-home-empty-no-microservices"`) without throwing, and the Navigation layout area is omitted so content spans edge-to-edge.

## Tasks / Subtasks

- [x] **Task 1 — Implement the bootstrap fail-fast guard (AC: #2) — THE NEW CODE**
  - [x] Add a startup validation gate that runs at host start and throws a clear, **named** message when the bootstrap is misconfigured. Follow the two existing fail-fast precedents — `IHostedService.StartAsync` throwing `InvalidOperationException` — in `Services/Customization/CustomizationContractValidationGate.cs` and `Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs`. Register it via `AddHostedService` from inside `AddHexalithFrontComposer` (the method Quickstart chains to), mirroring lines `ServiceCollectionExtensions.cs:255` and `:389`.
  - [x] Detect **mis-ordering**: have each of the three entry points append an ordered bootstrap marker so the gate can read insertion order from the `IServiceCollection`/DI and verify `Quickstart` precedes `Domain` precedes `EventStore`. (`IServiceCollection` preserves insertion order; a small `sealed record`/`sealed class` marker singleton per call site is the cheapest mechanism. Do NOT use a mutable process-static.) Recommended marker home: a new file under `Extensions/` or `Registration/`.
  - [x] Detect **missing required registrations**: if the foundational graph Quickstart owns is absent (e.g. `IFrontComposerRegistry`, the Fluxor store, `IStorageService`), throw a message naming the call the adopter forgot (`AddHexalithFrontComposerQuickstart()`). Note `AddHexalithDomain<T>()` is NOT required for a valid empty shell (AC#3) — do not force it.
  - [x] Write the message to the logger AND throw it (mirror `CustomizationContractValidationGate.cs:101-102`). Throw `InvalidOperationException`; do NOT introduce a new exception type.
  - [x] Keep the gate idempotent against duplicate registration (`TryAddEnumerable` / single registration) so calling Quickstart twice does not double-register or double-throw.

- [x] **Task 2 — Pin the AC#1 service-graph registrations with DI tests (AC: #1)**
  - [x] Add tests under `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/` (alongside `AddHexalithFrontComposerQuickstartTests.cs`) asserting the full three-call graph resolves: `IFrontComposerRegistry` (Singleton), `IStorageService` (Scoped, ADR-030), the command/query stub path (`ICommandService` / `ICommandServiceWithLifecycle`), and the badge/lifecycle/slot/template/view registries — `IBadgeCountService`, `ILifecycleStateService`, `ILifecycleBridgeRegistry`, `IProjectionSlotRegistry`, `IProjectionTemplateRegistry`, `IProjectionViewOverrideRegistry`.
  - [x] Build with `ValidateScopes = true` (mirror `AddHexalithFrontComposerQuickstartTests.cs:29` and the spike fixture `Story10ShellIntegrationSpikeTests.cs:171-186`) and replace `IStorageService` with `InMemoryStorageService` (real `LocalStorageService` needs `IJSRuntime`).
  - [x] Assert correct lifetimes where ADR-030 matters (storage = Scoped; registry = Singleton).

- [x] **Task 3 — Pin the AC#1 shell render with a bUnit test (AC: #1)**
  - [x] Add a bUnit render test for `FrontComposerShell` (extend `FrontComposerTestBase` per `tests/Hexalith.FrontComposer.Shell.Tests/FrontComposerTestBase.cs`; `JSInterop.Mode = Loose`). Assert the rendered DOM contains: the skip-to-content link (`a.fc-skip-link[href="#fc-main-content"]`), `<FluentProviders />` output, and the `<StoreInitializer />`-owned region — see `FrontComposerShell.razor:28-33,121`.
  - [x] Assert the `Ctrl+K` (palette) and `Ctrl+,` (settings) shortcuts are registered after first render. The registrar (`Shortcuts/FrontComposerShortcutRegistrar.cs:77-99`) registers `ctrl+k`, `meta+k`, `ctrl+,`, `meta+,` via `IShortcutService` in `OnAfterRenderAsync(firstRender:true)` (`FrontComposerShell.razor.cs:262`). Verify via the `IShortcutService` surface, not by simulating a keypress, to keep the test deterministic.

- [x] **Task 4 — Pin the AC#3 empty-state render with a bUnit test (AC: #3)**
  - [x] Render `FcHomeDirectory` (or `FrontComposerShell` wrapping it) against an EMPTY registry and assert the empty-state section renders without throwing: `data-testid="fc-home-empty-no-microservices"` (`Components/Home/FcHomeDirectory.razor:18-20`).
  - [x] Assert that with an empty registry the shell's `HasNavigation` is false so the Navigation `FluentLayoutItem` is omitted (`FrontComposerShell.razor:80`, `FrontComposerShell.razor.cs:206,505-513`).

- [x] **Task 5 — Confirm the adopter-facing `MainLayout` reduction is documented/exercised (AC: #1)**
  - [x] Verify the canonical reduction `MainLayout.razor` → `<FrontComposerShell>@Body</FrontComposerShell>` is demonstrated by the live reference host (`samples/Counter/Counter.Web`). If the sample already does this, cite it in the Completion Notes; do NOT duplicate a sample. If a doc page is the deliverable, it belongs under `_bmad-output/` or the FC-DOC page (Story 1.5), **never** scratch-written into the CI-gated `docs/` site.
  - [x] Do NOT add the bootstrap snippet to `docs/` ad hoc — FC-DOC (Story 1.5) owns the published component docs.

- [x] **Task 6 — Build clean + run the test lanes (DoD)**
  - [x] `dotnet build -c Release` clean (TWAE — zero warnings).
  - [x] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` green for everything this story touches; 13 pre-existing failures (8 Shell + 3 SourceTools + 2 Cli) were confirmed identical on the clean baseline commit `0db0fb0` and are NOT introduced by this story (see Completion Notes).
  - [x] Confirm the Story 1.0 spike regression suite (`Spike/Story10ShellIntegrationSpikeTests.cs`) still passes — this story builds directly on those pinned answers (all 8 spike tests green).

## Dev Notes

### What already exists (DO NOT rebuild — verify and pin)

The brownfield Shell already satisfies the *behavioural* half of AC#1 and all of AC#3. Exact anchors:

- **Three-call bootstrap** — `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`:
  - `AddHexalithFrontComposerQuickstart(...)` — `:476`. Chains `AddLocalization` + `AddAuthorizationCore` + `AddHexalithShellLocalization` + `AddHexalithFrontComposer`.
  - `AddHexalithFrontComposer(...)` — `:165`. Registers Fluxor (`AddFluxor`, scans the Shell assembly via `typeof(FrontComposerThemeState).Assembly`, `:178-182`), `IStorageService`→`LocalStorageService` **Scoped** (ADR-030, `:191-192`), `IFrontComposerRegistry`→`FrontComposerRegistry` **Singleton** (`:193`), the stub command path `StubCommandService`/`ICommandServiceWithLifecycle`/`ICommandService` (`:209-215`), and the registries: badge `IBadgeCountService` (`:310`), lifecycle `ILifecycleStateService`/`ILifecycleBridgeRegistry` (`:258,261-262`), slot `IProjectionSlotRegistry` (`:380`), template `IProjectionTemplateRegistry` (`:375`), view-override `IProjectionViewOverrideRegistry` (`:385`).
  - `AddHexalithDomain<T>(...)` — `:72`. Reflects the marker assembly for generated `*Registration` types and feeds them into the registry. Optional for an empty shell.
  - `AddHexalithEventStore(...)` — `Extensions/EventStoreServiceExtensions.cs:32`. **TryAdds** the registry (`:45`) so the Quickstart-installed authoritative registry survives when EventStore runs last; `RemoveStubCommandService` then swaps the stub for the real client.
- **Shell frame** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`:
  - `<Fluxor.Blazor.Web.StoreInitializer />` — `:28` (the shell OWNS the initializer; adopters must NOT mount a second one — see the `AddHexalithFrontComposer` XML remark at `:140-144`).
  - Skip links — `:29` (`#fc-main-content`) and `:32` (`#fc-nav`, gated on `HasNavigation`).
  - `FluentLayout` Header/Navigation/Content/Footer — `:36-118`.
  - `<FluentProviders />` — `:121`.
  - Global keydown router → `IShortcutService.TryInvokeAsync` — `.razor:27` + `.razor.cs:244-252`.
- **Shortcuts** — `Shortcuts/FrontComposerShortcutRegistrar.cs:67-139`. `RegisterShellDefaultsAsync()` (idempotent via `Interlocked.Exchange`, `:70`) registers `ctrl+k`+`meta+k` (palette) and `ctrl+,`+`meta+,` (settings), plus `g h`, `/`. Called once from `FrontComposerShell.razor.cs:262` on first render.
- **Empty-state home** — `Components/Home/FcHomeDirectory.razor:18-20` renders `<section class="fc-home-empty" data-testid="fc-home-empty-no-microservices">` when `Registry.GetManifests().Count == 0`. Mounted at `/` and `/home` by `Components/Pages/FcHomeRouteView.razor`.
- **Nav omitted when empty** — `FrontComposerShell.razor.cs:206` (`HasNavigation`) + `:505-513` (`HasRenderableManifest`); the Navigation `FluentLayoutItem` is gated at `FrontComposerShell.razor:80`.

### The one genuinely-new piece: AC#2 fail-fast guard

There is currently **no guard that names a missing/mis-ordered `AddHexalith*` call.** Today, omitting Quickstart yields an opaque "Unable to resolve service for type 'IFrontComposerRegistry'" at first render — exactly what AC#2 forbids. Build the guard:

- **Pattern to copy:** `IHostedService.StartAsync` that validates then throws `InvalidOperationException`, logging the same message first. Canonical examples already in the repo:
  - `Services/Customization/CustomizationContractValidationGate.cs:28,53-103` (force-resolves dependencies in its ctor so they hydrate before validation; builds a precise message; `LogError` + `throw`).
  - `Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:16-22` (`IHostedService`, snapshots registry/options, distinguishes "missing" from "not-configured").
- **Registration:** add `services.AddHostedService<...>()` inside `AddHexalithFrontComposer` (near `:255`/`:389`). Hosted-service `StartAsync` runs at host start — **before** first render — satisfying "fails fast … rather than failing later at first render."
- **Ordering signal:** `IServiceCollection` preserves insertion order. Have each entry point append a tiny ordered marker (a `sealed` marker carrying an enum `Quickstart|Domain|EventStore`). The gate reads the markers (resolve `IEnumerable<TMarker>` — DI preserves registration order) and verifies the required precedence. If `EventStore` appears before `Quickstart`, throw naming both calls. Keep markers immutable; **no mutable process-static** (mirrors the D33 "no mutable process-static" rule the codebase enforces, e.g. `ServiceCollectionExtensions.cs:218-219`).
- **Message quality (the AC's whole point):** name the offending call AND the fix, e.g.
  `"FrontComposer bootstrap is mis-ordered: AddHexalithEventStore(...) was called before AddHexalithFrontComposerQuickstart(). Call AddHexalithFrontComposerQuickstart() first so it can establish the authoritative Fluxor store, IStorageService, and IFrontComposerRegistry."`
- **Edge cases to honour:** (a) Quickstart called twice must not double-throw (idempotent registration); (b) `AddHexalithDomain<T>()` absent is **valid** (empty shell, AC#3) — do not require it; (c) test hosts that build a bare container without a `HostBuilder` won't run hosted services — your AC#1/AC#3 unit tests build the `ServiceProvider` directly, so don't rely on the gate firing there. If you also want the guard exercisable in a unit test, expose the validation as a static/internal method the hosted service calls and test that method directly (matches how the repo unit-tests `StartAsync` logic elsewhere).

### Must-not-break (regression surface)

A bootstrap story must leave the system working end-to-end. Preserve:

- **`AddHexalithEventStore` TryAdd discipline** — it must keep TryAdd-ing the registry so the Quickstart-installed singleton (already holding domain manifests) is NOT replaced when EventStore runs last. Pinned by `Story10ShellIntegrationSpikeTests.cs:50-61` — keep that test green.
- **ADR-030 scoped-lifetime discipline** — `IStorageService` stays Scoped; do not capture it (or other scoped accessors) in a singleton. The new gate is a hosted service (singleton-ish); resolve only singletons/options in its ctor, or capture `IServiceScopeFactory` if you must touch scoped services. `ValidateScopes=true` (Counter.Web + the test fixtures) will catch violations at boot.
- **StoreInitializer single-ownership** — `FrontComposerShell` owns `<StoreInitializer />`; do not add another, and do not move it into DI.
- **Shortcut idempotency** — the registrar's `Interlocked` guard (`FrontComposerShortcutRegistrar.cs:70`) must keep `Ctrl+K`/`Ctrl+,` registered exactly once across bUnit/prerender/hot-reload boot paths.

### Previous story intelligence (Story 1.0 spike — `done`)

- The spike (`_bmad-output/spike-notes/1-0-shell-integration-spike-2026-06-03.md`) **already confirmed** the four 🔴 AR5 questions and left a **permanent regression suite** at `tests/Hexalith.FrontComposer.Shell.Tests/Spike/Story10ShellIntegrationSpikeTests.cs` (8 tests). Use it as the worked example for: the canonical 3-call ordering, `ValidateScopes=true` fixture, `InMemoryStorageService` swap, and `GetManifests()` assertions. Your new AC#1/AC#2/AC#3 tests should sit beside it and reuse the same fixture shape.
- Spike findings F1–F5 were escalated with owners (in the spike note) — none block 1.1, but skim them so you don't re-surface a known-and-owned item as a bug.
- Reference host: `samples/Counter/Counter.Web/Program.cs` is the live, correct consuming host (`ValidateScopes=true`, the 3-call ordering, `<FrontComposerShell>@Body</FrontComposerShell>` layout body). Mirror it; don't invent a new bootstrap shape.

### Git intelligence

- HEAD `0db0fb0` = the Story 1.0 spike (`feat(story-1.0)`). The spike added only `_bmad-output/` + `tests/` artifacts — **no `src/` changes** — so the live `src/` bootstrap is exactly as documented above. Your story 1.1 will be the first to add production `src/` code on top of the confirmed baseline.
- Working tree has an unrelated modified file (`_bmad-output/story-automator/orchestration-*.md`); leave it alone.

### Testing standards summary

- xUnit **v3** + **Shouldly** (`ShouldBe`/`ShouldThrow`, never raw `Assert.*`) + **NSubstitute**; **bUnit** for the shell/component renders via `FrontComposerTestBase` (`JSInterop.Mode = Loose`).
- Test files are **plural `{Class}Tests.cs`**; methods are three-part **`Subject_Scenario_Expectation`** (see the spike suite for the house style).
- Run **solution-level**: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`. `DiffEngine_Disabled=true` is REQUIRED (else Verify hangs). This is the OPPOSITE of the EventStore submodule's per-project rule.
- For the AC#2 message, prefer asserting the thrown `InvalidOperationException.Message` **contains the offending method name** (`AddHexalithFrontComposerQuickstart` / `AddHexalithEventStore`) over snapshotting the whole string, so wording tweaks don't churn the test.

### Project-context rules that bite here

- **No copyright/license headers** (0 of 483 files have one). **`ConfigureAwait(false)` on every await in `src/`** (CA2007 → build error via TWAE) — EXCEPT inside Blazor components, where `#pragma warning disable CA2007` is already applied (see `FrontComposerShell.razor.cs:22`).
- **`sealed record`/`sealed class` by default** (the new marker + gate should be sealed). **File-scoped namespaces, Allman braces, `_camelCase` fields, `Async` suffix, `I`-prefixed interfaces.**
- **`TreatWarningsAsErrors=true`** — fix warnings, don't blanket-suppress. **CRLF, 4-space indent, final newline.**
- **`.slnx` only** (`Hexalith.FrontComposer.slnx`); never create a `.sln`. **Centralized package versions** — never add `Version=` to a `.csproj`.
- **Generated/BMAD docs go to `_bmad-output/`, never the CI-gated `docs/` DocFX site.**
- **Conventional Commits:** this is `feat` (new fail-fast guard); branch `feat/<desc>`, never commit to `main`. Run `/bmad-code-review` before flipping to done.

### Project Structure Notes

- New production code lives in `src/Hexalith.FrontComposer.Shell/` — suggested: the bootstrap marker + validation gate under `Extensions/` (next to the entry points) or `Registration/`. Keep the gate `internal sealed` (matches `CustomizationContractValidationGate`); the marker can be `internal sealed` too (it's an implementation detail, not adopter API).
- New tests live in `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/` (DI tests) and the shell/home component test folders (bUnit), reusing `FrontComposerTestBase` and the spike fixture pattern.
- If the gate adds a public type to any project with a `PublicAPI.Shipped.txt`, update that baseline intentionally — but prefer `internal` to avoid touching the public surface for a bootstrap guard.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.1: Bootstrap a minimal, bootable shell] (story + ACs)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: Shell Foundation & Bootstrap] (FR9, FR10, FR15; AR1–AR5)
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md] (🔴 Shell-integration spike → bootstrap; AR5)
- [Source: _bmad-output/implementation-artifacts/1-0-shell-integration-spike-verify-the-bootstrap-table-apis.md] (previous story; confirmed bootstrap answers)
- [Source: _bmad-output/spike-notes/1-0-shell-integration-spike-2026-06-03.md] (spike note; F1–F5 escalations)
- [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules] (startup wiring order, ADR-030 scoped lifetime, single-writer)
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:72,165,476] (the three entry points + full registration list)
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs:32,45] (`AddHexalithEventStore`, TryAdd registry)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:28,29,32,80,121] (StoreInitializer, skip links, nav gate, FluentProviders)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs:206,244-252,262,505-513] (HasNavigation, keydown router, shortcut registration, HasRenderableManifest)
- [Source: src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs:67-139] (Ctrl+K / Ctrl+, registration)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor:18-20] (empty-state branch + data-testid)
- [Source: src/Hexalith.FrontComposer.Shell/Services/Customization/CustomizationContractValidationGate.cs:28,53-103] (fail-fast hosted-service pattern to copy for AC#2)
- [Source: src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:16-22] (second fail-fast hosted-service precedent)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Spike/Story10ShellIntegrationSpikeTests.cs:34-61,171-186] (regression suite + ValidateScopes fixture to reuse)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Extensions/AddHexalithFrontComposerQuickstartTests.cs] (existing Quickstart DI test style)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/FrontComposerTestBase.cs] (bUnit base for shell renders)
- [Source: samples/Counter/Counter.Web/Program.cs] (canonical consuming-host bootstrap + MainLayout reduction)

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context) — bmad-dev-story workflow

### Debug Log References

- `dotnet build -c Release` — Build succeeded, 0 Warnings, 0 Errors (TWAE clean).
- `DiffEngine_Disabled=true dotnet test … --filter "Category!=Performance&…"` — see Completion Notes for the pre-existing-failure baseline diff.

### Completion Notes List

**AC#2 (the new code) — bootstrap fail-fast guard.** Added a startup gate that names the missing / mis-ordered `AddHexalith*` call instead of letting the host die with an opaque `IFrontComposerRegistry` DI error at first render:

- `Extensions/FrontComposerBootstrapMarkers.cs` — `internal enum FrontComposerBootstrapStage { Quickstart, Domain, EventStore }`, `internal interface IFrontComposerBootstrapMarker`, and three `sealed record` markers (one per call site). Immutable singletons — no mutable process-static (D33).
- `Extensions/FrontComposerBootstrapValidator.cs` — `internal static Validate(IEnumerable<IFrontComposerBootstrapMarker>)`. Reads markers in DI insertion order; throws `InvalidOperationException` naming both offending calls for mis-ordering, or naming the forgotten `AddHexalithFrontComposerQuickstart()` when the foundational call is absent. Exposed as a static method so it is unit-testable without a `HostBuilder` (bare-`ServiceProvider` test hosts don't run hosted services).
- `Extensions/FrontComposerBootstrapValidationGate.cs` — `internal sealed IHostedService` that logs then throws (mirrors `CustomizationContractValidationGate:101-102`). Depends only on the singleton markers + a logger → scope-safe under `ValidateScopes=true` (ADR-030).
- Wiring: each entry point appends its marker via `TryAddEnumerable` and registers the gate via `AddHostedService` (both idempotent → calling Quickstart twice neither double-registers nor double-throws). The gate is registered by **all three** entry points (not just `AddHexalithFrontComposer`) so the missing-foundational case is detectable even when an adopter wires only `AddHexalithEventStore(...)`. `AddHexalithDomain<T>()` is treated as optional (empty shell is valid — AC#3).

**AC#1 / AC#3 (confirm-and-pin).** No production shell/registry code was changed — the brownfield Shell already satisfied the behavioural half. Added regression pins beside the spike suite:
- `tests/.../Extensions/FrontComposerServiceGraphTests.cs` — three-call graph resolves under `ValidateScopes=true` with `InMemoryStorageService`; pins lifetimes (registry/slot/template/view = Singleton; storage/badge/lifecycle = Scoped) and the stub command path; pins EventStore-TryAdd preserving the Quickstart registry + Counter manifest.
- `tests/.../Extensions/FrontComposerBootstrapGuardTests.cs` — AC#2 validator + gate behaviour, marker ordering through the real entry points, and the duplicate-Quickstart idempotency edge case.
- `tests/.../Components/Layout/Story11BootstrapShellRenderTests.cs` — AC#1 frame (skip link `a.fc-skip-link[href="#fc-main-content"]`, `<FluentProviders />`, `<StoreInitializer />`), Ctrl+K / Ctrl+, registered on first render via the `IShortcutService` surface; AC#3 empty-state (`data-testid="fc-home-empty-no-microservices"`) and Navigation area omitted on the empty shell.

**Task 5.** The canonical `MainLayout.razor` → `<FrontComposerShell>@Body</FrontComposerShell>` reduction is already demonstrated by the live reference host `samples/Counter/Counter.Web/Components/Layout/MainLayout.razor` and pinned by `tests/.../Integration/CounterWebIntegrationTests.cs` (three-substantive-line check). Not duplicated; no `docs/` writes (FC-DOC / Story 1.5 owns published docs).

**Test-lane result.** Story 1.1's new tests + the regression surface (spike, Quickstart, FrontComposerShell, FcHomeDirectory, EventStoreRegistration) = **76 passed, 0 failed**. The full solution lane shows **13 failures (8 Shell + 3 SourceTools + 2 Cli)** that were verified to fail **identically on the clean baseline commit `0db0fb0`** (changes stashed → rebuilt → re-run): missing `deferred-work.md` planning doc, Verify date-format snapshots, a Linux case-sensitivity check, and CLI solution-path parsing. **None are introduced by this story** and all fall outside Story 1.1's scope and stated regression surface. Recommend the reviewer route them to their owning stories.

### File List

- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapMarkers.cs` (new)
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapValidator.cs` (new)
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapValidationGate.cs` (new)
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` (modified — marker + gate wiring in `AddHexalithFrontComposer` and `AddHexalithDomain`)
- `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs` (modified — marker + gate wiring in `AddHexalithEventStore`)
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerBootstrapGuardTests.cs` (new)
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerServiceGraphTests.cs` (new)
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story11BootstrapShellRenderTests.cs` (new)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — 1-1 status transitions)

### Change Log

- 2026-06-03 — Story 1.1 created (create-story). Comprehensive context engine analysis completed — comprehensive developer guide created. Status → ready-for-dev.
- 2026-06-03 — Story 1.1 implemented (dev-story). Added the AC#2 bootstrap fail-fast guard (marker + static validator + hosted gate) and AC#1/AC#3 regression pins; no production shell/registry code changed. Release build clean (TWAE); 76 story+regression tests green; 13 full-lane failures confirmed pre-existing on baseline `0db0fb0`. Status → review.
- 2026-06-03 — Adversarial review (story-automator-review, auto-fix). Independently re-verified: Release build clean (TWAE); the 8 Shell-lane failures were reproduced *identically* on the stashed baseline `0db0fb0`, confirming none are introduced by this story; File List matches git reality exactly; AC#1/AC#2/AC#3 all implemented and pinned. **One LOW correctness defect found & fixed:** `FrontComposerBootstrapValidator.Validate` produced a *false* diagnostic on the empty-marker path (claimed `AddHexalithDomain<TMarker>()` "was called" when no entry point had run) — corrected to an accurate "no FrontComposer bootstrap call was made" message, with the `Validate_NoMarkersAtAll` test tightened to forbid the false attribution. 28/28 Story 1.1 tests green after the fix. 0 CRITICAL issues. Status → done.

### Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-03 · **Outcome:** Approve (with one LOW fix applied)

**Verification performed**
- AC#1 — three-call graph resolves under `ValidateScopes=true` with `InMemoryStorageService`; lifetimes pinned (registry/slot/template/view = Singleton, storage/badge/lifecycle = Scoped); stub→real command/query swap preserved (`FrontComposerServiceGraphTests`). Frame render (skip link, `FluentProviders`, `StoreInitializer`) + `Ctrl+K`/`Ctrl+,` registration pinned via the `IShortcutService` surface (`Story11BootstrapShellRenderTests`). ✅ Implemented.
- AC#2 — new fail-fast guard (immutable per-call markers → static `FrontComposerBootstrapValidator` → idempotent `IHostedService` gate registered by all three entry points). Names the missing/mis-ordered `AddHexalith*` call; logs-then-throws `InvalidOperationException`; idempotent against duplicate Quickstart. ✅ Implemented; D33 (no mutable process-static) and ADR-030 (gate depends only on singletons) honoured.
- AC#3 — empty registry renders `data-testid="fc-home-empty-no-microservices"` without throwing and omits the Navigation `FluentLayoutItem`. ✅ Implemented.
- Regression — the 8 Shell-lane failures (`PendingStatusReopenGovernanceTests`, `CounterStoryVerificationTests`, `CommandRendererFullPageTests`, `NavigationEffectsLastActiveRouteTests`) reproduce identically with the story stashed to baseline `0db0fb0`; pre-existing, out of scope. Route to their owning stories.

**Findings**
- 🟢 LOW (fixed): empty-marker diagnostic falsely attributed a downstream call — see Change Log. No CRITICAL/HIGH/MEDIUM findings.
</content>
</invoke>
