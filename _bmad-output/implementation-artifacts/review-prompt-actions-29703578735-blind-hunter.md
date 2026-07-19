# Blind Hunter Review Prompt

Start with no prior conversation context. Invoke the `bmad-review-adversarial-general` skill on the complete change from baseline `b0254994e279a21d0496d6b3286d6524eebb14b4`.

Inspect the tracked diff with:

```bash
git diff b0254994e279a21d0496d6b3286d6524eebb14b4 -- .github/workflows/release-evidence.yml tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs
```

Also inspect the untracked spec at `_bmad-output/implementation-artifacts/spec-actions-29703578735-fix-release-evidence-noop.md`.

Review whether the replacement of the unsupported `gh release list --json targetCommitish` probe with `gh api` preserves fail-closed partial-publication detection, read-only workflow permissions, no-op behavior for frozen releases, and the real-tag verification path. Report only actionable findings with evidence, severity, and a concrete required action. Do not edit files.
