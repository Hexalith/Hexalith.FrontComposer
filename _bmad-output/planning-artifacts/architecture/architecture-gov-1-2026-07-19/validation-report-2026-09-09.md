# Reviewer Gate — Consolidated Validation Report — GOV-1 Architecture Spine

| Field | Value |
| --- | --- |
| Spine | `ARCHITECTURE-SPINE.md` — GOV-1 dependency provenance, 18 ADs, `status: final`, `updated: 2026-09-08` |
| Altitude | Epic — GOV-1 |
| Validated | 2026-09-09 (`bmad-architecture` VALIDATE) |
| Gate composition | Deterministic lint + rubric walker + configured current-fit and adversarial lenses + ad-hoc implementation-drift and security/trust-boundary lenses |
| Source integrity | Spine SHA-256 at review start: `20efd2722bb0befd6a5ecb8362045b54c2bfc7cead15bf0906cd5009465f40a2` |

## Gate verdict

**FAIL — architecture update required before this spine can be treated as a safe publication contract.**

The bounded committed-object graph core is mechanically clean and semantically strong, but the release
half has one Critical architecture contradiction and four merged High convergence gaps. Separately, the
current implementation fails the spine: one Critical fallback path defeats drift invalidation, four High
implementation clusters remain open, and two present gaps are missing from the existing nonconformance
register. The `production` environment still prevents the current non-admin writer from publishing without
a Release Owner approval; the gate failure is that approval presently admits candidate-controlled code into
a publication-credentialed job and that post-release evidence can be evaluated by later default-branch code.

| Axis | Verdict | Critical | High | Summary |
| --- | --- | ---: | ---: | --- |
| Deterministic mechanics | **PASS** | 0 | 0 | `lint_spine.py`: 0 findings |
| Spine convergence / safety | **FAIL** | 1 | 4 | Privilege boundary, governed-attempt classifier, canonical bytes, ledger state machine, post-release evaluator-code identity |
| Implementation conformance | **FAIL** | 1 | 4 | Fallback, governed reusable mode, quality/closure gates, post-release authorization, handoff/ledger implementation |

Raw reviewer counts are not additive because the independent lenses intentionally overlap: **3 Critical,
12 High, 12 Medium, 8 Low** before consolidation.

## Lens scoreboard

| Lens | Verdict | C | H | M | L | Full review |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| Deterministic lint | PASS | 0 | 0 | 0 | 0 | `lint_spine.py` JSON output: `ok: true` |
| Rubric walker | PASS WITH FINDINGS | 0 | 0 | 3 | 5 | [`reviews/review-rubric-validate-2026-09-09.md`](reviews/review-rubric-validate-2026-09-09.md) |
| Current fit / reality check | FAIL | 1 | 4 | 2 | 1 | [`reviews/review-current-fit-validate-2026-09-09.md`](reviews/review-current-fit-validate-2026-09-09.md) |
| Adversarial divergence | FAIL | 0 | 3 | 3 | 0 | [`reviews/review-adversarial-validate-2026-09-09.md`](reviews/review-adversarial-validate-2026-09-09.md) |
| Implementation drift | PASS WITH FINDINGS (spine) / FAIL (implementation) | 1 | 3 | 3 | 2 | [`reviews/review-code-drift-validate-2026-09-09.md`](reviews/review-code-drift-validate-2026-09-09.md) |
| Security / trust boundary | FAIL | 1 | 2 | 1 | 0 | [`reviews/review-security-validate-2026-09-09.md`](reviews/review-security-validate-2026-09-09.md) |

## Critical and High findings — consolidated

### Architecture contract

| # | Severity | Reviewer IDs | ADs | Finding | Disposition |
| --- | --- | --- | --- | --- | --- |
| A1 | **Critical** | SEC-VAL-01 | AD-13, AD-18, Deferred | **AD-18 promises that a write-scoped job does not consume candidate code, but its Rule does not impose a privilege split.** The pinned reusable checks out the candidate and executes restore/build, caller-provided shell, and Semantic Release in a job holding GitHub write scopes, OIDC/attestation authority, `NUGET_API_KEY`, and signing material. Release Owner approval is authorization, not a sandbox or artifact boundary. | **Discuss, then update.** Require a secretless/read-only builder producing a run-bound authenticated artifact and a separate protected publisher/signer that treats packages as data, runs only pinned owner-controlled code, and never checks out or executes candidate source. Update AD-13, AD-17, and AD-18 together. |
| A2 | **High** | ADV-2026-09-09-01 | AD-15, AD-16, AD-18 | **“Governed attempt” has no normative post-release classifier.** The Builds reusable exposes legacy and governed publication jobs; two verifiers can inspect different job names and disagree whether mandatory partial-publication checks apply. | **Discuss, then autofix.** Pin the authenticated job-topology vocabulary and a total governed-attempt predicate independent of one inner job name. |
| A3 | **High** | ADV-2026-09-09-02 | AD-5, AD-9, AD-12, AD-14, AD-17 | **Canonical JSON does not select one legal escape spelling.** `\"` and `\u0022` can encode the same accepted path while producing different graph, closure, fallback, workflow, and seal digests. | **Autofix.** Bind canonical bytes to one exact encoder contract or pin every escape form; add golden vectors for quotes, backslashes, controls, DEL, BMP, and supplementary code points. |
| A4 | **High** | ADV-2026-09-09-03, RW-03 | AD-15, AD-18 | **The closed ledger lists members but not its state machine or immutable identity.** `disposition`/`incident` vocabularies, field types, the Release-attempt primary key, verification-rerun policy, and projection into REL-AI-1 are undefined. | **Autofix.** Define closed types/enums, a total input-to-disposition mapping, immutable Release-attempt identity, append-only rerun semantics, and “incident can never be relabelled green.” |
| A5 | **High** | SEC-VAL-02 | AD-12, AD-15, Deferred | **Post-release ledger decisions may execute later default-branch helper code.** `release-evidence.yml` checks out ambient default branch before running the helpers that accept evidence and classify incidents; AD-12 excludes those helpers from fingerprints and does not otherwise bind their executable bytes. | **Autofix.** Put every executable post-release helper in the `post_release` evaluator closure (or use an exact pre-authorized helper tree), record its blob identities, and forbid evaluating a release with later default-branch code. |

### Current implementation

| # | Severity | Reviewer IDs | ADs / register | Finding | Disposition |
| --- | --- | --- | --- | --- | --- |
| I1 | **Critical** | CF-2026-09-09-01, ICD-01 | AD-9; NC-1/2/3/24 | **The publish-capable classifier overwrites the Release Owner approval digest with the live digest before comparing.** The workflow also does not forward the owner-recorded variable, and fingerprints use working-tree bytes plus a missing-file sentinel. Drift is re-authorized instead of invalidating the fallback. | **Fix before any fallback release.** Remove rebinding, hash exact candidate blobs, fail on missing input/blob, forward the owner value, and add production-path mutation fixtures. |
| I2 | **High** | CF-2026-09-09-02, SEC-VAL-03 | Inherited BUILD-REL-1, AD-13/16/18; not registered | **The caller explicitly selects the legacy reusable path**, omitting `governed-release: true` and the exact governed inputs. A release can therefore claim BUILD-REL-1 provenance while not using that mode. | **Do not flip the flag yet.** First resolve A1 and obtain a split-phase owner-accepted reusable; then pre-authorize it and switch in a later governed change. Add as the next register item (`NC-32` in this report; see register collision below). |
| I3 | **High** | CF-2026-09-09-03, ICD-04 | AD-12/13/16/18; NC-18/20–23/28/30 | **Release authenticates only `ci.yml`, not the same-head `quality.yml` run; trust-bearing assertions remain outside the authorized CI closure; row-closure and lineage checks are absent.** | Implement the exact quality coordinate, move trust-bearing checks into `ci.yml`, recompute added/changed rows, close checkout-injection/equality checks, and verify AD-16 lineage. |
| I4 | **High** | CF-2026-09-09-04, ICD-02 | AD-12/13/15; NC-4–9 | **Evaluator authorization is soft or absent across CI, Release, and post-release.** Active `post_release` rows have the wrong shape, miss the current caller, and are not enforced; the verifier trusts the embedded CI copy rather than independently downloading it. | Fix helper support and policy in AD-12 two-phase order, then enforce exact post-release authorization and independent CI-artifact authentication. |
| I5 | **High** | CF-2026-09-09-05, ICD-03 | AD-15/18; NC-10/12/19/26/29 | **The handoff and ledger code still predates the v2 contract.** Publication-start/gate state, evaluator, actors, environment protection, authenticated run projections, and one typed ledger writer are missing. | Implement handoff v2 and one closed ledger producer/validator after A2/A4 are fixed in the spine. |

## Medium and Low tail

### Architecture interoperability and hygiene

- **RW-01 / ADV-2026-09-09-04 (Medium):** `quality_run` is required in the ledger but has no
  authenticated carrier. Add it to the Release handoff and authenticate/copy it, or define a precise
  re-selection algorithm.
- **RW-02 (Medium):** CI/quality run uniqueness counts “all completed” in one source and “successful
  completed” in others. Decide the failed-run + successful-rerun case and reconcile the parent and
  FC-DEP-1.
- **ADV-2026-09-09-05 (Medium):** `release_run.conclusion` lacks a total projection over named `needs`
  results, including `skipped`, cancellation, missing, and unknown jobs.
- **ADV-2026-09-09-06 (Medium):** the Builds execution-SHA equality set lacks a time/scope rule for
  historical or recovery verification after a lineage advance.
- **SEC-VAL-04 (Medium):** incident detection has no binding containment owner, immediate freeze action,
  acknowledgement target, or runbook location/reopen trigger.
- **RW-04 (Low):** define the exact nested `environment_protection` shape and observation time.
- **RW-05 (Low):** name the embedded CI copy `dependency-release-handoff.json` and reject duplicate
  normative matches.
- **RW-06 (Low):** make the Builds execution-identity convention explicitly select the active-policy
  `release` row; keep the CI pin distinct.
- **RW-07 (Low):** add a frontmatter binding for the inherited no-runtime/API/package/UX invariant.
- **RW-08 (Low):** the GOV-1 story and G2 request remain declared sources while carrying superseded v2,
  freeze, pending-integration, and `@main` wording. Keep the recorded deferral and reconcile through
  their owners.
- **SCD-01 / SCD-02 (Low):** the module dependency diagram asserts several nonexistent imports and omits
  real ones; the seed says `tests/ci-governance/fixtures/` contains policy fixtures when it currently
  contains manifest fixtures.

### Implementation and version tail

- **CF-2026-09-09-06 / ICD-05 (Medium):** publication-oriented helper commands still accept manifest v2,
  and the diagnostic source proof can still act as candidate authority (NC-11/25).
- **CF-2026-09-09-07 (Medium):** no executable Governance path verifies the AD-16 first-parent lineage
  predicate (NC-30).
- **ICD-06 (Medium):** required canonical digest, definition-list, and permission/environment fixtures
  are absent (NC-13–15).
- **ICD-07 (Medium, new):** the handoff validator accepts duplicate asset names when fields differ in
  hash or size. Add the verifier rule/hostile fixture as **NC-31**.
- **CF-2026-09-09-08 (Low):** local/pinned .NET SDK is `10.0.400`; upstream SDK `10.0.401` shipped
  2026-09-08. Keep the row explicitly local/pinned-floor or record the resolved CI SDK.

## Nonconformance-register reconciliation

The implementation-drift lens rechecked `nonconformance-register-2026-09-08.md` against HEAD
`053b2008`:

- **27 active implementation items:** NC-1–15, NC-18–26, NC-28–30.
- **3 advisory/deferred entries:** NC-16 is a prune/retain policy decision; NC-17 is unreachable cleanup;
  NC-27 is the explicitly accepted/deferred live repository-control state.
- **0 resolved entries.**
- **Two present gaps are not registered:** duplicate asset-name acceptance (**NC-31**) and legacy rather
  than BUILD-REL-1 governed mode (**NC-32**). Reviewer SEC-VAL-03 proposed “NC-31 or next”; this report
  reserves NC-31 for ICD-07 and uses NC-32 for the mode-selection gap.
- **One architecture-first gap:** SEC-VAL-02 should gain its own implementation row only after the
  `post_release` helper-identity rule is amended, so the row targets the corrected contract.

This validation does not edit the register or implementation.

## Evidence that still holds

- AD-1–AD-8 and AD-10 remain a strong, implemented graph/catalog core: exact committed objects,
  closed-world identity, depth 1+2, closed envelopes, bounded failure, semantic/provenance separation,
  explicit revision selection, and isolated acquisition.
- AD-9's **text** correctly defines immutable, run-bound fallback approval and fail-closed ordering; the
  Critical item I1 is implementation nonconformance.
- The manual dispatch is bound to live `main` and exact CI run coordinates before protected work. Live
  `production` restricts deployment to `main`, has a required Release Owner reviewer, and disallows admin
  bypass; no GOV-1 workflow grants `actions: write`.
- Trust-bearing CI/Release `uses:` references are 40-hex pins; artifacts are run/attempt-bound and use the
  immutable artifact API generation.
- Manifest v3 production and its CI/Release provenance projection are current. Published GitHub assets
  are immutable/digested and NuGet-served packages are independently compared and repository-signature
  verified.
- Focused evidence passed: the lenses independently reported **180/180**, **190/190**, and **44/44** test
  executions (overlapping suites; not summed). These prove covered parser/contract behavior, not the
  missing controls above.
- Tooling remains suitable. Git 2.55.0, Python 3.14.7, and `actions/upload-artifact` 7.0.1 were verified
  current; .NET SDK 10.0.401 is the one-day upstream movement noted above.

## Recommended update order

1. **Resolve the publication privilege boundary (A1) first.** It changes the external reusable contract
   and determines what “governed mode” must mean.
2. Amend the spine's post-release contract together: A2/A4/A5, authenticated `quality_run`, conclusion
   truth table, historical verifier pin scope, and incident containment.
3. Pin canonical bytes (A3) and add cross-language hostile/golden vectors before any schema/digest work.
4. Add NC-31/NC-32 and the eventual post-release-helper row to the register; sequence implementation by
   the AD-12 two-phase activation rules.
5. Re-run this five-lens gate before returning the spine to `status: final` or claiming publication-control
   conformance.

## Next action

Run **`bmad-architecture UPDATE`** against this workspace, resuming from `.memlog.md` and keeping
AD-1…AD-18 stable. The update needs a Release Owner/Architect decision on A1 before drafting the
split-phase rule; the remaining clear wording/schema fixes can then be applied in the same update.
