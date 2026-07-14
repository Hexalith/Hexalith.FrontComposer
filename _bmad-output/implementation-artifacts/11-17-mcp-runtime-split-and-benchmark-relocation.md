---
baseline_commit: 5718f85b93ede05393219eae49dcc801b34323bd
---

# Story 11.17c: MCP/runtime split and benchmark-harness relocation

Status: review

<!-- Note: This executable child specializes the non-implementable Story 11.17 parent. The sprint/file key intentionally omits the letter suffix. -->

## Story

As a FrontComposer maintainer,
I want the MCP skill-corpus implementation split into one type per file and the LLM benchmark harness moved to the non-packable benchmark executable,
so that the shipped runtime contains only MCP/runtime responsibilities while behavior, security, and benchmark evidence remain stable.

## Acceptance Criteria

1. **Runtime declaration split.** Given `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs` currently contains 56 direct top-level declarations rather than the architecture review's historical approximation of 45, when the split is complete, then the aggregate is deleted and exactly 27 runtime declarations remain under `src/Hexalith.FrontComposer.Mcp/Skills/`, one direct top-level declaration in a same-named file: `SkillCorpusDiagnosticCategory`, `SkillCorpusDiagnostic`, `SkillCorpusSource`, `SkillCorpusResource`, `SkillCorpusSnapshot`, `SkillCorpusValidationResult`, `SkillCorpusParser`, `SkillCorpusLoader`, `SkillCorpusReferenceValidator`, `SkillCorpusSnippetValidator`, `SkillResourceDescriptor`, `SkillResourceReadResult`, `SkillCorpusAggregateManifest`, `SkillCorpusManifestEntry`, `SkillCorpusAggregateManifestBuilder`, `SkillResourceReadOptions`, `FrontComposerSkillResourceProvider`, `InvalidSkillCorpusException`, `FrontComposerSkillMcpResource`, `GeneratedCodeFailureCategory`, `GeneratedCodeFile`, `GeneratedCodeDiagnostic`, `GeneratedCodeValidationResult`, `GeneratedBoundedContextValidator`, `ISkillCorpusBaselineProvider`, `EmptySkillCorpusBaselineProvider`, and `SkillCorpusReleaseGuard`. No empty `SkillCorpus.cs` facade remains.
2. **Mechanical runtime preservation.** All 27 retained declarations preserve namespace `Hexalith.FrontComposer.Mcp.Skills`, assembly identity, public accessibility, type kind/modifiers, attributes, record parameters/defaults, constants, member order and bodies, regexes, XML/comments, nullable/directive behavior, exceptions, deterministic ordering, hashes/fingerprints, resource URIs, size limits, opaque failure tokens, aggregate-manifest rendering, reference/snippet validation, baseline guarding, and fail-closed security behavior. The generated-code five-type surface remains MCP-owned because the published skill corpus references `GeneratedBoundedContextValidator`; its package-boundary, unsafe-MSBuild, tenant-spoofing, generated-file, registration, test-scaffold, and SourceTools-manifest diagnostics do not change.
3. **Benchmark ownership relocation.** The 29 benchmark declarations move to `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/`, one same-named file each: the 26 public declarations `SkillBenchmarkPrompt`, `SkillBenchmarkPromptSet`, `SkillBenchmarkModelConfig`, `SkillBenchmarkCacheKey`, `SkillBenchmarkCachePolicy`, `SkillBenchmarkRedactionStatus`, `SkillBenchmarkResult`, `SkillBenchmarkEvidenceStatus`, `SkillBenchmarkBudgetStatus`, `SkillBenchmarkBaselineWriteDecision`, `SkillBenchmarkGateStatus`, `SkillBenchmarkProviderCapabilities`, `SkillBenchmarkProviderRequest`, `SkillBenchmarkBudgetState`, `SkillBenchmarkGateResult`, `SkillBenchmarkBaselineArtifact`, `SkillBenchmarkDeterminismPolicy`, `SkillBenchmarkBudgetPolicy`, `SkillBenchmarkBaselinePolicy`, `SkillBenchmarkGate`, `SkillBenchmarkEvidencePath`, `SkillBenchmarkSummarySanitizer`, `SkillBenchmarkArtifactBuildResult`, `SkillBenchmarkArtifactWriter`, `SkillBenchmarkScore`, and `SkillBenchmarkOfflineScorer`, plus internal `SkillBenchmarkPromptSetDto`, `SkillBenchmarkPromptDto`, and `SkillBenchmarkJsonContext`. Preserve their existing `Hexalith.FrontComposer.Mcp.Skills` namespace, accessibility, signatures, bodies, hashes, JSON shape, sanitization/redaction, budget/gate semantics, prompt ordering, one-shot threshold, and evidence-path rules; only assembly/package ownership intentionally changes. Move `BenchmarkHarnessTests` into the Bench `Skills` folder/namespace and class-tag it `[Trait("Category", "Performance")]`.
4. **Project/resource direction.** The non-packable Bench project references MCP so `SkillBenchmarkOfflineScorer` can reuse the retained generated-code validator. MCP adds one exact `InternalsVisibleTo` friend-assembly declaration solely because the two moved hash call sites reuse `SkillCorpusParser.Sha256Hex`. This is assembly-wide CLR access, so a source guard must prove the Bench harness consumes no other MCP internal; do not make `Sha256Hex` public or duplicate hashing. Move the existing prompt JSON embedded-resource item from MCP to Bench while retaining logical name `Hexalith.FrontComposer.Mcp.Skills.benchmark-prompts.v1.prompt-set.json`; the physical `docs/skills/frontcomposer/benchmark-prompts/v1/prompt-set.json` and `eng/llm_benchmark.py` path/contract remain unchanged. The MCP assembly/package exports no `SkillBenchmark*` type and embeds no benchmark prompt, while the Bench assembly contains the exact 29 declarations and prompt resource.
5. **Intentional compatibility break.** Removing the 26 public `SkillBenchmark*` types from the shipped MCP assembly is treated as a real binary API break against the configured `3.0.0` package-validation baseline. Generate and review exactly scoped MCP `CP0001` type-removal suppressions; add a one-to-one compatibility-ledger row for every suppression; remove exactly those 26 stale UIDs from `docs/validation/api-summary-baseline.txt`; and publish an adopter-facing `3.1.x` to `4.0.0` migration note stating that no runtime NuGet replacement exists and repository maintainers run the harness through `Shell.Tests.Bench`/`eng/llm_benchmark.py`. Runtime facades, type forwarders, wildcard suppressions, and minor/patch release classification are forbidden because they would either keep benchmark code in the runtime or conceal the breaking change. Before status moves to review, the Release Owner must approve the `4.0` posture in `_bmad-output/contracts/fc-4-0-mcp-benchmark-removal-release-version-decision-2026-07-14.md`; the compatibility ledger uses `currentRelease`/`targetRelease` `v4.0`, `expiresAfter` `v4.1`, and `intentional-major-break`. Before an implementation commit exists, validate a prospective breaking subject/footer with commitlint; after that commit is authorized and present, semantic-release dry-run evidence must classify the range as major without editing `CHANGELOG.md` manually. The next release-lifecycle update after `4.0` publishes must advance the package baseline to `4.0.0` and remove all 26 XML/JSON suppressions before any `v4.1` pack.
6. **Durable governance.** A Roslyn-based `[Trait("Category", "Governance")]` guard scans only the approved MCP `Skills` and Bench `Skills` slices, excludes only `bin`, `obj`, and generated output, fails if no sources are found, recursively counts compilation-unit/file-scoped/block/nested-namespace direct type or delegate declarations including conditional branches, requires declaration/file-name parity, and has no allowlist. A synthetic multi-kind, conditional/nested-namespace negative proves non-vacuity. Reflection/source pins prove the exact 27 runtime identities, the exact 29 benchmark identities and accessibilities, zero `SkillBenchmark*` types/prompt resources in MCP, and the prompt resource in Bench. Unrelated MCP multi-type files remain outside this child.
7. **Validation and evidence.** MCP Skills/generated-code in-process tests, the relocated 18-test benchmark contract class, `Hexalith.FrontComposer.Testing.Tests.PackageBoundaryTests`, Bench Release build, nightly-workflow governance, default and Governance solution lanes, `Category=Performance`, prompt validation, DocFX/docs validation, and MCP package validation against `3.0.0` pass. `McpRuntimePackageBoundaryTests` opens the produced MCP `.nupkg`, proves its `lib/net10.0` DLL is the inspected runtime assembly, pins the exact 27 exported Skills types, and rejects every `SkillBenchmark*` type and benchmark prompt resource. Final evidence also includes a normalized baseline-to-final declaration-body comparison for all 56 declarations, before/after counts, exact tracked/untracked/submodule ledger, CRLF/UTF-8/final-newline audit, generated/received-artifact audit, and `git diff --check`. No package version/reference beyond the Bench-to-MCP project reference, SDK/TFM, benchmark threshold, schema/wire contract, UI, or unrelated source changes occur.

## Tasks / Subtasks

- [x] Task 1: Capture the implementation and compatibility baselines (AC: 1, 2, 3, 5, 7)
  - [x] Record the implementation-start commit and complete dirty-worktree/submodule ledger before editing; preserve concurrent Story 11.17a/11.17b or other user work without absorbing, restoring, or relabeling it.
  - [x] Parse `SkillCorpus.cs` with Roslyn and pin all 56 direct declarations: 27 runtime plus 29 benchmark, with 53 public and three internal declarations. Capture each declaration body from the baseline for normalized comparison after the move.
  - [x] Capture the MCP assembly's exported `Hexalith.FrontComposer.Mcp.Skills` identities, manifest-resource names, prompt/corpus hashes, benchmark JSON artifacts, and the configured package-validation baseline before editing.
  - [x] Draft the exact v4 decision contract path named in AC5 and obtain Release Owner approval before moving the story to review. Source work may start before approval, but do not self-approve, relabel the break as `3.x`, advance the release ledger, or run the release pack/dry-run gates until approval exists.

- [x] Task 2: Split the 27 runtime declarations mechanically (AC: 1, 2, 6)
  - [x] Extract every AC1 declaration into its exact same-named `.cs` file under `src/Hexalith.FrontComposer.Mcp/Skills/`; keep nested declarations with their owning top-level type.
  - [x] Preserve declaration text and use only the imports each new file requires. Preserve file-scoped namespace, comments/XML docs, generated-regex partial declarations, CRLF, UTF-8, final newline, and behavior; do not bulk-format or opportunistically clean up.
  - [x] Delete `SkillCorpus.cs` only after the 27 runtime and 29 benchmark declarations are accounted for. SDK default compile globs include the new runtime files; add no explicit `Compile` items.

- [x] Task 3: Relocate the benchmark harness and evidence owner (AC: 3, 4, 6)
  - [x] Move the 29 AC3 declarations into same-named files under `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/`. Preserve the historical public benchmark namespace/accessibility as the deliberate ownership-move exception; do not promote the three internal serialization types.
  - [x] Move `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/BenchmarkHarnessTests.cs` to the Bench `Skills` folder, use the Bench test namespace, retain all 18 facts and helpers, and add the class-level Performance trait so the default lane excludes provider/benchmark evidence. Pin the exact 18 method names from the implementation baseline so neither of the two prompt-set facts nor any policy/path/gate fact can be dropped silently.
  - [x] Add Bench -> MCP `ProjectReference`; add exact MCP `InternalsVisibleTo` for the Bench assembly beside the existing MCP.Tests friend. Reuse `SkillCorpusParser.Sha256Hex` and retained `GeneratedBoundedContextValidator`; create no duplicate hash or validator, and add a source/architecture pin that the Bench harness accesses no other MCP internal.
  - [x] Remove only the prompt JSON `EmbeddedResource` from the MCP project and add it to Bench with the exact existing logical name. Keep MCP skill-markdown embedding, physical prompt content/path, loader behavior, and the existing PaletteScorer BenchmarkDotNet files unchanged.

- [x] Task 4: Add organization, ownership, and package guards (AC: 4, 6, 7)
  - [x] Add an MCP-tests Governance class using the existing Roslyn package to scan both approved Skills slices, require non-empty exact one-type/same-filename organization, handle nested namespaces and conditional declarations, and prove a synthetic negative.
  - [x] Reflection-pin the exact 27 retained runtime types and assert the MCP assembly exposes no type whose name begins `SkillBenchmark`; assert MCP still exposes all five generated-code validator types.
  - [x] In the relocated benchmark tests, pin the exact 29 benchmark declaration identities/accessibilities and prompt resource; retain deterministic prompt IDs, hashes, cache invalidation, sanitization, budget, gate, path, scorer, and artifact tests.
  - [x] Rename/update `SkillResourceTests.PackagingFootprint_*` so it retains the embedded markdown count and explicitly rejects the benchmark prompt resource from MCP.
  - [x] Add `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs`. It packs MCP to an isolated temporary directory with package validation enabled, opens the `.nupkg`, verifies the packaged `lib/net10.0/Hexalith.FrontComposer.Mcp.dll` bytes match the inspected Release assembly, and then uses that assembly's reflection/resource surface to pin the exact 27 Skills exports, zero `SkillBenchmark*`, and no benchmark prompt. A green source build or zip-entry-only check is insufficient.

- [x] Task 5: Govern the intentional MCP public-surface removal (AC: 5, 7)
  - [x] Run ApiCompat/package validation against the configured `3.0.0` baseline with suppression generation enabled only to discover exact signatures; review the output, then check in `src/Hexalith.FrontComposer.Mcp/CompatibilitySuppressions.xml` containing only the 26 required baseline `CP0001` type removals. No wildcard, member-wide, unrelated, or generated-file acceptance is allowed.
  - [x] Advance `docs/diagnostics/compatibility-suppressions.json` only after approval to `currentRelease: v4.0`; add one exact `Hexalith.FrontComposer.Mcp|net10.0|CP0001|T:<full-type>` row per XML suppression with `targetRelease: v4.0`, `expiresAfter: v4.1`, owner `11-17-mcp-runtime-split-and-benchmark-relocation`, `intentional-major-break`, bounded rationale, and exact relocation/removal state.
  - [x] Update `DiagnosticRegistryTests` to accept and one-to-one reconcile exact MCP suppressions while retaining the fail-closed schema, allowed-reason, target/current/expiry, uniqueness, and no-wildcard rules. Update `tests/eng/test_pack_release_packages.py` so pre-`4.0`, expired, or mismatched plans fail, the approved `4.0` plan passes, and a stale-suppression `v4.1` plan fails until a fixture advances the baseline and removes the 26 rows.
  - [x] Update `docs/diagnostics/README.md`; remove exactly the 26 old `SkillBenchmark*` UIDs from `docs/validation/api-summary-baseline.txt`; add `docs/migrations/3.1-to-4.0.md` and its index link; record approval at the exact AC5 contract path. The migration page uses `fromVersion: "3.1.1"`, `toVersion: "4.0.0"`, `diagnosticId: "none"` (do not appropriate an unrelated HFC ID), `skillCorpusImpact: "unchanged"`, `codeFixAvailable: false`, and this story as `ownerStory`. Preserve semantic-release ownership of `CHANGELOG.md`; validate the prospective breaking commit message with commitlint, and run semantic-release dry-run only after that approved commit exists.

- [x] Task 6: Update maintained topology and automation (AC: 3, 4, 7)
  - [x] Update `.github/workflows/nightly.yml` to run `BenchmarkHarnessTests` from `Hexalith.FrontComposer.Shell.Tests.Bench`, while retaining budget-before-spend, candidate-only baseline, prompt loader/corpus summary, and read-only evidence behavior.
  - [x] Strengthen `CiGovernanceTests.NightlyBenchmarkWorkflow_*` to require the new Bench project path and Performance ownership and reject the old MCP.Tests benchmark command, preventing a stale zero-test nightly run.
  - [x] Update the LLM benchmark command in `tests/README.md` and the maintained MCP/Bench entries in `_bmad-output/project-docs/source-tree-analysis.md`. Do not rewrite historical stories, reviews, deferred-work paths, or planning provenance.
  - [x] Leave `Hexalith.FrontComposer.slnx`, `Directory.Packages.props`, `global.json`, `eng/llm_benchmark.py`, the physical prompt set, generated-code skill doc/tests, and unrelated package inventory unchanged unless a failing required gate demonstrates a current-tree reference that must be reconciled.

- [x] Task 7: Execute release-aligned validation and reconcile evidence (AC: 2-7)
  - [x] Restore/build the Release solution plus MCP.Tests and Shell.Tests.Bench serially. Run the focused commands below, the default/Governance/Performance solution lanes, prompt validation, docs validation, and the approved non-publishing package-validation flow.
  - [x] Compare every extracted declaration against the baseline after newline normalization. Assembly ownership and per-file imports are the only expected benchmark-declaration deltas; the retained runtime and benchmark namespaces stay unchanged. Any signature, modifier, member-body, regex, diagnostic, JSON, threshold, or security change blocks completion.
  - [x] Reconcile tracked and untracked paths against the exact File List; audit submodules separately; verify CRLF/UTF-8/final newlines, no unexpected generated/received artifacts, and `git diff --check`. Record finding M14 as closed only for the MCP `SkillCorpus.cs`/benchmark slice.

## Dev Notes

### Scope and Current-State Decisions

- This is Story **11.17c**. The sprint key omits the suffix by repository convention. Story 11.17 is a non-implementable parent; 11.17a CLI, 11.17b SourceTools, and 11.17d Shell remain separate.
- The live aggregate is 2,042 lines/56 declarations. The original `~45` was an architecture-review estimate and must not be used as a completion count.
- Runtime boundary: the first 24 declarations through `GeneratedBoundedContextValidator`, plus `ISkillCorpusBaselineProvider`, `EmptySkillCorpusBaselineProvider`, and `SkillCorpusReleaseGuard`, remain MCP-owned. Benchmark boundary: `SkillBenchmarkPrompt` through `SkillBenchmarkOfflineScorer`, plus the three final benchmark JSON DTO/context declarations, move.
- The moved class has exactly 18 facts: `PromptSet_LoadsTwentyV1PromptsWithDeterministicIds`, `CacheKey_ChangesWhenContractInputsChange`, `CacheKey_ChangesWhenExpectedShapeChanges`, `CacheKey_ChangesWhenSeedChangesEvenIfOtherwiseEqual`, `ResultPersistence_BlocksWhenRedactionFails`, `ResultPersistence_BlocksWhenSanitizedDiagnosticsContainRawLocalPath`, `ResultPersistence_PersistsWhenRedactionPassedAndSanitizationLooksClean`, `OfflineScorer_PicksHighestPriorityCategoryWhenMultipleAreReported`, `OfflineScorer_UsesStructuralValidatorCategories`, `OneShotPassRate_ComputesAggregateAndComparesAgainstTarget`, `ProviderConfigHash_IsStableAcrossEqualConfigsAndDifferentForVariations`, `DeterminismPolicy_SendsTemperatureZeroAndSeedOnlyWhenSupported`, `BudgetPolicy_FailsClosedForMissingAtLimitExpiredMalformedAndRetryStormState`, `BenchmarkGate_RequiresExactlyTwentyValidPromptResultsAndApprovedBaseline`, `SummarySanitizerAndArtifactWriter_BlockHostileEvidenceContent`, `ResultPersistenceAndGate_BlockMissingProviderMetadata`, `EvidencePath_NormalizesUnderApprovedRootAndRejectsEscapes`, and `PromptSet_LoadsExpectedTwentyIdsByOrdinalOrderingFromFixture`.
- Preserve the 29 benchmark declarations' existing namespace/accessibility even though their assembly changes. This mirrors the repository's prior assembly-ownership moves with stable namespaces and makes the relocation body-preserving. The moved test class uses `Hexalith.FrontComposer.Shell.Tests.Bench.Skills` because its identity is not shipped.
- `SkillBenchmarkModelConfig.ConfigHash` and `SkillBenchmarkCacheKey.Create` call internal `SkillCorpusParser.Sha256Hex`; the exact friend-assembly seam is smaller and safer than widening or copying it. `SkillBenchmarkOfflineScorer` consumes the retained generated-code validator, so dependency direction is Bench -> MCP only.
- MCP has other known multi-type files. Scope the guard to `src/...Mcp/Skills` and `tests/...Shell.Tests.Bench/Skills`; do not claim package-wide MCP compliance or absorb unrelated M14 debt.
- The 26 public benchmark types already ship in the MCP baseline. Their removal is required, intentional, and major-version breaking. Keeping compatibility facades would fail the runtime-clean acceptance criterion; omitting exact compatibility governance would fail Release package validation.
- No v4 approval contract exists at story creation. The exact AC5 path is a visible external-authority gate: implementation may begin, but the ledger/release steps and transition to review must stop until the Release Owner supplies that approval.

### Architecture Compliance and Never-List

- Preserve MCP as the `net10.0` ASP.NET adapter/security boundary referencing Contracts + Schema. No reverse MCP -> tests dependency, new NuGet package, target change, solution project, database, endpoint, schema, wire format, diagnostic category/token, or logging migration.
- Preserve tenant-safe MCP resource projection, 32 KB bounded skill reads, hidden-equivalent auth/unknown responses, validation-before-serving, deterministic corpus/resource ordering, fingerprints, sanitization, and fail-closed behavior.
- Preserve benchmark prompt count/content/order, cache-key inputs, config hashes, provider metadata requirements, redaction/sanitization rules, budget fail-closed states, baseline-write trust rules, 80% one-shot threshold, failure-category priority, and path containment. Benchmark tuning/threshold changes require separate evidence and Release Owner approval.
- Do not move `GeneratedBoundedContextValidator` or its four contracts, alter `docs/skills/frontcomposer/testing/generated-code-validator.md`, duplicate validators/hashes/JSON contexts, create a runtime facade/type forwarder, or publish the Bench project.
- Do not update MCP SDK `1.4.0` to current `1.4.1`, adopt the `2.0.0-preview.1` line, update BenchmarkDotNet, add explicit SDK `Compile` items, alter PaletteScorer benchmarks, or change the physical prompt corpus.
- Do not absorb 11.17a/b/d, LoggerMessage Story 11.18, enforcement Story 11.19, deferred MCP parser/diagnostic hardening, release-evidence redesign, historical path rewrites, generated outputs, snapshots, submodules, or unrelated workspace drift.
- UX impact is **N/A**: no Razor, CSS, copy, layout, navigation, focus, responsiveness, or accessibility surface changes. No visual/e2e UX validation is required beyond proving no UI files changed.

### Current-Tree Update Map

| Path | Required change | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs` | Delete after exact 56-way accounting | Every declaration body except ownership namespace/assembly/import deltas |
| `src/Hexalith.FrontComposer.Mcp/Skills/*.cs` | Add the exact 27 runtime files | MCP namespace/public identity/runtime behavior |
| `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/*.cs` | Add 29 benchmark files; move benchmark tests | Existing benchmark namespace/accessibility/semantics; Performance trait |
| `src/...Mcp/Hexalith.FrontComposer.Mcp.csproj` | Remove prompt embed; add exact Bench friend | Skill markdown, dependencies, packability |
| `tests/...Shell.Tests.Bench.csproj` | Add MCP reference and prompt embed | Non-packable exe, existing packages/Palette benchmarks |
| `tests/...Mcp.Tests/Skills/SkillResourceTests.cs` | Assert prompt absent from MCP | Markdown packaging/resource behavior |
| `tests/...Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs` | Pack/open MCP and prove exact DLL/type/resource boundary | No source-only or zip-only false assurance |
| `tests/...Mcp.Tests/Skills/*Organization*Tests.cs` | Add scoped syntax/identity/ownership guard | Existing Roslyn dependency |
| `src/...Mcp/CompatibilitySuppressions.xml` | Add exact generated/reviewed CP0001 rows | Package-validation scope remains exact |
| `docs/diagnostics/{compatibility-suppressions.json,README.md}` | Govern approved v4.0 MCP break | Schema/fail-closed lifecycle rules |
| `tests/...SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs` | Reconcile exact MCP rows | One-to-one XML/JSON validation |
| `tests/eng/test_pack_release_packages.py` | Pin approved v4 lifecycle | Pre-target/mismatch/expiry rejection |
| `docs/validation/api-summary-baseline.txt` | Remove 26 stale benchmark UIDs | All retained runtime UIDs |
| `docs/migrations/{3.1-to-4.0.md,index.md}` | Explain removal/no NuGet replacement | Existing guides/index conventions |
| `_bmad-output/contracts/fc-4-0-mcp-benchmark-removal-release-version-decision-2026-07-14.md` | Record Release Owner approval | No self-approval or fabricated release decision |
| `.github/workflows/nightly.yml`, `CiGovernanceTests.cs`, `tests/README.md` | Retarget benchmark contract lane | Budget/read-only/candidate evidence semantics |
| `_bmad-output/project-docs/source-tree-analysis.md` | Reflect decomposed ownership | Layer/dependency descriptions |

### Library and Framework Requirements

- Repository pins: .NET SDK `10.0.301`, `net10.0`, C# latest/14, Roslyn `5.6.0`, MCP ASP.NET Core `1.4.0`, BenchmarkDotNet `0.15.8`, xUnit v3 `3.2.2`, runner `3.1.5`, Shouldly `4.3.0`, and NSubstitute `6.0.0-rc.1`. Do not change them in this story.
- SDK projects implicitly compile `**/*.cs`; no project-file compile list is needed. [Official .NET SDK default items](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview#default-includes-and-excludes)
- NuGet shows BenchmarkDotNet `0.15.8` as the latest stable while `0.16.0-preview.1` is preview; the existing package is compatible with `net10.0`. [BenchmarkDotNet package](https://www.nuget.org/packages/BenchmarkDotNet)
- NuGet lists MCP ASP.NET Core `1.4.1` as the latest stable and `2.0.0-preview.1` as preview as of 2026-07-14. Keep repository-pinned `1.4.0`; SDK upgrades are outside this mechanical story. [ModelContextProtocol.AspNetCore package](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore)
- xUnit v3/Microsoft Testing Platform test projects are standalone executables and support focused class filters; retain the repository's direct-executable and trait-lane conventions. [xUnit v3 with Microsoft Testing Platform](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)

### Testing Requirements

Use Release alignment, serial builds, `DiffEngine_Disabled=true`, and no live publish. The exact direct-executable filter spelling must be confirmed against the repository-pinned xUnit runner before recording evidence.

```bash
dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj \
  -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
dotnet build tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj \
  -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests \
  -namespace 'Hexalith.FrontComposer.Mcp.Tests.Skills*' -parallel none
DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests \
  -class Hexalith.FrontComposer.Mcp.Tests.Skills.McpRuntimePackageBoundaryTests -parallel none
DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests.Bench/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.Bench \
  -class Hexalith.FrontComposer.Shell.Tests.Bench.Skills.BenchmarkHarnessTests -parallel none
DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests \
  -class Hexalith.FrontComposer.Testing.Tests.PackageBoundaryTests -parallel none

python3 eng/llm_benchmark.py validate-prompt-set --root . --output artifacts/benchmark/prompt-set.json

DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --no-restore \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 \
  --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"
DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --no-restore \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 --filter "Category=Governance"
DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --no-restore \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 --filter "Category=Performance"

pwsh -NoLogo -NoProfile -File eng/validate-docs.ps1
python3 -m unittest tests/eng/test_pack_release_packages.py

# One-time discovery only: review this generated file and copy only the exact 26 CP0001 rows.
dotnet pack src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj -c Release \
  -o /tmp/frontcomposer-11-17c-apicompat -p:Version=4.0.0-review.discovery \
  -p:EnableFrontComposerPackageValidation=true -p:ApiCompatGenerateSuppressionFile=true \
  -p:ApiCompatSuppressionOutputFile=/tmp/frontcomposer-11-17c-generated-suppressions.xml

python3 eng/pack_release_packages.py --version 4.0.0-review.11-17c \
  --output /tmp/frontcomposer-11-17c-packages

# Run only after Release Owner approval and the authorized breaking implementation commit exist.
npx semantic-release --dry-run --no-ci

BASELINE=<implementation-start-commit>
git diff --name-status "$BASELINE"
git ls-files --others --exclude-standard
git diff --submodule=short "$BASELINE" -- references
rg --files -g '*.received.*' -g '!references/**' -g '!**/bin/**' -g '!**/obj/**'
git diff --check
```

If package validation cannot reach/cache the `3.0.0` baseline, record the exact feed/cache blocker and retain focused source/assembly/package evidence; do not disable package validation or claim the compatibility AC passed. Test totals are comparison evidence, not fixed AC values.

### Previous-Story Intelligence

- Story 11.16 review caught a lost null/empty behavior in a supposedly mechanical extraction. Compare all 56 baseline declaration bodies; a green build alone does not prove equivalence.
- Story 11.16 also caught omitted root gitlinks in its File List. Reconcile tracked, untracked, and submodule paths separately and exclude unrelated workspace changes.
- Story 11.17a established the strongest guard pattern: non-empty scoped source scan, recursive namespace traversal, conditional-branch activation, synthetic negative, and assembly-wide exported-type pin. Reuse that logic rather than inventing a weaker text count.
- Story 11.17b proves a split aggregate with no eponymous type should be deleted, current-tree topology should be updated, historical references should remain untouched, and normalized body comparison plus smallest-correct imports should be recorded.

### Git Intelligence

- At creation, `main` and `origin/main` are clean and point to `5718f85b93ede05393219eae49dcc801b34323bd`.
- Recent CLI/SourceTools split commits (`7f53cf3f`, `6ee67cfb`) are the direct organization precedents. Earlier benchmark commits `0cbd31a1` and `84fb9055` introduced/hardened the harness and reveal the prompt/resource/release-governance coupling; preserve their behavior rather than re-designing it.
- Follow the repository Git instructions. Delivery requires an authorized breaking Conventional Commit marker so the approved post-commit semantic-release dry run can be truthful; if the dev agent lacks commit authority, it must stop at that explicit gate and request it. Never publish, tag, or hand-edit semantic-release-owned `CHANGELOG.md` during validation.

### Project Structure Notes

- Expected source delta: delete one aggregate; add 27 MCP runtime files and 29 Bench benchmark files; move one test file; add one scoped Governance test file and one MCP runtime-package boundary test file.
- Expected project delta: MCP and Bench csproj only. The Bench project already appears in `Hexalith.FrontComposer.slnx`; no solution update is allowed.
- Expected compatibility/docs/automation delta is bounded by the Current-Tree Update Map. The physical prompt set, generated-code skill page/tests, `eng/llm_benchmark.py`, package inventory, central versions, and unrelated source remain unchanged.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-11.17] — non-implementable parent, 11.17c split/relocation, Performance trait, validation lane, and runtime-package exclusion.
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-readiness-major-issues.md#Proposal-2B] — approved independently reviewable child decomposition and durable guard.
- [Source: _bmad-output/planning-artifacts/prd.md#Functional-Requirements] — FR25 intentional contract evolution and FR29 architecture remediation.
- [Source: _bmad-output/planning-artifacts/prd.md#Non-Functional-Requirements] — NFR2 dependency direction, NFR5 security, NFR9 benchmark governance, NFR11 tests, and NFR12 release evidence.
- [Source: _bmad-output/project-docs/architecture-quality-review-2026-07-04.md#M14] — original god-file finding and explicit benchmark-runtime concern.
- [Source: _bmad-output/planning-artifacts/architecture.md] and [Source: _bmad-output/project-docs/architecture.md#MCP-server] — MCP adapter/security boundary and dependency direction.
- [Source: _bmad-output/project-context.md#MCP-Server-Rules] — fail-closed MCP contracts; [Source: _bmad-output/project-context.md#Testing-Rules] — solution lanes and benchmark ownership.
- [Source: src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs] — authoritative live 56-declaration inventory and coupling.
- [Source: src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj] and [Source: tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj] — current resources, friends, dependencies, and packability.
- [Source: tests/Hexalith.FrontComposer.Mcp.Tests/Skills/{SkillResourceTests,GeneratedCodeValidatorTests,BenchmarkHarnessTests}.cs] — runtime/resource/benchmark preservation evidence.
- [Source: docs/skills/frontcomposer/testing/generated-code-validator.md] — public validator ownership; [Source: docs/validation/api-summary-baseline.txt] — 26 currently public benchmark UIDs.
- [Source: Directory.Build.targets], [Source: docs/diagnostics/compatibility-suppressions.json], and [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs] — package-validation baseline and exact suppression governance.
- [Source: .github/workflows/nightly.yml], [Source: tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs], and [Source: tests/README.md#LLM-Benchmark-And-Release-Evidence] — scheduled benchmark owner and evidence contract.
- [Source: _bmad-output/implementation-artifacts/11-16-fatal-hydration-json-and-generated-literal-helper-consolidation.md] — mechanical-refactor review lessons.
- [Source: _bmad-output/implementation-artifacts/11-17-{cli-package-split,sourcetools-package-split}.md] — current organization/identity guard precedents.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-14 baseline `5718f85b93ede05393219eae49dcc801b34323bd`: Roslyn captured 56 direct declarations (53 public, three internal), all declaration bodies, MCP exported/resource identities, 12 corpus resources, corpus manifest hash `f78946865257929be43bfaaeebf4419ffb3a0d8768c544c0a1fa3e98e84b7109`, 20 prompt IDs, and prompt hash `248af6dafbff9e4eae3e1640a09f74254cc68a9fd2ffb7b7e2bbd3b4363ae877`.
- Normalized baseline-to-split Roslyn comparison: 56 current, zero duplicates, zero missing, zero changed declaration bodies.
- ApiCompat discovery against package baseline `3.0.0`: exactly 26 `CP0001` public type removals; reviewed suppression XML contains those 26 rows only.
- Focused pre-approval evidence: MCP Skills 57/57, relocated benchmark harness 18/18, Testing package boundary 3/3, MCP packaged-runtime boundary 1/1, nightly governance 1/1, release-plan Python 9/9, prompt validation, and docs/DocFX validation passed.
- Release Owner approval was received on 2026-07-15 and recorded at the exact decision-contract path. The ledger was then advanced to `v4.0`, the authorized breaking implementation commit `a7e94471` was created, the non-publishing release pack passed, and semantic-release analysis classified the range as major with next version `4.0.0`.
- Concurrent upstream work landed while the story was active and was preserved through merges: v3.2.1/v3.2.2 release records, immutable release-evidence changes, and root gitlink advances for Builds/EventStore/Memories/Tenants. These changes are outside the Story 11.17c File List; `a7e94471` remains the exact story-controlled change ledger.

### Completion Notes List

- Deleted the 2,042-line aggregate and retained exactly 27 public MCP runtime declarations in same-named files; moved the exact 29 benchmark declarations (26 public, three internal) and the 18-fact Performance harness to the non-packable Bench executable.
- Preserved all 56 declaration bodies after newline normalization. MCP corpus count stayed 12 and manifest hash stayed `f78946865257929be43bfaaeebf4419ffb3a0d8768c544c0a1fa3e98e84b7109`; prompt count stayed 20 and prompt hash stayed `248af6dafbff9e4eae3e1640a09f74254cc68a9fd2ffb7b7e2bbd3b4363ae877`.
- The MCP package now exports the exact 27 runtime Skills types, zero `SkillBenchmark*` types, and no benchmark prompt. Bench owns the exact 29 declarations and one prompt resource; its only non-public MCP usage is the two approved `SkillCorpusParser.Sha256Hex` call sites.
- Release Owner approved the intentional v4 break. ApiCompat and the JSON ledger reconcile exactly 26 `CP0001` type removals, expire before `v4.1`, and provide no runtime NuGet replacement. Commitlint passed the breaking message; semantic-release analysis selected major / `4.0.0`.
- Release validation passed with zero build warnings/errors: MCP Skills 57/57, relocated harness 18/18, Testing package boundary 3/3, default lane 4,098/4,098, Governance 292/292, Performance 26/26, prompt validation, DocFX/docs validation, nine release-plan tests, and eight `.nupkg` plus eight `.snupkg` artifacts.
- Story-controlled files are UTF-8 without BOM, CRLF-terminated with final newlines, and free of unexpected `*.received.*` or untracked artifacts. `git diff --check` is clean. Architecture finding M14 is closed only for the MCP `SkillCorpus.cs`/benchmark slice.

### File List

- `.github/workflows/nightly.yml` (modified)
- `_bmad-output/contracts/fc-4-0-mcp-benchmark-removal-release-version-decision-2026-07-14.md` (added)
- `_bmad-output/implementation-artifacts/11-17-mcp-runtime-split-and-benchmark-relocation.md` (added)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified)
- `_bmad-output/project-docs/source-tree-analysis.md` (modified)
- `docs/diagnostics/README.md` (modified)
- `docs/diagnostics/compatibility-suppressions.json` (modified)
- `docs/migrations/3.1-to-4.0.md` (added)
- `docs/migrations/index.md` (modified)
- `docs/validation/api-summary-baseline.txt` (modified)
- `eng/validate-docs.ps1` (modified)
- `src/Hexalith.FrontComposer.Mcp/CompatibilitySuppressions.xml` (added)
- `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj` (modified)
- `src/Hexalith.FrontComposer.Mcp/Skills/EmptySkillCorpusBaselineProvider.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/FrontComposerSkillMcpResource.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/FrontComposerSkillResourceProvider.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/GeneratedBoundedContextValidator.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/GeneratedCodeDiagnostic.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/GeneratedCodeFailureCategory.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/GeneratedCodeFile.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/GeneratedCodeValidationResult.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/ISkillCorpusBaselineProvider.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/InvalidSkillCorpusException.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs` (deleted)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusAggregateManifest.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusAggregateManifestBuilder.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusDiagnostic.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusDiagnosticCategory.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusLoader.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusManifestEntry.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusParser.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusReferenceValidator.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusReleaseGuard.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusResource.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusSnapshot.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusSnippetValidator.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusSource.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusValidationResult.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillResourceDescriptor.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillResourceReadOptions.cs` (added)
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillResourceReadResult.cs` (added)
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs` (added)
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs` (modified)
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillTypeOrganizationGovernanceTests.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj` (modified)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/BenchmarkHarnessTests.cs` (moved from `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/BenchmarkHarnessTests.cs`)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkArtifactBuildResult.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkArtifactWriter.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkBaselineArtifact.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkBaselinePolicy.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkBaselineWriteDecision.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkBudgetPolicy.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkBudgetState.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkBudgetStatus.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkCacheKey.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkCachePolicy.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkDeterminismPolicy.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkEvidencePath.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkEvidenceStatus.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkGate.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkGateResult.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkGateStatus.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkJsonContext.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkModelConfig.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkOfflineScorer.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkPrompt.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkPromptDto.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkPromptSet.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkPromptSetDto.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkProviderCapabilities.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkProviderRequest.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkRedactionStatus.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkResult.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkScore.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/SkillBenchmarkSummarySanitizer.cs` (added)
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` (modified)
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs` (modified)
- `tests/README.md` (modified)
- `tests/eng/test_pack_release_packages.py` (modified)

## Change Log

- 2026-07-15 — Split the MCP skill corpus into 27 runtime files, relocated 29 benchmark declarations and the 18-fact harness to Bench, moved the prompt resource, added exact organization/package/compatibility governance, documented the approved v4 removal, retargeted nightly/docs topology, and completed release-aligned validation.
