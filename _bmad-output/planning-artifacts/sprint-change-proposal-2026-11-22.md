---
title: 'Dependency restoration and reproducible release evidence'
date: '2026-11-22'
mode: batch
change_scope: moderate
status: 'approved and applied to planning'
---

# Dependency restoration and reproducible release evidence

## Approved planning decision

Keep Stories 16.3 and 17.10 in their existing epics. Record the failed dependency restoration as an open owner decision. Give release-evidence producers and reviewers a field-by-field rule for the bytes they hash. The Product Owner can schedule the documentation and verification work together. Story 17.2, the release incident runbook, can proceed through its existing review independently.

## 1. What changed on main

FrontComposer restored seven pinned dependencies at `a79cc810`. Its successor `8199bcf7` updated migration guidance and kept those pins. The local checkouts of all ten references were refreshed from their own main branches. The results below use the revisions **committed by FrontComposer**. [Restoration](https://github.com/Hexalith/Hexalith.FrontComposer/commit/a79cc810fafbebcb392a413d24b99ffa70326d58) · [Documentation successor](https://github.com/Hexalith/Hexalith.FrontComposer/commit/8199bcf7def7d9ddb39b1704429ba611d59fc08e)

| Local graph check | Observed result |
| --- | --- |
| Validate parent `4cecae4e` | Exit 1. Platform `0f6468ed` fails the owner-pin check. |
| Validate `a79cc810` and `8199bcf7` separately | Each passes with 76 dependency edges. |
| Compare `4cecae4e` to `a79cc810` under push and PR rules | Both exit 1 with `consumer source projection owner pin changed`. The check stops before producing affected-module evidence. |
| Compare `a79cc810` to `8199bcf7` under push and PR rules | Both pass with zero dependency changes and zero affected modules. The push event records this exact pair. |

The restore commit records owner authorization for local preparation. Its hosted CI attempt failed at “Collect exact graph diff and affected-module proof.” Quality also failed during catalog preparation. At 12:06 UTC on November 22, the successor's CI was running and Quality was queued. The successful local successor comparison covers the documentation change alone. [Restoration CI](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/45018052464/attempts/1) · [Restoration Quality](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/45018052234/attempts/1) · [Successor CI](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/45020254529/attempts/1) · [Successor Quality](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/45020254335/attempts/1)

## 2. Approved edits and acceptance

### Story 16.3 and the dependency prerequisite for Story 17.3

**Current wording.** The November 22 note in Story 16.3 describes a main revision that fails its own graph check. Story 17.3 and the Epic 17 context point to the earlier Platform mismatch. Those entries describe dated observations. [Story 16.3](../implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md#current-main-and-the-stopped-preflight--2026-11-22) · [Epic 17 dependencies](../implementation-artifacts/epic-17-context.md#cross-story-dependencies)

**Approved addition.** “The restored selection and its documentation successor each validate with 76 edges. The restoration fails when compared with its drifted parent. The Architect and Release Owner will record the disposition of that failed change, the actual event base and candidate, and the affected-module evidence required under the approved recovery rules. Until they can establish a qualifying path, Story 16.3 stays in progress and G-3 stays open.”

Append the dated finding to the owning spec. Reference it from the Story 17.3 prerequisite and the Epic 17 context. The upstream Builds owner's acceptance for 17.3 remains a separate dependency. The November 17 preflight ended before qualification. A future candidate needs its own authorized window, exact runtime evidence and named owner decisions. [November 21 recovery decision](sprint-change-proposal-2026-11-21.md#1-admit-a-platform-revision-change) · [G-3 requirements](prd.md#approved-g-3-candidate-identity-and-record-storage--2026-11-16)

**Acceptance.** Reproduce all four rows above from committed objects. For a proposed recovery, identify the real event coordinates, run the ordinary base/candidate check and retain its affected-module proof. If the original transition cannot meet the rule, record that failure and the owners' disposition. Treat a later candidate as a separate qualification. Story 16.3 can close only under its existing migration criteria.

### Story 17.10 and the GOV-1 architecture

**Current wording.** AD-5 and the consistency table say every digest uses canonical JSON. AD-6 hashes raw catalog bytes, AD-12 records the raw policy hash, and G-8 authenticates original CI artifact bytes. A verifier following the general rule can reject the exact file the producer used. [GOV-1 architecture](architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md) · [Finding CF-01](architecture/architecture-gov-1-2026-07-19/reviews/review-current-fit-validate-2026-11-21.md)

**Approved replacement.** “Each digest field identifies its input. For a committed file, read the Git blob at the named commit and path. For a CI archive or member, hash the original downloaded archive or extracted member bytes, respectively. Apply AD-5 canonical JSON to a structured value only where its schema defines the projection and member ordering.” Amend AD-5 and the consistency table together. Add the following cases to Story 17.10's first acceptance criterion. [Story definitions](epics.md) · [Graph collector and policy projection](../../eng/dependency_graph.py)

| Field or record | Input to SHA-256 |
| --- | --- |
| Policy, catalog, workflow and spine file identities | Exact committed blob bytes at the specified repository, revision and path |
| CI archive, packet, output, report or sidecar identified as a file | Exact bytes of the named archive or member from the authenticated run and attempt |
| Graph digest | AD-5 canonical JSON for the prescribed schema, root, edge count and ordered edges |
| Manifest seal | AD-5 canonical JSON of the defined manifest members with the seal omitted |

**Acceptance.** The policy blob at `8199bcf7` hashes to `ca0b7b7210c592d8431f8042ab350cd360ff255ccada6181804cffb3cdf5d43f`. Its parsed value, serialized with the repository's AD-5 routine, hashes to `7a8cadff9a47a35c845fe7e65d3acd0f21d321dd5629f3e35ed565d656bdb023`. Reject the second digest in the policy-file field. Verify that whitespace-only reformatting changes the file identity while the canonical digest of the same parsed value stays stable. Verify the graph and seal against their declared projections. Preserve the two protected reviews and original-byte retrieval rules. [G-8 retention decision](sprint-change-proposal-2026-11-21.md#2-decide-where-story-1710-keeps-conformance-evidence)

**Story 17.12 follow-through.** Three of the nine November 21 source hashes already differ at the reviewed head. They cover the PRD, architecture summary and GOV-1 spine. After editing, append a dated register calculated from all nine reviewed Git blobs. The Product Owner and Release Owner review those exact identities under the existing GOV-ACCEPT-2 decision. The historical register remains attributable to its original revision. [Source register](../implementation-artifacts/spec-17-1-reconcile-publication-governance-sources.md#source-digests-for-the-2026-11-21-sprint-planning-decision)

## 3. Impact, alternatives and handoff

The existing PRD covers candidate identity, release evidence and retention. These edits affect how maintainers qualify dependency changes and how reviewers reproduce evidence. They require no change to the MVP, user flows, UX designs or deployment topology. The sprint dry run found 17 epics and 140 stories with all 174 tracker entries preserved. Story 17.10 stays in backlog and G-8 stays open.

| Approach | Effort and risk | Consequence |
| --- | --- | --- |
| Adjust the existing stories and architecture | Low drafting effort, moderate verification effort. Release risk is high if a failing comparison or the wrong digest is accepted. | Recommended. The evidence-contract work and Story 17.2 review can proceed while the owners resolve the restoration. |
| Roll back completed work | Higher operational risk | Another pin reversal creates another dependency transition and leaves the digest conflict outstanding. |
| Reduce MVP scope | High product impact | Removing migration or publication evidence would change the approved readiness criteria. |

The story references, digest wording and acceptance examples are in place. The **Architect and Release Owner** own the dependency disposition. The **Developer** implements the Story 17.10 checks when its dependencies are ready. The **Product Owner and Release Owner** review the refreshed source identities under Story 17.12. Scheduling qualification depends on the recovery evidence and owner availability.

**Checklist disposition.** Trigger, epic impact, artifact impact, options and handoff have been assessed. UI changes and tracker renumbering are inapplicable. The operator approved the planning edits on November 22. The recovery disposition and subsequent evidence reviews are outstanding.

## Approval and verification

On November 22 the operator answered **“approve”** to the combined planning proposal. The disposition of the failed pin restoration remains with the Architect and Release Owner. G-3 and G-8 remain open.

The graph checks reproduced all four results above. The verifier accepted the original policy hash and rejected the canonical JSON digest in its place. A whitespace-only edit changed the file hash and preserved the canonical digest. The graph and manifest digests reproduced from their prescribed projections. All nine source SHA-256 values and Git blob IDs reproduced independently. The architecture lint reported zero findings, story-artifact validation passed, and the BMAD dry run preserved all 174 tracker entries across 17 epics and 140 stories. [Story 16.3 recovery record](../implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md#dependency-restoration-on-november-22) · [GOV-1 digest rules](architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md#ad-5--adopted-v1-canonical-material-and-digest) · [Nine-source register](../implementation-artifacts/spec-17-1-reconcile-publication-governance-sources.md#source-digests-for-the-2026-11-22-course-correction)
