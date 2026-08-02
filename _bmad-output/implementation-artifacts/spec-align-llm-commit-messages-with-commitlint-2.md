---
title: 'Include Visual Studio in commitlint-valid AI generation'
type: 'bugfix'
created: '2026-08-01'
status: 'done'
review_loop_iteration: 2
baseline_commit: '628414061366d703e944131f00fc86197ffda718'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-align-llm-commit-messages-with-commitlint.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer already requires Claude, Codex, and GitHub Copilot to produce commitlint-valid commit messages, but Visual Studio's Copilot commit-message generator is covered only implicitly. Current Visual Studio releases obtain repository commit-message guidance from `.github/copilot-instructions.md`, so the supported surface and its policy need to be explicit and regression-guarded.

**Approach:** Amend the synchronized assistant entry points to name Claude, Codex, GitHub Copilot, and Visual Studio's Copilot generator under the same policy-driven, fail-closed commit-message rule. Extend the existing governance contract without adding a Visual Studio-specific instruction file or changing commitlint enforcement.

## Boundaries & Constraints

**Always:** Keep `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md` identical as normalized text. Preserve the existing requirements for Conventional Commits, the owning repository's effective commitlint policy and tracked Git guidance, exact full-candidate validation with the pinned CLI, successful evidence, revision after rejection, and fail-closed reporting. Treat `.github/copilot-instructions.md` as the repository-owned Visual Studio 2026 commit-generation surface, while Husky and CI remain the deterministic enforcement layers.

**Ask First:** Any change to `commitlint.config.mjs`, package dependencies, semantic-release, Husky hooks, CI workflows, user or IDE settings, a `references/` submodule, staging, commits, branches, pushes, pull requests, or remote state.

**Never:** Add a Visual Studio-only repository instruction file, edit the old per-user Visual Studio 2022 instruction setting, claim that probabilistic AI instructions guarantee compliance, weaken or bypass commitlint, hard-code repository-specific type or length rules into the location-independent baseline, or edit unrelated files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Supported assistant generation | Claude, Codex, GitHub Copilot, or Visual Studio generates or suggests a message | The shared instructions require the exact candidate to satisfy Conventional Commits, active commitlint, and tracked Git guidance | Revise and revalidate rejected candidates; never present them as compliant |
| Visual Studio repository guidance | Visual Studio 2026 18.6+ generates from staged changes | `.github/copilot-instructions.md` supplies the same contract as the Claude and Codex entry points | Husky or CI rejects an invalid message that generation fails to follow |
| Instructions unavailable | Repository instructions are disabled, unsupported, or the pinned CLI cannot run | No tool-verified compliance claim is made | Report the limitation or exact validation blocker; rely on hook/CI enforcement where available |
| Entry-point drift | One shared entry point loses the named Visual Studio scope or policy invariant | Focused governance validation fails | Restore synchronized, explicit guidance |

</frozen-after-approval>

## Code Map

- `AGENTS.md` -- canonical location-independent assistant baseline and Codex entry point.
- `CLAUDE.md` -- synchronized Claude entry point.
- `.github/copilot-instructions.md` -- synchronized GitHub Copilot and current Visual Studio commit-generation entry point.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- existing exact-section synchronization and fail-closed policy guard.
- `.husky/commit-msg` and `.github/workflows/commitlint.yml` -- inspection-only local and CI enforcement layers.
- `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md` -- preserved concurrent user change; excluded from task ownership and included explicitly in the baseline scope audit.

## Tasks & Acceptance

**Execution:**
- [x] `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md` -- retain universal assistant coverage while explicitly naming the four requested surfaces; require capable assistants to validate the exact full candidate before any presentation or use, revise and revalidate rejected candidates, and stop with the exact command and blocker when validation cannot run; keep the instructions-enabled Visual Studio 2026 routing plus hook/CI enforcement inside the synchronized policy section.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- update the existing exact expected section so universal coverage, all four named surfaces, unconditional pre-presentation validation by capable assistants, distinct rejected/unavailable branches, instructions-enabled Visual Studio routing, and enforcement-layer wording fail closed without adding or renaming a test identifier.

**Acceptance Criteria:**
- Given any assistant creates, suggests, or uses a commit message, including Claude, Codex, GitHub Copilot, or Visual Studio, when repository guidance is applied, then the message must follow Conventional Commits, effective commitlint, and tracked Git policy.
- Given an assistant can run repository tooling, when it creates, suggests, or uses a commit message, then it validates the exact full candidate with the pinned commitlint CLI before any presentation or use and preserves successful evidence.
- Given Visual Studio 2026 version 18.6 or later has repository instructions enabled, when it generates a commit message, then `.github/copilot-instructions.md` provides the same commit-message contract as `AGENTS.md` and `CLAUDE.md`.
- Given validation rejects a candidate, when the guidance is followed, then the violations are reported and the candidate is revised and revalidated before presentation or use.
- Given a capable assistant cannot run validation, when the guidance is followed, then it reports the exact command and blocker and does not present or use the candidate until validation succeeds.
- Given an entry point drifts or Visual Studio is removed from the supported scope, when the focused governance test runs, then it fails.
- Given another assistant consumes the shared baseline, when it creates, suggests, or uses a commit message, then the universal rule still applies even though four current products are named explicitly.
- Given implementation is complete, when the baseline scope audit runs, then the four task-owned tracked files are the only owned changes, the concurrent `11-17-shell-bundle-split.md` edit is preserved and excluded, the approved spec is the only untracked task artifact, and commitlint configuration, hooks, workflows, dependencies, and submodules are unchanged.

## Spec Change Log

- **2026-08-01 — Review loop 1:** Adversarial review found that the first implementation narrowed a universal assistant rule to four products, conflated rejected candidates with unavailable validation, required the tool-less Visual Studio generator to execute the CLI, and placed the Visual Studio routing/enforcement statement outside the exact tested section. The review also exposed unconditional version wording, absent authoritative routing evidence and actual verification results, mixed line endings in this spec, and scope checks that omitted the untracked spec and concurrent story edit. Tasks, acceptance, design notes, and verification now require universal-plus-explicit wording, capability-aware validation branches, supported-version routing inside the pinned section, CRLF and untracked checks, actual result recording, and an exact baseline scope audit. This avoids excluding future assistants, impossible validation claims, silently removable routing, false clean-scope claims, and hidden artifact-format drift. **KEEP:** Preserve byte-identical entry points; policy-driven repository authority; exact-candidate validation by capable assistants; blocker reporting without compliance claims; hook/CI enforcement; the existing test identifier and stable line positions where practical; the clean analyzer seal; successful valid/invalid commitlint probes; and the unrelated user change.
- **2026-08-01 — Review loop 2:** Verification review found that the scope audit only printed changed paths instead of failing on unexpected files, while adversarial review found a loophole that allowed unvalidated messages to be presented without calling them compliant and an edge-case gap that assumed Visual Studio repository instructions were enabled. Tasks, acceptance, design notes, and verification now require validation before any presentation or use by capable assistants, fail-closed handling when the CLI cannot run, instructions-enabled Visual Studio routing, a structural hook assertion, and mechanically asserted expected-versus-actual tracked and untracked path sets. This avoids silently passing extra files, exposing unvalidated candidates, treating disabled IDE instructions as active, and overstating what a generation-time prompt can enforce. **KEEP:** Preserve universal coverage with all four requested surfaces named explicitly; byte-identical entry points; policy-driven repository authority; exact-candidate validation by capable assistants; distinct rejection and unavailable-validator branches; supported-version routing inside the pinned section; installed-hook and blocking-CI enforcement wording; the existing test identifier and stable line positions where practical; CRLF; prior verification expectations; and the unrelated user change.

## Design Notes

Microsoft documents the repository-owned `.github/copilot-instructions.md` route for Visual Studio 2026 May Update 18.6 and later when repository instructions are enabled; older Visual Studio 2022 releases use a per-user setting that this repository cannot configure. Repository instructions guide generation but cannot guarantee model behavior or execute commitlint themselves, so capable assistants perform pre-presentation validation while an installed commit-message hook and the blocking CI gate remain enforcement layers. Primary evidence: `https://learn.microsoft.com/en-us/visualstudio/version-control/git-make-commit?view=visualstudio`, `https://learn.microsoft.com/en-us/visualstudio/releases/2026/release-notes`, and `https://docs.github.com/en/copilot/how-tos/configure-custom-instructions-in-your-ide/add-repository-instructions-in-your-ide?tool=visualstudio`.

## Verification

**Commands:**
- `cmp -s AGENTS.md CLAUDE.md && cmp -s AGENTS.md .github/copilot-instructions.md` -- expected: all assistant entry points are byte-identical.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --no-restore --filter "FullyQualifiedName~CiGovernanceTests.AgentEntryPoints" --logger "console;verbosity=minimal"` -- expected: the synchronized four-surface policy test passes.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --no-restore --filter "FullyQualifiedName~AnalyzerPolicyGovernanceTests.AnalyzerPolicy_GovernanceContract_FailsClosed" --logger "console;verbosity=minimal"` -- expected: the identifier inventory remains sealed.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~CiGovernanceTests.CommitlintJob" --logger "console;verbosity=minimal"` -- expected: the blocking CI commitlint gate test passes.
- `printf '%s\n' 'docs: clarify Visual Studio commit guidance' | npx --no -- commitlint --verbose` -- expected: the exact valid candidate passes the pinned CLI.
- `printf '%s\n' 'Update Visual Studio commit guidance' | npx --no -- commitlint --verbose` -- expected: the invalid candidate is rejected with `type-empty` and `subject-empty`.
- `test "$(sed -n '2p' .husky/commit-msg)" = 'npx --no -- commitlint --edit "$1"'` -- expected: the installed commit-message hook invokes the pinned repository commitlint CLI without a bypass.
- `git diff --check 628414061366d703e944131f00fc86197ffda718 -- AGENTS.md CLAUDE.md .github/copilot-instructions.md tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- expected: no task-owned tracked-file whitespace errors.
- `git diff --no-index --check /dev/null _bmad-output/implementation-artifacts/spec-align-llm-commit-messages-with-commitlint-2.md` -- expected: exit 1 for the untracked addition with no whitespace-error output.
- `file AGENTS.md CLAUDE.md .github/copilot-instructions.md tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs _bmad-output/implementation-artifacts/spec-align-llm-commit-messages-with-commitlint-2.md` -- expected: every edited artifact reports CRLF line terminators only.
- `test "$(git diff --name-only 628414061366d703e944131f00fc86197ffda718 | LC_ALL=C sort)" = "$(printf '%s\n' '.github/copilot-instructions.md' 'AGENTS.md' 'CLAUDE.md' '_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md' 'tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs' | LC_ALL=C sort)" && test "$(git ls-files --others --exclude-standard | LC_ALL=C sort)" = '_bmad-output/implementation-artifacts/spec-align-llm-commit-messages-with-commitlint-2.md'` -- expected: exit 0 only when the baseline diff contains the four owned tracked files plus the preserved concurrent story file and the approved spec is the only untracked task artifact.
- `git diff --quiet 628414061366d703e944131f00fc86197ffda718 -- commitlint.config.mjs package.json package-lock.json .husky .github/workflows references` -- expected: exit 0; enforcement, dependencies, workflows, and submodules are unchanged.

**Results:**
- Entry-point byte comparison passed. All three entry points, the governance test, and this spec report CRLF-only line endings.
- `AgentEntryPoints_CommitMessageGuidance_IsSynchronizedAndFailClosed`, `AnalyzerPolicy_GovernanceContract_FailsClosed`, and `CommitlintJob_BlocksPrTitlesAndCommitMessagesUsedBySemanticRelease` each passed 1/1 with no failures or skipped tests. The existing test identifier, eight-line policy footprint, and analyzer inventory remained unchanged.
- The valid candidate passed the repository-pinned commitlint CLI with zero problems and warnings. The invalid plain-English candidate exited 1 with `type-empty` and `subject-empty` as expected.
- The structural hook assertion passed, confirming `.husky/commit-msg` invokes `npx --no -- commitlint --edit "$1"`; the focused CI test pins the blocking commitlint workflow wiring.
- Tracked and untracked whitespace checks passed; the no-index command returned the expected addition-only exit code with no whitespace diagnostics.
- The mechanical scope assertion passed with exactly the four task-owned tracked files plus the preserved concurrent `11-17-shell-bundle-split.md` edit and this spec as the only untracked artifact. The forbidden enforcement, dependency, workflow, and submodule diff returned exit 0.
- Matrix audit: the entry-point test pins the universal generation rule, all four requested surfaces, instructions-enabled Visual Studio routing, unavailable-instruction/validator wording, and synchronized drift behavior; the valid and invalid commitlint probes verify validator acceptance and rejection; the hook assertion and blocking-CI test verify the stated enforcement-layer wiring. Every covering test ran and passed.

## Suggested Review Order

**Shared policy**

- Universal scope names all four requested surfaces without excluding future assistants.
  [`AGENTS.md:49`](../../AGENTS.md#L49)

- Capability-aware validation blocks presentation or use until the exact candidate passes.
  [`AGENTS.md:53`](../../AGENTS.md#L53)

- Visual Studio routing is versioned, enabled-only, and bounded by external enforcement.
  [`copilot-instructions.md:55`](../../.github/copilot-instructions.md#L55)

- Claude receives the byte-identical shared contract.
  [`CLAUDE.md:49`](../../CLAUDE.md#L49)

**Governance coverage**

- Exact-section coverage pins synchronization, validation branches, and Visual Studio routing.
  [`CiGovernanceTests.cs:34`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L34)
