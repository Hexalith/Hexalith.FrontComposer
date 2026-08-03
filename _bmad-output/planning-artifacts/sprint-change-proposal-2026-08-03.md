---
project: frontcomposer
date: 2026-08-03
workflow: bmad-correct-course
mode: Batch
trigger: "REL-AI-1: Own the FR24 exact-artifact pre-publication gate for signed/timestamped packages, symbols, SBOM, checksums, package inventory, consumer validation, sealed manifest/readiness, durable GitHub Release evidence, published-byte verification, and historical reconciliation."
status: approved-and-applied
scope: Moderate
implementationRisk: High
recommendedApproach: Direct Adjustment
accountableOwner: Release Owner
publicationAuthorized: false
approvedBy: Administrator
approvedDate: 2026-08-03
appliedDate: 2026-08-03
refines:
  - sprint-change-proposal-2026-07-16.md
  - sprint-change-proposal-2026-08-02.md
---

# Sprint Change Proposal: Re-seal REL-AI-1 Operational Truth and Authorization

## 1. Issue Summary

### Trigger

The requested action is already the canonical open sprint action `REL-AI-1`, and
the same trigger was approved and applied in the 2026-07-16 Correct Course
proposal. The ownership model and FR24 product requirement do not need to be
created again. The current correction is required because implementation and
live repository state have moved beyond that proposal while several planning
artifacts and release controls have not remained synchronized.

### Evidence snapshot

The following evidence was inspected on 2026-08-03:

- FR24, NFR12, and SM2 already define the intended exact-artifact contract in
  `_bmad-output/planning-artifacts/prd.md`.
- The architecture already requires a pre-publication evidence chain, sealed
  `hexalith.release-evidence.v2` manifest, protected publication authorization,
  durable GitHub Release evidence, and published-byte verification.
- REL-4's technical freeze guard is implemented in
  `.github/workflows/release.yml`. It defaults to frozen unless the repository
  variable `HEXALITH_RELEASE_PUBLISH_ENABLED` is exactly `true`.
- A CI-authoritative frozen Release run exists at
  <https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29703682203>:
  the freeze guard succeeded and the reusable release job was skipped.
- The live repository variable was `true` when inspected on 2026-08-03. This
  opened the publish-capable reusable release path. For example,
  <https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30760188983>
  passed the freeze guard, entered the reusable release job, and failed at
  Semantic Release.
- No GitHub Release newer than `v4.0.1` was observed. This does not establish
  that all possible external publication side effects are absent; it only
  confirms that the inspected attempts did not create a newer GitHub Release.
- The current workflow evaluates the repository variable before the REL-3
  evidence chain can produce `classification=ready`. Therefore the variable
  cannot, by itself, be the post-evidence publication authorization implied by
  the current REL-5 acceptance wording.
- Upstream issue
  <https://github.com/Hexalith/Hexalith.Builds/issues/17> was closed on
  2026-07-20 without a qualifying accepted revision. The 2026-08-02 proposal
  already records that local GOV-1 work may proceed while upstream integration,
  end-to-end completion, eligibility, and unfreeze remain blocked.
- GitHub Releases `v4.0.0` and `v4.0.1` each expose 16 package assets (eight
  `.nupkg` and eight `.snupkg`) but no SBOM, checksum, inventory, readiness,
  sealed-manifest, or release-evidence assets. `dotnet nuget verify --all` on
  the downloaded `Hexalith.Application.Contracts` package from each release
  returned `NU3004: The package is not signed`.
- The historical ledger currently stops at `v3.2.2`; it omits `v4.0.0` and
  `v4.0.1`.
- Sprint tracking still describes REL-4 as unimplemented in the REL-AI-1 action,
  while the REL-4 story is `in-review`; REL-3 frontmatter says `done` while its
  body and sprint tracking say review; REL-5 frontmatter says `in-progress`
  while its body says `ready-for-dev`.

### Core problem

The release gate currently has two distinct concepts conflated:

1. a coarse switch that permits the reusable release workflow to execute; and
2. FR24 publication authorization granted only after the exact candidate has a
   sealed `classification=ready`, `publish_authorized=true` evidence set.

Because the coarse switch is checked before candidate evidence is generated, it
cannot prove or replace the second authorization. Leaving it `true` exposes a
publish-capable path while the qualifying upstream/GOV-1 seam, production
signing identity and secrets, post-classification approval, durable evidence,
and historical reconciliation remain incomplete.

### Required decision

Treat this as a high-risk, moderate-scope direct adjustment:

- immediately restore the coarse repository switch to fail-closed operational
  state;
- correct the planning and implementation artifacts to current truth;
- explicitly separate workflow-execution enablement from post-evidence
  publication authorization;
- complete the existing REL-4 -> REL-3 -> GOV-1 -> REL-5 chain without creating
  a duplicate epic or ownership action; and
- extend the historical ledger through `v4.0.1`.

FR24 remains unchanged. Publication remains unauthorized.

## 2. Impact Analysis

### Epic and story impact

- No new product epic is required.
- No existing product story is invalidated.
- `REL-AI-1` remains the single accountable umbrella action owned by the Release
  Owner.
- REL-4 remains the technical coarse freeze control and must complete review
  with its first frozen-run evidence recorded.
- REL-3 remains the repository-local exact-artifact evidence implementation. It
  must be represented as review work, not completed release readiness, until
  review accepts the implementation and the transferred real-release
  conditions are fulfilled through REL-5.
- GOV-1 remains the dependency/provenance correctness gate. Local work may
  continue; upstream integration and governed release eligibility remain
  blocked until a reopened or successor upstream revision satisfies the
  accepted contract.
- REL-5 remains the operational enablement and closure story. Its acceptance
  criteria must distinguish execution enablement from publication
  authorization and require a bounded, auditable authorization lifecycle.

### Artifact impact

| Artifact | Impact | Required action |
|---|---|---|
| PRD | None | Preserve FR24, NFR12, SM2, and D-6 as written. |
| Architecture | None | Preserve the existing candidate/evidence/protected-publication sequence. |
| UX | None | This is release governance with no user-experience change. |
| Epics | Clarification only | Preserve existing ownership and dependency graph; do not add a duplicate story. |
| Sprint status | Material correction | Replace stale preimplementation REL-AI-1 control/progress text and retain open status. |
| REL-4 story | Evidence correction | Record the first CI-authoritative frozen-run URL and keep review status until accepted. |
| REL-3 story | Truth-state correction | Align frontmatter/body/sprint tracking to review and clarify transferred closure conditions. |
| REL-5 story | Material correction | Align status and repair the circular authorization wording. |
| Historical ledger | Material correction | Add `v4.0.0` and `v4.0.1` as affected, non-compliant releases. |
| Live repository control | Immediate containment | Set the variable absent or non-`true` until an approved governed mechanism is ready. |

### Technical impact

- The implemented REL-4 guard remains useful as a fail-closed coarse control.
- The variable must not be described as sufficient FR24 publication
  authorization.
- A compliant release needs a workflow seam that can:

  1. build and identify the exact candidate once;
  2. sign and timestamp packages and symbols;
  3. generate SBOM, checksums, package inventory, consumer-validation results,
     policy and dependency/provenance evidence;
  4. seal a manifest with `classification=ready` and
     `publish_authorized=true`;
  5. pause before any external publication for protected owner authorization;
  6. publish only the manifest-bound bytes;
  7. download or otherwise independently resolve the published bytes and verify
     their identity; and
  8. attach the durable evidence set to the GitHub Release and reconcile the
     ledger.

- The current upstream reusable workflow does not yet provide the qualifying
  governed seam. Enabling its caller before that seam exists is not an
  acceptable substitute.
- Existing local evidence-generation work remains valid and should not be
  discarded.

### Risk impact

Implementation risk is **High** because a mistaken control state can permit
external package publication, which is not reliably reversible. Documentation
edits are low risk; the operational release decision is not.

Primary risks:

- an automatic Release event enters a publish-capable job while prerequisites
  are incomplete;
- an owner mistakes the coarse variable for evidence-backed authorization;
- a failed job creates partial external side effects that GitHub Release history
  alone cannot disprove;
- release history appears reconciled while `v4.0.0` and `v4.0.1` are absent;
- conflicting story statuses cause premature closure of REL-AI-1.

Mitigations:

- restore the live switch to non-`true` immediately after proposal approval;
- record before/after variable state and affected run URLs without exposing
  secrets;
- require the qualifying workflow seam or an explicitly approved equivalent
  contingency before any enablement;
- use one bounded candidate authorization with automatic or assured reset;
- inspect all registries and release surfaces after any attempted publication;
  and
- keep historical non-compliance visible rather than retroactively claiming
  compliance.

## 3. Recommended Approach

### Selected path: Direct Adjustment

Continue with the existing FR24/REL-AI-1 plan and correct the operational and
artifact drift. This is the smallest viable path because the requirement,
architecture, ownership, and story chain already exist.

### Options considered

| Option | Viability | Assessment |
|---|---|---|
| Direct adjustment | Viable and recommended | Preserves completed work, closes the unsafe control drift, and repairs the existing execution chain. |
| Roll back REL-3/REL-4 | Not viable | Would remove useful fail-closed controls and evidence work without resolving the upstream authorization seam. |
| Re-scope or create a new epic | Not viable | Duplicates FR24 and REL-AI-1 ownership; the needed work already belongs to GOV-1 and REL-5. |
| Continue with variable `true` and rely on package scripts | Rejected | Opens a publish-capable workflow before the required post-evidence owner authorization exists. |

### Effort and sequencing

1. **Operational containment — Release Owner, immediate after approval**
   - Set `HEXALITH_RELEASE_PUBLISH_ENABLED` to absent or a value other than the
     exact lowercase string `true`.
   - Capture timestamped, non-secret evidence of the before/after state.
   - Inspect release runs and all configured publication surfaces for the window
     during which the value was `true`; record any partial side effects.
2. **Truth-state repair — FrontComposer maintainers, current sprint**
   - Apply the sprint-status, REL-3, REL-4, REL-5, and ledger edits in Section 4.
   - Review and accept REL-3 and REL-4 independently; do not equate code review
     completion with a qualifying production release.
3. **Governed seam — GOV-1 / upstream Builds owner**
   - Complete the local dependency/provenance contract.
   - Obtain a reopened or successor accepted upstream revision.
   - Provide the prepublication-to-protected-authorization seam required by
     FR24, or obtain explicit approval for an equivalent fail-closed
     contingency.
4. **Operational release — Release Owner through REL-5**
   - Configure the approved production signing identity, scoped secrets, and
     timestamp authority.
   - Execute one bounded candidate through the complete chain.
   - Grant publication authorization only after the sealed candidate evidence is
     ready and while exact-byte binding remains intact.
   - Reset the coarse execution switch after the bounded attempt.
5. **Verification and reconciliation — Release Owner with security/project review**
   - Verify published bytes, consumer installability, symbols, signatures,
     timestamps, SBOM, checksums, inventory, and provenance.
   - Attach durable evidence to the GitHub Release.
   - Reconcile all affected historical releases through the new release.
   - Close REL-AI-1 only when the first real qualifying release and historical
     ledger meet the closure rule.

### Rollback plan

- Documentation/status edits can be reverted through normal review if they are
  factually disproved.
- The containment state is fail-closed: if the governed seam or candidate fails,
  keep the variable non-`true` and do not publish.
- Published package bytes are not treated as safely reversible. If a partial or
  invalid publication is found, freeze subsequent publication, preserve the
  evidence, classify the release non-compliant, and execute the existing REL-3
  technical correction path rather than deleting history.

## 4. Detailed Change Proposals

The following edits were approved by Administrator and applied on 2026-08-03.

### 4.1 Sprint status: REL-AI-1 control and progress

**File:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**OLD**

```yaml
release_control: "administratively frozen; technical enforcement remains pending until REL-4 is implemented and verified..."
progress:
  - "2026-07-16: ... REL-4 is ready-for-dev ..."
```

**NEW**

```yaml
release_control: >-
  Publication is unauthorized. REL-4's coarse technical guard is implemented
  and has CI-authoritative frozen-run evidence, but the live repository variable
  was observed as exactly true on 2026-08-03, enabling publish-capable Release
  attempts before a qualifying post-evidence authorization seam exists. The
  Release Owner must restore the value to absent or non-true and keep it there
  until GOV-1/upstream, production signing, sealed readiness, protected owner
  authorization, and exact-byte publication controls are approved for one
  bounded candidate.
progress:
  - "2026-08-03: REL-4's guard and REL-3's local evidence implementation exist and remain under review; frozen run 29703682203 proves the default skip path."
  - "2026-08-03: The repository switch was observed as true; run 30760188983 entered the reusable release job and failed at Semantic Release. No newer GitHub Release than v4.0.1 was observed; all external publication surfaces still require reconciliation."
  - "2026-08-03: The switch precedes evidence generation and is only coarse workflow enablement, not FR24 publication authorization. Keep it non-true until a candidate-phase/post-classification protected authorization seam is available."
  - "2026-08-03: Upstream issue 17 is closed without a qualifying revision. Local GOV-1 work may proceed, but integration, end-to-end completion, eligibility, and unfreeze remain blocked on a reopened or successor accepted revision."
  - "2026-08-03: Historical reconciliation must include v4.0.0 and v4.0.1; both lack durable evidence assets and sampled package verification reports NU3004 unsigned."
```

Retain:

- owner `Release Owner`;
- status `open`;
- implementation chain `REL-4 -> REL-3 -> GOV-1 -> REL-5` in substance; and
- the existing closure rule requiring one real qualifying release and complete
  historical reconciliation.

### 4.2 REL-4: record implemented freeze evidence

**File:**
`_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md`

**OLD**

```markdown
- **Pending (CI-authoritative, post-merge):** first frozen Release run URL showing
  `freeze-guard` success, `release` skip, and no publication side effect — record
  here on completion (AC6).
```

**NEW**

```markdown
- First CI-authoritative frozen Release run:
  https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29703682203
  (`freeze-guard` succeeded; reusable `release` job skipped).
- This proves coarse fail-closed enforcement only. It does not prove FR24
  readiness or authorize publication.
```

Keep story status `in-review` until review accepts the evidence and
implementation.

### 4.3 REL-3: align truth state

**File:**
`_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`

**OLD**

```yaml
status: done
```

**NEW**

```yaml
status: in-review
```

Also add a concise status note:

```markdown
Repository-local implementation and non-publishing validation are complete and
under review. A real signed/timestamped governed release, upstream/protected
authorization seam, production credentials, durable GitHub Release evidence,
published-byte verification, and historical closure remain transferred REL-5 /
GOV-1 conditions and are not implied by this story's implementation status.
```

### 4.4 REL-5: repair status and authorization semantics

**File:**
`_bmad-output/implementation-artifacts/rel-5-provision-signing-identity-and-first-governed-release.md`

**Status change**

```diff
- Status: ready-for-dev
+ Status: in-progress
```

**Replace the current AC6 interpretation** that says to set the repository
variable only after prepublication evidence is ready. The current guard runs
before that evidence can be produced, making this sequence circular.

**NEW acceptance wording**

```markdown
6. The Release Owner treats `HEXALITH_RELEASE_PUBLISH_ENABLED` as coarse
   workflow-execution enablement only, never as sufficient FR24 publication
   authorization. While no approved governed candidate mechanism is active, the
   variable is absent or not exactly `true`. A qualifying mechanism must create
   and seal the exact candidate evidence before a protected owner decision and
   must preserve byte identity from that decision through publication.

10. Every authorized release attempt is bounded and auditable: the owner records
    the candidate identity, enablement time, sealed-ready evidence, protected
    approval, run URL, publication result, verification result, and switch reset.
    Any failed, cancelled, invalid, or unauthorized path remains fail-closed and
    triggers inspection for partial external side effects.
```

**Add immediate task**

```markdown
- [ ] Restore `HEXALITH_RELEASE_PUBLISH_ENABLED` to absent/non-`true`; record
      before/after evidence and reconcile every publish-capable run during the
      enabled window across GitHub Releases and configured package registries.
```

Update the upstream dependency text to state that issue 17 is closed without a
qualifying revision and that a reopened or successor accepted revision is
required for integration, end-to-end completion, release eligibility, and
unfreeze.

### 4.5 Historical release ledger: add omitted releases

**File:**
`_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`

Add `v4.0.0` and `v4.0.1` to the ledger and evidence sections.

| Release | Observed evidence | Classification |
|---|---|---|
| `v4.0.0` | GitHub Release contains eight `.nupkg` and eight `.snupkg` assets, no durable FR24 evidence assets; sampled Contracts package returns `NU3004` unsigned | Non-compliant; affected pre-REL-4 release; remaining registry/readiness reconciliation open |
| `v4.0.1` | GitHub Release contains eight `.nupkg` and eight `.snupkg` assets, no durable FR24 evidence assets; sampled Contracts package returns `NU3004` unsigned | Non-compliant; affected pre-REL-4 release; remaining registry/readiness reconciliation open |

Evidence URLs:

- <https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.0.0>
- <https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.0.1>

Do not relabel these releases compliant if later replacement evidence is
created. Preserve their historical classification and link any correction or
superseding release.

### 4.6 Product and architecture artifacts

No text changes are proposed to the PRD, architecture, or UX artifacts. Their
current contracts already express the required behavior. Changing them would
weaken or duplicate the governing requirements instead of correcting execution.

No duplicate REL-AI-1 action or release epic is proposed.

## 5. REL-AI-1 Closure Gate

REL-AI-1 may close only when all of the following are true for at least one real
release candidate and its publication:

- the package inventory is explicit and complete;
- every `.nupkg` and `.snupkg` is signed by the approved production identity and
  carries an acceptable timestamp;
- SBOM, checksums, consumer-validation results, dependency graph, policy, and
  workflow provenance are complete;
- the manifest is sealed as `hexalith.release-evidence.v2`, classified `ready`,
  and says `publish_authorized=true`;
- protected Release Owner authorization occurs after the sealed evidence exists
  and before any publication side effect;
- only manifest-bound bytes are published;
- independently resolved published bytes match the sealed checksums;
- the complete durable evidence set is attached to the GitHub Release;
- the coarse execution switch is reset after the bounded attempt;
- all affected historical releases, including `v4.0.0` and `v4.0.1`, are
  reconciled with honest compliant/non-compliant classifications; and
- Security Reviewer and Project Lead accept the evidence required by the
  existing closure rule.

Any invalid, incomplete, unauthorized, failed, or cancelled path remains
fail-closed before publication. Discovery of a partial side effect freezes
further publication and invokes the REL-3 correction path.

## 6. Correct Course Checklist Result

| Checklist area | Result | Notes |
|---|---|---|
| Trigger and context | Done | Same 2026-07-16 trigger; current live/control/artifact drift established. |
| Epic impact | Done | Existing release-governance chain remains viable; no new epic. |
| Artifact conflicts | Action needed | Sprint status, REL-3, REL-4, REL-5, and ledger require the edits above. |
| PRD/architecture/UX | No change | Existing requirements and design remain correct. |
| Path evaluation | Done | Direct adjustment selected; rollback and duplicate scope rejected. |
| Scope classification | Done | Moderate scope, High implementation risk. |
| Publication decision | Done | Unauthorized; restore and retain fail-closed state. |
| Approval | Done | Administrator approved the proposal on 2026-08-03. Publication remains unauthorized. |
| Implementation handoff | Ready | Release Owner leads; FrontComposer, GOV-1/upstream, Security Reviewer, and Project Lead support. |

## 7. Approval and Handoff

Administrator approved the artifact edits in Section 4 and the handoff of the
live containment action to the Release Owner on 2026-08-03. It does **not**
authorize publication, expose or rotate secrets, accept an upstream revision,
or close REL-AI-1.

Applied/handoff state:

1. the listed artifact edits are applied and validated;
2. the immediate fail-closed variable reset and enabled-window audit are handed to the
   Release Owner;
3. preserve GOV-1's upstream gate and complete local work;
4. keep REL-AI-1 open until Section 5 is proven by a real release; and
5. update sprint status with durable evidence links as each gate completes.
