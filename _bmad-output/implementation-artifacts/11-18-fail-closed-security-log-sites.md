---
baseline_commit: 0a84e818b0ce220f291510ad094340f7296bb488
---
# Story 11.18a: Fail-closed and security log sites

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- Type: security/observability remediation; first executable child of the non-implementable Story 11.18 decomposition parent. -->

## Story

As a FrontComposer maintainer,
I want the remaining MCP and Shell fail-closed/security branches to use source-generated, sanitized logging,
so that operators can diagnose denials and safe degradations without exposing credentials, payloads, stack traces, or sensitive identifiers.

## Acceptance Criteria

This story refines PRD **FR-29**, canonical **NFR-6** (privacy/support safety), and canonical **NFR-10** (observability), and closes only the security-first child of architecture finding **M15**. The parent Story 11.18 remains a decomposition record, not an executable story.

1. **The executable scope is reconciled and frozen.** **Given** the July 4 M15 estimate of 206 direct Shell log sites across 50 files is historical, **when** implementation starts after the Story 11.17 file movement is settled, **then** a Roslyn-backed census is recorded against the actual implementation-start tree. The creation-time minimum scope is the exact 36 direct calls in the 13 files listed in **Current-State Security Logging Map** (four MCP calls and 32 Shell calls). Every listed call is either migrated in this story or, if its branch no longer exists, reconciled with the commit that removed it. Any newly discovered direct call meeting the fail-closed/security rule below is added to the frozen scope and migrated; the denominator is never silently reduced.

2. **Every in-scope call uses source-generated logging.** **Given** an in-scope branch emits a diagnostic, **when** the story is complete, **then** it invokes a `[LoggerMessage]`-generated method through `FrontComposerMcpLog` or the new eponymous Shell helper `FrontComposerSecurityLog`; no direct `LogTrace`, `LogDebug`, `LogInformation`, `LogWarning`, `LogError`, or `LogCritical` invocation remains in the 13-file minimum scope. Existing severity, exactly-once branch cardinality, diagnostic IDs, and non-sensitive structured field meanings are preserved; a sensitive field is replaced by an explicitly named count, digest, presence flag, or bounded category rather than retaining its old raw key. Event IDs are unique and pinned: MCP extends the existing 8310-8314 family from 8315 upward; Shell reserves 5660-5699 for this child without changing the existing 5601-5650 events.

3. **Security logs are support-safe by construction.** **Given** adversarial values are supplied as tokens, tenant/user or API-key secrets, JWT/payload fragments, exception messages, claim values/aliases, command/projection/policy/bounded-context/route names, correlation/message IDs, storage keys, or other sensitive identifiers, **when** every in-scope branch logs, **then** none of those raw values and no exception object, exception message, or stack trace reaches the captured entry. Logs contain only bounded enums/categories, diagnostic IDs, booleans, counts, allowlisted provider kinds, exception type names, and stable `sha256:` pseudonyms produced after an `IsEnabled` check when correlation is required. Missing-policy collections expose a count and optional digest, never the raw joined policy list. Captured `Exception` is null for every event in this security lane.

4. **Fail-closed behavior does not change.** **Given** the current MCP and Shell paths reject, hide, suppress, defer, or safely degrade work, **when** their logging is migrated, **then** response categories, hidden/unknown equivalence, HTTP/problem-details shape, exception type and inner-exception behavior, authorization decisions, cancellation propagation, token acquisition, claim caching, tenant/user storage suppression, CTA suppression, severities, state transitions, DI lifetimes, and whether downstream dispatch executes remain unchanged. Story 11.3's MCP EventIds 8310-8314 and Shell tenant EventId 5650 remain active and are audited rather than duplicated.

5. **Governance is non-vacuous and regression-resistant.** **Given** source-generated logging can regress without a build failure, **when** the MCP and Shell Governance lanes run, **then** scoped Roslyn guards: scan a non-empty production census; pin the reconciled file/member/event inventory; reject direct `ILogger.Log*` calls in scope; require `[LoggerMessage]` helpers with unique IDs and structured PascalCase placeholders; reject generated security-event signatures with an `Exception` parameter or generated-method calls that pass `ex`, `ex.Message`, stack/payload/token material, or unredacted identifier expressions; require any free-form identifier to cross only an audited wrapper that sanitizes it after `IsEnabled`; and report repository-relative offenders. Synthetic negatives prove a direct `LogWarning`, a raw generated exception parameter, a duplicate EventId, and an empty/over-broad allowlist are detected. The guards live outside the production set they inspect.

6. **Focused and broad validation is green.** **Given** the migration and guards are complete, **when** validation runs, **then** the Release solution build, focused MCP/Shell logging-security tests, full MCP test project, broad Shell non-Contract lane, MCP and Shell Governance lanes, artifact validation, and `git diff --check` pass with warnings treated as errors. No analyzer policy, package version, public API, generated output, UI/UX asset, localization, schema/wire contract, or submodule change is introduced. Story 11.18b continues to own the remaining Shell Warning+ migration and Story 11.18c continues to own lifecycle/projection/polling hot paths and the final direct-call ledger.

## Tasks / Subtasks

- [x] **Task 1 — Freeze the implementation baseline and security census (AC: #1, #4, #6)**
  - [x] Wait for or incorporate the final Story 11.17d file topology, record the implementation-start commit, and inventory tracked/untracked/root-gitlink state before editing. Preserve all unrelated Story 11.17 work and shared sprint-history changes.
  - [x] Re-run the full direct-call census for MCP and Shell with Roslyn, not a regex-only completion claim. Record the full counts and the exact 13-file/36-call minimum scope below; reconcile any path or count drift in this story before production changes.
  - [x] Apply this deterministic inclusion rule: a direct log site is in 11.18a when its branch denies/hides/suppresses side effects or resource exposure because authentication, token relay, tenant/user context, authorization policy, safe internal routing, MCP admission/schema/downstream handling, or tenant/user-scoped persistence cannot be trusted; or when it reports invalid/missing security configuration. Ordinary non-security Warning+ sites remain 11.18b, and lifecycle/projection/polling performance sites remain 11.18c.
  - [x] Audit existing Story 11.3 events 8310-8314 and Shell tenant event 5650 as preservation evidence. Do not count them as residual direct calls or create duplicate events.

- [x] **Task 2 — Add collision-free source-generated security helpers (AC: #2, #3, #5)**
  - [x] Extend `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs` for the four residual MCP sites using IDs 8315 upward. Preserve the wrapper pattern: nullable logger where required, `IsEnabled` before hashing/formatting, bounded category/type conversion, and private generated partial methods.
  - [x] Add `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerSecurityLog.cs` as one eponymous `internal static partial` helper type using IDs 5660-5699. Do not create `FrontComposerLog.Security.cs`: the Story 11.17d filename/declaration guard rejects a second non-eponymous `FrontComposerLog` partial file.
  - [x] Implement a bounded sanitizer for free-form identifiers only when a stable forensic handle is required: trim, UTF-8 encode, SHA-256 hash, emit `sha256:` plus the first eight bytes as lowercase hex, and zero temporary byte/hash buffers. Prefer bounded category, count, presence flag, or exception type over hashing when correlation is unnecessary.
  - [x] Never declare an `Exception` parameter on 11.18a generated methods. Convert caught failures to `ex.GetType().FullName ?? "Exception"` only after preserving the original catch/throw/result behavior.

- [x] **Task 3 — Migrate the residual MCP sites (AC: #2, #3, #4)**
  - [x] Replace the three direct calls in `FrontComposerMcpCommandInvoker` with generated events for schema-category failure, known MCP failure, and unexpected downstream failure. Remove the stale comment that deliberately preserved exception stacks; the unexpected event records only `DownstreamFailed` plus exception type.
  - [x] Replace `SchemaNegotiationRuntimeGate`'s non-exact decision log with a generated Information event whose fields remain bounded (`Category`, `MessageKey`, `DocsCode`, `DecisionKind`) and contain no fingerprint, path, tenant, descriptor, or request value.
  - [x] Preserve every current `FrontComposerMcpResult`, structured failure payload, cancellation arm, command rejection/validation behavior, lifecycle handle, and hidden/unknown response.

- [x] **Task 4 — Migrate the Shell auth, authorization, scope, and CTA sites (AC: #2, #3, #4)**
  - [x] Replace all 32 direct calls in the 11 Shell files in the map with `FrontComposerSecurityLog` wrappers/generated methods while preserving their current levels and diagnostic IDs HFC2012/HFC2013/HFC2014/HFC2105.
  - [x] For token/claim paths, log no token, claim value, raw alias list, user key, client secret, provider exception, or exception object. Preserve token-store lookup, authentication exception/inner-exception behavior, and per-principal claim-result caching.
  - [x] For authorization paths, replace raw command type, policy, bounded-context, route, projection/command name, correlation ID, and missing-policy list fields with bounded counts/categories/presence flags or stable pseudonyms. Preserve Pending/Canceled/Denied/FailedClosed distinctions, generic forbidden payloads, policy-catalog strict-mode throw behavior, and the invariant that denied dispatch never reaches the inner command service.
  - [x] For storage/LastUsed/CTA paths, preserve fail-closed return values, D31/HFC2105 behavior, feature/direction attribution, CTA selection/suppression and internal-route checks. Remove raw exception/identifier fields without changing the branch result.

- [x] **Task 5 — Add sanitization, event-contract, and behavioral tests (AC: #2, #3, #4)**
  - [x] Extend MCP command-invoker/schema tests to assert one entry, exact level/EventId/event name, bounded structured keys, null captured exception, unchanged result category, and absence of raw tenant/user/tool/argument/payload/JWT/exception/correlation sentinels. Retain existing Story 11.3 redaction assertions as regression coverage.
  - [x] Strengthen Shell auth, authorization, storage-scope, LastUsed, CTA, component-region, and authentication-extension tests with adversarial sentinels. Assert event ID/level/cardinality, null captured exception, and behavior preservation—not message substring alone.
  - [x] Where current test loggers capture only formatted text, extend the shared/local capture seam to retain `EventId`, structured state, level, formatted message, and exception without weakening existing assertions or creating multi-type production files.

- [x] **Task 6 — Add scoped governance and run the full validation lane (AC: #1, #5, #6)**
  - [x] Add one eponymous Governance test type per test project, reusing the repository's reviewed Roslyn patterns: non-empty source location, explicit narrow inventory, unique event-ID scan, direct-call classification, repository-relative diagnostics, and synthetic negatives.
  - [x] Pin the exact post-migration inventory and document every remaining direct MCP/Shell call outside this child as owned by 11.18b, 11.18c, or an explicit below-threshold/intentional rationale; do not use folder-wide or wildcard exemptions.
  - [x] Run the commands in **Testing Requirements**, reconcile the complete story-owned File List from tracked plus untracked paths, audit root gitlinks separately, check for received/generated artifacts, and record results before promotion to review.

## Dev Notes

### Authoritative Scope and Decomposition

1. **This is Story 11.18a, not Story 11.18.** The canonical Epic 11 plan explicitly marks 11.18 as a non-implementable decomposition parent. The later approved/applied mapping supersedes the older suggestion: 11.18a is fail-closed/security, 11.18b is all remaining Shell Warning+, and 11.18c is hot paths.
2. **Canonical PRD numbering applies.** Here NFR-6 means privacy/support safety and NFR-10 means observability. Older epic text used those numbers for different legacy requirements; do not use the legacy meanings for this child.
3. **The July estimate is provenance, not truth.** Creation-time text scanning finds 240 direct calls across 60 Shell files, including 144 Warning+ calls across 48 files, plus four direct calls across two MCP files. Those figures can drift with Story 11.17d; the implementation-start Roslyn census is authoritative.
4. **The 36-call minimum is deliberately security-first.** It covers residual MCP failure decisions and the Shell auth/token/policy/identity-scope/CTA ownership set. The semantic rule in Task 1 prevents a path rename from evading the guard while keeping ordinary warnings and hot paths independently reviewable.
5. **No UI change is intended.** `FcAuthorizedCommandRegion` behavior and user-visible Pending/NotAuthorized output are preservation targets. No Razor markup, CSS, JS, route, copy, focus, responsive, accessibility, or Fluent UI change belongs here.

### Current-State Security Logging Map

The counts below are creation-time direct `ILogger.Log*` invocations on the live Story 11.17d-shaped working tree. Reconcile them against the implementation-start commit before editing.

| Current UPDATE file | Direct sites | Required migration / preservation focus |
|---|---:|---|
| `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs` | 3 | Generated MCP schema/known/unexpected failure events; remove raw exception stack while preserving result taxonomy and cancellation. |
| `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiationRuntimeGate.cs` | 1 | Generated bounded non-exact schema-decision Information event; no fingerprint/request/descriptor values. |
| `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.razor.cs` | 1 | Generated evaluator-failure warning; no exception object or raw command/policy/correlation identifier; preserve fail-closed rendering. |
| `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs` | 1 | Generated bridge-replacement Information event; preserve EventStore token-provider replacement behavior. |
| `src/Hexalith.FrontComposer.Shell/Services/Auth/ClaimsPrincipalUserContextAccessor.cs` | 1 | Generated HFC2012 warning; log bounded reason/counts, not raw alias/value lists; preserve cache. |
| `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs` | 3 | Generated HFC2013/HFC2014 warnings; no token/broker message/exception; preserve exceptions and relay behavior. |
| `src/Hexalith.FrontComposer.Shell/Services/Auth/ServerCircuitUserContextAccessor.cs` | 1 | Same safe HFC2012 contract as request accessor; preserve circuit principal resolution/cache. |
| `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs` | 6 | Generated fail-closed/blocked events; bounded reason/type plus pseudonyms only; preserve decision mapping. |
| `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandDispatchAuthorizationGate.cs` | 9 | Generated direct-dispatch deny/defer/cancel events; preserve severity, cancellation, warning payload, and no-inner-dispatch invariant. |
| `src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs` | 2 | Generated empty/missing catalog warnings; count/digest only, strict mode unchanged. |
| `src/Hexalith.FrontComposer.Shell/Services/DerivedValues/LastUsedValueProvider.cs` | 1 | Generated D31 warning; preserve one-time diagnostic and fail-closed derived-value behavior. |
| `src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs` | 5 | Generated registry/match/BC/internal-route warnings with safe pseudonyms; preserve deterministic selection/suppression. |
| `src/Hexalith.FrontComposer.Shell/Services/StorageScopeResolver.cs` | 2 | Generated HFC2105 Information events; preserve feature/direction attribution and empty-out-parameter contract. |

### Architecture and Implementation Guardrails

- Keep source dependencies inward. Both logging helpers remain internal implementation details; no Contracts, package, or public API addition is authorized.
- Use C# `latest`, file-scoped namespaces, Allman braces, one direct top-level type per file, and warnings as errors. Generated methods follow the repository convention: declaring type `static partial`, `ILogger` first, named PascalCase placeholders, and no interpolation.
- Do not create a `FrontComposerLog.Security.cs` partial companion. Story 11.17d's organization guard enforces eponymous one-type files and exact modifiers. A distinct `FrontComposerSecurityLog.cs` avoids weakening that guard.
- Source-generated methods perform enabled checks by default, but arguments are evaluated before the method call. Any hashing, joining, `ToString`, digest construction, or other non-trivial formatting belongs behind a wrapper-level `logger.IsEnabled(level)` check.
- In normal logging migrations an `Exception` may be the second generated parameter, but this security child is stricter: exception output can include the message and stack. Use exception type/category only, and assert the captured exception is null.
- Preserve levels and branch cardinality. A migration may intentionally introduce non-zero EventIds/EventNames; pin those changes. Do not coalesce distinct events in a way that loses the failure reason, and do not add duplicate logging at wrapper and caller.
- Hashing is pseudonymization, not permission to log arbitrary data. Prefer omission/count/presence/category. Never hash or log raw tokens, API keys, client secrets, JWTs, payloads, exception text, or unrestricted PII.
- Do not enable CA1848/CA1873 globally or change `AnalysisMode`; analyzer-policy elevation belongs to Story 11.19d. This story's scoped tests are the enforcement mechanism.

### Testing Requirements

Use a Release-aligned restore/build, serial builds, the repository-pinned xUnit v3 direct runners, and `DiffEngine_Disabled=true`. Repository instructions reserve the solution for restore/build; run test projects individually, not `dotnet test` on the solution.

```bash
dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj \
  -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj \
  -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests \
  -class Hexalith.FrontComposer.Mcp.Tests.Invocation.CommandInvokerTests \
  -class Hexalith.FrontComposer.Mcp.Tests.Invocation.CommandInvokerSchemaGateTests \
  -class Hexalith.FrontComposer.Mcp.Tests.Logging.FailClosedLoggingGovernanceTests -parallel none

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -class Hexalith.FrontComposer.Shell.Tests.Architecture.SecurityLoggingGovernanceTests \
  -class Hexalith.FrontComposer.Shell.Tests.Services.Auth.AuthRedactionStressTests \
  -class Hexalith.FrontComposer.Shell.Tests.Services.Auth.FrontComposerAccessTokenProviderTests \
  -class Hexalith.FrontComposer.Shell.Tests.Services.Authorization.CommandAuthorizationEvaluatorTests \
  -class Hexalith.FrontComposer.Shell.Tests.Services.Authorization.CommandDispatchAuthorizationGateTests \
  -class Hexalith.FrontComposer.Shell.Tests.Services.StorageScopeResolverTests -parallel none

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests -parallel none
DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -notrait Category=Contract -notrait Category=Performance -notrait Category=e2e-palette \
  -notrait Category=NightlyProperty -notrait Category=Quarantined -parallel none

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests \
  -trait Category=Governance -parallel none
DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -trait Category=Governance -parallel none

python3 eng/validate-story-artifacts.py \
  --story _bmad-output/implementation-artifacts/11-18-fail-closed-security-log-sites.md

BASELINE=<implementation-start-commit>
git diff --name-status "$BASELINE"
git ls-files --others --exclude-standard
git diff --submodule=short "$BASELINE" -- references
rg --files -g '*.received.*' -g '!references/**' -g '!**/bin/**' -g '!**/obj/**'
git diff --check
```

If a broad lane is environmentally blocked, record the exact command and blocker separately from focused evidence; do not weaken the guard or relabel a partial run as complete. Test totals are evidence, not fixed acceptance values.

### Previous-Story Intelligence

- Story 11.3 already implemented and tested MCP tools-list, lifecycle-precheck, projection-reader, tenant-gate, and policy-gate sanitized events (8310-8314). Its strongest pattern is logger-enabled wrapper -> bounded conversion/hash -> private generated method, with captured `Exception == null`. Extend it; do not redo it.
- Story 11.16 review caught a lost null/empty guard during a mechanical extraction. Preserve branch behavior with direct tests; a green build or source census alone is not sufficient.
- Stories 11.17a-d established the required governance quality: non-empty Roslyn scan, recursive syntax handling, explicit inventory, synthetic negative, independent guard, and exact tracked/untracked/gitlink reconciliation. Reuse those patterns.
- Story 11.17d also makes filename/type parity a live constraint. Add the eponymous `FrontComposerSecurityLog` helper instead of a second partial file for `FrontComposerLog`, and reconcile all paths after its split lands.
- Current Shell tests already contain auth redaction, token-provider, user-context, authorization, storage-scope, CTA, and component behavior seams. Strengthen their observable log contract rather than replacing them with construction-only tests.

### Git Intelligence

- Creation baseline is `0a84e818b0ce220f291510ad094340f7296bb488` (`feat(tests): add BenchmarkHarnessGovernanceTests and update BenchmarkHarnessTests`), with `main` and `origin/main` aligned at creation.
- Commit `238aaa37` introduced the existing sanitized MCP generated-log family; commit `e17e3d84` evolved the Shell `FrontComposerLog` family. Preserve their event contracts and allocate new IDs deliberately.
- The working tree is intentionally dirty with Story 11.17d's Shell split/review record, sprint history, many new same-named Shell files, and unrelated root gitlink changes. Those changes belong to existing work. This story must neither revert nor absorb them and must record its actual implementation baseline after they settle.
- Do not commit, merge, tag, push, publish, reset, or alter submodules as part of story creation or validation. Follow the repository Git instructions if later delivery is explicitly requested.

### Latest Technical Information

- Microsoft's current source-generated logging guidance confirms that `[LoggerMessage]` removes runtime template parsing and boxing/temporary allocations and requires compatible partial method declarations. Keep static partial helpers to match the repository convention: <https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation>.
- The high-performance logging guidance emphasizes strongly typed structured templates and enabled checks: <https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/high-performance-logging>.
- Library guidance recommends `ILogger`-based structured logging and stable message templates: <https://learn.microsoft.com/en-gb/dotnet/core/extensions/logging/library-guidance>.
- CA1873 documents that expensive argument expressions may be evaluated even when a level is disabled; wrapper-level `IsEnabled` checks remain necessary for hashing/formatting: <https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1873>.
- CA1848 is the source-generated/high-performance logging recommendation, but repository-wide analyzer elevation is outside this child and remains Story 11.19d: <https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1848>.
- The repo pins SDK 10.0.302 and C# `latest`; compilation under that SDK is authoritative if older diagnostic wording conflicts with the current dedicated guide. Avoid generic/ref-struct logger complexity in this story.

### Project Structure Notes

- Expected production UPDATE: the 13 files in the current-state map plus `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs`.
- Expected production ADD: `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerSecurityLog.cs` only, unless the reconciled census discovers another semantically in-scope caller.
- Expected test UPDATE: focused MCP command/schema tests and existing Shell auth/authorization/scope/CTA/component tests needed to prove behavior and redaction.
- Expected test ADD: `tests/Hexalith.FrontComposer.Mcp.Tests/Logging/FailClosedLoggingGovernanceTests.cs` and `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs` (exact namespaces may follow the final folder convention, but filenames and type names remain eponymous).
- No `.csproj`, solution, package, public API baseline, compatibility suppression, generated source, resource, docs-site, UI, or submodule edit is expected.
- Creation-time artifact changes are limited to this story file and the surgical sprint-status decomposition/transition. Replace the initial File List with the exact story-owned implementation ledger before review.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 11 implementation order and Story 11.18 decomposition/acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/prd.md` — FR-29; canonical NFR-6, NFR-9, NFR-10, NFR-11]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-readiness-major-issues.md` — approved Proposal 2C security-first decomposition]
- [Source: `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` — finding M15 historical 206/50 census]
- [Source: `_bmad-output/planning-artifacts/architecture.md` and `_bmad-output/project-docs/architecture.md` — MCP/Shell boundaries and logging conventions]
- [Source: `_bmad-output/project-context.md` — privacy, MCP fail-closed, LoggerMessage, testing, and Git rules]
- [Source: `_bmad-output/implementation-artifacts/spec-11-3-mcp-cross-request-lifecycle-and-operability.md` — existing sanitized MCP events and tests]
- [Source: `_bmad-output/implementation-artifacts/11-16-fatal-hydration-json-and-generated-literal-helper-consolidation.md` — mechanical-preservation review lessons]
- [Source: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md` — current Shell topology and one-type governance]
- [Source: `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs` and `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerLog.cs` — existing helper/event families]
- [Source: the 13 production files in Current-State Security Logging Map — live direct-site behavior]
- [Source: Microsoft source-generated/high-performance/library logging and CA1848/CA1873 guidance linked above]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-15 implementation baseline: Story 11.17d topology is committed at implementation-start HEAD `77e58b31047ead105090dc95a56f118049ed3e80`; `main` and `origin/main` were aligned. The only pre-edit working-tree item was unrelated untracked `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-15.md`; it is preserved and excluded from story ownership. Root gitlinks were clean at their HEAD revisions.
- 2026-07-15 Roslyn implementation-start census: MCP has 4 direct `ILogger.Log*` calls / 2 files; Shell has 240 / 60; combined 244 / 62. The deterministic security inclusion rule selects exactly the frozen 36 calls / 13 files (MCP 4 / 2 plus Shell 32 / 11), with no creation-map path/count drift. The census parses invocation syntax, including nullable conditional-access member bindings.
- 2026-07-15 preservation audit: existing generated MCP events 8310-8314 remain in `FrontComposerMcpLog`; Shell tenant event 5650 remains in `FrontComposerLog`. None is counted as a residual direct call or duplicated by this child.
- 2026-07-15 runtime baseline: `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json` was attempted before production edits and stopped before resource startup because the Parties submodule has six pre-existing HFC0001 errors for deprecated flattened `QueryRequest` construction. `aspire stop` confirmed no AppHost remained running.
- 2026-07-15 red-green evidence: helper tests first failed to compile before the generated APIs existed; MCP caller tests then failed on the four legacy `EventId == 0` direct sites, and Shell caller tests failed on eight representative legacy contracts. The same focused tests passed after helper and caller migration.
- 2026-07-15 post-migration Roslyn census: the frozen security set is 0 direct calls / 0 files. MCP has no residual direct call. Shell has 208 residual calls / 49 exact files: 117 Warning/Error calls are pinned to Story 11.18b and 91 Debug/Information calls are pinned to Story 11.18c.
- 2026-07-15 broad regression resolution: the first Shell broad lane exposed 29 failures. Twenty-eight legacy scope tests used disabled substitute loggers and were updated to enable generated events; the remaining AuthBoundary failure was resolved by keeping provider-enum allowlisting in `FrontComposerAccessTokenProvider` and passing only an already-bounded provider category into the telemetry helper. The rerun passed all 2,319 selected tests.
- 2026-07-15 validation evidence: Release restore passed; the final serial Release solution build passed with 0 warnings / 0 errors; focused MCP passed 15/15; focused Shell passed 61/61; full MCP passed 364/364; broad Shell passed 2,319/2,319; MCP Governance passed 6/6; Shell Governance passed 155/155. Root gitlinks have no implementation-start drift, and no `*.received.*` artifact exists outside excluded roots.
- 2026-07-15 workspace reconciliation: additional planning, release-story, release-ledger, and deployment-guide edits appeared concurrently after implementation started. They were preserved, not modified or absorbed by this story, and are enumerated under **Documented Unrelated Workspace State** together with the already-committed creation-baseline-to-implementation-start delta.
- 2026-07-15 creation: loaded the complete Epic 11, PRD/addendum, architecture/quality review, approved correction proposals, UX artifacts, all discovered project-context files, repository instructions, sibling/previous stories, current source/tests, Git history/worktree state, and current official .NET logging guidance.
- 2026-07-15 creation census: full Shell text baseline 240 direct sites / 60 files, Warning+ 144 / 48; MCP residual four / two. Security-first minimum is 36 direct sites / 13 files (four MCP, 32 Shell), with existing MCP 8310-8314 and Shell 5650 retained as generated evidence.
- 2026-07-15 creation decision: canonical later-applied decomposition wins over the older child-name suggestion; create 11.18a only, keep 11.18b/c backlog, and leave the parent non-implementable.
- 2026-07-15 creation artifact check: explicit story-owned validation for this story file plus `sprint-status.yaml` passed. Full working-tree discovery is intentionally deferred to the implementation-start baseline because the current tree contains the unrelated, uncommitted Story 11.17d split and root-gitlink drift.

### Completion Notes List

- Migrated the frozen 36-call / 13-file security set to generated logging: MCP events 8315-8318 and Shell events 5660-5691. Existing MCP 8310-8314 and Shell 5650 remain unchanged and globally collision-free.
- Added support-safe wrappers that emit only bounded categories, counts, booleans, exception type names, and audited `sha256:` pseudonyms; all generated security signatures reject `Exception` parameters, and captured exceptions remain null.
- Preserved fail-closed behavior across MCP results, cancellation, authorization, dispatch suppression, token acquisition, claim caching, policy strict mode, storage scope, derived values, CTA selection, and component rendering.
- Added exact MCP and Shell Roslyn governance with non-empty inventories and synthetic negatives. The post-migration direct security census is zero; the 208 remaining Shell calls / 49 files are pinned exactly to 11.18b (117 Warning/Error) or 11.18c (91 Debug/Information).
- Release validation is green: solution build 0 warnings / 0 errors; focused MCP 15, focused Shell 61, full MCP 364, broad Shell 2,319, MCP Governance 6, and Shell Governance 155 tests passed. The only runtime-baseline limitation is the recorded pre-existing Parties HFC0001 compilation blocker before Aspire resource startup.

### File List

- `_bmad-output/implementation-artifacts/11-18-fail-closed-security-log-sites.md` (modified — implementation record, validation evidence, ownership reconciliation, and review transition)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — surgical Story 11.18a status transitions while preserving concurrent sprint history)
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs` (modified — generated MCP security events 8315-8318 and support-safe wrappers)
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs` (modified — migrate schema, known-failure, and downstream-failure direct logs)
- `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiationRuntimeGate.cs` (modified — migrate bounded schema decision log)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerSecurityLog.cs` (added — generated Shell security event family 5660-5691 and audited sanitizers)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerLog.Security.cs` (not created — named exception required by the checked one-type helper task and enforced as forbidden topology)
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.razor.cs` (modified — generated fail-closed evaluator diagnostic)
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs` (modified — generated authentication bridge diagnostic)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/ClaimsPrincipalUserContextAccessor.cs` (modified — bounded claim-resolution diagnostic)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs` (modified — bounded token-provider diagnostics and provider allowlist boundary)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/ServerCircuitUserContextAccessor.cs` (modified — bounded circuit claim-resolution diagnostic)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs` (modified — generated authorization decision diagnostics)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandDispatchAuthorizationGate.cs` (modified — generated dispatch denial/defer/cancel diagnostics)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs` (modified — count/digest-only policy catalog diagnostics)
- `src/Hexalith.FrontComposer.Shell/Services/DerivedValues/LastUsedValueProvider.cs` (modified — generated D31 fail-closed diagnostic)
- `src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs` (modified — sanitized CTA resolution diagnostics)
- `src/Hexalith.FrontComposer.Shell/Services/StorageScopeResolver.cs` (modified — generated HFC2105 scope diagnostics)
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs` (modified — exact generated-event, redaction, and behavior assertions)
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerSchemaGateTests.cs` (modified — schema-gate generated-event and redaction assertions)
- `tests/Hexalith.FrontComposer.Mcp.Tests/Logging/CapturedLogEntry.cs` (added — structured MCP log capture model)
- `tests/Hexalith.FrontComposer.Mcp.Tests/Logging/CapturingLogger.cs` (added — structured MCP logger seam)
- `tests/Hexalith.FrontComposer.Mcp.Tests/Logging/FrontComposerMcpLogTests.cs` (added — MCP helper event-contract and sanitization tests)
- `tests/Hexalith.FrontComposer.Mcp.Tests/Logging/FailClosedLoggingGovernanceTests.cs` (added — non-vacuous MCP Roslyn logging governance)
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcAuthorizedCommandRegionTests.cs` (modified — fail-closed rendering log contract assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerAuthenticationServiceExtensionsTests.cs` (modified — authentication bridge log contract assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/AuthRedactionStressTests.cs` (modified — adversarial security-log redaction assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/CapturingLogger.cs` (modified — retain structured state, EventId, level, and exception)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/ClaimsPrincipalUserContextAccessorTests.cs` (modified — request claim log and behavior assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAccessTokenProviderTests.cs` (modified — token-provider event, redaction, and behavior assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/ServerCircuitUserContextAccessorTests.cs` (modified — circuit claim log contract assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandAuthorizationEvaluatorTests.cs` (modified — authorization event/redaction and result preservation tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandDispatchAuthorizationGateTests.cs` (modified — dispatch event/redaction and no-inner-dispatch tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidatorTests.cs` (modified — catalog count/digest and strict-mode tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/DerivedValueProviderChainTests.cs` (modified — LastUsed fail-closed log contract assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs` (modified — CTA sanitization and selection preservation tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/StorageScopeResolverTests.cs` (modified — HFC2105 generated-event and scope behavior tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CapabilityDiscovery/CapabilityDiscoveryEffectsScopeTests.cs` (modified — enable generated logging in legacy scope regression tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsScopeTests.cs` (modified — enable generated logging in legacy scope regression tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsScopeTests.cs` (modified — enable generated logging in legacy scope regression tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsScopeTests.cs` (modified — enable generated logging in legacy scope regression tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsScopeTests.cs` (modified — enable generated logging in legacy scope regression tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/CapturedLogEntry.cs` (added — structured Shell security-log capture model)
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/CapturingLogger.cs` (added — structured Shell security logger seam)
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerSecurityLogTests.cs` (added — Shell event-contract, sanitizer, and adversarial redaction tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs` (added — exact Shell inventory and Roslyn logging governance)

### Documented Unrelated Workspace State

- `.agents/skills/pushall/SKILL.md` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `.claude/skills/pushall/SKILL.md` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `.github/skills/pushall/SKILL.md` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `_bmad-output/implementation-artifacts/11-17-cli-package-split.md` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `_bmad-output/implementation-artifacts/deferred-work.md` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md` (unrelated — concurrent workspace change outside Story 11.18a, preserved without modification)
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md` (unrelated — concurrent workspace change outside Story 11.18a, preserved without modification)
- `_bmad-output/planning-artifacts/architecture.md` (unrelated — concurrent workspace change outside Story 11.18a, preserved without modification)
- `_bmad-output/planning-artifacts/epics.md` (unrelated — concurrent workspace change outside Story 11.18a, preserved without modification)
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` (unrelated — concurrent workspace change outside Story 11.18a, preserved without modification)
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-15.md` (unrelated — concurrent workspace change outside Story 11.18a, preserved without modification)
- `_bmad-output/planning-artifacts/prd.md` (unrelated — concurrent workspace change outside Story 11.18a, preserved without modification)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md` (unrelated — concurrent workspace change outside Story 11.18a, preserved without modification)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md` (unrelated — concurrent workspace change outside Story 11.18a, preserved without modification)
- `_bmad-output/project-docs/deployment-guide.md` (unrelated — concurrent workspace change outside Story 11.18a, preserved without modification)
- `_bmad-output/project-docs/source-tree-analysis.md` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `references/Hexalith.AI.Tools` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `references/Hexalith.Builds` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `references/Hexalith.EventStore` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `references/Hexalith.Memories` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `references/Hexalith.Tenants` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Components/Badges/FcDesaturatedBadge.razor.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Components/Badges/OptimisticBadgeState.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/ColumnDescriptor.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/ColumnVisibilityContext.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Components/Home/HomeCardModel.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Components/Home/HomeProjectionRow.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/LifecycleTimerPhase.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/LifecycleUiState.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Extensions/DomainBootstrapMarker.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreBootstrapMarker.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapMarkers.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapStage.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Extensions/IFrontComposerBootstrapMarker.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Extensions/QuickstartBootstrapMarker.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClassification.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClassification.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnectionFactory.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionHubConnectionState.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionHubConnectionStateChanged.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/QueryClassificationOutcome.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/PendingWrite.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthCookieOptions.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthRedirectOptions.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthenticationOptions.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthenticationProviderKind.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerCustomBrokeredOptions.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerGitHubOAuthOptions.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerOpenIdConnectOptions.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerSaml2Options.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerTokenRelayOptions.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/CircuitServicesAccessor.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerCircuitServicesHandler.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerClaimExtractionResult.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerClaimExtractor.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerGatewayAuthorizationHandler.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerUserTokenStore.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecisionKind.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationReason.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationRequest.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationResource.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationSurface.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/EmptyStateCta.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/ExpandInRowJSModule.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Feedback/CommandFeedbackWarning.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/Feedback/ICommandFeedbackPublisher.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/IEmptyStateCtaResolver.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/Services/IExpandInRowJSModule.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryHydrationState.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/FrontComposerCapabilityDiscoveryState.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteLoadState.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteResult.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteResultCategory.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/IProjectionPageLoader.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageReducers.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/NullProjectionPageLoader.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/ProjectionPageResult.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/VirtualizationViewStateReducers.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/CommandExecutionAdmission.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/CommandExecutionAdmissionDenialReason.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/CommandExecutionAdmissionRequest.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/ICommandExecutionAdmissionGate.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/ICommandExecutionAdmissionReleaser.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/INewItemIndicatorStateService.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/IPendingCommandOutcomeResolver.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/IPendingCommandPollingCoordinator.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/IPendingCommandStatusQuery.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorEntry.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NullPendingCommandStatusQuery.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandEntry.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeObservation.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolutionResult.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolutionStatus.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeSource.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRegistration.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRegistrationResult.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRegistrationStatus.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandResolutionResult.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandResolutionStatus.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStatus.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandTerminalObservation.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandTerminalOutcome.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/IProjectionConnectionState.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/IProjectionFallbackRefreshScheduler.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionSnapshot.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStateService.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStatus.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionTransition.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackGroupKey.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackLane.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackLaneRefreshOutcome.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshContracts.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionReconciliationRefreshResult.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ClearExpiredReconciliationSweepsAction.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/IReconnectionReconciliationCoordinator.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/IReconnectionReconciliationState.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/MarkReconciliationSweepAction.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepFeature.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepMarker.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepReducers.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepState.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationSnapshot.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationStateService.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationStatus.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/AuthBoundaryTests.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellTypeOrganizationGovernanceTests.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/HydrationStateConsolidationTests.cs` (unrelated — committed before implementation-start HEAD and outside Story 11.18a)

### Change Log

- 2026-07-15: Implemented Story 11.18a generated security logging, sanitization, behavioral coverage, Roslyn governance, exact residual ownership ledger, and Release validation evidence.

### Review Findings

Adversarial code review 2026-07-21 (`/bmad-code-review`, 4 layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor) over the story-scoped diff `77e58b31..3356ae7e -- src/ tests/`. Consensus: the production migration is faithful and complete — all 36 direct calls in the 13 mapped files migrated, event-ID ranges (MCP 8315-8318, Shell 5660-5691) and diagnostic IDs (HFC2012/2013/2014/2105, D31) preserved, no forbidden changes, and every shipped security event is runtime redaction-tested (no raw sentinel, `Exception == null`). **No live leak and no live fail-closed regression.** All surviving findings are in the AC5 governance-enforcement layer (regression-resistance for *future* edits) plus minor test/observability gaps. Line references are relative to commit `3356ae7e`; the guard files were subsequently extended by Stories 11.18b/c (`FrontComposerWarningLog`/`FrontComposerHotPathLog`), so the same holes now also apply to those families — a fix should target the current working-tree guards.

- [x] [Review][Patch] Governance guards do not enforce identifier-argument redaction or force new security helpers to be redaction-tested — AC5 requires rejecting "generated-method calls that pass … unredacted identifier expressions", but the guards only scan for direct `Log*` calls, `EventName`, `Exception` params, and unique/pinned EventIds; a future helper that emits a raw identifier would pass (range pin `Range(5660,32)` forces a range bump but no sentinel test) [tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs:105 + tests/Hexalith.FrontComposer.Mcp.Tests/Logging/FailClosedLoggingGovernanceTests.cs] (verification-gap+acceptance-auditor) — fixed: added `FindUnwrappedIdentifierArguments` static-syntax check (both files) that flags a bare-identifier `string` argument at a generated-method or delegate-indirection call site unless allowlisted (`exceptionType`, `diagnosticId` — both explicitly permitted raw categories per AC3); wired into the main inventory assertion plus a new synthetic-negative test in each file. Verified zero false positives against the current codebase (including the pre-existing 11.18b/c `FrontComposerHotPathLog.cs`/`FrontComposerWarningLog.cs` families) after adding `diagnosticId` to the allowlist.
- [x] [Review][Patch] Exception-parameter guard uses an exact type-string match (`p.Type?.ToString() is "Exception" or "System.Exception"`) — derived/qualified/aliased exception params (`InvalidOperationException ex`, `global::System.Exception ex`, `using`-alias) are still captured by `[LoggerMessage]` yet report `HasExceptionParameter == false`; detect by semantic derivation from `System.Exception` [tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs:236 + tests/Hexalith.FrontComposer.Mcp.Tests/Logging/FailClosedLoggingGovernanceTests.cs] (blind-hunter+verification-gap) — fixed: replaced the exact-string match with `IsExceptionParameterType`, which strips a `global::` prefix and trailing `?` then matches any type name ending in `Exception` (both files).
- [x] [Review][Patch] `DirectLogMethodNames` omits the base `ILogger.Log(...)` primitive — a `logger.Log(level, eventId, state, ex, formatter)` call in a security source (the exact way to capture an Exception + raw state) is not flagged as a direct site; add `"Log"` and reconcile the pinned per-file inventory [tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs:12] (blind-hunter) — fixed: added `"Log"` to `DirectLogMethodNames` in both files; confirmed via repo-wide grep that no real source (outside `obj/`) calls the base `.Log(...)` method, so no pinned-inventory counts shifted.
- [x] [Review][Patch] AC5's fourth enumerated synthetic negative ("empty/over-broad allowlist") is not exercised — the non-empty-census + explicit-ledger mechanism exists but no synthetic feeds an empty/over-broad input to prove that guard fires (the other three negatives are covered) [tests/Hexalith.FrontComposer.Mcp.Tests/Logging/FailClosedLoggingGovernanceTests.cs + tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs] (acceptance-auditor) — fixed: added `GovernanceGuard_EmptySourceCensusAndOverBroadSecurityAllowlist_AreReported` to both files, proving via `Should.Throw<ShouldAssertException>` that an empty source census and a security-allowlisted path that still emits a raw direct call are both detected.
- [x] [Review][Patch] Per-field digest attribution is verified for only 2 of 32 Shell security events — `AllSecurityEvents_…` passes the same sentinel to every string param (catches a raw leak but not a swapped/mislabeled digest field); add distinct per-parameter sentinels with per-`State["…Digest"]` assertions for the remaining 30 [tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerSecurityLogTests.cs:14] (verification-gap) — fixed: added `MultiFieldSecurityEvents_DigestEachIdentifierIntoItsOwnField`, covering the 16 remaining events with 2+ digested identifier fields with distinct per-field sentinel values and a `ComputeExpectedDigest` helper mirroring the production SHA-256 algorithm; catches a swapped/mislabeled field that the shared-sentinel test cannot.

All 5 patches are test-only changes (no production code touched). Verified: MCP test project builds clean (0 warnings/0 errors) and its Governance trait passes 8/8; Shell test project builds clean and its Governance trait passes 195/197 — the 2 residual failures (`AnalyzerPolicyGovernanceTests.AnalyzerPolicy_GovernanceContract_FailsClosed`, `CiGovernanceTests.ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate`) were confirmed via `git stash` to pre-exist on baseline (unrelated repo-state drift — GOV-1/11.19 CA1707 ledger and REL-6 SHA-pin lockstep, both already tracked separately), not introduced by this patch. The 11 tests directly exercising the patched files (7 in `SecurityLoggingGovernanceTests`, 4 in `FrontComposerSecurityLogTests`) and the 4 in `FailClosedLoggingGovernanceTests` all pass.
- [x] [Review][Defer] Whole-project direct-call count pins (`208/117/91` + 45-file map) are brittle — any unrelated logging edit breaks the test and trains "just bump the number"; the real guard is the separate `SecuritySourcePaths → ShouldBeEmpty` assertion [tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs:51] — deferred: intentional per AC1 "denominator never silently reduced", flagged as a maintainability trade-off (blind-hunter)
- [x] [Review][Defer] MCP `BoundedSchemaToken` returns its input verbatim when ≤64 chars and charset-matches, whereas the Shell sanitizer hashes everything — no live leak (its three inputs are hardcoded static literals) but a weaker redaction guarantee if those fields ever become dynamic [src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs:176] — deferred: latent, revisit if the schema-decision fields ever carry request/agent-sourced values (blind-hunter)
- [x] [Review][Defer] `Reason` is baked into the message template for `AuthorizationMissingPolicyFailed`/`AuthorizationHandlerFailed` instead of a structured `{Reason}` hole (as siblings `AuthorizationBlocked`/`DispatchBlocked` use), so structured-log queries filtering on a `Reason` field won't match those two events [src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerSecurityLog.cs] — deferred: minor observability asymmetry, events remain distinguishable by EventId/EventName (verification-gap)
