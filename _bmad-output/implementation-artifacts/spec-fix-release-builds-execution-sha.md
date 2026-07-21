---
title: 'Fix Release workflow startup_failure — pass builds-execution-sha to domain-release.yml'
type: 'bugfix'
created: '2026-07-21'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'e13368a2f122d22cf240bb5ff4b3a5bc37de0a90'
context: ['{project-root}/references/Hexalith.Builds/.github/workflows/domain-release.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The `Release` workflow (`.github/workflows/release.yml`) has failed with `startup_failure` on every run since commit `c9ff529b` (2026-07-20; run 29798429699 is the latest). That commit bumped the `references/Hexalith.Builds` submodule to `7708256e`, whose reusable `domain-release.yml` added a new `required: true` input `builds-execution-sha` (introduced in Builds commit `819c1f6`). Our caller does not pass it, so GitHub rejects the reusable-workflow call at parse time. The whole Hexalith ecosystem is affected the same way (Tenants is also red). No package can be released while this is broken.

**Approach:** Follow the pattern documented in Builds' own `domain-release.md`: pin the `uses:` reference to an exact 40-hex Builds commit SHA and pass `builds-execution-sha` equal to that same SHA. The reusable workflow enforces `job.workflow_sha == builds-execution-sha` at runtime, so both must be the identical commit. Use `7708256eba4974ba005fd7fe86ec5bfd6152a25e` — the commit this repo's submodule is already pinned to (our vetted Builds version). Update the `CiGovernanceTests` guard that currently pins `@main`.

## Boundaries & Constraints

**Always:** Keep `release.yml`'s `freeze-guard` job, its publish-freeze gate (`vars.HEXALITH_RELEASE_PUBLISH_ENABLED`), the `workflow_run`/`event == 'push'` triggers, and the existing `with:`/`secrets:` entries (`solution`, `test-projects: ''`, `NUGET_API_KEY`) exactly as-is. The `uses:@<sha>` pin and `builds-execution-sha:` value must be the identical 40-hex lowercase SHA. That SHA must equal the `references/Hexalith.Builds` submodule gitlink so the pinned reusable workflow matches the vetted Builds version.

**Ask First:** Choosing a Builds SHA other than the current submodule pin (`7708256e`). Committing directly to `main` instead of a `fix/` branch + PR.

**Never:** Do not add `publish-containers`, `container-projects`, `workflow_dispatch`, or any release-approver input (governance guards forbid them here). Do not modify any file under `references/` (submodules are external). Do not lift the release freeze or touch `.releaserc.json`, `eng/release_prepublish.py`, or `release-evidence.yml` (out of scope; `release-evidence.yml` uses composite actions, not the reusable workflow, so it is unaffected).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Required input now supplied | `release.yml` calls `domain-release.yml@7708256e` with `builds-execution-sha: 7708256e` | Workflow passes GitHub startup validation; `freeze-guard` runs, reports frozen, `release` job skipped (freeze default) → run concludes green | N/A |
| Regression to `@main` | Someone reverts the `uses:` pin to `@main` or a tag | `CiGovernanceTests` fails (no 40-hex SHA match) | Test failure blocks merge |
| SHA mismatch | `uses:@<shaA>` but `builds-execution-sha:<shaB>` | `CiGovernanceTests` fails (SHAs not equal) | Test failure blocks merge |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml` -- the failing caller; `release` job's `uses:`/`with:` block is the fix site (lines ~82-88)
- `references/Hexalith.Builds/.github/workflows/domain-release.yml` -- reusable workflow declaring the new required `builds-execution-sha` input and the `job.workflow_sha == builds-execution-sha` identity check (READ-ONLY, external)
- `references/Hexalith.Builds/.github/workflows/domain-release.md` -- authoritative caller example: pin `uses:@<sha>` + `builds-execution-sha:<sha>` to the same SHA (READ-ONLY)
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- `ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` (line ~448) currently asserts `@main`; must guard the pinned-and-matched SHA invariant instead

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/release.yml` -- change the `release` job's `uses:` from `domain-release.yml@main` to `domain-release.yml@7708256eba4974ba005fd7fe86ec5bfd6152a25e`, and add `builds-execution-sha: 7708256eba4974ba005fd7fe86ec5bfd6152a25e` under `with:` -- supplies the required input and satisfies the identity check that caused the startup_failure
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- replace the `@main` `ShouldContain` with an invariant guard: extract the 40-hex SHA from the `uses:` pin and the `builds-execution-sha:` value and assert both exist and are equal (survives future Builds bumps, still catches `@main` regressions and mismatches) -- keeps the governance guard truthful about the new contract

**Acceptance Criteria:**
- Given the fixed `release.yml`, when the `Release` workflow is triggered by a successful CI `push` run on `main`, then it passes GitHub startup validation (no `startup_failure`) and the frozen `release` job is skipped, concluding green.
- Given the updated `CiGovernanceTests`, when the Shell.Tests Governance lane runs, then `ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` passes.

## Spec Change Log

- **2026-07-21 — adversarial review (no loopback; patch-only).** All three review layers confirmed the fix is fundamentally correct (gitlink == both literals, reusable's required inputs/secret satisfied, NuGet-only omissions correct). Two patches applied: (1) **gitlink third-leg cross-check** — all three reviewers flagged that the guard enforced only `pin == builds-execution-sha`, not the `== references/Hexalith.Builds gitlink` leg the comments promise; a well-formed-but-wrong or stale SHA would stay green yet reproduce the `startup_failure`. Added a `git ls-tree HEAD` cross-check to `CiGovernanceTests`. (2) **Stale doc** — `deployment-guide.md` (lines 84, 120) still described `domain-release.yml@main`; updated to the pinned-SHA model. Deferred (out of scope): sibling reusable callers (`ci.yml` → `domain-ci.yml@main`, etc.) remain on `@main` — logged to `deferred-work.md`. Rejected as contrived/mitigated: regex spacing/trailing-suffix/first-match brittleness, comment duplication, "first real release unverified" (intentionally frozen).

## Design Notes

Why a full SHA and not `@main`: the reusable workflow validates `RESOLVED_WORKFLOW_SHA` (`job.workflow_sha`) equals the passed `builds-execution-sha`. With `@main`, `job.workflow_sha` tracks the Builds `main` tip (already advanced to `dfb2f3fd`), so a hardcoded `builds-execution-sha` would drift out of match and fail the identity step. Pinning `uses:@<sha>` makes `job.workflow_sha` deterministic and equal to the passed SHA. Maintenance model: bump the `references/Hexalith.Builds` gitlink, the `uses:` pin, and `builds-execution-sha` together in lockstep.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug` -- expected: clean build (TreatWarningsAsErrors)
- Run the built Shell.Tests executable directly filtered to `CiGovernanceTests` (VSTest sockets are blocked here) with `DiffEngine_Disabled=true` -- expected: `ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` and `ReleaseModelGovernanceTests` green
- `python3 -c "import yaml,sys; yaml.safe_load(open('.github/workflows/release.yml'))"` -- expected: parses (YAML sanity)

**Manual checks:**
- Confirm `release.yml` still contains `freeze-guard`, `vars.HEXALITH_RELEASE_PUBLISH_ENABLED`, `test-projects: ''`, `NUGET_API_KEY`, and does NOT contain `publish-containers`/`workflow_dispatch`.
- After merge, re-trigger CI on `main` and confirm the downstream `Release` run no longer reports `startup_failure`.

## Suggested Review Order

**The fix — supply the required input (entry point)**

- The startup_failure root cause: pin the reusable workflow to an exact Builds SHA (was `@main`).
  [`release.yml:89`](../../.github/workflows/release.yml#L89)

- The new required input; must equal the `@sha` pin (reusable checks `job.workflow_sha == builds-execution-sha`).
  [`release.yml:92`](../../.github/workflows/release.yml#L92)

**The governance guard — enforce the contract so it can't regress**

- Invariant guard replacing the old `@main` string assertion: pin and input must be the same 40-hex SHA.
  [`CiGovernanceTests.cs:454`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L454)

- Review-added third lockstep leg: pin == input == `references/Hexalith.Builds` gitlink (catches bogus/stale SHAs).
  [`CiGovernanceTests.cs:474`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L474)

**Supporting — docs and deferred follow-up**

- Living deployment doc updated from `@main` to the pinned-SHA model (rows for the release path).
  [`deployment-guide.md:84`](../project-docs/deployment-guide.md#L84)

- Deferred: sibling reusable callers still on `@main` (same latent startup_failure class).
  [`deferred-work.md:1849`](deferred-work.md#L1849)
