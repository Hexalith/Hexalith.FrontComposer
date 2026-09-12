---
project: frontcomposer
date: 2026-09-11
workflow: bmad-correct-course
mode: Batch
trigger: "Epic 11 retrospective rejection: resolve the Story 11.24 release-identity conflict, restore the four immediate gates, rerun acceptance, and route E11R-AI-5 through E11R-AI-8 as owned implementation work."
status: approved
approved: 2026-09-12
approvedBy: Administrator
planningChangesApplied: 2026-09-12
scope: Moderate
recommendedApproach: Direct Adjustment
source: _bmad-output/implementation-artifacts/epic-11-retro-2026-09-10.md
supersedes: null
handoffStatus: ready-for-build
handoff:
  - Product Owner
  - Architect
  - Developer
  - QA Engineer
  - Release Owner
---

# Sprint Change Proposal: Epic 11 Acceptance Recovery

Approval: approved by Administrator on 2026-09-12.

This proposal preserves completed Epic 11 delivery history, resolves the active-release interpretation
of Story 11.24, and adds a bounded remediation extension for E11R-AI-1 through E11R-AI-8. It does not
authorize a release, close PRD gate G-3 without its named signatures and evidence, or alter an existing
submodule pointer or package version merely by being approved.

## 1. Issue Summary

The Epic 11 retrospective dated 2026-09-10 rejected epic acceptance after finding a planning-contract
conflict and four reproducible gate failures. Story 11.24 is a completed, immutable historical record
for an owner-approved EventStore `3.91.1` tuple, including a deliberately non-authorizing failed provider
capture. Later reconciliation proved the current EventStore `3.103.0` runtime with 19/19 Pact
interactions and a passing AppHost smoke, but that evidence was captured while the root selected Builds
`35c3d1e5…`. The repository now selects Builds `a32cb422…`.

The current PRD already rejects rollback and requires the active release identity to reconcile:

- EventStore source gitlink `059f6a8917bfab26b85775be464840a1610dfdeb`;
- EventStore package version `3.103.0`; and
- current Builds catalog gitlink `a32cb422749352cce8dec948aa3e78c8f00eb4cf`.

`epics.md`, `architecture.md`, and `epic-11-context.md` still project the older Story 11.24 gate and
status. The v1 identity JSON also intentionally separates the historical approved tuple from later
compatibility evidence, while the current Governance test hard-codes the now-stale `35c3d1e5…` Builds
provenance. The result is three different concepts being treated as one:

1. the immutable Story 11.24 historical authorization record;
2. the 2026-09-08 current-runtime compatibility capture at prior Builds provenance; and
3. the release identity that the current repository must approve and prove.

### Current Evidence

- Root gitlinks are EventStore `059f6a8917bfab26b85775be464840a1610dfdeb` and Builds
  `a32cb422749352cce8dec948aa3e78c8f00eb4cf`.
- The selected Builds catalog supplies EventStore `3.103.0`.
- The 2026-09-08 provider report passed all 19 committed interactions and the AppHost report passed all
  ten resources plus authenticated command, query/provenance, projection SignalR, and clean-stop
  observations. Both reports are bound to Builds `35c3d1e5…`, not the current Builds gitlink.
- The focused identity Governance test fails because it expects Builds `35c3d1e5…` but reads
  `a32cb422…` from the current tree.
- The focused analyzer-inventory test fails with actual count `3327` and SHA-256
  `e33cb6e8c92dea244db0cebde30721da208a5a569a7a7cf9c1e41b7d77ee270f` instead of the sealed
  `3325` inventory.
- The focused SourceTools documentation test fails because its FC-NIP manifest still requires
  `Resolved 2026-08-12` and wording that Stories 9.4-9.8 block completion, while the canonical PRD
  records completed Stories 9.3-9.8 and the passing 9.8 live proof.
- The Story 11.7 Playwright route test reaches
  `/commands/Counter/ConfigureCounterCommand`, but its generic `Counter` heading locator matches both
  the shell banner and route `h1`.
- E11R-AI-5 through E11R-AI-8 are open, owned findings with enough detail for implementation stories,
  but they have no implementation-story mapping or approved order.

### Problem Classification

This is a requirement-interpretation and artifact-consistency failure discovered at epic acceptance,
plus bounded runtime and evidence-hardening defects. The product goal and MVP are unchanged. Completed
Story 11.24 history remains valid for what it actually authorized; it is not the active release tuple.

## 2. Impact Analysis

### Epic and Story Impact

Epic 11 remains the correct owning epic and stays `in-progress`. No new epic is needed and no completed
Epic 1-10 work is reopened. Add Stories 11.25-11.32 as an Epic 11 retrospective-remediation extension:

| Sequence | Story | Action item | Outcome |
| --- | --- | --- | --- |
| 1 | 11.25 Current EventStore Release Identity and Evidence | E11R-AI-1 | Approve and prove the current release tuple while preserving Story 11.24 history. |
| 2a | 11.26 Analyzer Identifier Inventory Reconciliation | E11R-AI-2 | Review the two-identifier delta and intentionally reseal or correct it. |
| 2b | 11.27 Generated Command Route Acceptance Locator | E11R-AI-3 | Restore an unambiguous route-activation assertion. |
| 2c | 11.28 FC-NIP Semantic Fixture Alignment | E11R-AI-4 | Align the fixture to current PRD truth. |
| 3 | Epic 11 acceptance checkpoint | — | Rerun the rejected acceptance after 11.25-11.28 are green and 11.29-11.32 are approved, owned dispositions. |
| 4a | 11.29 Fallback Refresh and View Registration Correctness | E11R-AI-5 | Correct the three scheduler/registration runtime defects. |
| 4b | 11.30 Testing and MCP Boundary Hardening | E11R-AI-6 | Correct ULID, serialization, and credential-redaction behavior. |
| 4c | 11.31 Canonical Correlation Pseudonymization | E11R-AI-7 | Implement DW-1769 and DW-1770 through one joinable, non-raw contract. |
| 4d | 11.32 Epic 11 Artifact Integrity Enforcement | E11R-AI-8 | Reconcile story metadata and make recurrence fail closed. |
| 5 | Epic 11 final acceptance | — | Rerun after all remediation stories; close the epic only on a green decision and consistent artifacts. |

Stories 11.26-11.28 may be implemented in parallel after the 11.25 identity decision is recorded.
Stories 11.29-11.31 are independent implementation tracks, but Story 11.32 runs last so its integrity
check covers the final remediation artifacts and sprint state.

### Artifact Impact

| Artifact | Current conflict | Approved correction |
| --- | --- | --- |
| `prd.md` | G-3/D-12 allow either recapture or a semantic exception and name the v1 JSON as if it can represent the active approval; FR-29.7 points only to Story 11.24. | Select fresh evidence recapture at Builds `a32cb422…`; preserve v1 as historical; make a successor active-identity record the G-3 target; add Story 11.25 to FR-29.7 and keep G-3 open until evidence and named signatures exist. |
| `epics.md` | The workstream table says 11.24 is blocked and its old exact tuple is still presented as the active release criterion. | Mark 11.24 as completed historical authorization, explicitly superseded for active release identity by 11.25; update current state and add Stories 11.25-11.32 with the criteria below. |
| `architecture.md` | Epic 11 status and dependency order predate delivered Stories 11.18b-11.24 and do not separate historical authorization from active compatibility. | Record the successor-identity invariant, current tuple, evidence provenance rule, remediation sequence, and acceptance checkpoints. |
| `epic-11-context.md` | Ends with Story 11.24 blocked on EventStore migration authority. | Add Stories 11.25-11.32 and the ordered dependency/handoff rules. |
| v1 EventStore identity contract | Holds the old approved tuple plus a non-authorizing current-compatibility section. | Do not rewrite it. Story 11.25 creates a successor active identity record and moves the current Governance consumer to it. |
| Current Pact/AppHost evidence | Passes at EventStore `3.103.0` / source `059f6a89…`, but at Builds `35c3d1e5…`. | Retain it as prior compatibility evidence; recapture one provider-plus-AppHost packet at the exact current tuple and bind its hashes/coordinates in the successor record. |
| `sprint-status.yaml` | E11R-AI-1 through E11R-AI-8 are open without implementation story mappings; parent/child drift remains. | Add 11.25-11.32 as backlog stories, map each action item, retain `epic-11: in-progress`, and defer final structural reconciliation to 11.32. |
| Retrospective | Rejected decision is a dated evidence record. | No edit; later acceptance artifacts cite rather than overwrite it. |
| UX artifacts | No journey, interaction, layout, accessibility contract, or visual design changes. | No change. Story 11.27 repairs test targeting only. |

### PRD, Architecture, and Release Impact

- D-12 becomes deterministic: recapture exact-tuple evidence; do not use a semantic-compatibility
  exception and do not roll back to EventStore `3.91.1`.
- The historical tuple `bb94d93e… / 3.91.1 / a8a50859…` remains immutable authorization evidence and
  is never relabelled as current.
- The `059f6a89… / 3.103.0 / 35c3d1e5…` capture remains immutable prior compatibility evidence and is
  never projected onto `a32cb422…`.
- The active release candidate tuple is
  `059f6a8917bfab26b85775be464840a1610dfdeb / 3.103.0 /
  a32cb422749352cce8dec948aa3e78c8f00eb4cf`.
- Course-correction approval chooses that tuple and recapture strategy. It does not impersonate the
  EventStore maintainer, FrontComposer maintainer, or Release Owner. G-3 closes only when those named
  owners sign the successor record, or when OI-18 first records the required ownership transfer.
- Approval and planning application do not change a gitlink, package, workflow, release setting, tag,
  or published artifact.

### Scope and Estimate

Classification: **Moderate**. The correction changes Epic 11 backlog, requirements projection,
architecture status, evidence ownership, and acceptance sequencing without changing product scope.

- 11.25 is the critical path because live provider/AppHost recapture and named approvals are external
  to a purely static code change.
- 11.26-11.28 are bounded gate restorations and may proceed together after the identity choice.
- The first acceptance rerun is a decision checkpoint, not automatic epic closure.
- 11.29-11.32 are medium implementation stories; 11.32 follows the others to validate final state.
- Calendar completion depends on owner availability and live evidence capture; no release date is
  inferred by this proposal.

## 3. Recommended Approach

Use **Option 1 - Direct Adjustment**. Preserve the shipped baseline and add the ordered remediation
extension.

### Phase 1: Resolve E11R-AI-1

Approve the active tuple
`059f6a8917bfab26b85775be464840a1610dfdeb / 3.103.0 /
a32cb422749352cce8dec948aa3e78c8f00eb4cf` and require recapture rather than semantic exception.

Implement Story 11.25 through `[BD] Build — bmad-build`. It creates the successor identity record,
updates the Governance consumer, recaptures provider verification and AppHost smoke against the exact
tuple, and gathers the named approvals. The story may not set `migrationApprovalClaimed: true` before
the required signatures exist.

### Phase 2: Restore the Immediate Failing Gates

Implement Stories 11.26-11.28 through `[BD] Build — bmad-build`, retaining the existing policy and
behavior contracts:

1. review and resolve the analyzer inventory delta without weakening Recommended or
   `TreatWarningsAsErrors`;
2. make the Story 11.7 route locator target the route-level heading unambiguously while preserving the
   canonical URL assertion; and
3. update the FC-NIP semantic fixture to current PRD language rather than changing the PRD back to
   obsolete wording.

### Phase 3: Rerun Epic 11 Acceptance

After Stories 11.25-11.28 pass, rerun Epic 11 acceptance against the repository state. The rerun must
cite the 2026-09-10 rejection, the successor identity/evidence record, each restored gate, and the
approved owned dispositions represented by Stories 11.29-11.32.

The acceptance rerun records its evidence-based verdict; approval of this proposal does not predetermine
that verdict. Even if the historical-delivery acceptance concern is cleared because the remaining
findings now have explicit owned dispositions, `epic-11` remains `in-progress` while 11.29-11.32 are
open.

### Phase 4: Implement Remaining Runtime and Evidence Hardening

Route Stories 11.29-11.32 one at a time through `[BD] Build — bmad-build`. Stories 11.29-11.31 may be
scheduled independently once the Phase 3 verdict is recorded. Run Story 11.32 after their artifacts
exist. Each Build run must review, test, and verify its own change; correct-course approval is not
implementation approval-by-proxy.

### Phase 5: Final Acceptance and Closure

Rerun Epic 11 acceptance after 11.29-11.32. Mark the action items and epic done only if the acceptance
record is green, sprint/story artifacts agree, and no required owner approval or evidence remains open.
G-3 may close independently when its exact pass condition is met; other PRD milestone gates remain
unaffected.

### Alternatives Considered

- **Rollback to EventStore `3.91.1`: rejected.** D-12 already rejects rollback, the repository and
  catalog select `3.103.0`, and reverting would discard newer proven compatibility without resolving
  current provenance.
- **Treat the `35c3d1e5…` capture as evidence for `a32cb422…`: rejected.** Exact provenance is part of
  G-3 and the Builds pointer advanced after capture.
- **Approve a semantic-compatibility exception instead of recapture: rejected for this correction.**
  Recapture is available and produces the single exact provider-plus-AppHost record requested by the
  retrospective.
- **Rewrite Story 11.24 or its historical evidence: rejected.** The story's frozen record must remain
  auditable; the successor story resolves current release identity.
- **Close Epic 11 after only the first four actions: rejected.** The first rerun is a required checkpoint;
  final closure waits for the approved extension and a final green rerun.
- **Create a new epic or reduce the MVP: rejected.** The defects are bounded Epic 11 hardening and do
  not alter the product goal.

## 4. Detailed Change Proposals

The edits in this section were approved on 2026-09-12 and applied to the planning and sprint artifacts.

### 4.1 PRD G-3 and D-12

**OLD**

```markdown
Owners must recapture/extend evidence or approve a semantic-compatibility rule, then the EventStore
maintainer signs `migrationApprovalClaimed: true`.
```

**NEW**

```markdown
The approved active release tuple is EventStore source `059f6a89…`, package `3.103.0`, and current
Builds catalog `a32cb422…`. Owners selected exact-tuple recapture; no semantic-compatibility exception
is approved. Story 11.24 and identity v1 remain immutable historical authorization. Story 11.25 creates
the successor active-identity record, captures one passing provider-plus-AppHost packet at the exact
tuple, and records `migrationApprovalClaimed: true` only with EventStore maintainer, FrontComposer
maintainer, and Release Owner signatures. OI-18 remains the prerequisite if ownership is transferred.
```

Keep G-3 `Open` until the successor record, exact-tuple evidence, and named approvals exist. Update
FR-29.7 to reference Story 11.25 as the active remediation and Story 11.24 as historical delivery.

### 4.2 Epic 11 State and Story 11.24

**OLD**

```markdown
Story 11.24 is blocked backlog pending EventStore Story 1.20 migration authority.
```

**NEW**

```markdown
Story 11.24 is a completed historical authorization record. Its frozen tuple and evidence are not the
current release target. The 2026-09-11 retrospective-remediation extension, Stories 11.25-11.32, owns
active release identity, gate recovery, runtime hardening, artifact integrity, and epic reacceptance.
```

Retain the old Story 11.24 acceptance text as dated history and add an explicit supersession note; do
not silently rewrite the historical tuple.

### 4.3 Story 11.25: Current EventStore Release Identity and Evidence

As a Release Owner and framework maintainer,
I want one approved identity record and live proof for the EventStore runtime selected by the current
repository,
So that release compatibility is auditable without rewriting historical authorization.

**Acceptance Criteria:**

**Given** Story 11.24 identity v1 and its evidence,
**When** the current identity is recorded,
**Then** v1 remains byte-for-byte historical, and a successor record identifies v1 as superseded only
for active release selection.

**Given** the current repository selects EventStore source `059f6a8917bfab26b85775be464840a1610dfdeb`,
EventStore package `3.103.0`, and Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`,
**When** the successor record is validated,
**Then** it contains exactly that active tuple, a dated decision selecting recapture rather than a
semantic exception, and no changed gitlink or package version is attributed to the approval.

**Given** the committed FrontComposer consumer pacts and current AppHost,
**When** exact-tuple evidence is captured,
**Then** all 19 provider interactions pass over real loopback, all ten AppHost resources become healthy,
the authenticated health/command/status/query/provenance/SignalR observations pass, shutdown is clean,
and the bounded redaction-clean reports bind the exact source, package, Builds, candidate, and artifact
hashes.

**Given** the successor evidence is complete,
**When** migration approval is claimed,
**Then** the EventStore maintainer, FrontComposer maintainer, and Release Owner are named and dated; if
there is no distinct EventStore maintainer, OI-18 is approved first and the transferred role is explicit.

**Given** Governance evaluates runtime identity,
**When** the current Builds gitlink changes in a future candidate,
**Then** the gate compares the candidate with its active identity/evidence record and fails closed on
unreconciled provenance rather than embedding an unexplained historical SHA.

### 4.4 Story 11.26: Analyzer Identifier Inventory Reconciliation

As the analyzer policy owner,
I want the two-identifier CA1707 scope delta reviewed and intentionally resolved,
So that the inventory seal detects accidental drift without concealing legitimate declarations.

**Acceptance Criteria:**

**Given** the sealed count/hash and the current `3327` / `e33cb6e8…` inventory,
**When** the delta is reviewed,
**Then** the two added declarations, owning changes, and in-scope rationale are named in evidence.

**Given** the declarations are intended,
**When** the inventory is resealed,
**Then** the exact generated count/hash matches the reviewed source; if either declaration is
unintended, the source is corrected instead of blessing the drift.

**Given** the correction,
**When** focused and default analyzer lanes run,
**Then** `AnalysisMode=Recommended`, `TreatWarningsAsErrors=true`, built-in-analyzer scope, and narrow
ledger exceptions remain unchanged, with no broad suppression.

### 4.5 Story 11.27: Generated Command Route Acceptance Locator

As a QA engineer,
I want route acceptance to target the route-level heading unambiguously,
So that the test proves command activation without colliding with shell chrome.

**Acceptance Criteria:**

**Given** command-palette activation of `ConfigureCounterCommand`,
**When** the route test asserts navigation,
**Then** the URL remains exactly `/commands/Counter/ConfigureCounterCommand` and the command form is
visible.

**Given** both shell chrome and page content contain `Counter`,
**When** heading visibility or focus is asserted,
**Then** the locator is scoped to the route content or exact heading level/identity and resolves to one
element without weakening the route assertion.

**Given** the AppHost-backed e2e lane,
**When** the focused Story 11.7 test runs,
**Then** it passes without `strict mode violation` and retains tenant setup and palette activation.

### 4.6 Story 11.28: FC-NIP Semantic Fixture Alignment

As the SourceTools maintainer,
I want the semantic documentation fixture to describe current FC-NIP delivery truth,
So that documentation drift is caught without requiring obsolete PRD prose.

**Acceptance Criteria:**

**Given** the canonical PRD records D-4 complete, Stories 9.3-9.8 done, and the 9.8 live proof passed,
**When** the FC-NIP manifest is updated,
**Then** its positive and negative fragments assert those current outcomes and retain the explicit
command-target identity, typed materiality, and server-allocated-key non-goal.

**Given** older text such as `Resolved 2026-08-12` and `Stories 9.4-9.8 still block`,
**When** the correction is reviewed,
**Then** the obsolete fixture requirements are removed and the canonical PRD is not changed back to
match them.

**Given** the updated fixture,
**When** the SourceTools documentation and browserless FC-NIP guards run,
**Then** they pass against the same language-neutral manifest.

### 4.7 Story 11.29: Fallback Refresh and View Registration Correctness

As a shell maintainer,
I want fallback refresh and view registration to detect material scope/data changes,
So that equal counts, reused validators, or reused view keys cannot leave stale operator state.

**Acceptance Criteria:**

**Given** a no-ETag fallback response with the same row count but changed row values,
**When** change detection runs,
**Then** its deterministic signature includes bounded canonical row content and dispatches the changed
state.

**Given** an equal ETag while a reducer page required by visible state is missing,
**When** fallback reconciliation runs,
**Then** it dispatches or rebuilds the required page instead of treating the missing reducer state as
unchanged.

**Given** an existing ViewKey is registered again with a different tenant or query contract,
**When** registration is attempted,
**Then** the runtime rejects the conflict or atomically replaces it only after prior ownership is
disposed; it never silently retains the old scope.

**Given** deterministic regression fixtures for all three cases,
**When** focused Shell and applicable default lanes run,
**Then** changed and unchanged cases are distinguished without cross-tenant leakage or duplicate work.

### 4.8 Story 11.30: Testing and MCP Boundary Hardening

As an adopter and MCP host maintainer,
I want deterministic identifiers and evidence handling to be canonical, bounded, and fail safe,
So that test evidence is reproducible and malformed input or formatting failures cannot escape the
boundary.

**Acceptance Criteria:**

**Given** deterministic Testing command dispatch,
**When** message and correlation identifiers are allocated,
**Then** they are canonical 26-character ULIDs, stable for the configured deterministic sequence, and
distinct where the contract requires distinct identities.

**Given** an MCP lifecycle identifier,
**When** it is parsed,
**Then** canonical ULIDs through `7ZZZZZZZZZZZZZZZZZZZZZZZZZ` are accepted and overflow encodings
starting with `8` through `Z`, noncanonical text, and invalid lengths are rejected before side effects.

**Given** evidence serialization throws for an arbitrary command payload,
**When** Testing records dispatch evidence,
**Then** dispatch behavior is not failed by evidence formatting and a bounded redacted sentinel is
recorded instead.

**Given** objects or dictionaries contain credential-bearing keys including `Authorization`, `ApiKey`,
`Cookie`, `PrivateKey`, or `ConnectionString` in any casing,
**When** evidence is formatted,
**Then** their property/key names and values are redacted before truncation, while benign assertion
values remain useful.

### 4.9 Story 11.31: Canonical Correlation Pseudonymization

As an observability owner,
I want one correlation pseudonymization contract across diagnostic, lifecycle, readiness, and hot-path
logs,
So that related events are joinable without emitting raw identifiers.

**Acceptance Criteria:**

**Given** DW-1769 and DW-1770,
**When** the logging helpers are consolidated,
**Then** one shared implementation emits the approved token shape `sha256:` plus 16 lowercase hex
characters for non-empty correlation identifiers, and duplicate private digest implementations are
removed.

**Given** the same normalized identifier reaches diagnostic, lifecycle, readiness, and hot-path logs,
**When** events are emitted,
**Then** every family records the same token; different fixture identifiers produce different tokens;
no raw correlation identifier is present.

**Given** null, empty, whitespace, oversized, or Unicode input,
**When** pseudonymization runs,
**Then** normalization and bounded behavior are explicit, deterministic, allocation-conscious on hot
paths, and covered by tests.

**Given** the implementation and focused evidence pass,
**When** deferred work is reconciled,
**Then** DW-1769 and DW-1770 close with links to the canonical contract and tests.

### 4.10 Story 11.32: Epic 11 Artifact Integrity Enforcement

As a QA automation maintainer,
I want story and sprint artifacts validated against repository truth,
So that a done status cannot conceal nonexistent revisions, incomplete tasks, or contradictory queue
state.

**Acceptance Criteria:**

**Given** Stories 11.17, 11.18, and 11.19 are nonimplementable parents,
**When** sprint state is reconciled,
**Then** development status is carried by 11.17a-d, 11.18a-c, and 11.19a-d, while parent summaries do
not masquerade as implementable queue entries.

**Given** `final_revision` values for Stories 11.7, 11.9, and 11.12,
**When** each value is checked,
**Then** it resolves to an existing repository commit that supports the story or is removed/corrected
with an evidence-backed explanation.

**Given** unchecked required tasks in Story 11.6 or 11.17c and the stale Story 11.15 shadow-spec state,
**When** artifacts are reconciled,
**Then** task completion and status agree with evidence without marking unperformed work complete.

**Given** a done story with a nonexistent final revision, unchecked required task, conflicting shadow
status, missing materialized child, or parent/child queue mismatch,
**When** story validation runs,
**Then** it fails closed with the exact story and conflict.

**Given** all eight remediation stories,
**When** sprint state is finalized,
**Then** every E11R action has the correct `implementation_story`, evidence link, and status; no manual
bypass is needed for validation.

### 4.11 Acceptance Checkpoints

The Phase 3 acceptance rerun must include at minimum:

1. Release build through the `.slnx` with zero warnings/errors;
2. focused and default Shell Governance checks for active EventStore identity and analyzer inventory;
3. SourceTools documentation/contract guards;
4. the AppHost-backed Story 11.7 route Playwright test;
5. provider evidence and AppHost evidence validation at the exact active tuple; and
6. story/sprint artifact validation, reporting known 11.32-owned findings as approved disposition
   rather than silently ignoring them.

The Phase 5 final rerun repeats applicable full project lanes and additionally requires the focused
Shell scheduler/registration, Testing, MCP, pseudonymization, deferred-ledger, and strict artifact
integrity checks introduced by Stories 11.29-11.32.

### 4.12 Sprint Status

After approval, add these implementable keys in the existing schema and map the action items:

```yaml
11-25-current-eventstore-release-identity-and-evidence: backlog
11-26-analyzer-identifier-inventory-reconciliation: backlog
11-27-generated-command-route-acceptance-locator: backlog
11-28-fc-nip-semantic-fixture-alignment: backlog
11-29-fallback-refresh-and-view-registration-correctness: backlog
11-30-testing-and-mcp-boundary-hardening: backlog
11-31-canonical-correlation-pseudonymization: backlog
11-32-epic-11-artifact-integrity-enforcement: backlog
```

Keep `epic-11: in-progress`, leave E11R items `open` until their story acceptance criteria and evidence
pass, and add `implementation_story: "11.n"` to each corresponding item. Preserve the exact existing
YAML structure rather than treating this illustrative fragment as authority for unsupported keys.

## 5. Handoff and Success Criteria

### Change Scope

**Moderate**: bounded Epic 11 backlog and cross-artifact correction requiring Product, Architecture,
Development, QA, and Release coordination. No fundamental product replan or UX redesign is required.

### Recipients and Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Product Owner | Approve the direct-adjustment scope, Epic 11 extension, and any OI-18 ownership transfer; keep PRD and epic state truthful. |
| Architect | Own the current-tuple decision, successor identity invariant, architecture/context amendments, and analyzer-policy review. |
| Release Owner | Co-sign the successor identity, coordinate EventStore/FrontComposer maintainer signatures, and accept the exact live evidence. |
| Developer via `[BD] Build — bmad-build` | Implement and verify Stories 11.25-11.32 in the approved phase order without changing historical evidence. |
| QA Engineer | Run the Phase 3 acceptance checkpoint and Phase 5 final acceptance; retain complete command/evidence logs and verdict rationale. |

### Completion Criteria

Epic 11 may be marked done only when:

1. the successor active identity record names the exact current tuple, required owners, decision, and
   exact-tuple provider/AppHost evidence, and G-3 state agrees with the evidence;
2. the identity, analyzer, FC-NIP documentation, and Story 11.7 route gates pass;
3. the Phase 3 acceptance checkpoint is recorded with explicit disposition of Stories 11.29-11.32;
4. scheduler/registration, Testing/MCP, and pseudonymization fixes pass their focused and default lanes;
5. DW-1769 and DW-1770 are closed with verified evidence;
6. story files, shadow specs, final revisions, child queue keys, action items, and sprint status pass the
   strict artifact validator;
7. the Phase 5 Epic 11 acceptance decision is green; and
8. PRD, epics, architecture, context, sprint status, identity records, evidence, and retrospective
   history agree without rewriting dated records.

## 6. Correct-Course Checklist Results

| Checklist area | Result | Notes |
| --- | --- | --- |
| 1.1 Triggering story identified | Done | Story 11.24 and the 2026-09-10 Epic 11 retrospective are the trigger. |
| 1.2 Core problem defined | Done | Historical authorization, prior compatibility evidence, and current release identity were conflated; four immediate gates and four hardening tracks remain. |
| 1.3 Evidence gathered | Done | Canonical PRD/epics/architecture/UX, story/context/retro, identity/evidence, sprint status, source tests, gitlinks, and current catalog were inspected. |
| 2.1 Current epic impact | Action needed | Epic 11 remains in progress and receives Stories 11.25-11.32 plus two acceptance checkpoints. |
| 2.2 Story impact | Action needed | Preserve 11.24 history; add eight implementable remediation stories. |
| 2.3 Future epic impact | Done | G-3 and readiness remain dependent on exact identity evidence; no other epic is redefined. |
| 2.4 New/removed epics | N/A | No epic is added, removed, or reordered. |
| 2.5 Sequence validity | Done | Identity first; immediate gates second; acceptance checkpoint third; runtime/evidence hardening fourth; final acceptance last. |
| 3.1 PRD conflict | Action needed | Make D-12/G-3 deterministic on recapture and point FR-29.7 to the successor story. |
| 3.2 Architecture conflict | Action needed | Separate historical and active identity and update stale Epic 11 state/dependencies. |
| 3.3 UX conflict | N/A | No UX contract change; route test targeting only. |
| 3.4 Other artifacts | Action needed | Epics, context, sprint status, successor identity/evidence, tests, deferred ledger, and artifact validator. |
| 4.1 Direct adjustment | Recommended | Preserves completed history and current runtime while restoring truthful gates. Effort medium; risk medium. |
| 4.2 Rollback | Rejected | Contradicts D-12 and does not reconcile current release provenance. Effort/risk high. |
| 4.3 PRD/MVP reduction | Rejected | Unnecessary and would weaken release/runtime evidence. Effort/risk high. |
| 4.4 Recommended path selected | Done | Direct Adjustment, Moderate scope. |
| 5.1 Issue summary | Done | Includes trigger, context, evidence, classification, and resolved tuple recommendation. |
| 5.2 Impact and artifact edits | Done | Includes story mapping, explicit old/new amendments, acceptance criteria, and UX N/A. |
| 5.3 Recommended approach | Done | Includes sequence, tradeoffs, risks, and rejected alternatives. |
| 5.4 Handoff plan | Done | Names recipients, Build routing, two acceptance checkpoints, and completion proof. |
| 6.1 Checklist completeness | Done | All applicable sections evaluated in Batch mode. |
| 6.2 Proposal accuracy | Done | Administrator continued the complete Batch proposal without revision. |
| 6.3 Explicit approval | Done | Administrator explicitly approved on 2026-09-12. |
| 6.4 Apply approved changes | Done | PRD, epics, architecture, context, and sprint status were reconciled. |
| 6.5 Handoff confirmation | Done | Stories 11.25–11.32 are mapped and ready for ordered Build/acceptance handoff. |

## 7. Approval and Application Record

Administrator continued the complete Batch-mode proposal and explicitly approved it on 2026-09-12.
The approved planning changes were applied to:

- `_bmad-output/planning-artifacts/prd.md`;
- `_bmad-output/planning-artifacts/epics.md`;
- `_bmad-output/planning-artifacts/architecture.md`;
- `_bmad-output/implementation-artifacts/epic-11-context.md`; and
- `_bmad-output/implementation-artifacts/sprint-status.yaml`.

Application results:

- D-12 selects exact-tuple recapture for `059f6a89… / 3.103.0 / a32cb422…`; no semantic exception
  or rollback is approved.
- Story 11.24 and identity v1 remain historical and unchanged.
- Stories 11.25–11.32 are approved backlog entries mapped one-to-one to E11R-AI-1 through E11R-AI-8.
- `epic-11` remains `in-progress`; G-3 remains open for exact-tuple evidence and named signatures.
- Sprint-status YAML parsed successfully.
- `python3 eng/validate-story-artifacts.py` passed.
- `git diff --check` returned exit code 0; only configured CRLF normalization warnings were emitted.

No implementation code, test, identity contract, evidence payload, deferred-work disposition,
dependency, gitlink, commit, release, or external system was changed by Correct Course. Implementation
is handed off in the approved sequence through `[BD] Build — bmad-build`.
