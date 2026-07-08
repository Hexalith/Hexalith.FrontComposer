---
title: '11.4 Security-validation hardening'
type: 'feature'
created: '2026-07-06T20:20:00+02:00'
status: 'done'
baseline_revision: '5a7a34072db4ec948f10db725b0fe8c79888ee75'
final_revision: '9ff5f6764bca9a0374df0621e6f101ed78895405'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-11-3-mcp-cross-request-lifecycle-and-operability.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** Story 11.4 closes security-validation gaps where return-path defenses, storage-key scope builders, and SignalR/HTTP DTO JSON shapes are security-relevant but not directly pinned. Without direct tests, open-redirect bypasses, tenant/user storage-key collisions, or silent wire-format drift can reach adopters before v1.0.

**Approach:** Implement three independently verifiable groups: an exhaustive `ReturnPathValidator` theory, storage-key canonicalization convergence with FsCheck equivalence, and golden JSON/constant pins for the required communication DTOs.

## Boundaries & Constraints

**Always:** Preserve the `Contracts` kernel netstandard2.0 target; keep `SourceTools` referencing only `Contracts`; preserve Story 11.1 token redaction and Story 11.3 MCP response shapes; use `ConfigureAwait(false)` on awaited production calls; keep storage-key logs/evidence free of raw tenant/user segments where touched.

**Block If:** `StorageKeys` cannot converge on `FrontComposerStorageKey` tenant/user canonicalization without changing persisted-key migration semantics for an already shipped public storage contract; DTO pinning requires a breaking wire rename not already captured by Story 11.4; or `ReturnPathValidator` cannot cover non-root-base traversal and the Unix leading-slash file-scheme carve-out with deterministic tests.

**Never:** Do not change command/projection routes, generated output path contracts, MCP lifecycle behavior, package versions, submodules, EventStore contracts, Contracts.UI split work, or broad schema/MCP/source-scan hardening outside the three Story 11.4 groups.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Redirect attack theory | Protocol-relative, backslash-prefixed, multi-percent-decoded slash/traversal, BiDi/zero-width, absolute scheme, Unix leading-slash path, and non-root-base traversal candidates | Safe local root-relative paths return `true`; every documented escape/spoof/traversal shape returns `false`; Unix-style `/path` remains allowed | No exception; invalid values are denied |
| Storage canonicalization | Whitespace, colon-bearing tenant/user values, NFD/NFC variants, and mixed-case email-shaped users across both builders | `StorageKeys` tenant/user segments use the same trim, NFC, email lowercase, and URL-encoding semantics as `FrontComposerStorageKey`; feature/discriminator segments keep their documented shape | Null/blank tenant or user still fail closed; no synthetic anonymous/default segment |
| Wire DTO JSON pins | `ProjectionChangedDetail`, `CommandResult`, `ProblemDetailsPayload`, and command result status values serialize/deserialize through repo-standard JSON options | Golden JSON uses stable camelCase member names and status constants (`Accepted`, `Rejected`) rather than ad hoc string literals | Unknown/missing optional values remain backward-compatible where existing consumers already permit them |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Contracts/Rendering/ReturnPathValidator.cs` -- canonical generated-command return-path guard; needs direct exhaustive theory and likely bounded fixpoint decoding.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ReturnPathValidatorTests.cs` -- new contract tests for redirect attack classes and base-path traversal.
- `src/Hexalith.FrontComposer.Shell/State/StorageKeys.cs` -- general persisted-state key builder that must converge on canonical tenant/user segments.
- `src/Hexalith.FrontComposer.Shell/Services/FrontComposerStorageKey.cs` -- existing canonical tenant/user segment semantics and comparison target.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/StorageKeysTests.cs` -- new/updated examples and FsCheck property for convergence.
- `src/Hexalith.FrontComposer.Contracts/Communication/ProjectionChangedDetail.cs` -- SignalR detail DTO requiring JSON shape pinning.
- `src/Hexalith.FrontComposer.Contracts/Communication/CommandResult.cs` -- HTTP command result DTO and status constants owner.
- `src/Hexalith.FrontComposer.Contracts/Communication/CommandResultStatus.cs` -- one-type-per-file status constants introduced for command-result producers and tests.
- `src/Hexalith.FrontComposer.Contracts/Communication/ProblemDetailsPayload.cs` -- bounded RFC 7807 projection requiring JSON shape pinning.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs`, `src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs`, and `src/Hexalith.FrontComposer.Testing/TestCommandService.cs` -- command-result producers updated to use the pinned accepted status constant.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/Story114WireFormatTests.cs` -- new golden JSON and round-trip tests.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- mark Story 11.4 in progress/review/done with validation evidence.

## Tasks & Acceptance

**Execution:**
- [x] `ReturnPathValidator.cs` and `ReturnPathValidatorTests.cs` -- add exhaustive direct theory for documented attack classes, including bounded repeated percent-decoding and non-root-base traversal -- closes H7 without relying on generated-renderer smoke tests.
- [x] `StorageKeys.cs`, `FrontComposerStorageKey.cs`, and `StorageKeysTests.cs` -- converge tenant/user segment canonicalization and add FsCheck equivalence over whitespace, colon, NFD/NFC, and mixed-case-email inputs while preserving feature/discriminator semantics -- closes H9.
- [x] `ProjectionChangedDetail.cs`, `CommandResult.cs`, `ProblemDetailsPayload.cs`, and `Story114WireFormatTests.cs` -- pin JSON member names/round trips and introduce `CommandResultStatus` string constants used by producers/tests -- closes M11.
- [x] `_bmad-output/implementation-artifacts/spec-11-4-security-validation-hardening.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml` -- record final status, file list, and validation evidence -- keeps BMAD artifacts consistent.

**Acceptance Criteria:**
- Given `ReturnPathValidator` receives every documented redirect attack class, when the direct theory runs, then unsafe paths are denied and safe root-relative paths, including the Unix leading-slash carve-out, are allowed.
- Given persisted-state keys are built for whitespace, colon, NFD/NFC, and mixed-case-email tenant/user inputs, when either storage-key builder is used, then tenant/user scope segments converge on canonical `FrontComposerStorageKey` semantics.
- Given communication DTOs cross SignalR or HTTP boundaries, when they serialize or deserialize as JSON, then `ProjectionChangedDetail`, `CommandResult`, `ProblemDetailsPayload`, and command result statuses remain pinned by tests or explicit JSON attributes/constants.

## Spec Change Log

- 2026-07-06: Implemented Story 11.4 and moved it to in-review. Added bounded repeated percent-decoding and direct return-path attack theories; converged `StorageKeys` tenant/user canonicalization on `FrontComposerStorageKey` without changing feature/discriminator segments; pinned communication DTO JSON member names and introduced `CommandResultStatus` constants used by command-result producers/tests. Focused Contracts lane passed 38/38 and focused Shell lane passed 70/70.

## Review Triage Log

### 2026-07-06 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 11: (high 3, medium 6, low 2)
- defer: 0
- reject: 1: (high 0, medium 1, low 0)
- addressed_findings:
  - `[high]` `[patch]` `ReturnPathValidator` allowed raw nested absolute URL tokens such as `/redirect?next=https://evil.example`; added raw and encoded coverage and reject `://` tokens in validated paths.
  - `[medium]` `[patch]` `ReturnPathValidator` missed decoded NBSP, line/paragraph separators, and word-joiner spoofing characters; aligned its forbidden character set with the shell auth sanitizer.
  - `[medium]` `[patch]` `ReturnPathValidator` missed `..` segments immediately before query or fragment delimiters; stripped query/fragment before traversal-segment checks and added direct coverage.
  - `[high]` `[patch]` Shell auth return URLs still allowed traversal and malformed percent-encoding could throw; routed the sanitizer through `ReturnPathValidator`, kept existing shell-specific guards, and added shell tests.
  - `[medium]` `[patch]` `ETagCacheService.RemoveByProjectionTypeAsync` built raw tenant/user prefixes after canonicalized writes; switched invalidation to `StorageKeys.BuildKey(..., "etag") + ":"` and added canonical-prefix coverage.
  - `[medium]` `[patch]` `ETagCacheService.TryBuildKey` still rejected colon-bearing tenant/user values after canonicalization; removed the stale reject path and updated invalid/valid key tests.
  - `[medium]` `[patch]` `StorageKeys` accepted null/blank/separator-bearing features and blank discriminators, leaving a 3-arg/4-arg collision path; added guards and tests while preserving colon-bearing discriminators.
  - `[medium]` `[patch]` `ProblemDetailsPayload` could deserialize missing error collections as null despite its invariant; coalesced missing collections to empty values and pinned the behavior.
  - `[low]` `[patch]` `ProblemDetailsPayload` only pinned `rejectionDetails: null`; added a non-null nested `CommandRejectionDetails` golden JSON case.
  - `[low]` `[patch]` `settings.page.ts` duplicated the old raw storage-key algorithm; updated the e2e helper to trim, NFC-normalize, URL-encode, and lowercase email users.
  - `[low]` `[patch]` Story evidence omitted validation for the touched Testing package; ran the Testing test project and recorded the result.

### 2026-07-06 — Follow-up review pass
- intent_gap: 0
- bad_spec: 0
- patch: 12: (high 2, medium 6, low 4)
- defer: 1: (high 0, medium 1, low 0)
- reject: 4: (high 0, medium 2, low 2)
- addressed_findings:
  - `[high]` `[patch]` Replacing `Uri.IsWellFormedUriString` with `Uri.TryCreate(Relative)` silently loosened `ReturnPathValidator` to accept raw `<`, `>`, `"`, backtick, `{`, `}`, `|`, `^` (e.g. `/orders/<script>alert(1)</script>`); restored the strictness with an explicit forbidden-character set (`IsWellFormedUriString` itself cannot return because it also rejects legitimate `#fragment` relative references) and pinned raw markup-character attack rows.
  - `[high]` `[patch]` The bounded decode loop never re-checked exotic whitespace on decoded forms, so `/orders/%E2%80%89hidden` (thin space) passed while its raw form was rejected; non-ASCII whitespace is now unsafe in raw and decoded forms (ASCII space stays legal only percent-encoded, preserving `/files/report%20summary`) with encoded thin/ideographic-space rows pinned.
  - `[medium]` `[patch]` `IsDisplaySpoofingChar` and the shell sanitizer both missed soft hyphen (U+00AD) and Arabic letter mark (U+061C), and the two sets had drifted (directional isolates only in the validator); added both codepoints to both sets, added the isolates to the shell set, and pinned raw+encoded rows.
  - `[medium]` `[patch]` The nested-absolute-URL token reject only matched `://`, letting browser-lenient single-slash forms such as `/redirect?next=https:/evil.example` through; broadened the reject to `:/` and pinned it.
  - `[medium]` `[patch]` The mid-string `//` reject applied only to decoded forms, so `/foo//bar` was accepted raw but rejected once unrelated percent-encoding appeared; the check now applies to raw and decoded forms alike and both shapes are pinned.
  - `[medium]` `[patch]` `ProjectionChangedDetail.Metadata` deserialized to null when the member was absent or null despite its non-nullable contract (unlike the coalesced `ProblemDetailsPayload` collections); coalesced missing/null metadata to an empty dictionary and pinned both round trips.
  - `[medium]` `[patch]` The `[Property]` convergence test iterated hardcoded arrays and used FsCheck only to pick a discriminator, overstating the claimed FsCheck equivalence; replaced with a generated `StorageIdentityCase` arbitrary (core x padding x NFC/NFD x letter-case decorations) exercising both `BuildKey` overloads.
  - `[medium]` `[patch]` `ReturnPathValidator` and `FrontComposerReturnUrl` carried `catch (UriFormatException)` blocks that never execute on modern .NET (`Uri.UnescapeDataString` returns malformed sequences unchanged) while the story attributed malformed-percent fail-closed behavior to them; removed the dead net10 shell catch, documented the kernel catch as a legacy-netstandard2.0 safety net, and clarified that `HasInvalidPercentEncoding` is the actual malformed-percent guard.
  - `[low]` `[patch]` `ETagCacheService.TryBuildKey` kept a stale "StorageKeys also enforces the colon guard" comment after canonicalization started URL-encoding colons; rewrote the comment to match the fail-closed intent.
  - `[low]` `[patch]` The property test guarded a value-type FsCheck `NonNegativeInt` with `ArgumentNullException.ThrowIfNull` (can never throw); removed with the property rewrite.
  - `[low]` `[patch]` The e2e storage-key helper diverged from `Uri.EscapeDataString`/.NET `Trim` semantics (`!'()*` left unencoded; BOM/NEL trim mismatch); added strict RFC 3986 encoding and a .NET-whitespace-set trim.
  - `[low]` `[patch]` The residual-risk note understated that canonicalization re-keys all persisted per-user UI state (theme, density, navigation, palette, capability discovery, datagrid, etag) for email-shaped users on upgrade; broadened the Auto Run Result residual-risk statement.

### 2026-07-06 — Follow-up review pass 2
- intent_gap: 0
- bad_spec: 0
- patch: 4: (high 0, medium 2, low 2)
- defer: 2: (high 0, medium 1, low 1)
- reject: 9
- addressed_findings:
  - `[medium]` `[patch]` `ReturnPathValidator`'s bounded decode loop re-checked only whitespace/spoofing/shape, so percent-encoded RFC 3986 forbidden characters (`/orders/%3Cscript%3E`, `/a/%22b%22`) and astral/BMP Unicode format (Cf) code points (U+E0001, U+2061) passed while their raw forms were rejected. Rewrote `ContainsUnsafeCharacters` as a code-point-aware scan rejecting control + non-ASCII whitespace + forbidden-URI chars + the entire Cf category (netstandard2.0-safe via `CharUnicodeInfo.GetUnicodeCategory(string, index)`), run on the raw path and every decoded form, and removed the now-subsumed `IsDisplaySpoofingChar` denylist; pinned encoded-forbidden, astral (raw + encoded), and BMP-format (raw + encoded) rows. The principled `%25zz` malformed-percent asymmetry is deliberately preserved (`HasInvalidPercentEncoding` is still not re-run on decoded forms).
  - `[medium]` `[patch]` `ETagCacheService.RemoveByProjectionTypeAsync` called `StorageKeys.BuildKey` unguarded, so a non-blank-but-invalid-Unicode identity (unpaired surrogate) threw `ArgumentException` out of the Fluxor invalidation path instead of the method's designed log-and-skip; wrapped the call in `try/catch (ArgumentException)` mirroring `TryBuildKey`, emitting the P14 sanitized warning, and pinned a no-throw regression test.
  - `[low]` `[patch]` Aligned the shell `FrontComposerReturnUrl.ContainsForbiddenCharacter` guard with the validator by replacing its enumerated format-char denylist (which missed U+2061–2064, U+180E, U+FFF9–FFFB, and every astral format code point) with the same Cf-category + non-ASCII-whitespace, code-point-aware scan, and removed the stale `IsDisplaySpoofingChar` cross-reference.
  - `[low]` `[patch]` Corrected the misleading `ETagCacheService.TryBuildKey` catch comment (the `ArgumentException` catch fires today for invalid-Unicode identities, not only "if a future guard tightens") and documented the invalid-Unicode `ArgumentException` on both `StorageKeys.BuildKey` overloads.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReturnPathValidatorTests|FullyQualifiedName~Story114WireFormatTests"` -- passed: 38/38.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~StorageKeysTests|FullyQualifiedName~ETagCacheServiceTests|FullyQualifiedName~DataGridNavigationEffectsTests|FullyQualifiedName~FrontComposerAuthRedirectorTests"` -- passed: 70/70.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReturnPathValidatorTests|FullyQualifiedName~Story114WireFormatTests"` -- passed after review patches: 46/46.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~StorageKeysTests|FullyQualifiedName~ETagCacheServiceTests|FullyQualifiedName~DataGridNavigationEffectsTests|FullyQualifiedName~FrontComposerAuthRedirectorTests"` -- passed after review patches: 80/80.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj --configuration Release` -- passed: 30/30.
- `npm run typecheck` from `tests/e2e` -- passed.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-11-4-security-validation-hardening.md` -- passed after review patches and file-list update.
- `git diff --check` -- passed.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release` -- passed with 0 warnings and 0 errors.

**Follow-up review pass (2026-07-06):**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/... --filter "FullyQualifiedName~ReturnPathValidatorTests|FullyQualifiedName~Story114WireFormatTests"` -- passed after follow-up patches: 66/66.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/... --filter "FullyQualifiedName~StorageKeysTests|FullyQualifiedName~ETagCacheServiceTests|FullyQualifiedName~DataGridNavigationEffectsTests|FullyQualifiedName~FrontComposerAuthRedirectorTests"` -- passed after follow-up patches: 80/80.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/... --filter "FullyQualifiedName~SignalRProjectionHubConnectionFactoryTests|FullyQualifiedName~ProjectionSubscriptionServiceTests"` -- passed (ProjectionChangedDetail consumers): 32/32.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/... --configuration Release` -- passed: 30/30.
- `npm run typecheck` from `tests/e2e` -- passed.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release` -- passed with 0 warnings and 0 errors.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-11-4-security-validation-hardening.md` -- passed after follow-up artifact updates.
- `git diff --check` -- passed.

**Follow-up review pass 2 (2026-07-06):**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/... --filter "FullyQualifiedName~ReturnPathValidatorTests|FullyQualifiedName~Story114WireFormatTests"` -- passed after follow-up-2 patches: 76/76 (added 10 encoded-forbidden / astral / BMP-format attack rows).
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/... --filter "FullyQualifiedName~StorageKeysTests|FullyQualifiedName~ETagCacheServiceTests|FullyQualifiedName~DataGridNavigationEffectsTests|FullyQualifiedName~FrontComposerAuthRedirectorTests"` -- passed after follow-up-2 patches: 81/81 (added the invalid-Unicode fail-closed regression test).
- `dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj --configuration Release` -- passed for BOTH `net10.0` and `netstandard2.0` (0 warnings, 0 errors), confirming the `CharUnicodeInfo` code-point scan compiles on the netstandard2.0 kernel.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release` -- passed with 0 warnings and 0 errors.
- `git diff --check` -- passed.

## File List

- `_bmad-output/implementation-artifacts/spec-11-4-security-validation-hardening.md` -- story task status, implementation notes, verification evidence, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- moved Story 11.4 to review, then done; follow-up review pass noted.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- new deferred entry for the `retryAfter` TimeSpan wire-format decision (follow-up review pass).
- `src/Hexalith.FrontComposer.Contracts/Communication/CommandResult.cs` -- pinned command-result JSON member names and referenced status constants.
- `src/Hexalith.FrontComposer.Contracts/Communication/CommandResultStatus.cs` -- added stable accepted/rejected string constants.
- `src/Hexalith.FrontComposer.Contracts/Communication/ProblemDetailsPayload.cs` -- pinned ProblemDetails JSON member names including rejection details.
- `src/Hexalith.FrontComposer.Contracts/Communication/ProjectionChangedDetail.cs` -- pinned SignalR detail JSON member names.
- `src/Hexalith.FrontComposer.Contracts/Rendering/ReturnPathValidator.cs` -- added bounded repeated percent-decoding and safer root-relative validation; follow-up pass restored forbidden raw URI characters, rejected non-ASCII whitespace in raw and decoded forms, broadened the scheme-token reject to `:/`, made the mid-string `//` reject mode-consistent, and added U+00AD/U+061C to the spoofing set.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs` -- used `CommandResultStatus.Accepted` for EventStore accepted results.
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerReturnUrl.cs` -- aligned shell auth return URL sanitization with `ReturnPathValidator`; malformed-percent rejection is delegated to the validator (the never-firing net10 `UriFormatException` catch was removed in the follow-up pass) and the forbidden-format-character set now matches the validator's superset.
- `src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs` -- used `CommandResultStatus.Accepted` for stub accepted results.
- `src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs` -- aligned ETag cache key construction and projection-family invalidation with canonical tenant/user segments.
- `src/Hexalith.FrontComposer.Shell/State/StorageKeys.cs` -- canonicalized tenant/user segments with `FrontComposerStorageKey` semantics.
- `src/Hexalith.FrontComposer.Testing/TestCommandService.cs` -- used `CommandResultStatus.Accepted` for testing evidence and results.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/Story114WireFormatTests.cs` -- added DTO golden JSON and round-trip pins, plus missing/null-metadata coalescing pins for `ProjectionChangedDetail`.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ReturnPathValidatorTests.cs` -- added direct redirect/traversal/spoofing return-path theories; follow-up pass added forbidden-character, encoded-exotic-whitespace, soft-hyphen/ALM, single-slash-scheme, and mid-string double-slash rows plus Unicode-letter/colon-segment keep-true rows.
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAuthRedirectorTests.cs` -- added auth return URL traversal, nested absolute URL, and malformed-percent regression cases.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ETagCache/ETagCacheServiceTests.cs` -- added canonical ETag key and projection-family invalidation regression cases.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/StorageKeysTests.cs` -- added examples and FsCheck convergence coverage for identity segment canonicalization; follow-up pass replaced the hardcoded-matrix property with a generative `StorageIdentityCase` arbitrary.
- `tests/e2e/page-objects/settings.page.ts` -- aligned e2e localStorage key helper with canonical storage-key semantics, including `Uri.EscapeDataString` strict encoding (`!'()*`) and .NET-whitespace trim parity.

## Auto Run Result

Status: done

Summary: Implemented Story 11.4 security-validation hardening across the three required groups, then ran an independent follow-up review pass (fresh Blind Hunter + Edge Case Hunter over the full baseline diff). `ReturnPathValidator` now has direct attack-class coverage plus bounded repeated percent-decoding, raw nested absolute URL rejection (`:/` token, covering browser-lenient single-slash schemes), decoded spoofing-character and non-ASCII-whitespace rejection, forbidden raw URI characters (`<`, `>`, `"`, backtick, `{`, `}`, `|`, `^`), mode-consistent mid-string `//` rejection, and non-root-base traversal checks. Shell auth return URLs reuse the hardened validator with an aligned forbidden-character superset. `StorageKeys` canonicalizes tenant/user segments with `FrontComposerStorageKey` semantics, proven by a generative FsCheck convergence property; ETag cache key construction and projection-family invalidation use the canonical shape; the e2e storage-key helper is byte-identical to the runtime algorithm including `Uri.EscapeDataString` strict encoding and .NET whitespace trimming. Communication DTO JSON shapes are pinned with explicit member names, `CommandResultStatus` constants, and never-null collection invariants on both `ProblemDetailsPayload` and `ProjectionChangedDetail`.

Files changed: Contracts communication DTOs and tests; `ReturnPathValidator` and tests; Shell auth return URL sanitizer and tests; Shell storage/ETag key builders and tests; command-result producers in Shell and Testing; e2e settings page-object storage-key helper; BMAD story/sprint artifacts and deferred-work ledger.

Review findings breakdown: first pass addressed 11 patch findings (high 3, medium 6, low 2) with 1 reject. Follow-up pass addressed 12 patch findings (high 2, medium 6, low 4), deferred 1 (the `retryAfter` TimeSpan wire-format decision, recorded in the deferred-work ledger), and rejected 4 (orphaned legacy-format etag entries are LRU-evictable; the sanitizer/validator double-validation is deliberate defense-in-depth; deeper-decode malformed-percent asymmetry is principled because the wire form is well-formed and rejecting it would false-positive legitimate `%25` payloads; lone-surrogate identity input failing closed with an exception is preferable to replacement-character key collisions). 0 intent gaps and 0 bad-spec loopbacks across both passes. Follow-up review recommended: the follow-up pass again changed security-validation behavior in the return-path guard across four attack classes and altered a wire DTO deserialization invariant — an independent confirmation pass is warranted before v1.0 freeze even though every change is pinned by direct tests.

Verification: after follow-up patches the focused Contracts lane passed 66/66, the focused Shell lane 80/80, the SignalR projection lane (ProjectionChangedDetail consumers) 32/32, and the full Testing project 30/30; e2e `npm run typecheck` passed; Release solution build passed with 0 warnings and 0 errors; story artifact validation and `git diff --check` passed.

Residual risk: `StorageKeys` canonicalization re-keys persisted per-user UI state — theme, density, navigation, command palette, capability discovery, DataGrid views, and the ETag cache — for any tenant/user whose raw identity differs from its canonical form. Because email-shaped user ids are the documented common case (lowercased and `@` URL-encoded), adopters upgrading across this story should expect a one-time silent reset of those persisted preferences (values rehydrate through normal defaults; no data corruption). Old-format etag entries left in localStorage are only reclaimed through LRU eviction, not projection-family invalidation. The hardened validator is stricter than before: paths containing raw `<>"`{}|^``, mid-string `//`, `:/` tokens, or exotic whitespace are now rejected — hosts that legitimately used such shapes (unlikely, all are non-RFC3986 or attack-adjacent) would see returns fall back to `/`.

**Follow-up review pass 2 (2026-07-06):** An independent third pass (fresh Blind Hunter + Edge Case Hunter over the full baseline diff, same model capability) found no high-severity exploitable bypass — both reviewers confirmed the return-path guard is strictly stronger than before and the storage-key canonicalization is collision-safe. Four patches were applied (2 medium, 2 low): (1) the `ReturnPathValidator` bounded decode loop now rejects percent-encoded RFC 3986 forbidden characters and the entire Unicode format (Cf) category — including astral code points such as U+E0001 — on the raw path and every decoded form, via a code-point-aware scan (netstandard2.0-safe `CharUnicodeInfo.GetUnicodeCategory`), closing the encoded-`<script>` / invisible-format-char asymmetry while deliberately preserving the principled `%25zz` malformed-percent behavior; (2) `ETagCacheService.RemoveByProjectionTypeAsync` now fails closed (log-and-skip) on invalid-Unicode identities instead of throwing out of the Fluxor invalidation path; (3) the shell `FrontComposerReturnUrl` format-char guard was aligned to the same Cf-category scan; (4) doc/comment accuracy fixes on the `StorageKeys` / `TryBuildKey` invalid-Unicode throw. Two findings were deferred as NEW deferred-work ledger entries (latent DTO JSON wire pins + required-scalar null defense; full-string email lowercasing cross-user state collapse). Nine were rejected — including the already-adjudicated `%25zz` asymmetry, the by-design read-side `CommandResult.Status`, the intentional `:/`-in-query defense-in-depth, the near-tautological FsCheck convergence property (semantics already pinned by example tests), and the documented storage re-keying residual risk. 0 intent gaps, 0 bad-spec loopbacks. After patches: Contracts lane 76/76, Shell lane 81/81, Contracts builds clean on net10.0 + netstandard2.0, full Release solution build 0 warnings / 0 errors, `git diff --check` clean. Follow-up review still recommended: this pass rewrote the return-path character-safety predicate (enumerated denylist → Unicode-category classification, whose behavior depends on the host Unicode tables) and changed a cache fail-closed path — although both changes are tightening-only and fully test-pinned, one independent confirmation before the v1.0 freeze is warranted. The scope is materially smaller than the prior two passes (4 vs 11/12 patches), so this is a lower-urgency recommendation.
