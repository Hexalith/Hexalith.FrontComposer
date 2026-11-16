---
title: 'Epic 16: Exact migration identity, durable records and readiness before qualification'
created: '2026-11-16'
status: 'approved'
mode: 'batch'
change_scope: 'moderate'
decision: 'approved by jpiquot as Product Owner, Architect and Release Owner'
g_3: 'open'
story_16_3: 'in-progress'
---

# Sprint Change Proposal — Epic 16 G-3 candidate identity

This is the complete proposal for jpiquot's approval as Product Owner, Architect and Release Owner. It recommends retaining the exact four-part migration identity, explicitly adopting same-repository off-main record storage, and requiring execution readiness before qualification starts. The suggested EventStore/Builds/catalog runtime tuple is assessed as an alternative, not adopted by this document.

Only this proposal is produced in this round. Every amendment below is proposed text, not an applied change to its target. Existing approved rules continue until an actual planning decision is recorded. Story 16.3 stays **in-progress**, Epic 16 stays **in-progress**, and **G-3 stays open**. No candidate is selected and no qualification attempt is scheduled or started.

## 1. Issue summary and evidence

### Two causes require two remedies

The [owning Story 16.3 terminal disposition](../implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md#m-terminal-disposition--2026-11-16) and [Epic 16 context](../implementation-artifacts/epic-16-context.md#m-terminal-disposition--2026-11-16) record jpiquot's “End; correct course” answer, received by the first post-answer clock observation at `2026-11-16T10:10:24Z`. The selected option's description incorrectly summarized three candidates as lost to main drift. The decision stands; its causal summary was corrected in the preserved records.

| Candidate | Evidence and record lineage | Actual blocker |
| --- | --- | --- |
| C — `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` | Accepted engineering capture; [Quality 43755163930 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/attempts/1). First-parent main advanced through `902fe13fc411c9eb7cd3786180761a904a3b47fb`, then R, which stores C's capture. | Recording the work advanced main away from the tested commit. Three actual C approve statements were received with unmet prerequisites and remain nonoperative. |
| R — `695d23149c56a6104e81d23a54116b4fb914df95` | Independent [Quality 43759663406 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43759663406/attempts/1) succeeded. M then stored R-time amendment adoption and review. | Its own record lineage superseded current main again. No independent R capture or migration receipts exist; its capture deadline was `2026-11-15T22:57:29Z`. |
| M — `b34be9245443d21b72a6076cf205bbbc8a8c521f` | [Quality 43771512600 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43771512600/attempts/1) and [CI 43771512961 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43771512961/attempts/1) succeeded. The last required Quality step completed at `2026-11-15T23:11:05Z`. | The two-hour capture deadline, `2026-11-16T01:11:05Z`, expired while M was still main. Capture never ran because M-specific admission and schedule, a complete writer roster with dated acknowledgments, and record-ref Git authorization were never supplied. Stable main and green runs did not supply those prerequisites. |

M remained main at least until `11c8377a858a756c4e4069bf08e62db1f58e1733` (commit time `2026-11-16T05:01:35Z`; its push Quality run was created at `05:01:57Z`). These metadata times do not establish an exact push instant. No M Tuple Record, migration receipt or final G-3 review exists. The [M procedure is ended, never executed, and must not be executed](../implementation-artifacts/epic-16-lineage-procedure-2026-11-15.md#terminal-disposition--2026-11-16).

**C/R need a record-storage solution. M needs readiness before evidence starts aging.** Extending a window, observing another green run, or approving a smaller identity alone addresses neither pair of causes completely.

### Further inputs that affect the decision

From C through `fb454047dbf45f2bbd1a58bd6b84bb7ef056a3e2`, the EventStore/Builds/catalog triple stayed the same while FrontComposer commits changed. That makes the runtime-tuple option worth assessing; it does not establish permission to reuse approvals. Subsequently, workspace `/pushall` syncs changed the dependencies:

| Coordinate | C/R/M | First sync, `33e71036c04f2b99955376e9c90cbb1fc3a81e64` | Second sync, `c6233bdaf445d6a87e465a1e67f9eb266b0e322f`, reaching first-parent main through `ebd3f76dc0b320d455917c593ff86526b58d0c77` |
| --- | --- | --- | --- |
| EventStore source | `fdb59895975e86a1e234325a01237f259cdbd53b` | `eecc0e4da70a0d64aaeff4fb75cdc06f94eb7563` | `97c02f7bf99a694528a2b15f0b3b0a6e5fb10e9d` |
| Builds | `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` | `801f268cd1b37ecae41d4fb20d115c055b4c5793` | `65fd22f37b9fad64b595841923cdbe050563ab99` |
| Catalog default EventStore | `5.1.0` | `5.1.1` | `5.1.1` |

At the first sync, SDK42 selection also gained `UseHexalithProjectReferences`/`HexalithEventStoreFromSource` gating. The same catalog change gated the SDK42 OpenAPI pair, advanced Folders `1.4.0` to `2.0.0`, and added an SDK42 McpCli `2.0.1` pin. Seven other gitlinks moved at the first sync; Memories and Platform moved again at the second. The latter sync happened while the owner question was pending and was unseen by a session-list observation. A roster must cover automation and its write scope, not just visible agents.

The terminal record's red CI/Quality observations concern `33e71036…` and `ebd3f76d…`. Later source repair `a03ee98a8964d56506160bcd9c73de992df56f58` and its [activation record](../implementation-artifacts/release-gate-repair-2026-11-16.md) exist. Local HEAD at this investigation is `f26304dabf7153c7075fa8c4a82dc8fc537c5ed0`; local `origin/main` tracks it. This proposal makes no fresh remote CI, live-main, durability or compatibility claim for that revision. No historical red result is relabelled, and no current green candidate is inferred.

### Decisions and limits preserved

The [2026-11-15 six-provision amendment and receipt](sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md#planning-approval-receipt--2026-11-15) remains approved history. jpiquot's five separately confirmed capacities and additional workspace automation-owner identity are established; this proposal does not reopen those answered identity questions. The prospective owner coordination attestation did not supply the missing individual roster/acknowledgments and cannot establish continuity for M.

The accepted [C Tuple Record](../implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md#tuple-record--2026-11-15-1), engineering acceptance and D1 historical exception stay unchanged. C's approvals never activate retroactively or transfer. Retain the source/package distinction, 35-source-context/27-project D4 scope, standalone SDK42 `4.2.0` versus inherited `5.1.0`, property-only and excluded CounterFixture Release limits, framework/launch/restored-assets limits, configured browser retry, artifact expiry and BH2/BH3/QC-BH1/QC-VG1 residuals. A source provider/AppHost result proves no NuGet package-mode runtime compatibility.

The original outer deadline `2026-11-16T17:27:10Z` and each historical capture deadline remain unchanged. M is ended regardless of whether that outer time is still future at a later read. No due date or escalation trigger was supplied for deciding this correct-course proposal. None is assigned here; the existing four-working-hour incident rule remains in force.

## 2. Impact analysis

| Area | Impact and proposed treatment |
| --- | --- |
| Epic 16 / 16.1 | Keep engineering-done history. Future captures append in the owning 16.1 spec on an authorized record ref, bound to a new exact tested candidate. Preserve every earlier Tuple Record. |
| Story 16.2 / OI-18 | Keep done as not invoked. Reopen only if EventStore authority becomes unavailable or is proposed to move while that candidate's EventStore receipt is missing; require the full transfer path before substitution. |
| Story 16.3 | Keep in-progress. Add an explicit storage rule, admission-before-start criteria, human release comparison and durable decision ordering. Preparation and planning approval cannot complete the migration story. |
| E11R-AI-1 | Keep open under G-3. Clarify how future exact-candidate evidence and receipts can be durable without changing current main. Do not reopen completed Story 11.25. |
| E12R-AI-2 | Keep in-progress. Preserve its prohibition on bundling gitlink bumps into story/spec commits. Separate dependency repair/seal work from record storage; this proposal closes no repair or CI task. |
| PRD §0.1 G-3, FR-29.7, D-12 | Append the chosen storage and readiness rules after approval; retain exact four-part identity and separate evidence/approval. No MVP scope reduction, new product feature or milestone approval. |
| Architecture Active-Identity invariant | Explicitly select the same-repository record-ref interpretation. Distinguish tested candidate, record commits and release source. No API, runtime, topology, data-model or technology change. |
| Epic 17 | Final Product readiness continues waiting on G-3. G-1/G-2/G-8, AD-13/AD-15/AD-19 and publication-safety work remain independent. Complete any required source/workflow/policy changes before selecting a candidate intended for release; a later G-8 implementation commit invalidates an earlier exact-candidate release path. |
| Other epics / UX | No epic is removed, renumbered or made obsolete. Operator screens, journeys, accessibility matrices, UX supplements and adopter contracts need no change. |
| Release implementation | Existing exact-current-main, exact-source CI/Quality and protected-credential recheck remain. G-3 receipt review is human-controlled; examined release code has no G-3 receipt/ref parser. No workflow input, automatic enforcement or conformance is invented. |

Repository-only preparation is complete with this proposal. Operational feasibility remains conditional on the preflight below. If writers cannot honor the hold or durable storage cannot be authorized, the owner records that concrete blocker instead of launching another attempt.

## 3. Recommended approach

### Options assessed

| Option | What approval binds to | Benefit and remaining cost | Recommendation |
| --- | --- | --- | --- |
| A. Exact candidate; records on a separate same-repository ref | Tested FrontComposer commit, EventStore source gitlink, Builds gitlink and catalog-owned EventStore version, with evaluated selection scope and exact CI evidence. | Breaks the self-record cycle without treating different FrontComposer commits as equal. Requires explicit record-ref selection, Git authority, durable retention and a real main-writer hold before starting. | **Select A**, with readiness-first admission. The M proposal's storage reasoning becomes an explicit prospective planning rule only if this proposal is approved; M itself stays ended. |
| B. EventStore/Builds/catalog runtime tuple alone | Only those three coordinates, reusable across FrontComposer commits. | Removes record-commit sensitivity, but would also admit changed FrontComposer behavior, build inputs, other gitlinks or policy under the same triple. A default version omits conditional project/TFM/configuration/mode selection. It still needs valid evidence, receipts and execution readiness. | Do not adopt the plain triple as sufficient migration approval. The suggested direction is input, not prior authorization. |
| C. Runtime tuple plus a reviewed FrontComposer material-content identity | Triple plus an explicit protected set of source, dependency, build, workflow, policy and test inputs; bounded record-only descendants permitted. | Could tolerate main record commits. Requires a newly approved projection/exclusion rule, treatment of merges and every changed file, and a release comparison proving equivalence. Exact CI/Quality on the final main SHA still remains necessary. Directory names alone cannot establish harmlessness: planning text may change gates and tooling may consume it. | Viable subject to separate design and approval, but higher classification risk and more effort than A. No blanket `_bmad-output/` or documentation exemption is proposed. |
| D. Release an older approved commit after main advances | Older exact candidate selected independently of current main. | Conflicts with current dispatch/selector/recheck controls and needs release-owner/CiCd implementation work outside this request. | Do not select. No release-selector change. |

**Path: direct adjustment within Epic 16.** Keep the product requirement and exact identity; resolve record placement and sequence the missing operational prerequisites. Rollback would discard useful engineering work and is already unapproved for EventStore identity; it does not solve M's missing prerequisites. MVP reduction removes no relevant execution failure and is unnecessary.

Planning adoption is estimated at **half to one working day** across PO/Architect/Release/Developer, excluding owner availability. Preflight coordination and storage preparation are **roughly half a working day once the required owners are available**, with potentially longer external wait. These are estimates, not assigned dates. Qualification retains its separately authorized window of at most 24 hours; CI and human decisions govern elapsed time. No attempt date or completion promise is made here. Risk is moderate and concentrated in writer coverage, storage permissions/retention, evidence timing and manual G-3 enforcement. If a prerequisite is unavailable, work stays blocked before qualification rather than consuming its evidence window.

### 3.1 Proposed approval identity

Let **T** denote a future tested FrontComposer commit. T is notation, not a selected candidate or revival of C/R/M. Each migration decision binds to exactly:

1. The FrontComposer repository and full lowercase commit SHA T.
2. The exact EventStore source gitlink selected by T.
3. The exact Builds catalog gitlink selected by T.
4. The catalog-owned EventStore package version, with root presets/overrides explicitly disclosed and actual per-project, configuration, relevant target-framework and source/package-mode selections evaluated in the capture.

The existing four-part tuple remains the identity. Selection evaluations describe its exercised scope; a default catalog value is not a claim that all contexts use it. T additionally fixes every other committed input, including other gitlinks, policy and workflow references. Their independent dependency/release checks remain necessary. Do not introduce a new identity schema, digest registry, validator, packet or evidence workflow.

An owner cites the immutable owning capture and T's primary Quality attempt, reviews disclosed limits, and separately states approve or reject. A receipt identifies the actual signer, independently established role/authority, UTC decision date, supplied authoring time or receipt observation, exact tuple, evidence, rationale and limits. No missing answer or rationale is inferred. One person may exercise multiple confirmed capacities only through separately stated role decisions.

Approval remains historical for T if main later changes. It never authorizes a descendant, even one changing only records. A current-release G-3 recognition requires T still to be current main. A new candidate needs its own evidence and decisions; matching EventStore/Builds/default-version values do not transfer them.

### 3.2 Proposed readiness gate before qualification

The Release Owner, with Architect involvement, assembles the following in the existing owner decision conversation and owning 16.3 spec. Bootstrap conversation receipts may precede Git storage; they must be dated and retrievable. All rows must be satisfied and durably recorded before the named qualification window starts immediately before its authorized seal push (or an explicitly named equivalent start if no seal push is needed).

| Prerequisite | Required concrete evidence before start | Accountable role |
| --- | --- | --- |
| Planning selection | Actual approval of this complete proposal and synchronized prospective planning text. | jpiquot separately as Product Owner, Architect and Release Owner |
| Candidate and integration readiness | Complete prerequisite source/dependency/release-blocking work under its separate authority, and prepare the exact seal/activation commit and push sequence. Select the full candidate SHA T before the window starts; no substitution during the attempt. Distinguish an expected old-policy seal refusal from qualification of the later non-seal candidate T. | FrontComposer maintainer + Architect |
| Named schedule | Actual start and end, at most 24 hours; named EventStore/FrontComposer/Release decision appointments; time and authorized people for capture validation, commit/push and final review. A preparation document or an already-running green run is insufficient. | Release Owner |
| Writer and automation coverage | All active sessions assessed for FrontComposer-main write scope, including hosts, children, humans, apps, scheduled jobs, synchronization and `/pushall`. For each actual writer: accountable owner, scope, authorized hold action, dated acknowledgment and effective UTC interval. Record why other sessions are excluded. Session lists are leads only. | Workspace automation owner + each writer owner; Release Owner checks completeness |
| Authorized storage | Exact same-repository record ref based on the already selected T, persistent isolated worktree, owning paths, permitted Git actions, push destination and retention responsibility; actual separate Git authorization. T may be a prepared local commit awaiting its planned main push. Verify any pre-existing ref before use. Inspect triggers/rules so an authorized record push does not trigger a qualification/release from that ref. If this cannot work with existing controls, stop; change no workflow to bypass it. | Repository/Git owner + workspace automation owner |
| Admission and capacity | A prospective rule naming which exact T push runs/attempts will qualify, an available runtime maintainer/Architect and approvers, and completed record-storage setup containing the actual preflight receipts. No retrospective hold, late admission or inherited C/R/M evidence. | Release Owner + Architect |
| Incident handling | Named owner decision channel, working-hours calendar/timezone and a calculable disposition due rule for the retained four-working-hour duty, capped by the attempt end. No working calendar is inferred from earlier records. | Release Owner with Architect |

**No-go means no attempt.** Missing schedule, roster, acknowledgment, storage authority or capacity is recorded as that prerequisite's blocker and owner action. Stable main, role confirmation and owner attestation alone cannot substitute. A future executor must obtain the separate operational authorizations; approval of this proposal supplies none. This is the specific correction for M: do not wait for green results to request authority needed to capture them.

### 3.3 Coordination and cadence retained

- Retain repository-wide coordination under actual owner-authorized scope. Main stays T after the planned seal/activation sequence, including through all capture and receipt commits. Hold unrelated main commits, dependency/gitlink/catalog/policy changes and automated syncs. Sibling work without FrontComposer-main write scope can continue. No contact, suspension or configuration change occurs in this proposal round.
- Retain the at-most-24-hour qualification/decision window, starting immediately before the separately authorized seal push; if no seal transition is needed, explicitly schedule the start immediately before T's qualifying main push. After the planned push sequence, verify live main is T before admitting its runs. Do not reuse the original C/R/M window. No new start or end is assigned here.
- Retain capture within two hours of the **last required successful step of the same Quality attempt**, capped by the window end. All required catalog, four Gate 2c and 27 Epic 9 steps must succeed. Overall Quality success and independent exact-T push CI success are also required before accepting capture. Waiting for overall completion does not pause or reset the two-hour clock.
- For a future attempt under this proposal, capture durability means the owning record is committed, remotely reachable on the authorized ref and its immutable link verified by that capture deadline. All three actual decisions, their remotely reachable receipts and final G-3 review must finish by the decision-window end. A timely answer with a late durable receipt remains blocked.
- Retain the Release Owner/Architect bounded disposition within four working hours of drift, missing authority, failed qualification, timeout or lineage conflict. Use the prospectively supplied working calendar and record the actual due time; never silently substitute elapsed hours or extend the attempt. The deadline can end the hold before the four-working-hour target.
- An urgent fix, unexpected main change, failed/mismatched evidence, lost storage reachability, rejection or expired deadline ends the attempt/hold with G-3 open. Record a named disposition: one separately scheduled fresh attempt, rejection with bounded corrective work if needed, or an explicit planning-amendment request. No automatic retry, recapture or candidate substitution.
- End the qualification hold at final migration or terminal-blocker disposition, urgent fix, drift or deadline, whichever occurs first. A later release needs separate authorization and coordination; ending qualification never extends a release hold. Main drift between qualification and release makes T historical. Preserve the receipts, keep/reopen current-release G-3, and obtain a disposition.

### 3.4 Record lineage and retention

Choose a **new** same-repository ref for the future candidate; no name or Git action is selected here, and the ended M ref/procedure is not reused. The record worktree changes only the authorized owning 16.1/16.3 specs and their Epic 16 context/ledger/sprint records. It carries any needed approved planning references from before T is selected. It changes no runtime, workflow, dependency, gitlink or policy code.

| Order | Durable destination and identity | Constraint |
| --- | --- | --- |
| Before start | Actual preflight/admission/coordination receipts in the owning 16.3 spec on the authorized record ref. | Bootstrap messages retain their real sources and dates. Verify storage before starting the window. |
| Capture | A new append-only Tuple Record in the owning 16.1 spec, stored in commit **Q** on that ref. | Q stores evidence about T; Q is never the runtime candidate. Resolve Q's full commit and immutable spec/heading link after commit and verify remote reachability before capture deadline. |
| Migration decisions | Three actual role receipts in the owning 16.3 spec, stored in commit(s) **S**. | Each cites Q and T's evidence. Multiple separately stated decisions may share a storage commit. Preserve every prior receipt and its validity assessment. |
| Final review | Release Owner's actual G-3 comparison, Q/S links, exact tuple, evidence and disposition in the owning 16.3 spec, stored in **F**. Update context/ledger/affected sprint tracking only on that ref. | Resolve F's link after commit, cite it in the existing owner decision conversation, then recheck live main and UTC before recognition. F need not contain its own SHA. No self-referencing record loop. |
| Later consumption/preservation | Keep Q, all S, F and preflight records durably reachable in the same repository during and after any authorized consumption. | No merge, cherry-pick or auto-sync of these records onto main before T's release consumption. If release never happens, retain the ref until a separately authorized preservation disposition verifies another durable same-repository reference to every record. |

The test/CI-plus-owning-story evidence policy remains. Raw output stays local or in existing CI artifacts. Record actual artifact expiry and availability; a link cannot preserve expired bytes. If required evidence is unavailable at review, fail closed and seek a bounded owner disposition without reconstructing a packet.

Main tracking may still show 16.3 in-progress while authorized candidate-specific records live elsewhere. Human readiness must cite their immutable commits, never infer a result from main's cached status. After actual consumption or termination, later integration of records is separate authorized Git work; it creates a new main commit and transfers no approval. This proposal itself may be integrated before selecting any future T without creating a candidate exemption.

### 3.5 How release verifies identity

This is a proposed **human G-3 release-review rule**, alongside existing automated release controls. The examined [release selector](../../eng/release_contract.py), [release caller](../../.github/workflows/release.yml), [publisher](../../.github/workflows/release-publish.yml) and [main-only release configuration](../../.releaserc.json) do not implement a G-3 receipt parser. Planning approval cannot claim that they do.

1. Read the live `refs/heads/main` commit, release dispatch SHA and checkout SHA. Require all three to equal T. Compare T's committed EventStore and Builds gitlinks, exact selected catalog contents, root selection inputs and captured per-context selection scope with Q and all three receipts. Read committed objects, not mutable submodule working directories; no recursive submodule initialization.
2. Verify Q, S and F immutable spec/heading links and remote reachability. Confirm actual authority for each role, original dates, approve decisions for the same tuple, evidence/limits reviewed, complete preflight, capture timing and decision timing. Missing, stale, rejected, inferred or mismatched decisions block. Any required transfer must have preceded the substitute decision.
3. Verify T's exact successful primary Quality run/attempt, all required live steps and overall success; verify the independent exact-T successful push CI and Quality selected by release. Retain complete run/attempt/workflow/event/branch identity and fail on missing, malformed, duplicate-success or ambiguous selection. Source-mode evidence remains source-mode evidence.
4. Record the human comparison in F and recheck live main and time after F becomes durable. A same-triple descendant fails this comparison, including a record-only commit accidentally synchronized onto main. Do not dispatch or approve protected publication without a valid human G-3 result and all independent release authorities/gates.
5. A separately authorized later release continues using the current-main-only dispatch, exact-source successful push CI/Quality, authenticated AD-13 handoff, policy/evaluator/package checks, exact checkout and the existing current-main recheck before protected credentials. Recheck human G-3 record availability and tuple applicability at the release decision. Any observed drift stops consumption; do not alter the selector or dispatch from the record ref.

Migration approval grants no publication authority. **G-8's adopted production halt remains**, as do G-1/G-2 and all separate Product/security/release requirements. The historical Release `43810326251` had `prepare-candidate` cancelled, `release` skipped and `verify-publication` cancelled before the terminal answer was observed; no actor for cancellation is inferred and no release job ran. Neither that observation nor this proposal is G-3 or G-8 approval.

## 4. Detailed change proposals for adoption after approval

These edits are to be appended as dated resolutions with the actual planning receipt. OLD excerpts remain historical text. NEW text is the proposed prospective rule; it must not appear as adopted until jpiquot actually approves. No historical source or frozen intent is rewritten.

### P1 — PRD §0.1 G-3, FR-29.7 and D-12

**OLD:** “The release candidate's story or spec records one exact tuple — FrontComposer commit SHA, EventStore source gitlink, catalog-owned package version and Builds gitlink” and “Record/receipt descendants provide provenance without a documentation exemption.”

**NEW, append:**

> G-3 retains exact tested FrontComposer/EventStore/Builds/catalog identity. For a future authorized candidate T, owning capture and migration receipts shall be stored on a separately authorized same-repository record ref while main remains T. Record commits identify provenance and shall not become the release candidate or be integrated into main before T is consumed. Complete the candidate-specific schedule, writer/automation acknowledgments, storage/Git authority and capture/decision capacity before qualification begins. Apply proposal §§3.1–3.5 for scope, timing, durability and human release comparison. A matching dependency triple never authorizes a changed FrontComposer commit. Existing publication gates and the G-8 halt remain independent.

**Rationale:** Explicitly resolves the permissible storage path instead of treating another capture as a solution to C/R. Readiness-first sequencing addresses M. FR-29.7's compatibility outcome and MVP scope are retained; D-12 records the adopted mechanism and remains open until valid evidence and decisions.

### P2 — Architecture: EventStore Active-Identity invariant

**OLD:** “The release review compares the story's recorded tuple with the candidate gitlinks and catalog contents” and “The structural record-descendant/current-main cycle remains blocked”.

**NEW, append:**

> Compare the live main/dispatch/checkout SHA to tested T, then compare T's exact gitlinks and catalog selections with the owning capture and role receipts. The approved record-ref mechanism separates T from Q/S/F storage commits; it grants no commit-equivalence or documentation exception. Human Release Owner review verifies immutable record reachability, actual authority, cadence, evidence and applicability; automated current-main/CI/Quality/handoff/credential controls remain unchanged. Missing human review leaves G-3 open. Adopt the ordering and retention in proposal §§3.2–3.5.

**Architecture impact:** No component, API or deployment-topology diagram changes. Add this sequence to the existing invariant; any future automated receipt enforcement would require separate owner-scoped design/implementation and is outside this proposal.

### P3 — Epics: Epic 16 / Story 16.1 future-capture criteria

**OLD:** “Given FrontComposer HEAD, either gitlink, or the catalog package changes … the mismatch requires a new tuple record and CI run link”.

**NEW, append:**

> Given a future candidate T with complete prospective admission and an authorized same-repository record ref, when EVT-ID-1 records compatibility, then append its exact tuple, per-context selection scope and same-attempt evidence to the owning 16.1 spec on that ref, durably within the capture deadline. Main remains T. Q is the capture-storage commit and never replaces T. Any change to main, either gitlink, catalog selection or other committed inputs creates a different candidate requiring its own evidence; preserve all old records. No new capture is authorized by planning adoption alone.

**Rationale:** Preserve 16.1's done engineering history and exact evidence boundary while making future storage executable. Do not modify the existing accepted Story 16.1 file in this proposal/adoption round; future evidence is separate authorized append-only work.

### P4 — Epics and owning Story 16.3: admission, receipts and final recognition

**OLD:** “Given all required receipts approve the same current tuple … governance can recognize that exact tuple as approved” and the later requirement for independent capture/receipts while the structural cycle remains blocked.

**NEW, append:**

> Given an actual approved planning rule but missing execution prerequisites, when readiness is assessed, then keep 16.3 in-progress and G-3 open and do not start qualification. Given all preflight rows are durably satisfied, a separately authorized schedule may start one future attempt. Given T's complete timely capture and three actual approvals, when Q/S/F are durably reachable within the window and final main/time rechecks pass, then human governance may recognize G-3 only for exact T. Every record stays off main through T consumption. Any failure, rejection, expiry or drift preserves history, keeps current-release G-3 open and routes to the named bounded disposition. Story 16.2 retains its done/not-invoked state and conditional reopening rule.

**Rationale:** Distinguish completed preparation from actual approval acceptance. Append the actual planning decision and new prospective criteria outside preserved frozen intent; change no historical approval or review verdict. C decisions stay nonoperative; R and M receive no invented capture or decision.

### P5 — Prior proposals, ended procedure and cached context

**OLD:** The approved lineage amendment leaves storage an unresolved interpretation; the M procedure's terminal section says its reasoning is “input … never an adopted rule or an authorized storage path”.

**NEW, append after an actual approval:**

> The 2026-11-16 planning receipt adopts proposal Option A prospectively for a future candidate after all preflight prerequisites. The earlier M-specific procedure remains ended, never executed and must not be executed. Its record-storage reasoning informed this decision; neither its ref nor evidence schedule is revived. Prior windows, approvals, validity assessments and decisions remain unchanged. A new candidate-specific execution handoff and separate operational authorizations are required.

**Targets:** Append cross-references to both 2026-11-15 proposals and the ended procedure, then refresh Epic 16 context from the adopted PRD/architecture/epics. Preserve original bytes and dated statements. This new prospective adoption must not relabel the M interpretation as previously approved.

### P6 — Sprint tracking, deferred ledger and action lineage

**OLD:** The deferred entry says the candidate-identity correct course “closes when that proposal is approved or rejected”; E11R-AI-1 is open and E12R-AI-2 in-progress. Epic 16/16.3 are in-progress; 16.1 and 16.2 are done.

**NEW, after an actual decision:**

> Append the actual approve/reject receipt and proposal link to the existing human-owned Epic 16 ledger entry lineage and sprint comments. Close only the correct-course decision action on approve or reject. On approval, track planning adoption and the still-missing prospective execution prerequisites under 16.3/E11R-AI-1. On rejection, retain all existing approved rules, including provision 5's separately explicit at-most-24-hour fresh-attempt schedule and its own evidence/decisions. Revision or withdrawal supplies neither approval nor rejection and leaves that action open. No outcome by itself closes G-3 or supplies publication permission.

**E11R-AI-1 proposed action clarification:** “Capture one passing provider-plus-AppHost record for the exact tested candidate under the adopted readiness and record-ref rules, then obtain the three required durable migration receipts and exact-current-main human release review without rewriting Story 11.25 history.” Keep its status open and gate G-3.

**E12R-AI-2:** Append the automation/sync coordination dependency without weakening its existing gitlink-bundling prohibition or changing its status. Update only relevant comments/pointers and ordinary `last_updated` fields on actual adoption; preserve unrelated YAML values/comments. No added epic or renumbering is needed.

### P7 — PRD addendum, UX and release implementation

**Current to proposed:** No mechanism change to PRD addendum's publication/graph contracts, canonical UX design or supplements, release workflows, release-contract code, dependencies or gitlinks. Add no new product flow, schema, manifest, validator or evidence bundle. The exact current-source release boundary and G-8 halt remain. New G-3 automation is neither needed for this human-review proposal nor authorized here.

**Rationale:** This is planning, record placement and coordination work. Existing release controls already support dispatching T while durable records live elsewhere; they do not read those records automatically. The Release Owner must perform the explicit human gate.

## 5. Implementation handoff and approval

**Scope: moderate — PO/Developer coordination with Architect and Release ownership.** No new epic or fundamental product replan. The handoff is prepared, not sent to another person or agent and not executed.

| Sequence | Recipient / accountable role | Deliverable and stop condition |
| --- | --- | --- |
| 1 | jpiquot, separately as Product Owner, Architect and Release Owner | Approve or reject this complete proposal, or request revision. Record actual date, answer, capacities and scope; leave unanswered decisions pending. Approval adopts planning only. |
| 2, only after approval | Developer with Product Owner/Architect | Append P1–P6 and actual receipt to authoritative sources/tracking; refresh context; preserve history and semantic states. Existing artifact/whitespace/link checks pass. |
| 3, future separately authorized work | jpiquot as Release Owner with Architect, workspace automation owner and repository/Git owner | Supply every preflight item, actual operational permissions, selected record ref, retention owner, candidate-selection sequence and a named future schedule. If any row is missing, record no-go and stop before qualification. |
| 4, only after preflight and execution authority | Runtime maintainer + Architect; then three migration approvers | Use existing exact-T lanes, append new capture, obtain actual role decisions, store Q/S/F and verify deadlines/reachability. No prior receipts/evidence substitute. |
| 5, only after separate release authority and all gates | Release Owner | Perform §3.5 human comparison and preserve automated exact-source controls. G-8 closure and any source changes precede selection of a releasable T; G-3 planning approval never permits publication. |

**Success criteria for this proposal/adoption:**

- Both historical causes are accurately recorded; no M drift cause, retrospective approval, deadline renewal or historical evidence transfer is claimed.
- The chosen approval identity, storage location, release comparison and retained controls are explicit; the runtime triple remains an assessed alternative unless a later actual decision selects different text.
- After planning adoption, all relevant sources agree on T versus Q/S/F, readiness before start, immutable role receipts, cadence, hold termination and retention.
- A missing prerequisite is an owned preflight blocker. Another green run or record commit never automatically schedules work.
- 16.1 stays done; 16.2 stays done as not invoked; 16.3 and Epic 16 stay in-progress; G-3 remains open until the actual current-candidate acceptance conjunction is met. Epic 17's final Product readiness keeps waiting, and G-8's halt remains independent.

### Change-analysis checklist

| Checklist items | Status and result |
| --- | --- |
| 1.1–1.3 Trigger, core problem and evidence | [x] Story 16.3 terminal decision; C/R record supersession and M missing prerequisites separated in §1. |
| 2.1–2.5 Current/future epic scope and order | [x] Epic 16 direct adjustment; E11/E12 action lineage retained; Epic 17 acceptance dependency and integration ordering in §2. No new/obsolete epic. |
| 3.1 PRD | [x] G-3/FR-29.7/D-12 impacts and proposed append in P1; MVP unchanged. |
| 3.2 Architecture | [x] Exact identity, record provenance and human/automated boundary in P2; no component/API/diagram change. |
| 3.3 UX | [N/A] No screen, interaction, accessibility or journey change. |
| 3.4 Other artifacts | [x] Release controls, ended procedure, historical proposals, context, ledger, sprint and evidence storage assessed. |
| 4.1–4.4 Path evaluation | [x] Direct adjustment selected for recommendation; identity alternatives, rollback/MVP, effort, risk and timing assessed in §3. |
| 5.1–5.5 Proposal components | [x] Issue, impacts, rationale, explicit edits, dependency order, responsible roles and success criteria complete. |
| 6.1–6.2 Completion and consistency | [x] Proposal checked against supplied evidence and existing controls; verification recorded below. |
| 6.3 Explicit approval | [!] Pending jpiquot's actual decision on this complete proposal. |
| 6.4 Approved sprint edits | [!] Conditional on approval; no sprint value changed during preparation. |
| 6.5 Handoff | [x] Concrete responsibilities and sequence prepared. [!] Planning adoption and all operational execution await actual decisions/permissions. |

### Decision state and closure

**Planning decision: pending.** No approve/reject receipt is fabricated. No new deadline for this proposal is supplied. The correct-course decision action closes on actual approval or rejection; a revision leaves it open. Approval adopts the selected prospective provisions; rejection leaves the previously approved rules unchanged. Either outcome preserves all C/R/M history, Story 16.3 in-progress and G-3 open. Keep any prior record-durability condition tied to its original record and evidence; preparing this document does not assert or renew it.

Preparing or approving this proposal authorizes no runtime/workflow/dependency/gitlink edits, Git action, workflow dispatch, qualification attempt or publication. The next authorized action after presentation is the human planning decision. Subsequent implementation and execution are distinct handoffs with the prerequisites above.

### File List

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-16.md` — the only file created in this preparation round.

### Verification

Preparation verification on 2026-11-16:

- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py --sentinel-root _bmad-output/planning-artifacts/sprint-change-proposal-2026-11-16.md` — passed, “Story artifact validation passed.” This checks the proposal's authoring hygiene and existing repository integrity; it is not qualification.
- `git diff --check` — exit 0, no tracked-file whitespace errors. The new file's CRLF, final newline and absence of trailing whitespace were checked directly. `git diff --no-index --check /dev/null _bmad-output/planning-artifacts/sprint-change-proposal-2026-11-16.md` emitted no whitespace errors; its exit 1 indicates the new-file difference.
- Disposable read-only assertions under `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 -` — passed: exactly one new file (this proposal), no tracked or staged changes, 12 historical/authoritative files byte-identical to checked-out HEAD, all 10 relative links (10 unique) and their anchors resolve, and main/HEAD/index/all nine gitlinks unchanged. Story 16.1 SHA-256 remains `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`; sprint/spec states retain 16.1 done, 16.2 done as not invoked, 16.3/Epic 16 in-progress and G-3 open.

These checks establish document scope and preservation only. No runtime tests or new verification framework were needed for a proposal-only change. No remote call, contact, Git mutation, dependency change, workflow dispatch or qualification occurred. The proposal remains pending approval; implementation handoff and operational prerequisites are explicit future work.

## Planning approval receipt — 2026-11-16

**Signer:** Jérôme Piquot (`jpiquot`/Administrator). **Actual answer:** “approve”, responding to the complete proposal and the explicit request for a decision as Product Owner, Architect and Release Owner. **UTC decision date:** 2026-11-16. **Receipt observation:** `2026-11-16T12:23:13Z`, the first clock read after the answer; no separate authoring timestamp was supplied. Authority is the previously confirmed current capacity for each role, exercised again by this answer to the three-capacity request; no role is inferred from another.

| Separately confirmed capacity | Actual planning decision | Approved scope |
| --- | --- | --- |
| jpiquot — Product Owner | Approve | This complete proposal, selecting Option A and the prospective planning/tracking amendments P1–P6; P7 retains the existing product, UX and release implementation boundaries. |
| jpiquot — Architect | Approve | The same complete text: exact four-part candidate identity, explicit same-repository off-main record storage and readiness before qualification under §§3.1–3.5. |
| jpiquot — Release Owner | Approve | The same text, including human release comparison, coordination/cadence/retention controls and ownership of the remaining prospective execution prerequisites. |

The rationale presented was to break C/R's self-record supersession without a commit-equivalence exemption and to prevent another M-style readiness failure before evidence starts aging. No additional rationale, waiver, deadline or operational permission is inferred from “approve”. The runtime triple alone is not selected; the material-content/record-descendant alternative remains unadopted.

**Effect:** Option A and §§3.1–3.5 are adopted prospectively as of this receipt observation. The original proposal, pending-decision checklist and preparation verification above remain historical text; only the frontmatter status/decision have been reconciled. This dated receipt supersedes that pending state. It does not revive C, R or M, activate C's nonoperative statements, renew a deadline or supply any migration decision.

**Correct-course decision action: closed by approval.** Planning adoption is authorized and applied through the dated appendices linked below. The accountable next work is the Release Owner/Architect preflight under §3.2, with workspace automation and repository/Git owners responsible for their actual scope. No candidate, record-ref name, schedule, working calendar, new acknowledgment or Git/capture/release authority is supplied by this answer. Missing execution prerequisites remain blocking; no attempt starts.

The handoff is moderate PO/Developer coordination, applied in this shared workspace without contacting anyone or spawning another agent. Operational handoff remains future work. Story 16.1 stays done; Story 16.2 stays done as not invoked; Story 16.3 and Epic 16 stay in-progress; G-3 stays open. Epic 17's final Product readiness still waits on G-3, and G-8's production halt remains independent.

### Adoption record and checklist reconciliation

P1–P6 are applied as dated appendices to the [PRD](prd.md#approved-g-3-candidate-identity-and-record-storage--2026-11-16), [architecture](architecture.md#approved-eventstore-candidate-and-record-lineage--2026-11-16), [epics](epics.md#approved-epic-16-candidate-identity-and-readiness--2026-11-16), both 2026-11-15 proposals, the ended M procedure, owning Story 16.3, Epic 16 context, deferred ledger and sprint tracking. The E11R-AI-1 action clarification is applied; its prior wording is preserved in the new sprint comments. E12R-AI-2 retains its action/status and gains the approved coordination dependency. P7 requires no implementation change.

Checklist 6.3 is complete with the three-capacity receipt above. Checklist 6.4 is complete for approved planning/tracking edits; no story or epic state changes. Checklist 6.5 is complete for the documented planning handoff; operational execution remains blocked on the explicitly named prerequisites. This closes the correct-course planning workflow, not the migration approval story or a release gate.

### File List — planning adoption

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-16.md`
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-tuple-freeze.md`
- `_bmad-output/implementation-artifacts/epic-16-lineage-procedure-2026-11-15.md`
- `_bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md`
- `_bmad-output/implementation-artifacts/epic-16-context.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Adoption verification and workflow execution log — 2026-11-16

- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md --base f26304dabf7153c7075fa8c4a82dc8fc537c5ed0` — passed, “Story artifact validation passed.” The first run reported this proposal missing from the owning File List because the new heading had a suffix; using the recognized exact `File List` heading fixed that authoring issue without changing historical text.
- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py` — passed, “Story artifact validation passed.”
- `git diff --check` — passed. Direct checks also verify the new proposal's whitespace, CRLF and final newline.
- Disposable read-only assertions under `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 -` — passed: exactly the 11 adoption paths above; all nine tracked Markdown files preserve their complete HEAD bytes as an unchanged prefix; the prepared proposal preserves its original bytes except the approved frontmatter status/decision and appended receipt; all 18 added relative links (6 unique target/anchor pairs) resolve. The first prefix check found the tuple-freeze addition before its final historical paragraph; moving only the new section to the end restored the full prefix. The checker was also corrected to handle existing YAML action entries without an `id`; no data change was needed for that correction.
- Parsed sprint YAML equals HEAD apart from `last_updated` and the approved E11R-AI-1 action wording. Its prior action is retained in comments, all earlier comments are preserved, every story/epic/action status is unchanged, and E12R-AI-2's existing action remains unchanged. Story 16.1 and 16.2 are byte-identical to HEAD; Story 16.1 SHA-256 remains `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`. Story 16.3's complete frozen/prior prefix, in-progress status and open G-3 are preserved.
- HEAD/main remain `f26304dabf7153c7075fa8c4a82dc8fc537c5ed0`; the index and all nine gitlinks are unchanged and nothing is staged. No runtime test was needed for documentation adoption. No remote read, contact, workflow dispatch, Git mutation or qualification was performed.

**Workflow completion:** the complete proposal, explicit edit proposals, actual planning approval and moderate-scope handoff are recorded. PO/Developer planning adoption is applied in this workspace. The Release Owner/Architect handoff is the actual preflight in §3.2 with workspace automation and repository/Git owners; that operational work remains unscheduled and unauthorized by this receipt. The correct-course decision action is closed; Story 16.3 remains in-progress, G-3 open and G-8's halt independent. This log is documentation completion, not a migration approval or claim of remotely durable adoption records.
