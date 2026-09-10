# Reviewer Gate — brownfield implementation-drift lens — 2026-09-09

**Subject:** `ARCHITECTURE-SPINE.md` SHA-256
`6c4ab34cea9a3be2237e7d9f7017effcfc292fe6326eb464e075f8ee2bd0948b`

**Repository baseline:** FrontComposer HEAD
`053b2008307d4e476c0d4329e6c47763c301d43e`; current Builds catalog gitlink
`a32cb422749352cce8dec948aa3e78c8f00eb4cf`; release execution pin
`4eb33928a1d8c7775f97221cf9edc171db0cb5f8`.

## Verdict

**FAIL — the stated release halt is not true in the live brownfield system.** The target split is
substantially clearer and the current legacy implementation is not falsely described as conforming,
but the only stop named for the pre-adoption interval is live-set to `true`. The current caller selects
the exact pinned reusable's legacy publication job, and that job interprets the same value as explicit
permission to run Semantic Release with GitHub and NuGet credentials. This is a Critical
architecture/reality contradiction, not merely expected implementation drift.

After the live variable is set to literal `false`, the target implementation remains correctly
release-ineligible in principle. Its closure handoff is nevertheless not yet complete or mechanically
actionable: the named register still describes the prior same-job/v1-v3 design, the spine only asks for
eight broad future rows, and `implementation gate passes` has no named command, owner, or evidence
artifact proving that all old and new rows are closed.

## Counts

| Class | Critical | High | Medium | Low |
| --- | ---: | ---: | ---: | ---: |
| Architecture/reality and handoff findings | 1 | 1 | 1 | 0 |
| Expected brownfield implementation clusters (not spine contradictions) | 0 | 3 | 0 | 0 |

## Architecture/reality and handoff findings

### CDR-01 — Critical — the mandatory production halt is live-enabled

**Affected text:** Adoption and Release Eligibility, lines 76–86; Deferred companion/source
reconciliation, lines 1004–1015.

The spine says that production releases halt until owner acceptance, a new split Builds revision,
implementation-gate passage, and the approved incident runbook exist. It names
`HEXALITH_RELEASE_PUBLISH_ENABLED=false` as the authoritative emergency stop and says no exception is
authorized. A read-only live query at review time contradicts that safety premise:

```text
gh api repos/Hexalith/Hexalith.FrontComposer/actions/variables/HEXALITH_RELEASE_PUBLISH_ENABLED
=> {"name":"HEXALITH_RELEASE_PUBLISH_ENABLED","value":"true",...}
```

This value reaches executable publication code. FrontComposer's caller omits `governed-release`, so
the exact pinned `domain-release.yml` selects `jobs.release` via
`if: ${{ !inputs.governed-release }}`. That protected legacy job reads the repository variable,
sets `publish-enabled=true` on a literal `true`, and runs `npx semantic-release` with
`GITHUB_TOKEN`/`NUGET_API_KEY`. The production environment requires owner review and has no admin
bypass, but environment approval is not the spine's missing split/implementation gate and the spine
expressly says it cannot imply an exception.

**Evidence:** `ARCHITECTURE-SPINE.md:76-86`; `.github/workflows/release.yml:307-335`; exact pinned
Builds `.github/workflows/domain-release.yml:251-258,396-412,460-479` at `4eb33928…`; live repository
variable response above.

**Disposition:** **external state correction, then verify.** Release Owner/admin must set the
repository-level value to the literal string `false` immediately and retain read-only verification
evidence. Keep it false until every Adoption predicate is evidenced. This review did not mutate
repository settings.

### CDR-02 — High — the implementation handoff is incomplete and has no executable pass criterion

**Affected text:** Adoption and Release Eligibility, lines 76–86; Deferred companion/source
reconciliation, lines 1004–1015.

The nonconformance register is still explicitly bound to HEAD `053b2008`, but its requirements are
from the earlier architecture: `NC-19` asks for handoff v2 rather than current v3, `NC-25` makes v3
the publication schema rather than current manifest v4, and `NC-1`–`NC-3` describe the prior loose
fallback fields rather than the closed fallback-authorization v2 record and policy capability.
It has no discrete rows for several current release blockers, including:

- `release_owner_logins` and `attestation_capability` policy migration plus delayed activation;
- the exact `actions/attest@f7c74d28…` checksum-subject contract and attestation claim verifier;
- all publication-candidate archive ceilings and hostile ZIP fixtures;
- the draft-upload/verify-NuGet-publish sequence and mandatory durable Release assets;
- the `prepublication_denial` classifier, exact owner-controlled `publication_started` marker, and
  final handoff-v3 state matrix;
- append-only ledger sign-off production and the ruleset/runbook preconditions.

The Deferred section honestly says the register **must** gain eight split-migration rows and keeps the
release ineligible until owners reconcile them. However, those eight labels are broader than the
omitted closed contracts above, and Adoption's phrase `the implementation gate passes` identifies no
tool, workflow check, accountable owner, checklist version, or immutable evidence. Two implementers
can therefore close different subsets and both claim that the unnamed gate passed.

**Evidence:** `ARCHITECTURE-SPINE.md:76-86,235-278,485-650,708-839,990-1015` versus
`nonconformance-register-2026-09-08.md:1-45`.

**Disposition:** **fix the implementation handoff before coding/integration.** Reconcile the register
against this exact spine hash, assign one closure row for every independently testable migration
bundle (splitting broad rows where their evidence differs), supersede stale version/fallback wording,
and define `implementation gate` as a named, owner-approved checklist or command whose output records
the spine hash, selected Builds commit, policy commit/hash, tests, and every closed row.

### CDR-03 — Medium — ledger sign-off persistence requires an unstated second-PR protocol

AD-15 requires the approving review to cover an unchanged PR head that is merged to protected
`main`, but the sign-off record contains that review's server-generated ID/time and is appended only
after the review. It therefore cannot be present in the head that review approved. Once the required
no-bypass ruleset exists, pinned tooling also cannot append the record directly to `main`. A feasible
implementation needs two stages: merge the reviewed observation/approval request, then open and merge
a separately reviewed append-only sign-off-record PR whose record authenticates the first review.
The spine does not identify that protocol or distinguish the approval PR from the persistence PR.

**Evidence:** `ARCHITECTURE-SPINE.md:619-642,990-1003`.

**Disposition:** **clarify before sign-off automation.** Define the two immutable PR coordinates,
which review is carried in `approving_review`, who may approve the persistence PR, and which merge
commit/path proves the append. Do not attempt to commit the sign-off record onto the already approved
head; that would invalidate the recorded head approval.

## Expected implementation nonconformance — release-blocking, not architecture contradictions

### ICD-01 — High — exact pinned Builds reusable has no split privilege boundary

The exact release pin provides a default legacy job and one opt-in `governed-release` job, not the
required `release / build-publication-candidate` plus
`release / publish-publication-candidate` topology. Both current paths run candidate-controlled
checkout/setup/Semantic Release in an environment-bound write-capable job; the governed job also
loads signing secrets and attests before publishing in that same job. FrontComposer additionally runs
its own `prepare-candidate` under `environment: production` and leaves the reusable's governed input
unset, selecting the legacy path. This directly violates AD-18/AD-19, but the spine correctly requires
a new owner-accepted Builds revision, delayed activation, caller switch, and removal of production
from candidate/build jobs.

**Evidence:** `.github/workflows/release.yml:212-305,307-335`; exact pinned Builds
`domain-release.yml:250-258,490-503,643-686,843-1035`; `ARCHITECTURE-SPINE.md:758-839,1004-1015`.

**Disposition:** **expected migration; never waive.** Implement in Builds, accept into AD-16 lineage,
pre-authorize policy closure, then switch the FrontComposer caller in a later policy-governed change.

### ICD-02 — High — helpers and policy still implement handoff v1 / manifest v3 / old fallback

`dependency_handoff.py` declares release-verification handoff v1 and lacks the current
publication-candidate, quality-run, attestation, prepublication-denial, owner marker, and v3 state
contract. `release_evidence.py` emits manifest v3, still accepts the older fallback object, still has
the live-digest rebinding path, and permits source proof as an authority. The active policy lacks both
new top-level authorization members and publication-candidate resource ceilings. The 70 focused
Python tests passed, confirming that this old contract is internally tested; they do not demonstrate
conformance with the new spine.

**Evidence:** `eng/dependency_handoff.py:21-23,275-379`; `eng/release_evidence.py:64-68,1537-1556,
2322-2338,2472,2728-2743,3403,3834`; `eng/release_prepublish.py:400-411,617-664`;
`eng/dependency-graph-policy.json` top-level keys/resource limits; focused command:
`python3 -m unittest tests.eng.test_dependency_handoff tests.eng.test_release_contract
tests.eng.test_release_disposition tests.eng.test_release_evidence_v2
tests.eng.test_release_prepublish` — **70 passed**.

**Disposition:** **expected migration.** Replace the release data plane atomically with publication
candidate v1, manifest v4, handoff v3, ledger v2, fallback authorization v2, exact policy keys/limits,
and fixtures; do not treat the passing legacy suite as the implementation gate.

### ICD-03 — High — post-release verification executes ambient/candidate helpers

The current `release-evidence.yml` first checks out an ambient default branch, later checks out the
released candidate, initializes candidate dependencies, and invokes repository helpers. It validates
the Release-embedded CI copy rather than independently authenticating the CI artifact, has no active
`post_release` closure check, and emits ad-hoc ledger JSON. That is the opposite of AD-12/AD-15's
candidate-free, exact-pinned, read-only verifier. The spine identifies pinned post-release helper
execution and handoff/ledger migration as closure bundles, so this is expected drift; it remains
release-blocking.

**Evidence:** `.github/workflows/release-evidence.yml:20-35,115-207,220-334,392-424`;
`eng/dependency-graph-policy.json` current `post_release` rows;
`ARCHITECTURE-SPINE.md:559-650,340-360,1004-1015`.

**Disposition:** **expected migration.** Move all executable verification logic into the exact
active-policy-authorized post-release closure and independently authenticate every source artifact
before parsing it as data.

## Confirmed honest/sound parts

- The spine calls itself `draft`, states that owner acceptance is open, and does not claim the current
  release pin implements the split.
- The target two-job boundary, artifact crossing, owner-controlled pre-side-effect marker, final
  candidate-free classification, immutable draft sequence, durable asset set, and incident mapping are
  closed enough to implement once the handoff ledger is reconciled.
- Existing graph/semantic GOV-1 behavior is not invalidated by the split design; the focused release
  helper tests passed and the current divergences are concentrated at the release integration seam.
- Current repository/environment facts otherwise align: `production` has required reviewer
  `jpiquot`, `can_admins_bypass=false`, and a custom `main` branch policy; the current exact Builds pin
  and catalog gitlink are correctly treated as independent identities.

## Incomplete lower-severity checks

- No split reusable exists at an accepted Builds commit, so runner-level output names, job check names,
  artifact digest propagation, archive-limit behavior, attestation claims, draft publication order,
  and hostile-candidate resistance could not be executed end to end.
- The full 190-test governance suite was not rerun in this lens; 70 focused release-helper tests passed.
- Live repository variables and environment settings are mutable external state. The values above are
  observations at review time and must be re-queried by any later release-eligibility decision.

