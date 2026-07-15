---
created: 2026-07-15
updated: 2026-07-15
epic: 11
childStory: 11.19a
parentStory: 11.19
owner: Developer + Architect
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
status: ready-for-dev
implementationGate: post-correction-readiness-pass
baseline_commit: 32db5c3460a4aa0ae6382ae9db36fa42e512ffd3
---

# Story 11.19a: Doc-Comment Enforcement Realignment

Status: review.

## Story

As a release owner,
I want CS1591 to be genuinely enforced on the Contracts API-freeze surface,
so that public documentation policy matches what Release builds actually verify.

## Acceptance Criteria

1. Given `src/Directory.Build.props` currently places `1591` in the src-wide `NoWarn`, when policy is
   realigned, then that compiler-level suppression no longer makes the `.editorconfig` scoped warnings
   inert. Non-freeze code remains explicitly non-enforced without hiding the four freeze scopes.

2. Given the approved freeze scopes, when Release compilation runs, then missing XML documentation is
   a CS1591 warning in exactly these recursive surfaces:
   `Contracts/Attributes`, `Contracts/Rendering`, `Contracts/Mcp`, and `Contracts/Conformance`.
   Because warnings are errors, a synthetic undocumented public symbol in each scope fails the build.

3. Given documented public symbols and code outside the freeze scopes, when compilation runs, then
   documented symbols pass and out-of-scope symbols follow the explicitly recorded non-enforcement
   policy. Existing CS1570/1572/1573/1574 XML validation posture is not weakened.

4. Given future config drift, when Governance runs, then it rejects `1591` in any effective `NoWarn`,
   proves all four recursive globs match non-empty real source sets, and compiles synthetic positive and
   negative specimens so the rule cannot pass vacuously.

5. Given currently undocumented in-scope public APIs, when implementation is prepared, then docs are
   added or a bounded, symbol-specific suppression with rationale, owner, and review date is approved;
   no folder-wide or global suppression is introduced.

6. Given the correction is complete, when Release build, Contracts tests, Governance, docs validation,
   artifact validation, and file-integrity checks run, then they pass without package/public API shape,
   runtime behavior, analyzer package, generated output, UX, release workflow, or submodule changes.

## Tasks / Subtasks

- [x] Capture the effective CS1591 configuration and exact in-scope public-symbol/doc-gap census.
- [x] Remove the effective compiler-level `1591` suppression and make default vs freeze-scope policy explicit.
- [x] Close in-scope XML-doc gaps without changing public signatures or behavior.
- [x] Add non-vacuous config, source-set, and synthetic compile governance tests.
- [x] Run Release/Contracts/Governance/docs/artifact validation and reconcile the File List.

## Dev Notes

### Current State

- `src/Directory.Build.props` contains
  `<NoWarn>$(NoWarn);0419;1570;1572;1573;1574;1591;1734</NoWarn>`.
- `.editorconfig` attempts to set CS1591 to `warning` under the four recursive Contracts folders, but
  a compiler `NoWarn` suppresses emission before that severity can enforce the freeze.
- The safest scoped design is to remove `1591` from effective `NoWarn`, explicitly set the non-freeze
  default policy, and retain more-specific recursive warning scopes. Verify actual MSBuild/editorconfig
  precedence with synthetic compilation rather than relying on configuration text alone.

### Files To Read Before Editing

- `.editorconfig`
- `src/Directory.Build.props`
- `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj`
- the four approved Contracts folders and their public API baselines/tests
- existing Governance tests that inspect compiler/analyzer policy

### Anti-Patterns

- Do not add CS1591 to `WarningsNotAsErrors`, another `NoWarn`, command-line suppression, or a broad
  `#pragma warning disable`.
- Do not turn on documentation enforcement for generated output or unrelated packages accidentally.
- Do not “fix” the build by internalizing/removing public APIs or changing signatures.

### Technical Reference

Official C# compiler guidance distinguishes `NoWarn`, `WarningsAsErrors`, and
`TreatWarningsAsErrors`: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/errors-warnings

### Validation Commands

```bash
dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj \
  -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
python3 eng/validate-story-artifacts.py --story \
  _bmad-output/implementation-artifacts/11-19-doc-comment-enforcement-realignment.md
```

## References

- `_bmad-output/planning-artifacts/epics.md` — nonimplementable 11.19 parent and 11.19a child.
- `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` — enforcement mismatch.
- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs1591

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Implementation Plan

- Prove the current effective `NoWarn` and CS1591 gap census before policy changes.
- Express non-freeze suppression in `.editorconfig`, retain the four recursive warning scopes, and remove compiler-level `1591` suppression.
- Document the bounded in-scope API gaps without altering signatures or runtime behavior.
- Add Governance coverage that evaluates effective configuration, non-empty source scopes, and positive/negative synthetic builds.
- Run the focused and repository-wide validation gates, then reconcile changed files and story evidence.

### Debug Log References

- 2026-07-16: `dotnet msbuild ... -getProperty:NoWarn` returned `;0419;1570;1572;1573;1574;1591;1734` for both `net10.0` and `netstandard2.0`.
- 2026-07-16: Release compilation with `1591` removed from the command-line `NoWarn` failed as expected. The approved scopes produced 56 CS1591 errors across both TFMs: 28 unique undocumented public symbols, all in `Rendering/FrontComposerRenderContract.cs`; source-set counts were Attributes 17, Rendering 36, Mcp 6, Conformance 1.
- 2026-07-16: Config Governance tests failed red on the effective `NoWarn` and absent non-freeze default, then passed 3/3 after realignment. Recursive `**.cs` globs were compiler-proven; the prior `**/*.cs` form did not match files directly under a freeze folder.
- 2026-07-16: The normal Release Contracts build failed red with the same 56 scoped CS1591 errors after policy activation, then passed 0 warnings/0 errors after documenting the 28 unique public symbols.
- 2026-07-16: Governance passed 6/6, including evaluated `NoWarn` on both TFMs, root-owned suppression scanning, all four non-empty source scopes, four nested undocumented negative specimens, documented positive specimens, and an undocumented out-of-scope positive specimen.
- 2026-07-16: Release solution build passed with 0 warnings/0 errors; Governance passed 315/315; the configured regression lane passed 4,132/4,132; DocFX and story-artifact validation passed; whitespace, submodule, generated-output, package/public-API-baseline, release-workflow, and changed-file integrity checks passed.

### Completion Notes List

- Captured a deterministic red baseline proving the effective suppression and the exact bounded documentation gap before implementation.
- Removed `1591` from effective compiler `NoWarn`, explicitly disabled CS1591 outside the freeze surface, and activated the four recursive warning scopes.
- Added XML summaries and parameter documentation to the only in-scope gap file without changing declarations, signatures, or behavior; Release Contracts build and all 209 Contracts tests pass.
- Added non-vacuous Governance coverage; all six policy cases and the complete 209-test Contracts project pass.
- Ratcheted the DocFX missing-summary baseline by removing only the four resolved rendering-contract UIDs; no package/public API shape or runtime behavior changed.
- Completed every required Release, Contracts, Governance, docs, artifact, regression, and file-integrity gate without submodule or release-workflow changes.

### File List

- `_bmad-output/implementation-artifacts/11-19-doc-comment-enforcement-realignment.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `.editorconfig`
- `src/Directory.Build.props`
- `src/Hexalith.FrontComposer.Contracts/Rendering/FrontComposerRenderContract.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Architecture/Cs1591DocumentationPolicyTests.cs`
- `docs/validation/api-summary-baseline.txt`

## Change Log

- 2026-07-15: Materialized approved 11.19a child from the live inert-CS1591 configuration.
- 2026-07-16: Activated scoped CS1591 enforcement, documented the bounded rendering-contract gaps, added non-vacuous Governance coverage, and passed all completion gates.
