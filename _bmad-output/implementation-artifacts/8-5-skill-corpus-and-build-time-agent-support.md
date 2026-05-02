# Story 8.5: Skill Corpus & Build-Time Agent Support

Status: ready-for-dev

> **Epic 8** - MCP & Agent Integration. Covers **FR55**, **FR58**, **NFR85**, and the Epic 8 handoff into **FR69/FR73**. Builds on Stories **8-1** through **8-4** MCP descriptors, visibility, lifecycle, and Markdown projection rendering; consumes the PRD documentation strategy for single-source human/agent docs. Applies lessons **L03**, **L04**, **L06**, **L07**, **L08**, **L10**, **L11**, and **L14**.

---

## Executive Summary

Story 8-5 makes FrontComposer teachable to LLM coding agents without forking the documentation system:

- Ship a versioned skill corpus inside `Hexalith.FrontComposer.Mcp` as MCP-discoverable Markdown resources and NuGet package content.
- Use one source of truth for human docs and agent skills, with explicit front-matter/section markers controlling what the MCP resource renderer exposes.
- Cover attribute references, domain-modeling conventions, generated-code expectations, command/projection/lifecycle/MCP patterns, and sample microservice structure.
- Add a build-time structural validator and benchmark harness that can score generated bounded-context code against compile, package-boundary, SourceTools, manifest, and sample-shape expectations.
- Keep the nightly LLM benchmark apparatus ready for Story 10-6 while avoiding agent-specific prompt engineering, model-provider orchestration, or schema fingerprint work in this story.

---

## Story

As a developer,
I want a versioned skill corpus that teaches LLM agents how to write FrontComposer domain code and a benchmark that validates agent code generation quality,
so that AI-assisted development produces compilable, correct microservices on the first attempt.

### Adopter Job To Preserve

An adopter should be able to install or enable FrontComposer once and let an IDE agent fetch current framework guidance from the same package/runtime surface as the MCP server. The agent should learn the actual C# attributes, command/projection conventions, validation patterns, lifecycle expectations, and sample structures from versioned framework-owned resources rather than stale copied prompts or hand-written team notes.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | `Hexalith.FrontComposer.Mcp` is packaged | The package is built | The skill corpus ships with the MCP package and is versioned with the framework; there is no separate `.Skills` package that can drift from MCP descriptors. |
| AC2 | An MCP client lists resources | The FrontComposer MCP server is enabled | Skill corpus resources are discoverable with stable `frontcomposer://skills/...` URIs, safe titles/descriptions, `text/markdown` content, and deterministic ordering. |
| AC3 | A human developer opens the documentation site source | The same source file is used for skill output | Narrative/concept sections and reference/agent sections are separated by explicit metadata so the docs site can include both while MCP skill resources expose only agent-safe reference guidance. |
| AC4 | The skill corpus is inspected | Reference coverage is checked | It includes attribute references, domain-modeling conventions, command patterns, projection patterns, lifecycle/two-call MCP guidance, tenant/policy safety rules, generated partial expectations, sample microservice structure, and test scaffold guidance. |
| AC5 | A skill resource references command or projection APIs | Framework code changes those APIs | The build detects stale attribute names, moved namespaces, removed public types, broken sample snippets, or invalid generated partial expectations before package publication. |
| AC6 | An LLM agent reads the skill corpus | It generates a new bounded context from a fixed prompt | The generated code compiles against the current framework, registers commands/projections using supported attributes, and does not hand-edit generated `.g.cs` output. |
| AC7 | The generated bounded-context output is structurally validated | The validator runs | It checks required files, namespaces, attributes, validation rules, bounded-context registration, sample tests, SourceTools manifest output, no forbidden dependencies, and no tenant/user spoofing fields in agent-authored command inputs. |
| AC8 | A framework change breaks a shipped skill corpus example | The PR/build runs | A migration-guide requirement is raised regardless of semantic-version bucket, and the skill corpus examples must be updated in the same change before release. |
| AC9 | The benchmark harness runs | It scores the v1 prompt set | It supports 20 prompts, pinned model/provider configuration, temperature 0, cached prompt-response artifacts, deterministic structural scoring, and an 80% one-shot target while leaving final nightly gate wiring to Story 10-6. |
| AC10 | Benchmark output is persisted | Results are published or inspected | Results include prompt id, framework version, skill corpus version, model id, scorer version, compile result, validator result, failure category, and redacted diagnostics without prompt secrets, tenant IDs, tokens, claims, customer data, or raw provider internals. |
| AC11 | A team wants local conventions | The agent consumes FrontComposer guidance | Framework skills remain framework-owned defaults; team-specific overrides are supported as external/team skill files without being merged into the framework package or trusted for security decisions. |
| AC12 | MCP resources expose skill content | Authorization, tenant, and visibility context exists | Skill resources are framework reference material, not tenant-specific domain data; they must not include runtime tenant/user context, command payloads, policy decisions, hidden tool lists, or query results. |
| AC13 | The official MCP SDK or resource APIs change | The skill resource adapter is updated | Corpus source files, validator contracts, and benchmark artifacts remain SDK-neutral; SDK DTO mapping stays inside `Hexalith.FrontComposer.Mcp`. |
| AC14 | Story 8-5 completes | Stories 8-6, 9-5, 10-2, and 10-6 continue | Skill corpus resources can later be tied to schema fingerprints, Diataxis docs publication, agent E2E, and signed benchmark releases without redesigning the corpus format. |

---

## Tasks / Subtasks

- [ ] T1. Define the skill corpus source layout and metadata contract (AC1, AC3, AC4, AC8, AC14)
  - [ ] Add a source directory such as `docs/skills/frontcomposer/` or `_bmad-output/planning-artifacts/docs/skills/` only if it aligns with the existing docs pipeline; final packaged content must flow into `Hexalith.FrontComposer.Mcp`.
  - [ ] Define normative front matter for `id`, `title`, `version`, `audience`, `docfx`, `mcpResource`, `resourceUri`, `order`, `sourceDoc`, `narrative`, `references`, and optional `migrationOwner`; unknown required-shape fields or malformed values fail validation.
  - [ ] Use explicit section markers for human-only narrative vs MCP-exposed reference content; MCP extraction exposes only `agent-reference` sections and must fail validation on unknown marker names instead of relying on heading text heuristics.
  - [ ] Require stable resource IDs and URI slugs independent of localized titles or file paths.
  - [ ] Include a corpus manifest listing every skill resource, source file, public API references, sample snippets, and owning story/follow-up.

- [ ] T2. Author the v1 skill corpus content set (AC2-AC4, AC6, AC11, AC12)
  - [ ] Create concise agent-facing Markdown for: package setup, domain attributes, bounded-context registration, command records, projection records, validation rules, policy attributes, tenant context rules, EventStore command/query flow, MCP tool/resource flow, two-call lifecycle, Markdown projection reading, and generated partial boundaries.
  - [ ] Include one complete "new bounded context" path with commands, events or command DTOs as appropriate, projections, validators, registration, and tests using the existing Counter/sample conventions.
  - [ ] Include "do not" rules: do not edit `.g.cs`, do not add MCP SDK references to Contracts/SourceTools, do not invent tenant/user inputs, do not call EventStore directly, do not fork label/humanizer rules, and do not hand-maintain tool schemas.
  - [ ] Keep content reference-oriented for MCP resources: no marketing prose, long narrative, provider-specific prompt hacks, hidden tenant names, customer examples, or runtime environment details.
  - [ ] Support external team skill overlays by documenting precedence as "framework defaults first, team conventions second" while keeping framework validation independent of team files.

- [ ] T3. Package and expose skill resources through MCP (AC1, AC2, AC12, AC13)
  - [ ] Embed or pack the approved skill Markdown and corpus manifest into `Hexalith.FrontComposer.Mcp` via central package configuration.
  - [ ] Add a skill resource provider inside the MCP package with stable resource descriptors, URI templates only where necessary, `text/markdown` content, deterministic ordering by manifest `order` then canonical `frontcomposer://skills/...` URI, duplicate slug/case collision rejection, and bounded response size.
  - [ ] Map resource descriptors and content to official MCP C# SDK resource DTOs only at the adapter edge.
  - [ ] Apply Story 8-2 hidden/unknown-safe response categories for missing or stale skill resources, but do not apply tenant-specific command visibility to framework reference resources.
  - [ ] Add resource tests for listing, reading, deterministic ordering, content type, missing resource, truncation, cancellation, and SDK-boundary containment.

- [ ] T4. Add static drift checks for corpus references and examples (AC4, AC5, AC8)
  - [ ] Build a validator that extracts C# fenced snippets and declared API references from the skill corpus manifest.
  - [ ] Compile snippets against the current solution/package references where feasible; otherwise validate symbol references via Roslyn against `Hexalith.FrontComposer.Contracts`, `SourceTools`, `Shell`, and `Mcp`.
  - [ ] Verify referenced attribute names, namespaces, enum members, generated partial type names, package names, package content paths, sample project paths, resource URI prefixes, SourceTools references, and command/projection conventions still exist.
  - [ ] Fail or emit an explicit diagnostic when any shipped skill example no longer compiles or references a removed convention.
  - [ ] Detect whether a migration guide link is present for each breaking corpus change and block release packaging when missing.

- [ ] T5. Implement the structural validator for agent-generated code (AC6, AC7, AC9, AC10)
  - [ ] Add a validation entry point in an appropriate test/tooling location such as `tests/Hexalith.FrontComposer.Mcp.Tests` or a future testing package seam; avoid creating a separate CLI unless required by Story 9-2.
  - [ ] Validate generated bounded-context projects for required project files, command/projection attributes, validators, registration calls, SourceTools output, test fixtures, no forbidden package references, and no direct infrastructure coupling beyond approved packages.
  - [ ] Check generated code compiles with `TreatWarningsAsErrors=true` and does not modify generated artifacts.
  - [ ] Classify failures into stable machine-readable categories: compile, package-boundary, missing-registration, invalid-attribute, validation-shape, tenant-spoofing, generated-file-edit, test-scaffold, SourceTools-manifest, and unknown.
  - [ ] Redact generated payload values and local paths before benchmark summaries or result artifacts are persisted while keeping enough file/section/diagnostic context for maintainers.

- [ ] T6. Scaffold the v1 benchmark harness without taking over Story 10-6 (AC6, AC9, AC10, AC14)
  - [ ] Define the 20-prompt v1 corpus as versioned input files with ids, expected bounded-context shape, allowed variation notes, and scorer expectations.
  - [ ] Store model/provider configuration as data: model id, temperature 0, seed when supported, timeout, retry policy, and cache-key derivation.
  - [ ] Implement deterministic offline scoring over captured agent output; live model invocation may be stubbed or opt-in until Story 10-6 signs and publishes the benchmark lane.
  - [ ] Persist offline result artifacts with prompt id, framework version, corpus version, model/provider id, provider configuration hash, scorer/validator version, compile result, validator result, failure category, redaction status, generated artifact hash/path token, and sanitized diagnostics.
  - [ ] Record the benchmark target as `>=80%` one-shot pass for v1, with the PRD-documented option to set an honest lower shipping floor only through explicit release governance.
  - [ ] Ensure benchmark artifacts are append-only or content-addressed and do not commit secret prompt additions, provider responses containing sensitive user data, or local machine paths.

- [ ] T7. Wire release and migration-guide guardrails (AC5, AC8, AC14)
  - [ ] Add a release-time check that compares corpus manifest version, package version, public API references, and example compile results.
  - [ ] Require any skill-corpus-breaking change to include a migration guide reference, old/new code example, analyzer/fix-it owner where applicable, and corpus update in the same PR.
  - [ ] Integrate with existing `deferred-work.md` or future Story 9-5 docs index without making Story 8-5 own the whole Diataxis documentation site.
  - [ ] Keep the guardrail scoped: it protects shipped skill corpus examples, not every doc prose paragraph or every post-v1 experimental sample.

- [ ] T8. Tests and verification (AC1-AC14)
  - [ ] Corpus manifest tests for stable IDs, duplicate slug detection, required front matter, invalid narrative/reference markers, missing migration owner, and deterministic ordering.
  - [ ] MCP resource tests for list/read, `text/markdown`, reference-section extraction, bounded output, cancellation, missing resource, SDK DTO containment, and no tenant/user/runtime data leakage.
  - [ ] Packaging tests proving skill files are included in `Hexalith.FrontComposer.Mcp` package output and versioned with the package.
  - [ ] Roslyn/reference tests proving skill snippets compile or fail with clear diagnostics when public API names drift.
  - [ ] Structural validator tests using one good generated bounded-context fixture plus targeted negative fixtures for generated-file edits, tenant spoofing, forbidden dependencies, missing registrations, missing validation tests, and invalid attributes.
  - [ ] Benchmark harness tests for prompt metadata, cache-key determinism, scorer category stability, result artifact schema, pre-persistence redaction, and 20-prompt aggregation math.
  - [ ] Migration-guide guardrail tests for skill-breaking API/reference changes with and without guide metadata.
  - [ ] Adopter-experience tests that diagnostics identify the exact source file/section for missing manifest entries, broken URIs, duplicate titles/slugs, and invalid migration-guide links without requiring repo-local absolute paths.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [ ] Targeted tests: `tests/Hexalith.FrontComposer.Mcp.Tests`, `tests/Hexalith.FrontComposer.SourceTools.Tests`, and packaging tests; Story 10-6 owns live multi-agent/provider benchmark gates.

---

## Dev Notes

### Existing State To Preserve

| File / Area | Current state | Preserve / Change |
| --- | --- | --- |
| `Hexalith.FrontComposer.Mcp` package direction | PRD says `.McpServer` and `.Skills` are merged into `.Mcp`; version skew between the server and corpus is unacceptable. | Put skill packaging/resource exposure in `.Mcp`; do not create a separate runtime package. |
| Stories 8-1 through 8-4 | Own MCP hosting/descriptors, hidden/unknown semantics, lifecycle two-call flow, and Markdown projection rendering. | Skill resources explain these contracts and consume their adapter seams; do not replace them. |
| `Hexalith.FrontComposer.Contracts` | Dependency-free public attributes and contracts. | Skill examples can reference Contracts; do not make Contracts depend on MCP or docs infrastructure. |
| `Hexalith.FrontComposer.SourceTools` | Source generator/analyzer source of truth for command/projection metadata. | Validator should verify snippets and expected generated partials against SourceTools, not duplicate generator logic. |
| Documentation strategy | Human docs and agent skill corpus come from the same Markdown source with narrative/reference markers. | Implement explicit metadata and tests so the MCP corpus strips narrative safely. |
| Story 9-5 | Owns full Diataxis documentation site. | Story 8-5 owns skill corpus source/packaging/validation only; do not build the whole docs site here. |
| Story 10-6 | Owns signed LLM benchmark releases and CI quality gates. | Story 8-5 can scaffold prompts/scorer/offline harness but must leave live provider orchestration and signed release policy to 10-6. |

### Architecture Contracts

- Skill corpus content is framework reference material, not runtime domain data. It must be safe to expose as MCP resources without tenant-specific filtering.
- The skill corpus is SDK-neutral Markdown plus manifest metadata. MCP SDK types appear only in the `.Mcp` resource adapter.
- One source should feed human docs and agent skills. Agent-facing output is not a second hand-maintained prompt library.
- Skill examples are part of compatibility. A change that breaks a shipped skill example requires migration guidance regardless of semantic-version bucket.
- Agent generated-code scoring is structural first: compile, SourceTools output, manifest shape, package boundaries, registration, tests, and no forbidden inputs. Do not score by prose quality.
- Team-specific skill overlays are adopter data, not framework package content and not authorization inputs.

### Skill Corpus Shape

Recommended initial resource tree:

```text
frontcomposer://skills/index
frontcomposer://skills/setup/package-and-hosting
frontcomposer://skills/domain/commands
frontcomposer://skills/domain/projections
frontcomposer://skills/domain/validation
frontcomposer://skills/security/tenant-and-policy-boundaries
frontcomposer://skills/mcp/tools-resources-lifecycle
frontcomposer://skills/mcp/projection-markdown
frontcomposer://skills/samples/new-bounded-context
frontcomposer://skills/testing/generated-code-validator
frontcomposer://skills/migration/versioned-corpus-rules
```

The concrete file layout can differ, but the resource IDs must be stable and tested.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 8-1 | Story 8-5 | MCP package boundary, generated descriptors, SDK containment, package registration, and resource adapter pattern. |
| Story 8-2 | Story 8-5 | Hidden/unknown-safe resource behavior and tenant/policy safety rules for agent-visible guidance. |
| Story 8-3 | Story 8-5 | Two-call lifecycle guidance and read-your-writes sequence to teach agents. |
| Story 8-4 | Story 8-5 | Markdown projection conventions used by agent examples and benchmark prompts. |
| Story 8-5 | Story 8-6 | Corpus manifest/resource version can later participate in schema fingerprints and version negotiation. |
| Story 8-5 | Story 9-5 | Docs site can reuse the same source while preserving narrative/reference section markers. |
| Story 8-5 | Story 10-6 | Benchmark prompt set, structural scorer, and captured artifacts feed signed LLM benchmark releases. |

### Binding Decisions

| Decision | Rationale |
| --- | --- |
| D1. Skill corpus ships inside `Hexalith.FrontComposer.Mcp`. | Matches PRD package-family decision and eliminates `.Mcp` / `.Skills` version skew. |
| D2. Markdown source is shared by docs and MCP skills. | Prevents duplicate docs drift and preserves human-readable explanation alongside agent reference. |
| D3. MCP output exposes reference sections, not narrative sections. | Agents need compact operational guidance; humans still need concepts and tutorials. |
| D4. Skill resources are framework-global, not tenant-scoped. | They contain reference guidance, not domain data; tenant filtering would imply false runtime dependence. |
| D5. Skill examples are compatibility artifacts. | FR69 says breaking shipped corpus examples requires migration guidance regardless of semver bucket. |
| D6. Structural validation beats natural-language scoring. | Compile and SourceTools shape are stable, automatable proof of agent output quality. |
| D7. Benchmark harness is scaffolded, live signed gate deferred. | Story 10-6 owns provider orchestration, signed releases, SBOM, and CI gating policy. |
| D8. Team conventions are overlays, not framework defaults. | Supports adopter corrections without polluting core guidance or security decisions. |
| D9. SDK DTO mapping remains at the `.Mcp` edge. | Protects corpus files and validator tests from MCP SDK churn. |
| D10. Corpus resources and benchmark artifacts are bounded and deterministic. | Applies L14 and keeps agent sessions/packages from accumulating unbounded content. |

### Latest MCP Notes

- As of 2026-05-01, the official MCP documentation lists C# as a Tier 1 SDK and describes servers as exposing tools, resources, and prompts. Story 8-5 should expose corpus content as resources and keep prompt templates optional/future until a concrete client need exists.
- The official MCP prompts specification allows embedded resources in prompt messages, but prompts are user-controlled templates. FrontComposer should not require prompts for the skill corpus; resources are the stable reference surface.
- The official MCP "Build with Agent Skills" guidance describes skills as portable instruction sets with `SKILL.md` plus supporting references. FrontComposer's corpus can mirror that shape internally while exposing it through MCP resources.
- The MCP C# SDK repository identifies `ModelContextProtocol.AspNetCore` as the HTTP-server package and latest release `v1.2.0` on 2026-03-27. Keep SDK usage behind `.Mcp` adapter tests.

### Party-Mode Review Clarifications

These clarifications were applied by `/bmad-party-mode 8-5-skill-corpus-and-build-time-agent-support; review;` on 2026-05-02 and are part of the pre-dev contract.

| Area | Clarification |
| --- | --- |
| Corpus metadata | Front matter, manifest fields, and section marker names are normative contracts. Unknown or malformed extraction-critical values fail validation so docs, MCP resources, and packaging do not invent divergent interpretations. |
| Resource identity | Canonical `frontcomposer://skills/...` URIs are lowercase, stable, independent of titles/file paths, duplicate-checked case-insensitively, and ordered by manifest `order` then URI. Corpus version stays in metadata; schema negotiation is deferred to Story 8-6. |
| MCP extraction | MCP resources expose only marked `agent-reference` sections. Human narrative can share the source file, but narrative must not leak into resource payloads. |
| Drift checks | Build-time drift detection is structural: snippets, declared symbols, generated partial expectations, package content paths, sample project paths, SourceTools references, and migration-guide links. It is not a prose-quality linter. |
| Validator diagnostics | Generated-code validator output is SDK-neutral and machine-readable. Diagnostics include stable category, source file/section where applicable, sanitized path tokens, and enough context for maintainers without persisting secrets or local paths. |
| Benchmark boundary | Story 8-5 proves cached/offline prompt enumeration, scoring, artifact schema, pinned config loading, and redaction. Live provider execution, signed benchmark releases, and CI gate policy remain Story 10-6. |
| Test budget | Use a small golden corpus fixture plus targeted negative fixtures for high-risk structural failures instead of broad near-duplicate Markdown/prose cases. |

### Scope Guardrails

Do not implement these in Story 8-5:

- Schema hash fingerprints, manifest version negotiation, migration delta diagnostics, or renderer abstraction redesign. Owner: Story 8-6.
- Full DocFX site, docs navigation, publishing pipeline, or Diataxis IA. Owner: Story 9-5.
- Live CI calls to Claude/Codex/Cursor/Mistral, signed benchmark releases, SBOM, or public benchmark attestation. Owner: Story 10-6.
- Agent E2E through real IDE clients or browser/chat visual specimens. Owner: Story 10-2.
- New MCP command/resource behavior beyond skill resource listing/reading. Owners: Stories 8-1 through 8-4.
- Team-specific convention files as bundled framework defaults.
- Model-specific prompt hacks, semantic scoring by LLM judge, or telemetry-driven prompt mutation.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Schema fingerprinting of corpus, MCP manifest, and projection contracts. | Story 8-6 |
| Full Diataxis documentation publication and migration guide pages. | Story 9-5 |
| CLI command for inspecting skill corpus and generated outputs. | Story 9-2 |
| Agent E2E across Claude Code, Codex, Cursor, and native chat. | Story 10-2 |
| Signed LLM benchmark release, SBOM, and CI gate policy. | Story 10-6 |
| Team skill overlay discovery convention. | Post-v1 adopter tooling follow-up |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-8-mcp-agent-integration.md#Story-8.5`] - story statement, AC foundation, FR55/FR58/NFR85 scope.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR55`] - versioned skill corpus as NuGet + MCP resources.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR58`] - build-time LLM generation from fixed prompt corpus.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR69`] - migration guide trigger for skill corpus breaking changes.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR73`] - nightly LLM code-generation benchmark.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md#NuGet-Package-Family`] - `.McpServer` + `.Skills` merged into `.Mcp`.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md#Documentation-Strategy`] - shared docs/skill source and narrative/reference markers.
- [Source: `_bmad-output/planning-artifacts/prd/user-journeys.md#Journey-6`] - build-time agent Coda journey and team convention correction pattern.
- [Source: `_bmad-output/planning-artifacts/prd/innovation-novel-patterns.md#LLM-native-code-generation`] - skill corpus, typed partials, and nightly benchmark rationale.
- [Source: `_bmad-output/implementation-artifacts/8-1-mcp-server-and-typed-tool-exposure.md`] - MCP package, descriptors, SDK boundary, and skill corpus follow-up.
- [Source: `_bmad-output/implementation-artifacts/8-2-hallucination-rejection-and-tenant-scoped-tools.md`] - agent-visible safety, hidden/unknown semantics, and descriptor sanitation.
- [Source: `_bmad-output/implementation-artifacts/8-3-two-call-lifecycle-and-agent-command-semantics.md`] - lifecycle guidance for agents.
- [Source: `_bmad-output/implementation-artifacts/8-4-projection-rendering-for-agents.md`] - Markdown projection conventions consumed by the skill corpus.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review and elicitation are complementary.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10`] - deferrals need specific owning stories.
- [Source: Official MCP SDK list](https://modelcontextprotocol.io/docs/sdk) - C# SDK Tier 1 and server resources/tools/prompts support as of 2026-05-01.
- [Source: Official MCP prompts specification](https://modelcontextprotocol.io/specification/2025-06-18/server/prompts) - prompt templates and embedded resources.
- [Source: Official MCP Build with Agent Skills](https://modelcontextprotocol.io/docs/develop/build-with-agent-skills) - skill directory/reference shape and agent-skill workflow.
- [Source: MCP C# SDK repository](https://github.com/modelcontextprotocol/csharp-sdk) - package family and `ModelContextProtocol.AspNetCore` boundary.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-01: Story created via `/bmad-create-story 8-5-skill-corpus-and-build-time-agent-support` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-02: Party-mode review completed via `/bmad-party-mode 8-5-skill-corpus-and-build-time-agent-support; review;`. Applied corpus metadata schema, URI/order, section extraction, structural drift-check, validator diagnostic, offline benchmark artifact, and test-budget clarifications. Ready for advanced elicitation on a later run.

### Party-Mode Review

- **Date/time:** 2026-05-02T11:28:32+02:00
- **Selected story key:** `8-5-skill-corpus-and-build-time-agent-support`
- **Command/skill invocation used:** `/bmad-party-mode 8-5-skill-corpus-and-build-time-agent-support; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), Paige (Technical Writer)
- **Findings summary:** The round found that the story direction is sound, but implementation needed sharper pre-dev contracts around front matter and section markers, deterministic MCP resource identity/order, structural drift inputs, generated-code diagnostic shape, offline benchmark artifact schema, and L06/L07 test-budget discipline.
- **Changes applied:** Tightened T1, T3, T4, T5, T6, and T8; added Party-Mode Review Clarifications covering corpus metadata, URI identity, MCP extraction, drift checks, SDK-neutral diagnostics, benchmark boundaries, and fixture-focused test budgeting.
- **Findings deferred:** Schema fingerprinting and manifest negotiation remain Story 8-6; full DocFX/site publication remains Story 9-5; IDE/client E2E remains Story 10-2; live provider execution, signed benchmark releases, and CI gate policy remain Story 10-6; team-specific overlays remain external and are not bundled framework defaults.
- **Final recommendation:** ready-for-dev

### File List

(to be filled in by dev agent)
