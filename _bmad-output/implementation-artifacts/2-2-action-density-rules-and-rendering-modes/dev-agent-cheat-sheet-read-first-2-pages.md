# Dev Agent Cheat Sheet (Read First — 2 pages)

> Amelia-facing terse summary. Authoritative spec is the full document below. Every line here links to a section for detail.

**Goal:** emit one renderer per `[Command]` that selects Inline / CompactInline / FullPage based on `CommandDensity` classification, delegating all submit/validation to Story 2-1's `{CommandTypeName}Form`.

**13 files to create / extend:**

| Path | Action |
|---|---|
| `src/Hexalith.FrontComposer.Contracts/Attributes/IconAttribute.cs` | Create (Task 0.5) |
| `src/Hexalith.FrontComposer.Contracts/Rendering/CommandRenderMode.cs` | Create (Task 2.1) |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ICommandPageContext.cs` | Create (Task 2.2) |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionContext.cs` | Create (Task 2.3) |
| `src/Hexalith.FrontComposer.Contracts/Rendering/IDerivedValueProvider.cs` + `DerivedValueResult.cs` | Create (Task 3.1) |
| `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs` | Create/extend (Task 2.4, D26, D33) |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` | Extend — add `CommandDensity` enum + `Density`/`IconName` on `CommandModel` (Task 1) |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs` + `CommandRendererModel.cs` | Create (Task 4.1) |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` + `CommandPageEmitter.cs` + `LastUsedSubscriberEmitter.cs` | Create (Tasks 4, 4bis) |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Extend — add `DerivableFieldsHidden`, `ShowFieldsOnly`, `OnConfirmed`, `RegisterExternalSubmit` params (Task 5.1) |
| `src/Hexalith.FrontComposer.Shell/Services/DerivedValues/{System,ProjectionContext,ExplicitDefault,LastUsed,ConstructorDefault}ValueProvider.cs` | Create 5 (Task 3.2–3.6) |
| `src/Hexalith.FrontComposer.Shell/Services/{IExpandInRowJSModule,ExpandInRowJSModule,LastUsedSubscriberRegistry}.cs` | Create 3 (Task 4.9, D25, D35) |
| `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/` | Create feature — State/Actions/Reducers/Feature, NO effects (Task 6.1, D30) |
| `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-expandinrow.js` | Create (Task 7.1) |
| `samples/Counter/Counter.Domain/{BatchIncrementCommand,ConfigureCounterCommand}.cs` | Create (Task 9.1–9.2) |
| `samples/Counter/Counter.Web/Pages/CounterPage.razor` | Update — wrap renderers in `<CascadingValue>` (Task 9.3) |

**Generated naming (Decision D22 — full TypeName, no stripping):**
- `IncrementCommand` → `IncrementCommandRenderer.g.razor.cs`
- `ConfigureCounterCommand` → `ConfigureCounterCommandRenderer.g.razor.cs` + `ConfigureCounterCommandPage.g.razor.cs`
- Button label strips `Command` for UX only: `"Increment"`, `"Configure Counter"` (Decision D23)

**AC quick index (details in ACs section below):**

| AC | One-liner | Task(s) |
|---|---|---|
| AC1 | `CommandModel.Density` computed from `NonDerivableProperties.Length` | 1 |
| AC2 | Inline mode: 0 fields = button; 1 field = FluentPopover + single field (2-1 Form inside popover) | 4.4, 10.1 |
| AC3 | CompactInline mode: FluentCard with expand-in-row JS scroll stabilization | 4.4, 7, 10.2 |
| AC4 | FullPage mode: `/commands/{BC}/{CommandTypeName}` route, max-width via `FcShellOptions`, ReturnPath validated (D32) | 4.4–4.5, 10.3 |
| AC5 | `RenderMode` parameter override; HFC1015 runtime log on mismatch | 4.3, 10.4 |
| AC6 | 5-provider chain: System → ProjectionContext → ExplicitDefault → LastUsed → ConstructorDefault (D24) | 3, 4bis |
| AC7 | `DataGridNavigationState` reducer-only (effects deferred to Story 4.3, D30) | 6 |
| AC8 | Button hierarchy per UX spec §2236 (Secondary inline, Primary compact/full) | 4.4, 4.6 |
| AC9 | Accessibility: scroll-before-focus, circuit reconnect handling, keyboard tab order, axe-core clean | 10.1, 10.5, 12 |
| AC10 | Counter sample demonstrates all 3 modes (no state-restoration demo — deferred to 4.3) | 9 |

**Binding contract with Story 2-1 (ADR-016):** Renderer is CHROME ONLY. `{CommandTypeName}Form` owns `<EditForm>`, validation, submit, lifecycle dispatch. Renderer extends Form with 4 new params: `DerivableFieldsHidden`, `ShowFieldsOnly`, `OnConfirmed`, `RegisterExternalSubmit`. **Re-approve ALL 12 existing Story 2-1 `.verified.txt` snapshots** with the D23 `DisplayLabel` button text (was "Send X", now "X") in a single pre-emitter-change commit (Task 5.3). Byte-identical regression gate guards the contract (Task 5.2).

**ADR-016 one-liner:** Form = engine. Renderer = shape. One form, three possible shapes.

**4 new diagnostics (HFC numbers in AnalyzerReleases.Unshipped.md):**
- HFC1015 Warning — RenderMode/density mismatch (runtime log; analyzer emission → Epic 9) [renumbered from HFC1008 — collides with Story 2-1's Flags-enum diagnostic]
- HFC1011 Error — property count > 200
- HFC1012 Error — `[DefaultValue]` type mismatch
- HFC1014 Error — nested `[Command]` unsupported

**Scope guardrails (do NOT implement — see Known Gaps for owning story):**
- Destructive command Danger UX, 30s abandonment, `beforeunload` guard → **Story 2-5**
- DataGridNav `IStorageService` effects, hydration, capture-side wiring → **Story 4.3**
- Shell header breadcrumb, shell-level `ProjectionContext` cascade → **Stories 3.1 / 4.1**
- HFC1009 (invalid ident), HFC1010 (icon format), HFC1013 (BaseName collision) → deferred/redundant (see Round 2 Trim section)

**Test expectation: 121 new tests, cumulative ~463.** Breakdown at Task 13.1.

**Start here:** Task 0 (prereqs) → Task 1 (IR) → Tasks 2–3 (Contracts + providers + FrontComposerStorageKey helper) → Task 4 (emitter) → Task 4bis (LastUsed subscriber with D38 correlation-dict) → Task 5 (Form extension + regression gate) → Task 6 (Fluxor feature) → Task 7 (JS module) → Task 8 (pipeline wiring) → Task 9 (Counter sample + FcDiagnosticsPanel) → Tasks 10–12 (tests) → Task 13 (automated E2E with machine-readable artifact).

**The 39 Decisions and 4 ADRs in the sections below are BINDING. Do not revisit without raising first.** (Updated in Round 3: D36 = 0-field button disabled until Form's `RegisterExternalSubmit` fires; D37 = at-most-one Inline popover open at a time via `InlinePopoverRegistry`. Updated in Party Mode review: D38 = LastUsed subscriber uses `ConcurrentDictionary<CorrelationId, PendingEntry>` with TTL=5min + MaxInFlight=16 eviction; D39 = storage-key segments NFC-normalized + URL-encoded + email-lowercased via `FrontComposerStorageKey.Build(...)` helper.)

---
