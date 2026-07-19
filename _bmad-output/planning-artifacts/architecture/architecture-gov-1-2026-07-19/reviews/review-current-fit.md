# Current-Fit Review — GOV-1 Dependency Provenance

**Target:** `ARCHITECTURE-SPINE.md`  
**Lens:** verified-current technology, Git/GitHub revision semantics, repository reality, and brownfield
release integration  
**Review date:** 2026-07-19  
**Verdict:** **BLOCKED — the Git object model and bounded census fit current reality, but the existing
`workflow_run` release path does not check out the upstream CI commit that AD-9 requires, and the policy
trust coordinate plus verifier grammar remain too ambiguous for safe implementation.**

## Critical

### C-1 — The existing release workflow does not preserve the upstream CI revision

**Spine assertion:** AD-9 requires preparation, live verification, fallback comparison, and prepublish
orchestration to receive the same explicit 40-hex root commit
(`ARCHITECTURE-SPINE.md:135-147`). The capability map treats the existing release-evidence seam as a
compatible extension point (`:220-229`).

**Reality check:**

- `.github/workflows/release.yml:24-28` is triggered by `workflow_run`, then invokes the external
  `domain-release.yml@main` at `:72-87` without passing `github.event.workflow_run.head_sha`.
- The currently selected Hexalith.Builds reusable workflow performs `actions/checkout` with
  `fetch-depth: 0` but no `ref`
  (`references/Hexalith.Builds/.github/workflows/domain-release.yml:85-94`).
- GitHub documents `GITHUB_SHA` for `workflow_run` as the last commit on the default branch, not the
  triggering run's head. Reusable workflows retain the caller's event context, and checkout without a
  `ref` uses the ref/SHA for that event. Therefore the release job can check out a later `main` commit
  than the one CI tested. See [workflow_run event semantics](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_run),
  [reusable-workflow caller context](https://docs.github.com/en/actions/reference/workflows-and-actions/reusing-workflow-configurations#github-context),
  and [checkout ref behavior](https://github.com/actions/checkout#usage).
- `eng/release_prepublish.py:462-475` does not pass `--commit-sha`; the current parser falls back to
  `GITHUB_SHA` (`eng/release_evidence.py:2901-2917`). The manifest can therefore seal the same wrong
  default-branch revision rather than detect it.
- The independent post-publication lane is correct by contrast: `.github/workflows/release-evidence.yml`
  explicitly checks out `github.event.workflow_run.head_sha` with full history.

**Impact:** If the freeze is enabled and `main` advances between CI completion and release execution,
packages, the dependency graph, and the sealed manifest can come from a revision that the triggering CI
run did not test. The current freeze reduces immediate exposure but does not make the architecture
implementable or safe to finalize.

**Disposition:** **Block, then autofix the spine and route the upstream change.** Add the release trigger
and reusable workflow to the Capability Map. Require an explicit `source_commit =
github.event.workflow_run.head_sha`, pass it as a required checkout input to `domain-release.yml`, check
out that SHA, assert `git rev-parse HEAD == source_commit`, and pass `--commit-sha source_commit`
explicitly through prepublish/prepare/live verification. The reusable-workflow input is an upstream
Hexalith.Builds change; GOV-1 must fail closed until that exact revision handoff exists.

## High

### H-1 — The policy's revision is not a trust boundary yet

**Evidence:** AD-3 says both base and candidate mappings are validated against “the versioned
FrontComposer-owned policy” (`:63-71`); AD-10 permits acquisition from policy-approved remotes
(`:149-157`); AD-12 puts trusted identities, semantic profiles, executable argv, and limits in one file
(`:167-175`). No rule identifies which commit's policy bytes govern each mode.

**Current-fit failure:** In pull-request CI, the candidate merge revision is intentionally untrusted.
Reading the candidate copy of `dependency-graph-policy.json` lets the same PR authorize a new remote or
command before the gate evaluates it. Reading only the base copy prevents a legitimate policy-plus-
gitlink addition from being evaluated and leaves historical live verification without the policy that
originally produced its graph. GitHub explicitly warns that event/context data and untrusted workflow
inputs require defensive treatment; the current repository has no pre-existing policy file or bootstrap
mechanism to resolve this choice.

**Disposition:** **Discuss/block, then autofix.** Assign immutable policy coordinates independently for
base collection, candidate data validation, PR acquisition, command execution, release preparation, and
historical live verification. At minimum, remote/argv authorization must come from a trusted base or
separately approved workflow coordinate, while the semantic policy used to produce a manifest must be
identified and sealed by raw-byte SHA-256. Define a two-step approval path for adding a repository so a
candidate cannot approve its own acquisition authority.

### H-2 — The canonical JSON recipe is sound, but the verifier schema is not closed

**Verified portion:** Python's current documentation confirms `ensure_ascii`, `allow_nan`, `sort_keys`,
and `separators`; `hashlib.sha256()` is guaranteed. Constraining the value domain and explicitly sorting
the edge array makes AD-5's project-specific canonicalization viable, and the spine correctly avoids
claiming RFC 8785 compatibility. See the [Python 3.14 JSON documentation](https://docs.python.org/3.14/library/json.html)
and [hashlib documentation](https://docs.python.org/3.14/library/hashlib.html).

**Gap against current code:** AD-4/AD-5 say objects “contain” fields and offline verification checks
types/uniqueness, but do not require duplicate JSON member-name rejection, exact allowed members,
integer-vs-boolean checks, extension behavior, or a grammar for non-null
`catalog_contract_version`. The brownfield `read_json()`/`json.load()` path loses duplicate member names
before `manifest_diagnostics()` can inspect them; the current outer seal similarly canonicalizes the
already-collapsed object (`eng/release_evidence.py:2183-2227`). Strict and permissive verifiers can
therefore accept different documents or hash different projections.

**Disposition:** **Autofix.** Define a closed v1 JSON schema and validation order: reject duplicate member
names during decode; reject unknown/missing members; require exact JSON scalar types (booleans are not
integers); require `depth` in `{1,2}`; forbid Builds-only members on other edges; keep the marker exactly
`null` until BUILD-CAT-1 adopts a bounded string grammar; and hash the exact validated logical object.
Add hostile fixtures for duplicate names, unknown members, bool-as-int, and extension injection in
addition to the successful golden digest.

### H-3 — Pull-request base semantics still allow two graph diffs

**Verified portion:** GitHub confirms that for a `pull_request` workflow, `github.sha` is the synthetic
merge commit on `refs/pull/<n>/merge`; using it as the candidate matches the revision currently built by
both primary and supplemental CI. The primary reusable CI also fetches full history. See
[GitHub's merge-branch behavior](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#pull_request).

**Unresolved assertion:** AD-8 names `github.event.pull_request.base.sha` as the base input but only says
to “record” the computed merge-base (`:122-129`). The memlog and GOV-1 acceptance text require a
merge-base comparison. One implementation can diff `base.sha..github.sha`; another can diff
`merge-base(base.sha, github.sha)..github.sha`. The supplemental `quality.yml` checkout is currently
depth 1, so the graph gate cannot be placed there unchanged; the Capability Map correctly places it in
the release-triggering `CI` workflow, but the algorithm must be explicit.

**Disposition:** **Autofix.** Define `base_commit = git merge-base(base.sha, candidate_commit)` and use
that exact commit for the base graph, evidence, and diff, or explicitly adopt `base.sha` and reconcile
the acceptance text. Specify fail-closed/full-affected behavior for missing, multiple, or non-ancestor
merge bases. Add the graph gate as a separate required job in `.github/workflows/ci.yml` with explicit
full-history checkout; a job that calls a reusable workflow cannot contain additional steps.

## Medium

### M-1 — Git and Python versions are observations, not reproducible CI constraints

**Evidence:** The local environment reports Git 2.53.0 and Python 3.14.4, matching the Stack table
(`:189-196`). Current official Git documentation is already at 2.55.0, and Python 3.14 documentation is
at 3.14.6. The repository does not pin Git or run `actions/setup-python`; `ubuntu-latest` determines both
at execution time. By contrast, `.NET SDK 10.0.302` is present locally, declared in root and selected
module `global.json` files, and published by Microsoft. Root `rollForward: latestPatch` permits a later
10.0.3xx patch rather than an immutable exact SDK. See [Git current documentation](https://git-scm.com/docs/git-config),
[.NET 10 SDK downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), and
[global.json roll-forward rules](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json).

**Disposition:** **Autofix wording.** Label Git/Python as “locally verified” rather than architecture
pins, declare the minimum supported feature/version or pin the CI runtime, and make the golden fixtures
the compatibility gate. Describe .NET as `10.0.302, latestPatch within the 10.0.3xx feature band` unless
the repository changes `rollForward` to `disable`.

### M-2 — Mutable `@main` workflow/action code is outside the graph but the boundary is not stated

**Evidence:** `.github/workflows/ci.yml` and `.github/workflows/release.yml` invoke Hexalith.Builds
reusable workflows at `@main`; `quality.yml`, `release-evidence.yml`, and the selected reusable release
workflow invoke `Hexalith.Builds/Github/initialize-build@main`. GitHub documents that branch-referenced
reusable workflows can resolve differently on a full rerun, and recommends full commit SHAs for the
strongest stability. See [reusable workflow rerun behavior](https://docs.github.com/en/actions/reference/workflows-and-actions/reusing-workflow-configurations#behavior-of-reusable-workflows-when-re-running-jobs)
and [GitHub Actions SHA policy](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/enabling-features-for-your-repository/managing-github-actions-settings-for-a-repository).

**Impact:** AD-2/AD-10 make the *dependency graph data* exact, but readers may interpret “Builds
provenance” or the sealed graph as proving which workflow/action implementation ran. It does not.

**Disposition:** **Autofix or explicitly defer.** State that v1 seals dependency/catalog provenance, not
complete build-process/SLSA provenance. Require the new graph collector/acquirer not to depend on
`initialize-build@main`. Prefer pinning the reusable workflow and actions to approved full SHAs and
recording `job.workflow_sha`; if that wider CI provenance is out of GOV-1, name the upstream owner and
revisit trigger rather than leaving the claim implicit.

### M-3 — Existing manifest evolution needs an explicit brownfield cutoff

**Evidence:** The current release helper is version 1.2.0 and has no dependency graph. It seals compact
sorted Python JSON and performs offline/live checks through one verifier. AD-9 says collection,
preparation, sealing, fallback invalidation, fixtures, and post-publication verification change
atomically, but does not identify the new manifest/helper version or whether pre-GOV-1 manifests remain
verifiable.

**Disposition:** **Autofix.** Declare the exact manifest member path, bump policy (the graph is a breaking
evidence-schema change), and legacy rule. New publication authorization must reject graph-less
manifests. If historical legacy verification remains supported, isolate it as non-authorizing and never
synthesize provenance that was not sealed at publication time.

## Low

### L-1 — The source list does not substantiate all named platform decisions

The frontmatter cites Git `ls-tree`/`config` and Python `json`/`hashlib`, but not Git `cat-file`, Git
object-format behavior, GitHub pull-request/workflow-run revision semantics, reusable workflow context,
checkout behavior, or .NET `global.json`. These are load-bearing to AD-5, AD-7, AD-8, AD-9, and AD-10.

**Disposition:** **Autofix.** Add the official sources used in this review to the spine/memlog so future
revalidation can distinguish verified behavior from repository assumptions.

## Verified Current Fit — Preserve

| Area | Evidence | Result |
| --- | --- | --- |
| Bounded census | Exact committed-object census at `600f4c738bd28b1efe0e69940ccec8b03faba7c4`: 8 root gitlinks + 32 direct nested gitlinks = 40 edges. Seven Builds selectors resolve to six distinct Builds commits. | **Pass; counts remain evidence, not acceptance constants.** |
| Git object format | Root and every root-selected repository report `sha1`; strict 40-hex v1 IDs fit current repositories. Git officially supports both SHA-1 and SHA-256 object formats, so fail-closed future-format handling is appropriate. | **Pass.** |
| Git plumbing | Local Git 2.53.0 successfully runs committed `.gitmodules` parsing with `git config --blob`, size checks with `git cat-file -s`, and NUL-delimited full-tree enumeration with `git ls-tree -r -z --full-tree`. Current Git docs retain all operations. | **Pass.** |
| Catalog bytes | All six distinct selected Builds commits expose `Props/Directory.Packages.props`; raw-byte hashing is possible from exact blobs and no adopted contract marker was found. | **Pass for bootstrap.** |
| PR candidate | `github.sha` is the PR merge revision and matches default checkout/current CI semantics. | **Pass, subject to H-3 base algorithm.** |
| JSON/hash primitives | Python provides the stated deterministic serializer controls and guaranteed SHA-256. Project-specific canonicalization is accurately distinguished from RFC 8785. | **Pass, subject to H-2 schema closure.** |
| .NET affected builds | Root and buildable selected modules use SDK 10.0.302; current local SDK is 10.0.302 and Microsoft publishes it. | **Pass with documented roll-forward semantics.** |
| Release seams | `release_evidence.py` has existing prepare/seal/verify/fallback seams, and `release-evidence.yml` already performs exact `workflow_run.head_sha` checkout for post-publication verification. | **Good extension points; prepublication path fails C-1.** |

## AD-by-AD Current-Fit Audit

| Decision | Research/reality status | Review result |
| --- | --- | --- |
| AD-1 | Exact HEAD census repeated from committed trees. | Fits current shape; still requires authority reconciliation outside this lens. |
| AD-2 | Git plumbing and current SHA-1 repositories verified. | Fit. |
| AD-3 | Current root/direct URLs fit the proposed GitHub grammar. | Blocked by H-1 policy coordinate. |
| AD-4 | Current graph includes repeated/self-target identities; explicit edge-before-cache behavior is necessary. | Fit; exact schema closure moves to H-2. |
| AD-5 | Python JSON/hash primitives and Git object-format transition checked in current official docs. | Algorithm fits; verifier grammar incomplete. |
| AD-6 | Current C# Governance uses working-tree catalogs/index gitlinks and hard-coded SHAs, confirming the migration need. All selected exact catalog blobs exist. | Fit as target architecture. |
| AD-7 | `cat-file -s` and streamed `ls-tree -z` are current supported plumbing. | Fit; ceiling values remain assumptions requiring fixtures. |
| AD-8 | GitHub PR merge-revision semantics and current CI checkout/history inspected. | Candidate fits; base algorithm blocked by H-3. |
| AD-9 | Current manifest/release workflows and helper call graph inspected. | **Fails current prepublication seam (C-1).** |
| AD-10 | Current initialization is root-only; isolated direct-object acquisition does not yet exist. | Feasible target; trust input blocked by H-1. |
| AD-11 | No runtime/API/package/UI implementation surface is needed. | Fit. |
| AD-12 | No current policy/bootstrap exists; current code defines policy across C#, Python, and workflows. | Direction fits; authority unresolved (H-1). |

## Gate Recommendation

Do not finalize or hand this spine to `bmad-dev-story` until C-1 and H-1 are resolved in enforceable
rules and H-2/H-3 receive unambiguous schema/revision algorithms. The Git plumbing, bounded census,
SHA-256 approach, and .NET toolchain need no redesign; the necessary changes are at the trust,
revision-handoff, and verifier-contract seams.
