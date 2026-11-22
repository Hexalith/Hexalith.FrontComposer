---
title: 'Epic 17 readiness and sprint tracker correction'
date: '2026-11-21'
mode: batch
change_scope: moderate
status: 'approved for implementation'
---

# Epic 17 readiness and sprint tracker correction

## What triggered the change

Sprint planning on FrontComposer `a9e755da2c060f4bf49e0cc2df094bd8a4940df6` exposed two decisions an implementer cannot make from the current instructions. AD-12 requires one prior policy to validate both sides of a dependency change. AD-12a seals one Platform revision. Story 17.10 requires the release evidence to be independently verifiable, but the GOV-1 spine and PRD §7 give different instructions for storing its raw files. The November 21 architecture review identified these as RW-01 and RW-02. [Architecture review](architecture/architecture-gov-1-2026-07-19/validation-report-2026-11-21.md)

The stock sprint generator also counted the two prospective Epic 16 criteria as new stories and treated two completed stories as new backlog entries. Those errors can be corrected from the numbered story definitions and the recorded decisions. [Epic definitions](epics.md) · [Sprint tracker](../implementation-artifacts/sprint-status.yaml)

## 1. Admit a Platform revision change

**Current evidence.** The committed policy seals Platform `9c39cd58` and Builds `6548f248`. The reviewed FrontComposer commit selects Platform `550b896d` and Builds `055d7df5`. The working checkouts were `0aacf132` and `d532b1e9`, respectively. Running the command below against the committed tree exited 1 with `consumer source projection owner pin changed`. The same check on historical commit `aa6f9e87baa60fa479685b9aa62726694587cf9a` passed against its own policy. That provides a reproducible starting fixture. [Dependency policy](../../eng/dependency-graph-policy.json) · [Graph validator](../../eng/dependency_graph.py)

```sh
python3 eng/dependency_graph.py validate --commit a9e755da2c060f4bf49e0cc2df094bd8a4940df6
```

**Adopted change to AD-12/12a and the prerequisite for Story 17.3.** Have the Architect and Release Owner review one directional transition between two complete source projections. First deploy support for the transition with the original selection still active. Then review a policy-only change that records the old and new projections. Its own check must pass under the previous policy. Only a subsequent selector change may consume that authorization. Authenticate the base against the old projection and the candidate against the new one. Bind both Platform and Builds commits, inventory, both source files, compile links, direct selectors, consumer configuration and semantic expectations as a unit. Derive each value from the committed tree. After the move, retire the transition with a policy-only change at the new selection. Ordinary changes at either end must continue to validate while the authorization is staged or retired.

**Implementation acceptance.** Exercise the normal PR and push comparisons from a matching base. Verify the policy-only steps, the authorized forward move and a later unchanged graph. Reject self-authorization, a third revision, reverse movement and mixing fields from different projections. Apply the same selection in acquisition, collection, semantic checks, affected builds and handoff verification. Each endpoint must independently satisfy catalog compatibility. A mismatched base still fails. [Current projection checks](../../tests/eng/test_dependency_graph.py)

**Current-main recovery.** The pulled commit already fails its own check. Before selecting it or the six newer working gitlinks, the Architect and Release Owner must record how to recover from that specific base. The historical passing commit can serve as a fixture for developing the change. It cannot substitute for the real event base. Acceptance on main needs an actual passing base/candidate comparison and affected-module evidence. The upstream Builds owner acceptance for Story 17.3 is a separate prerequisite. [Epic 17 dependencies](../implementation-artifacts/epic-17-context.md#cross-story-dependencies)

## 2. Decide where Story 17.10 keeps conformance evidence

**Conflict identified before adoption.** PRD §7 placed raw results in local scratch or CI and keeps a concise record in the owning story or spec. The GOV-1 gate required a protected PR containing the packet, raw outputs, five reviewer reports and sidecars, followed by a separate approval-projection PR. A run link alone cannot recover expired bytes. [PRD §7](prd.md#7-constraints-and-dependencies) · [GOV-1 spine](architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md)

**Selected G-8 retention policy.** Apply the October 23 storage rule to G-8. CI retains the packet and raw members for the approval window. The first protected PR records the candidate, policy, run and attempt, artifact/member identities and byte hashes in the owning spec. Its required check retrieves the exact bytes and verifies them against the claimed run/job and the five reviews. Bind the independent Release Owner approval to that unchanged head and packet digest. The second protected PR records the authenticated review coordinates and rechecks the required sources. Preserve both approvals' provenance, existing merge protections and all candidate, publisher and incident checks. [Owner's evidence-storage decision](../implementation-artifacts/spec-evidence-storage-simplification.md)

**Retention consequence.** After CI expiry, the merged spec and review still identify what was accepted. Raw bytes can only be replayed while retrievable. An unavailable or expired required artifact fails a new verification. Requalification requires a complete, newly identified evidence set for the applicable candidate and fresh reviews. Mixing attempts or substituting a local copy is insufficient.

**Alternative considered and set aside.** If byte-for-byte replay after CI expiry is required, approve a narrowly scoped G-8 archive exception. Identify the protected location, files, retention period, access and redaction rules, size limits, and the custodian. Demonstrate retrieval by immutable identity and rehash the required members after the CI copy expires. Keep the current missing-source refusal wherever live authentication is required. An unselected exception supplies no authority to store raw evidence in Git.

The adoption updates PRD §7, the architecture summary, the spine's G-8 packet and protected-PR clauses, the GOV-1 spec, and Story 17.10 in this change. The nine governance source digests are recorded for the existing GOV-ACCEPT-2 review. [Source digests](../implementation-artifacts/spec-17-1-reconcile-publication-governance-sources.md#source-digests-for-the-2026-11-21-sprint-planning-decision) Verify a valid two-PR sequence and rejection of expired inputs, changed bytes, mixed attempts, a changed head, and a missing independent approval. Reassess the remaining November 21 architecture findings before claiming conformance.

## 3. Repair tracker generation

| Existing input | Correction | Verification |
| --- | --- | --- |
| Two amendment headings look like new Stories 16.1 and 16.3 | Retitle the subsections while retaining their approved criteria | One row per existing Epic 16 story |
| Story 13.8 ends in `prrevokwg` and the Story 15.6 key exceeds the generator's title limit | Use the two keys derived from the canonical story titles and preserve both `done` values | Stock generation reports zero new or dropped rows |
| The tracker does not explain the two November 21 prerequisites | Record them on the existing Epic 17 backlog rows with this proposal as the decision reference | 17 epics, 140 stories, identical lifecycle counts and action items |

Story 13.8 has a recorded Product decision. Story 15.6 closed when the October 23 storage policy superseded its original design. The Epic 16 criteria still govern future work on the original stories. [Story 13.8](../implementation-artifacts/spec-13-8-accept-the-completed-fresh-row-live-proof.md) · [Epic 16 and Story 15.6](epics.md)

The generator dry run reported `in_sync: true`, with zero new, dropped or changed entries. A full regeneration on a temporary copy reproduced all 174 epic, story and retrospective states. Of the 140 stories, 124 are done, one is in progress, one is in review and 14 are in the backlog. The live tracker passed BMAD validation and `python3 eng/validate-story-artifacts.py --project-root .`. The tracked action items and approved-change backlog match their previous contents.

## Handoff and sequencing

Use the existing Epic 17 stories. Direct adjustment preserves delivered scope. Rolling back the submodules would hide the current mismatch while leaving the next transition undefined. Cutting MVP scope would leave release owners with conflicting acceptance criteria. The UX review already supplies acceptance checks for the three Counter findings. They concern the product implementation and follow their existing UX handoff. [UX recheck](ux-designs/ux-frontcomposer-2026-09-09/review-after-update-2026-11-21/validation-report.md)

The tracker repair is small and independently verifiable. The graph change has moderate implementation effort and high release impact. Start with fixtures while Architecture and Release select the recovery for the drifted base. Schedule Story 17.10 conformance after the selected storage contract, its validator, upstream dependencies and required reviews are ready. Story 16.3 continues under its existing G-3 terms. G-8 remains open.

**Planning decision, 2026-11-21.** The repository operator answered “apply recommended” after reviewing the two recommendations and the archive alternative. This selects the directional prior-policy transition and CI retention with requalification after expiry. Selecting a live recovery target for the drifted base, qualifying the release candidate and approving publication each require their own evidence and decision.

## Application and verification

The graph checker accepts an optional reviewed transition. The production policy contains no active transition. The temporary Git repository used in the regression covers staging, a forward move under PR and push events, retirement, live handoff verification and materialization of the reviewed source. It also exercises self-authorization, reverse movement, a third pin and mixed selectors.

| Local check | Result |
| --- | --- |
| Consumer projection regressions | 35 passed |
| CI handoff and release engineering suites | 24 handoff and 83 release tests passed |
| Complete dependency graph suite | Four catalog-related test methods reproduced three assertion failures and four errors, including subtests, with the original checker and current Builds checkout. The same four passed against the historical sealed Builds revision `6548f248`. |
| Committed graph verification | `a9e755da` still refuses Platform `550b896d`. Historical `aa6f9e87` validates 76 edges. |
| Architecture and sprint planning | Spine lint returned zero findings. Story-artifact validation passed. The tracker stayed in sync at 17 epics and 140 stories. |

Story 17.10 now specifies CI retention, original-byte checks at both protected PRs and fresh qualification after expiry. The planned G-8 validator and protected PR checks still require implementation and hosted verification. G-8 remains open. The local environment has no Aspire CLI (`aspire run` returned `command not found`).
