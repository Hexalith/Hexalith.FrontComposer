---
title: 'Story 17.2: Publish and Exercise the Release Incident Runbook'
type: 'chore'
created: '2026-11-17'
status: 'in-review'
route: 'dispatch'
story_id: '17.2'
story_key: '17-2-i-gov-i-publish-and-exercise-the-release-incident-runbook'
baseline_commit: '66250f89230e1b261b51ce873c83d96176a9d325'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-17-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GOV-I has neither an incident runbook nor a participant-executed tabletop. Operators lack an approved procedure for containing credential exposure, unauthorized capability, mismatched packages, or partial publication while preserving evidence.

**Approach:** Publish the canonical operator runbook through the existing documentation site, exercise it with the required human participants, and retain one support-safe record in this spec. Completion requires explicit runbook approval and no unresolved containment-critical finding.

## Boundaries & Constraints

**Always:** Distinguish observed facts, simulated events, owner decisions, and future capabilities. Preserve historical ledger/register records. Use existing release artifacts and ledger identities. Record actual participant capacities, UTC timestamps, decisions, gaps, owners, deadlines, and approval of the exact runbook digest. Keep publication authorization and G-8 acceptance separate.

**Never:** Execute candidate code during containment; publish, unlist, rotate credentials, change remote settings, dispatch workflows, send messages, or mutate packages during this exercise. Do not fabricate participation, approvals, incidents, ledger attempts, or immutable evidence. Add no evidence system, schema, workflow, or release gate. Do not close GOV-I while required receipts or containment-critical fixes are missing.


**Owner decisions, 2026-11-17:**
- The user accepted prospective acknowledgement within 15 minutes and containment within 60 minutes of detection, measured in elapsed wall-clock time.
- The user confirmed “me with all roles”: jpiquot covers Product/Release approval, Security, GitHub administration, NuGet policy/credential custody, and independent verification. Record the combined capacities explicitly; exercise participation, verification results and runbook approval still require actual receipts.

</frozen-after-approval>

## Code Map

- `docs/release-incident-response.md`: missing; canonical destination required by PRD OI-15 and the GOV-1 spine, AD-15 and Deferred incident-runbook section.
- `.github/workflows/release-publish.yml:35`: reads the flag during preparation; later steps use cached output. Literal `false` alone cannot revoke an in-flight job or issued credential. Read only.
- `docs/docfx.json`, `docs/toc.yml`, `docs/how-to/index.md`: existing site inclusion and navigation; top-level release documents are currently excluded.
- `eng/validate-docs.ps1:60`: existing content allowlist also excludes this page; extend it rather than introducing a validator.
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`: existing attempt/release history; reference without inventing a simulation row.
- `_bmad-output/implementation-artifacts/sprint-status.yaml`: story row and GOV-I action; preserve unrelated gates.
- `.github/workflows/release-incident-preservation.yml`: absent. AD-15 recovery is required before retry, but unavailable today; do not run ambient recovery helpers or claim the capability exists.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs`: prove the runbook is selected by executable metadata validation and included in generic page metadata checks.
- [x] `docs/release-incident-response.md`: write operator metadata, triggers, roles, approved targets and escalation. Cover literal-false stop with authenticated readback/server timestamp, refusal of approvals/dispatches, in-flight cancellation/restriction, credential revocation/rotation, and independent confirmation without candidate execution.
- [x] `docs/release-incident-response.md`: preserve candidate/run/attempt identities, manifests, handoffs, assets and ledger observations; inventory GitHub/NuGet effects across all package IDs; distinguish absent evidence from verified absence. Explain immutable AD-15 preservation, service-unavailable escalation, unlisting preference, correction under a new version, communication, recovery and re-enable prerequisites. Missing safe preservation blocks retry. Require cause/remediation, independent verification, Release Owner approval and all existing G-8 conditions before re-enable.
- [x] `docs/docfx.json`, `docs/toc.yml`, `docs/how-to/index.md`, `eng/validate-docs.ps1`: include, link and validate the runbook using the existing site pipeline.
- [x] This spec: facilitate one dated synthetic partial-publication/possible-exposure tabletop, including cached-true, unavailable preservation and recovery branches. Record actual responses separately from scenario time; capture approvals, findings and bounded follow-ups. Exercise no live mutations.
- [x] `sprint-status.yaml` and this spec: record evidence and truthful status; keep missing receipts and containment-critical gaps open. Historical NC-35/36 dispositions and per-attempt backfill remain unchanged.

**Acceptance Criteria:**
- Given any specified incident trigger, when operators follow the published runbook, then acknowledgement, containment, evidence, credentials, effects, recovery, communication and re-enable actions have owners, measurable targets and escalation.
- Given a cached-true publisher or issued credential, when containment is rehearsed, then participants remove authority separately from setting the flag and preserve original evidence without candidate execution.
- Given missing immutable preservation or unresolved containment-critical findings, when recovery is considered, then retry remains forbidden and GOV-I remains open where its acceptance requirements are unmet.
- Given the required human participants and approved runbook, when the tabletop completes, then one support-safe record contains actual timestamps, decisions, gaps and bounded follow-ups, with no live publication or package mutation.

## Implementation Notes

### Implemented scope

- Added the canonical operator runbook at `docs/release-incident-response.md`, including the approved prospective 15/60-minute elapsed wall-clock targets and the owner's explicit combined capacities. Its approval-neutral `status: controlled` metadata remains stable when a separate owner receipt arrives.
- Added this exact page to the existing DocFX content allowlist, How-to navigation and index, and `eng/validate-docs.ps1` content selection. No new validator, schema, workflow, release gate or evidence system was added.
- The runbook distinguishes the legacy cached preparation output from future flag reads and requires separate run cancellation/restriction and credential invalidation/expiry evidence. It records the absent AD-15 preservation workflow as unavailable capability, with a hard retry prohibition rather than an ambient helper fallback.
- Existing `rel-ai-1-release-evidence-ledger.md` identities, historical observations/dispositions (including NC-35/36), the nonconformance register and per-attempt backfill are untouched. This exercise creates no release/verification attempt, machine observation or immutable evidence claim.

### Runbook identity and approval state

Prepared for review on 2026-11-17. Exact raw-file SHA-256 of `docs/release-incident-response.md` (UTF-8, CRLF):

`303fc2b614be85dae00664f0aa92b68ebd5cf6419d5e41b739f5dbef5090c04e`

Product/Release approval of these exact bytes: **pending actual receipt**. Role assignment and response-target decisions above are real owner decisions; they are not exercise responses, verification results or digest-bound approval. A later runbook-byte change invalidates any earlier digest approval and requires a new receipt. Approval of the runbook never grants publication authorization or closes G-8.

### Single tabletop record — 2026-11-17

**Record state:** prepared, awaiting actual participant responses. At actual UTC `2026-11-17T00:17:51Z`, Codex prepared this synthetic exercise record and prompts. That timestamp is preparation time, not participant acknowledgement, containment, exercise completion or approval. All injected events below are fictional and no live mutation is part of the exercise.

**Participants designated by the owner:** `jpiquot` covers Product/Release approval, Security, GitHub administration/operations, NuGet policy ownership/credential custody, and independent verification. This is one named person in combined capacities, not evidence of a second reviewer or of participation already performed. Actual receipts for those capacities remain pending.

| Scenario elapsed time | Synthetic inject; no claim about any real historical release | Response requested from the actual participant |
| --- | --- | --- |
| T+00 minutes | A fictional planned version appears partially published: Contracts is visible on NuGet, Shell publication/symbol state is unknown, GitHub Release is incomplete, and publication-key exposure is plausible. | Acknowledge as Release/Security; name T0, elapsed 15/60-minute targets, scope, immediate stop actions and next update. State what evidence would prove containment. |
| T+05 minutes | The stop flag reads literal `false`, but a still-running publisher cached `publish-enabled=true` and may hold an issued temporary key. | As GitHub admin and NuGet custodian, explain refusal of approvals/dispatches, cancellation/restrictions, policy stop, issued-key invalidation/expiry confirmation, rotation/custody and separate read-only verification. A flag receipt alone cannot close containment. |
| T+20 minutes | `release-incident-preservation.yml` is absent; GitHub Releases also cannot accept immutable incident evidence. Actions replay material may expire. | Preserve safe original working data/identities; classify unavailable evidence truthfully; escalate to Release Owner/Story 17.8 and provider support; refuse ambient recovery helpers and retry. Inventory all eight IDs, symbols, Releases/tags/assets and prior attempts rather than only the visible package. |
| T+60 minutes | A proposed correction is green and someone offers to rerun the old version while preservation remains unavailable. Later the service may recover and an authorized preservation mechanism may become available. | Reject current retry/re-enable; state the immutable run-bound preservation, cause/remediation, credential, complete-effect inventory, independent verification, communication, Release Owner approval and separate G-8 prerequisites. State how a later recovery preserves prior partial attempts and uses a new corrected version. |

Scenario timing is not measured response performance. For each actual answer retain received UTC, speaker/capacities, decision and evidence/limits here. Do not invent an incident URL, actual run coordinates, evidence Release, service failure, exposed credential or ledger row for this scenario; use existing ledger identities only as labelled historical references.

| Actual receipt required | Received UTC / response / result |
| --- | --- |
| `jpiquot`, Product/Release + Security: acknowledgement, elapsed targets, containment decision and escalation | Pending actual response. |
| `jpiquot`, GitHub admin + NuGet custodian: cached-true stop/revoke decisions and complete external-effect inventory | Pending actual response. |
| `jpiquot`, Release/Security: missing-preservation and recovery branches, communications/audience/channel/update timing | Pending actual response. |
| `jpiquot`, independent verification capacity: separate evidence checks, result and limits without candidate execution | Pending actual response. |
| `jpiquot`, all required capacities: findings, criticality, accepted owners/deadlines and disposition | Pending actual response. |
| `jpiquot`, Product/Release: accept/reject exact raw-file digest above | Pending actual response. |

### Open gaps and proposed bounded follow-ups

| Item | Observed state and consequence | Accountable capacity and proposed bound; not a fabricated acceptance |
| --- | --- | --- |
| 17.2-R1 | Actual tabletop responses, independent verification and digest-bound runbook approval are absent. Story 17.2 and GOV-I remain in-progress/open. | `jpiquot`, in the capacities above: respond and decide findings by `2026-11-18T00:17:51Z`, or record a new named deadline; deadline acceptance is pending. |
| 17.2-R2 | AD-15 candidate-free preservation workflow is unavailable at baseline. This is a known recovery blocker: no retry is safe without a later authorized, verified immutable result. The runbook provides the hold/escalate branch; no live incident or preservation result is claimed. | Release Owner + Story 17.8 implementation owner: triage by `2026-11-18T00:17:51Z`; implement and verify before any real recovery/retry. Owner scheduling/acceptance is pending. |
| 17.2-R3 | Any containment-critical finding raised by actual participants must be recorded here with its owner and a bounded deadline, fixed and verified before GOV-I closes. None has yet been dispositioned by participants. | Release/Security: disposition with the actual exercise response; do not infer that no findings exist from the absence of responses. |

No publication, unlisting, credential rotation, remote-setting change, workflow dispatch, package mutation or outbound message was performed for the exercise. No actual wall-clock response target has yet been demonstrated.


### Actual owner tabletop and runbook-approval receipt — 2026-11-17

This dated receipt supersedes the prepared record's pending-receipt and proposed-deadline state above; those earlier notes remain as preparation history. The user's actual response was **“confirm all three”**, referring to the immediately preceding numbered containment, recovery/follow-up and digest-approval requests. The first UTC clock observation after receiving that response was **2026-11-17T07:15:31Z**; this is an observation bound, not a supplied message-authoring time or fictional incident time.

The previously designated participant is `jpiquot` in all agreed capacities. The received decisions are:

| Capacity / actual receipt | Decision confirmed at the observation bound above |
| --- | --- |
| Product/Release + Security | Accept the synthetic incident response: stop/revoke and independently verify the cached-true publisher's authority. The already approved prospective targets remain 15-minute acknowledgement and 60-minute containment from detection. |
| GitHub administration + NuGet custody | The confirmed containment sequence refuses new approvals/dispatches, verifies literal false, cancels/restricts publishers with termination confirmation, stops new credential exchanges, and requires revocation or proven expiry plus separate read-only checks. A false flag alone is insufficient. These are tabletop decisions, not executed operations. |
| Release/Security recovery and effects | Keep retry/re-enable blocked pending immutable preservation, preserve original evidence, inventory all eight package IDs and GitHub effects, and retain the approved runbook's escalation, communication, separately authorized unlisting and new-version correction procedure. No outbound notice or package mutation is authorized by the confirmation. |
| Independent verification capacity | Confirm separate review of the reported verification results and limits: DocFX rendered the page/navigation; all eight snippets and artifact checks passed; the full docs gate failed on two unchanged Fluent catalog-version rows. No live containment, credential revocation, immutable preservation or response-time performance was proved. |
| All designated capacities / findings and follow-up | Accept personal ownership of triaging the missing AD-15 preservation capability by **2026-11-18T00:00:00Z**, replacing the earlier unaccepted proposed bound. It remains a recovery blocker routed to Story 17.8; real retry requires the approved mechanism and independent verification. No additional containment-critical finding was reported in this confirmation; that is not a claim that production safeguards or real containment are proven. |
| Product/Release runbook approval | Approve the exact UTF-8/CRLF raw-file SHA-256 **`303fc2b614be85dae00664f0aa92b68ebd5cf6419d5e41b739f5dbef5090c04e`**. The primary agent rehashed the still-identical file before recording this receipt. |

The simulated T+00/T+05/T+20/T+60 branches and their required decisions have now been exercised with the actual designated participant. Preparation UTC, actual receipt observation and scenario offsets remain distinct. No live SLA result, actual exposure, external side effect, immutable evidence Release or ledger attempt is inferred. The tabletop/digest-receipt gap 17.2-R1 is resolved; 17.2-R2 has the owner-accepted triage deadline above and remains a release-recovery prerequisite; 17.2-R3 continues to require reopening GOV-I for any later containment-critical finding.

Story implementation acceptance may now proceed to review. This receipt neither closes G-8/GOV-ACCEPT-1/2 nor authorizes publication, recovery dispatch, credential changes or other remote operations. The existing unrelated full-docs blocker remains reported separately.


### Review patches and replacement runbook identity — 2026-11-17

Review BH-2 through BH-6 are corrected: the guide now gives private-channel/support routing and concrete trusted administration references, classifies known incident facts/unknowns immediately with append-only observations, follows complete affected credential scopes beyond the eight expected IDs, and links the existing source authorities while distinguishing mutable navigation from exact evidence. No new workflow, validator, evidence system or live operation was added.

VG-1 is addressed through the existing executable metadata fixture: `ReleaseIncidentRunbookMetadataIsValidatedByPublicationCommandAsync` removes `reviewed` and requires the real PowerShell validator's runbook-specific failure. Generic content-metadata coverage now includes the runbook too. A disposable Debug project build completed with **0 warnings / 0 errors**, and both focused tests passed (**2 total; 0 failed/skipped/not run**). Exact commands and logs are retained in `artifacts/verification/story-17-2/verify-review-fixes.sh`, `review-fixes-build.log` and `review-fixes-tests.log`. The primary agent added the required XML summary to the new public test method; the final full documentation job includes that source revision.

Current runbook UTF-8/CRLF raw-file SHA-256 is **`24814b2f4a13b76b2c88e3ae72dfcd22b52abb09b0ba5b496683bd7fe0c69d5c`**. The owner-approved `303fc2b614be85dae00664f0aa92b68ebd5cf6419d5e41b739f5dbef5090c04e` remains the historical pre-patch revision. Its receipt does not approve the replacement bytes. **Replacement-digest approval is pending.** The actual tabletop decisions, assigned capacities, response targets and accepted preservation-gap triage deadline remain recorded; they need no fabricated repeat exercise.

While review work was in progress, HEAD advanced externally to `b23b4cb91d974b4266a9369a85e6dae32163a893`, containing the earlier Story 17.2 source/receipt/triage state. The primary agent observed that full identifier directly from Git, preserved the commit and kept the original `baseline_commit` unchanged. No commit or push was performed by this workflow.


### Final revised-runbook approval and completion receipt — 2026-11-17

The user answered **“approve”** to the request naming the reviewed replacement runbook SHA-256 **`24814b2f4a13b76b2c88e3ae72dfcd22b52abb09b0ba5b496683bd7fe0c69d5c`**. The first UTC clock observation after receipt was **2026-11-17T07:48:14Z**; this bounds observation, not message authorship or scenario time. The primary agent rehashed the unchanged UTF-8/CRLF runbook before recording this approval. This receipt supersedes all earlier pending-replacement-approval statements while preserving both the old approval and review history.

The current runbook is approved in the previously designated Product/Release capacities; the actual combined-role tabletop decisions and separate verification review were received at the earlier recorded observation. All implementation tasks and story acceptance criteria are now met. Review BH-2 through BH-6 and VG-1 are fixed and verified; BH-1 was rejected; Edge Case Hunter found no issue. BH-7 is retained as the workflow-required agent-context follow-up in deferred-work.md. No unresolved containment-critical finding remains in this story's runbook/tabletop acceptance; no live containment or production readiness is claimed.

Implementation workflow status is complete; sprint Story 17.2 and the GOV-I action move to `review`. The existing two Fluent contingency version errors remain the explicitly reported full-docs blocker. The owner-accepted Story 17.8 preservation-gap triage deadline remains **2026-11-18T00:00:00Z**; the missing capability continues to forbid real recovery/retry until independently verified. G-8, GOV-ACCEPT-1/2 and publication authorization remain separate.

## Spec Change Log

- 2026-11-17: applied review patches BH-2 through BH-6 and executable validation coverage VG-1. The runbook digest changed; old approval remains historical and replacement approval is required. Frozen intent and actual tabletop receipts are preserved.
- 2026-11-17: recorded the actual owner confirmation of all three tabletop/approval requests at first post-receipt UTC observation `2026-11-17T07:15:31Z`; marked the tabletop task complete without changing frozen intent or approved runbook bytes.
- 2026-11-17: implemented runbook/site integration and prepared the single support-safe tabletop record. Frozen intent/owner decisions were preserved. Actual participant verification/approval remains pending; no governance acceptance or publication authorization was inferred.

- 2026-11-17: recorded the actual replacement-digest approval at first post-receipt UTC observation `2026-11-17T07:48:14Z`; all story tasks are complete and the implementation workflow is finalized.

- 2026-11-28: recorded the actual owner receipt (“confirm 1A, 2A, and 3A”) at first post-receipt UTC observation `2026-11-28T07:29:30Z`, approving the revised runbook bytes from `9d1e4f4939b5991bd754b781fd0130f40c0736e0` (unchanged at `HEAD` `0e9912939cfe69c228d486725ebc23c830d657b5`), confirming independent-verification review of the `HEAD` verification results and limits, and closing the overdue F4 triage action by routing AD-15 preservation to Story 17.8 with product retry still forbidden.

## Review Triage Log


### Review pass 1 — 2026-11-17

All three context-free review layers completed before triage: Blind Hunter, Edge Case Hunter (no findings), and Verification Gap. The complete saved diff includes the spec; reviewer content deliberately excludes it so the claims file goes only to the edge-case layer, as required by the workflow.

| ID | Finding | Verdict | Evidence and route |
| --- | --- | --- | --- |
| BH-1 | Review input omits the story receipts. | false | The review step explicitly sends the spec/claims only to Edge Case Hunter; it received and examined that file and returned no findings. The separate full diff contains it. Reject; omitting claims from blind/verification inputs is required. |
| BH-2 | Private-channel and escalation routing cannot be located when needed. | medium | The guide names an established private channel and service support but provides neither lookup instructions nor support URLs. A responder without that assumed channel can lose the response window. Patch existing routing text with record/access instructions and official private support routes, retaining the explicit single-person/no-invented-backup limitation. |
| BH-3 | Time-critical administration steps lack concrete interfaces/references. | medium | The stop/reject/restrict/policy actions name outcomes; only force-cancellation has an operational API reference. Patch the existing steps with official trusted administration/API references; perform no live action. |
| BH-4 | Incomplete inventory blocks incident classification. | medium | The inventory paragraph explicitly says to keep incident classification blocked, despite earlier immediate incident recognition and append-only observations. Patch by removing classification delay and recording available findings/unknowns immediately; keep the recovery hold. |
| BH-5 | Eight package IDs can be treated as the complete exposure inventory. | medium | The guide lists the FrontComposer IDs but does not explicitly follow wider credential scopes to other package IDs/repositories. A broader exposed credential could affect owners beyond that set. Patch to make eight the minimum and route expanded-scope investigation to the affected owners without assuming absence on access failure. |
| BH-6 | Published readers cannot navigate approval/ledger/architecture authorities. | low | Those sources are code-formatted paths or identifiers outside DocFX content. The source files can be linked directly in the existing repository; this is a direct navigation correction, not a new evidence system. Patch with source links and retain exact-digest authority rather than treating mutable navigation links as approval evidence. |
| BH-7 | Regenerated epic context drops exact release-entry predicates. | medium | The previous cache explicitly included main/live-candidate equality, CI/Quality success and authenticated run-bound handoff; the refreshed summary omits those details. Defer because the fix changes agent-context material, as the review routing rule requires. The canonical authorities remain unchanged; subsequent implementation must consult them. |
| VG-1 | New root runbook inclusion lacks an executable negative regression fixture. | medium | Pre-verified regression gap: removing its selection entry leaves DocFX publication intact while the inspected negative fixtures mutate only other pages. This loses the changed support-safe content-validation safeguard without a test failure. Patch the existing executable metadata fixture with the runbook case; this tests real pipeline behavior, not runbook prose. |

The surviving patches share no root cause merely because they touch the same guide. Each is a direct correction to demonstrated documentation or executable-validation behavior, adds no public API or new state guard, and preserves the approved intent. BH-2 through BH-6 and VG-1 have now been patched and verified with the focused metadata tests; final documentation verification and replacement-byte approval follow. No intent-gap or bad-spec loopback is required. Runbook patches will supersede the earlier exact-byte approval and require a new digest receipt after verification.

### Review Findings (code review, 2026-11-22)

**Changes requested.** Four independent reviews covered the two story commits through `eae2cccfce573837bf238d195c0bf75267a577f5` and the November 22 verification note. The approval on November 17 covers the current runbook bytes. Any revision to that file needs a new digest-bound owner approval. GOV-I stays open while containment-critical findings are outstanding.

- [x] [Review][Decision] **D1 · Medium · Record the verifier’s T+05 decision.** The participant confirmed the intended stop and credential checks. The separate verifier row reports documentation results and limits on live verification. It leaves the verdict for the *simulated* still-running job and possibly valid key unstated. Obtain the designated verifier’s actual conclusion and the evidence required to change it, or leave this receipt open. Date a new response at receipt. Preserve the November 17 record. [This spec, “Single tabletop record” and “Actual owner tabletop and runbook-approval receipt”] (Blind Hunter 8, Acceptance Auditor 2)
- [x] [Review][Patch] **P1 · High · Hold immutable evidence publication when original bytes may contain a secret.** AD-15 requires byte-identical assets even for quarantined slots. The runbook also requires restricted custody of sensitive originals. Add an explicit pre-upload decision to retain affected originals privately, escalate and block preservation completion and retry. A redacted copy cannot satisfy the original-byte slot. [docs/release-incident-response.md:220] (Blind Hunter 6)
- [x] [Review][Patch] **P2 · High · Revoke or rotate reusable credentials on plausible exposure.** The trigger includes suspected exposure, but the execution step limits action on static keys to “exposed” keys. An inconclusive access log could leave a possibly exposed key active. Align the step with the epic and AD-15. Retain the documented revocation or proven-expiry rule for already-issued temporary keys. [docs/release-incident-response.md:136] (Blind Hunter 4)
- [x] [Review][Patch] **P3 · High · Repeat the run inventory after dispatch restrictions take effect.** The first enumeration precedes the restriction. The later confirmation of job termination does not explicitly discover attempts queued in between. Re-enumerate and reconcile those attempts before declaring containment. [docs/release-incident-response.md:109] (Blind Hunter 3)
- [x] [Review][Patch] **P4 · Medium · Scope the containment verdict to compromised actors, credentials and publication paths.** The requirement that “no remaining actor” can mutate the affected scope also includes the trusted administrators needed to complete recovery. Identify retained, authorized administrative access separately. [docs/release-incident-response.md:151] (Blind Hunter 1)
- [x] [Review][Patch] **P5 · Medium · Cover a genuinely absent stop variable.** The referenced GitHub update and read operations cannot create a missing variable. Document creation after authenticated absence, followed by the existing independent literal-false readback. An access failure does not prove absence. [docs/release-incident-response.md:101] (Blind Hunter 2, Edge Case Hunter 1)
- [x] [Review][Patch] **P6 · Medium · Keep an unresolved external effect separate from permission to retry.** The inventory section permits an effect to be “truthfully accounted for” in immutable evidence. The recovery checklist requires every effect to be reconciled. Explain that recording an unknown preserves the observation while the unresolved effect still blocks product retry. [docs/release-incident-response.md:300] (Blind Hunter 7)
- [x] [Review][Patch] **P7 · Medium · Distinguish an outage that prevents verification of containment from an outage limited to preservation.** The escalation paragraph calls any service outage containment-critical. An unavailable evidence-release service after independent revocation and job termination blocks preservation and retry. Classify the outage by which proof it prevents. [docs/release-incident-response.md:81] (Acceptance Auditor 3)
- [x] [Review][Patch] **P8 · Low · Explain how to reproduce the approved runbook digest.** For the reviewed commit, the unfiltered Git blob hashes to `017ea7c196c04884cdf798b8db7387e3c300f7c5febc13479d87967c129e223f`. The checkout with the repository’s CRLF rule hashes to the approved `24814b2f4a13b76b2c88e3ae72dfcd22b52abb09b0ba5b496683bd7fe0c69d5c`. Show the verification procedure using the revision identified in the external receipt. [docs/release-incident-response.md:20] (Blind Hunter 10)
- [x] [Review][Patch] **P9 · Medium · Verify publication and both rendered navigation links in the recurring docs gate.** The existing negative test proves metadata selection. In a disposable checkout of `6d90c615`, removing the DocFX inclusion and both navigation entries left the content check and DocFX build at exit 0. The How-to index rendered and the runbook HTML was absent. Extend the existing gate to catch a missing page or either missing link in fresh output. [eng/validate-docs.ps1:1244] (Blind Hunter 12, Verification Gap 1)
- [x] [Review][Patch] **P10 · Low · Enumerate the documentation tree once.** Adding the runbook to the unused pattern list adds another full recursive traversal. Only the path predicate selects the files. Preserve that predicate and remove the redundant traversals. [eng/validate-docs.ps1:74] (Blind Hunter 11, Verification Gap 2)
- [x] [Review][Defer] **F1 · Medium · Restore the exact release-entry predicates in the Epic 17 context.** The pre-story context specified live-main equality, successful candidate CI and Quality, and the authenticated run-bound handoff. This remains the existing BH-7 agent-context follow-up. [_bmad-output/implementation-artifacts/epic-17-context.md:38] (Edge Case Hunter 3)
- [x] [Review][Defer] **F2 · Medium · Correct classifier precedence in the Epic 17 context.** The shorthand always maps a missing handoff to `missing-artifact`. AD-15 gives incomplete or unequal externally observed publication precedence as `partial-publish-incident`. Correct the context before a downstream implementation uses it. [_bmad-output/implementation-artifacts/epic-17-context.md:43] (Edge Case Hunter 2)
- [x] [Review][Defer] **F3 · Medium · Update the publisher identity in the Epic 17 context.** The November 19 amendment assigns the jobs to the FrontComposer workflow pair and requires an owner-accepted Builds actions revision. The generated context still assigns both jobs to a Builds reusable workflow. The operator runbook already names the FrontComposer workflows. [_bmad-output/implementation-artifacts/epic-17-context.md:38] (Acceptance Auditor 1)
- [x] [Review][Defer] **F4 · Medium · Account for the expired Story 17.8 triage deadline.** The owner accepted November 18 at 00:00 UTC. The review found no dated outcome for that deadline in the tracked release records. This is an outstanding owner follow-up under Story 17.8. Real retry remains blocked. [This spec, “Actual owner tabletop and runbook-approval receipt”] (Blind Hunter 9, Acceptance Auditor 4)

**Rejected**

- Blind Hunter 5 · False. The runbook assigns Release and Security the communication decision, requires the next update time in each consumer update, and requires hourly inventory updates until the inventory is complete (lines 248–249 and 315–320). The 60-minute ceiling applies during unproven containment. The story prescribes no fixed post-containment consumer cadence.

### Review patch disposition — 2026-11-22

**T+05 verifier follow-up (D1).** In response to the explicit recommendation for the synthetic T+05 inject, the user replied **“apply recommended”**. The execution environment clock read `2026-11-22T16:11:31Z` after receipt. The adopted verdict is **containment unproven**. The flag was false, a publisher was running and its temporary NuGet key might still have been usable. A separate read-only verifier must establish that new dispatches and credential exchanges are blocked, all affected jobs have terminated, and each issued key has been revoked or has verifiably expired before recording containment. The response applies to the simulated inject. The November 17 tabletop and approvals remain historical.

**Planning follow-ups.** F1 through F3 are resolved in the Epic 17 context. The entry now requires live-main equality, successful candidate CI/Quality push runs and an authenticated run-bound handoff. It identifies the FrontComposer workflow pair and the accepted Builds actions revision. It gives observed incomplete or unequal publication precedence over a missing handoff.

**Story 17.8 (F4).** The accepted triage deadline was November 18 at 00:00 UTC. Asked for a new date or a dated triage receipt, the user replied **“I dont know”**. The follow-up remains overdue with `jpiquot` and the Story 17.8 owner. A new date and the triage outcome are unconfirmed. Verified immutable preservation remains a prerequisite for product retry.

**Replacement runbook approval.** The November 17 receipt covers the earlier bytes. The revised working file based on FrontComposer `6d90c6154d2ca6155eeae17a3f280dc22ff9560b` has UTF-8/CRLF SHA-256 `24d1e9b5666b89fe16a9382bce0630b83f25bea2f5c9bae476c284785611476e` and Git-normalized blob ID `15b453a464a28a7cd245b5076bfebece3445c094`. The base commit identifies the starting tree. Product/Release approval of these revised bytes is pending. Story 17.2 is in progress and GOV-I is open.

### Revised runbook approval, verification confirmation, and F4 triage receipt — 2026-11-28

This dated receipt supersedes the pending-approval and overdue-triage states from November 22 above while preserving all prior receipts, tabletop records, and review history. In response to the explicit approval, verification-review, and F4 triage options naming the exact revised runbook hashes, the user replied **“confirm 1A, 2A, and 3A”**. The first UTC clock observation after receipt was **`2026-11-28T07:29:30Z`**; this is an observation bound, not a supplied message-authoring time or fictional incident time.

The designated participant is `jpiquot` in the previously confirmed combined capacities (Product/Release Owner, Security, GitHub administrator/operations, NuGet policy owner/credential custodian, and independent verification capacity). The primary agent rehashed the unchanged runbook at `HEAD` (`0e9912939cfe69c228d486725ebc23c830d657b5`) before recording this receipt:

| Capacity / actual receipt | Decision confirmed at `2026-11-28T07:29:30Z` |
| --- | --- |
| Product / Release Owner (1A) | Approve the revised [docs/release-incident-response.md](../../docs/release-incident-response.md) committed in `9d1e4f4939b5991bd754b781fd0130f40c0736e0` and unchanged at `HEAD` `0e9912939cfe69c228d486725ebc23c830d657b5`: raw UTF-8/CRLF SHA-256 **`24d1e9b5666b89fe16a9382bce0630b83f25bea2f5c9bae476c284785611476e`** and Git blob ID **`15b453a464a28a7cd245b5076bfebece3445c094`**. Supersedes the historical November 17 approval (`24814b2f4a13b76b2c88e3ae72dfcd22b52abb09b0ba5b496683bd7fe0c69d5c`); any later byte change requires a new digest-bound receipt. |
| Independent verification capacity (2A) | Confirm separate review of the `HEAD` (`0e9912939cfe69c228d486725ebc23c830d657b5`) post-patch verification results and limits below, with no additional containment-critical finding raised. Combined-role capacity (`jpiquot` filling all roles) remains explicit; no live containment, credential revocation, immutable AD-15 preservation, or wall-clock SLA performance is claimed. |
| Release Owner / Story 17.8 triage (`F4` / `17.2-R2`, 3A) | Triaged: missing `.github/workflows/release-incident-preservation.yml` remains routed to Story 17.8 in normal Epic 17 sequence (after Stories 17.3–17.7) with no interim ambient workaround. Closes the overdue `2026-11-18T00:00:00Z` triage action without claiming preservation capability exists; real product retry remains forbidden until Story 17.8 is implemented and independently verified. |

All Story 17.2 implementation tasks, review patches (`P1–P10`, `D1`, `F1–F4`), and acceptance criteria are now satisfied. This receipt neither closes `G-8`, `GOV-ACCEPT-1`, `GOV-ACCEPT-2`, or `REL-AI-1`, nor authorizes publication, unlisting, credential changes, workflow dispatch, or remote operations. Historical [rel-ai-1-release-evidence-ledger.md](rel-ai-1-release-evidence-ledger.md) records and NC-35/NC-36 dispositions remain untouched.

## Verification

- Run the existing `eng/validate-docs.ps1` gate, including DocFX; confirm the generated runbook page and navigation. Use a disposable repository copy beneath `run-with-temp`; retain useful logs outside its scratch directory.
- Run `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 eng/validate-story-artifacts.py --project-root .` and `git diff --check`.
- Manually trace each acceptance criterion through the runbook and actual tabletop record. Validate links, redaction, digest-bound approvals and unresolved findings. No new prose-mirroring tests.
- Investigation at source `66250f89230e1b261b51ce873c83d96176a9d325`: authenticated GitHub variable read returned literal `false`, updated `2026-11-16T21:30:25Z`; server Date `2026-11-17T00:04:15Z`. This later observation does not prove credential revocation, complete containment, or permission to publish; the 2026-11-16 `true` observation remains historical.

### Executed checks — 2026-11-17

- `run-with-temp bash artifacts/verification/story-17-2/verify-docs.sh`: disposable shared-object clone of baseline `66250f89230e1b261b51ce873c83d96176a9d325`, overlaid with the implementation. Only directly declared Builds and Platform checkouts were materialized; no nested submodule was initialized. The job ran `dotnet tool restore` and the unchanged full invocation `pwsh -NoProfile -File eng/validate-docs.ps1`, without skip switches or weakened audits. The existing Release solution build completed, all **8** documentation snippets passed, and DocFX **2.81.0** metadata/build completed with SDK **11.0.100**.
- The full documentation gate **exited 1**, with exactly two pre-existing blockers: `docs/fluent-ui-v5-contingency.md` lists both `Microsoft.FluentUI.AspNetCore.Components` and `.Icons` at **5.0.1**, while the root HEAD-selected Builds catalog at `66fc4855a6d3746fbfc8ad02db27d4ecd44dd8c9` selects **5.1.0** for both. The contingency page and Builds gitlink were not changed by this story. Exact gate output is retained in `artifacts/verification/story-17-2/validate-docs.log`; its existing evidence manifest is retained as `validation-manifest.json` in that directory. This blocker is reported, not suppressed or repaired through an unrelated dependency/documentation change.
- Independently inspected the generated `release-incident-response.html` (27,540 bytes), root TOC link `href="release-incident-response.html"`, and How-to index link `href="../release-incident-response.html"`. The rendered title and complete operator content are present. Copies are retained in the same ignored verification directory; these local generated files are verification material, not immutable release evidence or a deployed-site claim.
- After adding the new page to the validator's existing manifest `inputRoots`, ran the narrower final-source content invocation in another disposable clone: `run-with-temp bash artifacts/verification/story-17-2/verify-content.sh`, executing `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild`. It **exited 1 with the same two contingency blockers only**. Its `content-validation-manifest.json` includes `docs/release-incident-response.md`; the runbook, DocFX configuration and navigation bytes are unchanged from the full rendered check. No new runbook content/frontmatter/redaction failure was reported.
- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 eng/validate-story-artifacts.py --project-root .`: **passed**, `Story artifact validation passed.` Log: `artifacts/verification/story-17-2/validate-story-artifacts.log`.
- `git diff --check`: **passed**. Confirmed UTF-8/CRLF runbook bytes, all **8/8** current package IDs, official-documentation links, site inclusions and exact raw-file SHA-256 `303fc2b614be85dae00664f0aa92b68ebd5cf6419d5e41b739f5dbef5090c04e`. Both disposable jobs and their children finished; useful logs/rendered outputs were retained outside scratch.

### Acceptance trace and limits

| Criterion | Runbook/record evidence | Result |
| --- | --- | --- |
| Trigger-to-response owners, acknowledgement/containment targets, escalation, evidence, credentials, effects, recovery and communication | Runbook sections “Recognize and acknowledge”, “Stop publication and remove authority”, “Preserve original identities and bytes”, “Inventory external effects”, and “Communicate and recover”. | Implemented; prospective targets are owner-approved. Exact-byte runbook approval and tabletop decisions are recorded in the actual owner receipt; live response-time performance is not claimed. |
| Cached-true / issued credential containment without candidate execution | Explicit cached-true branch, separate job/authority invalidation and independent confirmation; synthetic T+05 inject. | Procedure implemented; participant confirmed containment decisions and separate review of verification results/limits in the actual owner receipt. |
| Missing immutable preservation or critical findings forbid retry | Explicit current missing-workflow/service-unavailable branches, AD-15 slot/namespace/immutability/prior-recovery requirements and recovery conjunction; T+20/T+60 injects. | Safe hold procedure implemented; no real preservation capability/result claimed. Participant accepted the recovery hold and bounded triage follow-up; no real preservation result is claimed. |
| Dated human tabletop with truthful timestamps, decisions, gaps, owners, deadlines and approvals | This spec's single tabletop record distinguishes preparation UTC, scenario offsets, real prior owner decisions and the prepared receipt rows followed by dated actual owner responses and the final revised-runbook approval. | Met by the actual tabletop receipt and final replacement-digest approval receipt. Review fixes and focused verification are complete; G-8 and every publication gate remain independent. |


### Primary-agent verification and pending participation

The primary agent read the baseline-to-working-tree diff, including both untracked new files, and independently checked the retained rendered page/TOC/How-to links and the unchanged runbook digest. The per-story artifact check initially identified the missing File List; the list is now present and that exact command passes. Global artifact validation and `git diff --check` also pass. No new DocFX/source change followed the full and narrow documentation runs.

Two synthetic tabletop decision prompts and a separate exact-byte runbook approval/verification prompt were issued to the owner in this session. At the implementation handoff, responses remained pending; the earlier spec approval and role assignment were not converted into tabletop or runbook-approval receipts. The later actual owner receipt above supplies the missing decisions and exact-digest approval before the review transition. There is no new CI run link because no workflow was dispatched or remote operation performed.



### Final verification after review patches — 2026-11-17

- `run-with-temp bash artifacts/verification/story-17-2/verify-docs-post-review.sh` repeated the full existing docs invocation in a fresh disposable clone, including the final changed test source in its overlay. Release build, all **8** documentation snippets and DocFX metadata/build completed. The final page and both navigation links were independently inspected, including the corrected immediate-classification text and minimum/full-scope inventory wording.
- The overall gate again **exited 1 with the same two unchanged Fluent contingency catalog-version errors only**. No new runbook or test-source failure was reported. Final logs, rendered outputs and the existing validation manifest are retained under `artifacts/verification/story-17-2/post-review/`. This records the known blocker; no check was suppressed and no unrelated page/catalog was changed.
- Focused executable metadata tests: **2 passed, 0 failed, 0 skipped, 0 not run**; disposable Debug build **0 warnings / 0 errors**. Source-only XML documentation was then included in the successful final full build. Runbook raw SHA-256 remains `24814b2f4a13b76b2c88e3ae72dfcd22b52abb09b0ba5b496683bd7fe0c69d5c`; Git-normalized blob ID read directly from Git is `a0de1e2cdacfc205ef7047692dd07ff6984281c7`.
- Primary-agent verification confirmed all six patch entries are resolved. BH-1 is rejected on the explicit claims-separation rule; Edge Case Hunter reported no finding; BH-7 remains the required agent-context deferral. No reviewer finding is silently dropped. Replacement runbook approval is the remaining current acceptance receipt; the tabletop task stays unchecked until it arrives.



### Completion gates — 2026-11-17

After the actual replacement approval, targeted and repository-wide `eng/validate-story-artifacts.py` checks both printed `Story artifact validation passed.` and `git diff --check` passed. The owning repository's lockfile-pinned and installed commitlint CLI **21.2.4** validated the exact completion message with **0 problems / 0 warnings**; successful output is retained in `artifacts/verification/story-17-2/post-review/commitlint.log`. Root-declared submodule working trees are clean and selected HEADs have no unpublished current-HEAD work; unrelated historical side branches are preserved. The workflow's final local commit contains only this story's remaining four paths; no push is part of completion.


### Verification on current main — 2026-11-22

At FrontComposer `6d90c6154d2ca6155eeae17a3f280dc22ff9560b`, a clean checkout using the submodule revisions committed by FrontComposer passed `pwsh -NoProfile -File eng/validate-docs.ps1`. All eight snippets compiled. DocFX 2.81.0 rendered the runbook, and the generated table of contents and How-to index both linked to it. The two Fluent catalog errors recorded on November 17 did not recur.

Both focused xUnit tests passed, including the negative case that removes the runbook's `reviewed` field. The repository-wide story-artifact check and the commit-scope check for the original Story 17.2 completion commit `eae2cccfce573837bf238d195c0bf75267a577f5` also passed. The checked-out runbook's SHA-256 matched the final approval receipt (`24814b2f4a13b76b2c88e3ae72dfcd22b52abb09b0ba5b496683bd7fe0c69d5c`). Story 17.2 and GOV-I remain in sprint review.

### Verification of the review patches — 2026-11-22

- A disposable checkout of `6d90c6154d2ca6155eeae17a3f280dc22ff9560b` used the root-selected dependency commits and the revised story sources. The original and revised PowerShell file-selection functions returned the same 148 Markdown paths.
- `pwsh -NoProfile -File eng/validate-docs.ps1` passed with all eight snippets and zero blocking failures. DocFX 2.81.0 generated the revised runbook. Both `toc.html` and `how-to/index.html` linked to it, as independently checked against the generated files. The runbook used for this build matched the proposed SHA-256 in the patch disposition.
- The Release build of the SourceTools test project passed with zero warnings and errors. The first build had identified xUnit1030 in the new fixture. After removing the disallowed await configuration, all **71** cases in `DocsSiteValidationTests` passed, with zero skipped or not run. The four new real DocFX fixtures covered successful publication, removing each navigation link independently, and removing the page with stale HTML pre-seeded from a prior build.

### Verification on HEAD — 2026-11-28

- At FrontComposer `HEAD` `0e9912939cfe69c228d486725ebc23c830d657b5` (containing Story 17.2 commits `b23b4cb9`, `eae2cccf`, and `9d1e4f4939b5991bd754b781fd0130f40c0736e0`), `run-with-temp dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release` succeeded with **0 warnings / 0 errors**.
- Direct xUnit v3 execution of `DocsSiteValidationTests` passed **72/72** tests (**0 failed, 0 skipped, 0 not run**), including the negative metadata test and the four real DocFX page/navigation link tests (`none`, `toc`, `index`, `page`).
- In a disposable clone of `HEAD` with direct root-declared `references/Hexalith.Builds` and `references/Hexalith.Platform` (no nested submodules initialized), `dotnet tool restore` (DocFX `2.81.0`) and `pwsh -NoProfile -File eng/validate-docs.ps1` (no skip switches) exited **0 (`Docs validation passed.`)**, generating `docs/_site/release-incident-response.html` (35,828 bytes) with verified links in `docs/_site/toc.html` and `docs/_site/how-to/index.html`.
- Rehashed `docs/release-incident-response.md` at `2026-11-28T07:29:30Z`: raw UTF-8/CRLF SHA-256 `24d1e9b5666b89fe16a9382bce0630b83f25bea2f5c9bae476c284785611476e`, Git blob ID `15b453a464a28a7cd245b5076bfebece3445c094` (identical at both `9d1e4f4939b5991bd754b781fd0130f40c0736e0` and `HEAD`). Confirmed `.github/workflows/release-incident-preservation.yml` remains absent.
- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 eng/validate-story-artifacts.py --project-root .` and `git diff --check` passed after recording the November 28 receipt.

## File List

- `docs/release-incident-response.md` — canonical operator runbook and tabletop procedure.
- `docs/docfx.json` — includes the runbook in the documentation build.
- `docs/toc.yml` — adds the runbook under How-to navigation.
- `docs/how-to/index.md` — links the incident response guide.
- `eng/validate-docs.ps1` — selects the page for existing validation and input tracking.
- `_bmad-output/implementation-artifacts/spec-17-2-publish-and-exercise-the-release-incident-runbook.md` — approved intent, implementation, verification and actual/pending tabletop receipts.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — records the review-ready story and GOV-I final approval evidence reference.
- `_bmad-output/implementation-artifacts/epic-17-context.md` — refreshes stale planning context before this story's implementation.

- `_bmad-output/implementation-artifacts/deferred-work.md` — records the review-routed agent-context follow-up without modifying existing entries.

- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs` — adds executable missing-runbook-metadata regression and generic root-page metadata coverage.
