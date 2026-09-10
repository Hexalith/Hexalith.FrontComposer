# Current-Fit Review (VALIDATE) — GOV-1 Architecture Spine — 2026-09-09

**Lens (verbatim):** "Verify every committed decision was web-researched or reality-checked rather
than asserted from training data: current library/framework versions, that each named technology
still exists and fits, and — greenfield — the live defaults of any starter it leans on. Flag anything
that could be out of date and wasn't confirmed against the web, the existing project, or the current
starter."

**Subject:** `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`
(status `final`, updated 2026-09-08), reviewed against root HEAD `053b2008`, the uncommitted spine
update, current repository code/configuration, pinned Hexalith.Builds workflow bytes, and live GitHub
state. This is a brownfield review; no starter is involved.

## Verdict: FAIL

The Git/Python/.NET/GitHub Actions choices still exist and fit, and the graph core (AD-1–AD-8,
AD-10) remains grounded in executable code and tests. The release seam does not fit the current
brownfield system: the spine intentionally records a hardened target while the repository still
implements the already-listed NC-1..NC-30 backlog. One live fallback path violates AD-9's primary
authorization invariant, and four additional High findings mean the `final` spine must not be read as
a description of controls currently enforced in production.

## Findings

| ID | Severity | AD / section | Finding | Evidence | Disposition |
| --- | --- | --- | --- | --- | --- |
| CF-2026-09-09-01 | **Critical** | AD-9; NC-1/NC-2/NC-3/NC-24 | **The immutable fallback-approval digest is still rebound to live inputs by the publish-capable classifier.** `classify_release()` calls `classify_release_payload(..., bind_live_fallback_digest=True)`; that branch overwrites `approved_against_fingerprints_sha256` with the just-computed digest before validation. `fallback_invalidation_fingerprints()` also hashes working-tree files and substitutes `sha256("missing")`, contrary to the exact-candidate-blob/no-sentinel rule. The workflow does not forward `vars.RELEASE_ATTESTATION_FALLBACK_FINGERPRINTS_SHA256`. This defeats the design's graph/policy/workflow drift invalidation once the remaining fallback variables are provisioned; live repository state currently limits exposure because none of the `RELEASE_ATTESTATION_*` variables exists. | `eng/release_evidence.py:2232-2269,2472,2723-2743,3834`; `.github/workflows/release.yml:282-290`; live `GET /repos/Hexalith/Hexalith.FrontComposer/actions/variables` returned only `HEXALITH_RELEASE_PUBLISH_ENABLED`. The project already records this as NC-1..NC-3/NC-24. Python's supported SHA-256 and deterministic JSON APIs remain suitable; see the official [`hashlib`](https://docs.python.org/3/library/hashlib.html) and [`json`](https://docs.python.org/3/library/json.html) documentation. | **Update implementation before any fallback release.** Remove live rebinding, read exact candidate blobs, fail on a missing blob/value, forward the owner-recorded digest, and add the production-path mutation fixtures required by AD-9. |
| CF-2026-09-09-02 | **High** | Inherited BUILD-REL-1 invariant; AD-13, AD-18 | **FrontComposer still invokes the pinned reusable's legacy job, not BUILD-REL-1 governed mode.** The caller omits `governed-release`, `release-commit`, CI coordinates, and the governed candidate/evidence inputs; its own comment says the governed job is left unset/false. The pinned workflow therefore selects `release`, while the spine inherits the contract that the reusable authenticates the exact unsigned candidate/evidence in governed mode. Pinning the workflow SHA is sound, but it does not activate the named mode. | `.github/workflows/release.yml:307-335`, especially lines 310-313; the exact pinned [`domain-release.yml@4eb33928`](https://github.com/Hexalith/Hexalith.Builds/blob/4eb33928a1d8c7775f97221cf9edc171db0cb5f8/.github/workflows/domain-release.yml) defines `release` with `if: !inputs.governed-release` and `governed-release` with `if: inputs.governed-release`. The GOV-1 story still has its external integration task unchecked and the G2 request still calls integration pending. | **Update implementation.** Either activate governed mode with all exact-candidate inputs/outputs or explicitly weaken the inherited invariant; do not claim BUILD-REL-1 governed-mode enforcement from the immutable pin alone. |
| CF-2026-09-09-03 | **High** | AD-12, AD-13, AD-18; NC-18/NC-28 | **The release selector does not require the spine's exact-source successful `quality.yml` run, and the trust-bearing governance assertions remain outside the authorized CI evaluator closure.** `release.yml` queries only `ci.yml`; `select_exact_ci_run()` accepts only that response and has no quality coordinate. Consequently `quality_run` cannot be recorded in the AD-15 ledger, and the release authority does not enforce the only workflow currently running several newly required governance assertions. | `.github/workflows/release.yml:41-76`; `eng/release_contract.py:231-273`; no `quality` reference in either file. GitHub's workflow-runs API supports filtering and authenticating workflow runs, so the design is implementable with the current platform ([official REST documentation](https://docs.github.com/en/rest/actions/workflow-runs#list-workflow-runs-for-a-workflow)). | **Update implementation.** Select exactly one successful `quality.yml` push run for the candidate, bind its run ID/attempt, run trust-bearing assertions inside `ci.yml` as AD-13 says, and add fail-closed duplicate/missing/pagination tests. |
| CF-2026-09-09-04 | **High** | AD-12, AD-15; NC-6..NC-9 | **Post-release evaluator authorization is neither shaped nor enforced as specified.** All five active `post_release` rows use a non-null `domain-release.yml` reusable, although AD-12 requires `null`; none authorizes the current `release-evidence.yml` blob (`185593f0…`). The workflow never drafts or checks a `post_release` evaluator, calls `verify-release` without `--root`, and authenticates only the CI-handoff copy embedded in the Release artifact instead of independently downloading the CI artifact. | `eng/dependency-graph-policy.json:1723-2259`; current committed `.github/workflows/release-evidence.yml` SHA-256 = `185593f03ebf6920b6db66195fadb75a80b3db3f9b791e06176b50c16d0ae0b4`, absent from the five rows; `.github/workflows/release-evidence.yml:154-180`; `eng/dependency_handoff.py:275-359`; `eng/workflow_source_closure.py:806-850` explicitly adds the reusable as an independent root when `require_reusable_edge=false`. | **Update implementation and policy in the AD-12 two-phase order.** Support nullable post-release reusable, recompute only caller-reachable closure, pre-authorize the exact caller, enforce it before ledger acceptance, and independently authenticate the CI artifact. |
| CF-2026-09-09-05 | **High** | AD-15, AD-18; NC-10/NC-12/NC-19/NC-29 | **The committed handoff and ledger schemas predate the spine's contracts.** Code still declares `hexalith.release-verification-handoff.v1`; its `release` object is exactly `{version,tag,github_release_id,published}` with no `publication_started` or `publish_gate_variable`. The two ledger-producing `jq` blocks emit ad-hoc objects rather than the closed `frontcomposer.release-ledger-record.v2` shape and omit `quality_run`, `verification_run`, `evaluator`, `actors`, `environment_protection`, and the run-bound publish-gate value. The verifier never calls the environment or deployment-review APIs. | `eng/dependency_handoff.py:22,275-343`; `.github/workflows/release-evidence.yml:210-219,585-601`; repository-wide search finds no implementation of `environment_protection`, `dispatching_actor`, or `triggering_actor`. Live platform state is compatible with the target: `production` has required reviewer `jpiquot`, `can_admins_bypass=false`, and a custom branch policy exactly `main`; GitHub documents these environment fields and deployment controls in its [Environments REST API](https://docs.github.com/en/rest/deployments/environments#get-an-environment). | **Update implementation.** Version the handoff to v2, derive the gate-aware publication state, generate one closed typed ledger record for every disposition, and collect the run/deployment/environment evidence read-only before accepting it. |
| CF-2026-09-09-06 | **Medium** | AD-14; NC-25 | **The one-way manifest migration is not enforced at the claimed command boundary.** The helper's `seal-manifest` and `fallback-digest` paths still accept both v2 and v3, while AD-14 confines v2 to `verify-manifest` and audit commands. | `eng/release_evidence.py:64-65,3142-3212,3648-3676`; `MANIFEST_SCHEMA` is v2, `CURRENT_MANIFEST_SCHEMA` is v3, and both are admitted by seal/fallback. | **Update implementation.** Reject v2 in seal, classify, fallback, verify-prepared, and publish paths; retain it only in the explicit verification/audit surface. |
| CF-2026-09-09-07 | **Medium** | AD-16; NC-30 | **The new accepted-lineage predicate is asserted but not reality-checked by executable Governance.** No code walks first-parent policy history or proves pins back to `a8a50859…`; production checks only whether an exact evaluator row matches the active policy. The current lineage can be reconstructed from Git history, but the stated automatic control does not exist. | Repository search for the accepted SHA/`first-parent`/lineage finds no GOV-1 verifier; the only `a8a50859…` C# constant is unrelated EventStore historical identity evidence. `git log --first-parent -- eng/dependency-graph-policy.json` provides the human-auditable history, not the claimed gate. | **Update implementation.** Add the first-parent policy-history verifier and fixtures, or change “Governance verifies” to an explicit manual control until it lands. |
| CF-2026-09-09-08 | **Low** | Stack | **The local .NET observation is true but no longer current upstream.** The machine and `global.json` still report 10.0.400, but .NET 10.0.12 / SDK 10.0.401 shipped on 2026-09-08. With `rollForward: latestPatch`, CI can select 10.0.401 when installed, so “10.0.400” should remain labeled local/pinned-floor rather than imply the resolved CI SDK. | `global.json`; local `dotnet --version` = `10.0.400`; Microsoft's current [.NET 10 download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) lists SDK 10.0.401 and runtime 10.0.12 dated 2026-09-08. | **Autofix in a later update.** Record `10.0.400 local/pinned feature-band floor; upstream 10.0.401`, or capture the actual SDK in CI evidence. |

**Severity counts:** Critical 1 · High 4 · Medium 2 · Low 1.

## Decision coverage and confirmed-sound items

- **AD-1–AD-7 and AD-10 (graph core): confirmed current.** `eng/dependency_graph.py` implements
  committed-object traversal, the closed repository map, depth 1+2 graph, ordered closed envelope,
  Python canonical bytes, exact-object acquisition, and the stated resource ceilings. The focused
  graph/handoff/closure/release suite ran **180/180 passing** on 2026-09-09. Git 2.55.0 remains the
  current upstream release and the required plumbing is documented: [`ls-tree`](https://git-scm.com/docs/git-ls-tree),
  [`cat-file`](https://git-scm.com/docs/git-cat-file), and [`config --blob`](https://git-scm.com/docs/git-config#Documentation/git-config.txt---blobltblobgt).
- **AD-8: confirmed for the graph revision model.** The repository passes the documented
  `pull_request` merge SHA/base SHA and push `before`/head inputs, uses full history where required,
  and the graph tests cover diff classification. GitHub's current [event reference](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows)
  confirms `workflow_dispatch` and `workflow_run` SHA/ref behavior. The separate quality-run addition
  is the High gap in CF-2026-09-09-03.
- **AD-11: confirmed as a scope constraint.** GOV-1 changes governance/evidence files, not runtime API,
  package inventory, or UX.
- **AD-12 policy core: confirmed, apart from the post-release and activation gaps above.** The live
  JSON schema, semantic profiles, build registry, resource limits, and `ci`/`release` evaluator row
  shapes match the spine. Exact committed CI and Release caller blobs each match an active row.
- **AD-13 immutable primitives: confirmed.** `release.yml` is manual `workflow_dispatch`, re-reads
  live `main`, selects one exact-source successful push-CI run, downloads the run-bound zip, and pins
  all trust-bearing `uses:` references to 40-hex commits. GitHub documents `job.workflow_ref` and
  `job.workflow_sha` with the stated GitHub.com-only caveat in the [job context](https://docs.github.com/en/actions/reference/workflows-and-actions/contexts#job-context).
- **AD-17: confirmed current.** `prepare-manifest` emits `hexalith.release-evidence.v3`, and the v3
  workflow-provenance member sets/digest match the spine. CF-2026-09-09-06 is specifically the older
  schema's over-broad acceptance by other commands.
- **AD-18 static permission/environment half: confirmed.** FrontComposer grants the exact five write
  scopes at the reusable call, no GOV-1 workflow has `actions: write` or `pull_request_target`, and the
  pinned reusable binds both write-scoped jobs to `${{ inputs.environment-name }}` with caller value
  `production`. Live environment protection matches the target; live `main` still has no branch
  protection/ruleset, no `CODEOWNERS` exists, and collaborators are two admins plus one non-admin
  writer, so the Deferred evaluator-code statement remains accurate.
- **Stack: otherwise current.** Local Git 2.53.0 / upstream 2.55.0; local Python 3.14.4 / upstream
  3.14.7; SHA-1 Git object format; `actions/upload-artifact` latest v7.0.1 at pinned commit
  `043fb46d…`. Its official [README](https://github.com/actions/upload-artifact/blob/main/README.md)
  confirms v4+ immutability, unique artifact names, archive mode, and lack of GHES support; the
  [v7.0.1 release](https://github.com/actions/upload-artifact/releases/tag/v7.0.1) is still latest.
  Git's current [3.0 breaking-change plan](https://git-scm.com/docs/BreakingChanges#_git_3_0) still
  changes only the default for new repositories to SHA-256 and explicitly does not deprecate SHA-1,
  so the Deferred trigger is sound.

## Method notes

- Read-only checks used committed Git objects for workflow SHA-256 values, the active policy JSON,
  exact pinned Builds content from GitHub, and live repository/environment APIs. No submodule was
  initialized or updated.
- The existing dirty worktree was preserved. The only file added by this reviewer is this report; the
  spine and implementation were not modified.
- The known implementation gaps are already enumerated in
  `nonconformance-register-2026-09-08.md`; this review independently rechecked the verdict-affecting
  items against the current files and platform state rather than treating the register as proof.
