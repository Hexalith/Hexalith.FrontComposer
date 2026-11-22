# Epic 17 Context: Release Owners Establish Safe Publication and Trustworthy Release Records

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Establish FR-24 publication safety through a secretless builder, protected candidate-free publisher, append-only release records, exercised incident response, and owner decisions. Production must remain halted until G-8 closes. This requirement is not observed fact: on 2026-11-16 the emergency-stop variable read `true`, and releases had been published after halt adoption without an approved exception.

## Stories

- Story 17.1: Reconcile Publication-Governance Sources
- Story 17.2: Publish and Exercise the Release Incident Runbook
- Story 17.3: Select the Accepted Split Publication Topology
- Story 17.4: Remove Publication Authority from Candidate Execution
- Story 17.5: Produce an Authenticated Run-Bound Publication Candidate
- Story 17.6: Validate Handoff V3 and Typed Append-Only Attempt State
- Story 17.7: Publish with Candidate-Free Pinned Code
- Story 17.8: Pin Post-Release Verification and Preserve Incident Evidence
- Story 17.9: Reject Ambiguous Publication Asset Names
- Story 17.10: Revalidate GOV-1 on One Unchanged Candidate
- Story 17.11: Accept the Publication Boundary and Incident Posture
- Story 17.12: Accept Reconciled Governance Sources
- Story 17.13: Accept the Release-Ledger State Model
- Story 17.14: Complete Evidence-Backed Release Ledger Rows
- Story 17.15: Decide the Sample-Host Container Boundary
- Story 17.16: Record Final Product Readiness

## Requirements & Constraints

- Keep upstream acceptance, implementation, evidence, owner approval, publication authorization, and readiness separate. Implementation requires proof; approval requires dated named accept/reject receipts citing exact identities. Missing evidence leaves gates open.
- `HEXALITH_RELEASE_PUBLISH_ENABLED=false` is a deny-only emergency stop. No release approval or dispatch is eligible until the Release Owner or repository administrator sets literal `false` and records a subsequent authenticated API observation and server timestamp. `true`, green checks, protected-environment approval, and Product approval never authorize publication. No bounded risk exception is approved.
- The legacy caller executes candidate code with publication authority and is not G-8 conformant. Source reconciliation does not repair it. Distinguish requirements from dated nonconformance observations.
- Incident readiness requires an approved runbook and measurable acknowledgement/containment targets, a dated Release/Security/operations tabletop, immutable evidence, credential rotation when exposure is plausible, complete external-effect inventory, recovery, communication, and explicit re-enable approval. Unresolved containment-critical findings keep GOV-I open. Prefer unlisting affected packages and correcting under a new version.
- Reuse existing tooling, records, releases, and ledger contracts. Record commands, counts, source SHA, and CI links; add no duplicate evidence system. Durable release evidence lives on GitHub Releases; Actions artifacts are supplemental replay material.

## Technical Decisions

- One immutable Builds `domain-release.yml` implements `split-publication-v1` with fixed builder/publisher jobs. The builder has no production environment, publication credential, write scope, OIDC/attestation authority, or signing material. It packs once into one authenticated run/attempt-bound `hexalith.publication-candidate.v1` artifact.
- The protected publisher executes only active-policy-authorized pinned owner code, never candidate source. It treats candidate bytes as data, independently authenticates the closed archive and provenance/fallback, seals `hexalith.release-evidence.v4`, and requires `publish_authorized=true` before any product-publication mutation.
- GitHub assets must match sealed bytes exactly. NuGet downloads may add only a valid root `.signature.p7s`; every other normalized ZIP member must remain byte-equivalent. Rebuilding or repacking cannot prove equivalence. Candidates are author-unsigned.
- Separate five identities: the lineage anchor `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a`, independently held execution pins, Builds catalog gitlink, future owner-accepted split revision, and evidence-bound candidates. Re-read moving authorities; snapshots become historical. Git ancestry alone does not prove policy-history lineage.
- Dependency evidence covers root gitlinks and their direct gitlinks from committed objects without recursive initialization. Candidate policy changes activate only from a later base.
- An always-uploaded `hexalith.release-verification-handoff.v3` and total classifier cover every attempt outcome. Set `publication_started` immediately before the first product mutation. Missing or unauthenticated handoffs are `missing-artifact`; the sole valid deferred sentinel is permanently incident-bearing `deferred-no-ci-handoff`.
- Extend `frontcomposer.release-ledger-record.v2` with immutable attempt keys and append-only observations. A later green result never erases or weakens an incident. Independent post-release verification never authorizes retroactively. Incomplete publication requires separately authorized candidate-free preservation of immutable evidence in the reserved incident namespace before retry.
- Builds owns the reusable contract and minimum permissions; FrontComposer owns validation, evidence, publication, and verification; the Release Owner owns production, credentials, and incidents. Runbook and response-target approval remain Product Owner/Release Owner decisions.
- Both GOV-J approval PRs retrieve and hash the original CI packet, check results and five report/sidecar pairs. The owning spec preserves the coordinates and approval. Expiry requires a complete new qualification.
- GOV-J binds deterministic conformance and five review lenses to one unchanged candidate, a closed nonconformance register, an unchanged protected-main evidence PR, and a distinct approval-projection PR. Open Critical/High findings, stale or mixed identities, direct push, squash, or rebase fail acceptance.

## Cross-Story Dependencies

- A Platform pin change consumes one directional transition from the prior policy. The mismatched base at `a9e755da` still requires an Architecture and Release recovery decision.
- EXT-BUILDS-1 must supply an accepted immutable split revision before Story 17.3 or caller activation. Source reconciliation, incident readiness, and ledger-model decisions can progress independently of that upstream blocker.
- GOV-J follows GOV-B through GOV-I. GOV-ACCEPT-1 needs the approved incident runbook; GOV-ACCEPT-2 follows source reconciliation and becomes stale on source-digest changes. Incident readiness does not itself close either acceptance decision.
- Ledger backfill follows state-model acceptance. Preserve missing evidence and permanent dispositions for every publication-capable attempt, including later discovered releases.
- Final readiness waits for all gates, including open G-3 EventStore evidence/decisions. Finish release-blocking source, workflow, and policy integration before choosing a releasable candidate; later G-8 edits create a different candidate.
