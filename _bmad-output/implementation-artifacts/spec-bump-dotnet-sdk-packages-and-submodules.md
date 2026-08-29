---
title: 'Bump .NET SDK, Packages, and Root Submodules'
type: 'refactor'
created: '2026-08-29'
status: 'in-progress'
baseline_commit: '85216682495f8cae26cd0883e2e84a538450af4a'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer pins unavailable SDK `10.0.302`, Aspire `13.4.6`, stale npm dependencies, and eight stale governance values although NuGet is current; one root submodule also trails `main`.

**Approach:** Align active SDK/package mirrors, fast-forward root submodules, preserve central ownership and historical evidence, then validate .NET, release tooling, E2E, governance, and Aspire behavior.

## Boundaries & Constraints

**Always:** Re-resolve versions/tips before editing; pin SDK `10.0.400` and Aspire SDK/CLI `13.5.3`; keep NuGet authority in `Hexalith.Builds`; update npm manifests/locks together, including TypeScript 7 and conventional-changelog 10; retain latest Fluent V5 prerelease; preserve CRLF and package-family alignment.

**Ask First:** Editing shared submodule contents for a newly published NuGet candidate; advancing Builds execution SHA/closures; recapturing manual IDE evidence; accepting non-fast-forward movement or an undocumented version retention.

**Never:** Add root package-version metadata; rewrite `CHANGELOG.md`, dated scenarios/evidence, or completed stories; initialize nested submodules; use recursive/remote updates; downgrade stable to prerelease; commit, push, or publish.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| SDK | Installed `10.0.400`; repo asks for `10.0.302` | Active pins/docs/tests use `10.0.400` | Fail on active stale pins; retain history |
| Packages | NuGet current; npm/Aspire/policy lag | Update owned pins/locks; major npm behavior passes | Retain documented prerelease/unresolved packages |
| Submodules | Seven current; EventStore one commit behind | Root worktrees equal remote `main`; nested remain uninitialized | Halt on dirty/divergent/non-FF state |

</frozen-after-approval>

## Code Map

- `global.json:3`, `.github/workflows/{quality,nightly,mutation-property-nightly,quarantine-governance-nightly}.yml` -- active SDK pins; `quality.yml:581` also owns Aspire CLI.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj:1` -- CPM-exempt SDK; must match catalog/exception `13.5.3`.
- `Directory.Packages.props`, `references/Hexalith.Builds/Props/Directory.Packages.props` -- read-only import/catalog; live audit found all 33 direct NuGet dependencies current.
- `eng/dependency-graph-policy.json:68-83` -- eight stale exact selected-catalog mirrors: FsCheck, Roslyn Workspaces, Localization, TimeProvider.Testing, MCP, Immutable, Verify, and Verify.XunitV3.
- Root/two E2E `package.json`/locks and `CiGovernanceTests.cs:920-945` -- npm authority, aligned Playwright/Axe, and release-parser gate.
- IDE parity matrix/job/test plus `_bmad-output/project-context.md` and current project docs -- current mirrors; historical evidence is read-only.
- `references/Hexalith.EventStore` -- current `62d28510...`, discovered remote `2aa94e80...`; all other root gitlinks were current during planning.

## Tasks & Acceptance

**Execution:**
- [ ] Re-query official registries and eight remote refs; halt on unsafe movement.
- [ ] Update SDK/Aspire/current mirrors, npm manifests/locks, and eight policy rows without duplicating NuGet authority.
- [ ] Fast-forward changed roots only; run .NET, package, parser, E2E, governance, and Aspire checks, recording the commit-bound gate limitation while uncommitted.

**Acceptance Criteria:**
- Given the root, when CLI/restore/build/AppHost checks run, then SDK `10.0.400` and Aspire `13.5.3` resolve warning-free.
- Given live registries, when freshness/behavior checks run, then NuGet remains latest-compatible, npm direct packages are latest stable, and major upgrades pass parser/typecheck gates.
- Given catalog and remotes, when governance/status run, then eight policy rows match without changing Builds execution pins, root gitlinks are latest fast-forward tips, and nested submodules remain uninitialized.

## Spec Change Log

## Design Notes

The Builds catalog gitlink and the approved CI/CD execution SHA are deliberately independent. This refresh repairs catalog mirrors but must leave `4eb33928...` workflow execution pins and evaluator closures untouched.

## Verification

**Commands:**
- `dotnet --version`; restore/build solution Release and AppHost Debug -- expected: `10.0.400`, warning-free.
- Root/E2E `npm ci`, E2E typecheck, and focused release-parser tests -- expected: reproducible locks and unchanged behavior.
- Dependency-graph unit tests and focused governance/IDE-parity tests -- expected: worktree-safe checks pass; report commit-object limitation separately.
- `aspire start --non-interactive`, `aspire describe`, `aspire stop`; submodule status and `git diff --check` -- expected: healthy topology, root-only gitlinks, no nested initialization/whitespace errors.
