---
created: 2026-07-19
updated: 2026-11-14
story: GOV-1
owner: Product Owner + Architect + Developer + Release Owner
source_proposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19.md
course_correction: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md
decision: _bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md
architecture_spine: _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
status: in-progress
architecture_status: final
product_release_acceptance: pending
implementation_conformance: open
release_eligibility: blocked
priority: before the next governed production release
baseline_commit: 3786330d241c2d87449fa3e01afc95fc832552df
upstream_catalog_follow_up: BUILD-CAT-1
upstream_release_follow_up: BUILD-REL-1 split-publication successor in the AD-16 lineage
implementation_gate: GOV-1 split implementation gate
---

# GOV-1: Validate Shared-Catalog Compatibility and Seal Dependency Provenance

## Story

As a framework maintainer and Release Owner,
I want semantic shared-catalog compatibility and exact dependency provenance carried through a
privilege-separated release,
so that pointer advances remain reviewable and reproducible while candidate-controlled code cannot
obtain publication authority.

## Why This Story Exists

Historical Governance conflated compatibility with selected commit identity. GOV-1 separates semantic
compatibility from deterministic provenance, then binds the exact CI-tested candidate and its graph to
one release evidence chain. The finalized 2026-09-09 spine additionally closes the publication
privilege gap: candidate construction occurs in a secretless builder, while only pinned candidate-free
owner code in a protected publisher may attest, seal manifest v4, classify, and publish.

The current implementation is not conforming merely because earlier graph, handoff, or manifest work
exists. The checked-in caller still selects a legacy publication-capable path, the target contracts have
advanced to handoff v3 and manifest v4, and the spine's exact implementation gate has not passed.

## Authority And Current State

- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`
  is authoritative. AD-1 through AD-19 keep their stable IDs; this story cannot redefine them.
- The 2026-07-19 FC-DEP-1 approval remains valid for the original graph/provenance decision.
  Administrator adopted the 2026-09-09 architecture direction, but Product Owner and Release Owner
  acceptance of AD-19 and the halt/no-exception posture remains open.
- Hexalith.Builds revision `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` is the accepted AD-16
  lineage anchor/predecessor. A later owner-accepted immutable revision must implement
  `split-publication-v1` before FrontComposer selects it.
- Production releases remain halted. `HEXALITH_RELEASE_PUBLISH_ENABLED` must be literal `false` as a
  deny-only emergency stop and can never authorize publication. No bounded risk exception exists.

## Acceptance Criteria

1. **Semantic compatibility and bounded provenance.** Given an explicit FrontComposer commit, GOV-1
   collects the complete AD-1 depth-1/depth-2 committed-object graph, validates every selected Builds
   catalog under its exact active-policy semantic profile, and records deterministic graph/catalog
   provenance without a commit or fingerprint allowlist. Compatible pointer advances pass;
   incompatible or unavailable inputs fail with exact owner/edge/catalog diagnostics.

2. **Exact revision and policy authority.** PR and push evidence use AD-8's explicit revisions and
   AD-12's immutable base/before policy. Candidate policy or evaluator changes cannot authorize
   themselves. Every CI, Release, post-release, and incident-recovery evaluator matches one exact
   active-policy row and complete pinned closure; zero/unknown/duplicate/mutable authority fails closed.

3. **Authenticated release selection.** Operator `workflow_dispatch` authenticates the exact live
   `main` SHA against exactly one completed successful push run of both `ci.yml` and `quality.yml`.
   `hexalith.dependency-release-handoff.v1` preserves the sole release candidate, policy, graph, and CI
   evaluator. No tag, ambient checkout, second-hop SHA, later default branch, or diagnostic source proof
   can replace it.

4. **Privilege-separated publication.** The selected immutable Builds reusable exposes exactly one
   `build-publication-candidate` job and one `publish-publication-candidate` job. Candidate-controlled
   code executes only in the secretless/read-only builder and crosses the boundary only through the
   authenticated `hexalith.publication-candidate.v1` archive. Builder output may deny early but cannot
   become a final seal, classification, authorization, or executable publisher input. The protected
   publisher executes only candidate-free pinned owner code and is the sole product-publication actor.

5. **Handoff v3 and manifest v4.** Every authenticated Release run emits
   `hexalith.release-verification-handoff.v3` under `if: always()`. It carries the selected quality run,
   original CI handoff/candidate, policy, publication-candidate coordinates, release state,
   prepublication denial, final `hexalith.release-evidence.v4`, attestation/fallback, exact assets, and
   Release evaluator. V4 seals the graph, policy, selected runs, workflow provenance, and byte-identical
   attestation-or-fallback projection. V1-v3 manifests are audit-only for new publication.

6. **Run-bound fallback authorization.** Fallback is eligible only when the active policy explicitly
   records unsupported attestation capability. The canonical
   `hexalith.attestation-fallback-authorization.v3` matches one Release run/attempt, candidate, CI
   handoff, policy, fallback digest, expiry, and exact protected-environment approval. Retry or any
   candidate/graph/policy/workflow/package-set/run drift requires a new request and approval. Ordinary
   operational failure cannot select fallback.

7. **Attempt truth, durable evidence, and recovery.** The total AD-15 classifier maps every governed
   attempt to exactly one closed disposition. `frontcomposer.release-ledger-record.v2` observations are
   append-only and attempt-keyed; later verification or sign-off cannot erase an incident. Every
   publication-started attempt is checked for partial effects. If the product Release is incomplete or
   absent, the separately authorized candidate-free recovery stage preserves authenticated and
   quarantined evidence in an immutable reserved-namespace prerelease before any retry.

8. **Split implementation gate.** GOV-1 completes only when the spine-defined
   `frontcomposer.gov1-split-conformance.v1` packet and approval projection pass live authentication,
   all eight closure bundles and every required register row are closed, five reviewer lenses report
   PASS with zero Critical/High findings, and both unchanged evidence/approval PRs merge through the
   required no-bypass protected-main process. Stale spine hashes, mixed identities, unavailable source
   evidence, direct pushes, squash/rebase, or an open row fail the gate.

## Implementation Work Remaining

- [x] Finalize and adopt the GOV-1 architecture spine while preserving AD-1 through AD-19.
- [x] Reconcile the parent architecture, FC-DEP-1, this story, the G2 request, PRD projection, and
  nonconformance register with the spine.
- [ ] Record Product Owner and Release Owner acceptance of AD-19, the production-release halt, and the
  current no-exception posture.
- [ ] Set `HEXALITH_RELEASE_PUBLISH_ENABLED` to literal `false` and capture an authenticated API
  observation. This is a Release Owner or repository-administrator action, not part of this document
  update.
- [ ] Obtain the owner-accepted Hexalith.Builds split-reusable revision and activate its exact closure
  through the AD-12/AD-16 two-phase process.
- [ ] Close the eight implementation bundles in the nonconformance register:
  1. split reusable and delayed activation;
  2. caller switch and exact two-job topology;
  3. removal of `production` from candidate/build jobs;
  4. publication-candidate production/authentication and hostile-candidate proof;
  5. handoff-v3, total classifier, and typed-ledger migration;
  6. candidate-free attestation/fallback, final manifest-v4 classification, and publication;
  7. pinned post-release helper execution without ambient/candidate code; and
  8. duplicate destination asset-name rejection.
- [ ] Publish and approve `docs/release-incident-response.md`, including the measurable Product/Release
  acknowledgement and containment target, immutable evidence preservation, credential rotation,
  external-effect inventory, unlisting preference, new-version correction, and re-enable approval.
- [ ] Establish and verify the no-bypass protected-main ruleset required for conformance and append-only
  ledger approvals.
- [ ] Produce the canonical conformance packet, authenticated run/check/reviewer evidence, Release Owner
  approval PR, and distinct approval-projection PR; pass the live validator and close GOV-1.

Existing graph/catalog implementation and historical test evidence may be reused only after it is
revalidated against the finalized spine and included in the gate packet. Prior checkmarks or green runs
do not substitute for current conformance evidence.

## GOV-1 Split Implementation Gate

The exact schema, equality map, validator command, allowed checks, reviewer sidecars, evidence runs,
approval review body, merge requirements, and negative fixtures are defined only in the spine's
Deferred section. The required packet path is:

`_bmad-output/implementation-artifacts/gov-1-split-publication-conformance.json`

The packet PR must add evidence only and leave every implementation, policy, workflow, and prior
evidence byte unchanged relative to its tested `frontcomposer_commit`. The later approval-projection PR
must likewise preserve prior bytes. Release remains ineligible until that gate and every adoption
condition pass.

## Unresolved Owner Decisions

| Decision | Owner | Current disposition | Closure evidence |
| --- | --- | --- | --- |
| Accept AD-19, the production-release halt, and the no-exception posture | Product Owner + Release Owner | Open; architecture adoption does not imply acceptance | Dated decision citing AD-19 and PRD D-16/G-8 |
| Accept the immutable split-reusable successor into AD-16 lineage | Hexalith.Builds owner + Release Owner | Open; `a8a50859…` is predecessor only | Accepted revision and exact closure evidence |
| Apply and verify the literal-false emergency stop | Release Owner or repository administrator | Open; the spine records the last observation as literal `true` | Authenticated API value and server timestamp |
| Approve incident acknowledgement/containment target and runbook | Product Owner + Release Owner | Open | Approved `docs/release-incident-response.md` plus exercise/tabletop evidence |
| Establish the no-bypass protected-main process | Release Owner | Open | Authenticated ruleset/protection evidence |
| Authorize a bounded risk exception | Product Owner + Release Owner + Architect | No exception authorized | Separate dated decision with scope, expiry, evidence, compensating controls, and revocation trigger |

## Non-Goals

- No runtime, public API, generated output, package inventory, dependency-version, or UX change.
- No code, workflow, dependency, submodule, or GitHub-settings change in this specification update.
- No author signing, production PFX, or RFC 3161 author-timestamp requirement. Candidate packages are
  author-unsigned; GitHub provenance attestation or the approved unsupported fallback remains mandatory,
  and NuGet.org repository-signature/normalized-member verification remains binding.
- No same-job candidate build and publication path.
- No graph traversal below depth 2 and no mandatory BUILD-CAT-1 marker without a separate approval.
- No FrontComposer-owned contingency without the separate dated decision required by AD-16.

## References

- Authoritative spine: `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`.
- Parent architecture: `_bmad-output/planning-artifacts/architecture.md`.
- Decision record: `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`.
- Upstream request: `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`.
- Product gates: `_bmad-output/planning-artifacts/prd.md` D-16, G-2, and G-8.
- Implementation register: `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/nonconformance-register-2026-09-08.md`.

Historical implementation narration and validation receipts remain available in repository history and
the dated architecture review artifacts. They are not current conformance claims.


## Story 13.6 bounded source-consumer amendment — 2026-10-28

The user-approved Story 13.6 amendment is implemented through AD-12a in the [GOV-1 spine](../planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md) and the [FC-DEP-1 contract](../contracts/shared-catalog-dependency-governance-2026-07-19.md). Raw v1 inventory/catalog/digest rules and global identity trust remain intact; the closed source-consumer projection records the exact sealed umbrella inventory without acquiring/executing its seven untrusted identities. Only the two approved canonical Compile blobs and existing root Builds tooling participate in consumer materialization; whole-owner scheduling is forbidden.

Collection, object-only consumer validation, affected-proof checks and every live handoff/release consumer share the same active-policy gate. Configuration/root-selector drift or a rehashed forged depth-two envelope fails before build/materialization. The two exact14 semantic expectations are policy-only changes for already-selected81ab. AD-12 delayed activation remains mandatory; no root commit/publication or new runtime was created for this amendment. Current commands/counts/object diagnostics are appended to the [owning Story 13.6 result](13-6-accessibility-result.md); this amendment does not complete GOV-1's independent release/adoption gates.


## Root evidence authentication refinement after parent audit — 2026-10-28

The parent identified a concrete new-primitive boundary: rehashed supplied depth-one rows could omit the Platform selection or disagree with actual root gitlinks/mappings, and root Builds catalog evidence was not independently authenticated. Validation now recomputes supplied root direct rows from actual committed root `.gitmodules`/gitlinks **before any selected-owner/skip path**, and verifies raw root Builds catalog bytes/digest/null marker alongside the existing nested metadata checks. A legitimate authenticated older root without Platform still receives no projection authority. These changes affect governance inspection only; AppHost/service/runtime code and binaries remain unchanged.

The final engine suite passes **128/128**, including structurally valid rehashed omitted-owner, actual-root-target/mapping and root-catalog forgeries through affected-build validation with no materialization/execution, plus a legitimate older-root case. Final consumer reruns pass handoff **11/11** and release-evidence **15/15**. Logs: `/tmp/fc-mtls-final-gad9h52g/governance-full-tests-root-evidence.log`, `governance-handoff-consumers-root-evidence.log`, `governance-release-consumers-root-evidence.log`. The pre-fix attempts/fixture failures remain retained, without relabelling retries as an original clean pass. Real-object component diagnostics are refreshed in `governance-actual-object-diagnostics.json` and `governance-actual-object-diagnostics-root-evidence.log`: full raw 69/all 17 and both root 81ab/nested 51 catalog evidence remain intact; current working seals/14 expectations pass their diagnostic components. Strict committed c494/old-active-base refusals retain their previously documented expected AD-12 limitations; no actual-root commit or candidate self-authorization is created.

The refined identity/refusal guard separately retains Python **73/73** and read-only actual-candidate acceptance. c2tg0uhi remains the earlier runner's successful development runtime proof, not a new-runner full-pass claim. No full runtime was repeated. Guarded copyback, exact hashes and refreshed review patches are external evidence under `/tmp/fc-mtls-final-gad9h52g/`; parent task/matrix audit, independent Build review and independent release/manual gates remain the parent's responsibility.


## Story 13.6 independent Build review follow-through — 2026-10-28

The independent review found that AD-12a's consumer configuration digest omitted tracked SDK/restore inputs. The engine now seals `global.json` and NuGet configuration filename variants; the actual two added files are unchanged, and the sole policy correction is the consumer configuration seal `68386ed899cdef31131e081c321424fa18af8864b8d8efcd6f59eae99a870fca`. The adopted spine, FC-DEP-1 contract and memory log agree. All existing source/owner/inventory/selector seals, actual SDK/package/catalog/Gitlink selections and AD-12 delayed activation remain unchanged.

Parent verification passes engine **130/130**, handoff **11/11** and release-evidence **15/15**. Positive integration now depends on exact canonical materialization before commands; its bypass mutation is detected. Read-only actual-object diagnostics independently confirm the expanded working-candidate seal, 69 total / 17 Platform raw edges and original canonical committed blobs. Logs and reproducible diagnostics are `/tmp/fc-parent-13-6-final-verification/`. The current committed root still predates the approved consumer/policy landing and correctly refuses strict acceptance; later owner landing and a later active base are required. No current hosted CI, release acceptance, commit or publication is claimed.


## Local publication-candidate implementation — 2026-11-07

Added `eng/publication_candidate.py` for deterministic publication-candidate
production and the bounded, data-only archive/descriptor verification boundary
defined by AD-7/AD-19. It checks exact run/attempt, raw CI handoff, policy and
evaluator bindings; safe integers and nonzero commits; every file hash/size;
canonical versions and the independent package/symbol inventory; and duplicate
destination names across candidate and mandatory durable publisher assets.
The existing release-handoff verifier now rejects duplicate asset names even
when their hashes or sizes differ. The caller-owned input/authentication
contract and inactive integration state are documented in
`eng/dependency-governance.md`.

Initial pre-review validation: `python3 -m unittest tests/eng/test_publication_candidate.py
tests/eng/test_dependency_handoff.py tests/eng/test_release_contract.py
tests/eng/test_release_evidence_v2.py tests/eng/test_release_prepublish.py
tests/eng/test_release_disposition.py tests/eng/test_workflow_source_closure.py
tests/eng/test_dependency_graph.py` passed **263/263**. This includes **35** new
publication-candidate tests and the new handoff collision regression. Quality
runs the new module explicitly as a deny-only check. Offline hostile-data and
process-launch-trap fixtures do not substitute for the authenticated live
hostile-candidate credential test or any conformance evidence run.

The story remains in progress. No closure bundle is closed by this local work.
The owner-accepted split reusable, delayed activation, exact caller topology,
handoff-v3/manifest-v4/ledger migration, protected candidate-free attestation
and publication, pinned post-release/recovery actions, live artifact/API
authentication, approved incident response target/runbook, literal-false API
observation, owner acceptance, protected-main process and exact conformance
packet/approval protocol remain required. No policy pin, submodule pointer,
release eligibility, GitHub setting or product publication was changed.


## Review Triage Log

The 2026-11-07 local implementation was reviewed by independent blind, edge-case,
and verification-gap agents. This audit covers the working-tree changes from
`d042e1c97ce2bb66aa30148b03aacf928dbcd9b9`; the original story
`baseline_commit` remains preserved. The complete historical-baseline diff is
retained separately from the local review diff in
`/tmp/frontcomposer-gov1-review-5hwv0w0t/`. This bounded code review does not
constitute the five-lens, authenticated GOV-1 split-conformance gate.

| Finding | Verdict | Route | Verification and disposition |
| --- | --- | --- | --- |
| Blind 1: encoded XML declarations | medium | patch | Parent reproduced a UTF-16 internal DTD resolving an entity into the accepted package ID; reject DTDs before expansion regardless of encoding. |
| Blind 2: nuspec envelope and scalar identity | medium | patch | Parent reproduced acceptance of a non-package root and an ID with ignored child content; require the package root and scalar identity fields. |
| Blind 3: nested package amplification | false | reject | The demonstrated 4 MiB nested payload remains inside the existing release-contract package limits. AD-7's 100:1 and aggregate limits govern the transfer ZIP, which is independently checked before inflation; no breached package ceiling or unbounded path was demonstrated. No new unapproved nested budget is introduced. |
| Blind 4: nested package special modes | medium | patch | The delegated package-content verifier ignores external file modes; reject special/link package members at the new data boundary and cover the caller path. |
| Blind 5: unsupported XML encoding | low | patch | Parent reproduced uncaught LookupError for unknown-codec; normalize it to the CLI's structured failure without accepting the metadata. |
| Blind 6: raw archive FIFO/device | medium | patch | Snapshot opens the supplied path before inspecting its type, so a FIFO can block. Require a nonblocking, no-follow regular-file open before reading. |
| Blind 7: close-time producer failure | medium | patch | The cleanup handler surrounds the copy loop but excludes buffered target close; include close in the handler so only this call's truncated output is removed. |
| Blind 8: late CLI output collision | medium | patch | The exclusive result-file open occurs after create/extract, so an existing result path leaves new artifacts after command failure; reserve/check destinations first and preserve existing files. |
| Blind 9: CLI validation coverage | medium | patch | No test invoked main; add real exact-object CLI positive and negative paths, including malformed-input and output-collision reporting. |
| Edge 1: UTF-16 DTD/entity bypass | medium | patch | Independently reproduced by the parent; shares the encoded XML declaration defect in Blind 1. |
| Edge 2: nested identity elements | medium | patch | Independently reproduced by the parent; shares the scalar identity defect in Blind 2. |
| Edge 3: no-release package relabelling | medium | patch | The no-release early return precedes the extension/role check; reject relabelled package/symbol files before that return. |
| Verification 1: signed-package caller regression | medium | patch | Reviewer mutation removed the unsigned-validator delegation while all 35 tests stayed green; add signed nupkg/snupkg archive-level negatives with no output destination. |
| Verification 2: CLI live graph regression | medium | patch | Existing tests call only the library, so dropping the CLI root argument evades coverage; exercise both commands against committed objects and rehashed graph/policy drift. |


All patch findings above were resolved and audited by the parent after the
implementation follow-up; none was deferred. The follow-up added
encoding-aware declaration rejection, scalar nuspec identity checks, nested
member-mode validation, no-release package checks, nonblocking regular-file
archive opens, close-failure cleanup, early result-path reservation, and real
committed-object CLI coverage. Valid declaration-free UTF-8/UTF-16 inputs still
pass. The publication-candidate suite now has **49** distinct passing tests.
Mutation probes independently removed unsigned-package rejection and live-CI
validation: the signed package/symbol cases and both CLI graph cases failed as
required, so these checks are observed by the tests.

Final parent verification reran the exact eight-module command recorded above
and passed **277/277** in 22.352 seconds after all code fixes. Syntax compilation,
create/verify CLI help, seven-file CRLF verification, and `git diff --check`
also passed. Retained local logs are
`/tmp/frontcomposer-gov1-20261107/parent-related-tests.log`,
`/tmp/frontcomposer-gov1-20261107/review-fixes-tests.log`, and
`/tmp/frontcomposer-gov1-20261107/review-mutations.log`. These are local checks;
no hosted run, release approval, conformance packet, or publication is claimed.
The existing upstream, implementation, adoption and live acceptance gates remain
open, and the story stays `in-progress` without a completion commit.

### Active-policy authority continuation review — 2026-11-13

Three independent local review lenses inspected the bounded working-tree patch.
The full historical-baseline diff is retained separately and is not represented
as exhaustively reviewed. Edge-case review returned no findings. Parent probes
and logs are under `/tmp/frontcomposer-gov1-authority-review-579s9r36/`.
This review does not satisfy the authenticated five-lens conformance gate.

| Finding | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| Blind 1: declared CI caller hash can disagree with committed candidate bytes | high | patch | Parent reproduced successful CI creation after changing the committed candidate workflow while retaining its authorized declaration. Bind object-backed creation/verification to the actual caller blob. |
| Blind 2: Release creation omits CI evaluator authorization | high | patch | Parent rehashed a CI evaluator with an unauthorized reusable commit; Release creation returned success. Require CI authorization under the same bound active policy. |
| Blind 3: Release evaluator caller can name another commit | high | patch | Parent changed the Release caller to a nonexistent commit and rehashed it; creation returned success. Bind its caller commit and actual workflow bytes to the CI candidate. |
| Blind 4: disposition consumes another AD-15 attempt | high | patch | Parent supplied run 999999/attempt 999/failure while classifying run 42/attempt 1/success; the CLI accepted it. Validate the handoff shape and authenticated Release coordinates before using its candidate. |
| Blind 5: rejected repeat invocation preserves prior output | low | reject | Existing outputs are caller-owned and a nonzero exit is not a newly produced handoff. The checked-in workflows invoke creation once in fresh job directories; no reachable stale artifact path was demonstrated there. Deleting prior caller files would add behavior and violate preservation expectations. |
| Blind 6: missing AD-15 evidence has no total incident/ledger observation | high | defer | The legacy verifier already lacks a total classifier and typed append-only ledger for earlier authentication/validation failures (NC-10/NC-19/NC-26/NC-29). Preserve the source-substitution refusal; atomic handoff-v3/classifier/ledger migration remains required and open. |
| Blind 7: disposition negatives absent from Quality | medium | patch | The explicit Quality Python list omits the module containing the new missing/unequal-candidate tests. Add that module to the blocking deny-only lane. |
| Blind 8: Release shell tests omit published attempts | medium | patch | Current harness fixes publication jobs to skipped. Add publication-success/failure and unavailable-CI fixtures with no external side effects. |
| Blind 9: story review status differs from progress narration | low | reject | The snapshot captured the workflow's temporary local-review status. Restore in-progress after local review because implementation/adoption gates remain open; no conformance or completion status is claimed. The proposed finding edits only the build spec. |
| Verification gap 1: disposition negatives absent from normal workflow | medium | patch | Pre-verified explicit-workflow search confirms the test module is unregistered. Shares Blind 7's root cause; retain its separate finding row. |
| Verification other 1: C# Governance positive omits mandatory candidate | medium | patch | Parent ran the exact built xUnit method: 1 test failed at CiGovernanceTests.cs:2827 with missing AD-15 candidate. Supply a matching handoff to its positive fixture and retain the negative coverage. |
| Parent verification 1: release-evidence fixture declares an unrelated CI caller hash | medium | patch | The first post-review 157-test run failed two provenance positives because the new live check correctly rejected their invented caller hash. The fixture now commits deterministic CI bytes with the base policy and authorizes their actual hash; all original assertions remain. |

### EventStore default selection review — 2026-11-14

Three independent continuation reviewers inspected the scoped engine/test diff;
edge-case and verification-gap reviewers returned no findings. The five blind
findings were independently checked by the parent against actual MSBuild in
isolated, authored fixtures. Probe receipts and the local review diff are under
`/tmp/frontcomposer-gov1-eventstore501-2r2jo7ji/`.

| Finding | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| EventStore blind 1: case-variant version declaration escapes the proof | high | patch | Parent and reviewer reproduced accepted XML with a lowercase EventStore declaration while actual MSBuild selected 4.2.0. Reject ambiguous case-insensitive version declarations before excluding the inactive row. |
| EventStore blind 2: imported assignment after the compatibility row | high | patch | A parent probe inserted an Import after the retained override and before the default; validation accepted it while MSBuild selected imported 4.2.0. Require the qualified proof to remain import-free across the catalog. |
| EventStore blind 3: invocation-level global selector override | low | patch | MSBuild intentionally honors a supplied -p:HexalithPartiesSdk42CatalogView=true. Clarify in the helper/documentation that this validates catalog defaults; approved governed argv remains separate authority and supplies no such override. The same invocation-level limitation already applies to ordinary canonical self-defaults. |
| EventStore blind 4: an unmeasured Parties project remains allowed | false | reject | The parent changed the literal selector to Hexalith.Parties.UnmeasuredConsumer; actual FrontComposer evaluation still selected 5.0.1. This FrontComposer-only proof neither qualifies another module nor authorizes its execution; changed raw catalog provenance and other owners' profiles/build gates remain binding. |
| EventStore blind 5: default mutation helper rejects supported shapes | medium | patch | Both an unconditional default and accepted self-default whitespace passed production validation/MSBuild as 5.0.1 but raised the helper's AssertionError. Locate every supported unique default and retain the production/negative assertions. |

### Immutable compatibility review — 2026-11-14

Three independent reviewers inspected the scoped Immutable diff. Every finding
is recorded below before grouping. Parent probes using actual MSBuild confirm
the wildcard, escaped-identity and task-output counterexamples; receipts are
`parent-review-masking-probes.json` under `/tmp/frontcomposer-immutable-zx6m5qi7/`.
This is local review, not the authenticated GOV-1 conformance gate.

| Finding | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| Immutable blind 1: wildcard removal bypasses operation matching | high | patch | Parent appended Remove=System.*; the proof accepted but MSBuild returned no Immutable row. Correct the existing reversed fnmatch arguments and cover package masking behavior. |
| Immutable blind 2: escaped package identity bypasses operation matching | high | patch | Parent appended Update=System%2ECollections%2EImmutable; the proof accepted but MSBuild selected 99.0.0. Reject unproven escaped identities in the closed view and retain literal matching. |
| Immutable blind 3: task output can append PackageVersion | high | patch | A CreateItem task outputting ItemName=PackageVersion passed the proof and produced both 10.0.15 and 11.0.0 rows. Reject executable target/output authority in the catalog's closed compatibility proof. |
| Immutable blind 4: shared default is not fixed by the FrontComposer reader | false | reject | Parent changed the unrelated default to 1.0.0; the admitted FrontComposer consumer still selected exactly 10.0.15. This profile proves its owner scope; the normal Builds matrix independently requires 11.0.0 for unlisted consumers and rejects that mutation. No whole-ecosystem compatibility or publication authority is granted by the FrontComposer proof. |
| Immutable blind 5: Builds view test does not load the generator | false | reject | The new owning test asserts evaluated catalog roles. The normal FrontComposer net10 test build and GeneratorDriverTests cover generator consumption; the parent reproduced CS1705 with 11 and the corrected lane passes 40/40. A separate real SDK10 compiler smoke also passes with both analyzers and warnings-as-errors. The matrix claims no compiler-load coverage of its own. |
| Immutable blind 6: copied role sets omit future projects together | medium | patch | A new tracked project could be absent from all three copied lists. Add a root tracked-project inventory comparison with explicit archived-sample exclusions so the current scope cannot silently lag source. |
| Immutable blind 7: package audit evidence remains stale | medium | defer | Parent hash comparison shows the stored audit hash already differs from the pre-change catalog. Existing accepted EventStore/Memories selections also lag the neutral defaults. Keep the new candidate/64-view/compiler receipts; canonical audit acceptance and an owning committed revision remain required at landing, without rewriting approval evidence to green. |
| Immutable edge 1: percent-escaped Immutable update | high | patch | Independent reviewer reproduced System.Collections.%49mmutable selecting 11.0.0 after static acceptance. Same root cause as blind 2; retain the separate row and cover both encodings. |
| Immutable verification other 1: existing blocking view guard rejects Immutable | medium | patch | The pre-verified unchanged Test-OwnedCatalogView Counter.Domain probe reports unrelated-package drift because it permits only EventStore/OpenAPI/OIDC differences. Assert the exact Immutable value for admitted roles while preserving the unrelated-package guard for every other package/consumer. |

## Local release-boundary denial fixes — 2026-11-13

This resumed implementation corrects three independently testable legacy-path
defects. It preserves the original `baseline_commit` and does not activate a
split reusable or close any conformance bundle.

- `release_contract.py select-ci` now requires both CI and Quality API responses,
  selects completed successful exact-candidate push runs before testing
  uniqueness, rejects malformed/paginated/ambiguous responses and unsafe run
  integers, and records the selected Quality projection. Failed or cancelled
  runs cannot make one successful run ambiguous. The Release caller fetches
  Quality before emitting the candidate and carries its selected projection
  alongside the unchanged raw CI handoff.
- Production fallback classification compares the immutable owner-recorded
  digest; it no longer replaces that digest with the current graph/policy/workflow
  digest. The existing explicit digest input is forwarded and shape-checked by
  prepublication tooling. Missing or stale approval denies publication. The
  deliberate behavior change bumps the evidence helper to `4.3.0`.
- The legacy release-disposition helper refuses a governed publication attempt
  without its AD-15 candidate and rejects malformed or unequal candidate/head
  identities. Its CLI cannot silently fill that candidate from the run API head.
  This is a refusal fix, not the still-required total classifier or typed ledger
  migration.

Verification:

`python3 -m unittest tests/eng/test_publication_candidate.py tests/eng/test_dependency_handoff.py tests/eng/test_release_contract.py tests/eng/test_release_evidence_v2.py tests/eng/test_release_prepublish.py tests/eng/test_release_disposition.py tests/eng/test_workflow_source_closure.py`

passes **144/144** in 7.779 seconds. New regressions execute the real selection
CLI, the checked-in Release authentication shell block with local API fixtures,
the production classification CLI with unchanged approval followed by
graph/policy/workflow drift or a missing approval, and the missing/malformed
governed-candidate CLI paths. `actionlint .github/workflows/release.yml` passes.
Logs are `/tmp/frontcomposer-gov1-release-tests.log` and
`/tmp/frontcomposer-gov1-actionlint.log`.

The same eight-module command with `tests/eng/test_dependency_graph.py` appended
runs **288** tests and fails with **14 failures and 7 subtest errors** in existing
catalog-profile fixtures. Those fixtures read the current root Builds working
catalog, which has two `HexalithEventStoreVersion` values (`4.2.0` and `5.0.1`),
before their intended mutations. The unchanged graph code and test files have
an empty Git diff. The standalone command
`python3 -m unittest tests.eng.test_dependency_graph.PolicyShapeTests.test_frontcomposer_profile_unconditional_module_property_passes`
independently reproduces that failure. Logs are
`/tmp/frontcomposer-gov1-related-tests.log` and
`/tmp/frontcomposer-gov1-existing-graph-failure.log`. Catalog/policy/dependency
changes are outside these three fixes; this failure prevents an all-tests-pass
or GOV-1 completion claim.

The story remains `in-progress`, release eligibility remains `blocked`, and all
owner decisions and split closure bundles remain open. Handoff-v3/manifest-v4,
run-bound fallback authorization-v3, the total classifier/append-only ledger,
candidate-free pinned publisher/verifier/recovery, hostile-credential live proof,
accepted upstream split revision, delayed activation, incident runbook/targets,
literal-false authenticated setting, protected-main protocol, and authenticated
conformance/approval packets are still required. These checks harden existing
paths and do not substitute for that migration. No remote operation, workflow
dispatch, policy pin, submodule pointer, approval, release, or publication was
performed. The changed caller also requires the normal independently reviewed
AD-12 authorization process; this work does not pre-authorize itself.


### Parent verification of the resumed patch

The parent inspected the local unified diff and reran the seven release modules
with verbose test reporting: **144/144** passed in 7.445 seconds, including the
checked-in workflow shell regression with no skip. `actionlint`, Python syntax
compilation of the four changed helpers and their four test modules, and
`git diff --check` also passed. The unchanged graph case above independently
reproduced its existing duplicate-property error in the parent run (exit 1).

Parent logs and both captured diffs are retained under
`/tmp/frontcomposer-gov1-parent-wyuscpuk/`. The preserved historical baseline
produces a 75,965,342-byte diff spanning 1,194,724 lines; it is retained as an
audit artifact, not represented as fully reviewed here. This inspection covers
the resumed local patch only and does not satisfy the independent five-lens
conformance review or the remaining task/acceptance gates.

During implementation an independent `/pushall` operation advanced the root to
`0623f3acca3cc6d8b8151b99636458beaa8dd34a`, changing only the nine submodule
pointers. This workflow preserved that work and verified its local patch against
the resulting checkout. No staging, commit, push, or dependency update was
performed by this Build run; the original story baseline remains unchanged.


### User approval of the local fixes — 2026-11-13

The user replied `approve` after receiving the report of the three local
release-boundary fixes and the 144/144 focused-test result. This records
approval of that implementation scope: exact-candidate CI and Quality
selection, immutable fallback-approval digest checks, and governed-candidate
identity rejection, with the reported broader catalog-test blocker.

The story remains `in-progress`. Product/release role acceptance, upstream
split-publication acceptance, independent conformance review, live evidence,
protected-main approval records, and release eligibility retain their recorded
open or blocked states. This conversational implementation approval supplies
no authenticated conformance or production-publication evidence.


## Local active-policy handoff refusal fixes — 2026-11-13

This continuation implements the independently testable NC-4/NC-5/NC-17 denial
corrections without switching the split reusable or changing policy authority.
The original `baseline_commit`, all earlier working changes, the in-progress
status and blocked release eligibility are preserved.

- CI rejects a zero/malformed push-before before drafting an evaluator and uses
  only that exact base policy. It no longer selects the candidate policy or
  treats an unauthorized handoff's exit 2 as a successful push gate.
- `dependency_handoff.py create-ci` and `create-release` return hard failure
  for unauthorized/invalid handoff evidence, with no fabricated deferred
  sentinel. The Release CLI also requires its supplied policy commit and the
  loaded raw policy projection to equal the authenticated CI handoff, refusing
  candidate-policy or digest substitution before evaluator authorization.
  The Release emitter also fails a defensive legacy exit 2 after
  authenticating CI. The legitimate no-authenticated-CI sentinel path and
  `if: always()` evidence upload remain intact.
- Seven new tests exercise real CLI subprocesses against committed active and
  candidate-only policy objects, plus the checked-in CI/Release shell blocks
  with local command/API fixtures. Authorized active-policy controls pass;
  candidate-only authority, every nonzero helper result, and zero/malformed
  bases fail. Isolated mutation probes independently restored CI/Release exit-2
  soft success and unauthorized sentinel creation, then removed policy binding;
  all five regressions were detected. No working-tree production source was mutated by those probes.

The existing seven-module release regression command recorded above passes
**151/151** (the prior 144 plus seven distinct tests), with no skipped shell
regressions. `actionlint .github/workflows/ci.yml .github/workflows/release.yml`,
Python syntax compilation, CRLF checks and `git diff --check` pass. Logs are
`/tmp/frontcomposer-gov1-authority-gates-tests.log` and
`/tmp/frontcomposer-gov1-authority-gates-mutations.log`.

The exact existing graph command
`python3 -m unittest tests.eng.test_dependency_graph.PolicyShapeTests.test_frontcomposer_profile_unconditional_module_property_passes`
still exits 1 with one error: `HexalithEventStoreVersion` must define exactly
one property but the current root Builds catalog supplies `4.2.0` and `5.0.1`.
The graph engine, graph fixture module, catalog and policy are unchanged by this
continuation. Evidence is
`/tmp/frontcomposer-gov1-authority-gates-graph-blocker.log`; no all-tests-pass
claim is made.

NC-22's checkout-ref/closure work is unchanged and open. The accepted upstream
split successor, atomic handoff-v3/manifest-v4/run-bound authorization and ledger
migration, pinned candidate-free execution/recovery, incident readiness,
GitHub settings and authenticated conformance/approval gates remain required.
No closure bundle is closed by these local refusal fixes. The changed caller
blobs must be independently authorized through AD-12 delayed activation; their
current active-policy mismatch can intentionally block CI and cannot be
relabelled a deferral. No policy pin, dependency, submodule, setting, remote
operation, workflow dispatch, approval, release or publication was changed or
performed by this continuation.


### Final parent verification of the authority continuation

All patch findings in this continuation's review table were resolved. Exact
committed caller checks protect object-backed creation and live validators;
the Release CLI rechecks CI authorization under the same bound policy. Offline
structural validators and supplied-data factories retain their existing
already-authenticated-input contract. The disposition CLI validates the full
AD-15 object and its API repository/workflow/run/attempt/conclusion before
using its candidate. Quality now runs the disposition negatives, and its C#
publication-positive supplies the shared closed handoff fixture. Published
shell negatives cover helper failures/legacy exit 2 and unavailable CI while
retaining forensic files without external calls.

The parent's final seven-module release command recorded above passes
**157/157** in 12.099 seconds, with no skipped tests. The corrected built xUnit
fact `Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.ReleaseEvidenceWorkflow_IndependentlyVerifiesPublishedArtifacts`
passes **1/1** after the focused Debug test-project rebuild (zero warnings and
errors, existing dependency outputs). Actionlint passes for CI, Quality and
Release. Python syntax, CRLF and whitespace checks pass. The earlier two failing
fixture tests and the original failing C# fact remain retained as failure
receipts; subsequent successes are not relabelled original clean passes.

Parent logs, reproduced source/attempt forgeries, the original and final local
review diffs are under `/tmp/frontcomposer-gov1-authority-review-579s9r36/`.
Its final historical baseline is retained as `final-historical-baseline.diff.gz`,
with a verified decompressed SHA-256 in `historical-diff-sha256.txt`; the
superseded uncompressed snapshot was removed to relieve disk exhaustion. The exact final
Python receipt is `final-release-tests-after-fixture.log`; C# and workflow
receipts are `final-csharp-governance.log` and `final-actionlint.log`.
The complete historical diff is an audit artifact, not an exhaustive review
claim. The independent implementation build and focused logs remain at
`/tmp/frontcomposer-gov1-review-fixes-csharp-build.log` and
`/tmp/frontcomposer-gov1-review-fixes-csharp-focused.log`.

Acceptance audit: AC1 cannot pass while the unchanged selected-catalog fixture
reports duplicate EventStore version properties. AC2/AC3 have stronger local
refusal evidence, but exact active-closure activation and authenticated hosted
selection are not established by this run. AC4 through AC8 remain open for the
accepted split reusable, atomic v3/v4/run-bound authorization migration, total
classifier/append-only ledger/recovery, owner/settings decisions and live
conformance/approval evidence. The original completed checkboxes remain
unchanged and no remaining implementation task is marked complete. The spec
contains no frozen I/O matrix to audit. The total-classifier finding is recorded
in `deferred-work.md`; NC-22 and every split closure bundle remain open.

The story is restored to `in-progress`, implementation conformance remains
open, and release eligibility remains blocked. The original baseline is
preserved. No staging, commit, push, remote operation, dependency/submodule
update, policy authorization, setting change, dispatch or publication was
performed by this run. The changed evaluator callers still need independent
AD-12 authorization and delayed activation; a missing row must block CI.


## User-selected EventStore 5.0.1 follow-through — 2026-11-14

The user directed `use 5.0.1` to resolve the reported catalog blocker. A read-only
MSBuild evaluation of `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`
with `-p:Configuration=Debug -getProperty:HexalithEventStoreVersion` already
returns `5.0.1`. The shared catalog's separate `4.2.0` assignment is conditional
on retained Parties and Builds packaged-host project selectors; it is not
FrontComposer's selected default. Preserve those other consumers and the raw
catalog bytes. No shared dependency, submodule pointer or local package-version
override is required for this selection.

Implementation intent for this continuation:

- Update `eng/dependency_graph.py`'s FrontComposer-only selected-property check
  to recognize the exact retained SDK4.2 assignment only when its two selector
  definitions are proved inactive for FrontComposer. Verify their false defaults,
  direct top-level unconditional placement, declaration order, and closed
  conditions containing only literal `MSBuildProjectName.Equals` clauses for
  Parties identities and the three Builds packaged-host roles. Do not evaluate
  arbitrary MSBuild expressions or execute candidate code. Any changed/ambiguous
  selector, FrontComposer project in scope, duplicate/reassigned selector,
  conditional/Target/Choose placement or malformed override stays fail-closed.
- Continue requiring exactly one unconditional/canonical-self-default literal
  EventStore version after excluding that proven-inactive compatibility row.
  Preserve strict behavior for other profiles and all existing properties. The
  selected default is currently `5.0.1`; compatible future version advances
  remain value-independent under GOV-1's semantic/provenance separation.
- Update `tests/eng/test_dependency_graph.py` with positive selection and hostile
  scope/shape/order regressions. Existing mutation helpers must target the
  canonical default, so missing/default-version tests cannot accidentally mutate
  only the inactive compatibility row. Preserve all existing assertions.
- Verify the former standalone blocker, the full graph suite, and the existing
  seven release modules. Confirm actual FrontComposer MSBuild selection as
  `5.0.1` and document the exact commands/results, retaining earlier failure
  receipts. This does not close the separate active-policy activation, release,
  upstream split, owner or authenticated conformance gates.


### EventStore 5.0.1 implementation and local verification

Implemented the bounded static proof in `eng/dependency_graph.py` and retained
all original strict default checks. The exact `4.2.0` compatibility row is
excluded only for the FrontComposer profile after validating both selectors'
false/true declarations, direct unconditional top-level placement, literal
project-name Equals scopes and declaration order. Case aliases, duplicate or
reassigned selectors, dynamic or broadened conditions, malformed override
content and conditional/Target/Choose placement are rejected. An intervening
Import/ImportGroup also rejects the proof because imported code can reassign
selector authority outside the inspected XML. No candidate MSBuild expression
is evaluated by this proof.

The existing graph-fixture mutation helpers now target the unique canonical
default. Nine additional test methods cover current/future defaults, strict
other profiles, hostile selector scope/shape/order/placement, malformed
compatibility rows, imported authority and preservation of default-version
validation. Earlier positive and negative assertions remain in place.

Verification on 2026-11-14:

- `python3 -m unittest tests.eng.test_dependency_graph.PolicyShapeTests.test_frontcomposer_profile_unconditional_module_property_passes`
  passes **1/1**; the previously reported standalone blocker is resolved.
- `python3 -m unittest tests/eng/test_dependency_graph.py` passes **153/153**
  in 44.398 seconds. The focused profile class separately passes **49/49**.
- The existing seven-module release command recorded above passes **157/157**
  in 17.267 seconds, with no skipped tests.
- `dotnet msbuild src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj -p:Configuration=Debug -getProperty:HexalithEventStoreVersion,HexalithPartiesSdk42CatalogView,HexalithBuildsPackagedHostSdk42CatalogView`
  exits 0 and reports `5.0.1`, `false`, `false` respectively.
- Python syntax compilation, CRLF checks and `git diff --check` pass.
  The shared catalog has no owning-repository diff and retains raw SHA-256
  `8cbf2779f0d971c72c71d37e035af908c0092242326b456eedaa0839761aa8e0`.

Logs are `/tmp/frontcomposer-gov1-eventstore5-former-blocker.log`,
`/tmp/frontcomposer-gov1-eventstore5-profile-tests.log`,
`/tmp/frontcomposer-gov1-eventstore5-graph-tests.log`,
`/tmp/frontcomposer-gov1-eventstore5-release-tests.log` and
`/tmp/frontcomposer-gov1-eventstore5-msbuild-selection.json`. During the first
validation attempt, the filesystem filled: bytecode compilation reported
`[Errno 28] No space left on device`, and MSBuild exited 1 with empty redirected
output. After the parent removed only its superseded historical-diff artifact,
syntax compilation and the exact MSBuild command passed. Those retries are not
represented as original clean attempts; all earlier catalog-failure receipts
remain retained.

The local duplicate-property fixture blocker is resolved without changing raw
catalog bytes, package versions, submodule pointers or runtime code. This is
semantic selection evidence, not hosted graph/adoption or production-release
authorization. AC2 through AC8 and the remaining live AC1 evidence retain their
independent open gates. The story remains `in-progress`, implementation
conformance remains open and release eligibility remains blocked. No commit,
push, policy authorization, settings change, dispatch or publication was
performed by this continuation.


### EventStore import-proof refinement after local review

The review's blanket import-free fix exposed an existing legitimate import:
`Props/Directory.Packages.props` optionally imports
`$(MSBuildThisFileDirectory)Logging.Build.targets` under its exact Exists
condition. The selected Builds revision contains that target; it has one
post-ResolveReferences analyzer-selection/removal Target and no property
assignments. Do not strip this import from the synthetic catalog copy or alter
the shared catalog to manufacture a pass.

Complete the same-commit, data-only semantic proof: accept only this exact
optional import shape, directly at the catalog root after the canonical version
default. Read its target (or prove its absence) from the same Builds commit
using the existing bounded committed-object reader, with regular-file mode
validation. For a present target, inspect its closed analyzer-only XML structure
as data: no property assignment, further import, task execution or unfamiliar
shape is admitted. Never trust a filename alone, use working-tree target bytes,
follow a symlink, execute the target or adopt a content-hash/version allowlist.
Unknown, duplicate, grouped/misplaced or changed imports remain rejected.
Preserve existing graph/envelope/catalog-digest schemas; the immutable Builds
commit already binds the auxiliary file.

Retain the raw logging import in existing fixture copies. Add positive and
hostile committed-target coverage, including missing optional target, changed
property/import/task content, file modes, same-commit authority and working-tree
drift. Re-run the former blocker, graph/release suites and actual 5.0.1 selection.
The intermediate import-free refusal and any failed verification attempts remain
evidence; only final tests of the actual supported catalog shape resolve this
continuation.


### Final parent verification of EventStore 5.0.1 selection — 2026-11-14

All accepted EventStore review findings are resolved. The final version-alias
check precedes the override-recognition early return, including renamed and
unconditional legacy aliases. The logging-import refinement authenticates the
exact optional target/absence from the selected Builds commit, enforces regular
mode and the existing blob ceiling, and accepts only the closed analyzer-only
XML structure. The raw logging import remains in fixture copies. Unconditional
and supported whitespace self-default mutations now target the actual default.
Invocation-level global properties remain separate policy-bound inputs; no
new override or admission of other modules is introduced.

The parent ran the combined eight-module command through
`python3 /tmp/frontcomposer-gov1-eventstore501-2r2jo7ji/parent-final-checks.py`.
It passes **317/317** in 30.369 seconds, with no skipped tests (160 graph and
157 release tests). Syntax, CRLF and whitespace checks pass. The parent also
confirmed `5.0.1` and both false compatibility selectors in Contracts, Shell
and AppHost under both Debug and Release; the six exact MSBuild commands and
responses are `msbuild-selection.json` in that directory.

The same parent script checks the actual selected committed catalog/import
proof, without fixture normalization, at Builds
`1fc93c238d5ef2a8f5c2590a8e9fa0cdab63c57c`: it passes for the `5.0.1` default.
Its committed raw catalog SHA-256 is
`6588e843dffcdc732f51e87f5a4dde196a010bd1a6f115e764e547189c50298b`.
The separately observed
`8cbf2779f0d971c72c71d37e035af908c0092242326b456eedaa0839761aa8e0`
identifies the CRLF checkout used by the formatting gate, not the committed
provenance blob. The shared repository has no diff; dependency pointers, raw
catalog bytes, policy pins and runtime code are unchanged.

Whole-profile acceptance remains false: the actual committed catalog supplies
`System.Collections.Immutable` **11.0.0**, while the active FrontComposer
profile requires **10.0.15**. The raw package check reports
`System.Collections.Immutable expected version '10.0.15', found '11.0.0'`.
The other 13 required package pins match. The user's EventStore selection does
not change that separate package decision. This blocker is appended to the
deferred-work ledger; the earlier synthetic fixture normalizes package pins
for isolation and is not whole-profile acceptance evidence.

Final receipts are `parent-final-related-tests.log` and
`parent-actual-committed-selection.json` under
`/tmp/frontcomposer-gov1-eventstore501-2r2jo7ji/`. The initial scoped review
diff, final diff and parent adversarial probes are retained there as well.
The pre-review 310-test pass, intermediate overbroad import refusal and disk
failures are not relabelled as the final run. Earlier review/C# receipts remain
in their recorded directories.

The EventStore duplicate-property blocker is resolved. GOV-1 remains
`in-progress` with release eligibility blocked by the independent Immutable
policy mismatch and its existing activation, upstream split, implementation,
owner/settings and authenticated conformance gates. No remaining original
implementation checkbox is marked complete. No staging, commit, push,
dependency update, policy authorization, remote operation, workflow dispatch
or publication was performed.


## User-authorized Immutable mismatch correction — 2026-11-14

The user instructed `fix mismatch`. Fix the actual FrontComposer catalog/policy
selection while preserving its supported .NET 10 compiler/generator compatibility
and the shared Builds repository's intentional .NET 11 default. EventStore stays
at the user-selected 5.0.1.

Parent pre-change qualification (logs in `/tmp/frontcomposer-immutable-zx6m5qi7/`)
restored and built SourceTools/Contracts against the current Immutable 11.0.0.
The library build passed, but the freshly restored SourceTools.Tests Debug build
failed CS1705: the generator references Immutable 11 while the net10 test host
references Immutable 10. An isolated SDK 10.0.404 compiler-host smoke then failed
CS8032 because its analyzer could not load Immutable 11. These reproduce the
existing documented compatibility hold. Do not solve this by weakening tests,
raising the policy pin to 11, changing target frameworks, bundling a new runtime,
or globally downgrading the intentional Builds/.NET 11 catalog default.

Implement one cohesive shared-catalog consumer compatibility correction:

- Work from `references/Hexalith.Builds` for its owned changes, after reading its
  tracked AGENTS.md, DEVELOPMENT.md and build/test configuration. Never load an
  agent skill from references. Add an explicit named FrontComposer consumer view
  in `Props/Directory.Packages.props` so the relevant FrontComposer projects,
  including Contracts, SourceTools and test/compiler consumers, select Immutable
  10.0.15. Keep non-FrontComposer consumers on the existing 11.0.0 default. Use
  central package authority and literal project-name scoping, without local
  PackageVersion/VersionOverride declarations or altering other package selections.
- Extend FrontComposer's canonical semantic reader in `eng/dependency_graph.py`
  only as needed to validate that exact active consumer contract against its
  existing 10.0.15 policy requirement. Keep other profiles/packages strict. Prove
  selector defaults, exact admitted literal scopes, order and unique package/value
  authority; reject aliases, missing/duplicate/reassigned or broadened selectors,
  dynamic/unproven values and imports. Reuse the bounded same-commit logging import
  proof where appropriate. No executable MSBuild evaluation of candidate data and
  no hash/commit compatibility allowlist. Preserve graph/evidence schemas and
  active-policy/delayed-activation authority.
- Add meaningful positive/hostile coverage in the owning repository and
  `tests/eng/test_dependency_graph.py`, and register any new owning test in its
  normal blocking lane. Test actual evaluated FrontComposer/non-FrontComposer
  catalog views and both Debug/Release where relevant. Preserve earlier tests and
  their strict expectations; do not normalize away the compatibility view to
  manufacture a pass. Update the governance documentation and current result.
- Restore/rebuild SourceTools and SourceTools.Tests using the corrected shared
  catalog, run the relevant generator test lane, and repeat the SDK 10.0.404
  analyzer-host probe. The probe at
  `/tmp/frontcomposer-immutable-host-h_6q4rdh/CompilerHostSmoke.csproj` references
  the real newly built SourceTools and its Contracts analyzer dependency. It must
  build cleanly without adding Immutable 11, suppressing analyzer warnings, or
  removing an analyzer. Re-run graph/release regressions and validate the corrected
  candidate catalog against the actual unchanged FrontComposer package profile.

This authorizes the necessary local shared-catalog source change. Preserve all
prior working changes and the story baseline. Do not stage, commit, push, move a
submodule checkout/pointer or perform remote operations. Qualify candidate bytes
with isolated committed-object fixtures when needed; do not claim the old selected
Builds commit already contains an uncommitted correction. Normal owning-repository
landing/selection and AD-12 activation remain separate, as do GOV-1's broader
upstream, owner, publication and conformance gates.

## Immutable correction result — 2026-11-14

The local correction is implemented in the owning Builds catalog and the
FrontComposer semantic reader. The named 25-project view selects Immutable
10.0.15; non-admitted consumers retain 11.0.0. EventStore remains 5.0.1. The
existing policy pin, target frameworks and central package authority are
unchanged. Every patch disposition in the Immutable Review Triage Log above has
been applied, including wildcard matching, escaped identities, task outputs,
tracked-project inventory and the existing mandatory catalog guard. The stale
accepted audit remains deferred; the two rejected findings remain rejected for
the evidence recorded above.

Parent final verification receipts are in
`/tmp/frontcomposer-immutable-zx6m5qi7/`:

| Check | Result | Receipt |
| --- | --- | --- |
| Eight graph/release Python modules | 327 passed, no skips | `parent-final-regressions.log` |
| GeneratorDriverTests on the rebuilt net10 test host | 40 passed, no skips | `parent-final-generator-tests.log` |
| Owning catalog view matrix, Debug and Release | 2 tests passed; 64 actual evaluations | `parent-final-view-tests.log` |
| Central package version validation | 317 entries passed | `parent-final-central-versions.log` |
| Actual Contracts, SourceTools and SourceTools.Tests project views | Six Debug/Release views select Immutable 10.0.15 and EventStore 5.0.1 | `parent-final-actual-project-views.json` |
| SDK 10.0.404 compiler host, both real analyzers, warnings as errors | Rebuild succeeded, zero warnings/errors | `parent-final-sdk10-smoke-rebuild.log` |
| Exact candidate bytes under all 14 unchanged package requirements | One selector validated from an isolated committed fixture | `parent-final-candidate-profile.json` |
| Root and owning repository whitespace checks | Passed | `git diff --check` in each repository |

The candidate checkout catalog SHA-256 is
`ce766c4a3359091835cbeddc700be1aba18fea2084a92215fa2b1c112c3163b2`.
The fixture's committed LF catalog blob SHA-256 is
`430c753a9c621141bf618c7dba7402dcba92e306e48bba891a3c56f02b0e853d`.
The active policy's committed blob SHA-256 remains
`e51cb46ea6ae1bb9b3f2436cb1d551f9f54e9659ad33d34e1affba7c3bec16c2`;
its CRLF checkout has the same normalized content. These are candidate-byte
receipts, not selected-root or live release evidence.

Two broader owning checks remain red, with exact baseline comparisons:

- From `references/Hexalith.Builds`,
  `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` exits 1
  with 19 errors: an already stale declaration hash and 18 unmatched accepted
  EventStore/Memories selections. Parent comparison proves all 317 neutral
  package selections unchanged by this correction. Logs:
  `/tmp/frontcomposer-immutable-owning-audit-validation.log` and
  `parent-audit-hash-baseline.json` / `parent-audit-neutral-baseline.json`.
- From the same repository,
  `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` exits 1
  with 462 existing Platform tuple assertions after executing 142 view probes
  and 17 authority controls. The exact pre-change script/catalog at `1fc93c2`
  failed 966 assertions: the same 462 Platform assertions plus 504 stale default
  EventStore 5.0.0 expectations. Only current-default expectations now use the
  already-selected 5.0.1; historical/negative 5.0.0 controls remain. There are no
  FrontComposer/Immutable failures. Baseline/candidate logs and counts are in
  `/tmp/frontcomposer-immutable-reviewfix-03ohw556/review-fix-receipt.json`.

No accepted audit record, repository commit, branch, remote or submodule pointer
was changed. Builds remains selected at `1fc93c238d5ef2a8f5c2590a8e9fa0cdab63c57c`,
which still contains the original mismatch. The qualified local changes require
owning landing and subsequent consumer selection; changed evaluators remain
subject to AD-12 activation. The story baseline is preserved and status remains
`in-progress`, with release eligibility blocked. This does not close the open
upstream, publication, role-decision or live conformance requirements. Review
covered the scoped current correction; the large historical baseline diff was
preserved separately and was not exhaustively reviewed.
