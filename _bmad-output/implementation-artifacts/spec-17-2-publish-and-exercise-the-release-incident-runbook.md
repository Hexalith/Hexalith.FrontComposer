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

## Spec Change Log

- 2026-11-17: recorded the actual owner confirmation of all three tabletop/approval requests at first post-receipt UTC observation `2026-11-17T07:15:31Z`; marked the tabletop task complete without changing frozen intent or approved runbook bytes.
- 2026-11-17: implemented runbook/site integration and prepared the single support-safe tabletop record. Frozen intent/owner decisions were preserved. Actual participant verification/approval remains pending; no governance acceptance or publication authorization was inferred.

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

The surviving patches share no root cause merely because they touch the same guide. Each is a direct correction to demonstrated documentation or executable-validation behavior, adds no public API or new state guard, and preserves the approved intent. No intent-gap or bad-spec loopback is required. Runbook patches will supersede the earlier exact-byte approval and require a new digest receipt after verification.

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
| Dated human tabletop with truthful timestamps, decisions, gaps, owners, deadlines and approvals | This spec's single tabletop record distinguishes preparation UTC, scenario offsets, real prior owner decisions and six pending actual-receipt rows. | Met by the dated actual owner receipt: all three decisions and exact-digest approval confirmed. Review remains required; G-8 and every publication gate remain independent. |


### Primary-agent verification and pending participation

The primary agent read the baseline-to-working-tree diff, including both untracked new files, and independently checked the retained rendered page/TOC/How-to links and the unchanged runbook digest. The per-story artifact check initially identified the missing File List; the list is now present and that exact command passes. Global artifact validation and `git diff --check` also pass. No new DocFX/source change followed the full and narrow documentation runs.

Two synthetic tabletop decision prompts and a separate exact-byte runbook approval/verification prompt were issued to the owner in this session. At the implementation handoff, responses remained pending; the earlier spec approval and role assignment were not converted into tabletop or runbook-approval receipts. The later actual owner receipt above supplies the missing decisions and exact-digest approval before the review transition. There is no new CI run link because no workflow was dispatched or remote operation performed.


## File List

- `docs/release-incident-response.md` — canonical operator runbook and tabletop procedure.
- `docs/docfx.json` — includes the runbook in the documentation build.
- `docs/toc.yml` — adds the runbook under How-to navigation.
- `docs/how-to/index.md` — links the incident response guide.
- `eng/validate-docs.ps1` — selects the page for existing validation and input tracking.
- `_bmad-output/implementation-artifacts/spec-17-2-publish-and-exercise-the-release-incident-runbook.md` — approved intent, implementation, verification and actual/pending tabletop receipts.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — records the in-progress story and GOV-I evidence reference.
- `_bmad-output/implementation-artifacts/epic-17-context.md` — refreshes stale planning context before this story's implementation.

- `_bmad-output/implementation-artifacts/deferred-work.md` — records the review-routed agent-context follow-up without modifying existing entries.
