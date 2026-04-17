# Appendix: Review & Elicitation History

This appendix preserves the audit trail from the story's iterative review process. It is NOT required reading for implementation — the authoritative spec is in the sections above. Useful for:
- Retrospectively understanding why a decision was made
- Debugging a decision's rationale if an adopter challenges it
- Feeding into the `_bmad-output/process-notes/story-creation-lessons.md` ledger for future story creation

The appendix has three parts in chronological order:

### A.1 Party Mode Round 1 (Multi-Agent Review)

Four agents (Winston, Amelia, Sally, Murat) reviewed the first draft. Key changes:

| Concern | Resolution | Reference |
|---|---|---|
| `DataGridNavigationState` half-shipped (effects without producers) | Trimmed to REDUCER-ONLY in Story 2-2; effects deferred to Story 4.3 | Decision D30, Task 6.2 |
| Renderer vs Form submit ownership ambiguity (double `<EditForm>` risk) | Renderer is chrome; Form owns submit. Explicit contract. | ADR-016, Decisions D21 |
| Naming collision `IncrementCommandCommandRenderer` | Initially proposed trailing-`Command` strip. **Subsequently reverted in R2 Trim** — uniform `{CommandTypeName}Renderer` / `{CommandTypeName}Page`. Display label strips " Command" for UX only via Decision D23 `DisplayLabel`. | Decision D22 (final), Decision D23, Naming table |
| Button label inconsistency ("Send" prefix) | Unified to `{DisplayLabel}` in all modes (HumanizeCamelCase of TypeName with trailing " Command" stripped for display); Story 2-1 snapshots re-approved | Decision D23, Task 5.3 |
| Pre-fill chain LastUsed-beats-Default footgun | `[DefaultValue]` becomes a hard floor BEFORE LastUsed in the chain | Decision D24, Task 3.4 |
| JS module re-import per component + prerender crash risk | Scoped `IExpandInRowJSModule` with `Lazy<Task<IJSObjectReference>>` cache + prerender guard | Decision D25, Task 4.9 |
| 720px hard-coded literal | `FcShellOptions.FullPageFormMaxWidth` (default 720px) | Decision D26, AC4 |
| `ProjectionContext` cascading unowned | Epic 4 cascades at shell; Story 2-2 ships null-tolerant renderer + Counter manual `<CascadingValue>` | Decision D27, Task 9.3 |
| `LastUsedValueProvider` Fluxor subscription unclear | Hand-written generic provider; per-command emitted typed subscriber (`{CommandTypeName}LastUsedSubscriber.g.cs`) | Decision D28, Task 4bis |
| `FluentPopover` outside-click dismissal semantics | Manual wiring via backdrop click handler | Decision D29 |
| Focus return on popover submit/dismiss | Explicit AC + 2 dedicated tests | AC9, Task 10.1 #8-9 |
| Counter AC10 state-restore "theater" (no capture side) | AC10 no longer claims end-to-end state restoration (deferred to Story 4.3); only proves `RestoreGridStateAction` dispatch contract | AC10, Decision D30 |
| Missing 2-1 regression gate | 12 byte-identical snapshot assertions in CI | Task 5.2 |
| Missing 2-1↔2-2 contract test | Structural equality test | Task 11.4 |
| bUnit JS-interop flakiness risk | `JSRuntimeMode.Loose` prohibited for Task 10.2; explicit `SetupModule` + `VerifyInvoke` required | Task 10 fixture rules |
| Pre-fill race (OnInitializedAsync vs render) | `cut.WaitForAssertion(...)` mandatory; no synchronous `Find` post-render | Task 10 fixture rules |
| Density classification test redundancy | Replaced 9 example tests with 1 FsCheck property + snapshot boundary + equality/hash = 4 tests | Task 1.4 |
| Axe-core count unfalsifiable ("per mode") | Explicit 3 tests (one per mode) + separate keyboard tab-order tests | Task 12.1, Task 10.5 |
| Task 13.4 "automated" ambiguity | Transparency note — dev-agent local, not CI-automated E2E | Task 13.4 |

### A.2 Advanced Elicitation Round 2 (Pre-mortem / Red Team / First Principles / Chaos / Hindsight)

Five-method pass identified security gaps and robustness edges:

| Concern | Severity | Resolution | Reference |
|---|---|---|---|
| Cross-tenant PII leak when adopter forgets `IHttpContextAccessor` wiring | 🔴 P0 (security) | `LastUsedValueProvider` refuses read/write when tenant or user is null/empty | Decision D31, Task 3.5 |
| Open-redirect CVE via `ICommandPageContext.ReturnPath` | 🔴 P0 (security) | Relative-URI validation + log-and-navigate-home on violation | Decision D32, AC4 clause, Task 10.3 #8-9 |
| DoS via 10k fields, `[DefaultValue]` mismatch, nested Command | 🔴 P0 (security/correctness) | **HFC1011/1012/1014** parse-time diagnostics. [NOTE: HFC1009 invalid ident, HFC1010 invalid icon, HFC1013 name collision were also proposed here but subsequently cut in A.3 Trim — see trim table below.] | Task 0.7, Task 1.3a–c, Task 1.4 #5–7 |
| Storage quota DoS (DataGridNav unbounded) | 🟠 P1 | DataGridNav LRU cap of 50 via `FcShellOptions.DataGridNavCap`. [LastUsed 1000/tenant cap was initially proposed here but subsequently cut in A.3 Trim — deferred to adopter signal.] | Decision D33, Task 6.1, Task 6.4 |
| `[Icon]` typo → compile error in generated code (framework-bug perception) | 🟠 P1 | Runtime try/catch fallback to default icon + warning log | Decision D34, Task 4.7, Task 10.1 #12 |
| `{CommandTypeName}LastUsedSubscriber` hot-reload accumulation + eager-resolution startup latency | 🟠 P1 | Idempotent registration via `LastUsedSubscriberRegistry` + lazy-on-first-dispatch resolution | Decision D35, Task 4bis.2, Task 4bis.3 #6–7 |
| Circuit reconnect loses popover draft silently (NFR88 violation) | 🟠 P1 | Fail-closed close + warning log; full preservation is 2-5 | AC9 clause, Task 10.1 #10 |
| Trigger button scrolled off-screen when `Confirmed` arrives 2s later | 🟠 P1 | Scroll-into-view MUST precede focus-return | AC9 clause, Task 10.1 #8 |
| Popover + FluentDialog z-index collision | 🟠 P1 | **[Subsequently cut in A.3 Trim below]** `IPopoverCoordinator` was proposed but deferred to Story 2-5. Popovers now expose `ClosePopoverAsync()` for 2-5 to integrate. | Known Gaps, A.3 Trim |
| FullPage form ships without ANY abandonment guard for 6 weeks until 2-5 | 🟠 P1 | **[Subsequently cut in A.3 Trim below]** Minimal `beforeunload` guard was proposed but deferred to Story 2-5 (half-UX rejected). | Known Gaps, A.3 Trim |
| All-fields-derivable edge case under-tested | 🟡 P2 | Explicit 0-field inline test | Task 10.1 #11 |
| Renderer/Form split (ADR-016) never challenged by party review | 🟡 P2 | First-principles fold-in alternative explicitly considered and rejected in ADR-016 | ADR-016 rejected alternatives |

### A.3 Elicitation Round 2 Trim (Occam + Matrix — Cuts Applied)

Multi-method scoring (Occam's Razor + Critical Challenge + Comparative Matrix) identified over-engineering in the Round 2 additions. Cuts applied to tighten scope without losing core safety:

| Cut | Matrix Score | Reason | Destination |
|---|---|---|---|
| **Decision D22 — strip trailing `Command` suffix** | n/a (Occam) | Cosmetic benefit didn't justify HFC1013 collision-detection + naming edge-cases. Reverted to full `{CommandTypeName}` naming (`IncrementCommandRenderer`). | Removed entirely |
| **HFC1009** (invalid identifier) | 2.40 | Roslyn rejects invalid C# identifiers natively; parse-time duplication | Deferred (redundant) |
| **HFC1010** (invalid icon format) | 3.35 | Redundant with Decision D34 runtime icon fallback | Deferred (redundant) |
| **HFC1013** (BaseName collision) | 2.55 | Only existed because of D22; cut together | Removed with D22 |
| **Decision D33 LastUsed LRU cap** (1000 keys) | 2.75 | Arbitrary number; v0.1 has no DoS evidence. DataGridNav cap (50) retained. | Deferred to adopter signal |
| **`IPopoverCoordinator` service + contract** | 2.45 | Speculative future-proofing — Story 2-5 doesn't exist yet. Popovers expose `ClosePopoverAsync()` for 2-5 to integrate when it lands. | Deferred to Story 2-5 |
| **FullPage beforeunload minimal guard** | 2.25 | Native `confirm()` creates prompt-fatigue debt before 2-5's real UX. | Deferred to Story 2-5 |

**Net impact:** 118 tests → 111 tests; 9 new diagnostics → 4 new diagnostics; 2 new services → 1 service; ~6 hours dev effort saved. All P0 security retained (D31, D32, HFC1011/1012/1014). Core UX retained (AC9 focus return, scroll-before-focus, circuit reconnect handling).

### A.4 Round 3 Consistency Pass (Self-Consistency + Rubber Duck + Thread of Thought)

Scan after the Round 2 trim surfaced orphan placeholder references and two real implementation gaps:

| Finding | Resolution | Reference |
|---|---|---|
| 17+ orphan `{BaseName}` / `{FullCommandTypeName}` references after D22 revert | Global placeholder normalization to `{CommandTypeName}`; `BaseName` IR field replaced by `DisplayLabel` (display-only) | Doc-wide |
| Stale "HFC1010 parse-time" reference in Task 4.7 | Removed; runtime fallback (D34) is sole icon validation layer | Task 4.7 |
| RegisterExternalSubmit race (silent click-drop during SSR→interactive transition) | 0-field button disabled until Form registers external submit; re-render on registration | Decision D36, AC2, Task 10.1 #13 |
| Multiple simultaneous Inline popovers (ambiguous v1 behavior) | At-most-one open via new `InlinePopoverRegistry` scoped service | Decision D37, AC2, Task 10.1 #14 |
| Counter sample tenant/user wiring gap | Task 9.4 adds demo `IHttpContextAccessor` stub | Task 9.4 |
| Fluxor double-scan risk | New integration test `Fluxor_AssemblyScan_NoDuplicateRegistration` | Task 6.3 |
| ADR-016 hard to explain simply | One-liner added: "Form = engine. Renderer = shape. One form, three possible shapes." | ADR-016 TL;DR |

### A.5 Round 4 Polish (Critique + Persona Focus Group + Reverse Engineering + Explain Reasoning + Yes-And)

Reader-validation pass:

| Finding | Resolution | Reference |
|---|---|---|
| History tables between Story and Critical Decisions created narrative speed bump for first-time readers | **Moved to this appendix** — current spec flows directly from Story → Critical Decisions | Doc structure |
| Legacy test counts 97, 111 lingered in doc body | Scrubbed — only 114 is authoritative | Task 13.1 |
| No adopter migration guide | Task 9.7 added: write note to `deferred-work.md` | Task 9.7 |
| No telemetry hook for mode/density usage | Observability `ILogger.LogInformation` emitted in renderer `OnInitialized` | Task 4.3 |
| Standalone CompactInline multiplicity undefined | AC3 clarifies DataGrid container (Story 4.5) enforces; 2-2 standalone is unconstrained | AC3 |
| Future-extension hints not captured | Known Gaps extended with 3 speculative items (MCP manifest, custom RenderMode resolver, LastUsed audit) | Known Gaps |

### A.6 Party Mode Review (Post-Checklist, 2026-04-15)

After the 7-fix checklist pass, Jerome ran a Party Mode multi-agent review (Winston, Amelia, Murat, Sally spawned as independent subagents). All four converged on: **the 7 fixes closed surface holes but C5 opened a deeper architectural seam**. Findings applied:

| Finding | Agent | Resolution | Reference |
|---|---|---|---|
| Story 2-1 `SubmittedAction(string CorrelationId, TCommand Command)` carries typed payload ✓; `ConfirmedAction(string CorrelationId)` does NOT — scalar `_pendingCommand` field would cross-contaminate interleaved submits | Amelia (verified via grep of `CommandFluxorActionsEmitter.cs`) | Redesigned subscriber to `ConcurrentDictionary<string, PendingEntry>` keyed by CorrelationId | Task 4bis.1 code, Decision D38 |
| Pending-command state has no bounded lifetime — orphaned Submitted without Confirmed leaks on long-lived circuits | Winston + Murat | TTL=5min (command lifecycle upper bound) + MaxInFlight=16 per type per circuit; eviction emits `LogWarning` | Decision D38, Task 4bis.3 tests T-race-1/T-race-2/T-orphan/T-dispose/T-cap |
| Storage-key naive `:`-concat breaks under NFC/NFD, whitespace, case variance, `:` in email local-part, `:` in tenant ID | Murat | `FrontComposerStorageKey.Build(...)` canonicalization helper (NFC + URL-encode + email-lowercase); FsCheck roundtrip property test | Decision D39, Task 3.5a, Task 3.9 test #19 |
| D22 revert residue: `{CommandBaseName}` still in AC4 route example at line 281 | Winston | Changed to `{CommandTypeName}` | AC4 |
| Fail-closed D31 silently no-ops for dev who hasn't wired `IHttpContextAccessor` — UX bug as security feature | Sally (Journey 3) | Added dev-mode `<FluentMessageBar>` surface via `IDiagnosticSink` + `<FcDiagnosticsPanel>`; prod-stripped | Task 3.5, Task 3.5a, Counter `CounterPage.razor` |
| "Second-try feature" UX on first session — user expects LastUsed, gets empty form with no signal | Sally (Journey 1) | First-session caption "Your last value will be remembered after your first submission" in popover when chain returned no value | AC2 |
| Counter FullPage demo without state persistence feels broken | Sally (Journey 2) | Explicit `<FluentMessageBar Intent="Informational">` "Navigation state persistence lands in Story 4.3" above the full-page anchor | Task 9.3 |
| Task 13.3 as "scenario catalog" left ambiguous execution authority | Winston (E2 half-done) | Merged into Task 13.3 as single automated E2E task with machine-readable JSON result artifact + explicit DOM/console assertion predicates per scenario | Task 13.3 (merged), Task 13.4 (merged-away) |
| Test count 114 would drift stale after C3/C4 strikethroughs + Party Mode additions (7 new tests) | Amelia + Murat | Recomputed to 121, added CI gate: `dotnet test --list-tests | wc -l` MUST match rollup at merge | Task 13.1 |

Net impact: 114 tests → 121 tests, 37 decisions → 39 decisions, one race-prone scalar field → bounded concurrent dict, manual/automated split → one automated E2E with machine-verifiable artifact. Story remains `ready-for-dev`.

### A.7 Process Lessons Harvested

Reusable patterns from this review process have been saved to `_bmad-output/process-notes/story-creation-lessons.md` (L01–L11) and three user-memory entries in `~/.claude/projects/.../memory/`. Apply these when creating Stories 2-3, 2-4, 2-5, and Epic 4+ stories.

---
