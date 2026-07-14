---
title: 'Fix semantic-release breaking marker parsing'
type: 'bugfix'
created: '2026-07-14'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'd9d2656e989b616ce8d790e601770488358a6f10'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-actions-29316660112-fix-cicd.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release run `29319125606` correctly rejected a `2.1.0` package plan because the compatibility ledger requires `v3.0`, but semantic-release selected the minor line after its default Angular parser ignored the valid `fix!:` head commit. The follow-up commit `d9d2656e` also failed because `BREAKING CHANGE:` was used as a subject rather than a footer.

**Approach:** Align semantic-release with the repository's Conventional Commits contract by using the `conventionalcommits` preset for both commit analysis and release notes, declaring that preset directly, and behaviorally proving that supported breaking markers select a major release.

## Boundaries & Constraints

**Always:** Keep commitlint, semantic-release analysis, and release-note parsing on the same Conventional Commits grammar; retain the published `2.0.4` package baseline and exact `v3.0` compatibility ledger; test the configured parser rather than only matching configuration text.

**Ask First:** Rewriting published `main` history, changing release/CI trigger dependencies, altering the compatibility-suppression lifecycle, or adding a custom commit grammar.

**Never:** Modify submodules, weaken commitlint or package validation, extend/relabel suppressions to permit `2.1`, hand-edit `CHANGELOG.md`, publish packages during verification, or treat a subject-only `BREAKING CHANGE:` line as valid metadata.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Breaking header | `fix!: ...` or `feat!: ...` | Commit analyzer returns `major`; current post-`v2.0.4` history resolves to `3.0.0` | Regression test fails before release |
| Breaking footer | Conventional subject plus blank line and `BREAKING CHANGE: ...` footer | Commit analyzer returns `major` | Regression test fails before release |
| Ordinary change | Valid `fix:` / `feat:` commit | Existing patch/minor behavior remains unchanged | Regression test reports the unexpected type |
| Malformed marker | `BREAKING CHANGE: ...` as the subject | Commitlint rejects it and analyzer does not treat it as a major signal | No compatibility-gate bypass |

</frozen-after-approval>

## Code Map

- `.releaserc.json` -- semantic-release plugin configuration that currently leaves both parsing plugins on Angular defaults.
- `package.json` and `package-lock.json` -- reproducible Node dependency graph; the Conventional Commits preset is currently only transitive through commitlint.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- blocking governance lane and existing process helper suitable for exercising the installed analyzer against repository configuration.
- `docs/diagnostics/compatibility-suppressions.json` and `eng/pack_release_packages.py` -- unchanged downstream guard that proves only the `v3.0` plan is acceptable.

## Tasks & Acceptance

**Execution:**
- [x] `.releaserc.json` -- configure `@semantic-release/commit-analyzer` and `@semantic-release/release-notes-generator` with the `conventionalcommits` preset -- make version selection and generated notes interpret the same valid breaking markers as commitlint.
- [x] `package.json` and `package-lock.json` -- add `conventional-changelog-conventionalcommits` as an explicit development dependency -- avoid relying on commitlint's transitive dependency for release execution.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- add a behavioral analyzer regression covering both valid breaking forms, normal patch/minor commits, and the malformed subject-only marker -- prevent a string-present/configured-but-ineffective recurrence.

**Acceptance Criteria:**
- Given tags through `v2.0.4` and the current commit range containing `9ca3c825`, when the configured commit analyzer evaluates the range, then it selects a major release and semantic-release computes `3.0.0`.
- Given the release configuration, when analyzer and notes plugin options are inspected, then both use the same explicit `conventionalcommits` preset backed by a direct dependency.
- Given the implementation diff, when scope is reviewed, then release triggers, publish commands, compatibility suppressions, submodule pointers, and `CHANGELOG.md` are unchanged.

## Spec Change Log

## Design Notes

The release plugins default to the Angular preset when no parser is configured. Commitlint's Conventional Commits grammar accepts `type!:` but Angular does not, producing the observed split-brain result. Selecting the maintained preset is preferable to duplicating a custom header regex, and configuring both semantic-release plugins avoids version/changelog disagreement.

## Verification

**Commands:**
- `npm ci && npm audit signatures` -- expected: lockfile installs cleanly and registry signatures verify.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "FullyQualifiedName~CiGovernanceTests"` -- expected: the behavioral parser regression and existing release governance pass.
- `python3 eng/pack_release_packages.py --version 3.0.0-ci.fix --output /tmp/frontcomposer-release-parser --plan` -- expected: the v3 plan is accepted without building or publishing.
- `printf '%s\n' 'fix!: recognize conventional breaking markers' | npx commitlint --verbose` -- expected: zero commitlint errors and warnings.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false` -- expected: zero warnings and errors.

**Observed 2026-07-14:**
- `npm ci` passed; `npm audit signatures` verified 506 packages and 123 attestations. npm still reports one pre-existing high-severity audit finding.
- The focused matrix test passed 1/1 and the full `CiGovernanceTests` filter passed 52/52 with no skipped tests.
- The configured analyzer evaluated all 23 commits after `v2.0.4` as `major`, producing next version `3.0.0`; the unchanged `3.0.0-ci.fix --plan` package guard accepted release line `v3.0`.
- Commitlint accepted `fix!: recognize conventional breaking markers`; the Release solution build completed with zero warnings/errors; `git diff --check` and the protected-scope audit passed.
- Review patches exercised the real release-notes generator, linted every matrix row including scoped breaking headers, and installed npm dependencies before post-release evidence tests; the focused tests passed 2/2 and the full default lane passed 4,101/4,101.

## Suggested Review Order

**Release semantics**

- Shared parser preset keeps version selection and generated notes consistent.
  [`.releaserc.json:5`](../../.releaserc.json#L5)

- Direct preset dependency makes release-time grammar resolution reproducible.
  [`package.json:26`](../../package.json#L26)

**Behavioral guardrails**

- Real analyzer, notes generator, and commitlint CLI exercise the supported commit matrix.
  [`CiGovernanceTests.cs:441`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L441)

- Clean evidence runners install Node dependencies before executing governance tests.
  [`release-evidence.yml:140`](../../.github/workflows/release-evidence.yml#L140)

- Workflow governance locks dependency installation behind tag resolution and before tests.
  [`CiGovernanceTests.cs:1129`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L1129)

**Supporting artifacts**

- Lockfile records the preset as a direct root development dependency.
  [`package-lock.json:19`](../../package-lock.json#L19)

- Broader Release dependency hardening remains explicitly deferred for separate approval.
  [`deferred-work.md:1564`](deferred-work.md#L1564)
