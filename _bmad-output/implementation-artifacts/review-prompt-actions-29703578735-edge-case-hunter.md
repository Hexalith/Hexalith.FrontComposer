# Edge Case Hunter Review Prompt

Start with no prior conversation context. Invoke the `bmad-review-edge-case-hunter` skill on the complete change from baseline `b0254994e279a21d0496d6b3286d6524eebb14b4`.

Inspect the tracked diff with:

```bash
git diff b0254994e279a21d0496d6b3286d6524eebb14b4 -- .github/workflows/release-evidence.yml tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs
```

Also inspect the untracked spec at `_bmad-output/implementation-artifacts/spec-actions-29703578735-fix-release-evidence-noop.md`.

Walk every branch of the tag resolver: no tag with a confirmed zero-result API probe, API outage, orphaned GitHub Release, direct-head tag, parent-commit tag, malformed probe output, and successful publication. Check shell `set -e`/`pipefail` behavior, quoting, `gh api` field/query compatibility, mocked test fidelity, cleanup, and platform assumptions. Report only unhandled edge cases with evidence and a concrete required action. Do not edit files.
