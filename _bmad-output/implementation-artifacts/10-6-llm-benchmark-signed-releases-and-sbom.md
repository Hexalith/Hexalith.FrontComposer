# Story 10.6: LLM Benchmark, Signed Releases & SBOM

Status: done

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
| Trusted writes | Baseline updates, release evidence markers, package-count issue updates, and publish steps must run only from trusted release/main contexts; dry runs and PR/fork runs are read-only evidence. |
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
| AC41 | The benchmark gate evaluates a completed run | Baseline and current evidence are compared | The gate uses aggregate one-shot pass count across exactly 20 prompts, treats invalid/missing prompt results as invalid evidence rather than legitimate misses, applies the 5 percentage point grace only to the approved baseline artifact, and never allows a threshold below the initial baseline or 16/20 target. |
| AC42 | A baseline capture or update is requested | The benchmark baseline artifact is written | The artifact records corpus hash, scorer version, validator version, redaction policy version, provider/model/config hash, commit SHA, timestamp, approver marker, and sanitized summary hash; malformed, stale, or unapproved baseline evidence is rejected. |
| AC43 | Release inventory is generated | Artifacts are prepared for signing, SBOM, checksums, symbols, attestation, or publish | Inventory records every package id, lockstep version, commit SHA, artifact path, checksum, symbol artifact or exception, SBOM component entry, signing status, and attestation status; publication is blocked when any required row is missing or mismatched. |
| AC44 | Release verification runs before publish | Any package, symbol, SBOM, checksum, signing verification, timestamp, attestation, or approved fallback is missing, malformed, stale, or unverifiable | Release publication fails closed and no artifact is pushed as a normal release output. |
| AC45 | LLM output, provider diagnostics, package metadata, issue text, or release tool output is consumed | Evidence, logs, summaries, or artifact paths are produced | The content is treated as untrusted data only and is never interpreted as workflow syntax, shell input, file paths outside the evidence directory, trusted release metadata, or executable commands. |
| AC46 | Artifact attestations are unavailable or fail | Release evidence is generated | The workflow distinguishes approved unsupported-attestation fallback from unexpected attestation failure; fallback explains that checksum, signature, SBOM, and commit provenance remain available while attestation is not, and unexpected failure blocks publish. |
| AC47 | Budget governance evaluates benchmark spend or release cost | Budget state is at limit, one unit over limit, missing, malformed, expired, or provider cost metadata is unavailable | Missing or over-limit benchmark budget fails closed before API spend; release-minute and publish-latency budgets record warn-only NFR100 evidence and trigger package-count evaluation after three consecutive breaches. |
| AC48 | Story 10-6 touches release workflows or package metadata | Implementation is planned or reviewed | The story consumes existing versioning, packaging, CI, quarantine, Pact, mutation, accessibility, and publishing contracts; it may add verification gates and evidence but must not redesign those neighboring story owners without a named follow-up decision. |
| AC49 | A benchmark baseline capture, baseline update, release evidence marker, or budget issue update is requested | The workflow runs from a fork, PR, untrusted branch, schedule without approved marker, or local dry run | The write is skipped or fails closed, no durable baseline/issue/release marker is changed, and the run records a read-only evidence status. |
| AC50 | Release artifacts are ready for publish | The manifest is sealed before NuGet or GitHub publication | The manifest binds package ids, versions, checksums, SBOM hash, signing verification, attestation state, benchmark evidence, commit SHA, tag, run id, and workflow ref; publish is blocked if any artifact differs after sealing. |
| AC51 | NuGet or GitHub publication partially succeeds | A later publish step fails or is cancelled | The workflow records a partial-publish incident with package ids, versions, checksums, destinations, failed step, and rerun guidance; reruns must reuse the sealed manifest and cannot silently rebuild or republish different artifacts. |
| AC52 | Timestamp authority, NuGet, GitHub release, attestation, SBOM metadata, or provider APIs are unavailable or rate-limited | Release or benchmark evidence is evaluated | The run records the exact external dependency state and fails closed or uses only an explicitly approved unsupported fallback; it does not substitute stale, unsigned, unattested, or partial evidence as passing. |
| AC53 | Signing, publishing, attestation, or provider credentials are used | Commands, logs, temp files, artifacts, and summaries are produced | Certificate material, passwords, API keys, OIDC tokens, NuGet keys, provider keys, and secret-derived paths are masked, not passed through echoable command arguments when avoidable, cleaned up after use, and rejected by evidence scans if leaked. |
| AC54 | Evidence paths are read, written, attached, or summarized | Tool output or generated metadata supplies filenames or paths | Paths are normalized under an approved evidence root, reject traversal/absolute paths/symlink escapes, and summaries publish logical artifact names instead of local filesystem paths. |

---

## Tasks / Subtasks

- [x] T1. Add nightly LLM benchmark orchestration (AC1-AC6, AC9-AC15, AC37-AC38, AC41-AC42, AC49)
  - [x] Add or extend `.github/workflows/nightly.yml` with scheduled/manual benchmark execution.
  - [x] Load the embedded v1 prompt set through `SkillBenchmarkPromptSet.LoadEmbeddedV1()` instead of parsing a second prompt format.
  - [x] Add benchmark runner script or CLI command under `jobs/`, `eng/`, or the existing MCP test/tooling surface.
  - [x] Persist provider/model/scorer/validator/redaction versions and provider config hash with every result.
  - [x] Implement cache lookup/reuse using `SkillBenchmarkCacheKey` and reject cache mismatches.
  - [x] Enforce the AC41 aggregate 20-prompt one-shot pass rule only after valid evidence exists; handle baseline capture/update through the AC42 approved artifact path.
  - [x] Make baseline capture/update a trusted-context write only; fork/PR/local dry-run executions may report candidate evidence but must not update durable baseline markers.
  - [x] Keep 28-day rolling ratchet and model-transition rules documented as v1.x deferrals.

- [x] T2. Harden benchmark evidence, cost, and provider failure behavior (AC3-AC8, AC13-AC15, AC39-AC42, AC45, AC47, AC49, AC52-AC54)
  - [x] Send temperature 0 and seed only when provider/API supports those controls; record unsupported determinism explicitly.
  - [x] Capture `system_fingerprint` or provider-equivalent metadata when available.
  - [x] Add monthly budget cap state and fail-closed handling for at-limit, over-limit, missing, malformed, expired, provider-cost-failed, and retry-storm budget metadata.
  - [x] Reuse `SkillBenchmarkArtifactWriter` redaction gates and add fixtures for secrets, local paths, tenant/user ids, command payload fragments, markdown injection, workflow-command injection, oversized diagnostics, cancellation, and partial results.
  - [x] Add cache-key fixtures that independently mutate provider id, model id/version, provider config hash, corpus hash, prompt template hash, scorer version, validator version, redaction policy version, framework version, and runtime config.
  - [x] Add baseline fixtures for creation, approved update, stale baseline hash, missing approver marker, malformed baseline JSON, invalid evidence, and grace-threshold boundaries.
  - [x] Add fixtures proving read-only contexts cannot mutate baselines and provider/API outages cannot reuse stale evidence as fresh passing evidence.
  - [x] Ensure raw provider outputs are never uploaded when redaction or artifact-shape checks fail.
  - [x] Normalize benchmark evidence paths under the approved evidence root before writing or summarizing artifacts.

- [x] T3. Inventory package output and lockstep release shape (AC16-AC18, AC24, AC29-AC30, AC43-AC44, AC50-AC51)
  - [x] Define the expected package inventory from evaluated packable projects. Explicit package metadata exists for `Hexalith.FrontComposer.Cli`, `Hexalith.FrontComposer.Mcp`, `Hexalith.FrontComposer.Schema`, and `Hexalith.FrontComposer.Testing`; verify actual MSBuild packability for `Contracts`, `Shell`, and `SourceTools` and record package or exception status for each.
  - [x] Update `Directory.Build.props`, project metadata, or release scripts so all intended packages use the semantic-release version without per-project drift.
  - [x] Add governance tests that packable projects produce expected package ids and lockstep versions.
  - [x] Generate a release inventory manifest containing package id, version, commit SHA, artifact path, checksum, symbol package or exception, SBOM component reference, signing status, attestation status, and publish status.
  - [x] Seal the manifest before publish and verify no package, symbol, SBOM, checksum, signature, attestation, benchmark, tag, run id, or workflow ref changes between sealing and publication.
  - [x] Ensure release tests run before `dotnet pack`, signing, attestation, and publish.

- [x] T4. Add SBOM, signing, symbols, checksums, and verification (AC17, AC19-AC24, AC27-AC30, AC43-AC46, AC50-AC53)
  - [x] Extend `.github/workflows/release.yml` and `.releaserc.json` to generate `.snupkg` symbol packages where appropriate.
  - [x] Add CycloneDX SBOM generation for the solution or package output and fail closed on incomplete SBOM evidence.
  - [x] Sign every `.nupkg` with the approved certificate and RFC 3161 timestamp; verify signatures before push.
  - [x] Generate checksum manifests for `.nupkg`, `.snupkg`, SBOM, and evidence files before publication.
  - [x] Add a release verification script or governance test that fails before publish when package inventory, lockstep versions, symbols, SBOM, checksums, signatures, timestamps, or required evidence do not match the manifest.
  - [x] Keep certificate path/password/key material out of logs, summaries, artifacts, and issue bodies.
  - [x] Treat timestamp-authority, NuGet, GitHub Release, and SBOM metadata outages as explicit fail-closed or approved-fallback states; do not publish unsigned, stale, or partial evidence as complete.
  - [x] Record partial-publish incidents and rerun guidance when any destination accepts only part of the sealed manifest.

- [x] T5. Add provenance and release evidence publication (AC25-AC30, AC39-AC40, AC43-AC46, AC49-AC54)
  - [x] Add GitHub artifact/SBOM attestation where available using minimum required permissions.
  - [x] Detect attestation capability before publish and distinguish supported attestation, approved unsupported-attestation fallback, and unexpected attestation failure.
  - [x] Publish an explicit unsupported-attestation note when repository plan, visibility, or permissions prevent attestation, including what adopters can still verify through checksum, signature, SBOM, and commit provenance.
  - [x] Attach release evidence to the GitHub Release: package inventory, checksums, SBOM, signing verification, symbols, benchmark summary, and publish outcome.
  - [x] Add summary escaping/truncation around all markdown/JSON evidence publication.
  - [x] Bind every attached evidence artifact to the sealed manifest, commit SHA, tag, run id, and workflow ref.
  - [x] Reject generated filenames, paths, or links that escape the approved evidence root or expose local filesystem paths.

- [x] T6. Add release budget governance and NFR100 trigger (AC31-AC34, AC47, AC49)
  - [x] Measure billable minutes per release tag and wall-clock tag-to-nuget.org duration.
  - [x] Persist release budget history in a reviewable JSON/markdown artifact or stable issue marker.
  - [x] Open/update a package-count collapse evaluation issue after 3 consecutive breaches of 90 billable minutes or 2-hour publish latency; this is warn-only governance evidence, not an automatic release block.
  - [x] Update package-count evaluation issues only from trusted release/main contexts using a stable marker; PR/fork/dry-run contexts report what would change without writing it.
  - [x] Include tag, run id, package count, slow jobs, publish status, and suspected package-count pressure.

- [x] T7. Add governance tests and docs (AC27-AC54)
  - [x] Extend `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` or add focused release governance tests for no recursive submodules, release test ordering, least-privilege permissions, expected evidence files, package inventory, lockstep versions, signing verification, SBOM generation, attestation fallback, and hostile evidence summaries.
  - [x] Add MCP benchmark tests for baseline capture/update, cache mismatch, invalid evidence, budget-blocked status, unsupported seed/fingerprint metadata, redaction failures, stale/malformed evidence, and exact threshold boundaries.
  - [x] Add governance tests for trusted-context writes, sealed-manifest publication, partial-publish incident records, secret scan failures, external outage states, and evidence path containment.
  - [x] Update `tests/README.md`, release docs, or process notes with local dry-run commands for benchmark, SBOM, signature verification, and release evidence review.

### Review Findings

- [x] [Review][Patch] Nightly workflow never runs the 20-prompt LLM benchmark [.github/workflows/nightly.yml:35]
- [x] [Review][Patch] Release manifest verification accepts placeholder checksums and SBOM hashes [eng/release_evidence.py:210]
- [x] [Review][Patch] Attestation evidence is generated after semantic-release has already published assets [.github/workflows/release.yml:91]
- [x] [Review][Patch] Release budget governance reads a missing history file and suppresses the failure [.github/workflows/release.yml:122]
- [x] [Review][Patch] Package inventory does not discover unexpected packable projects [eng/release_evidence.py:106]
- [x] [Review][Patch] Benchmark artifact/gate paths can accept missing provider metadata [src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs:1501]

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
| D11 | Benchmark baseline state is an approved artifact, not an implicit latest-success marker. | A scheduled run can be noisy, partial, or cost-blocked; baseline updates need explicit provenance and approval evidence. |
| D12 | Release publication depends on a verification manifest. | A single manifest lets package inventory, symbols, SBOM, checksums, signatures, timestamps, attestations, and fallback notes be checked before publish. |
| D13 | LLM/provider output is never executable release input. | Generated text can contain workflow commands, shell fragments, path traversal, or misleading markdown; every publication surface treats it as untrusted data. |
| D14 | Attestation fallback is a distinct governed state. | Unsupported attestation is acceptable only when explicitly detected and explained; unexpected attestation failure remains blocking. |
| D15 | NFR100 budget breaches trigger evaluation, not automatic package collapse. | Package-count reduction changes public package shape and needs product/architecture approval after measured evidence. |
| D16 | Story 10-6 adds release evidence gates, not release ownership redesign. | Keeping boundaries with Stories 10-2 through 10-5 prevents this story from silently changing adjacent quality policies. |
| D17 | Durable baseline, release-marker, and issue-marker writes require trusted context. | PRs, forks, dry runs, and unapproved scheduled executions can produce evidence but must not mutate release governance state. |
| D18 | Publishing consumes a sealed manifest. | NuGet and GitHub publication are hard to undo; the workflow must prove it is publishing the exact artifacts that passed verification. |
| D19 | External service outages are explicit states, not implicit passes. | Timestamp, NuGet, GitHub, attestation, SBOM metadata, and provider failures must not be hidden behind stale or partial evidence. |
| D20 | Evidence paths are confined to an approved root. | Tool output and generated metadata are untrusted and can otherwise escape into local paths, workspace files, or misleading release links. |

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
- Pass threshold: aggregate one-shot pass count across exactly 20 prompts. The floor is 16/20. After an approved baseline exists, the effective gate is the stricter of 16/20 and the approved baseline pass count minus 1 prompt (5 percentage points), and invalid or missing prompt results are invalid evidence rather than legitimate misses.
- Baseline artifact: a reviewable JSON or markdown evidence file produced by the benchmark runner, with corpus hash, scorer version, validator version, redaction policy version, provider/model/config hash, commit SHA, timestamp, approver marker, sanitized summary hash, and artifact hash.
- Baseline writes: only trusted release/main contexts with an approved marker may create or replace durable baseline artifacts; PR, fork, local dry-run, and unapproved scheduled runs emit candidate evidence only.
- Legitimate miss: a generated response that compiles/runs through the validator but misses an expected shape in a known category. Invalid evidence is not a legitimate miss.
- Invalid evidence includes provider failure, budget unknown/exhausted, redaction failure, cache mismatch, partial run, cancellation, missing artifacts, malformed JSON, wrong prompt set, unsupported determinism reported as deterministic, or scorer/validator version drift not recorded.
- Cache key inputs must include prompt id/text/expected shape, framework version, corpus hash/version, provider id, model id/version, provider config hash, prompt template hash, scorer version, validator version, redaction policy version, and runtime config hash.
- Result summaries must include pass rate, prompt ids, failure categories, invalid-evidence count, provider capability notes, budget status, cache-hit count, and artifact hashes.
- Benchmark gate scope: nightly benchmark failures block the benchmark job and release evidence only when documented contract assertions regress against the current approved corpus; this story does not add PR-blocking policy unless the release workflow explicitly consumes that evidence.

### Release Evidence Contract

- Build artifacts: `.nupkg`, `.snupkg`, CycloneDX SBOM, checksum manifest, signing verification report, benchmark summary, release budget summary, and attestation files/links when available.
- Signing: author-sign `.nupkg` packages with timestamp; verify before push.
- Symbols: publish `.snupkg` alongside packages that support debugging; exceptions must be explicit and reviewed.
- SBOM: generated from solution/package output, attached to GitHub Release, and included in checksum/evidence manifest.
- Attestation: use GitHub artifact/SBOM attestation when supported. Missing support must produce a clear evidence note rather than silent omission.
- Package inventory: expected package ids and lockstep version must be checked before publish.
- Release cannot publish if blocking tests, package inventory, signing, SBOM, checksum, or evidence generation fail.
- Release inventory rows must include package id, lockstep version, commit SHA, artifact path, checksum, symbol artifact or exception, SBOM component reference, signing status, attestation status, and publish status.
- Sealed manifest: the publish stage must consume a pre-publish manifest bound to commit SHA, tag, run id, workflow ref, artifact checksums, SBOM hash, signing verification, attestation state, and benchmark summary hash; any post-seal drift blocks publication.
- Partial publication: if a destination accepts only part of the manifest, release evidence records the accepted artifact ids/checksums and failed step, and reruns must continue from the same manifest rather than rebuilding different artifacts.
- Release evidence allowlist: publish only bounded summaries, artifact hashes, package ids/versions, commit SHA, provider/model capability labels, sanitized failure categories, budget status, and links/paths under the evidence directory. Do not publish raw prompts plus raw completions, raw provider logs, signing secrets, certificate paths, API keys, tenant/user ids, command payload bodies, local absolute paths, provider request ids, or unbounded diagnostic output.
- Capability matrix:
  - Required blocking evidence: package inventory, lockstep version, checksums, `.nupkg` signatures, RFC 3161 timestamps, signing verification, SBOM, release test ordering, sanitized benchmark summary, and root-level submodule checkout.
  - Required or explicit exception: `.snupkg` symbols for packages that support debugging, with reviewed exception rows.
  - Opportunistic but explicit: GitHub artifact/SBOM attestations. Supported attestation publishes links/evidence; approved unsupported state publishes a fallback note; unexpected attestation failure blocks publish.

### Negative Test Requirements

- Benchmark evidence fixtures cover malformed output, missing provider metadata, stale cache evidence, stale/mismatched baseline hash, redaction-only summaries, untrusted-job evidence, cancellation, partial runs, unsupported determinism reported as deterministic, and missing scorer/validator/redaction versions.
- Hostile content fixtures cover markdown injection, GitHub workflow command injection, HTML/script fragments, path traversal, shell fragments, oversized/multiline diagnostics, provider request ids, API keys, org ids, local paths, tenant/user ids, command payload bodies, signing metadata, prompts, and completions.
- Release verification fixtures cover missing SBOM, incomplete SBOM, unsigned package, timestamp verification failure, checksum mismatch, absent symbols without exception, inventory version drift, attestation failure without approved fallback, and release tests ordered after publish.
- Budget fixtures cover monthly benchmark budget exactly at limit, one unit over limit, missing budget config, malformed budget config, expired budget metadata, provider cost API failure, retry storms, and grace-window expiration.
- Trusted-context fixtures cover fork/PR/local dry-run attempts to update baselines, issue markers, release markers, and publish steps.
- Evidence path fixtures cover traversal, absolute paths, symlink escapes, local path disclosure, generated filenames, and logical artifact-name mapping.
- External outage fixtures cover timestamp authority failure, NuGet publish failure after partial success, GitHub release outage, attestation service outage, SBOM metadata rate limit, and benchmark provider outage.

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
- Redesigning versioning, package ownership, CI topology, semantic-release ownership, quarantine policy, Pact verification, mutation/property quality policy, accessibility gates, or release dashboard ownership.
- Recursive nested submodule initialization.
- Publishing raw LLM responses, raw provider logs, certificate material, API keys, local absolute paths, tenant/user ids, command payloads, or unbounded release logs.
- Updating durable baselines, release markers, budget issue markers, or package-count evaluation issues from PR/fork/local dry-run contexts.
- Rebuilding different packages after a partial publish instead of reconciling against the sealed manifest.

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
| 28-day rolling benchmark ratchet. | Future v1.x benchmark governance story requiring product + architecture approval |
| Model transition policy across provider/model upgrades. | Future v1.x benchmark governance story requiring product + architecture approval |
| Release evidence rollup that also includes Pact, mutation, accessibility, and quarantine debt. | Future release dashboard/process story |
| Automatic package-count collapse implementation after NFR100 trigger. | Product/architecture decision record after 3 consecutive release-budget breaches; Story 10-6 only measures, reports, and opens/updates the evaluation issue |
| Certificate provider selection and renewal runbook. | Story 10-6 implementation documentation |
| Multi-destination release rollback automation after a partial publish. | Future release-operations story; Story 10-6 records incident evidence and rerun constraints only |

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

GPT-5 Codex

### Debug Log References

- 2026-05-10: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --filter FullyQualifiedName~BenchmarkHarnessTests --no-restore` passed (17 tests).
- 2026-05-10: `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter FullyQualifiedName~CiGovernanceTests --no-restore` passed (18 tests).
- 2026-05-10: `python eng\llm_benchmark.py validate-prompt-set --root . --output artifacts\benchmark\prompt-set.json` passed.
- 2026-05-10: `python eng\release_evidence.py inventory --root . --expected eng\release-package-inventory.json --output artifacts\release\package-inventory.json` passed.
- 2026-05-10: `python eng\release_evidence.py verify-manifest --manifest tests\ci-governance\fixtures\release-manifest-valid.json` passed.
- 2026-05-10: `dotnet test Hexalith.FrontComposer.sln --configuration Release --no-restore` passed (2909 passed, 3 skipped).
- 2026-05-10: `python -m py_compile eng\llm_benchmark.py eng\release_evidence.py` passed.
- 2026-05-10: `python eng\llm_benchmark.py run-benchmark --root . --budget-artifact <temp-budget> --output <temp-output>` produced 20 prompt results and failed closed as budget-blocked.
- 2026-05-10: `python eng\release_evidence.py verify-manifest --manifest tests\ci-governance\fixtures\release-manifest-valid.json` passed; invalid manifest failed.
- 2026-05-10: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --filter FullyQualifiedName~BenchmarkHarnessTests --no-restore` passed (18 tests).
- 2026-05-10: `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter FullyQualifiedName~CiGovernanceTests --no-restore` passed (18 tests).

### Completion Notes List

- 2026-05-10: Story created via `/bmad-create-story 10-6-llm-benchmark-signed-releases-and-sbom` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-10: Party-mode review hardening applied. Added benchmark baseline/gate oracles, release verification manifest gates, hostile evidence containment, attestation fallback states, budget boundary behavior, NFR100 evaluation scope, and cross-story boundary guardrails.
- 2026-05-10T13:40:02+02:00: Advanced elicitation completed via `/bmad-advanced-elicitation 10-6-llm-benchmark-signed-releases-and-sbom`.
  - Batch 1 methods: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation.
  - Batch 2 methods: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records.
  - Changes applied: Added AC49-AC54; hardened T1-T7; added trusted-context writes, sealed manifest publication, partial-publish incident handling, external outage states, secret handling, and evidence path confinement.
  - Findings deferred: multi-destination rollback automation remains future release-operations work; certificate provider selection still belongs to implementation documentation.
  - Final recommendation: ready-for-dev
- 2026-05-10: Implemented nightly benchmark orchestration, deterministic provider/budget/baseline gates, release package inventory and sealed-manifest evidence scripts, SBOM/signing/symbol/checksum workflow wiring, attestation fallback evidence, release budget NFR100 governance, package metadata lockstep updates, governance fixtures, and local verification docs. Full solution regression suite passed.
- 2026-05-10: Code review patches applied. Nightly now executes a 20-prompt benchmark gate; release manifests reject pending checksum/SBOM evidence; attestation fallback is recorded before publish; release budget history is appended from the current run; package inventory discovers unexpected projects; benchmark artifact/gate validation requires provider metadata.

### Change Log

- 2026-05-10: Added benchmark and release governance implementation for Story 10-6; status moved to review after full validation.
- 2026-05-10: Resolved six code-review findings and moved Story 10-6 to done.

### File List

- `.github/workflows/nightly.yml`
- `.github/workflows/release.yml`
- `.releaserc.json`
- `Directory.Build.props`
- `eng/llm_benchmark.py`
- `eng/release-package-inventory.json`
- `eng/release_evidence.py`
- `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj`
- `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj`
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs`
- `src/Hexalith.FrontComposer.Schema/Hexalith.FrontComposer.Schema.csproj`
- `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`
- `src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/BenchmarkHarnessTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/README.md`
- `tests/ci-governance/fixtures/release-budget-three-breaches.json`
- `tests/ci-governance/fixtures/release-manifest-invalid.json`
- `tests/ci-governance/fixtures/release-manifest-valid.json`

---

## Party-Mode Review

- ISO date/time: 2026-05-10T13:30:34+02:00
- Selected story key: `10-6-llm-benchmark-signed-releases-and-sbom`
- Command/skill invocation used: `/bmad-party-mode 10-6-llm-benchmark-signed-releases-and-sbom; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)

### Findings Summary

- Winston: The benchmark gate and release evidence contracts were directionally sound, but the story needed exact baseline/gate formulas, artifact capability states, redaction allowlists, budget semantics, and boundary language so Story 10-6 does not redesign neighboring quality policies.
- Amelia: Several release promises were not yet executable oracles. Package inventory, lockstep versions, symbols, SBOM, checksums, signatures, attestations, cache behavior, budget exits, and baseline/grace rules needed concrete failing tests and a verification manifest.
- John: The adopter value splits into two jobs: LLM contract evidence and release provenance. Benchmark evidence, package scope, attestation fallback wording, NFR100 consequences, and package-count-collapse deferral needed clearer product-facing outcomes.
- Murat: The highest test risks were invalid evidence and hostile output. Missing provider metadata, stale cache/baseline evidence, redaction leaks, workflow-command injection, release trust-chain failures, budget boundaries, and recursive-submodule regressions needed negative fixtures.

### Changes Applied

- Added AC41-AC48 for exact benchmark gate behavior, approved baseline artifacts, release inventory manifest fields, fail-closed release verification, hostile-output containment, governed attestation fallback, budget boundary semantics, and cross-story boundary ownership.
- Hardened T1-T7 with baseline, cache-key, budget, release manifest, attestation capability, hostile-summary, and governance-test requirements.
- Added Decisions D11-D16 for approved baseline state, release verification manifests, untrusted LLM/provider output, distinct attestation fallback, NFR100 evaluation-only behavior, and preserving release ownership boundaries.
- Strengthened the Benchmark Contract, Release Evidence Contract, Negative Test Requirements, Scope Guardrails, and Known Gaps so implementation can build executable gates without hidden product or architecture decisions.

### Findings Deferred

- The 28-day benchmark ratchet and provider/model transition policy remain deferred to a future v1.x benchmark governance story requiring product and architecture approval.
- Automatic package-count collapse remains deferred to a product/architecture decision record after three consecutive measured release-budget breaches; Story 10-6 only measures, reports, and opens or updates the evaluation issue.
- Certificate provider selection and renewal runbook stay in Story 10-6 implementation documentation rather than being chosen by the pre-dev review.
- Broader release evidence rollup across Pact, mutation, accessibility, and quarantine debt remains a future release dashboard/process story.

### Final Recommendation

ready-for-dev

---

## Advanced Elicitation

- ISO date/time: 2026-05-10T13:40:02+02:00
- Selected story key: `10-6-llm-benchmark-signed-releases-and-sbom`
- Command/skill invocation used: `/bmad-advanced-elicitation 10-6-llm-benchmark-signed-releases-and-sbom`
- Batch 1 method names: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records

### Findings Summary

- Pre-mortem and failure analysis: the remaining high-risk path was false release confidence from untrusted baseline writes, stale budget markers, incomplete external-service evidence, or publication after artifact drift.
- Red-team and security personas: signing, publishing, attestation, provider, and summary surfaces needed explicit secret handling and evidence-path containment because generated/tool output is untrusted.
- Self-consistency validation: the story already separated benchmark and release jobs well, but publish safety needed a single sealed manifest binding artifact checksums to commit, tag, run id, workflow ref, SBOM, signatures, attestation state, and benchmark evidence.
- Chaos and hindsight review: partial NuGet/GitHub publication, timestamp authority outages, SBOM metadata rate limits, provider outages, and reruns after failure needed explicit incident evidence rather than broad rollback automation.
- Occam and matrix review: the lowest-cost hardening is trusted-context write gates, sealed manifest checks, fail-closed outage states, and path/secret scans; broader release rollback tooling is deferred.

### Changes Applied

- Added AC49-AC54 covering trusted-context durable writes, sealed-manifest publication, partial-publish incident handling, external outage states, credential handling, and evidence path confinement.
- Hardened T1-T7 with read-only baseline dry runs, pre-publish manifest sealing, partial-publish records, outage/fallback fixtures, trusted issue-marker updates, and governance tests for path containment and secret scans.
- Added Decisions D17-D20 for trusted writes, sealed publish manifests, explicit external outage states, and approved evidence-root confinement.
- Expanded the Benchmark Contract, Release Evidence Contract, Negative Test Requirements, Scope Guardrails, and Known Gaps without changing product scope or adjacent Epic 10 ownership.

### Findings Deferred

- Multi-destination release rollback automation after a partial publish remains a future release-operations story; Story 10-6 records incident evidence and rerun constraints only.
- Exact certificate provider selection and renewal process remain implementation documentation, not a pre-dev architecture choice.
- Exact sealed-manifest file format remains an implementation detail as long as AC50 and the Release Evidence Contract are satisfied.

### Final Recommendation

ready-for-dev
