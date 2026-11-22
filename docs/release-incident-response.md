---
title: "Respond to a release incident"
description: "Contain suspected release credential exposure, unauthorized publication, package mismatch, or partial publication while preserving original evidence."
genre: how-to
audience: operator
ownerStory: 17-2-i-gov-i-publish-and-exercise-the-release-incident-runbook
status: controlled
reviewed: 2026-11-22
uid: frontcomposer.release.incident-response
slug: release-incident-response
---

# Respond to a release incident

This is the canonical FrontComposer release incident runbook. The Release Owner
owns it with Security. It applies to `Hexalith/Hexalith.FrontComposer`, its
`production` environment, GitHub Releases, and the NuGet packages listed below.
Its approval and participant-executed tabletop are recorded in
[the Story 17.2 record](https://github.com/Hexalith/Hexalith.FrontComposer/blob/main/_bmad-output/implementation-artifacts/spec-17-2-publish-and-exercise-the-release-incident-runbook.md).
Until that record contains approval of the exact raw-file SHA-256 and the required
exercise receipts, GOV-I remains open. A change to these bytes requires renewed
approval; documentation availability alone proves neither approval nor readiness.
Repository links using `main` below locate the current sources; they are mutable
navigation, not approval evidence. Read the exact digest/revision named by the
actual approval or incident receipt before relying on an authority.

To verify a published revision, use a clean, trusted checkout. Compare
`git rev-parse HEAD:docs/release-incident-response.md` with the Git blob ID
in the approval receipt. From the repository root, run
`sha256sum docs/release-incident-response.md` and compare the complete digest
with the receipt. The approved byte representation is UTF-8 with CRLF line
endings as specified by the repository's `.gitattributes`. Git stores the text
blob with LF endings, so hashing the output of `git show` yields a different digest.

## Recognize and acknowledge

Open an incident on suspicion; do not wait for a confirmed malicious package.
Triggers include a possibly exposed publication credential or OIDC token;
candidate-controlled code receiving publication authority; an unauthorized actor,
environment approval, dispatch, policy or workflow closure; missing or
unauthenticated manifests, handoffs or ledger observations; unequal package
bytes or invalid repository signatures; unexpected tags, assets or package
versions; and failed, cancelled or incomplete publication with possible external
effects. A green run or later successful verification does not dismiss these
triggers. Historical NC-35/NC-36 records are retained, not reinterpreted by a later
stop observation.

Record the first detection as `T0` in UTC and start elapsed wall-clock timing.
On 2026-11-17 the owner accepted acknowledgement within **15 minutes** and
containment within **60 minutes** of detection. These prospective targets do not
claim historical response performance. Acknowledgement names the incident lead,
suspected scope, affected identities and next update time. Containment requires
verified removal of publication authority, not merely a changed flag or accepted
cancellation request.

| Capacity | Current designated participant | Responsibility |
| --- | --- | --- |
| Product / Release Owner | `jpiquot` | Lead, approve runbook and response targets, refuse publication, decide consumer response and eventual re-enable. |
| Security | `jpiquot` | Assess exposure, coordinate revocation/rotation, judge containment and approve security remediation. |
| GitHub administrator / operations | `jpiquot` | Stop flag, pending approvals, dispatch restrictions, run cancellation, repository authority and GitHub support escalation. |
| NuGet policy owner / credential custodian | `jpiquot` | Stop new credential exchange, revoke exposed authority, inventory and unlist affected versions when separately authorized, contact NuGet support. |
| Independent verification capacity | `jpiquot` | Perform a separate read-only verification using independently fetched API/asset evidence; record results and limitations separately from operator assertions. |

The owner explicitly selected one person covering all capacities. This records
combined capacity, not a second human reviewer. Exercise participation and each
capacity's decisions still need actual receipts. In a live incident the lead must
confirm availability immediately. If a capacity is unavailable or access fails,
escalate to the repository/organization owner or relevant service support, assign
an available authorized responder and record the gap; do not assume an unnamed
backup exists.

At detection, locate the selected private channel in the existing incident
record's communications entry. If none is recorded, the Release Owner selects
an access-controlled channel and records its locator, authorized participants
and custodian there; keep the private locator in restricted owner custody and
only a support-safe reference in this public record. No channel or contact
receipt is implied by this guide. Notify all required capacities through that
channel, recording each capacity explicitly even when `jpiquot` fills them all.
For provider escalation, sign in to the
[GitHub Support portal](https://support.github.com/) using its
[ticket procedure](https://docs.github.com/en/support/contacting-github-support/creating-a-support-ticket),
or use [NuGet's official contact page](https://www.nuget.org/policies/Contact)
and its signed-in **Contact Support** route; do not post exposure details to a
public issue or discussion. Record actual ticket references/responses in the
existing restricted incident record. These routes appoint no backup and claim
no contact has occurred. If acknowledgement is at risk, escalate before T+15 minutes;
if containment cannot be proven, escalate immediately and again before T+60
minutes. Record each recipient, channel, UTC time, response and next owner.
A service outage is containment-critical if it prevents verification of job
termination, revoked authority or credential expiry. If those checks and the
other containment checks below have passed, a GitHub Releases outage limited
to evidence storage allows the verifier to record containment. Preservation
and product retry stay blocked until immutable storage can be verified.
The Release Owner gives a status update at acknowledgement, at T+60 minutes, and
at least every 60 minutes while containment is unproven. Security may require
an earlier consumer advisory; the lead records the decision and deadline.

## Stop publication and remove authority

These are live-incident actions for authorized owners. A tabletop only describes
them: it must not change settings, cancel runs, mint or revoke credentials,
dispatch workflows, publish, unlist or send messages. Do not execute candidate
source, release scripts, package hooks, extracted binaries or ambient recovery
helpers during containment. Use trusted administration interfaces and read-only
data inspection from a clean operator environment.

Use the following trusted administration references for the actions below;
they are instructions, not operations executed by this guide or the tabletop:

| Action | Official administration reference |
| --- | --- |
| Stop flag and authenticated readback | GitHub [create repository variable](https://docs.github.com/en/rest/actions/variables#create-a-repository-variable), [update repository variable](https://docs.github.com/en/rest/actions/variables#update-a-repository-variable) and [get repository variable](https://docs.github.com/en/rest/actions/variables#get-a-repository-variable), targeting the exact variable/repository below. |
| Refuse pending `production` approval | [Get pending deployments](https://docs.github.com/en/rest/actions/workflow-runs#get-pending-deployments-for-a-workflow-run), then an authorized reviewer uses [review pending deployments](https://docs.github.com/en/rest/actions/workflow-runs#review-pending-deployments-for-a-workflow-run) with `state: rejected` for the recorded environment IDs. |
| Restrict new release execution | Administrator uses [disable workflow](https://docs.github.com/en/rest/actions/workflows#disable-a-workflow) for the identified publication-capable caller workflow, including `release.yml`, and confirms its state with [get workflow](https://docs.github.com/en/rest/actions/workflows#get-a-workflow). This does not terminate existing jobs or invalidate issued credentials. |
| Stop and verify in-flight jobs | [Cancel workflow run](https://docs.github.com/en/rest/actions/workflow-runs#cancel-a-workflow-run), [force cancel](https://docs.github.com/en/rest/actions/workflow-runs#force-cancel-a-workflow-run) if needed, then [get run attempt](https://docs.github.com/en/rest/actions/workflow-runs#get-a-workflow-run-attempt) and [list jobs for that attempt](https://docs.github.com/en/rest/actions/workflow-jobs#list-jobs-for-a-workflow-run-attempt) for terminal status. |
| Stop NuGet exchange / revoke static keys | Sign in to NuGet.org → username → [Trusted Publishing](https://www.nuget.org/account/TrustedPublishing), as described in [NuGet's setup guide](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing#github-actions-setup), and select the affected owner/policy for removal. For static keys use username → [API Keys](https://www.nuget.org/account/apikeys), following [NuGet's delete/regenerate guidance](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package). If an authorized removal control is unavailable, record that gap and escalate through NuGet Contact Support; do not assume revocation. |

1. **Release Owner / GitHub administrator:** refuse new production approvals,
   dispatches and reruns immediately. Record pending approvals and every active,
   queued or waiting publication-capable run, including reusable jobs and
   related attempts. Restrict new release execution and approval through
   authorized repository/environment administration; record the actual restriction
   and readback. Prevent bypass by other writers, automation and dispatch credentials.
2. **GitHub administrator:** set repository variable
   `HEXALITH_RELEASE_PUBLISH_ENABLED` to the exact lowercase string `false`.
   Update an existing variable. If an authenticated administrator confirms absence
   in the complete repository-variable listing, create it with that same value.
   On a permission error, incomplete listing or ambiguous 404, escalate the
   missing proof and keep the other restrictions in effect.
   From a separately authenticated read-only request, capture the resulting value,
   `updated_at`, HTTP status, server `Date`, observer and UTC observation time.
   An unset or other non-`true` value may stop a future job mechanically, but does
   not satisfy the required literal-false receipt. A readback failure is a gap.
   The following request is read-only and must not log request authorization:

   ```sh
   gh api --include repos/Hexalith/Hexalith.FrontComposer/actions/variables/HEXALITH_RELEASE_PUBLISH_ENABLED
   ```

3. **GitHub administrator:** after the dispatch restriction takes effect, list
   publication-capable runs and attempts again, following all pages. Reconcile
   both lists, including attempts queued during the restriction change. Cancel
   active publishers and confirm terminal status for every affected job through
   the Actions API. If new attempts appear or the restriction is uncertain,
   recheck the restriction and repeat the inventory before declaring containment.
   If normal cancellation does not stop a run, use the authorized force-cancel
   interface and escalate failures to GitHub support. Capture the request result
   and subsequent run/job observations. Confirm termination before recording it.
   Preserve available metadata first where possible without delaying containment. Never
   delete runs, logs, artifacts, tags or assets to stop publication.
4. **NuGet custodian + Security:** block further token exchanges by disabling or
   removing the affected trusted-publishing policy through owner administration;
   preserve its non-secret identity, scopes and original settings. Inventory
   already-issued temporary NuGet keys, static keys and other publication
   credentials. When exposure is plausible, revoke each affected reusable NuGet
   key, GitHub App credential, PAT or dispatch credential, or rotate it and
   verify that its old value is invalid. Include the full scope of each
   affected credential. Issue replacements only to a trusted execution path. Record the
   credential identifiers, scopes, latest possible issuance, expiry, revocation
   results and custody. Keep credential values private.
5. **Security:** treat an issued temporary key as potentially usable until
   independently confirmed revoked or expired. Stopping the policy prevents new
   exchange; it does not prove a previously issued key is revoked. NuGet documents
   temporary keys as valid for one hour. Request provider-assisted invalidation
   if self-service revocation cannot be confirmed. Unknown issuance/expiry,
   provider outage or inability to remove authority keeps containment unproven;
   escalate rather than substituting an assumed timeout. Record any possibly
   exposed OIDC/signing material and its provider-specific invalidation, even
   though the current FrontComposer flow does not require author signing.
6. **Independent verification capacity:** separately read the flag, restrictions,
   run/job termination, credential/policy receipts and external inventories.
   Confirm that affected actors, credentials and publishing paths have lost
   mutation authority across their full GitHub and NuGet scope. Record the
   authorized responders who retain trusted administrative access and why.
   Any unknown scope or still-usable affected credential keeps containment
   unproven. Once verified, record `contained_at` and calculate `contained_at - T0`.
   Otherwise record partial containment, the missing proof, owner and next
   escalation time. Do not test
   revocation with a package push or execute a candidate as verification.

**Cached-true branch:** the current `release-publish.yml` reads the flag during
preparation and later uses `steps.prepare.outputs.publish-enabled`. A job that
cached `true`, or already obtained a credential, can continue after the variable
changes. The flag is authoritative deny-only input for later preparation; live
containment also needs cancellation/restriction and credential invalidation.
Protected-environment approval, `true`, a green CI result, and Product approval
never grant publication authorization. Changing default token permissions is not
evidence that a token already issued to a running job has lost authority.

GitHub documents cancellation behavior and the
[force-cancel fallback](https://docs.github.com/en/rest/actions/workflow-runs#force-cancel-a-workflow-run).
NuGet describes
[trusted publishing and temporary credentials](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
and [API-key custody and revocation](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package).

## Preserve original identities and bytes

**Release Owner + Security**, starting at detection and before any retry, retain
the original candidate SHA, candidate descriptor/archive digests, active
policy commit/digest, actual execution pins and action closure identities,
Builds catalog gitlink, CI/Quality/Release/verification run IDs and attempts,
repository, workflow paths, actor/triggering actor, production review history,
environment protection, flag observations, planned versions and publication
marker. The candidate named by the authenticated handoff remains authoritative;
do not replace it with today's `main`, a tag head or a later successful run.

Capture authenticated API metadata and the raw available archives, manifests,
seals, attestations/fallback authorizations, CI and release handoffs, package and
symbol assets, logs and ledger observations. Record API pagination/completeness,
server times, artifact IDs/names/expiry, asset IDs/names/sizes/digests, and SHA-256
over downloaded bytes before treating contents as data. Preserve authentication
failures as quarantined bytes, not usable evidence. Missing/expired evidence is
not reconstructed by rebuilding, repacking, rerunning or resealing an old release.
Actions artifacts are supplemental replay material and can expire; local copies
and this tabletop record are not immutable release evidence.

Reference the existing
[REL-AI-1 release-evidence ledger](https://github.com/Hexalith/Hexalith.FrontComposer/blob/main/_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md) and
its immutable Release-attempt identities. Keep each historical disposition and
machine observation; append an evidence-backed observation only for an actual
attempt through the accepted ledger process. A later successful check, sign-off
or new attempt cannot erase, weaken or replace an incident-bearing observation.
A synthetic exercise creates no Release attempt or ledger row. Do not backfill
historical per-attempt evidence as part of this runbook exercise.

**Immutable AD-15 preservation:** if publication started and the product Release
is absent or incomplete, recovery requires a separately approved, candidate-free,
active-policy-authorized `incident_recovery` closure in the protected `production`
environment. It may write only evidence, without NuGet, OIDC, package or product
publication authority, to a separate immutable non-draft prerelease whose tag and
name are exactly:

```text
release-evidence/incident/<release_run_id>-<release_attempt>/<recovery_run_id>-<recovery_attempt>
```

The prerelease points to the authenticated original candidate; this reserved
namespace is excluded from product/version discovery. Its mandatory
`incident-evidence.json` follows the existing AD-15
`frontcomposer.release-incident-evidence.v1`
[preservation contract in the GOV-1 spine](https://github.com/Hexalith/Hexalith.FrontComposer/blob/main/_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md#ad-15--adopted-release-to-verifier-handoff-preserves-the-original-candidate), accounting for
`publication_candidate`, `descriptor`, `ci_handoff`, `release_handoff`, `manifest`,
`authorization`, `ledger_observation`, `github_inventory`, `nuget_inventory` and
`prior_recovery_inventory`. Each slot is present with its original byte identity,
quarantined with reason `authentication-failed`, or absent with the applicable
`not-produced`, `not-recorded`, `expired` or `api-unavailable` reason. Captured API
responses/transport errors support every absence claim; an absent source can be
truthfully recorded without pretending its bytes were preserved.

Before uploading any original bytes, Security checks whether the intended
audience may see them, including quarantined assets, logs and API responses.
If an asset may contain a credential, a signed URL or other restricted material,
hold the upload. Keep the original in restricted owner custody and record only a
support-safe reference in the incident record. Escalate to Security and the
Release Owner. A redacted copy can support investigation but cannot satisfy
the original-byte slot. Preservation remains incomplete and product retry
stays blocked until the contract can be met without disclosing restricted data.

Independently verify every slot/asset identity, the candidate, non-draft status
and immutability before retry. A failed recovery keeps its partial tag, draft and
assets untouched; a later recovery gets its own run-bound namespace and inventories
every prior recovery attempt. Never adopt, replace or delete prior recovery state.

**Current capability gap, source baseline `66250f89230e1b261b51ce873c83d96176a9d325`:**
`.github/workflows/release-incident-preservation.yml` does not exist. The AD-15
mechanism above is required future capability, not an available command. Do not
dispatch a similarly named workflow, use the legacy evidence-recovery selector,
invoke ambient repository helpers, or manually manufacture an immutable evidence
receipt. Escalate to the Release Owner and the implementation owner of Story 17.8.
Hold retry until the approved mechanism exists and a real preservation result
passes independent verification. If GitHub Releases is unavailable, retain safe
working copies and transport-error observations, escalate to GitHub support,
and keep retry forbidden until the service returns and preservation is verified
immutable. Do not weaken the prerequisite because evidence is expiring.

## Inventory external effects

**GitHub administrator + NuGet custodian**, with a separate read-only confirmation,
inventory the complete interval from the earliest plausible exposure/publication
through confirmed containment, including adjacent run attempts and versions.
Start immediately; provide the first inventory and explicit unknowns by T+60
minutes, update at least hourly until complete, and finish before recovery/re-enable.

For GitHub, inspect authenticated Releases (including drafts/prereleases and
reserved incident releases), tag/ref targets, asset identities/downloaded bytes,
immutability, related run/job timelines, artifacts, approvals and audit events.
Inventory unexpected tags, releases, versions and assets as well as planned ones.
Do not rely on a successful job or `publication_started=false`: observed external
mutation with a false marker is an incident. For NuGet inspect package/version
registration, listed/unlisted state, publication times, downloadable original
package bytes, repository signatures, and symbol acceptance/availability. Search
by exact ID/version as well as version listings; indexing delays and unlisting
can hide results from search.

The eight expected IDs in
[the release package list](https://github.com/Hexalith/Hexalith.FrontComposer/blob/main/tools/release-packages.json),
cross-checked with packable rows in
[the package inventory](https://github.com/Hexalith/Hexalith.FrontComposer/blob/main/eng/release-package-inventory.json)
at the affected source, are a **minimum**, not the exposure boundary. Follow
each affected credential/policy/actor's full package and repository scope,
including unexpected IDs and other repositories. Route those other scopes to
their authorized owners; `jpiquot`'s FrontComposer capacities confer no authority
over them. Record their response or an explicit unknown when access/ownership
cannot be confirmed. Inspect **all eight** below even if a failed run names only one:

| Package ID | Required effects to reconcile per affected version |
| --- | --- |
| `Hexalith.FrontComposer.Cli` | GitHub `.nupkg` / `.snupkg`, NuGet package and symbol state |
| `Hexalith.FrontComposer.Contracts` | GitHub `.nupkg` / `.snupkg`, NuGet package and symbol state |
| `Hexalith.FrontComposer.Contracts.UI` | GitHub `.nupkg` / `.snupkg`, NuGet package and symbol state |
| `Hexalith.FrontComposer.Mcp` | GitHub `.nupkg` / `.snupkg`, NuGet package and symbol state |
| `Hexalith.FrontComposer.Schema` | GitHub `.nupkg` / `.snupkg`, NuGet package and symbol state |
| `Hexalith.FrontComposer.Shell` | GitHub `.nupkg` / `.snupkg`, NuGet package and symbol state |
| `Hexalith.FrontComposer.SourceTools` | GitHub `.nupkg` / `.snupkg`, NuGet package and symbol state |
| `Hexalith.FrontComposer.Testing` | GitHub `.nupkg` / `.snupkg`, NuGet package and symbol state |

The AppHost and combined UI are non-packable; unexpected publication still
belongs in the inventory. Record expected/observed version, source identity,
raw hash, size, URL, UTC observation, API status, listing/signature/symbol result
and evidence reference. GitHub bytes must equal the sealed bytes exactly.
NuGet may add only a valid root `.signature.p7s`; every other normalized ZIP
member must remain byte-equivalent. Never restore/install the packages to inspect
them during containment. When safe verification is unavailable, record unknown.

Distinguish **verified absent** (complete authenticated service observation at a
recorded time) from **not observed**, **not recorded**, **expired** and
**API unavailable**. A 404, permission failure, partial page, delayed index or
inaccessible symbol server alone does not prove no external effect. Escalate
unresolved gaps to the service owner. Classify known incident facts and explicit
unknowns immediately; append timestamped observations as evidence arrives through
the existing incident/ledger process without replacing or weakening earlier
incident observations. An incomplete inventory must not delay incident
classification. Record an unknown effect and its source in the preservation
evidence, with an owner and a next check in the inventory. Keep product retry
blocked until each external effect has been reconciled, even when the evidence
recording the unknown has been verified immutable.

## Communicate and recover

**Release Owner + Security:** record the cause, exposure window, affected package
versions and consumers, containment evidence, remaining unknowns and corrective
actions. Use the existing incident/spec/ledger records rather than a new evidence
system. Keep a support-safe account with UTC times, named capacities, actual
decisions, owners and deadlines. Keep secrets, raw credentials, signed download
URLs, private paths and sensitive logs out of public documentation; restrict
original sensitive material in approved owner custody and reference redacted
receipts. Do not paste tokens as proof of revocation.

Decide the audience and channel for each update (responders, repository/package
owners, affected consumers and providers), then obtain the appropriate owner
authorization before sending. Record when notices were actually sent and by whom;
a drafted notice is not delivery. Communicate affected IDs/versions, observed
impact, whether containment is verified, safe consumer action, and the next update
time. Do not announce successful remediation while inventory or revocation is unknown.

Prefer separately authorized **unlisting** of affected NuGet versions and a
corrected release under a **new version**. Unlisting reduces discovery but exact
version restore remains possible; warn consumers accordingly. Do not overwrite,
delete/recreate or reseal old packages, tags, product Releases or evidence.
Retain original byte identities and append the corrective-version relationship
through existing records. Product retry, preservation dispatch and unlisting
each require their own applicable authority; none is authorized by this tabletop.
See [NuGet unlisting policy](https://learn.microsoft.com/en-us/nuget/nuget-org/policies/deleting-packages)
and [correcting a published package](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/publish-nuget-package).

**Recovery decision:** keep publication halted until all of the following have
evidence, owners and recorded completion:

- Cause/remediation and exposure scope are documented, all containment-critical
  findings are fixed, credentials are revoked/rotated or proven expired, and
  replacement custody prevents recurrence.
- Every GitHub/NuGet effect across the complete interval is reconciled, affected
  versions and consumer actions are decided, and required communications have
  actual receipts.
- Original manifests/handoffs/assets/ledger observations remain intact. Required
  AD-15 immutable preservation has succeeded, accounts for prior recoveries and
  missing evidence, and is independently verified. Missing safe preservation
  forbids retry even when the proposed correction passes tests.
- Independent verification records exact identities, observations and limitations;
  the Release Owner approves the recovery plan and an explicitly scoped re-enable
  decision with UTC time, candidate, policy/execution identities and evidence.
- **All existing G-8 conditions are satisfied separately:** an owner-accepted
  Builds split revision (EXT-BUILDS-1) enters approved AD-16 lineage through delayed
  activation under the
  [GOV-1 AD-16 lineage authority](https://github.com/Hexalith/Hexalith.FrontComposer/blob/main/_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md#ad-16--adopted-hexalithbuilds-workflow-revision-is-an-owner-accepted-external-input);
  GOV-B through GOV-H are implemented and evidenced; GOV-I has an
  approved runbook and actual tabletop with no unresolved containment-critical
  finding; GOV-J passes on one unchanged candidate; source reconciliation and
  GOV-ACCEPT-1/2 have their required exact-identity receipts. All other existing
  release gates, including G-1/G-2/G-3, still apply.

Only after that conjunction may an authorized administrator perform the scoped
re-enable and record authenticated readback. Setting `true` supplies no authority
itself. A later incident restarts this runbook; a successful new attempt cannot
make the original incident compliant. As of the stated source baseline the
legacy publisher and missing preservation capability prevent treating this
procedure or its approval as G-8 closure or permission to publish.

## Rehearse without live effects

Use the existing Story 17.2 spec for the single dated support-safe tabletop
record. Give participants a clearly synthetic partial-publication/possible-key
exposure scenario, then inject a cached-true job, unavailable immutable preservation
and a proposed recovery. Use historical attempt identities only as references;
never present the synthetic events as observations about those attempts.

Record scenario elapsed times separately from actual response UTC timestamps.
Each capacity must state its acknowledgement/containment decisions, evidence to
verify, credential action, complete package inventory, unavailable-service response,
recovery refusal/prerequisites and communications. Capture real response receipts,
findings with criticality, assigned owners/deadlines, separate verification and
Product/Release approval of the exact runbook raw-file SHA-256. Do not infer an
exercise response or digest approval from a previous role assignment. Missing
receipts or unresolved containment-critical findings leave GOV-I open.
