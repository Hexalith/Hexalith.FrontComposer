# Reviewer Gate — code-drift / release-reality lens — pass 2 — 2026-09-09

**Reviewed artifact:** `ARCHITECTURE-SPINE.md`  
**Spine SHA-256:** `4a765adbf2026396d9d1258163a41f155522d8c3a7304d6d6857533a5a37624f`  
**FrontComposer baseline:** `053b2008307d4e476c0d4329e6c47763c301d43e`  
**Catalog gitlink:** `a32cb422749352cce8dec948aa3e78c8f00eb4cf`  
**Current Release reusable pin:** `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`

## Verdict

**PASS for architecture gating; FAIL for current conformance and release eligibility.** The current
spine now describes the brownfield state honestly and places every known split-publication drift
cluster behind explicit, fail-closed adoption gates. No Critical or High architecture contradiction
remains. In particular, the spine does not call HEAD conformant, requires an owner-accepted new Builds
revision, enumerates the missing register/source migrations, and defines a hash-bound, live-validated
implementation packet whose reviewer sidecars must all be PASS with zero Critical/High findings.

The implementation, sources, and operational controls have not passed those gates. Most urgently, a
read-only live query still returns `HEXALITH_RELEASE_PUBLISH_ENABLED=true`. The existing caller selects
the exact pinned reusable's legacy job, whose literal-true branch can run Semantic Release with
publication credentials. The spine explicitly classifies this as a Critical operational blocker and
forbids approval or dispatch until a later authenticated API observation records literal `false`.
This review did not change repository variables, settings, source, workflows, or policy.

## Counts

| Dimension | Critical | High | Medium | Low |
| --- | ---: | ---: | ---: | ---: |
| Architecture completeness / honesty | 0 | 0 | 0 | 0 |
| Expected implementation and source nonconformance | 0 | 4 | 0 | 0 |
| External operational blockers | 1 | 2 | 0 | 0 |

The implementation and operational counts are release-readiness findings, not defects in the reviewed
spine.

## Architecture gating assessment

The known drift is completely and actionably gated at architecture altitude:

- **Current state is not overstated.** Structural Seed says the target appears only after the split
  implementation gate and expressly names manifest v4, handoff v3, ledger v2, and incident recovery
  as current-to-target deltas. Adoption requires role acceptance, a new owner-accepted Builds
  revision in AD-16 lineage, the implementation gate, and the incident runbook/response target.
- **Every observed source family has a closure route.** Deferred requires reconciliation of
  `architecture.md`, FC-DEP-1, the GOV-1 story, G2 request, and the nonconformance register before
  implementation. Its discrete migration list covers the split reusable and delayed activation,
  caller/two-job topology, candidate environment removal, candidate production/authentication and
  hostile fixtures, handoff-v3/ledger, candidate-free publication/final classification, pinned
  post-release helpers, and duplicate destination names.
- **The implementation gate is executable and identity-bound.** The canonical packet fixes the
  validator path/command/check; binds the spine, FrontComposer, Builds, policy, caller, register,
  producer runs and artifacts; requires successful closed-allowlist checks and mixed-identity
  negatives; authenticates Release Owner approval in a protected packet PR; and persists the review
  through a second protected projection PR. Missing, expired, stale, forged, open-row, or nonzero
  Critical/High reviewer evidence fails.
- **Operational gaps cannot be mistaken for acceptance.** The live publish flag, no-bypass ruleset,
  incident controls, owner acceptance, and response target are separately named and release-blocking.

Consequently, the stale register and sources below do not reopen an architecture finding: they are
inputs that the architecture requires to be changed, hashed, reviewed, and closed before its named
gate can pass.

## Expected implementation and source nonconformance

### ICD2-01 — High — the current reusable/caller has no split privilege boundary

FrontComposer's `prepare-candidate` job is itself bound to `environment: production`. The reusable
call leaves `governed-release` unset/false and invokes `domain-release.yml@4eb33928…`, selecting the
callee's `jobs.release` legacy branch. That job is environment-bound and runs Semantic Release after
the literal publish-variable check. The callee's alternative `governed-release` remains one
environment-bound job that checks out candidate code, prepares/attests it, and then publishes with
write credentials. Neither implementation provides the required separately named
`release / build-publication-candidate` and `release / publish-publication-candidate` jobs or the
artifact-only handoff between a secretless candidate job and candidate-free publisher.

**Evidence:** `.github/workflows/release.yml:212-335`; exact pinned Builds
`.github/workflows/domain-release.yml:251-258,396-479,490-503,643-700,843-1035` at `4eb33928…`.

**Gate:** explicit Deferred rows plus owner-accepted Builds lineage, exact caller/two-job checks,
hostile-candidate evidence, packet review, and delayed policy activation. Release remains ineligible.

### ICD2-02 — High — helpers and active policy still implement the pre-split schemas

`dependency_handoff.py` declares `hexalith.release-verification-handoff.v1` rather than v3.
`release_evidence.py` declares current manifest v3 rather than v4 and retains prior fallback/source
authority behavior. The policy has only `ci`, `release`, and `post_release` evaluator stages and lacks
the required `incident_recovery`, `release_owner_logins`, `attestation_capability`, and split-candidate
archive ceilings. The current tests exercise this old contract; they cannot satisfy the new packet's
schema, hostile-candidate, attestation, archive, and external-effect checks.

**Evidence:** `eng/dependency_handoff.py:21-23`; `eng/release_evidence.py:64-68` and its existing
fallback/source paths; `eng/dependency-graph-policy.json` top-level keys, `resource_limits`, and
`evaluator_authorizations`.

**Gate:** the packet pins policy/implementation identities and requires all named schema, static,
hostile, package, repository, and mixed-identity checks to succeed. Missing files or old versions fail.

### ICD2-03 — High — post-release, incident, and ledger target controls are absent

The current `release-evidence.yml` begins from an ambient/default-branch checkout and later executes
repository-owned helpers rather than the exact candidate-free `post_release` closure required by
AD-12/AD-15. There is no `incident_recovery` evaluator stage, no
`.github/workflows/release-incident-preservation.yml`, and no `docs/release-incident-response.md`.
The release path does not implement the owner-controlled pre-side-effect `publication_started`
marker, closed handoff-v3 state matrix, durable incident evidence sequence, or protected two-PR ledger
sign-off/projection protocol.

**Evidence:** `.github/workflows/release-evidence.yml:20-80` and later candidate/helper execution;
missing incident workflow/runbook; active policy stage set above.

**Gate:** Adoption blocks on the approved runbook/response target; Deferred blocks integration on the
incident workflow; the conformance evidence requires gate-frozen and hostile runs plus final verifier
classification; reviewer and owner approvals must be projected through protected main.

### ICD2-04 — High — companion sources/register and the conformance packet remain unreconciled

The current register contains the earlier fallback and same-job migration model: for example NC-19
targets handoff v2, NC-25 makes manifest v3 current, and NC-28/29 describe the previous publication
state. It has none of the new discrete split closure rows and therefore cannot satisfy the required
`register_sha256` predicate. `architecture.md` still says manifest v3; the GOV-1 story and G2 request
retain manifest-v2, handoff-v1/v2, same-job Semantic Release, and older completion statements. The
canonical conformance packet and approval projection do not exist.

**Evidence:** `nonconformance-register-2026-09-08.md:1-45`; `architecture.md:138`; GOV-1 story
`56-86,141-158`; G2 request `48-88,160-198`; missing
`_bmad-output/implementation-artifacts/gov-1-split-publication-conformance.json` and
`gov-1-split-publication-conformance-approval.json`.

**Gate:** the spine requires source-owner reconciliation, an updated all-closed register, exact
packet/review artifacts, authenticated Release Owner approval, and the second approval projection.
Direct push or textual assertion cannot pass the gate.

## External operational blockers

### OPS2-01 — Critical — the authoritative emergency stop is still live-enabled

Read-only API result at review time:

```text
gh api repos/Hexalith/Hexalith.FrontComposer/actions/variables/HEXALITH_RELEASE_PUBLISH_ENABLED
=> {"name":"HEXALITH_RELEASE_PUBLISH_ENABLED","value":"true",
    "created_at":"2026-08-06T13:31:29Z","updated_at":"2026-08-06T13:31:29Z"}
```

The exact pinned legacy reusable treats only literal `true` as `publish-enabled=true` and conditionally
runs `npx semantic-release` with `GITHUB_TOKEN` and `NUGET_API_KEY`. Successful Release workflow
dispatches also exist as recently as 2026-09-07; a green run is not evidence that the new GOV-1 gate
passed. The production environment reviewer is a useful independent barrier, but the spine correctly
states that environment approval cannot imply the missing exception or adoption evidence.

**Required external action:** Release Owner or repository administrator sets the repository variable
to literal `false`; a later authenticated API query records that literal value and server timestamp.
Keep it false until every Adoption predicate passes. No mutation was made by this review.

### OPS2-02 — High — protected-main conformance/ledger approval cannot yet be enforced

The live rulesets API returns `[]`, the main branch-protection API returns HTTP 404, and no CODEOWNERS
file exists. Therefore the required no-bypass protected-main packet PR, approval projection PR, and
ledger sign-off projection cannot yet demonstrate the architecture's unchanged-head/code-owner
conditions.

**Required external action:** install the no-bypass main ruleset with required PR, named checks, and
code-owner review over the governed paths, then capture that live state in the gate/ledger evidence.

### OPS2-03 — High — release-owner incident readiness and acceptance remain open

The production environment exists, has required reviewer `jpiquot`, `can_admins_bypass=false`, and a
custom main deployment policy. However, the approved incident runbook, candidate-free recovery
workflow, measurable response target, Product Owner acceptance, and Release Owner acceptance required
by Adoption are not evidenced in the repository. The missing conformance packet and its owner approval
also independently prevent eligibility.

**Required external action:** owners record the required acceptances/response target and merge the
runbook/recovery controls before split integration; after the implementation and ruleset exist, run
the exact packet/approval workflow.

## Read-only checks performed

- Recomputed the spine SHA-256 and FrontComposer/Builds identities.
- Compared the current caller, release-evidence workflow, helpers, tests/policy surface, register, GOV-1
  story, G2 request, and parent architecture with AD-12/AD-15/AD-16/AD-17/AD-18/AD-19 and Deferred.
- Read the exact `domain-release.yml` blob at the execution pin `4eb33928…`, not the catalog worktree
  version.
- Queried the repository variable, production environment, rulesets, main protection, and recent
  Release workflow runs through read-only GitHub APIs.
- Confirmed the inspected workflow/helper/policy/test files have no worktree diff from FrontComposer
  HEAD. Existing unrelated user changes were left untouched.

## Scope note

No split reusable or conformance packet exists, so this review could not execute the future job-name,
artifact, archive-limit, attestation-claim, hostile-candidate, publication-order, owner-approval, or
incident-recovery proofs. That absence is an expected fail-closed readiness result and is fully covered
by the named implementation gate; it is not a hidden architecture gap.
