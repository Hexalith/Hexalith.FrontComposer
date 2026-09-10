---
review: reconcile-parent-contract
spine: _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
spineUpdated: 2026-09-08
date: 2026-09-08
inputs:
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md
liveFactsChecked:
  - .github/workflows/ci.yml L25 uses domain-ci.yml@4eb33928a1d8c7775f97221cf9edc171db0cb5f8
  - .github/workflows/release.yml L7 workflow_dispatch; L17/L286/L321/L329 builds-execution-sha 4eb33928a1d8c7775f97221cf9edc171db0cb5f8
  - eng/release_evidence.py L64-65 MANIFEST_SCHEMA v2 (accepted), CURRENT_MANIFEST_SCHEMA v3 (produced)
  - eng/dependency-graph-policy.json carries a8a50859, 99d5a46c, 3f0e3595, 4eb33928 rows
---

# Reconcile parent architecture and FC-DEP-1 contract against the 2026-09-08 spine

Read-only check. Line numbers are from the inputs as they stand on 2026-09-08. Each (A) item gives
the exact replacement text; each (B) item is a requirement the input carries and the spine should
carry; (C) lists what already agrees. Spine references use its `AD-n` identifiers.

Ratified decisions driving this pass (`.memlog.md`, last entries): A-1 (immutable fallback
fingerprints input), T1/T2 (`workflow_dispatch` from exact `main` SHA; AD-16 as timeless lineage
invariant; `workflow_run` caller and REL-4 freeze retired), A-3 (`.hexalith/builds-execution/` local
uses), A-11 (path is evidence, identity is trust), T3/AD-17 (manifest v3), A-5 (fallback formula
identifiers), D-6 (missing closure is a gate failure), D-10 (`dependency-release-source.v1` is a
diagnostic only), S-2/S-3/AD-18, S-6 (ledger), R-6/R-9 (roster and bootstrap reduced).

---

## Input 1 — `_bmad-output/planning-artifacts/architecture.md`

### (A) Statements the spine contradicts or supersedes — 8 contradictions, 4 omissions

**P-A1. L99-101 — 2026-08-04 release-integration note (contradicts AD-13, AD-16, D-10).**

Current:
> **Release integration update 2026-08-04:** FrontComposer uses its truthful exact-source CI proof and
> the approved immutable Builds production workflow identity. It does not claim a shared CI evaluator
> closure that the current mutable CI reference cannot prove.

The spine now states the opposite on both points: the authenticated
`hexalith.dependency-release-handoff.v1` artifact is the sole release-candidate authority and the
source proof is a CI diagnostic only (AD-13); `ci.yml` and `release.yml` reference Builds by a literal
40-hex commit and the CI evaluator closure is sealed transitively through `evidence_sha256` (AD-13,
AD-17). The "mutable CI reference" no longer exists (`ci.yml` L25 is pinned).

Replace with:
> **Release integration update 2026-09-08 (supersedes 2026-08-04):** FrontComposer releases from an
> operator-dispatched exact `main` SHA authenticated against exactly one completed successful push-CI
> run and that run's sealed dependency-release handoff. Primary CI and release call Hexalith.Builds by
> literal 40-hex commits inside the owner-accepted lineage, so the CI evaluator closure is sealed, not
> merely claimed; the exact-source proof CI also emits is diagnostic only.

**P-A2. L132-136 — bootstrap procedure (superseded by AD-12 and R-9).**

Current:
> The one-time bootstrap requires an unchanged graph, frozen publication, and approval of the exact policy
> digest, enforced by the Release Owner-controlled `HEXALITH_DEPENDENCY_POLICY_BOOTSTRAP_SHA256`
> repository variable. Base-policy existence permanently disables bootstrap after the initial landing.

AD-12 reduces this to one historical sentence and adds that base-policy existence is a blob at the
canonical path regardless of validity, with malformed-policy recovery being a dated decision, never a
bootstrap. Naming the bootstrap variable as an active enforcement mechanism is now stale.

Replace with:
> The one-time v1 bootstrap was consumed when the first policy landed on 2026-07-19; no bootstrap mode
> is reachable. Base-policy existence means a blob at the canonical path regardless of validity; a
> malformed active policy fails closed and recovery is a dated Architect + Release Owner decision.

**P-A3. L138-139 — who records the policy coordinates (contradicts AD-13/AD-14, D-10).**

Current:
> Exact-source proofs and manifest v3 record its repository, canonical `eng/dependency-graph-policy.json`
> path, schema, 40-hex revision, and raw-byte SHA-256.

Replace with:
> The CI dependency-release handoff and manifest v3 record its repository, canonical
> `eng/dependency-graph-policy.json` path, schema, 40-hex revision, and raw-byte SHA-256.

**P-A4. L191-196 — manifest v2 paragraph (superseded by AD-14, AD-17, AD-9/A-1, A-5).**

Current:
> GOV-1 introduces the top-level `manifest_schema: hexalith.release-evidence.v2`, closed
> `dependency_graph`, closed `dependency_policy`, and closed `workflow_provenance` members atomically.
> The existing outer seal covers every top-level member except `seal`. V2 fallback approval hashes the
> existing definition/package-set inputs together with the dependency graph digest, active policy
> SHA-256, and canonical combined CI/release workflow-definition digest. Older manifests are audit-only and always non-publishable; they cannot satisfy fallback and are
> never upgraded or resealed in place. Historical ledger bytes remain unchanged.

Replace with:
> The manifest lineage is `hexalith.release-evidence.v1` → `v2` → `v3`; `prepare-manifest` emits only
> the current `hexalith.release-evidence.v3`, whose closed `dependency_graph`, `dependency_policy`, and
> `workflow_provenance` members landed atomically. The outer seal covers every top-level member except
> `seal`. Fallback approval hashes the existing definition/package-set inputs together with the
> dependency graph digest, active policy SHA-256, and the combined CI/release
> `workflow_provenance.definition_digest`; the approved value is an immutable Release Owner-recorded
> input captured outside any release run, recomputed live at every classification, and never written,
> defaulted, or rebound by a prepublish, release, or verification step. A v2 manifest is accepted for
> verification of historical evidence only; older manifests are audit-only and always
> non-publishable, cannot satisfy fallback, and are never upgraded or resealed in place. Historical
> ledger bytes remain unchanged.

**P-A5. L233-237 — source proof "replaces the pending AD-16 evaluator handoff" (contradicts AD-13, D-10; wrong AD number).**

Current:
> Push CI emits a closed `hexalith.dependency-release-source.v1` proof for facts FrontComposer can
> authenticate without inventing an upstream guarantee: the successful push-CI run/attempt, exact
> candidate, active base policy, and candidate dependency graph. This deliberately replaces the pending
> AD-16 evaluator handoff. It does not claim an immutable closure for the shared CI workflow while that
> workflow remains selected through a mutable reference.

The handoff is AD-13 (AD-16 is the Builds owner-accepted revision). The handoff is adopted and is the
sole candidate authority; the source proof never is. The CI workflow is pinned.

Replace with:
> Push CI uploads exactly one `dependency-release-handoff-<run_id>-<run_attempt>` artifact
> (`hexalith.dependency-release-handoff.v1`) binding the successful push-CI run/attempt, exact
> candidate and base/before revisions, the matched CI evaluator closure and its definition digest, the
> active base policy, and the candidate dependency graph. That authenticated handoff is the sole
> release-candidate authority. The closed `hexalith.dependency-release-source.v1` proof CI also emits
> is a diagnostic only; it is never release-eligible and never a candidate authority for manifest
> preparation.

**P-A6. L242 — release "fetches only the run/attempt-named source proof" (contradicts AD-13).**

Current:
> It fetches only the run/attempt-named source proof through read-only Actions APIs.

Replace with:
> It downloads only that run's `dependency-release-handoff-<run_id>-<run_attempt>` artifact (exactly
> one match, zip archive) through read-only Actions APIs, verifies it offline and live, and requires
> its recorded candidate to equal the dispatched SHA.

**P-A7. L248-251 — "binds the raw CI source-proof hash" (contradicts AD-17).**

Current:
> V3 provenance
> binds the raw CI source-proof hash, exact Release caller workflow bytes at the candidate, exact
> reusable `domain-release.yml` bytes at the Builds execution commit, and the identical
> `builds-execution-sha`.

Replace with:
> V3 provenance binds the authenticated CI run coordinates and the raw CI handoff JSON SHA-256 (which
> transitively binds the CI evaluator closure), exact Release caller workflow bytes at the candidate,
> exact reusable `domain-release.yml` bytes at the Builds execution commit, and the identical
> `builds-execution-sha`; `definition_digest` over `{ci, release}` is the fallback
> `workflow_definition` input.

**P-A8. L251-253 — "The approved Builds execution identity is `3f0e3595…`" (stale; contradicts AD-16 and the AD-12 no-mirroring rule).**

Current:
> The approved Builds execution identity is
> `3f0e3595be693fce56a37648c0bd0f89390f5fd3`; the reusable workflow call and input must remain identical
> literal 40-hex coordinates.

Live pin is `4eb33928a1d8c7775f97221cf9edc171db0cb5f8` (release.yml L17/L286/L321/L329) and the
spine forbids mirroring volatile policy rows in architecture. Name the lineage root, not the pin.

Replace with:
> The Builds execution identity is an immutable 40-hex commit whose lineage starts at the
> owner-accepted revision `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` (Hexalith.Builds issue 17,
> 2026-08-08) and advances only through pre-authorized closures in the active policy; the current pin
> lives in `release.yml` and the policy, not here. The reusable workflow call, the
> `builds-execution-sha` input, and the `.hexalith/builds-execution` checkout ref must remain one
> identical literal 40-hex value.

**Omissions the parent should add (spine carries, parent does not):**

- **P-O1 (after L131, AD-12 activation).** Add: "At release the active policy is the exact
  FrontComposer commit recorded in the authenticated CI handoff."
- **P-O2 (L221-223 or L255, AD-18).** Add: "Every job holding `contents: write`, `id-token: write`,
  `attestations: write`, or the NuGet publishing credential runs under the `production` environment
  with Release Owner review and records the dispatching actor; graph collection, semantic validation,
  and affected-module builds run secretless with a read-only token; `pull_request_target` is
  forbidden."
- **P-O3 (L261-265, AD-15).** Add: "Every governed Release attempt uploads one
  `release-verification-handoff-<run_id>-<run_attempt>` artifact under `if: always()` (or a deferred
  sentinel when no authenticated CI handoff was obtained); the post-release verifier derives the
  candidate only from that handoff and the sealed manifest, must match a `post_release` policy
  authorization, and records `frontcomposer.release-ledger-record.v2` for the Release Owner-appended
  REL-AI-1 ledger."
- **P-O4 (L152-153, A-11).** Add after "verifies the catalog graph hash": "Identity is the trust key;
  the gitlink path is graph evidence and a submodule move is a removed-plus-added logical edge."

### (B) Quiet requirements in the parent that the spine dropped

- **P-B1. Release/NuGet build mode (L150).** The parent requires every build row to run "in
  Release/NuGet mode" (memlog: `Configuration=Release`, `UseNuGetDeps=true`). Spine AD-8 and the
  Executable Policy section say only "literal argv" from the registry. The spine should state that
  affected-module argv must build in Release configuration with NuGet-resolved dependencies so a
  registry row cannot silently pass a Debug/project-reference build as the compatibility proof.
- **P-B2. Ownership boundaries (L276-284).** The parent assigns the reusable workflow contract and
  its minimum permissions to Hexalith.Builds, exceptions and partial-publication incident response to
  the Release Owner. Spine AD-18 asserts permissions statically from `ci.yml`/`release.yml`/
  `release-evidence.yml` without saying that the reusable `domain-release.yml` permissions are a
  Builds-owned contract input consumed through the AD-16 lineage, and the spine's Deferred entry
  covers incident runbooks but not "exceptions". Carry both: AD-18 scope is the FrontComposer caller
  workflows; reusable-workflow permissions are verified only by lineage acceptance; exceptions are
  Release Owner decisions.
- **P-B3. Durable public evidence chain (L272-274).** The parent says evidence attached at GitHub
  Release creation is the public chain and short-retention workflow artifacts are supplemental. The
  spine's AD-13/AD-15 trust chain re-authenticates run artifacts (S-7 relies on artifact
  immutability) and only the Deferred section notes they expire. The spine should say which GOV-1
  evidence is attached as a Release asset (sealed v3 manifest; whether the CI handoff copy is) and
  that the ledger record must remain verifiable after artifact expiry.

### (C) Statements that agree with the spine

- L49 (semantic compatibility, no SHA allowlist) — AD-6.
- L50-52 (approved GOV-1 amendment, depth 1 + 2) — AD-1; spine Inherited Invariants row 1.
- L95-97 (2026-07-19 ratification supersedes complete-reachable) — AD-1.
- L103-117 (paradigm, v1 boundary, compatibility, provenance) — AD-1, AD-4, AD-6.
- L119-123 (closed-world, records edges before dedup, never recursive init/clone/candidate commands) — AD-2, AD-3, AD-4, AD-10.
- L125-131 (offline/object-only engine, bare stores, base-only removals, policy owns trust, `.gitmodules` untrusted, PR/push activation) — AD-10, AD-12.
- L136-139 (zero before never release-eligible; policy is release-definition and fallback material) — AD-12.
- L141-148 (no implicit defaults; policy is sole executable authority; architecture does not duplicate rows) — AD-6, AD-12, Executable Policy section.
- L150-156 (isolated checkout, bounded contract-tree materialization, caps 16,384 / 16 MiB / 256 MiB, self-owned Builds tree) — AD-7, AD-8.
- L158-161 (depth-1 first, subsumption, collapse to FrontComposer) — AD-8.
- L163-173 (edge order, closed envelope, digest material, canonical bytes, not RFC 8785, offline vs live) — AD-4, AD-5, AD-9.
- L175-178 (SHA-1 only, ceilings measured before reads, Python owns semantic policy) — AD-5, AD-6, AD-7.
- L180-182 (self/back edges recorded, cache by repository/commit, per-selector evaluation) — AD-4, AD-6.
- L184-189 (PR/push revision model, merge-base equality, unchanged graph builds nothing) — AD-8.
- L198-201 (BUILD-CAT-1 marker migration) — AD-6, Deferred.
- L203-204 (decision record and spine links) — spine frontmatter.
- L208-223 (exact-artifact pipeline; only pre-publication authorizes) — Inherited Invariants rows 3-4, AD-9.
- L225-231 (sealed manifest binds v1 graph; blocked/invalid terminates before side effects) — AD-5, AD-9, AD-17.
- L239-244 except L242 (`workflow_dispatch`, `refs/heads/main`, 40-hex, live main re-read, exactly one successful push CI run, fail before protected job, no substitute candidate) — AD-13 (T1/T2).
- L246-248 (live graph recompute, real `160000` Builds gitlink as catalog identity independent of execution commit, emits v3) — AD-16, AD-17, Consistency "Builds identities".
- L255-259 (sealed to Release run/attempt, upload after production approval, reusable publisher re-verifies, tag resolves to dispatched SHA) — AD-13, AD-15, AD-18.
- L261-274 (run-topology authentication, partial-publication incident, no retroactive authorization, durable Release assets) — AD-15, AD-9.
- L286 (no runtime/UX change) — AD-11.

Spine-side notes (not input edits): the Inherited Invariants table cites "`architecture.md` GOV-1
section" for the no-runtime-change invariant, but that sentence lives at L286 inside "FR-24 Release
Evidence Architecture"; cite that section. The spine frontmatter still reads `status: draft`
although the memlog records user ratification of the 2026-09-08 decisions.

---

## Input 2 — `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`

### (A) Statements the spine contradicts or supersedes — 13 contradictions, 3 omissions

**C-A1. L19 frontmatter — "accepted immutable revision pending" (contradicts AD-16).**

Current:
> upstreamReleaseFollowUp: BUILD-REL-1 issue 17 (accepted immutable revision pending)

Replace with:
> upstreamReleaseFollowUp: BUILD-REL-1 issue 17 (owner-accepted immutable revision a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a, 2026-08-08; later pins by pre-authorized closure)

Also bump `amended:` (L6) to `2026-09-08` when these edits land, keeping `amendmentStatus: approved`
and the 2026-07-19 `ratified:` date.

**C-A2. L89-92 — bootstrap wording in decision 10 (superseded by AD-12, R-9).**

Current:
> The initial bootstrap requires an unchanged dependency graph, frozen publication, and
> Architect + Release Owner approval of the exact policy digest, enforced by the Release Owner-
> controlled `HEXALITH_DEPENDENCY_POLICY_BOOTSTRAP_SHA256` repository variable. Once the base contains
> policy, bootstrap is permanently unavailable.

Replace with:
> The one-time bootstrap was consumed when the first policy landed on 2026-07-19 and is unreachable;
> base-policy existence is a blob at the canonical path regardless of validity, and a malformed active
> policy fails closed with recovery by a dated Architect + Release Owner decision, never a bootstrap.

**C-A3. L110-111 — "The focused architecture spine contains the exact identity/profile and identity/solution/catalog-binding matrices" (contradicts AD-12 and R-6).**

Current:
> The focused architecture spine contains the exact identity/profile and
> identity/solution/catalog-binding matrices; missing or candidate-added entries fail closed under
> decision 10.

Replace with:
> The active `eng/dependency-graph-policy.json` is the sole executable registry of identity/profile
> and identity/solution/catalog-binding rows; the focused architecture spine states only their closed
> shapes and coverage invariants, and missing or candidate-added entries fail closed under decision 10.

**C-A4. L120-122 — release caller passes `workflow_run.head_sha` (contradicts AD-13, T1/T2).**

Current:
> The release caller passes `github.event.workflow_run.head_sha` as a required exact commit; the
> reusable workflow checks out and propagates that commit through preparation, sealing, verification,
> fallback, and publication.

Replace with:
> Release is operator `workflow_dispatch` on `refs/heads/main` only: the dispatched `github.sha` must
> be lowercase 40-hex, equal the live `main` ref re-read through the API, and equal the head of
> exactly one completed successful push-CI run selected through read-only Actions APIs; the reusable
> workflow checks out and propagates that authenticated commit through preparation, sealing,
> verification, fallback, and publication.

**C-A5. L123-126 — caller "passes the triggering CI run ID" and matches "the event head SHA" (contradicts AD-13).**

Current:
> The caller
> passes the triggering CI run ID and fetches the single versioned dependency-release handoff through
> the read-only Actions API only after repository/workflow/event/branch/conclusion/run/head metadata
> match and the recorded candidate equals the event head SHA.

Replace with:
> The caller selects the CI run ID/attempt for the dispatched SHA and downloads that run's single
> `dependency-release-handoff-<run_id>-<run_attempt>` artifact (zip, exactly one match) through the
> read-only Actions API only after repository/workflow/event/branch/conclusion/run metadata match and
> the recorded candidate equals the dispatched SHA; that handoff candidate is the sole release
> authority.

**C-A6. L131-133 — "Manifest CI provenance must equal those sources ... recomputes both CI-only and combined" (superseded by AD-17).**

Current:
> Manifest CI provenance must equal
> those sources, project the authenticated run, and bind the raw handoff JSON SHA-256; offline
> verification recomputes both CI-only and combined CI/release definition digests.

Replace with:
> Manifest CI provenance projects the authenticated run coordinates and binds the raw handoff JSON
> SHA-256, which transitively binds those sources; release recomputes the handoff's CI-only evaluator
> digest before acceptance and offline verification recomputes the combined CI/release
> `definition_digest`.

**C-A7. L133-135 — "current mutable CI/release `@main` calls ... REL-4 remains frozen" (retired by T1/T2; contradicts live pins).**

Current:
> The current mutable
> CI/release `@main` calls and missing release exact-ref input are non-conforming, so REL-4 remains
> frozen until this seam and its tests exist.

Replace with:
> Any `@main` or other mutable reference anywhere in the CI, release, or post-release closure is
> non-conforming and fails the gate; a closure absent from the active policy is a gate failure, never
> a soft deferral.

**C-A8. L138 — "the AD-13 closure ceilings" (ceilings now stated once in AD-7).**

Current:
> ambiguous/unsupported metadata syntax, cycles, or the AD-13 closure ceilings fail closed;

Replace with:
> ambiguous/unsupported metadata syntax, cycles, or the AD-7 workflow-closure ceilings fail closed;

**C-A9. L140-142 — decision 14 introduces v2 with action provenance in the manifest (superseded by AD-14, AD-17).**

Current:
> GOV-1 introduces required
> top-level `manifest_schema: hexalith.release-evidence.v2`, complete graph, closed policy coordinates,
> and closed caller/reusable/CI/action workflow provenance.

Replace with:
> GOV-1 introduces required top-level `manifest_schema` (lineage `hexalith.release-evidence.v1` →
> `v2` → current `v3`, with only the current schema produced), complete graph, closed policy
> coordinates, and closed workflow provenance `{ci: {run, evidence_sha256}, release: {caller,
> reusable, builds_execution_sha}, definition_digest}`; the CI action closure is bound through the raw
> handoff hash, not repeated in the manifest.

Then in L145-146 keep "Legacy manifests are audit-only..." and add: "A v2 manifest is accepted for
verification of historical evidence only and is never fallback-eligible for a new publication."

**C-A10. L159-162 — decision 16 "The accepted revision is currently pending ... remain blocked until" (superseded by AD-16 timeless invariant).**

Current:
> The accepted revision is
> currently pending. FrontComposer may proceed with local graph/policy work, but GOV-1 Tasks 4/5,
> story completion, release eligibility, and REL-4 unfreeze remain blocked until that revision and
> workflow/action blobs are recorded in the active policy.

Replace with:
> The owner-accepted revision is `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` (recorded 2026-08-08). It
> is the lineage root: every later execution pin is valid only when it and its exact workflow/action
> blob closure were pre-authorized in the active policy of an earlier change. No reusable-workflow
> integration, GOV-1 completion claim, or release eligibility exists against a reference outside that
> lineage.

**C-A11. L177-178 — "Publication stays frozen until release is wired..." (freeze retired; keep as timeless eligibility).**

Current:
> - Publication stays frozen until release is wired to the exact CI-tested commit and immutable reusable
>   workflow provenance.

Replace with:
> - Publication is eligible only when release is wired to the exact CI-tested commit and immutable
>   reusable workflow provenance; any drift from that wiring blocks the release before side effects.

**C-A12. L183 — "accepted immutable revision is a GOV-1 completion/unfreeze prerequisite" (superseded by AD-16).**

Current:
> - BUILD-REL-1 issue 17's accepted immutable revision is a GOV-1 completion/unfreeze prerequisite.

Replace with:
> - BUILD-REL-1 issue 17's accepted immutable revision (`a8a50859`) is the lineage root; every
>   Builds execution pin must descend from it through pre-authorized closures.

**C-A13. L208-209 — Verification "triggering successful CI head SHA" (contradicts AD-13).**

Current:
> - Release fails unless checkout, evidence, and publication all use the triggering successful CI head SHA
>   and sealed immutable reusable-workflow identity.

Replace with:
> - Release fails unless checkout, evidence, and publication all use the dispatched `main` SHA
>   authenticated against exactly one completed successful push-CI run's handoff, and the sealed
>   immutable reusable-workflow identity.

**Omissions the contract should add:**

- **C-O1 (decision 10, after L88, AD-12).** "At release the active policy is the exact FrontComposer
  commit recorded in the authenticated CI handoff."
- **C-O2 (decision 14, after L144, A-1).** "The approved fallback fingerprints digest is an immutable
  Release Owner-recorded input captured outside any release run; classification recomputes the
  formula live and fails closed on inequality, and no prepublish, release, or verification step may
  write, default, or rebind it."
- **C-O3 (decision 15, after L156, AD-15/S-6).** "An attempt that never obtained an authenticated
  CI handoff emits a deferred sentinel from which the verifier records a failed attempt; the machine
  ledger record is `frontcomposer.release-ledger-record.v2`, appended by the Release Owner to the
  REL-AI-1 ledger, and non-compliant or partial-publish dispositions are never retried to green."

### (B) Quiet requirements in the contract that the spine dropped

- **C-B1. Release/NuGet build mode (L112).** Same as P-B1: "exact static Release/NuGet restore/build
  argv" is a mode constraint the spine's AD-8 no longer states.

Everything else the contract quietly requires is carried: no SHA allowlist in Governance tests (AD-6),
actionable owner/commit/mismatch diagnostics (Consistency "Errors"), one module scheduled at most once
(AD-8), atomic schema/producer/verifier/fixture change (AD-9, AD-14), two-change trust expansion
(AD-12), no local Builds contingency without a dated decision (AD-16), and no edits to
`references/Hexalith.Builds` from FrontComposer (AD-16 Prevents).

### (C) Statements that agree with the spine

- L25-26 ratification note — AD-1.
- Decision 1 (L40-44) — AD-1.
- Decision 2 (L45-47) — AD-6.
- Decision 3 (L48-50) — AD-6; Consistency "Errors".
- Decision 4 (L51-53) — AD-8.
- Decision 5 (L54-56) — AD-4, AD-17.
- Decision 6 (L57-64) — AD-2, AD-3, AD-10 (spine adds: remotes reconstructed from canonical identity, content-address trust; optional refinement).
- Decision 7 (L65-73) — AD-4, AD-5.
- Decision 8 (L74-78) — AD-7.
- Decision 9 (L79-82) — AD-6, Deferred.
- Decision 10 except L89-92 (policy owner, untrusted `.gitmodules`, PR/push activation, delayed activation, zero-before diagnostics, Python owns semantics, fingerprint material, one authorization per closure, pre-authorize-then-switch) — AD-12.
- Decision 11 (L101-105) — AD-5, AD-8.
- Decision 12 except L110-111 (closed registries, seed roster, materialization caps, depth-1-first classification) — AD-7, AD-8, AD-12, Executable Policy section.
- Decision 13 L122-123, L126-131, L136-139 (active-policy-authorized immutable Builds commit sealed with caller hash; refs must match sealed coordinates; policy reloaded at recorded commit; 40-hex pinned transitive sources; Builds local actions from exact SHA checkout; static closure independent of conditions; Docker/dynamic/cycles fail closed; raw metadata blob SHA-256) — AD-12, AD-13 (A-3 adds the `.hexalith/builds-execution/` single-literal-ref rule as a refinement).
- Decision 14 L142-146 (seal over all members except `seal`; fallback digest inputs; legacy audit-only; ledger bytes unchanged; fixtures migrate atomically) — AD-9, AD-14.
- Decision 15 (L147-156) — AD-15.
- Decision 16 L162-163 (no local contingency without dated decision) — AD-16.
- Consequences L167-176, L179-182, L184-187 — AD-1, AD-6, AD-8, AD-9, AD-12, AD-15, AD-16, Deferred.
- Rejected Alternatives (L189-196) — consistent with AD-1, AD-6.
- Verification L200-207, L210-212 — AD-5, AD-6, AD-8, AD-9, AD-12, AD-14.

---

## Summary

| Input | Contradictions/supersessions | Omissions to add | Dropped requirements for the spine |
| --- | --- | --- | --- |
| `architecture.md` | 8 (P-A1..P-A8) | 4 (P-O1..P-O4) | 3 (P-B1 Release/NuGet mode; P-B2 ownership boundaries; P-B3 durable evidence chain) |
| `shared-catalog-dependency-governance-2026-07-19.md` | 13 (C-A1..C-A13) | 3 (C-O1..C-O3) | 1 (C-B1 Release/NuGet mode, same as P-B1) |
