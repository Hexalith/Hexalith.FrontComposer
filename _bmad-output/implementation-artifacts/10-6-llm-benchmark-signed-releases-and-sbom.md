# Story 10.6: LLM Benchmark, Signed Releases & SBOM

Status: ready-for-dev

> **Epic 10** - Framework Quality & Adopter Confidence. Covers **FR73**, **FR75**, **NFR24-NFR26**, **NFR60-NFR63**, and **NFR100**. Builds on Stories **8-5**, **9-5**, and **10-1** through **10-5**. Applies lessons **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 10-6 turns the existing skill benchmark primitives and release workflow into adopter-verifiable quality evidence:

- Run a nightly LLM code-generation benchmark against the embedded v1 prompt set of 20 prompts.
- Use pinned provider/model configuration, temperature 0 where supported, seed where supported, fixed prompt/corpus/scorer/redaction versions, and deterministic cache keys.
- Persist only sanitized prompt-response evidence and score summaries; never publish raw prompts plus raw model outputs if redaction fails.
- Keep the v1 benchmark as a regression gate against the initial baseline plus grace, while deferring 28-day ratchets and model-transition policy to v1.x.
- Generate CycloneDX SBOMs, sign NuGet packages with timestamped author signatures, publish `.snupkg` symbols, and attach/attest release evidence.
- Monitor release minutes and tag-to-nuget.org latency so the package-count collapse trigger in NFR100 is evidence-based rather than subjective.

---

## Story

As a developer,
I want a nightly LLM code-generation benchmark that validates AI-assisted development quality, and signed releases with supply chain transparency,
so that I can trust the framework's AI development story and verify the provenance of every package I install.

### Adopter Job To Preserve

An adopter should be able to answer two questions quickly: "Will AI-generated FrontComposer code still follow the documented contracts?" and "Can I verify what package was built, signed, and published from this repository?"

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Existing benchmark core | Reuse `Hexalith.FrontComposer.Mcp.Skills.SkillBenchmark*` types and `docs/skills/frontcomposer/benchmark-prompts/v1/prompt-set.json`; do not build a parallel prompt/scoring model. |
| Nightly benchmark | Add a scheduled/manual workflow or extend nightly ownership so all 20 v1 prompts run with pinned model/provider config and fixed scorer/validator/redaction policy versions. |
| Determinism | Record model id, provider id, provider config hash, temperature, seed support, `system_fingerprint` or equivalent when available, framework version, corpus version, scorer version, validator version, and redaction policy version. |
| Cache | Cache prompt-response pairs only when `SkillBenchmarkCacheKey` matches the full contract input. Contract changes must invalidate cache. |
| Scoring | Gate v1 against the week-8 initial baseline plus 5 percentage points grace, and never below the initial baseline. Current offline target is 16/20 (80%) via `SkillBenchmarkOfflineScorer.OneShotPassTarget`. |
| Cost | Add a monthly LLM budget cap and a fail-closed "budget exhausted" status that skips API spend but does not claim a passing benchmark. |
| Release | Extend `.github/workflows/release.yml` and `.releaserc.json` so package output includes `.nupkg`, `.snupkg`, CycloneDX SBOM, checksums, and release evidence. |
| Signing | Sign every released `.nupkg` with an approved OSS code-signing certificate and RFC 3161 timestamp. Do not log certificate secrets. |
| Attestation | Add provenance/SBOM attestation where GitHub plan and permissions allow; otherwise publish a clear unsupported-attestation evidence note. |
| Package set | First evaluate the actual MSBuild package inventory, then enforce lockstep versions across all intended packages and fail if package count, package ids, or symbol package coverage drift unexpectedly. |
| Release budgets | Track GitHub Actions billable minutes per release tag and tag-to-nuget.org wall-clock latency. Trigger package-count collapse consideration after 3 consecutive breaches. |
| Submodules | Use root-level submodules only. Never introduce recursive nested submodule checkout. |

Start here: T1 benchmark workflow and config -> T2 benchmark cache/redaction/budget -> T3 release package inventory and lockstep version gate -> T4 SBOM/signing/symbols -> T5 provenance and release evidence -> T6 release budget governance -> T7 docs/governance tests.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- |
| AC1 | The nightly benchmark runs on `main` | It loads the prompt corpus | Exactly 20 v1 prompts are loaded from the embedded `docs/skills/frontcomposer/benchmark-prompts/v1/prompt-set.json` resource and prompt ids are unique and ordinally ordered. |
| AC2 | A benchmark provider is configured | A request is issued | The provider id, model id, temperature, seed value when supported, timeout, retry count, and provider config hash are recorded with the result. |
| AC3 | The configured provider supports deterministic controls | Benchmark requests run | Temperature is `0`, the configured seed is sent, and provider-side fingerprint metadata is recorded when available. |
| AC4 | The configured provider does not support a seed or fingerprint field | Benchmark requests run | The result records an explicit unsupported capability and cannot claim byte-for-byte reproducibility. |
| AC5 | Prompt, expected shape, framework version, corpus version, model config, scorer version, validator version, or redaction policy changes | Cache lookup occurs | Cached prompt-response evidence is rejected with `contract-input-changed`. |
| AC6 | A cached response is reused | The benchmark scores it | The cache key, original provider config hash, and sanitized artifact token match the current benchmark contract. |
| AC7 | A response is generated | Evidence is persisted | Only redacted/sanitized benchmark artifacts are written; raw local paths, secrets, tenant/user ids, command payload bodies, and provider API keys are rejected. |
| AC8 | Redaction fails or sanitized diagnostics contain obvious local paths | Persistence is attempted | The artifact writer blocks persistence and the benchmark result is marked invalid evidence. |
| AC9 | The v1 benchmark completes | Scores are evaluated | Up to 4 legitimate misses out of 20 are allowed, producing a minimum one-shot pass rate of 80%. |
| AC10 | Week-8 initial baseline has not been captured yet | The nightly benchmark runs | The job publishes observability evidence but does not establish a permanent gate without an explicit baseline capture marker. |
| AC11 | The initial baseline has been captured | Later nightly benchmark results are evaluated | The v1 gate is the initial baseline with 5 percentage points grace, and results must not regress below the initial baseline. |
| AC12 | The 28-day rolling ratchet or model-transition policy is requested | Story 10-6 is implemented | The policy is documented as deferred to v1.x and no hidden ratchet is enforced in v1. |
| AC13 | The LLM monthly budget cap is reached or budget metadata is unavailable | The benchmark wants to call the provider | The job fails closed as `budget-exhausted` or `budget-unknown`, avoids API spend, and does not report a pass. |
| AC14 | A benchmark run is cancelled, partially executed, or missing artifacts | Results are evaluated | The run is invalid evidence and cannot update the baseline or release evidence. |
| AC15 | Benchmark result summaries are published | Markdown, JSON, or workflow summaries are written | Output is bounded, escaped, and classified as pass, legitimate miss, invalid evidence, provider unavailable, or budget blocked. |
| AC16 | Release packaging runs | Packages are built | Every packable project emits a `.nupkg` with the same lockstep version. |
| AC17 | Symbol publication is enabled | Packages are built | Every packable library/tool package that should support debugging emits a matching `.snupkg`, or an explicit exception list names why not. |
| AC18 | Release packaging completes | Package ids are checked | The package inventory is stable, expected, and reviewable; unexpected package additions/removals fail release governance. |
| AC19 | A release is tagged | The release workflow runs | A CycloneDX SBOM is generated for the solution/package output and attached to the release evidence. |
| AC20 | SBOM generation uses GitHub license resolution or external metadata | The tool hits rate limits or missing credentials | SBOM generation fails closed or runs in a deterministic offline mode; it does not publish partial SBOMs as complete. |
| AC21 | NuGet packages are signed | Signing completes | Every released `.nupkg` has an author signature and RFC 3161 timestamp, and verification runs before publish. |
| AC22 | Signing certificate secrets are missing or invalid | Release workflow runs | The release fails before publishing and does not fall back to unsigned packages. |
| AC23 | Signed packages are verified | Verification runs on CI | Signature, timestamp, package id, version, and package hash are recorded in release evidence. |
| AC24 | Packages are pushed to nuget.org | Publish completes | Published `.nupkg` and `.snupkg` artifacts correspond to the same lockstep version and checksums recorded before push. |
| AC25 | GitHub artifact attestations are available for the repository/workflow | Release artifacts are built | Provenance and SBOM attestations are generated with minimum required permissions and linked in the release evidence. |
| AC26 | Artifact attestations are unavailable because of plan, repository visibility, or permission limits | Release artifacts are built | The workflow publishes an explicit attestation-unavailable note and still keeps SBOM, signatures, and checksums blocking. |
| AC27 | Workflow permissions are changed for signing, publishing, or attestation | Governance tests run | Permissions remain least-privilege: read-only jobs stay `contents: read`; release write scopes are limited to release, package, attestation, OIDC, and publish steps that need them. |
| AC28 | A workflow checks out submodules | CI or release runs | Checkout uses root-level submodules only (`submodules: true`) and never uses recursive checkout or `git submodule update --init --recursive`. |
| AC29 | Release tests run | Release workflow executes | Non-quarantined blocking tests run before package publication and cannot be bypassed by benchmark, SBOM, signing, or attestation success. |
| AC30 | Release artifacts are uploaded | Evidence is published | Evidence includes package list, checksums, SBOM path/hash, signing verification, symbol package list, benchmark summary link, and publish result. |
| AC31 | A release tag consumes more than 90 billable GitHub Actions minutes | Release budget monitor runs | The breach is recorded with run id, tag, package count, and slow jobs. |
| AC32 | Tag-to-nuget.org wall-clock latency exceeds 2 hours | Release budget monitor runs | The breach is recorded with tag, start/end timestamps, publish outcome, and package count. |
| AC33 | Either release budget breach happens across 3 consecutive releases | Governance evaluates NFR100 | A package-count collapse issue is opened or updated, recommending evaluation of 8 packages to 5 with evidence. |
| AC34 | A package-count collapse issue already exists | Another breach is detected | The existing issue is updated through a stable marker instead of opening duplicates. |
| AC35 | A developer reads release docs | They need local verification | Docs explain benchmark dry run, budget dry run, SBOM generation, signature verification, symbol package expectations, and attestation verification where supported. |
| AC36 | Existing Story 10-5 quarantine lanes exist | Release governance reads test status | Quarantine status remains advisory for quarantined tests only and cannot turn a failed release test lane green. |
| AC37 | Benchmark prompt corpus or skill corpus public examples change | Release governance runs | `SkillCorpusReleaseGuard` and benchmark cache invalidation detect contract drift and require migration-owner evidence when public examples break. |
| AC38 | Provider output includes generated files | The offline scorer validates them | The scorer uses structural categories such as tenant spoofing, generated file edits, package-boundary violations, missing registration, invalid attributes, validation shape, test scaffold, and SourceTools manifest failures. |
| AC39 | Release evidence contains markdown, provider diagnostics, tool output, package metadata, or issue body text | Summaries are written | Workflow commands, markdown control sequences, HTML/script fragments, oversized fields, and multiline injection are escaped, truncated, or rejected. |
| AC40 | Release automation needs write permissions | The workflow is triggered from a fork, PR, or untrusted branch context | Write-capable signing, publish, issue, and attestation steps are skipped or fail closed; they never execute untrusted branch code with write tokens. |

---

## Tasks / Subtasks

- [ ] T1. Add nightly LLM benchmark orchestration (AC1-AC6, AC9-AC15, AC37-AC38)
  - [ ] Add or extend `.github/workflows/nightly.yml` with scheduled/manual benchmark execution.
  - [ ] Load the embedded v1 prompt set through `SkillBenchmarkPromptSet.LoadEmbeddedV1()` instead of parsing a second prompt format.
  - [ ] Add benchmark runner script or CLI command under `jobs/`, `eng/`, or the existing MCP test/tooling surface.
  - [ ] Persist provider/model/scorer/validator/redaction versions and provider config hash with every result.
  - [ ] Implement cache lookup/reuse using `SkillBenchmarkCacheKey` and reject cache mismatches.
  - [ ] Enforce 16/20 one-shot pass target only after valid evidence exists; handle baseline capture separately from nightly scoring.
  - [ ] Keep 28-day rolling ratchet and model-transition rules documented as v1.x deferrals.

- [ ] T2. Harden benchmark evidence, cost, and provider failure behavior (AC3-AC8, AC13-AC15, AC39-AC40)
  - [ ] Send temperature 0 and seed only when provider/API supports those controls; record unsupported determinism explicitly.
  - [ ] Capture `system_fingerprint` or provider-equivalent metadata when available.
  - [ ] Add monthly budget cap state and fail-closed handling for missing/expired budget metadata.
  - [ ] Reuse `SkillBenchmarkArtifactWriter` redaction gates and add fixtures for secrets, local paths, tenant/user ids, command payload fragments, markdown injection, workflow-command injection, oversized diagnostics, cancellation, and partial results.
  - [ ] Ensure raw provider outputs are never uploaded when redaction or artifact-shape checks fail.

- [ ] T3. Inventory package output and lockstep release shape (AC16-AC18, AC24, AC29-AC30)
  - [ ] Define the expected package inventory from evaluated packable projects. Explicit package metadata exists for `Hexalith.FrontComposer.Cli`, `Hexalith.FrontComposer.Mcp`, `Hexalith.FrontComposer.Schema`, and `Hexalith.FrontComposer.Testing`; verify actual MSBuild packability for `Contracts`, `Shell`, and `SourceTools` and record package or exception status for each.
  - [ ] Update `Directory.Build.props`, project metadata, or release scripts so all intended packages use the semantic-release version without per-project drift.
  - [ ] Add governance tests that packable projects produce expected package ids and lockstep versions.
  - [ ] Ensure release tests run before `dotnet pack`, signing, attestation, and publish.

- [ ] T4. Add SBOM, signing, symbols, checksums, and verification (AC17, AC19-AC24, AC27-AC30)
  - [ ] Extend `.github/workflows/release.yml` and `.releaserc.json` to generate `.snupkg` symbol packages where appropriate.
  - [ ] Add CycloneDX SBOM generation for the solution or package output and fail closed on incomplete SBOM evidence.
  - [ ] Sign every `.nupkg` with the approved certificate and RFC 3161 timestamp; verify signatures before push.
  - [ ] Generate checksum manifests for `.nupkg`, `.snupkg`, SBOM, and evidence files before publication.
  - [ ] Keep certificate path/password/key material out of logs, summaries, artifacts, and issue bodies.

- [ ] T5. Add provenance and release evidence publication (AC25-AC30, AC39-AC40)
  - [ ] Add GitHub artifact/SBOM attestation where available using minimum required permissions.
  - [ ] Publish an explicit unsupported-attestation note when repository plan, visibility, or permissions prevent attestation.
  - [ ] Attach release evidence to the GitHub Release: package inventory, checksums, SBOM, signing verification, symbols, benchmark summary, and publish outcome.
  - [ ] Add summary escaping/truncation around all markdown/JSON evidence publication.

- [ ] T6. Add release budget governance and NFR100 trigger (AC31-AC34)
  - [ ] Measure billable minutes per release tag and wall-clock tag-to-nuget.org duration.
  - [ ] Persist release budget history in a reviewable JSON/markdown artifact or stable issue marker.
  - [ ] Open/update a package-count collapse issue after 3 consecutive breaches of 90 billable minutes or 2-hour publish latency.
  - [ ] Include tag, run id, package count, slow jobs, publish status, and suspected package-count pressure.

- [ ] T7. Add governance tests and docs (AC27-AC40)
  - [ ] Extend `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` or add focused release governance tests for no recursive submodules, release test ordering, least-privilege permissions, expected evidence files, package inventory, lockstep versions, signing verification, SBOM generation, and attestation fallback.
  - [ ] Add MCP benchmark tests for baseline capture, cache mismatch, invalid evidence, budget-blocked status, unsupported seed/fingerprint metadata, and redaction failures.
  - [ ] Update `tests/README.md`, release docs, or process notes with local dry-run commands for benchmark, SBOM, signature verification, and release evidence review.

---

## Developer Context

### Existing State

- `.github/workflows/ci.yml` exists and uses root-level `submodules: true`.
- `.github/workflows/release.yml` currently restores, tests, runs semantic-release, and uses `contents: write` for tag/release creation.
- `.releaserc.json` currently builds, packs to `./nupkgs`, pushes `.nupkg` files to nuget.org, and uploads `nupkgs/*.nupkg` to GitHub releases.
- No general `.github/workflows/nightly.yml` exists in the current workspace.
- `docs/skills/frontcomposer/benchmark-prompts/v1/prompt-set.json` already contains 20 v1 prompts.
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs` already defines `SkillBenchmarkPromptSet`, `SkillBenchmarkModelConfig`, `SkillBenchmarkCacheKey`, `SkillBenchmarkCachePolicy`, `SkillBenchmarkArtifactWriter`, `SkillBenchmarkOfflineScorer`, and `SkillCorpusReleaseGuard`.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/BenchmarkHarnessTests.cs` already covers prompt loading, cache-key invalidation, redaction persistence, offline scoring, and the 16/20 one-shot pass target.
- `Directory.Build.props` has shared package validation settings for `IsPackable=true` projects.
- Current packable metadata is uneven: some projects set `IsPackable=true`, some rely on SDK defaults, and `SourceTools` is explicitly non-packable despite being part of the architecture package graph. Treat actual package inventory as a required MSBuild evaluation step, not a static hand-written list.

### Critical Decisions

| Decision | Rule | Rationale |
| --- | --- | --- |
| D1 | Reuse the existing `SkillBenchmark*` core from Story 8-5. | A second benchmark model would create contradictory scoring and cache semantics. |
| D2 | v1 benchmark determinism is best-effort but evidence-first. | Provider APIs may not guarantee identical output forever; metadata must show when deterministic controls are unsupported or backend fingerprints changed. |
| D3 | Cached responses are valid only for exact contract-input matches. | Prompt, expected-shape, scorer, validator, redaction, model, and framework changes all affect benchmark meaning. |
| D4 | Budget exhaustion is not success. | Avoiding spend is valid, but reporting a pass without fresh or valid cached evidence would mislead adopters. |
| D5 | v1 gate is baseline plus grace, with 28-day ratchet deferred. | Epic 10.6 explicitly defers NFR61-NFR62 ratchet/model-transition sophistication to v1.x. |
| D6 | Signing and SBOM are blocking release gates. | Supply-chain transparency is only useful if unsigned or incomplete evidence cannot be published as normal release output. |
| D7 | Attestation is opportunistic but explicit. | GitHub artifact attestations depend on plan, visibility, and permissions; unsupported must be visible without weakening signing/SBOM gates. |
| D8 | Package inventory must be reviewable before package-count collapse decisions. | NFR100 needs evidence across releases, not one noisy run or hidden package drift. |
| D9 | Write-token release steps run only in trusted release contexts. | Fork/PR code must never execute with publish, signing, attestation, issue, or release-write authority. |
| D10 | Release evidence is hostile input. | Tool output, provider diagnostics, package metadata, and markdown can inject misleading summaries unless escaped and bounded. |

### Architecture and Package Boundaries

| Surface | Story 10-6 responsibility |
| --- | --- |
| `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs` | Reuse benchmark primitives; extend only if runner needs missing status types or evidence fields. |
| `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/BenchmarkHarnessTests.cs` | Add runner, baseline, budget, provider capability, and invalid-evidence tests. |
| `.github/workflows/nightly.yml` | Nightly benchmark owner if no cleaner workflow exists. |
| `.github/workflows/release.yml` | Blocking release test ordering, SBOM/signing/symbol/checksum/attestation/evidence steps. |
| `.releaserc.json` | Pack/sign/publish choreography and GitHub release assets. |
| `Directory.Build.props` and packable `.csproj` files | Lockstep package metadata, symbol package settings, package validation, and expected package inventory. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/` | Workflow and release governance tests. |
| `docs/skills/frontcomposer/benchmark-prompts/v1/prompt-set.json` | v1 prompt corpus; changes require cache invalidation and release guard review. |
| `docs/` or `tests/README.md` | Local verification, benchmark dry run, SBOM/signature verification, release evidence guide. |

### Benchmark Contract

- Prompt corpus owner: `docs/skills/frontcomposer/benchmark-prompts/v1/prompt-set.json`.
- Prompt count: 20 for v1; 50+ deferred to v1.x.
- Pass threshold: 16/20 one-shot pass target unless the captured initial baseline plus 5 percentage points grace produces a stricter effective threshold without dropping below the initial baseline.
- Legitimate miss: a generated response that compiles/runs through the validator but misses an expected shape in a known category. Invalid evidence is not a legitimate miss.
- Invalid evidence includes provider failure, budget unknown/exhausted, redaction failure, cache mismatch, partial run, cancellation, missing artifacts, malformed JSON, wrong prompt set, unsupported determinism reported as deterministic, or scorer/validator version drift not recorded.
- Cache key inputs must include prompt id/text/expected shape, framework version, corpus version, provider config hash, scorer version, validator version, and redaction policy version.
- Result summaries must include pass rate, prompt ids, failure categories, invalid-evidence count, provider capability notes, budget status, cache-hit count, and artifact hashes.

### Release Evidence Contract

- Build artifacts: `.nupkg`, `.snupkg`, CycloneDX SBOM, checksum manifest, signing verification report, benchmark summary, release budget summary, and attestation files/links when available.
- Signing: author-sign `.nupkg` packages with timestamp; verify before push.
- Symbols: publish `.snupkg` alongside packages that support debugging; exceptions must be explicit and reviewed.
- SBOM: generated from solution/package output, attached to GitHub Release, and included in checksum/evidence manifest.
- Attestation: use GitHub artifact/SBOM attestation when supported. Missing support must produce a clear evidence note rather than silent omission.
- Package inventory: expected package ids and lockstep version must be checked before publish.
- Release cannot publish if blocking tests, package inventory, signing, SBOM, checksum, or evidence generation fail.

### Latest Technical Notes

- OpenAI's reproducibility guidance describes `seed` plus matching parameters and a `system_fingerprint` field as controls toward mostly deterministic output; it also notes backend changes can still affect determinism.
- The OpenAI Responses API reference documents temperature as a sampling control; use provider-specific capability detection instead of assuming all models expose identical deterministic knobs.
- Microsoft Learn's NuGet signed-package reference requires RFC 3161 timestamps for signed packages so signatures remain valid beyond certificate expiry.
- GitHub artifact attestation docs require workflow permissions such as `id-token: write`, `contents: read`, and `attestations: write`; newer `actions/attest` guidance also documents `artifact-metadata: write` for linked artifact metadata.
- CycloneDX .NET tooling can generate .NET SBOMs from solution/project inputs and can fail when external GitHub license lookup hits unauthenticated rate limits; choose authenticated or deterministic offline behavior deliberately.

### Scope Guardrails

Do not implement these in Story 10-6:

- Flaky quarantine detection/reintroduction logic. Owner: Story 10-5.
- Accessibility, visual specimen, or screen-reader verification gates. Owner: Story 10-2.
- Pact provider/consumer verification. Owner: Story 10-3.
- Mutation testing or FsCheck quality gates. Owner: Story 10-4.
- Broad package upgrades unless required for signing/SBOM/attestation and covered by governance tests.
- A new analytics platform for release duration history.
- Auto-ratchet or model-transition rules for v1.x.
- Recursive nested submodule initialization.
- Publishing raw LLM responses, raw provider logs, certificate material, API keys, local absolute paths, tenant/user ids, command payloads, or unbounded release logs.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 8-5 | Story 10-6 | Skill corpus, embedded benchmark prompt set, offline scorer, cache key, artifact writer, and release guard are the benchmark foundation. |
| Story 9-5 | Story 10-6 | Public docs and API references provide adopter-facing verification instructions and release evidence links. |
| Story 10-1 | Story 10-6 | Testing utilities may support generated-code validation, but benchmark scoring owns LLM quality evidence. |
| Story 10-2 | Story 10-6 | Accessibility/visual evidence can appear in release rollup later; Story 10-6 must not redefine those gates. |
| Story 10-3 | Story 10-6 | Pact evidence may be included in release evidence later; this story owns only SBOM/signing/release provenance. |
| Story 10-4 | Story 10-6 | Mutation/property gates stay separate blocking quality evidence; benchmark misses cannot suppress invalid test evidence. |
| Story 10-5 | Story 10-6 | Release tests must respect quarantine semantics but cannot let advisory quarantine success override blocking release failures. |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| 50+ prompt corpus expansion. | v1.x LLM benchmark expansion |
| 28-day rolling benchmark ratchet. | v1.x benchmark governance |
| Model transition policy across provider/model upgrades. | v1.x benchmark governance |
| Release evidence rollup that also includes Pact, mutation, accessibility, and quarantine debt. | Future release dashboard/process story |
| Automatic package-count collapse implementation after NFR100 trigger. | Product/architecture decision after 3 consecutive release-budget breaches |
| Certificate provider selection and renewal runbook. | Story 10-6 implementation documentation |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-10-framework-quality-adopter-confidence.md#Story-10.6`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Nightly`] - LLM benchmark and nightly gate context.
- [Source: `_bmad-output/implementation-artifacts/10-5-flaky-test-quarantine-and-ci-governance.md`] - release/test governance handoff.
- [Source: `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs`] - existing benchmark prompt, cache, redaction, scoring, and release guard primitives.
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/BenchmarkHarnessTests.cs`] - current benchmark harness tests and 16/20 target.
- [Source: `docs/skills/frontcomposer/benchmark-prompts/v1/prompt-set.json`] - v1 20-prompt corpus.
- [Source: `.github/workflows/release.yml`] - current release workflow.
- [Source: `.releaserc.json`] - current semantic-release pack/publish flow.
- [Source: `Directory.Build.props`] - current packable package validation settings.
- [Source: `.gitmodules`] - root-level submodule list.
- [Source: GitHub Docs artifact attestations](https://docs.github.com/en/actions/concepts/security/artifact-attestations) - provenance/attestation context.
- [Source: GitHub Docs using artifact attestations](https://docs.github.com/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations-to-establish-provenance-for-builds) - workflow permission requirements and attestation usage.
- [Source: Microsoft Learn signed packages](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference) - NuGet timestamp requirement.
- [Source: CycloneDX .NET](https://github.com/CycloneDX/cyclonedx-dotnet) - .NET SBOM generation and external metadata rate-limit behavior.
- [Source: OpenAI Cookbook reproducible outputs](https://cookbook.openai.com/examples/reproducible_outputs_with_the_seed_parameter) - seed and system fingerprint reproducibility notes.
- [Source: OpenAI Responses API reference](https://developers.openai.com/api/reference/responses/create) - temperature and response metadata notes.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-10: Story created via `/bmad-create-story 10-6-llm-benchmark-signed-releases-and-sbom` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### File List

(to be filled in by dev agent)
