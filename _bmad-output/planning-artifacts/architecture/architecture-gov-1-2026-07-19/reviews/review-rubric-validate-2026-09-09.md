# Reviewer Gate — Rubric Walker (VALIDATE) — GOV-1 Architecture Spine

**Target:** `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md` (`status: final`, `updated: 2026-09-08`, AD-1..AD-18)

**Review date:** 2026-09-09

**Lens:** semantic good-spine checklist, VALIDATE intent

**Inputs inspected:** the complete spine, `.memlog.md`, the parent `architecture.md`, FC-DEP-1 contract, GOV-1 story, BUILD-REL-1/G2 request, the 2026-09-08 nonconformance register, selected handoff/disposition implementation surfaces, repository toolchain pins, and official current-version sources.
**Mechanical gate:** `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19` → `ok: true`, 0 findings.

## Verdict

**PASS WITH FINDINGS** — 0 Critical, 0 High, 3 Medium, 5 Low. The spine is a coherent, ratified contract for the bounded graph and release-authorization core. Its remaining semantic gaps are localized to handoff/ledger interoperability and traceability hygiene; none creates a presently unbounded publication-authority path, but the three Medium findings can still make separately built producer and verifier units disagree while each plausibly follows the text.

## Finding counts

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 3 |
| Low | 5 |
| **Total** | **8** |

## Medium findings

### RW-01 — `quality_run` has no defined carrier between Release and the ledger

- **Severity:** Medium
- **AD refs:** AD-13, AD-15
- **Evidence:** AD-13 lines 323-325 requires `select-ci` to record the selected `quality.yml` coordinates as `quality_run`. AD-15 lines 458-465 requires a non-null-or-null `quality_run` member in the closed `frontcomposer.release-ledger-record.v2`. None of the closed cross-workflow carriers includes it: the AD-13 CI handoff is exactly `{schema, run, revisions, evaluator, dependency_policy, dependency_graph}` (lines 339-348), the AD-15 Release handoff is exactly `{schema, release_run, ci_handoff, candidate, dependency_policy, release, manifest, assets, evaluator}` (lines 421-438), and AD-17's manifest provenance contains only CI and Release projections (lines 502-516). AD-15 says the post-release verifier re-authenticates the Release and CI runs, but never requires it to re-select `quality.yml`.
- **Divergence:** one implementation can add `quality_run` to the Release handoff (violating its closed schema), another can query and re-select the run, and another can emit ledger `null`; all are plausible readings of the current rules.
- **Disposition:** **autofix** — choose one carrier. Prefer adding an exact `quality_run` projection to the AD-15 Release handoff and requiring the post-release verifier to authenticate it before copying it to the ledger. If re-selection is intended instead, state that algorithm and its uniqueness predicate explicitly.

### RW-02 — completed-run uniqueness is ambiguous and differs across authoritative projections

- **Severity:** Medium
- **AD refs:** AD-13; inherited FR-24 exact-artifact pipeline
- **Evidence:** AD-13 lines 319-324 says the candidate is the head of “exactly one completed push run ... (whatever its conclusion) whose conclusion is `success`,” then requires “exactly one completed successful” `quality.yml` run. The parent at `architecture.md` lines 242-245 and FC-DEP-1 decision 13 lines 118-120 use only “exactly one completed successful” run. These predicates differ when one failed run and one successful rerun share a head SHA: counting all completed runs rejects it; filtering successful runs first accepts it.
- **Divergence:** independent selectors can make opposite release-eligibility decisions for the same GitHub run set without clearly violating their chosen source.
- **Disposition:** **discuss, then autofix** — decide whether uniqueness counts every completed run or successful runs only. State the predicate in two clauses (for example, “exactly one completed run of any conclusion has that head, and it concluded success”) and reconcile the parent and FC-DEP-1 projection in the same update.

### RW-03 — the durable ledger has no closed disposition/incident vocabulary or total mapping

- **Severity:** Medium
- **AD refs:** AD-15; Consistency Conventions
- **Evidence:** AD-15 lines 458-470 closes the ledger's member set but not the value set for `disposition` or the shape/vocabulary of `incident`. It names `non-compliant`, `partial-publish-incident`, and `missing-artifact` as incident states, while earlier text also introduces `gate-frozen` and a valid `.deferred.json` outcome for attempts that never authenticated CI (lines 414-438). It never maps no-releasable, rejected-before-publication, deferred, successful publication, and each incident path to exact ledger values and nullability. FC-DEP-1 decision 15 lines 148-158 additionally calls a “missing or deferred handoff pair” an incident, whereas the spine distinguishes the valid deferred sentinel from `missing-artifact`.
- **Divergence:** separately built disposition classifiers and ledger writers can choose incompatible strings and can disagree whether a deferred sentinel is an incident or a recorded non-governed attempt.
- **Disposition:** **autofix** — define closed `disposition` and `incident` enums plus a total mapping from AD-15 inputs (`publication_started`, `published`, handoff state, API conclusion, asset observations). Reconcile FC-DEP-1 so the deferred-sentinel semantics match.

## Low findings

### RW-04 — `environment_protection` is named but not closed

- **Severity:** Low
- **AD refs:** AD-18, AD-15
- **Evidence:** AD-18 lines 532-538 says the ledger records `required_reviewers`, `prevent_self_review`, `can_admins_bypass`, and “the deployment-branch policy” as `environment_protection`, but it does not define an exact nested member set, member name for the branch policy, types, or nullability. AD-15 otherwise calls the ledger exact and closed.
- **Disposition:** **autofix** — define the exact nested object, including a named `deployment_branch_policy` member and the representation of unavailable approval data.

### RW-05 — the authenticated CI-handoff copy has no normative file name

- **Severity:** Low
- **AD refs:** AD-15
- **Evidence:** AD-15 lines 414-420 requires the Release artifact to contain the Release handoff plus a byte-identical CI-handoff copy and says other files are non-normative, but does not name the copy. The current implementation uses `dependency-release-handoff.json` (`release.yml` lines 556-557 and `release-evidence.yml` lines 171-176), which is a reasonable seed but not a rule.
- **Disposition:** **autofix** — pin `dependency-release-handoff.json` as the normative copy name and reject zero or multiple normative matches.

### RW-06 — the Builds “execution identity” convention does not select the Release row

- **Severity:** Low
- **AD refs:** AD-13, AD-16; Consistency Conventions
- **Evidence:** the convention at line 565 says execution identity is active-policy `reusable.commit` / `builds_execution_sha`. AD-16 lines 479-480 explicitly permits distinct CI and Release reusable pins and says `builds_execution_sha` names the Release pin only. An active policy can therefore contain multiple conforming `reusable.commit` values.
- **Disposition:** **autofix** — say “the active-policy `release` row's `reusable.commit`, equal to `builds_execution_sha`; the CI row's pin is independently sealed in the CI handoff.”

### RW-07 — frontmatter omits a binding for one inherited invariant

- **Severity:** Low
- **AD refs:** AD-11; Inherited Invariants
- **Evidence:** frontmatter line 11 has six `parent:*` bindings for seven inherited rows. The row “GOV-1 alters no runtime, API, package inventory, or UX” (line 65) has no matching binding slug.
- **Disposition:** **autofix** — add a stable binding such as `parent:no-runtime-or-ux-change`, without renumbering any AD.

### RW-08 — two declared driving sources still contain superseded release semantics

- **Severity:** Low
- **AD refs:** AD-9, AD-13–AD-18; Sources
- **Evidence:** the GOV-1 story is `status: done` in frontmatter but says `Status: in-progress`, still requires manifest v2 and “freeze controls,” and retains pre-acceptance issue-17/`@main` narrative. The G2 request still describes pending integration, a shared `@main` submodule, and manifest v2. The 2026-09-08 nonconformance register already records this companion drift, so the spine is not silently depending on it, but these files remain listed as sources/companions.
- **Disposition:** **defer to the source owners** — keep the existing recorded deferral, then reconcile their current-state and schema wording to the AD-9/AD-13..18 model. No architecture decision should be changed merely to match stale delivery history.

## Good-spine checklist

| Checklist item | Result | Assessment |
| --- | --- | --- |
| Real divergence points for the level below are fixed | **Partial** | Graph, policy, evaluator, manifest, actor, and release-candidate invariants are strong; RW-01..RW-03 leave three release-evidence interoperability points open. |
| Every AD Rule is enforceable and prevents its stated divergence | **Partial** | AD-1..AD-14 and AD-16..AD-17 are enforceable as written. AD-15/AD-18 need the carrier/value/subshape pins in RW-01, RW-03, and RW-04. |
| Nothing under Deferred lets compliant units diverge | **Met** | Deferred items either require a new schema/approval, are external-owner work, or fail closed. Network/time budgets can vary operational availability but cannot authorize different bytes. |
| Named technology is verified-current | **Met** | Local Git 2.53.0, Python 3.14.4, and .NET SDK 10.0.400 match the spine/repository. Official sources still identify Git 2.55.0 as current, Python 3.14.7 as the current 3.14 maintenance release, and `actions/upload-artifact` v7.0.1 as latest; the repository pins its documented commit. See [Git installation/version page](https://git-scm.com/install/linux), [Python 3.14.7](https://www.python.org/downloads/release/python-3147/), and [upload-artifact releases](https://github.com/actions/upload-artifact/releases). |
| Brownfield reality is ratified rather than silently contradicted | **Met with explicit transition debt** | The spine's deliberate target-state deltas are ratified in `.memlog.md` and enumerated as NC-1..NC-30 in the nonconformance register; they are not hidden architectural assumptions. |
| Driving capability set is covered | **Met** | Story AC1-AC6 map to AD-1..AD-18 and the capability map. The source wording debt is RW-08, not a missing capability. |
| Parent invariants are inherited without weakening | **Met** | The parent says this spine is authoritative for GOV-1; the seven inherited rows align with its Key Invariants and FR-24 section. No local AD weakens the bounded graph, semantic-not-SHA compatibility, exact-artifact pipeline, pre-publication-only authorization, ownership, governed reusable mode, or no-runtime-change constraints. |
| Every epic-owned dimension is decided, deferred, or open | **Partial** | Boundaries, ownership, state/evidence mutation, migration, operations, provider/environment, failure behavior, and retention are covered. The release-evidence carrier and value-contract residue is captured in RW-01/RW-03/RW-04. |

## Confirmed-sound items

- Deterministic structure is clean: no placeholders, duplicate AD IDs, missing `Binds`/`Prevents`/`Rule`, or unpinned stack rows.
- AD-1..AD-7 form a closed, deterministic graph contract: boundary, identity, edge shape/order, canonical bytes/digest, semantic compatibility separation, and resource failure behavior agree.
- AD-8/AD-10 consistently bind diff/build/acquisition to explicit committed objects and a single candidate revision; candidate URLs and ambient nested worktrees never become trust inputs.
- AD-9, AD-12, AD-14, and AD-17 agree on delayed policy activation, immutable fallback approval, one-way manifest migration, v3 provenance, and seal material.
- AD-13 and AD-16 consistently separate the catalog gitlink from CI/Release execution pins, require literal 40-hex closure coordinates, and define the accepted Builds lineage plus active-row authorization.
- AD-18 places secrets/write scopes behind the production environment and keeps actors/approvals out of the sealed manifest; AD-15 gives those observations a durable ledger home.
- The operational/environmental envelope is not silent: GitHub.com-only behavior, production environment, provider ownership, incident handling, retention, and no-runtime topology are decided or explicitly deferred.
- The 7,085-word contract-grade size is not re-raised as bloat: `.memlog.md` records the deliberate decision to retain member sets and sort keys because they are divergence points.

## Gate conclusion

This spine is usable as the governing build substrate, with an Update recommended before treating the AD-15 ledger schema as independently implementable. The first update should close RW-01 through RW-03 together because they all affect the same Release → post-release evidence seam; the Low items are safe to bundle into that edit without changing AD IDs.
