---
title: 'DW-668 follow-up review of Story 11.4 security validation'
type: 'bugfix'
created: '2026-08-28'
status: 'done'
baseline_revision: '80b7a26b1ec24c271f18a09b6e7a687b35a1d2bd'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-11-4-security-validation-hardening.md'
warnings: [oversized]
deferred:
  - summary: >-
      The checked-in central package graph blocks the repository's normal .NET test and solution-build lanes before tests execute.
    evidence: |-
      Exact focused-test and solution-build commands fail NU1107 because xunit.v3 4.0.0 resolves xunit.v3.common 4.0.0 while xunit.v3.extensibility.core remains pinned to 3.2.2. Validation-only overlays proved this change, but the unchanged blocking workflow cannot reach those assertions.
    location: >-
      references/Hexalith.Builds/Props/Directory.Packages.props:318
    severity: medium
  - summary: >-
      The blocking Playwright workflow does not execute the settings-persistence Unicode storage-key regression.
    evidence: |-
      The focused serverless Playwright regression passes 1/1, but the blocking workflow typechecks and selects other specs; reverting the helper casing could therefore escape that CI lane.
    location: >-
      .github/workflows/quality.yml:481
    severity: low
---

<intent-contract>

## Intent

**Problem:** DW-668 records that Story 11.4 exhausted its review budget while still recommending an independent confirmation. The confirmation found three low-severity gaps in the reviewed touchpoints: ill-formed UTF-16 return paths are accepted and normalize to a different navigation target, query-only normalization can exceed the shell return-URL length cap, and the E2E storage-key mirror does not match .NET invariant lowercasing for supported Unicode email-shaped identities.

**Approach:** Tighten the shared and shell return-path predicates to reject unpaired surrogates while preserving valid astral scalars, reassert the shell length cap after normalization, and make the TypeScript storage-key mirror apply .NET-compatible simple invariant lowercasing. Pin every correction with focused regression tests.

## Boundaries & Constraints

**Always:** Keep `Contracts` compatible with both `net10.0` and `netstandard2.0`; preserve accepted well-formed root-relative paths and valid non-format astral Unicode; preserve runtime storage-key canonicalization and persisted-key shape; keep the shell sanitizer aligned with `ReturnPathValidator`; use repository-standard xUnit v3/Shouldly and Playwright assertions; leave the deferred-work ledger untouched.

**Block If:** JavaScript parity cannot be implemented without a broad generated Unicode mapping table or changing .NET runtime semantics, or a return-path fix requires changing an already-supported well-formed URL shape rather than rejecting only invalid Unicode/cap overflow.

**Never:** Do not reopen the separately deferred required-DTO-scalar, `retryAfter` wire-format, or full-email-lowercasing policy decisions; do not change communication DTO shapes, production storage-key semantics, package versions, submodules, generated output, or unrelated story artifacts.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Ill-formed return path | Root-relative text containing a lone high surrogate, lone low surrogate, or mispaired surrogate | `ReturnPathValidator` returns `false`; shell sanitization returns `/` | Fail closed without throwing |
| Valid astral return path | Root-relative text containing a valid non-`Cf` surrogate pair | Shared validation and shell sanitization preserve the path | No error expected |
| Query-only cap boundary | `?` plus 2,047 characters, initially exactly `MaxReturnUrlLength` | Prepending `/` must not return a value over the cap | Fall back to `/` |
| Unicode storage identity | Email-shaped user `İΣ@Example.COM` after trim and NFC | E2E helper emits `%C4%B0%CF%83%40example.com`, matching .NET invariant casing | No runtime storage policy change |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Contracts/Rendering/ReturnPathValidator.cs` -- `ContainsUnsafeCharacters` (around lines 192-207) classifies valid pairs but currently accepts lone surrogate code units; this is the shared renderer/auth security funnel and must remain netstandard2.0-safe.
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerReturnUrl.cs` -- `Sanitize` (lines 14-59) normalizes query-only input after its sole length check; `ContainsForbiddenCharacter` (lines 69-82) duplicates the invalid-character defense and has the same surrogate omission.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ReturnPathValidatorTests.cs` -- direct shared-surface attack theory; add invalid UTF-16 rows and a valid astral control.
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAuthRedirectorTests.cs` -- observable sanitizer/challenge surface; pin invalid Unicode and post-normalization cap behavior.
- `src/Hexalith.FrontComposer.Shell/Services/FrontComposerStorageKey.cs` -- read-only parity authority: `CanonicalizeUser` uses NFC plus `InvariantCulture` lowercasing for email-shaped identifiers.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/StorageKeysTests.cs` -- .NET-side golden vector anchors the Unicode email-shaped identity bytes used by the E2E mirror.
- `tests/e2e/page-objects/settings.page.ts` -- `SettingsPage.userSegment` (lines 130-133) currently uses contextual/full JavaScript lowercasing; reuse code-point iteration plus the U+0130 invariant exception before existing `escapeDataString`.
- `tests/e2e/specs/settings-persistence.spec.ts` -- add a serverless pure regression over the public `SettingsPage.storageKey` helper; no browser navigation or storage mutation is needed.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- read-only orchestration-owned ledger; never edit it.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Contracts/Rendering/ReturnPathValidator.cs` and `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ReturnPathValidatorTests.cs` -- reject unpaired/mispaired surrogate code units in the shared predicate and preserve valid astral scalars with direct regression rows.
- [x] `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerReturnUrl.cs` and `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAuthRedirectorTests.cs` -- mirror invalid-Unicode fail-closed behavior and enforce `MaxReturnUrlLength` on the normalized candidate.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/State/StorageKeysTests.cs`, `tests/e2e/page-objects/settings.page.ts`, and `tests/e2e/specs/settings-persistence.spec.ts` -- replace contextual/full JavaScript lowercasing with the smallest .NET-invariant-compatible code-point transformation and pin the same `İΣ@Example.COM` canonical key on the .NET runtime authority and TypeScript mirror.
- [x] `_bmad-output/implementation-artifacts/spec-dw-668-followup-review-11-4-security-validation-hardening.md` -- record implementation, review triage, validation evidence, and final file list without touching the deferred-work ledger.

**Acceptance Criteria:**
- Given a generated-command or authentication return path contains ill-formed UTF-16, when the shared validator or shell sanitizer evaluates it, then navigation fails closed without throwing or returning a replacement-character target.
- Given a well-formed local return path contains a valid non-format astral scalar, when the same surfaces evaluate it, then its existing local navigation behavior is preserved.
- Given shell query-only normalization would expand an exactly-at-cap input, when sanitization completes, then the returned value is `/` and never exceeds `MaxReturnUrlLength`.
- Given the E2E helper builds a storage key for a Unicode email-shaped user, when its output is compared with `FrontComposerStorageKey.CanonicalizeUser`, then casing and encoded bytes match without changing runtime key policy.

## Spec Change Log

## Review Triage Log

### 2026-08-28 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 2: (high 0, medium 0, low 2)
- defer: 2: (high 0, medium 1, low 1)
- reject: 18: (high 0, medium 0, low 18)
- addressed_findings:
  - `[low]` `[patch]` The TypeScript Unicode storage-key golden was self-referential; added the same `İΣ@Example.COM` byte vector to `StorageKeysTests` so the runtime authority and E2E mirror are independently pinned.
  - `[low]` `[patch]` The post-normalization overflow test omitted the adjacent supported boundary; added a query-only case that normalizes to exactly `MaxReturnUrlLength` and remains preserved.

## Verification

**Commands:**
- `dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj --configuration Release` -- passed for `net10.0` and `netstandard2.0`, 0 warnings / 0 errors.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReturnPathValidatorTests"` -- exact repository command is restore-blocked by NU1107 (`xunit.v3` 4.0.0 versus `xunit.v3.extensibility.core` 3.2.2); a temporary validation-only central-package overlay restoring xUnit 3.2.2 passed 73/73.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~FrontComposerAuthRedirectorTests|FullyQualifiedName~StorageKeysTests"` -- exact repository command remained restore-blocked by NU1107; a temporary validation-only overlay aligning xUnit at 3.2.2 and `FsCheck.Xunit.v3` with the catalog's FsCheck 3.3.3 built and ran the focused lane successfully: 59/59 passed, including the exact-cap query-only preservation boundary and .NET Unicode storage-key golden vector.
- `dotnet build src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj --configuration Release` -- passed, 0 warnings / 0 errors.
- `npm run typecheck` from `tests/e2e` -- passed.
- `PLAYWRIGHT_SKIP_WEBSERVER=1 npx playwright test specs/settings-persistence.spec.ts --project=chromium --grep "storage key helper"` from `tests/e2e` -- passed 1/1 without a web server.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release` -- exact repository command is restore-blocked by NU1107; the serialized Release build with the same temporary validation-only package overlay passed with 0 warnings / 0 errors. The overlay was deleted after validation and no dependency file was changed.
- `git diff --check` -- passed after implementation.

**Matrix test audit:**
- Ill-formed return paths and valid astral preservation ran in the focused .NET lanes: Contracts 73/73 and the combined Shell auth/storage lane 59/59.
- Both sides of the query-only normalization cap boundary ran in the focused Shell lane: a normalized length of exactly 2,048 is preserved and 2,049 falls back to `/`.
- Unicode storage-key parity is anchored by the .NET runtime golden vector in the 59/59 Shell lane and matched by the serverless Playwright regression: 1/1.

## File List

- `_bmad-output/implementation-artifacts/spec-dw-668-followup-review-11-4-security-validation-hardening.md` -- captured the baseline commit, completed task checkboxes, implementation notes, verification evidence, and final file list.
- `src/Hexalith.FrontComposer.Contracts/Rendering/ReturnPathValidator.cs` -- rejects lone and mispaired UTF-16 surrogate code units before Unicode-category classification while preserving valid astral scalars.
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerReturnUrl.cs` -- mirrors invalid-surrogate rejection and reapplies the return-URL cap after normalization.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ReturnPathValidatorTests.cs` -- pins lone-high, lone-low, mispaired, and valid-astral behavior.
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAuthRedirectorTests.cs` -- pins shell invalid-UTF-16 fallback, valid-astral preservation, and both sides of the query-only normalization cap boundary.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/StorageKeysTests.cs` -- anchors the TypeScript Unicode email-shaped storage-key vector to .NET invariant runtime canonicalization.
- `tests/e2e/page-objects/settings.page.ts` -- applies code-point-wise lowercase with the .NET invariant U+0130 exception before URL encoding.
- `tests/e2e/specs/settings-persistence.spec.ts` -- pins the canonical key for `İΣ@Example.COM` in a serverless pure helper test.

## Auto Run Result

Status: done

Summary: Completed the independent DW-668 confirmation across Story 11.4's return-path/auth and storage/DTO/status groups. The audit found and fixed three low-severity defects: malformed UTF-16 return paths now fail closed, query-only normalization cannot exceed the shell cap, and the E2E storage-key mirror now matches .NET invariant simple casing. The storage/ETag/DTO/status audit found no additional credible current gap beyond concerns already deferred by the original story.

Files changed:
- `src/Hexalith.FrontComposer.Contracts/Rendering/ReturnPathValidator.cs` -- rejects lone and mispaired UTF-16 surrogate code units while preserving valid astral scalars.
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerReturnUrl.cs` -- mirrors invalid-Unicode rejection and reapplies the length cap after normalization.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ReturnPathValidatorTests.cs` -- covers malformed UTF-16 and valid astral paths.
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAuthRedirectorTests.cs` -- covers invalid UTF-16 plus both sides of the normalized length boundary.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/StorageKeysTests.cs` -- pins the .NET Unicode storage-key golden vector.
- `tests/e2e/page-objects/settings.page.ts` -- mirrors .NET simple invariant lowercasing for email-shaped identities.
- `tests/e2e/specs/settings-persistence.spec.ts` -- pins the TypeScript side of the shared Unicode vector.
- `_bmad-output/implementation-artifacts/spec-dw-668-followup-review-11-4-security-validation-hardening.md` -- records scope, triage, evidence, and completion.

Review findings breakdown: 2 low patches applied; 2 pre-existing verification gaps deferred (1 medium, 1 low); 18 low findings rejected as disproven by the full artifact, already covered, speculative, or beyond the focused intent.

Follow-up review recommendation: false. Patched findings were high 0, medium 0, low 2; score = `3 × 0 + 1 × 2 = 2`, below the threshold of 5.

Verification: Contracts built Release for `net10.0` and `netstandard2.0` with 0 warnings/errors; focused Contracts tests passed 73/73; combined Shell auth/storage tests passed 59/59; Shell built Release with 0 warnings/errors; TypeScript typecheck passed; serverless Playwright passed 1/1; serialized Release solution build passed with 0 warnings/errors under a deleted validation-only package overlay; story artifact validation and `git diff --check` passed. Exact repository test and solution commands remain restore-blocked by the unchanged NU1107 central-package conflict recorded in `deferred`.

Residual risks: the checked-in dependency graph currently prevents normal .NET CI lanes from reaching tests, and the blocking Playwright workflow does not select the new settings-persistence regression. Both are pre-existing workflow/catalog concerns recorded above; no dependency, workflow, submodule, generated output, or deferred-work ledger file was changed.
