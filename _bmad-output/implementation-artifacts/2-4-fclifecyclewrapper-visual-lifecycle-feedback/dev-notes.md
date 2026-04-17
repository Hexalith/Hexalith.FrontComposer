# Dev Notes

### Service Binding Reference

No new DI registrations in `AddHexalithFrontComposer` beyond what Story 2-3 already landed **except** the options validator from Task 3.2:

```csharp
// Story 2-4 Task 3.2 — ordered-threshold validator for FcShellOptions.
services.AddSingleton<IValidateOptions<FcShellOptions>, FcShellOptionsThresholdValidator>();

// Story 2-4 Task 3.4 — fail-fast on startup if shell options misconfigured.
services
    .AddOptions<FcShellOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

`FcLifecycleWrapper` injects `ILifecycleStateService` (Story 2-3 D12 Scoped), `IOptionsMonitor<FcShellOptions>` (built-in), `NavigationManager` (built-in), `ILogger<FcLifecycleWrapper>` (built-in), `TimeProvider` (built-in on .NET 10). No wrapper-specific registrations — the wrapper is a Razor component + scoped CSS + standalone timer class, all resolved through normal Blazor DI.

### `FcShellOptions` Growth Risk (demoted from former ADR-023 during advanced-elicitation 2026-04-16)

Story 2-2 added three lifecycle-adjacent properties to `FcShellOptions` (`FullPageFormMaxWidth`, `DataGridNavCap`, `EmbeddedBreadcrumb`, `LastUsedDisabled`). Story 2-4 D12 adds four MORE threshold properties (`SyncPulseThresholdMs`, `StillSyncingThresholdMs`, `TimeoutActionThresholdMs`, `ConfirmedToastDurationMs`). The options class now mixes three concerns: form layout, data grid navigation, lifecycle timing.

**v0.1 decision: keep all eight properties on `FcShellOptions`**, accepting the concern mixing. Named trigger for splitting into `FcFormOptions` + `FcLifecycleOptions` (+ potentially `FcGridOptions`): when `FcShellOptions` reaches **≥12 properties** OR when a second cross-concern appears (e.g., an MCP-related option). The split lands in **Story 9-2** alongside the CLI inspection/migration tooling that rewrites adopter `appsettings.json` sections (`"Hexalith:Shell"` → `"Hexalith:Shell:Form"`/`"Hexalith:Shell:Lifecycle"`).

Was originally drafted as ADR-023 during party-mode review; demoted to this Dev Note during advanced-elicitation Occam pass because the content documents a *future refactor* and binds no v0.1 behavior — the ADR overhead (Status / Context / Decision / Consequences / Rejected alternatives sections) wasn't pulling its weight.

### Runtime-Log CorrelationId Sanitization (advanced-elicitation Red Team RT-4 + Pre-mortem PM-E 2026-04-16)

HFC2100/2101/2102 runtime-log messages MUST NOT emit raw CorrelationIds — hash each to its first 8 characters plus ellipsis (e.g., `"a1b2c3d4…"`). Rationale: CorrelationIds leak into third-party log aggregators (Datadog, Splunk, New Relic) when adopters use default `ILogger` sinks. In multi-tenant deployments this enables cross-tenant traffic-pattern correlation ("Tenant A users colliding on order approvals"). Hashing to first 8 chars preserves enough for debug-time matching within a single log context while breaking cross-context correlation.

Implementation pattern:
```csharp
private static string HashForLog(string correlationId)
    => correlationId.Length <= 8 ? correlationId : $"{correlationId[..8]}…";

_logger.LogWarning("HFC2100 …for correlationId={CorrelationIdFirst8}", HashForLog(transition.CorrelationId));
```

Unit-test assertion in `FcLifecycleWrapperTests`: log output for HFC2100/2101/2102 does NOT contain the full CorrelationId string.

### `ILifecycleStateService` is Internal-Surface-Only (advanced-elicitation Red Team RT-3 2026-04-16)

`ILifecycleStateService.Transition(...)` is the lifecycle state bus's ONLY write surface. Story 2-3 D19 guarantees the bridge is the single legitimate writer, but that's a convention, not enforcement. **Adopters MUST NOT expose `ILifecycleStateService` to JavaScript interop in production builds** — doing so would let a malicious page script forge `Confirmed` transitions for arbitrary CorrelationIds, causing every wrapper subscribed to that id to celebrate a submission that never happened.

If an adopter needs JS-visible lifecycle observation (e.g., a dev-mode overlay), expose a **read-only projection** (e.g., `IReadOnlyLifecycleObserver` with only `GetState`/`Subscribe` — no `Transition`), NOT the full service interface. This is a documentation-level warning in v0.1; analyzer enforcement can land in Story 9-1 build-time drift detection if adopters need the safety net.

### Fluent UI v5 Naming Note (D8)

UX specification wording frequently uses **"FluentProgressRing"**. In Fluent UI Blazor **v5.0.0-rc.2-26098.1** (the pinned version in `Directory.Packages.props`), `FluentProgressRing` is **deprecated/renamed to `FluentSpinner`** (confirmed via `mcp__fluent-ui-blazor__search_components` — see Task 0.2). **Use `FluentSpinner` in all new code**; it's also what `CommandFormEmitter` already emits at L390. Do not re-introduce `FluentProgressRing`.

Per UX spec §472-474, lifecycle feedback uses:
- `FluentSpinner` (in the submit button during Submitting — existing emitter)
- `FluentBadge` (Accent appearance, "Still syncing…" text at 2-10 s)
- `FluentMessageBar` (Success / Danger / Warning for Confirmed / Rejected / ActionPrompt)

`IToastService` is **removed** in Fluent UI v5 — UX spec §492 explicitly confirms this. Do NOT inject or reference `IToastService`. Use `FluentMessageBar` inline per spec §1145.

### Files Touched Summary

**Shell/Components/Lifecycle/** (new):
- `FcLifecycleWrapper.razor`
- `FcLifecycleWrapper.razor.cs`
- `FcLifecycleWrapper.razor.css`
- `LifecycleUiState.cs`
- `LifecycleThresholdTimer.cs`

**Shell/Options/** (new folder if absent):
- `FcShellOptionsThresholdValidator.cs`

**Shell/Extensions/** (modified):
- `ServiceCollectionExtensions.cs` — register validator (Task 3.2), ensure `AddOptions<FcShellOptions>().ValidateDataAnnotations().ValidateOnStart()` present (Task 3.4)

**Shell/_Imports.razor** (modified):
- Add `@using Hexalith.FrontComposer.Shell.Components.Lifecycle` (Task 4.2)

**Contracts/** (modified):
- `FcShellOptions.cs` — 4 new threshold properties with `[Range]` (Task 3.1)
- *(optional)* `Diagnostics/FcDiagnosticIds.cs` — extend with HFC2100-2102 (Task 3.3)

**SourceTools/Emitters/** (modified):
- `CommandFormEmitter.cs` — wrap emitted `<EditForm>` in `<FcLifecycleWrapper>` (Task 4.1)

**samples/Counter/Counter.Web/** (modified):
- `Program.cs` — bind `FcShellOptions` from `Hexalith:Shell` config section (Task 6.2)
- `appsettings.Development.json` — override thresholds (Task 6.2)

**tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/** (new):
- `LifecycleUiStateTests.cs` (10 unit tests on pure `From(...)` function — Task 5.1a; includes D22 XSS encoding test `RejectionMessage_with_script_tag_renders_HTML_encoded`)
- `FcLifecycleWrapperTests.cs` (7 bUnit behavioural tests — Task 5.1b, includes R-Reentrancy + auto-dismiss-timer-dispose)
- `FcLifecycleWrapperThresholdTests.cs` (7 FakeTimeProvider threshold tests — Task 5.2, includes R-Options-Hot-Reload)
- `LifecycleThresholdTimerPropertyTests.cs` (3 FsCheck property tests — Task 5.2b)
- `FcLifecycleWrapperA11yTests.cs` (6 accessibility tests — Task 5.3, includes R-Circuit-Reconnect dispose-during-inflight + focus-preservation-on-message-bar-insert)
- `LifecycleThresholdTimerTests.cs` (4 timer-unit tests — Task 5.4)

**tests/Hexalith.FrontComposer.Shell.Tests/Options/** (new folder):
- `FcShellOptionsValidationTests.cs` (3 tests — Task 5.5)

**tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/** (new folder):
- `CounterCommandLatencyE2ETests.cs` (2 tests — Task 6.1, revised to n=300 cold + 10-click warm-up discard per Murat review)
- `WrapperConcurrencyLoadTests.cs` (1 test — Task 6.4 — 20-wrapper concurrency load test per Winston review)

**tests/Hexalith.FrontComposer.Shell.Tests/Generated/** (modified):
- `CommandRendererInlineTests.cs` — add `fc-lifecycle-wrapper` presence assertion
- `CommandRendererCompactInlineTests.cs` — same
- `CommandRendererFullPageTests.cs` — same

**tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/Snapshots/** (re-approved):
- `CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt`
- `CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`
- (any other `CommandFormEmitterTests.*.verified.txt` snapshots exercising the EditForm emission — inspect and re-approve all that change)

### Naming Convention Reference

| Element | Pattern | Example |
|---|---|---|
| Wrapper component | `FcLifecycleWrapper` | — |
| Scoped CSS file | `FcLifecycleWrapper.razor.css` | generated as `_content/Hexalith.FrontComposer.Shell/FcLifecycleWrapper.razor.css` |
| Sync-pulse CSS class | `.fc-lifecycle-pulse` | — |
| CSS keyframes | `@keyframes fc-lifecycle-pulse` | — |
| Timer class | `LifecycleThresholdTimer` | — |
| UI state record | `LifecycleUiState` | — |
| Timer phase enum | `LifecycleTimerPhase { NoPulse, Pulse, StillSyncing, ActionPrompt, Terminal }` | — |
| Options validator | `FcShellOptionsThresholdValidator` | — |
| Runtime diagnostic codes | `HFC2100`, `HFC2101`, `HFC2102` | — |

### Testing Standards

- xUnit v3 (3.2.2), Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2 (inherited from 2-1/2-2/2-3)
- `FakeTimeProvider` via `Microsoft.Extensions.TimeProvider.Testing` — verify it's already referenced in `Hexalith.FrontComposer.Shell.Tests.csproj` per `dotnet list package`; add if missing (Task 0.1 catches this)
- Microsoft.Playwright for Task 6.1 E2E — verify reference, install browsers on CI agent via `playwright install chromium` before the E2E job (reuse any existing Playwright wiring from `Hexalith.EventStore`'s Playwright gate if present)
- `TestContext.Current.CancellationToken` on async test helpers (xUnit1051)
- `TreatWarningsAsErrors=true` global
- `DiffEngine_Disabled: true` in CI
- **Test count budget (L07 applied):** **~47 new tests** (revised up from 33 per Murat review, further +2 per advanced-elicitation Pre-mortem PM-D/PM-F — 10 unit + 7 bUnit + 7 threshold + 3 FsCheck properties + 4 timer-unit + 6 a11y + 3 validation + 3 renderer-assertion + 2 E2E latency + 1 E2E concurrency + 1 Counter sample manual-validation sanity check). Cumulative target **~506**. L07 cost-benefit applied: 8 bUnit tests re-leveled to pure unit tests (net +2 tests, 10× speed per test, broader combinatorial coverage). 3 new FsCheck properties reuse 2-3's infrastructure (1000 CI / 10000 nightly). R-Reentrancy + R-Circuit-Reconnect + R-Options-Hot-Reload coverage closed; Pre-mortem PM-D (auto-dismiss-timer dispose) + PM-F (focus-preservation) + Red Team RT-1 (XSS encoding) coverage added.

### Build & CI

- Build race CS2012: `dotnet build` then `dotnet test --no-build` (inherited pattern)
- No `AnalyzerReleases.Unshipped.md` update — HFC2100-2102 are runtime-logged only (architecture.md §648 precedent honored by 2-3 HFC2004-2007)
- Roslyn 4.12.0 pinned (inherited)
- `Microsoft.FluentUI.AspNetCore.Components` stays at `5.0.0-rc.2-26098.1` — do NOT bump in this story
- E2E test `[Trait("Category", "E2E")]` — new CI job `command-latency-gate` filters `--filter Category=E2E` and runs the Counter Aspire topology via `dotnet run --project samples/Counter/Counter.AppHost &` before running the Playwright tests

### Previous Story Intelligence

**From Story 2-3 (immediate predecessor — review status):**

- **Binding consumer contract (D19):** `FcLifecycleWrapper` MUST consume `ILifecycleStateService.Subscribe` and NEVER read `{Command}LifecycleFeature` state directly. This is the single hardest gotcha — easy to slip into `@inject IState<IncrementCommandLifecycleFeatureState>` because that's the obvious Blazor-Fluxor idiom. The story above pins it in the cheat sheet, Decision D2, AC1, and AC9 all converge on the same invariant.
- **Monotonic anchor (D15):** Use `CommandLifecycleTransition.LastTransitionAt`, not `DateTime.UtcNow`. Story 2-3 already pays the implementation cost; skipping it here would regress Sally's Story C reconnect-staleness fix.
- **IdempotencyResolved flag (D10):** Logs HFC2101 in v0.1, full UX copy lands in Story 2-5. Do not drop the flag or the log — Story 2-5 will grep history for HFC2101 production hits to size the localization work.
- **Story 2-3 used `FakeTimeProvider` in `Microsoft.Extensions.TimeProvider.Testing`** for property-based state machine tests; reuse the same package for threshold timer tests here. 2-3 cut timed eviction (ADR-019) so the package may have been pulled from the Shell.Tests csproj — check Task 0.1.
- **ServiceCollectionExtensions precedent (Story 2-3):** AddHexalithDomain<T> now scans for types ending in `LifecycleBridge` AND `LastUsedSubscriber`. No change needed here — `FcLifecycleWrapper` is a Razor component, not a discovered domain type.
- **Options validator pattern precedent:** Story 2-3 did NOT add a validator; Story 2-4 is the first shell-options validator. Use `IValidateOptions<T>` rather than `ValidateDataAnnotations()` for cross-property validation. Register once.

**From Story 2-2:**

- `CommandFormEmitter` L380-396 already handles the in-button spinner + disabled-while-submitting behaviour — D7 says DO NOT alter this. Story 2-1 Decision D13 owns the in-button visual; don't overreach.
- Snapshot re-approval pattern: re-approve only the files that changed, inspect diffs first to confirm only the intended wrap addition (not accidental trailing whitespace, not unrelated format changes).

**From Story 2-1:**

- `_submittedCorrelationId` is the form's CorrelationId field name in emitted Razor (introduced in the emitter for Fluxor action CorrelationId plumbing). Verify the field name at Task 4.1 before wrapping; if it's renamed in a later patch, use the current name.

### Lessons Ledger Citations (from `_bmad-output/process-notes/story-creation-lessons.md`)

- **L01 Cross-story contract clarity:** The cheat sheet's "Binding contract with Story 2-3" and "Binding contract with Stories 2-1 / 2-2" sections + ADR-020/021/022/023 lock the cross-story seams explicitly. Reference: Story 2-3 D19 cited 4× in this spec so the invariant can't be lost.
- **L04 Generated name collision detection:** No new emitted artifact added (D1), so no collision risk. The emitter wrap uses the existing `<FcLifecycleWrapper>` naked component name — collides with no other generated file name. Scoped CSS file (`FcLifecycleWrapper.razor.css`) collides with no existing file in `Shell/Components/`.
- **L05 Hand-written service + emitted per-type wiring:** The wrapper is hand-written; wiring into generated forms is done by modifying `CommandFormEmitter` (not emitting per-command subscribers). Matches the Story 2-3 bridge-pattern split.
- **L06 Defense-in-depth budget:** 23 Decisions after party-mode review + advanced-elicitation (D20 + D21 + D22 + D23 added) — under the ≤25 feature-story cap. Occam trim applied (ADR-023 options-consolidation demoted to Dev Note). No further trim needed.
- **L07 Test cost-benefit:** 47 new tests / 23 decisions = ~2.0/decision, tighter than 2-3's 2.3/decision and 2-2's 3.1/decision. All threshold-boundary tests (5.2) use `FakeTimeProvider` for determinism — avoids the Story 2-2 TestGuidFactory / TestUlidFactory proliferation. 8 bUnit tests re-leveled to pure unit tests per Murat — cheaper + faster + broader combinatorial coverage (net +2 tests for substantially higher signal). Advanced elicitation added 2 tests covering specific production-regret failure modes (auto-dismiss-timer dispose, focus-preservation), at high value per test.
- **L09 ADR rejected-alternatives discipline:** ADR-020 cites 4, ADR-021 cites 2, ADR-022 cites 3, ADR-023 cites 2 (the former ADR-024 renumbered after advanced-elicitation demoted former-ADR-023 options-consolidation to a Dev Note). Minimum 2 per ADR satisfied on all four. ADR-020's 4 surfaced the "wrap at renderer chrome" alternative that nearly won; ADR-021 lead-rationale rewritten to reconnect-anchor-recomputation per Winston review; ADR-023 pins hot-reload semantics per Winston review + rate-limit per Chaos Monkey CM-3.
- **L10 Deferrals name a story:** All 10 Known Gaps cite a specific owning story (no "Epic N" or "future" vagueness).
- **L11 Dev Agent Cheat Sheet:** Present despite the story being under 30 decisions — feature story with cross-story bindings benefits from the fast-path entry.

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-2-command-submission-lifecycle-feedback.md#Story 2.4 — AC source of truth, §970-1017]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR2 — FcLifecycleWrapper requirements, §315]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR48 — sync pulse frequency rule, §361]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR49 — sync pulse + focus ring coexistence, §362]
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md#NFR1-2 — P95/P50 command latency SLOs, §1344-1345]
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md#NFR11-14 — progressive visibility thresholds]
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md#NFR88 — zero "did it work?" hesitations]
- [Source: _bmad-output/planning-artifacts/prd/functional-requirements.md#FR23 — five-state lifecycle]
- [Source: _bmad-output/planning-artifacts/prd/functional-requirements.md#FR30 — exactly-one user-visible outcome]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#FcLifecycleWrapper — component anatomy, §1803-1842]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#Threshold configuration — SyncPulseThresholdMs deployment override, §1835]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md#Accessibility — aria-live politeness + reduced-motion contract, §1838-1842]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#ILifecycleStateService publisher/subscriber architecture, §1698-1723]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md#Brand-signal fusion frequency rule, §750]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#Zero-override commitment for custom components, §1690]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#Tier 1 testing strategy for FcLifecycleWrapper, §2162-2166]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/index.md#Fluent UI v5 IToastService removal, §492]
- [Source: _bmad-output/planning-artifacts/architecture.md#397 — D2 Fluxor lifecycle + wrapper (v0.1 → v1)]
- [Source: _bmad-output/planning-artifacts/architecture.md#536 — CommandLifecycleState ephemeral; not persisted]
- [Source: _bmad-output/planning-artifacts/architecture.md#648 — HFC diagnostic ID ranges; 2xxx runtime-logged]
- [Source: _bmad-output/planning-artifacts/architecture.md#920-935 — Shell/Components/Lifecycle folder structure]
- [Source: _bmad-output/planning-artifacts/architecture.md#1144 — Contracts must not reference other packages (dependency-free — FcShellOptions extension stays in Contracts)]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management/critical-decisions-read-first-do-not-revisit.md#Decision D19 — single-writer invariant + binding consumer contract for Story 2-4]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management/critical-decisions-read-first-do-not-revisit.md#Decision D15 — CommandLifecycleTransition.LastTransitionAt monotonic anchor]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management/critical-decisions-read-first-do-not-revisit.md#Decision D10 — IdempotencyResolved detection-only, no terminal synthesis]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management/critical-decisions-read-first-do-not-revisit.md#Decision D11 — ConnectionState deferred to Story 5-3]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management/architecture-decision-records.md#ADR-017 — service as cross-command correlation index]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management/architecture-decision-records.md#ADR-018 — bespoke callback subscription contract]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes/architecture-decision-records.md#ADR-016 — renderer/form split]
- [Source: _bmad-output/implementation-artifacts/2-1-command-form-generation-and-field-type-inference.md#L380-396 — in-button FluentSpinner during Submitting]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md — running list of known deferrals]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L01 — cross-story contract clarity]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L05 — hand-written service + emitted wiring]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L06 — ≤25 decisions budget for feature story]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L07 — test count cost-benefit]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L09 — ADR rejected-alternatives discipline]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L10 — deferrals name a story]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L11 — cheat sheet for cross-story-binding stories]
- [Source: memory/feedback_no_manual_validation.md — automated E2E preference; Task 6 honors with Playwright + Aspire]
- [Source: memory/feedback_cross_story_contracts.md — explicit cross-story contracts per ADR-016 canonical example; ADR-020/021/022 here mirror the pattern]
- [Source: memory/feedback_tenant_isolation_fail_closed.md — D13 inherits Story 2-3 D13 rationale (ephemeral, no persisted data)]
- [Source: memory/feedback_defense_budget.md — 19 decisions, under the ≤25 feature-story cap]

### Project Structure Notes

- **Alignment with architecture blueprint** (architecture.md §920-935):
  - New `Shell/Components/Lifecycle/` folder — architecture.md §929-931 designates this folder for `FrontComposerLifecycleWrapper.razor` + `.razor.cs`. Our filename is `FcLifecycleWrapper` (per Fc-prefix convention UX spec §1692 vs. the architecture's "FrontComposer*" prefix), resolving the naming conflict as follows: the UX spec's `Fc` prefix is canonical for custom components per §1692 "Naming convention: All custom components use the `Fc` prefix"; the architecture doc's `FrontComposer*` names in the folder tree are pre-UX-spec-finalization and represent an earlier draft. **Decision to use `Fc` prefix is bound here** — pull the rest of the architecture blueprint filenames into the `Fc` prefix pattern as those components land in Epic 3+ stories; do NOT rename existing `FrontComposer*` service classes (that's Story 9-x).
  - New `Shell/Components/Lifecycle/` is a new subfolder under `Shell/Components/`, consistent with existing `Shell/Components/Rendering/` (which holds `FcFieldPlaceholder.razor` — v0.1 precedent for `Fc` prefix).
  - `FcShellOptions` extension stays in Contracts — preserves the dependency-free invariant (architecture.md §1144). The four new threshold `int` properties are pure POCO; no new namespace pulled in.
  - New `Shell/Options/` folder for `FcShellOptionsThresholdValidator.cs` — consistent with the existing `Shell/Services/` and `Shell/Infrastructure/` folder-per-concern organization.
  - No new test project — existing `Hexalith.FrontComposer.Shell.Tests` absorbs all new tests.

- **Detected conflicts or variances:**
  - **`FrontComposerLifecycleWrapper` vs. `FcLifecycleWrapper` naming** — as above, UX spec `Fc` prefix wins; architecture blueprint §929-930 references `FrontComposerLifecycleWrapper` and is stale. Treat blueprint names as advisory, UX spec names as canonical.
  - **`FcShellOptions` now holds both form-shell options (FullPageFormMaxWidth, DataGridNavCap) and lifecycle options (SyncPulseThresholdMs et al.)** — this is intentional per D12. Future split into `FcFormOptions` + `FcLifecycleOptions` is a refactor for Story 9-2 if `FcShellOptions` grows beyond ~10 properties; not in scope for 2-4.
  - **Playwright package not yet referenced in Shell.Tests csproj** — Task 0.2 + Task 6.1 detect and add if absent. Not a blocker, just a one-line csproj edit.

---
