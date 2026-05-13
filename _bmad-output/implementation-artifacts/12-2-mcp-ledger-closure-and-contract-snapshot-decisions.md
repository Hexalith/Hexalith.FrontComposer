# Story 12.2: MCP Ledger Closure and Contract Snapshot Decisions

Status: ready-for-dev

> **Epic 12** - Release Certification and Evidence Alignment. This story converts the remaining Story 11.5 MCP ledger ambiguity into row-addressable release evidence. It applies lessons **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 11.5 is marked `done` and records strong MCP validation evidence, but the deferred-work ledger still contains current `Reconciliation: ... Owner: Story 11.5` markers. Direct inventory at story creation found **205** active Story 11.5-owned rows with ordered row-id fingerprint `sha256:d0df4a8ff4f113e059c582a69ad2f67fa947d65d62baaff13190ce7d0c780e83`.

Story 12.2 is the narrow MCP release-certification pass. It reconciles those active markers against Story 11.5 source/test evidence, contract snapshot behavior, accepted v1 constraints, and release-note needs. The outcome must be row-addressable: every row becomes closed, accepted with full release metadata, split to a named owner, or elevated to a deliberate release gate. This story does not reopen broad Epic 8, Story 11.2, Story 11.4, EventStore, trusted publishing, accessibility, or stakeholder acceptance work.

---

## Story

As an agent integrator,
I want Story 11.5 ledger closure to match MCP contract evidence,
so that schema negotiation and agent contract readiness are not overstated.

### Release-Readiness Job To Preserve

An agent integrator and release owner should be able to inspect `_bmad-output/implementation-artifacts/deferred-work.md`, Story 11.5 evidence, and the Story 12.2 Dev Agent Record and know exactly which MCP contract rows are fixed, accepted for v1, split, or still blocking release.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary ledger input | `_bmad-output/implementation-artifacts/deferred-work.md`; detailed rows containing `Reconciliation:` are the source of truth. Do not rely on Story 11.5 status or summary prose alone. |
| Starting audit target | At story creation, active `Owner: Story 11.5` markers = 205 rows. First row `DW-0058`; last row `DW-0641`; ordered row-id hash `sha256:d0df4a8ff4f113e059c582a69ad2f67fa947d65d62baaff13190ce7d0c780e83`. |
| Current closure evidence | Story 11.5 records fixed rows (`DW-0071`, `DW-0082`, `DW-0090`, `DW-0092`, `DW-0633`-`DW-0638`) plus category matrices A-E. Treat the matrix as candidate evidence, not automatic closure. |
| Contract snapshot focus | Verify immutable contract snapshot or epoch behavior from negotiation through side-effect admission; no request may validate under one descriptor/corpus state and dispatch under another. |
| Accepted constraints | Missing claimed fingerprints, runtime corpus aggregate publication, public category compatibility, and enum/display-label parity require owner, expiry/revalidation trigger, downstream impact, evidence path, release-note need, and regression guard. |
| Row closure rule | Each active row receives one final marker: `Resolved`, `Accepted constraint`, `Split to named story`, `Superseded`, `Non-action decision`, or deliberately open release gate. |
| Boundary | Do not bulk close rows with range-only prose. Category grouping is allowed only when every row ID is listed and inherits identical owner, evidence class, downstream impact, and trigger. |
| Runtime evidence order | Prove MCP snapshot/epoch, fail-closed admission, tenant safety, public category stability, and zero side effects before final ledger mutation. If the runtime contract cannot be proven, keep the row open as a release gate or split it to a named owner. |
| Release owner summary | Produce a bounded `Story 12.2 Release Owner Summary` with the 205-row audit target, starting hash, final decision counters, accepted constraints, splits, release gates, residual risk, and validation commands. |
| Evidence contract | Evidence records must carry `row_id`, classification, target story or gate, evidence reference, sanitization check, decision owner, and expiry/regression guard when accepted as a constraint. |
| Scope guardrail | No unrelated SourceTools, diagnostic registry, Shell UX, EventStore, release publishing, accessibility, or stakeholder acceptance implementation. Route those rows to named owners if they are not MCP contract work. |
| Validation | Re-run row inventory before and after edits, focused MCP tests if source/test behavior changes, YAML/status-artifact consistency, and `git diff --check`. |

Start here: T1 snapshot active Story 11.5 rows -> T2 map each row to Story 11.5 evidence or adjacent owner -> T3 classify adjacent rows without resolving non-MCP scope -> T4 prove MCP contract snapshot and fail-closed runtime behavior before ledger mutation -> T5 classify accepted constraints and release gates -> T6 update ledger markers, release-owner summary, and Story 12.2 record -> T7 validate zero stale Story 11.5 markers or explicitly named release gates.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | `_bmad-output/implementation-artifacts/deferred-work.md` contains current `Owner: Story 11.5` markers | Story 12.2 starts | The implementer records the count, ordered row IDs, section distribution, and SHA-256 fingerprint for active Story 11.5 rows before any edit. |
| AC2 | Story 11.5 is marked `done` | Ledger closure is evaluated | Story status is not accepted as proof; each current marker is reconciled against row-specific evidence, accepted constraint metadata, split ownership, or a release-gate rationale. |
| AC3 | Story 11.5 resolution markers contain category matrices A-E | The ledger is updated | Category grouping may be reused only when every active row ID is named and the inherited disposition, owner, evidence, downstream impact, and revalidation trigger are valid for that row. |
| AC4 | Rows represent diagnostic registry, docs governance, or diagnostic UX work rather than MCP runtime behavior | The rows are reconciled | They are split to Story 11.2, a named docs/diagnostic follow-up, or accepted as v1.x diagnostic defense-in-depth with full metadata; they are not left under Story 11.5 ownership. |
| AC5 | Rows represent SourceTools or schema-fingerprint generation hardening upstream of MCP | The rows are reconciled | They are split to Story 11.4 or accepted with evidence proving MCP consumes canonicalized fingerprint values without needing additional runtime changes. |
| AC6 | Rows represent runtime MCP negotiation, admission, descriptor registry, skill corpus, or agent contract behavior | The rows are reconciled | They are closed only when Story 11.5 source/tests or new Story 12.2 validation prove fail-closed behavior, stable public categories, redaction, tenant safety, and zero side effects before admission. |
| AC7 | A row is accepted as a v1 constraint | The ledger records the acceptance | The row names owner, likelihood, impact, release risk, downstream agent/adopter impact, evidence path, expiry or revalidation trigger, release-note requirement, and regression guard. |
| AC8 | Missing claimed fingerprint or runtime corpus aggregate behavior remains accepted | Story 12.2 completes | The acceptance states whether it is a v1 release constraint, v1.x backlog item, or release blocker; it names the event that reopens the decision. |
| AC9 | Public `MessageKey`, `AgentCategory`, `decisionKind`, URI category, lifecycle category, or fingerprint equality behavior changes or remains compatibility-pinned | The contract snapshot is assessed | Machine keys/categories are ordinal/invariant and tests or evidence prove localized/display prose cannot become contract input unless a compatibility change is recorded. |
| AC10 | A descriptor, runtime manifest, corpus provider, or claimed fingerprint can change during request handling | Contract snapshot behavior is assessed | A single immutable snapshot or epoch is used from negotiation through side-effect admission, or the request restarts/fails closed before side effects; evidence is recorded. |
| AC11 | A row cannot be closed with existing evidence | Story 12.2 completes | The row is split to a named backlog item or left as an explicit release gate with owner, required evidence, release impact, and close trigger. |
| AC12 | Evidence is attached to ledger or story records | The artifacts are updated | Evidence is bounded and sanitized: no raw headers, command payloads, tenant/user IDs, tokens, exception text, local absolute paths, raw descriptor dumps, or unbounded logs. |
| AC13 | Ledger summary counts mention Story 11.5 or MCP closure | Detailed rows are changed | Summary counts and detailed current markers agree; no active `Owner: Story 11.5` marker remains unless it is deliberately documented as an open release gate. |
| AC14 | Story 12.2 completes | Validation runs | Row inventory, YAML/status-artifact consistency, and `git diff --check` pass; focused MCP tests pass if MCP source/tests changed. |
| AC15 | Story 12.2 closes or defers work | The Dev Agent Record is updated | It lists changed files, validation commands and outcomes, final row counts/fingerprints, accepted constraints, split owners, release gates, and residual risk. |
| AC16 | A runtime MCP row is closed as `Resolved` | The implementer records evidence | The row references repository evidence and, where behavior is executable, a focused test proving fail-closed behavior for absent/invalid contract material, wrong tenant, divergent or stale epoch, descriptor/corpus absence, or attempted side-effect admission without a valid snapshot. |
| AC17 | Rows are grouped under one accepted constraint or split decision | The grouped disposition is recorded | The group lists every row ID, shared evidence class, shared owner, downstream impact, expiry/revalidation trigger, release-note decision, and regression guard; any row with different metadata is split out. |
| AC18 | A row is split, accepted, or left as a release gate | The owner is assigned | The row names a downstream story, follow-up artifact, or release-gate owner; generic `future`, `backlog`, `later`, or team-only owners are not valid. |
| AC19 | Story 12.2 produces release evidence | The release owner reads the story record | A `Story 12.2 Release Owner Summary` lists audited count, first/last row, starting and final fingerprints, final decision counters, accepted constraints, split owners, release gates, validation outcomes, and residual risk in bounded prose. |
| AC20 | Evidence or validation summaries are committed | Redaction is checked | The final artifacts contain no raw headers, command payloads, tokens, tenant/user identifiers, exception dumps, local absolute paths, raw descriptor dumps, or unbounded logs; the Dev Agent Record names the sanitization check performed. |
| AC21 | Runtime MCP behavior is used as closure evidence | Negative cases are evaluated | Evidence covers negotiation rejection, ambiguous/missing tenant, unknown descriptor, missing corpus, failed admission, side-effect attempt before admission, public category stability, and stale snapshot/epoch behavior, or the missing case is a named release gate. |
| AC22 | Any row remains blocking release | Story 12.2 completes | The blocker summary states whether one blocking MCP contract row blocks v1 release, names the release decision owner, required evidence, and close trigger. |
| AC23 | The active Story 11.5 row inventory differs from the creation baseline | Story 12.2 starts implementation | The implementer records the new count/hash, lists added/removed row IDs, and treats unexplainable drift, duplicate IDs, malformed IDs, or rows without `Row: DW-####` as a release-gate condition before ledger mutation. |
| AC24 | A row is closed using Story 11.5 matrices or repository evidence | The closure is recorded | The evidence map names the specific source/test/story reference, negative case covered, and side-effect boundary; broad matrix membership alone cannot close a row. |
| AC25 | A descriptor, corpus, fingerprint, or tenant context changes during an MCP request | Snapshot/epoch behavior is assessed | The story records whether the request fails closed, restarts before side effects, or preserves one immutable snapshot; stale retries and memoized failure reuse are included in evidence or named as gates. |
| AC26 | An accepted v1 constraint has an expiry or revalidation trigger | The release-owner summary is produced | The summary states who watches the trigger, what artifact must change when it fires, and whether the constraint blocks release notes, package promotion, or only post-v1 backlog. |
| AC27 | Final counters are produced | Story 12.2 completes | The ledger top summary, detailed row markers, Story 12.2 release-owner summary, Dev Agent Record, and any Story 12.1 routing references reconcile to the same Story 11.5 row disposition counts. |
| AC28 | Redaction evidence is captured | Validation runs | The scan includes MCP-specific adversarial fixtures for descriptor dumps, manifest paths, bearer-style headers, tenant/user claims, command payload fragments, correlation IDs, and raw tool/resource URIs. |

---

## Tasks / Subtasks

- [ ] T1. Capture starting Story 11.5 ledger inventory (AC1, AC14)
  - [ ] Read `_bmad-output/implementation-artifacts/deferred-work.md` from top to bottom before edits.
  - [ ] Count detailed rows containing both `Reconciliation:` and `Owner: Story 11.5`.
  - [ ] Record a deterministic inventory table with `count`, `first_id`, `last_id`, `section_distribution`, `starting_hash`, and sanitized `generated_at` timestamp.
  - [ ] Preserve the starting fingerprint in the Dev Agent Record.
  - [ ] Compare the implementation-time inventory to the creation baseline and fail closed on unexplained count/hash drift, duplicate row IDs, malformed row IDs, or missing `Row: DW-####` markers.

- [ ] T2. Compare active rows against Story 11.5 evidence (AC2, AC3, AC6)
  - [ ] Review `_bmad-output/implementation-artifacts/11-5-mcp-schema-negotiation-and-agent-contract-hardening.md`.
  - [ ] Review the Story 11.5 fixed-row list and category matrices A-E in `deferred-work.md`.
  - [ ] Verify that each claimed fixed row has evidence in MCP source/tests or Story 11.5 validation records.
  - [ ] Mark broad or range-only closure claims as insufficient until row IDs and inherited metadata are explicit.

- [ ] T3. Classify adjacent non-MCP rows (AC4, AC5, AC11)
  - [ ] Split diagnostic registry/docs governance rows to Story 11.2 or a named diagnostic/docs follow-up.
  - [ ] Split SourceTools/schema-generation rows to Story 11.4 or a named schema-fingerprint follow-up.
  - [ ] For diagnostic UX/path-truncation polish, decide whether it is a v1 release concern, v1.x accepted constraint, or Story 11.2 docs/diagnostic owner.
  - [ ] Record downstream MCP impact as `none`, `contract-adjacent`, or `release-gate` for each grouped set.
  - [ ] Classify non-MCP rows without reanalyzing or resolving their implementation scope inside Story 12.2; assign them to a named story, named follow-up, or explicit release gate per L10.

- [ ] T4. Verify MCP contract snapshot and fail-closed behavior before ledger mutation (AC6, AC9, AC10, AC16, AC21)
  - [ ] Audit Story 11.5 tests for negotiation, tool admission, command invocation, projection reader, auth header parsing, skill resources, aggregate manifest integrity, and `Story11_5ResolutionTests`.
  - [ ] Confirm compatible-additive/current-server validation happens before side effects.
  - [ ] Confirm hidden/unknown/tenant/schema/fingerprint precedence remains fail-closed and public responses stay bounded.
  - [ ] Confirm descriptor/fingerprint memoized failures produce stable sanitized categories on retry.
  - [ ] Confirm negotiation, tenant validation, descriptor/corpus selection, and side-effect admission share one immutable snapshot or epoch; reject, restart, or fail closed when the snapshot is absent, stale, expired, or divergent.
  - [ ] Cover negative cases for invalid negotiation, ambiguous or missing tenant, unknown descriptor, missing corpus, failed admission, attempted side effects before admission, public category instability, and stale epoch behavior.
  - [ ] Include mid-request descriptor/corpus/fingerprint change, stale retry, memoized failure reuse, and wrong-tenant replay in the closure evidence matrix or leave the affected rows as release gates.
  - [ ] If evidence is missing, add focused MCP tests or split the row to a named release gate.

- [ ] T5. Resolve accepted constraints and release gates (AC7, AC8, AC11, AC12, AC17, AC18, AC22)
  - [ ] Decide the v1 status of missing claimed fingerprint behavior.
  - [ ] Decide the v1 status of runtime corpus aggregate publication and build-time corpus signing/baseline materialization.
  - [ ] Decide whether public category compatibility is fully pinned, accepted as v1 constraint, or requires release notes.
  - [ ] Decide whether enum/display-label parity can remain outside MCP contract material.
  - [ ] For every accepted constraint, complete a matrix with `row_id`, `owner`, `target story or gate`, `likelihood`, `impact`, `release risk`, `downstream agent/adopter impact`, `evidence`, `expiry/revalidation trigger`, `release-note requirement`, and `regression guard`.
  - [ ] State whether one blocking MCP contract row blocks v1 release, and name the release decision owner for any exception.
  - [ ] For every expiry/revalidation trigger, name the watcher, artifact to update, release-note impact, and package-promotion impact so accepted constraints cannot age silently.

- [ ] T6. Update ledger and story evidence (AC3, AC7, AC11, AC13, AC15, AC19, AC20)
  - [ ] Replace active Story 11.5 current markers with row-addressable final states or explicit release gates.
  - [ ] Update the Story 11.5 resolution marker summary if it currently contradicts detailed row state.
  - [ ] Add the `Story 12.2 Release Owner Summary` with final counts, validation, decisions, changed files, accepted constraints, splits, release gates, and residual risk.
  - [ ] Update this Story 12.2 Dev Agent Record with final counts, validation commands and outcomes, decisions, changed files, sanitization check, and evidence locations.
  - [ ] Reconcile final counters across the ledger top summary, detailed row markers, release-owner summary, Dev Agent Record, and any Story 12.1 routing references.
  - [ ] Keep historical review text and original row IDs intact.

- [ ] T7. Validate completion (AC12, AC14, AC15)
  - [ ] Re-run active Story 11.5 row inventory and record final count/fingerprint.
  - [ ] Re-run sprint status YAML parse and status-artifact consistency checks.
  - [ ] Run `git diff --check`.
  - [ ] Scan changed evidence for forbidden raw headers, payloads, tokens, tenant/user identifiers, exception dumps, local absolute paths, descriptor dumps, and unbounded logs.
  - [ ] Include adversarial MCP redaction fixtures for descriptor dumps, manifest paths, bearer-style headers, tenant/user claims, command payload fragments, correlation IDs, and raw tool/resource URIs.
  - [ ] Run focused MCP tests if any MCP source or test files changed.

---

## Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Detailed ledger rows are authoritative over Story 11.5 `done` status. | The Epic 11 retrospective found Story 11.5 completion prose and current row markers can diverge. |
| D2 | Story 12.2 is a release-certification reconciliation pass, not broad MCP feature expansion. | Keeps the story bounded and prevents reopening Epic 8 or Epic 11 feature scope implicitly. |
| D3 | Every Story 11.5-owned row must exit with a final state or named release gate. | Leaving rows under completed Story 11.5 ownership overstates readiness. |
| D4 | Category grouping is allowed only with explicit row lists and identical inherited metadata. | This preserves auditability without repeating hundreds of identical lines. |
| D5 | MCP runtime contract rows require executable or repository evidence, not only rationale text. | Agent contracts are compatibility surfaces; tests or source evidence must prove behavior. |
| D6 | Accepted constraints are release decisions. | They need owner, risk, downstream impact, expiry/revalidation trigger, release-note need, and regression guard. |
| D7 | Missing claimed fingerprints and runtime corpus aggregate publication remain high-attention decisions. | They affect whether an agent can trust schema/corpus contract material across deployment versions. |
| D8 | Machine contract keys and categories are invariant compatibility values. | Localization or display labels must not enter fingerprint, category, or decision material accidentally. |
| D9 | Evidence must be row-scoped and sanitized. | Raw headers, tenant/user data, payloads, tokens, paths, and unbounded logs are not acceptable release evidence. |
| D10 | No recursive nested submodule commands are needed. | This story is repository artifact and MCP evidence work; root-level submodule policy remains unchanged. |
| D11 | Runtime MCP closure requires behavior proof before ledger mutation. | The ledger cannot be administratively closed while fail-closed runtime behavior remains unproven. |
| D12 | Snapshot/epoch continuity is a release boundary. | A request must not negotiate under one MCP descriptor/corpus state and admit side effects under another. |
| D13 | Accepted-constraint owners must be named release artifacts. | L10 forbids generic future ownership for release certification decisions. |
| D14 | Non-MCP rows are routed, not solved, by Story 12.2. | This prevents diagnostic, SourceTools, EventStore, publishing, accessibility, or stakeholder scope from reopening inside MCP ledger closure. |
| D15 | The release owner summary is a required deliverable. | The release decision must be readable without reconstructing 205 row-level dispositions manually. |
| D16 | Sanitization is a validation gate, not a prose reminder. | Evidence that leaks headers, payloads, tokens, tenant/user IDs, paths, descriptor dumps, or unbounded logs cannot support release readiness. |
| D17 | Inventory identity failures fail closed before ledger mutation. | Duplicate, malformed, missing, or unexpectedly drifted row IDs can make a release owner certify the wrong evidence set. |
| D18 | Matrix membership is not closure evidence by itself. | Each row still needs a source/test/story reference and a covered negative case so Story 11.5 completion prose cannot over-close the ledger. |
| D19 | Snapshot/epoch proof must include change and retry paths. | Happy-path negotiation does not prove safety when descriptors, corpus, fingerprints, or tenant context change mid-request. |
| D20 | Accepted constraints need trigger ownership. | Expiry and revalidation metadata is ineffective unless a named owner watches the trigger and knows the release/package consequence. |
| D21 | Final counters must reconcile across artifacts. | Divergent top summaries, detailed markers, release summaries, and Dev Agent Records create false release-readiness evidence. |
| D22 | MCP redaction fixtures must be contract-specific. | Generic secret scans may miss descriptor dumps, manifest paths, correlation IDs, raw tool/resource URIs, and tenant claims. |

---

## Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update | Primary row-state reconciliation and Story 11.5 marker closure. |
| `_bmad-output/implementation-artifacts/12-2-mcp-ledger-closure-and-contract-snapshot-decisions.md` | Update | Dev Agent Record, validation evidence, file list, and completion notes. |
| `_bmad-output/implementation-artifacts/11-5-mcp-schema-negotiation-and-agent-contract-hardening.md` | Update possible | Only if Story 11.5 evidence summary needs a clarifying reference to Story 12.2 closure. |
| `tests/Hexalith.FrontComposer.Mcp.Tests/**` | Update possible | Only for missing focused evidence on MCP runtime contract behavior. |
| `src/Hexalith.FrontComposer.Mcp/**` | Avoid unless required | Only change production MCP code if the ledger proves a real release blocker that cannot be closed by evidence. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Avoid during implementation | This story should not change status except through normal dev workflow or if a deliberate blocker is found. |
| `_bmad-output/process-notes/story-creation-lessons.md` | Update only if new reusable lesson emerges | Append-only; do not rewrite existing lessons. |

No unrelated SourceTools, diagnostic registry, Shell UX, EventStore, release workflow, accessibility, generated docs site, or submodule content should be changed by default.

---

## Project Structure Notes

- MCP runtime code lives under `src/Hexalith.FrontComposer.Mcp`.
- MCP tests live under `tests/Hexalith.FrontComposer.Mcp.Tests`.
- SourceTools schema/fingerprint work belongs under `src/Hexalith.FrontComposer.SourceTools` and `tests/Hexalith.FrontComposer.SourceTools.Tests`; do not move it into MCP unless the row is truly contract-crossing.
- Deferred-work ledger entries must retain `Row: DW-####` identifiers for audit continuity.
- Use repository-relative evidence paths and bounded command summaries.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`; do not initialize or update nested submodules.

---

## Testing Strategy

- Use a deterministic row-inventory script that:
  - parses only detailed lines containing `Reconciliation:`;
  - counts `Owner: Story 11.5`;
  - emits ordered row IDs and SHA-256 hash;
  - groups rows by the nearest `## Deferred from:` heading;
  - fails if any active row has no `Row: DW-####` marker.
- Re-run YAML/status-artifact consistency checks after artifact edits.
- Run `git diff --check`.
- Validate the final release-owner summary includes the audited count, first/last row, starting and final fingerprints, decision counters, accepted constraints, split owners, release gates, validation outcomes, and residual risk.
- Scan changed evidence and summaries for forbidden raw headers, command payloads, tokens, tenant/user identifiers, exception dumps, local absolute paths, raw descriptor dumps, and unbounded logs.
- Fail the story validation if inventory identity checks find duplicate row IDs, malformed row IDs, missing row markers, or unexplained drift from the baseline.
- Validate that closure evidence maps each row or identical row group to a specific reference and at least one covered negative case, not only to category matrix prose.
- Reconcile final Story 11.5 disposition counters across the ledger summary, detailed markers, Story 12.2 release-owner summary, Dev Agent Record, and Story 12.1 routing references.
- Run or document an adversarial MCP redaction fixture set covering descriptor dumps, manifest paths, bearer-style headers, tenant/user claims, command payload fragments, correlation IDs, and raw tool/resource URIs.
- If no MCP source or test files changed, explicitly record why repository evidence was sufficient and which runtime negative cases remain release gates, if any.
- If source or MCP tests change, run:

```powershell
dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release
```

- Run broader main-lane tests only if production code, shared contracts, source-generation schema material, or release workflow artifacts change.

---

## Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 11.1 ledger reconciliation | Story 12.2 | Row IDs and `Reconciliation:` markers define current deferred-work state. |
| Story 11.5 MCP hardening | Story 12.2 | Source/test evidence and accepted constraints are candidate proof for closing active Story 11.5 markers. |
| Story 12.1 ledger parity | Story 12.2 | Any Story 11.5 routing created by Story 12.1 must remain row-addressable and cannot be broad range-only closure. |
| Story 12.2 | Release owner | MCP schema negotiation and agent contract readiness is certified as fixed, accepted, split, or blocked. |
| Story 12.2 | Stories 12.3-12.5 | EventStore provider gates, trusted release evidence, and accessibility/stakeholder acceptance stay outside MCP ledger closure. |

---

## Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Provider-backed pending-command status release gate. | Story 12.3 |
| Trusted release-context signing, SBOM, attestation, package inventory, and dry-run evidence. | Story 12.4 |
| Manual accessibility, localization/RTL/AT, stakeholder acceptance evidence. | Story 12.5 |
| Diagnostic registry/docs governance rows that do not affect MCP contract material. | Story 11.2 or named diagnostic/docs follow-up |
| SourceTools drift/generator rows upstream of MCP runtime behavior. | Story 11.4 or named schema-fingerprint follow-up |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md#Story-12.2`] - story statement, acceptance criteria, and Epic 12 scope.
- [Source: `_bmad-output/implementation-artifacts/12-1-ledger-marker-parity-and-epic-status-decision.md`] - prior Epic 12 story pattern and Story 11.5 row-fingerprint context.
- [Source: `_bmad-output/implementation-artifacts/11-5-mcp-schema-negotiation-and-agent-contract-hardening.md`] - MCP contract hardening evidence, accepted constraints, review traces, and validation history.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md`] - active Story 11.5 ledger markers and row-scoped closure matrix.
- [Source: `_bmad-output/implementation-artifacts/epic-11-retro-2026-05-13.md`] - retrospective finding that Story 11.5 ledger markers remain active after story completion.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13.md`] - approved Epic 12 release-certification correction.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR49-FR61`] - MCP, agent, schema negotiation, and multi-surface functional requirements.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#MCP-security-boundary`] - MCP security boundary, tenant scope, and schema stability requirements.
- [Source: `_bmad-output/planning-artifacts/architecture.md#MCP-Interaction-Model-per-Winston`] - typed MCP tools, hallucination rejection, tenant-scoped tool list, and skill corpus architecture.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope and decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08--Party-review-vs-elicitation--different-roles`] - later party review and elicitation sequencing.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - named owner requirement.
- [Source: `_bmad-output/project-context.md`] - project rules for MCP contracts, tenant safety, tests, redaction, generated output, release evidence, and submodules.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-13: Story created via `/bmad-create-story 12-2-mcp-ledger-closure-and-contract-snapshot-decisions` during recurring pre-dev hardening job.
- 2026-05-13: Pre-creation audit parsed `sprint-status.yaml`, confirmed status-artifact consistency had no drift, found ready buffer count 1, and selected the first backlog story `12-2-mcp-ledger-closure-and-contract-snapshot-decisions`.
- 2026-05-13: Starting detailed ledger-marker audit found 205 active `Owner: Story 11.5` rows with ordered row-id fingerprint `sha256:d0df4a8ff4f113e059c582a69ad2f67fa947d65d62baaff13190ce7d0c780e83`.
- 2026-05-13T20:50:14+02:00: `/bmad-party-mode 12-2-mcp-ledger-closure-and-contract-snapshot-decisions; review;` completed with Winston, Amelia, John, and Murat; all four initially recommended `needs-story-update` before dev.

### Completion Notes List

- 2026-05-13: Created the Story 12.2 developer guide and marked it ready for development. Ready for party-mode review on a later recurring run.
- 2026-05-13T20:50:14+02:00: Party-mode review applied low-risk story hardening for runtime MCP evidence, release-owner summary, accepted-constraint grouping, named split ownership, snapshot/epoch proof, redaction validation, and deterministic completion checks.
- 2026-05-14T01:10:20+02:00: Advanced elicitation applied low-risk hardening for inventory identity drift, matrix-overclosure prevention, snapshot/epoch retry paths, accepted-constraint aging, cross-artifact counter reconciliation, and MCP-specific redaction fixtures.

### Change Log

- 2026-05-13: Created Story 12.2 and marked ready-for-dev.
- 2026-05-13T20:50:14+02:00: Party-mode review findings applied; added AC16-AC22, D11-D16, evidence-contract guardrails, runtime negative-case validation, release-owner summary requirements, and canonical party-mode trace.
- 2026-05-14T01:10:20+02:00: Applied advanced elicitation hardening; added AC23-AC28, D17-D22, task/testing guardrails for row identity, matrix closure proof, snapshot/epoch retries, constraint aging, counter reconciliation, and adversarial MCP redaction.

## Party-Mode Review

- **ISO date and time:** 2026-05-13T20:50:14+02:00
- **Selected story key:** `12-2-mcp-ledger-closure-and-contract-snapshot-decisions`
- **Command/skill invocation used:** `/bmad-party-mode 12-2-mcp-ledger-closure-and-contract-snapshot-decisions; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- **Findings summary:** The story was close to dev-ready but needed stronger proof that ledger closure reflects runtime MCP behavior rather than administrative cleanup; the agents called out deterministic 205-row inventory, source/test-backed fail-closed evidence, immutable snapshot/epoch continuity, release-owner summary format, named split ownership, accepted-constraint grouping discipline, and evidence redaction.
- **Changes applied:** Added AC16-AC22, D11-D16, release-owner summary requirements, evidence-contract fields, runtime negative-case checks, accepted-constraint matrix requirements, non-MCP routing guardrails, redaction scan validation, and task-order clarification requiring runtime proof before ledger mutation.
- **Findings deferred:** Exact downstream owners for non-MCP ambiguous rows, final v1 release-note decisions for accepted constraints, artifact-retention policy for long-lived certification evidence, and whether every MCP capability requires an automated test versus repository evidence remain implementation-time decisions to record row-by-row.
- **Final recommendation:** `ready-for-dev`

## Advanced Elicitation

- **Date/time:** 2026-05-14T01:10:20+02:00
- **Selected story:** `12-2-mcp-ledger-closure-and-contract-snapshot-decisions`
- **Command/skill invocation used:** `/bmad-advanced-elicitation 12-2-mcp-ledger-closure-and-contract-snapshot-decisions`
- **Batch 1 methods:** Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation.
- **Batch 2 methods:** Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records.
- **Findings summary:** The highest remaining risk was a false MCP release-certification pass caused by inventory drift, matrix prose that over-closes rows, happy-path-only snapshot evidence, accepted constraints with stale triggers, unreconciled final counters, or MCP-specific evidence leaks that generic redaction scans miss.
- **Changes applied:** Added AC23-AC28; tightened T1, T4, T5, T6, and T7; added D17-D22; expanded testing strategy with row identity fail-closed checks, row-to-evidence negative-case mapping, cross-artifact counter reconciliation, and adversarial MCP redaction fixtures.
- **Findings deferred:** The final owner/watch policy for each accepted constraint trigger; whether closure evidence is stored as a generated machine-readable matrix or only as Dev Agent Record tables; the exact validation lane that should own long-lived MCP redaction fixtures after Story 12.2 completes.
- **Final recommendation:** `ready-for-dev`

### File List

- `_bmad-output/implementation-artifacts/12-2-mcp-ledger-closure-and-contract-snapshot-decisions.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
