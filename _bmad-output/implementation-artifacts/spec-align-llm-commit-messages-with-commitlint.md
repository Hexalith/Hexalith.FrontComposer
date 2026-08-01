---
title: 'Align LLM commit messages with commitlint'
type: 'bugfix'
created: '2026-08-01'
status: 'done'
review_loop_iteration: 1
baseline_commit: '6cb11107159e81329edfd7240fb3952d912b80bd'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The shared Codex, Claude, and GitHub Copilot entry points tell agents to use Conventional Commits but do not require them to inspect the repository's effective commitlint policy or validate the exact message they create. This permits apparently conventional suggestions and generated squash subjects to fail the repository gate, as the current `HEAD` demonstrates with `type-empty` and `subject-empty`.

**Approach:** Strengthen the synchronized, location-independent entry points so any commit message an LLM creates, suggests, or uses is checked against the owning repository's configuration with its pinned commitlint CLI. Add a focused governance test that locks the three entry points together and preserves this behavior.

## Boundaries & Constraints

**Always:** Keep `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md` identical as normalized text. Treat each owning repository's commitlint configuration and tracked Git guidance as authoritative, validate the complete candidate message before presenting or using it, and report the exact blocker without claiming compliance when validation cannot run. Preserve the unrelated `ShellTypeOrganizationGovernanceTests.cs` working-tree change untouched.

**Ask First:** Any change to `commitlint.config.mjs`, package dependencies, Husky hooks, CI workflows, semantic-release configuration, the `Hexalith.AI.Tools` submodule, commit history, branches, staging, commits, pushes, pull requests, or remote state.

**Never:** Hard-code FrontComposer's current type list or length/casing limits into the location-independent baseline; weaken or bypass commitlint; duplicate repository-specific policy in the universal entry points; edit unrelated files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Valid candidate | LLM creates, suggests, or uses a message accepted by the repository-pinned CLI | The exact full message may be presented or used | Preserve successful validation evidence |
| Invalid candidate | Commitlint rejects the exact candidate | Revise and revalidate; do not present or use it as compliant | Surface the reported rule failures if work cannot continue |
| Validation unavailable | Dependencies, configuration, or CLI cannot be resolved | Do not claim the candidate complies | Report the exact command and blocker |
| Entry-point drift | One shared instruction file differs or loses the validation invariant | Focused governance test fails | Restore normalized equality and required behavior |

</frozen-after-approval>

## Code Map

- `AGENTS.md` -- canonical working copy of the location-independent shared baseline.
- `CLAUDE.md` -- Claude entry point that must remain byte-equivalent to `AGENTS.md`.
- `.github/copilot-instructions.md` -- GitHub Copilot entry point that must remain byte-equivalent to `AGENTS.md`.
- `commitlint.config.mjs` -- authoritative repository policy; inspect and verify, but do not change.
- `.husky/commit-msg` -- repository-pinned local enforcement path; inspect only.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- fail-closed inventory that must be re-sealed when the new underscored test identifier is added.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- existing governance suite and repository-root helper for focused regression coverage.

## Tasks & Acceptance

**Execution:**
- [x] `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md` -- replace the generic Conventional Commits sentence with synchronized guidance covering created, suggested, and used messages; requiring both effective commitlint policy and tracked Git guidance to be satisfied; exact-message validation before any presentation or use; and fail-closed reporting.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- add a focused, three-part-named test that requires normalized equality across all three entry points and asserts the complete normalized normative guidance so reordering, relocation, or semantic negation fails.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- re-seal only the deterministic test-identifier count and hash after the new test reaches its final name.

**Acceptance Criteria:**
- Given Copilot, Codex, or Claude creates, suggests, or uses a commit message, when the shared guidance is followed, then the exact full candidate is validated by the owning repository's pinned commitlint CLI before it is presented or used as compliant.
- Given validation cannot execute or rejects the candidate, when the guidance is followed, then the agent reports the exact blocker or violations and does not claim compliance.
- Given any shared entry point drifts or drops the invariant, when the governance test runs, then it fails.
- Given the change is complete, when the three files are compared, then their normalized text is identical and no commitlint configuration or enforcement file has changed.
- Given the new underscored test identifier is present, when analyzer-policy governance runs, then its deterministic inventory is current and the fail-closed test passes.

## Spec Change Log

- **2026-08-01 — Review loop 1:** The adversarial review exposed that the new underscored test identifier left the fail-closed analyzer inventory stale (`count=6213`, `sha256=943e196124b5e9e8441267ccde117c0dfbd3eb5bb5542c7d48d0fc7bf94e27df`), and that the verification plan used a prohibited solution-level test command that stalled. The plan now includes the analyzer ledger, a repository-conforming three-part test name, a semantic assertion over the complete normative guidance, unconditional validation before presentation or use, and project-level test commands. This avoids the known-bad state where the focused fact passes while broad Governance fails, semantic negation remains undetected, and solution evaluation hangs. **KEEP:** Preserve the synchronized policy-driven guidance; owning-repository authority; validation of the exact candidate with the pinned CLI; fail-closed blocker/violation reporting; revise-and-revalidate behavior; no validation bypass; focused entry-point equality coverage; and the successful valid/invalid CLI evidence.

## Design Notes

Keep the baseline policy-driven rather than rule-driven: repositories can legitimately customize allowed types, subject casing, line lengths, and parser behavior. The stable invariant is to satisfy both the effective repository policy and tracked Git guidance, and to execute the owning repository's pinned validator against the exact message before presenting or using it. The topical Hexalith Git guidance retains detailed commit and pull-request procedures.

## Verification

**Commands:**
- `cmp -s AGENTS.md CLAUDE.md && cmp -s AGENTS.md .github/copilot-instructions.md` -- expected: all shared entry points are byte-identical.
- `printf '%s\n' 'docs: require commitlint-valid AI messages' | npx --no -- commitlint --verbose` -- expected: zero problems and zero warnings under the current repository policy.
- `printf '%s\n' 'Update subproject reference for Hexalith.Memories' | npx --no -- commitlint --verbose` -- expected: nonzero exit with `type-empty` and `subject-empty`.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --no-restore --filter "FullyQualifiedName~CiGovernanceTests.AgentEntryPoints" --logger "console;verbosity=minimal"` -- expected: the focused entry-point governance test passes.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --no-restore --filter "FullyQualifiedName~AnalyzerPolicyGovernanceTests.AnalyzerPolicy_GovernanceContract_FailsClosed" --logger "console;verbosity=minimal"` -- expected: the analyzer-policy inventory gate passes.
- `git diff --check -- AGENTS.md CLAUDE.md .github/copilot-instructions.md _bmad-output/contracts/analyzer-policy-exception-ledger-v1.json tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- expected: no tracked-file whitespace errors.
- `git diff --no-index --check /dev/null _bmad-output/implementation-artifacts/spec-align-llm-commit-messages-with-commitlint.md` -- expected: no whitespace-error output; exit 1 indicates the expected untracked-file diff.

**Results:**
- Shared entry-point byte comparison passed; all three files retain CRLF line endings.
- `AgentEntryPoints_CommitMessageGuidance_IsSynchronizedAndFailClosed` passed: 1 test, 0 failures. It checks normalized entry-point equality and pins the complete `Git and Submodules` subsection exactly once, including the Conventional Commits, validation-evidence, and fail-closed requirements.
- `AnalyzerPolicy_GovernanceContract_FailsClosed` passed after re-sealing only `testUnderscoreIdentifierTokens=6213` and `testInventorySha256=f989f0e449b42579d85ab601f882f1e69b79d45fc92fda367bb09bea59e7f2b9`: 1 test, 0 failures.
- The valid candidate `docs: require commitlint-valid AI messages` passed the repository-pinned commitlint CLI with zero problems and zero warnings.
- The invalid candidate `Update subproject reference for Hexalith.Memories` was rejected as expected with `type-empty` and `subject-empty`.
- The focused governance test covers validation-unavailable fail-closed reporting and entry-point drift; the two commitlint CLI checks cover the valid- and invalid-candidate matrix rows.
- Scoped tracked and untracked whitespace validation passed, and the commitlint configuration, hook, workflows, and package manifests are unchanged.

## Suggested Review Order

**Commit-message policy**

- Canonical rule binds Conventional Commits, repository policy, exact CLI validation, and evidence.
  [`AGENTS.md:49`](../../AGENTS.md#L49)

- Claude receives the byte-identical policy baseline.
  [`CLAUDE.md:49`](../../CLAUDE.md#L49)

- Copilot receives the byte-identical policy baseline.
  [`copilot-instructions.md:49`](../../.github/copilot-instructions.md#L49)

**Governance enforcement**

- Exact-section guard rejects drift, duplicates, negation, and contradictory appended policy.
  [`CiGovernanceTests.cs:34`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L34)

- Deterministic analyzer seal keeps underscore-named governance additions fail-closed.
  [`analyzer-policy-exception-ledger-v1.json:74`](../contracts/analyzer-policy-exception-ledger-v1.json#L74)
