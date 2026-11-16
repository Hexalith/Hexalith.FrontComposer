# Epic 17 Context: Release Owners Establish Safe Publication and Trustworthy Release Records

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Close FR-24 release safety. The current legacy caller runs candidate code while it holds publication authority. This epic replaces it with a split pipeline: a secretless builder hands authenticated candidate bytes to a protected publisher that never runs candidate code. It also keeps an append-only, attempt-keyed release ledger, rehearses incident containment, and records the owner decisions that gate G-1, G-2, G-8 and the v1.0 milestone. Production stays halted until G-8 closes.

## Stories

- Story 17.1: [I · GOV-SRC-1] Reconcile Publication-Governance Sources
- Story 17.2: [I · GOV-I] Publish and Exercise the Release Incident Runbook
- Story 17.3: [I · GOV-B] Select the Accepted Split Publication Topology
- Story 17.4: [I · GOV-C] Remove Publication Authority from Candidate Execution
- Story 17.5: [I · GOV-D] Produce an Authenticated Run-Bound Publication Candidate
- Story 17.6: [I · GOV-E] Validate Handoff V3 and Typed Append-Only Attempt State
- Story 17.7: [I · GOV-F] Publish with Candidate-Free Pinned Code
- Story 17.8: [I · GOV-G] Pin Post-Release Verification and Preserve Incident Evidence
- Story 17.9: [I · GOV-H] Reject Ambiguous Publication Asset Names
- Story 17.10: [A · GOV-J] Revalidate GOV-1 on One Unchanged Candidate
- Story 17.11: [A · GOV-ACCEPT-1] Accept the Publication Boundary and Incident Posture
- Story 17.12: [A · GOV-ACCEPT-2] Accept Reconciled Governance Sources
- Story 17.13: [A · REL-LEDGER-1] Accept the Release-Ledger State Model
- Story 17.14: [I · REL-LEDGER-2] Complete Evidence-Backed Release Ledger Rows
- Story 17.15: [A · REL-A3-APP-1] Decide the Sample-Host Container Boundary
- Story 17.16: [A · PRD-APP-1] Record Final Product Readiness

## Requirements & Constraints

- **Separate statuses.** Two streams are tracked separately: publication boundary (EXT-BUILDS-1, GOV-B to GOV-H, GOV-J, GOV-SRC-1, GOV-ACCEPT-1/2) and evidence/decisions (GOV-I, REL-LEDGER-1/2, REL-A3-APP-1, PRD-APP-1). Acceptance, implementation, evidence, approval, publication authorization and milestone status never merge.
- **Item classes.** Only I items claim implementation completion. An A item closes through a dated accept/reject decision that cites exact digests. X (EXT-BUILDS-1) is owned by Builds. A missing receipt or a rejection keeps the gate open; any fix is a bounded residual.
- **Halt.** `HEXALITH_RELEASE_PUBLISH_ENABLED` must be literal `false`, a deny-only stop that never authorizes. No risk exception exists. A green run, environment approval, attestation, classification or Product approval never authorizes a release.
- **Exact bytes.** GitHub assets must match the sealed checksums byte for byte. NuGet may add only a valid root `.signature.p7s`; all other normalized ZIP members stay byte-equivalent. Rebuilding or repacking is never evidence. Candidates are author-unsigned.
- **Ledger.** Each attempt has an immutable key and one disposition. `deferred-no-ci-handoff` is the only deferred sentinel and is permanent. A bad or unauthenticated handoff is `missing-artifact`. Reruns only append; incidents are never relabelled green.
- **Lean evidence.** Reuse existing tests, lanes, tooling and artifacts; add no duplicate packets, schemas, validators or workflows. Stories record commands, counts, commit SHA and CI run link. Durable release evidence lives on the GitHub Release.

## Technical Decisions

- **Topology.** The caller pins one immutable Builds `domain-release.yml` (`split-publication-v1`) with two fixed jobs: `build-publication-candidate` and `publish-publication-candidate`. A new revision enters the AD-16 lineage (anchor `a8a50859…`) only through delayed activation, and rollback is tested.
- **Identities.** The lineage anchor, `builds-execution-sha`, Builds catalog gitlink, future split revision and candidate SHA are distinct; the gitlink need not equal the pin.
- **Gate.** It requires `refs/heads/main` and a 40-hex SHA equal to live main with one successful CI run and one successful `quality.yml` run. It reads only the run-named `hexalith.dependency-release-handoff.v1`, read-only. Bad run data fails first.
- **Builder.** It packs once, runs the existing checks, and uploads one `publication-candidate-<run_id>-<run_attempt>` (`hexalith.publication-candidate.v1`). Its outputs never authorize.
- **Graph.** `hexalith.dependency-graph.v1` covers root gitlinks and their direct gitlinks only, read from committed objects without initializing submodules.
- **Publisher.** It runs only pinned owner code in protected `production`. It authenticates every candidate byte as data, verifies an attestation or the approved fallback, and seals `hexalith.release-evidence.v4`. It requires `publish_authorized=true` before any NuGet, Release or tag effect. Asset destination names must be canonically unique.
- **Attempt state.** Every run uploads `hexalith.release-verification-handoff.v3` under `if: always()`. A total classifier assigns one state. `publication_started` is set just before the first mutation. Work extends `frontcomposer.release-ledger-record.v2`.
- **Post-release.** Verification uses the original candidate and evaluator closure and never authorizes retroactively. A separately authorized recovery stage preserves partial publications in a reserved-namespace prerelease.
- **GOV-J.** `frontcomposer.gov1-split-conformance.v1` and five review lenses bind one unchanged candidate. An open Critical or High finding blocks. It needs an evidence PR plus a separate approval PR; a direct push, squash or rebase fails.
- **Ownership.** Builds owns the reusable workflow and its permissions. FrontComposer owns artifacts, validation, evidence, publication and verification. The Release Owner owns production, credentials, exceptions and incidents.
- **Incidents.** `docs/release-incident-response.md` contains incidents without running candidate code and preserves evidence. Prefer unlisting and a new version.
- **Sample host.** The working assumption, for Story 17.15 to decide, is that the `Hexalith.FrontComposer.UI` image is local/e2e-only and never published.

## Cross-Story Dependencies

- EXT-BUILDS-1 blocks 17.3 and caller activation. 17.1, 17.2 and 17.13 can start now.
- 17.10 needs 17.2 to 17.9 on one candidate. 17.11 needs the runbook from 17.2. 17.12 needs 17.1 and goes stale on any digest change.
- 17.13 reviews 17.6's handoff and ledger. 17.14 waits for 17.13.
- 17.16 is last and needs G-1 to G-8 closed. G-3 from Epic 16 is still open.
- Finish source, workflow and policy changes before choosing a candidate; a later G-8 commit invalidates it.
