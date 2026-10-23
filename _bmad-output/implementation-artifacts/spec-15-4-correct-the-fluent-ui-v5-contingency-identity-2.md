---
title: 'Story 15.4: Reject Excluded Fluent Catalog Identities'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
story_id: '15.4'
story_key: '15-4-i-doc-c-correct-the-fluent-ui-v5-contingency-identity'
baseline_commit: '73768df6d2dcd8e15e9c6f023f0a60f9cb3a3609'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 15.4's selected-catalog parser accepts a Fluent PackageVersion with an Exclude attribute, although MSBuild can exclude that package entirely. Documentation validation then claims an identity that is not selected.

**Approach:** Extend the existing fail-closed parser to reject Exclude on either governed Fluent entry. Add committed-object regression fixtures for Components, Icons and an unresolved exclusion, plus a passing unrelated-package exclusion. Preserve current published identity, decision, dependencies and gitlinks.

</frozen-after-approval>

## Implementation Notes

- Follow-up to the completed original Story 15.4; its spec and accepted history are preserved. No intent gaps, irreversible operations or new APIs. Scope is the existing validator, its existing test helper and this spec.
- Reuse `Get-FluentCatalogVersions` in `eng/validate-docs.ps1` and `AssertFluentCatalogParityAsync` in `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs`. Reject any Exclude attribute on a governed entry, consistently with the existing conservative handling of conditional entries; unrelated items remain outside the identity predicate.
- MSBuild reproduction: `dotnet msbuild <temporary-catalog.proj> -nologo -getItem:PackageVersion` returns only Icons when Components excludes itself; exit 0. Evidence: `/tmp/frontcomposer-15-4-exclude-msbuild.log`.
- Baseline checks: focused Debug test-project build passed with zero warnings/errors; `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` and `python3 eng/validate-story-artifacts.py` passed. Existing 53-case documentation suite is running. Baseline logs use `/tmp/frontcomposer-15-4-recheck-*`.
- Sprint row is already at review, later than in-progress; the workflow's monotonic synchronization preserves it and Epic 15 in progress.
- Baseline AppHost command: `aspire start --non-interactive --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` exited 2 before startup with CS1704 for duplicate Hexalith.Memories.Contracts and Hexalith.Memories.Client.Rest assemblies. Full log: `/tmp/frontcomposer-15-4-exclude-aspire-baseline.log`. This unrelated runtime dependency failure does not prevent the focused documentation lane; no dependency repair is in scope.
- New four-case regression method built successfully. Before the parser fix, the actual committed-object fixtures failed for all three governed-exclusion cases and passed for the unrelated excluded package: 4 total, 3 failed, exit 1. Command: `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -method '*FluentCatalogParityHandlesExcludedItemsAsync'`; log `/tmp/frontcomposer-15-4-exclude-red-tests.log`.
- Added the Exclude-attribute guard to the existing governed-entry predicate; no new resolver, registry or MSBuild evaluation. Test fixture messages were validated before fixture execution using the pinned commitlint CLI; successful evidence `/tmp/frontcomposer-15-4-recheck-fixture-commitlint.log`.
- After the guard, the same four fixtures pass: 4 total, zero errors/failures/skips/unrun cases, exit 0; `/tmp/frontcomposer-15-4-exclude-green-tests.log`. The actual documentation command also passes with zero blocking failures on the root HEAD-selected catalog; `/tmp/frontcomposer-15-4-exclude-docs.log`.
- The existing documentation suite, started before the patch, completed successfully: 53 total, zero errors/failures/skips/unrun cases, exit 0; `/tmp/frontcomposer-15-4-recheck-doc-tests.log`. Together with the post-fix four-case suite, 57 documentation cases passed.

## Review Triage Log

- Blind Hunter ran at the parent model capability; Edge Case Hunter and Verification Gap were skipped by the oneshot route. Every finding was checked against the actual code and fixtures.
- BH-1 — low / patch: the old literal-version diagnostic hid the unsupported Exclude attribute. The shared failure now explicitly names Exclude and retains bounded, relative, deterministic output.
- BH-2 — low / patch: the new cases did not distinguish the conservative attribute-presence policy from evaluating only matching exclusions. Added empty and unrelated-value exclusions on a governed entry; both must fail. The unrelated-package exclusion remains a passing control.
- BH-3 — low / reject: duplicating the disposable manual MSBuild fixture XML in this spec would add another example without improving normal validation. The complete catalog builder, mutations and assertions are checked in under FluentCatalogParityHandlesExcludedItemsAsync and AssertFluentCatalogParityAsync; the exact repeatable regression command is recorded above. No evidence is needed from the temporary log to rerun the regression. No work deferred.

## File List

- `eng/validate-docs.ps1`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs`
- `_bmad-output/implementation-artifacts/spec-15-4-correct-the-fluent-ui-v5-contingency-identity-2.md`

## Verification

- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Debug --no-restore --nologo -v:minimal` — exit 0, zero warnings/errors on the final tree; `/tmp/frontcomposer-15-4-exclude-final-build.log`.
- `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Docs.DocsSiteValidationTests` — baseline suite: 53 passed, zero errors/failures/skips/unrun; `/tmp/frontcomposer-15-4-recheck-doc-tests.log`.
- `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -method '*FluentCatalogParityHandlesExcludedItemsAsync'` — final six-case suite: 6 passed, zero errors/failures/skips/unrun, exit 0; `/tmp/frontcomposer-15-4-exclude-final-tests.log`. The two review regressions and passing unrelated-item control ran successfully.
- `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` — exit 0 after the review patches, zero blocking failures; `/tmp/frontcomposer-15-4-exclude-final-docs.log`. DocFX and snippet compilation were intentionally skipped because publication content and snippet inputs did not change.
- `node node_modules/@commitlint/cli/cli.js --edit /tmp/frontcomposer-15-4-exclude-commit-message.txt --verbose` — exit 0, zero problems/warnings before the local commit; `/tmp/frontcomposer-15-4-exclude-commitlint.log`.
- Review is complete, both accepted findings are patched, and no work is deferred. Spec is done; the sprint row remains at review and Epic 15 remains in progress, as required by monotonic workflow synchronization.
- `python3 eng/validate-story-artifacts.py` — exit 0, Story artifact validation passed; `/tmp/frontcomposer-15-4-exclude-artifacts-global.log`.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-4-correct-the-fluent-ui-v5-contingency-identity-2.md` — exit 0, complete three-file ownership matches the preserved baseline; `/tmp/frontcomposer-15-4-exclude-artifacts-story.log`.
- `git diff --check` — exit 0. The final worktree contains only this follow-up's three owned files; no dependencies, gitlinks or published documentation changed.
