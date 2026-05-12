# Story 11.4: Drift Detection and Source Generator Coverage Hardening

Status: in-progress

> **Epic 11** - Deferred Hardening & Release Readiness. Closes drift-detection, SourceTools generator coverage, metadata-drift tests, performance evidence, deterministic output, and older parser/transform/emitter follow-ups routed from Stories 9.1, 1.4, 1.5, 1.8, and related SourceTools reviews. Applies lessons **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 11-4 is the release-readiness hardening pass for SourceTools drift detection and generator coverage.

Story 9-1 delivered opt-in generated UI drift detection through structural baselines, HFC1058-HFC1070 diagnostics, baseline trust checks, deterministic structural comparison, and trim/AOT advisory behavior. Later review rounds left focused gaps around `PublishAot`-only HFC1070 behavior, AC11 drift performance evidence, diagnostic structural comparison, baseline path assertions, metadata-drift coverage, and several low-level SourceTools hardening items that were too narrow for their original stories.

This story implements or explicitly accepts those follow-ups with evidence. The intended outcome is that build-time drift diagnostics keep their release promises, generated output remains deterministic and sanitized, and remaining SourceTools limitations are visible in the deferred ledger instead of hidden in old review notes.

---

## Story

As a framework maintainer,
I want drift detection and generator coverage gaps closed or explicitly accepted,
so that build-time diagnostics keep their release promises.

### Release-Readiness Job To Preserve

A maintainer preparing a release candidate should be able to run focused SourceTools drift/generator tests and know which deferred SourceTools gaps are fixed, accepted as constraints, or split to a named owner with evidence.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary production files | Harden `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`, `Drift/DriftDetection.cs`, `Parsing/AttributeParser.cs`, `Parsing/CommandParser.cs`, `Transforms/*`, and emitter files only where Story 11.4-owned rows require it. |
| Primary test files | Extend `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/**`, `Benchmarks/DriftBenchmarkTests.cs`, `Parsing/**`, `Transforms/**`, `Emitters/**`, and `Integration/**` as needed. |
| Deferred ledger | Close or explicitly accept `DEF-9-1*`, Story 1.4/1.5/1.8 SourceTools rows, and SourceTools-specific follow-ups routed to Story 11.4 in `_bmad-output/implementation-artifacts/deferred-work.md`. |
| Drift promises | Preserve structural baselines, deterministic ordering, sanitized diagnostics, and HFC1058-HFC1070 help-link/property contracts. |
| Generator promises | Preserve incremental pipeline behavior, `netstandard2.0` SourceTools compatibility, no runtime reflection as the primary discovery path, and deterministic generated hint names. |
| Scope guardrail | Do not reopen CLI/IDE hardening, diagnostic registry governance, MCP schema negotiation, shell UX, EventStore reliability, or release workflow credential work. |
| Validation | Start with SourceTools focused tests; run drift performance tests intentionally as `Category=Performance`; record skipped/advisory status explicitly. |

Start here: T1 inventory Story 11.4 deferred rows -> T2 patch HFC1070 and drift perf evidence -> T3 strengthen structural/path/metadata drift tests -> T4 triage older parser/transform/emitter gaps -> T5 update ledger and evidence.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- |
| AC1 | Story 11.4-owned deferred rows exist in `deferred-work.md` | Story 11.4 completes | Each row is marked resolved, superseded, split, or accepted with date, owner, rationale, and validation evidence; no row is silently deleted. |
| AC2 | A project has `[Projection]` or `[Command]` contracts and `PublishAot=true` with `PublishTrimmed=false` | SourceTools runs | HFC1070 behavior matches AC14 from Story 9.1: emit the trim/AOT advisory when no adopter `IActionQueueProjectionCatalog` override is statically observable, and keep no-diagnostic behavior for unrelated projects without Contracts. |
| AC3 | A project has `PublishTrimmed=true`, `PublishAot=true`, both, or neither | Trim/AOT tests run | The matrix covers all four combinations plus adopter override evidence and no-Contracts defensive silence. |
| AC4 | Drift detection AC11 performance evidence is assessed | SourceTools performance tests run | Cache-hit and cache-miss paths report warmup-excluded median and p95 values for a representative bounded drift fixture; skipped/advisory status is either removed with stable evidence or explicitly accepted with rationale. |
| AC5 | Drift diagnostics are compared in tests | Order and equality assertions run | Tests compare full diagnostic shape including ID, severity, message, location/path where applicable, and properties bag; no test relies only on `Id + "|" + Message` when severity/properties matter. |
| AC6 | Baseline path redaction and path-normalization assertions run on Windows and non-Windows path shapes | Tests evaluate paths | Assertions reject Windows drive-root absolute paths specifically without failing benign repo-relative values containing `:`. |
| AC7 | Baseline JSON contains BOM, malformed content, duplicate identities, unsafe values, or boundary-length content | Loader tests run | Trust failures remain deterministic, bounded, sanitized, and do not leak raw absolute paths, payload fragments, tenant/user identifiers, tokens, or raw JSON snippets. |
| AC8 | Metadata drift affects badge mappings, destructive command state, projection role, currency/display format, empty-state CTA, icon, policy, display, description, priority, or field group | Drift comparison tests run | Exactly one HFC1066 emits per declaration/member/category where applicable, and each high-value metadata category has a focused test or a recorded accepted constraint. |
| AC9 | Generated output uses hint names and source keys | Two same-named contracts appear in different namespaces or unusual namespace display forms are parsed | Generated source hint names remain collision-safe and deterministic, or the limitation is explicitly accepted with a diagnostic/story owner. |
| AC10 | A `[BoundedContext]` name or display label contains display metadata, spaces, punctuation, XML-sensitive characters, or conflicting labels across declarations | Parser/transform/emitter tests run | Behavior is either implemented with safe diagnostics/escaping or explicitly accepted with evidence; invalid generated C# or XML doc output is not silently introduced. |
| AC11 | A malformed `[Projection]` type is present during edit-in-progress | Generator runs | The current tolerated inner-loop contract is preserved or improved: no generator crash, no generator-amplified compile errors, and any stricter zero-output behavior is test-backed before changing production. |
| AC12 | HFC1010 / RS2002 release-tracking policy is evaluated | SourceTools diagnostic descriptors are enumerated | Every real `DiagnosticDescriptor` has release-tracking coverage, reserved/comment-only IDs are handled intentionally, and blanket suppression remains only if justified with a test or documented Story 11.2 handoff. |
| AC13 | Parser/transform edge cases are exercised | SourceTools tests run | Policy alias collisions, Unicode/normalization in policy names, partial declaration ordering, unsupported fields, and struct projection edge cases are fixed, split, or accepted with evidence. |
| AC14 | Destructive command rendering and source-generator emitter snapshots are reviewed | Snapshot/approval tests run | Destructive command emission has representative snapshot coverage or a recorded accepted cost/risk decision; new snapshots must not create broad unrelated approval churn. |
| AC15 | Generated output contract tests run | Baselines and snapshots are inspected | Output remains deterministic, timestamp-free, local-path-free, secret-free, culture-invariant, and stable across identical structural inputs. |
| AC16 | SourceTools changes build under analyzer host constraints | Build/test runs | `Hexalith.FrontComposer.SourceTools` stays `netstandard2.0`, `IsRoslynComponent=true`, keeps CodeAnalysis dependencies private, and does not take Shell/Fluent UI/Fluxor runtime dependencies. |
| AC17 | Story 11.2 owns diagnostic registry/docs governance and Story 11.3 owns CLI/IDE hardening | Story 11.4 touches diagnostics or docs | Changes are limited to SourceTools drift/generator behavior and test evidence; registry/docs/HFCM/CLI work is handed off instead of silently absorbed. |
| AC18 | Validation completes | Story 11.4 moves to review | The Dev Agent Record lists commands, outcomes, touched files, unresolved accepted constraints, and evidence paths. |
| AC19 | A clean baseline/snapshot corpus exists for SourceTools drift and generated output | The drift verification command runs twice on the same commit | Both runs produce identical normalized results, leave the working tree unchanged, and prove the contract source of truth is the checked-in baseline/snapshot corpus plus compiler-backed fixture expectations. |
| AC20 | Generated output or drift baseline content is intentionally changed without updating the expected corpus | CI or the focused drift verification runs | The gate fails closed with an actionable diagnostic or test failure naming the expected artifact, actual artifact, normalized diff evidence, and the update command if one exists; CI must not silently regenerate expectations. |
| AC21 | Drift/generator tests run on Windows and non-Windows path shapes with different line endings and cultures | Fixtures are compiled and compared | Paths, separators, line endings, and culture-sensitive values are normalized; generated output contains no timestamps, local absolute paths, temp directories, user names, `bin/obj` paths, tokens, or tenant/user identifiers. |
| AC22 | Source generator coverage is evaluated beyond string-only assertions | Roslyn fixture compilations run | Tests inspect diagnostics, generated trees, hint names, incremental cache behavior, missing `AdditionalFiles`/metadata handling where applicable, and absence of parasite output for no-contract or malformed-contract inputs. |
| AC23 | Performance evidence is kept or accepted as advisory | `Category=Performance` drift benchmark tests run or are explicitly skipped | The Dev Agent Record captures environment, baseline source, threshold/rationale, median/p95 results, and whether the gate is blocking, advisory, or accepted unstable with owner and follow-up. |
| AC24 | Story 11.4 implementation pressure reveals broader generator architecture or public contract changes | The dev identifies work outside the owned SourceTools rows | Refactors, API changes, snapshot format overhauls, auto-fix CLI modes, and cross-story contract consolidation are recorded as deferred decisions instead of being absorbed into this story. |
| AC25 | Story 11.4 starts and ends deferred-row reconciliation | The implementer inventories routed rows and later updates `deferred-work.md` | The starting inventory, alias map, and final bucket counts reconcile exactly; duplicate aliases, split rows, accepted constraints, and unresolved rows are named without silently dropping any row. |
| AC26 | A Story 11.4 row is accepted or split instead of fixed | The Dev Agent Record and ledger are updated | The entry includes likelihood/impact rationale, release risk, validation evidence or reason skipped, owner, revisit trigger, and why the acceptance does not invalidate release readiness. |
| AC27 | Drift, generator, snapshot, benchmark, or ledger evidence is written | The evidence is committed or referenced | A bounded redaction scan covers updated evidence surfaces and fails closed on local absolute paths, temp roots, user names, `bin/obj`, tokens, tenant/user identifiers, raw JSON snippets, or source payload fragments. |
| AC28 | A fixture, benchmark, or approval path cannot run consistently on the current platform | Validation is recorded | The outcome is `blocking`, `advisory`, `accepted unstable`, or `split` with explicit platform/environment conditions; tests must not pass by silently reducing assertions or fixture coverage. |
| AC29 | SourceTools fixes touch diagnostic IDs, severities, help links, release-tracking rows, or registry/docs governance | Implementation proceeds | The change stays within existing Story 11.4 SourceTools behavior or is handed to Story 11.2 with evidence; new governance policy is not invented inside this story. |
| AC30 | A generator limitation remains after Story 11.4 | The limitation is recorded as an accepted constraint | The constraint names the affected surface, bounded fixture proving current behavior, downstream impact, owner, and condition that would reopen the decision; vague "low priority" closure is not sufficient. |

---

## Tasks / Subtasks

- [ ] T1. Inventory and classify Story 11.4 deferred rows (AC1, AC17, AC18)
  - [ ] Read `_bmad-output/implementation-artifacts/deferred-work.md` from top to bottom.
  - [ ] Capture unresolved `DEF-9-1A-*`, `DEF-9-1B-*`, `DEF-9-1C-*`, older Story 1.4/1.5/1.8 SourceTools rows, and SourceTools-specific parser/emitter rows routed to Story 11.4.
  - [ ] Classify each row as fix now, accept with evidence, split to Story 11.2/11.3/11.6/11.7, or leave blocked with a named decision.
  - [ ] Record a starting inventory table with canonical row ID, aliases, source story/review, owning ACs, selected bucket, and expected evidence before editing production code.
  - [ ] Reconcile the final ledger buckets back to the starting inventory; duplicate aliases and split rows must keep a backlink to the canonical row.
  - [ ] Identify the checked-in baseline/snapshot/fixture corpus that is the source of truth for each drift or generator row before changing production code.
  - [ ] Freeze the owned file and fixture perimeter for this story; record any desired broad generator refactor, public API change, or snapshot-format change as deferred.
  - [ ] Preserve historical review text; append resolution markers rather than rewriting the ledger.

- [ ] T2. Patch trim/AOT and performance evidence gaps (AC2-AC4, AC16)
  - [ ] Update `DriftOptions` / `FrontComposerGenerator` so `PublishAot=true && PublishTrimmed=false` participates in the HFC1070 advisory gate.
  - [ ] Restore the PublishAot-only test case in `TrimAotReflectionCatalogDiagnosticTests`.
  - [ ] Keep adopter override evidence and no-Contracts defensive silence behavior pinned.
  - [ ] Convert `DriftBenchmarkTests` from red-phase skip to actionable `Category=Performance` coverage if stable on the target environment, or document why it remains advisory.
  - [ ] Record median and p95 evidence for cache-hit and cache-miss drift paths with warmup excluded.
  - [ ] Record the benchmark baseline source, threshold rationale, execution environment, and whether the performance gate is blocking or advisory.
  - [ ] If the benchmark remains skipped or advisory, record the exact environment condition and the release-risk rationale instead of treating the skip as closure.

- [ ] T3. Strengthen drift diagnostic comparison, path, and baseline trust tests (AC5-AC7, AC15)
  - [ ] Replace `Id + "|" + Message` comparisons with a shared diagnostic-shape assertion helper where severity, path, and properties matter.
  - [ ] Update baseline path checks to reject drive-root absolute paths specifically instead of rejecting every colon.
  - [ ] Add or confirm boundary tests for BOM-only/minimal baseline text, malformed JSON, duplicate identity origin pairs, load-phase diagnostic caps, and redaction precedence.
  - [ ] Keep diagnostics bounded by `MaxDiagnostics` or a separate load-phase cap; hostile baselines must not flood builds.
  - [ ] Assert diagnostics remain sanitized and do not leak raw baseline JSON, absolute paths, tokens, tenant/user data, or source snippets.
  - [ ] Add a fail-closed drift test that intentionally changes generated output or expected baseline data and verifies the gate fails with normalized, actionable diff evidence.
  - [ ] Add clean-run determinism coverage: two consecutive executions on the same input must produce byte-stable normalized output and no working-tree mutation.
  - [ ] Run or implement a bounded forbidden-token scan across updated drift diagnostics, reports, snapshots, benchmark evidence, and ledger evidence.

- [ ] T4. Complete high-value metadata drift coverage (AC8, AC15)
  - [ ] Add ProjectionBadge drift tests using enum member attributes such as `[ProjectionBadge(BadgeSlot.Danger)] New` and `[ProjectionBadge(BadgeSlot.Success)] Done`.
  - [ ] Add destructive command metadata drift tests against a real command compilation path, then port the fixture into the classifier tests.
  - [ ] Add projection role, currency/display-format, empty-state CTA, and icon drift tests with exact HFC1066 category assertions.
  - [ ] Preserve the one-diagnostic-per-declaration/member/category bound.
  - [ ] If any category cannot be tested without broad fixture churn, record the accepted constraint and owner in the ledger.

- [ ] T5. Triage older parser/transform/emitter SourceTools gaps (AC9-AC14, AC16)
  - [ ] Decide whether `BoundedContextAttribute.DisplayLabel` should be implemented end-to-end now; if not, record a named accepted constraint or product decision.
  - [ ] Harden source hint names for same-named types in different namespaces, exotic namespace strings, and command/projection mixed surfaces.
  - [ ] Escape XML doc comment content emitted from bounded-context or display metadata when `<`, `>`, or `&` can appear.
  - [ ] Decide whether conflicting display labels across the same bounded context need a diagnostic or explicit acceptance.
  - [ ] Preserve or improve the malformed `[Projection]` inner-loop contract without introducing generator-amplified compile errors.
  - [ ] Evaluate HFC1010 / RS2002 release-tracking suppression and add a descriptor release-row guard if Story 11.2 does not already own it.
  - [ ] For any diagnostic ID, severity, help-link, release-row, or registry impact, either keep the change inside existing SourceTools behavior or create a Story 11.2 handoff with evidence.
  - [ ] Add destructive renderer snapshot coverage only if the fixture set stays bounded; avoid reapproving unrelated snapshots.
  - [ ] Triage policy alias collisions, Unicode/normalization policy names, partial declaration ordering, and struct projection empty-state CTA flow.
  - [ ] Use compiler-backed Roslyn fixtures for generator coverage; assert diagnostics, generated trees, hint names, incremental cache behavior, missing metadata/`AdditionalFiles` behavior where applicable, and no parasite output for unrelated projects.

- [ ] T6. Build deterministic fixture and redaction gates (AC19-AC24)
  - [ ] Create or extend isolated valid/invalid SourceTools fixtures with unique temp roots, shared minimal abstractions, deterministic cleanup, and normalized snapshot output.
  - [ ] Cover Windows and non-Windows separators, CRLF/LF normalization, invariant-culture formatting, malformed syntax, missing references, no-contract projects, and multi-target/multi-project shapes where currently supported.
  - [ ] Add redaction assertions for diagnostics, logs, benchmark output, and generated/snapshot artifacts: no local absolute paths, temp directories, user names, `bin/obj` paths, tokens, tenant/user identifiers, raw JSON snippets, or source payload fragments.
  - [ ] When a fixture cannot execute on the current platform, fail closed into `blocking`, `advisory`, `accepted unstable`, or `split`; do not substitute weaker assertions without recording the reduction.
  - [ ] Keep snapshot/golden approval bounded to touched SourceTools surfaces; do not introduce broad unrelated approval churn.
  - [ ] Treat auto-fix/update command behavior, long-term snapshot format, and JSON/SARIF/text drift report format as deferred unless already implemented locally.

- [ ] T7. Update docs, ledger, and validation evidence (AC1, AC15-AC24)
  - [ ] Update `_bmad-output/implementation-artifacts/deferred-work.md` with resolution/acceptance/split markers for every Story 11.4-owned row.
  - [ ] Update SourceTools comments or focused docs only where behavior changes need a maintainer-facing explanation.
  - [ ] Record exact validation commands and outcomes in this story's Dev Agent Record.
  - [ ] Record SourceTools fixture corpus, expected drift/generator artifacts, redaction evidence, benchmark status, and any accepted constraints in the Dev Agent Record.
  - [ ] For each accepted constraint, record likelihood, impact, release risk, owner, validation evidence, and the condition that reopens the decision.
  - [ ] Move Story 11.4 to `review` only after implementation and validation evidence are complete.

---

## Dev Notes

### Current State

- `FrontComposerGenerator` uses Roslyn incremental APIs and separates normal generation from drift comparison and trim/AOT advisory output.
- `DriftOptions.Bind` currently reads `PublishTrimmed` before `PublishAot`; because `false` is a real value, `PublishTrimmed=false` prevents `PublishAot=true` from enabling the advisory path. The generator then gates HFC1070 on `optionsResult.Options.PublishTrimmed`.
- `DriftDetection.cs` owns baseline parsing, trust failures, structural/metadata comparison, diagnostic shape, redaction, path normalization, and HFC1058-HFC1070 facts.
- Drift test coverage lives under `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/**`; general generator performance tests live under `Benchmarks/` and `Performance/`.
- `Benchmarks/DriftBenchmarkTests.cs` contains the intended AC11 cache-hit/cache-miss harness but is currently skipped as red-phase.
- `DriftClassifierMetadataTests.cs` covers display, description, column priority, field group, relative time, and policy drift; badge/destructive/role/currency/empty-state/icon rows remain deferred.
- Older SourceTools gaps include unsupported `DisplayLabel`, namespace-collision-safe source hint naming, bounded-context names with invalid C# or XML-sensitive characters, malformed projection emit-gating, HFC1010/RS2002 release tracking, destructive command renderer snapshots, policy alias/Unicode edge cases, and partial/struct edge cases.

### Deferred Rows To Close Or Accept

| Deferred ID / row | Required Story 11.4 treatment |
| --- | --- |
| DEF-9-1C-2 | Patch and test PublishAot-only HFC1070 behavior. |
| DEF-9-1C-1 | Confirm or complete AC11 drift performance coverage; remove or justify red-phase skip. |
| DEF-9-1B-1 | Replace narrow diagnostic comparisons with structural diagnostic-shape assertions. |
| DEF-9-1B-2 | Replace broad colon path assertion with drive-root-specific path checks. |
| DEF-9-1B-3 | Add minimal/BOM boundary fixture only if reachable through current input paths; otherwise accept with rationale. |
| DEF-9-1B-4 | Rename helper parameters when touching the same tests; otherwise accept as cosmetic. |
| DEF-9-1B-5 | Add ProjectionBadge metadata-drift test using enum member attributes and HFC1066 `kind="ProjectionBadge"`. |
| DEF-9-1B-6 | Reproduce destructive metadata drift through a real command compilation and add classifier coverage. |
| DEF-9-1B-7 | Add projection role, currency/display-format, empty-state CTA, and icon metadata drift coverage. |
| DEF-9-1A-1 | Decide SourceText/checksum equality for drift baseline inputs; preserve incremental behavior. |
| DEF-9-1A-2 | Decide descriptor severity cloning vs editorconfig; avoid behavior churn unless tests prove benefit. |
| DEF-9-1A-3 | Decide whether diagnostic message parameterization is Story 11.4 or Story 11.2 governance. |
| DEF-9-1A-4 | Add or accept contract-level `[Description]` baseline comparison. |
| DEF-9-1A-5 | Improve duplicate identity diagnostics to name both origin paths without leaking absolute paths. |
| DEF-9-1A-6 | Remove or document truncation sort-key sentinel. |
| DEF-9-1A-7 | Cap load-phase diagnostic count separately from comparison count. |
| DEF-9-1A-8 | Replace high-allocation dictionary comparison only if perf evidence warrants it. |
| DEF-9-1A-9 | Use deterministic ordinal hash for `DriftBaselineInput.GetHashCode` or prove current behavior is harmless. |
| DEF-9-1A-10 | Keep redaction precedence documentation aligned with Story 11.2 diagnostic governance. |
| DEF-9-1A-11 | Decide whether skipped non-`DomainModel` parse results should emit bounded info or remain silent. |
| DEF-9-1A-12 | Revisit property ordering only if Roslyn behavior changes; avoid breaking established approval baselines without need. |
| DisplayLabel unsupported end-to-end | Implement or record named product/architecture decision; avoid partial parser-only support. |
| Namespace-collision-safe source naming | Fix generated hint/source key collisions for same simple type names in different namespaces. |
| Conflicting DisplayLabels across BoundedContext | Add diagnostic or accepted constraint if DisplayLabel remains unsupported. |
| BoundedContext invalid C# identifier chars | Add sanitization/diagnostic or accepted constraint; generated C# must not break silently. |
| Hint name sanitization for exotic namespace formats | Sanitize or test current namespace display behavior. |
| XML doc comment escaping | Escape metadata before emitting XML doc comments. |
| Incremental generator caching edge case | Investigate only if DisplayLabel or partial metadata changes are implemented. |
| Gate emit stage on clean parse of malformed `[Projection]` types | Preserve current inner-loop tolerance or add stricter clean-parse gating with tests. |
| HFC1010 analyzer implementation / RS2002 guard test | Coordinate with Story 11.2 for registry/release governance; add SourceTools descriptor guard if in scope. |
| Destructive renderer emitter snapshot coverage | Add bounded snapshots for destructive command render output or accept test-cost explicitly. |
| DestructiveNamePattern `RegexOptions.Compiled` | Replace with generator-safe approach only if touching `CommandParser`; otherwise accept as low priority. |
| DF3 / DF4 policy edge cases | Fix or accept policy alias collision and Unicode/normalization behavior with evidence. |
| ProjectionEmptyStateCta struct target | Verify or accept struct projection edge behavior. |

### Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Story 11.4 owns SourceTools drift/generator hardening, not broad diagnostics registry or CLI/IDE behavior. | Keeps Epic 11 stories independently implementable. |
| D2 | High-value release promises are fixed before cosmetic/perf micro-optimizations. | Applies L06/L07 and avoids unbounded SourceTools cleanup. |
| D3 | HFC1070 must treat PublishAot as a first-class advisory trigger, independent of PublishTrimmed. | Story 9.1 AC14 explicitly says trim-enabled or native-AOT host. |
| D4 | Drift diagnostics are public contract surface; tests must assert structured shape, not incidental strings only. | Prevents severity/properties/path regressions passing silently. |
| D5 | Metadata drift categories need representative compiler-backed fixtures. | Hand-built JSON-only checks can pass while parser/transform flow is broken. |
| D6 | Accepted constraints must be explicit in docs/comments/ledger and backed by a command or test. | Avoids false closure of old review findings. |
| D7 | SourceTools remains analyzer-host compatible. | `netstandard2.0`, Roslyn 4.12.0, and no Shell/Fluxor/Fluent UI dependency are release constraints. |
| D8 | Checked-in baselines/snapshots plus compiler-backed fixture expectations are the Story 11.4 source of truth. | Prevents ambiguous drift checks that compare against incidental local output or stale review notes. |
| D9 | Drift gates fail closed and CI must not silently regenerate expectations. | Release readiness requires actionable failure when generated output changes without reviewed baseline updates. |
| D10 | Determinism is a release contract, not a cosmetic test concern. | Stable ordering, invariant culture, normalized paths/line endings, and timestamp-free output prevent CI/local split-brain. |
| D11 | Generator coverage must compile real Roslyn fixtures before trusting string-only assertions. | Parser, transform, emitter, diagnostic, hint-name, and incremental cache behavior can diverge while string checks still pass. |
| D12 | Redaction applies to all evidence surfaces, including benchmark and drift reports. | SourceTools evidence is public release material and must not leak local paths, users, tenants, payloads, or tokens. |
| D13 | Broad generator refactors, API changes, auto-fix modes, and snapshot format overhauls are deferred by default. | Keeps Story 11.4 as bounded release hardening rather than an architecture rewrite. |
| D14 | Deferred-row inventory reconciliation is a release gate, not bookkeeping. | Story 11.4 exists to close old review findings; losing an alias or split row would create false confidence. |
| D15 | Accepted constraints require explicit likelihood, impact, owner, evidence, and reopen trigger. | "Low priority" is not enough justification for release readiness. |
| D16 | Evidence redaction must run across every updated evidence surface before review. | A sanitized diagnostic can still leak through benchmark output, snapshots, logs, or ledger notes. |
| D17 | Platform-dependent fixtures and benchmarks fail closed into named states. | Silent assertion reduction turns release-hardening tests into false positives. |
| D18 | Diagnostic governance policy stays with Story 11.2 unless this story only exercises existing SourceTools behavior. | Prevents drift fixes from smuggling registry, docs, release-row, or severity policy changes. |
| D19 | Snapshot and generator coverage stays bounded to touched SourceTools surfaces. | Full-corpus reapproval or broad generator rewrites would exceed the story's L06/L07 decision and test budget. |

### Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` | Update likely | HFC1070 gate and source hint naming if fixed. |
| `src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs` | Update likely | Drift options, baseline input equality/hash, diagnostics, redaction, structural comparison support. |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` | Update possible | HFC1010/RS2002 guard or message format decisions only if in scope. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` | Update possible | DisplayLabel, invalid bounded context, partial ordering, struct projection CTA. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs` | Update possible | Destructive metadata and policy alias/Unicode behavior. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/*` | Update possible | DisplayLabel and metadata propagation only if implemented end-to-end. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/*` | Update possible | Hint names, XML escaping, destructive snapshots, deterministic output. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/**` | Update likely | Drift regression, metadata, trim/AOT, diagnostics, baseline trust, incremental tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/DriftBenchmarkTests.cs` | Update likely | AC11 cache-hit/cache-miss performance evidence. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/**` | Update possible | Parser edge cases and policy/DisplayLabel behavior. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/**` | Update possible | End-to-end metadata propagation. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/**` | Update possible | Snapshot/approval coverage and deterministic output. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update | Mark Story 11.4 rows resolved/accepted/split after implementation. |
| `_bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md` | Update | Dev Agent Record, validation, file list, completion notes. |

### Project Structure Notes

- Keep SourceTools under `src/Hexalith.FrontComposer.SourceTools` and SourceTools tests under `tests/Hexalith.FrontComposer.SourceTools.Tests`.
- SourceTools targets `netstandard2.0` and is loaded by Roslyn analyzer hosts; avoid APIs unavailable to `netstandard2.0`.
- Do not add direct dependencies from SourceTools to Shell, Fluxor, Fluent UI, MCP runtime, EventStore, or DAPR.
- Package versions stay centralized in `Directory.Packages.props`.
- Generated source hint names and snapshot baselines must be deterministic, culture-invariant, timestamp-free, and machine-path-free.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`; do not initialize or update nested submodules.

### Testing Strategy

- Run focused SourceTools tests first:
  - `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release`
- Run focused drift tests while iterating:
  - `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "FullyQualifiedName~Drift"`
- Run performance/advisory drift tests intentionally when AC11 changes:
  - `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "Category=Performance|FullyQualifiedName~DriftBenchmark"`
- Run snapshot/approval verification only for touched emitter surfaces.
- Run redaction and fail-closed drift checks with hostile path/payload fixtures before moving the story to review; record whether each gate is blocking or advisory.
- Run a bounded forbidden-token scan across changed drift/generator evidence surfaces before review, including benchmark output and ledger evidence if updated.
- For skipped, advisory, or unstable performance/fixture checks, record the exact platform/environment condition and risk rationale in the Dev Agent Record.
- When a snapshot or golden file update is required, the committed diff is the review artifact; CI must verify it rather than regenerating it.
- For final release-confidence, run the main lane if time allows:
  - `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 9.1 build-time drift detection | Story 11.4 | HFC1058-HFC1070 behavior, baseline trust, structural/metadata drift, trim/AOT advisory, and AC11 performance evidence are hardened or explicitly accepted. |
| Stories 1.4 and 1.5 SourceTools parse/transform/emit | Story 11.4 | Parser, IR, transform, source naming, and generated output gaps are fixed or accepted. |
| Story 1.8 hot reload and Fluent UI contingency | Story 11.4 | Malformed projection inner-loop tolerance, HFC1010/RS2002, and generator performance follow-ups are triaged. |
| Story 11.1 ledger reconciliation | Story 11.4 | Routed deferred rows must close with evidence or explicit accepted constraints. |
| Story 11.2 diagnostic governance | Story 11.4 | Registry/docs/HFCM governance remains separate unless SourceTools descriptor tests require a narrow handoff. |
| Story 11.3 CLI/IDE hardening | Story 11.4 | CLI/IDE path, manifest, and help behavior stay out of scope; SourceTools generator evidence remains in scope. |
| Story 11.5 MCP schema hardening | Story 11.4 | MCP schema negotiation remains separate; only generated metadata drift that feeds MCP descriptors is in scope. |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Diagnostic registry schema, docs stubs, HFCM release-row strategy, and compatibility suppression governance. | Story 11.2 |
| CLI inspect/migrate path, sidecar, manifest, and help/reference semantics. | Story 11.3 |
| MCP schema negotiation, corpus aggregate, runtime gate, and agent contract follow-ups. | Story 11.5 |
| Shell UX/accessibility/sample coverage and visual specimen follow-ups. | Story 11.6 |
| Release workflow NuGet/GitHub ordering, CI race, credentials, and EventStore reliability governance. | Story 11.7 |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-11-deferred-hardening-release-readiness.md#Story-11.4`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-9-1-build-time-drift-detection-chunk-C-2026-05-07`] - PublishAot and AC11 performance deferred rows.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-9-1-build-time-drift-detection-chunk-B-2026-05-07`] - diagnostic comparison, baseline path, and metadata drift coverage rows.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-9-1-build-time-drift-detection-chunk-A-2026-05-07`] - baseline input, diagnostic, load cap, and parser-order rows.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-story-files-1-3-1-4-1-5-1-6-1-7-2026-04-14`] - DisplayLabel and namespace source-name follow-ups.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-Story-1-8-Hot-Reload-Fluent-UI-Contingency-2026-04-14`] - malformed projection emit gate and HFC1010/RS2002 follow-ups.
- [Source: `_bmad-output/implementation-artifacts/9-1-build-time-drift-detection.md`] - original drift detection story, ACs, review traces, and implemented behavior.
- [Source: `_bmad-output/implementation-artifacts/1-4-source-generator-parse-stage.md`] - parser conventions and badge parsing foundation.
- [Source: `_bmad-output/implementation-artifacts/1-5-source-generator-transform-and-emit-stages.md`] - transform/emitter conventions and generated output expectations.
- [Source: `_bmad-output/implementation-artifacts/1-8-hot-reload-and-fluent-ui-contingency.md`] - incremental rebuild, malformed source, and hot-reload constraints.
- [Source: `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`] - current generator pipeline and HFC1070 gate.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs`] - current drift options, baseline loader, comparison, diagnostics, and sanitizer.
- [Source: `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/TrimAot/TrimAotReflectionCatalogDiagnosticTests.cs`] - current trim/AOT matrix and deferred PublishAot-only case.
- [Source: `tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/DriftBenchmarkTests.cs`] - current red-phase AC11 benchmark harness.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope and decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - owner specificity requirement.
- [Source: `_bmad-output/project-context.md`] - project rules for SourceTools, diagnostics, tests, generated output, redaction, and submodules.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

- 2026-05-11: Story created via `/bmad-create-story 11-4-drift-detection-and-source-generator-coverage-hardening` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-11: Party-mode pre-dev hardening applied; added SourceTools source-of-truth, fail-closed drift, deterministic fixture, redaction, benchmark, and scope-boundary guardrails.
- 2026-05-11: Advanced elicitation applied via `/bmad-advanced-elicitation 11-4-drift-detection-and-source-generator-coverage-hardening`. Added deferred-row reconciliation, accepted-constraint risk rationale, redaction scan, platform fail-closed, diagnostic-governance boundary, and bounded snapshot guardrails.

### Change Log

- 2026-05-11: Created Story 11.4 and marked ready-for-dev.
- 2026-05-11: Applied party-mode review hardening for drift/generator determinism and release-readiness gates.
- 2026-05-11: Advanced elicitation hardening applied; added AC25-AC30, Decisions D14-D19, task refinements, validation guidance, and canonical trace.

### Party-Mode Review

- Date/time: 2026-05-11T09:37:09+02:00
- Selected story key: `11-4-drift-detection-and-source-generator-coverage-hardening`
- Command/skill invocation used: `/bmad-party-mode 11-4-drift-detection-and-source-generator-coverage-hardening; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary:
  - Story 11.4 needed a clearer SourceTools drift/generator source of truth so implementation does not compare against incidental local output.
  - Drift verification needed fail-closed behavior with actionable normalized diff evidence and no silent CI regeneration.
  - Source generator coverage needed compiler-backed Roslyn fixtures, not only string assertions or happy-path generation checks.
  - Determinism, path/line-ending/culture normalization, analyzer-host compatibility, and redaction needed to be explicit release-readiness gates.
  - Benchmark evidence needed baseline, environment, threshold/rationale, and blocking/advisory status instead of an ambiguous skipped/advisory test.
- Changes applied:
  - Added AC19-AC24 for source-of-truth corpus, fail-closed drift, cross-platform normalization/redaction, compiler-backed generator coverage, benchmark evidence, and scope deferral.
  - Added T1/T2/T3/T5 hardening bullets and new T6/T7 tasks for fixture, redaction, validation, and evidence gates.
  - Added Decisions D8-D13 to bound drift source of truth, CI regeneration behavior, deterministic output, Roslyn fixture coverage, redaction surfaces, and deferred architecture/API/snapshot-format work.
  - Updated testing strategy with redaction/fail-closed drift checks and snapshot/golden-file review guidance.
- Findings deferred:
  - Auto-fix or update-command mode for snapshots/golden files.
  - Long-term snapshot/baseline report format such as text, JSON, SARIF, or combined output.
  - Global generator architecture refactor, public API changes, and cross-story contract consolidation.
  - Exact performance regression threshold if current environment cannot support a stable blocking benchmark.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- ISO date and time: 2026-05-11T10:03:47+02:00
- Selected story key: `11-4-drift-detection-and-source-generator-coverage-hardening`
- Command/skill invocation used: `/bmad-advanced-elicitation 11-4-drift-detection-and-source-generator-coverage-hardening`
- Batch 1 method names: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation.
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records.
- Findings summary: The elicitation found that Story 11.4 already had strong drift/generator coverage goals, but could still fail release-readiness by losing deferred-row aliases during closure, accepting constraints without risk rationale, leaking sanitized-looking evidence through secondary surfaces, weakening platform-specific fixtures into passing tests, smuggling diagnostic governance into SourceTools fixes, or reapproving too broad a snapshot corpus.
- Changes applied: Added AC25-AC30; added Decisions D14-D19; tightened T1-T7 for starting inventory reconciliation, accepted-constraint likelihood/impact rationale, benchmark skip/advisory evidence, forbidden-token scans across evidence surfaces, diagnostic-governance handoffs, platform fail-closed outcomes, and bounded snapshot/generator coverage; expanded validation guidance.
- Findings deferred: No product-scope, architecture-policy, diagnostic registry policy, CLI/IDE behavior, MCP schema, shell UX, EventStore reliability, release automation, auto-fix command, full-corpus snapshot reapproval, or broad generator refactor was applied. Those remain with the named follow-up stories or product/architecture decisions already captured in the story.
- Final recommendation: ready-for-dev

### File List

- `_bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md`
