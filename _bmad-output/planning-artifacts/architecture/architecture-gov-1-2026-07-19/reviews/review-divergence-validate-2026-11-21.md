# GOV-1 architecture validation — independent implementation divergence

**Verdict — FAIL.** Four High findings affect handoff verification, the frozen qualification run or incident classification. One Medium finding affects the graph differ. Each pairs rules that independent implementations can follow to different results.

Reviewed on 2026-11-21 against the complete working-tree [architecture spine](../ARCHITECTURE-SPINE.md), lines 1–1270. Raw file SHA-256 is `d38994cceeb5cd9e0a0682ddb3b6ff642a53b3cbfe1c13587b7f91185dfeab07` including CRLF. Repository baseline is `61dcd07fb412acf2feb66d8ee61513c16ef6ec4d`. Findings apply to the architecture target. This desk review supplies no live run or G-8 conformance receipt.

## DV-01 · High · The CI handoff has two possible caller projections

**Rules.** AD-12 lines 411–422 defines the policy caller as exactly `{repository, workflow_path, blob_sha256}` so a workflow can be approved in advance. AD-13 lines 550–562 reuses “the AD-12 source shapes” for the run handoff and hashes its caller. The checked-in CI reader requires an additional `caller.commit`, hashes it, and requires it to equal the candidate. Its authorization projection removes the commit before comparing the policy row. [Handoff reader, lines 53–85, 89–105 and 174–187](../../../../../eng/dependency_handoff.py)

**Divergence.** A producer following the literal AD-13 cross-reference emits three caller members. A verifier following the running handoff contract rejects that evidence. Adding the commit also changes the evaluator digest. The policy has a legitimate need for an advance authorization coordinate. The evidence has a legitimate need for the observed runtime coordinate.

**Action.** Specify the closed policy and runtime shapes separately. Include the authenticated candidate commit in the runtime caller, define the projection used for row matching, and give a golden digest for each. For the Release variant, show the reviewed publisher commit used for policy matching and the actual candidate commit used for evidence when the two commits contain identical workflow bytes (lines 101–109, 421–429 and 851–857).

## DV-02 · High · A skipped publisher cannot supply the required approval history

**Rules.** AD-15 lines 676–679 and 714–719, and the qualification packet at lines 1223–1229, require a skipped publisher, no external effects and a successful `gate-frozen` verification. AD-18 lines 896–911 makes `actors.environment.approvers` a nonempty list containing an authorized Release Owner. AD-15 lines 700–712 requires that actor projection in the ledger and allows unavailable objects to be null.

**Divergence.** A correctly frozen attempt can have no production deployment review because the protected job was skipped. A producer can preserve the dispatch actors and report an empty approval set, or judge the approval object unavailable. A strict verifier can reject either representation under AD-18. The run can meet the frozen evidence conditions and still fail the packet.

**Action.** Give the frozen and no-releasable dispositions an explicit actor/environment shape for a skipped deployment. Specify the representation of observed zero approvals and unavailable API data separately. Retain the required authenticated approval for a publisher that enters production. Pin both paths to expected ledger bytes.

## DV-03 · High · The packet attributes Release provenance to every handoff

**Rules.** The conformance packet includes a CI handoff and a gate-frozen Release handoff at lines 1218–1228. Its equality map at lines 1233–1238 says every handoff and checked output names the policy and its selected Release evaluator names the packet's Builds revision and caller hash. The closed push-CI handoff at lines 550–562 can carry one CI evaluator. AD-17 lines 851–857 binds that CI evidence separately from the Release coordinates. CI and Release may select different Builds revisions.

**Divergence.** One validator can apply the Release equality only to evidence that owns a Release evaluator. Another can enforce the universal equality on the CI artifact and reject a legitimate run because its caller is `ci.yml` and its Builds pin is the CI workflow revision. Adding a second evaluator to the CI artifact violates its closed schema.

**Action.** State the expected field path and authority for each artifact kind. Authenticate the CI evaluator against its CI row and bind the exact CI member hash. Authenticate the Release evaluator against its Release row and compare the selected Release actions and caller to the packet. Specify which checked outputs carry each projection. Include a passing packet whose CI and Release pins differ.

## DV-04 · High · A recorded start with zero external bytes can change incident kind

**Rules.** AD-15 lines 670–682 sets the marker immediately before the first mutation. The table at lines 714–723 includes a started, incomplete publication in `partial-publish-incident`. The stated precedence at lines 725–735 gives priority to external publication evidence, then required-artifact failure, denial and other contract failures. The recovery rule at lines 783–790 explicitly includes an absent Release after a recorded start.

**Divergence.** A publisher sets its marker and its first draft-creation request fails without creating any external object. The table yields `partial-publish-incident` directly. A verifier using the ordered mapping can reach `non-compliant` for the failed publication or `missing-artifact` if required evidence is also unavailable. This changes the permanent incident kind for the same Release attempt.

**Action.** Include the authenticated marker independently of observed external effects in the precedence rule. Specify the result for a started, incomplete attempt with zero external bytes, including the case with missing evidence. Keep the prepublication-denial path restricted to attempts that never started publication.

## DV-05 · Medium · A supported self-reference can collide in the diff

**Rules.** AD-3 lines 157–168 rejects duplicates within one committed mapping. AD-4 lines 172–179 retains every self/back-reference and identifies physical edges by owner, owner commit and path. AD-8 lines 275–283 compares by `(owner_repository, path, repository)`, ignores the owner commit for unchanged depth-one edges, and compares every member for depth two.

**Divergence.** A root can select a previous commit of its own repository whose tree contains the same selector path and target identity. This yields a depth-one and a depth-two edge with different owner commits. Each mapping has one entry and both physical edges have valid distinct identities. The two edges share one logical diff key. An implementation indexing by that key can retain one edge while a multimap-based differ can compare both and schedule different work.

**Action.** Include depth in the logical diff key or specify equivalent matching that preserves both edges. Keep the depth-one owner-commit exception. Pin a previous-root self-selector with a change to only one depth and assert the affected owner.

## Scope decisions

The parent and spine explicitly distinguish the FrontComposer signer commit, Builds action revision and catalog gitlink. They also allow identical publisher blobs to be authorized before the candidate exists. These are intentional roles. The dated CiCd depth-two omission is identified as implementation drift at spine lines 133–142. Current v3 manifest/v1 handoff behavior is separately identified at lines 590–607. None of these observations establishes a design conflict or release eligibility. [Parent architecture, lines 286–321](../../../architecture.md)
