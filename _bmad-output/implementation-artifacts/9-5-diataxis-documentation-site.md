# Story 9.5: Diataxis Documentation Site

Status: ready-for-dev

> **Epic 9** - Developer Tooling & Documentation. Covers **FR68**, **FR69**, **NFR95**, **NFR96**, **NFR97**, and **NFR98**. Builds on Stories **8-5**, **9-1**, **9-2**, **9-3**, and **9-4**. Applies lessons **L01**, **L06**, **L07**, **L08**, **L10**, and **L11**.

---

## Executive Summary

Story 9-5 publishes the FrontComposer documentation system as a governed product surface:

- Add a DocFX-powered documentation site, pinned through a local .NET tool manifest and CI validation.
- Organize human documentation into the four Diataxis genres: tutorials, how-to guides, reference, and explanation/concepts.
- Keep a single-source authoring model where DocFX renders narrative plus reference content, while MCP/agent docs consume reference-only slices.
- Ship the day-1 customization gradient cookbook with the same example implemented at all four gradient levels.
- Publish diagnostics, migration guides, IDE parity, CLI, and generated-output guidance from the metadata and stubs created by Stories 9-1 through 9-4.
- Add fail-closed validation for navigation, front matter, cross-links, docs links, code snippets, section markers, migration-guide triggers, and MCP reference extraction.

---

## Story

As a developer,
I want a comprehensive documentation site organized by learning need, with a day-1 customization cookbook,
so that I can find tutorials when learning, how-tos when building, reference when checking, and concepts when understanding.

### Adopter Job To Preserve

An adopter should be able to start from a blank FrontComposer project, learn the mental model, copy a customization recipe, inspect API and diagnostic reference, follow migration guidance, and hand the same source material to an MCP/agent workflow without human prose collapsing into agent reference or agent reference leaking into tutorial tone.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Site engine | DocFX only. Do not build a Blazor-native static site in this story. |
| Tooling | Add `.config/dotnet-tools.json` pinning `docfx` to the current stable version verified at implementation time; research on 2026-05-04 found NuGet `docfx` 2.78.5. |
| Site root | Prefer `docs/docfx.json`, `docs/toc.yml`, `docs/index.md`, and `docs/{tutorials,how-to,reference,concepts}` unless implementation discovers an existing stronger repo convention. |
| API reference | Generate from built assemblies or project metadata without recursively initializing submodules. Keep API docs scoped to FrontComposer packages. |
| Reference source | Consume Story 9-4 diagnostic registry/stubs, Story 9-3 IDE matrix, Story 9-2 CLI/path docs, Story 8-5 skill corpus, and public XML docs. |
| Single source | Require explicit front matter / markers for `audience`, `genre`, `mcpReference`, and narrative/reference boundaries. |
| Cookbook | Day-1 customization gradient cookbook solves one problem at annotation, template, slot, and full-replacement levels with compile-checked snippets. |
| Tests | Add docs build/link/schema/snippet/MCP-extraction/migration-trigger tests; include `docfx metadata`/`docfx build` in validation. |

Start here: T1 tool/site skeleton -> T2 taxonomy/front matter -> T3 single-source marker extraction -> T4 diagnostic/API/reference publication -> T5 cookbook -> T6 migration guide trigger -> T7 CI/docs validation.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | The documentation site is generated | The docs build runs | DocFX produces the site from a checked-in `docfx.json`; Blazor-native SSG output is not introduced for v1 documentation publication. |
| AC2 | DocFX tooling is restored | `dotnet tool restore` runs | A local tool manifest pins `docfx` to a stable version, and CI uses that manifest instead of relying on a globally installed tool. |
| AC3 | DocFX metadata is generated | The API reference stage runs | FrontComposer public API docs are generated from repo-owned packages using project or assembly metadata, with XML documentation files available for public API surfaces. |
| AC4 | The site navigation is built | A developer opens the docs site | Top-level navigation is organized by Diataxis genres: Tutorials, How-to, Reference, and Concepts/Explanation. |
| AC5 | A docs page is authored | Front matter validation runs | Every content page declares `title`, `description`, `genre`, `audience`, `ownerStory`, `status`, `reviewed`, and stable `uid` or slug metadata. |
| AC6 | Human docs and MCP reference share a source file | The MCP extraction pipeline runs | Narrative sections are stripped from MCP output and reference sections are retained; DocFX output keeps both. |
| AC7 | Narrative/reference markers are malformed, nested incorrectly, duplicated, or missing on a mixed-audience page | Validation runs | The build fails with a deterministic error naming the page, marker, and expected fix. |
| AC8 | A page contains code examples | Docs validation runs | C# examples in tutorials, how-to guides, cookbook pages, diagnostics, and migration guides are either compile-checked or explicitly marked non-compilable with a reason. |
| AC9 | The customization gradient cookbook is published | v1 docs are generated | One page shows the same relative-time `DateTimeOffset` field problem solved at all four levels: annotation, typed template, typed slot, and full replacement. |
| AC10 | Cookbook examples reference customization APIs | Compile checks run | Examples use current FrontComposer contracts, include required usings/package references, and fail if API names drift. |
| AC11 | A diagnostic registry entry from Story 9-4 exists | Docs validation runs | Each active/reserved diagnostic has a published reference page under the canonical diagnostics path, and `HelpLinkUri`/runtime docs links resolve to that page. |
| AC12 | A diagnostic page is published | A developer reads it | It includes problem, common causes, how to fix, example, suppression guidance, migration/deprecation if applicable, related diagnostics, and owner package metadata. |
| AC13 | A Story 9-2 migration or CLI docs link exists | The site builds | CLI inspect/migrate pages publish dry-run/apply semantics, generated-output path contracts, JSON/report schemas, exit codes, and migration-guide links. |
| AC14 | A Story 9-3 IDE parity matrix exists | The site builds | The matrix is published as a reference page and linked from onboarding, troubleshooting, and generated-output path docs. |
| AC15 | A framework change would break a shipped skill corpus example | The change is merged | A migration guide page is required regardless of semver bucket, and validation fails when the guide or diagnostic link is absent. |
| AC16 | A migration guide is required | Docs validation runs | The guide includes old code, new code, why the change happened, affected versions, related HFC diagnostic/migration ID, analyzer/code-fix availability, and skill-corpus update evidence. |
| AC17 | A new attribute, diagnostic, CLI command, MCP resource, generated-output path, or customization API is added | Docs validation runs | The relevant tutorial/how-to/reference/concept index points to it or the change records a story-specific deferred docs owner. |
| AC18 | Public API XML docs are consumed by DocFX | API docs validation runs | Public API reference excludes internal/generated implementation noise, includes summaries for public contracts, and does not hide adopter-facing attributes or customization contracts. |
| AC19 | A page includes local paths, generated output paths, tenant/user values, diagnostics, CLI output, IDE output, or MCP payload examples | Markdown/HTML/JSON output is generated | Output is sanitized and bounded; docs do not publish local usernames, machine names, absolute private paths, tokens, tenant IDs, command payloads, raw exception dumps, or terminal control sequences. |
| AC20 | Link validation runs | Any internal link, image, diagnostic URL, API UID, migration guide, or generated-output path reference is broken | Validation fails closed with the referring page and target. External links may be allowlisted with scheduled validation and deterministic offline behavior. |
| AC21 | Search/index metadata is generated | The site builds | Pages have unique titles, descriptions, canonical slugs, and no duplicate UIDs or hidden orphan pages. |
| AC22 | The site is deployed to GitHub Pages or an equivalent static host | CI publishes artifacts | The build artifact contains only site output, no source secrets, obj/bin folders, local `.git` data, tool caches, or submodule internals. |
| AC23 | The docs build runs in CI | Network, NuGet, DocFX metadata, or package restore fails | The docs job fails or emits a blocking report. It must not silently skip DocFX, API metadata, link validation, or snippet checks. |
| AC24 | Docs validation scans repository paths | It encounters symlinks, junctions, case-sensitive paths, or root-level submodule folders | It stays inside approved docs/source roots, uses project-relative forward-slash paths in reports, and does not initialize or update nested submodules. |
| AC25 | The story implementation touches docs IA or tooling scope | Review checks scope | It does not create custom docs UI components, a Blazor docs shell, a separate docs product backlog, or broad content for features not yet owned by v1 stories. |

---

## Tasks / Subtasks

- [ ] T1. Add DocFX toolchain and site skeleton (AC1-AC4, AC21-AC23)
  - [ ] Create a local .NET tool manifest if missing and pin `docfx` to a verified stable version.
  - [ ] Add `docs/docfx.json`, `docs/index.md`, `docs/toc.yml`, and top-level genre folders.
  - [ ] Configure DocFX build content for Markdown, generated API YAML, resources, and static assets.
  - [ ] Configure metadata generation for FrontComposer packages only; exclude root-level submodule source trees unless intentionally referenced as external links.
  - [ ] Add a local command documented in the repo: `dotnet tool restore`, `dotnet docfx docs/docfx.json`, and optional local serve.
  - [ ] Ensure build output goes to a disposable docs site folder such as `docs/_site` and is ignored by git.

- [ ] T2. Define documentation taxonomy, front matter, and navigation contract (AC4, AC5, AC17, AC21)
  - [ ] Define allowed `genre` values: `tutorial`, `how-to`, `reference`, `concept`.
  - [ ] Define allowed `audience` values such as `adopter`, `framework-contributor`, `agent`, and `operator`.
  - [ ] Add validation for required front matter fields, duplicate slugs, duplicate UIDs, orphan pages, missing TOC entries, and invalid owner stories.
  - [ ] Publish starter index pages for Tutorials, How-to, Reference, and Concepts that explain scope through links and page titles, not marketing copy.
  - [ ] Keep documentation text concise and domain-specific; do not add broad landing-page prose that does not unblock adopter tasks.

- [ ] T3. Implement single-source narrative/reference markers (AC6, AC7, AC19, AC24)
  - [ ] Define marker syntax in front matter or HTML comments, e.g. `<!-- hfc:narrative:start -->` / `<!-- hfc:narrative:end -->` and `<!-- hfc:reference:start -->`.
  - [ ] Add a validator/extractor that produces MCP-safe reference slices from marked pages.
  - [ ] Validate marker pairing, nesting, duplicate sections, unknown marker names, missing required reference sections, and path traversal in generated reference outputs.
  - [ ] Ensure MCP extraction preserves code fences and tables needed by agents while stripping tutorial narrative, conceptual backstory, and human onboarding prose.
  - [ ] Add regression tests for Markdown/HTML/script injection, terminal escapes, and absolute local-path links inside extracted reference material.

- [ ] T4. Publish diagnostic, API, IDE, CLI, and generated-output reference (AC3, AC11-AC14, AC18, AC20)
  - [ ] Consume Story 9-4 diagnostic registry/stubs and publish diagnostics under `docs/reference/diagnostics/` or an equivalent canonical path.
  - [ ] Validate every active registry slug resolves and every generated diagnostic page has required sections.
  - [ ] Publish API reference from XML docs and DocFX metadata without exposing internal generated implementation noise.
  - [ ] Link Story 9-3 IDE parity matrix and generated-output path guidance from reference and troubleshooting pages.
  - [ ] Link Story 9-2 CLI inspect/migrate docs, including JSON/report schemas, exit codes, dry-run/apply behavior, and package/tool distribution.
  - [ ] Keep Story 9-4 as owner of diagnostic registry schema; Story 9-5 transforms it into navigable public docs.

- [ ] T5. Ship the customization gradient cookbook (AC8-AC10, AC17, AC19)
  - [ ] Create `docs/how-to/customization-gradient-cookbook.md` or equivalent.
  - [ ] Use one concrete example across all four levels: relative-time rendering for a `DateTimeOffset` projection field.
  - [ ] Include compile-checked snippets for annotation, typed Razor template, typed slot, and full replacement paths.
  - [ ] Explain what each level preserves: lifecycle wrapper, accessibility contract, generated metadata, hot reload limits, diagnostics, and test expectations.
  - [ ] Include a short decision table for choosing the lowest viable gradient level.
  - [ ] Add snippet tests that fail if customization contract names, package namespaces, or required usings drift.

- [ ] T6. Enforce migration-guide and skill-corpus break triggers (AC15, AC16, AC23)
  - [ ] Add validation that any changed shipped skill corpus example requires either a linked migration guide or explicit non-breaking evidence.
  - [ ] Require migration guide front matter to include `fromVersion`, `toVersion`, `diagnosticId`, `ownerStory`, `skillCorpusImpact`, and `codeFixAvailable`.
  - [ ] Validate old/new code examples, affected package list, analyzer/code-fix status, and related docs links.
  - [ ] Ensure migration pages link from relevant diagnostics, CLI migration docs, release notes, and skill corpus references.
  - [ ] Do not make semver bucket the only migration trigger; skill-corpus compile break is sufficient.

- [ ] T7. Add docs quality gates and CI integration (AC8, AC19-AC24)
  - [ ] Add unit tests for front matter schema, marker extraction, TOC integrity, duplicate slugs/UIDs, diagnostics docs coverage, and migration trigger rules.
  - [ ] Add snippet compile tests or a deterministic docs snippet harness for C# examples.
  - [ ] Add link validation for internal links, API UID links, diagnostics, migration guides, images/resources, and generated-output path references.
  - [ ] Add sanitization tests for Markdown, HTML, JSON, CSV, terminal output, CLI reports, IDE logs, and MCP examples.
  - [ ] Add a CI docs job that runs restore, build, metadata, validation, and artifact publishing checks.
  - [ ] Ensure CI reports are deterministic, project-relative, and fail closed when DocFX, restore, metadata, or validation cannot run.

- [ ] T8. Final verification and handoff (AC1-AC25)
  - [ ] Run `dotnet tool restore`.
  - [ ] Run the docs validation tests.
  - [ ] Run `dotnet docfx docs/docfx.json` or the checked-in equivalent command.
  - [ ] Run `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [ ] Update completion notes with docs build output path, validation command results, and any deferred docs owners.

---

## Dev Notes

### Current Repository State

- No tracked DocFX site skeleton is present at story creation time. `README.md` is minimal.
- The workspace currently has untracked `docs/skills/frontcomposer/...` files from the skill-corpus work. Treat them as candidate Story 8-5/8-6 inputs after branch integration, but do not assume untracked local files are authoritative without checking git history.
- There is no `.config/dotnet-tools.json` in the repo at story creation time. Story 9-5 should add one for DocFX instead of relying on global tools.
- `Directory.Packages.props` currently pins Roslyn `Microsoft.CodeAnalysis.CSharp` to `4.12.0`; do not upgrade Roslyn broadly for docs work.
- Existing FrontComposer projects do not yet have a repo-wide XML docs generation policy equivalent to the EventStore submodule. API docs work should add the minimum needed for FrontComposer public API reference and avoid sweeping CS1591 changes outside agreed public API scope.

### DocFX Reality Check

- DocFX uses `docfx.json` as the site configuration, with `build.content` globs controlling Markdown/API YAML transformed to HTML and `build.resource` globs copied as static assets.
- DocFX can generate .NET API docs from assemblies, projects, solutions, or source files. For projects/solutions, it uses MSBuild design-time build behavior and requires restore/prerequisites to be available.
- DocFX URL shape follows file paths relative to `docfx.json` by default. Use `src`/`dest`, stable slugs, redirects, and validation intentionally so diagnostic and migration URLs do not drift.
- NuGet listed `docfx` 2.78.5 as the current stable tool version on 2026-05-04. Re-check before implementation if time has passed, then pin intentionally.

### Diataxis Interpretation for FrontComposer

Use Diataxis as information architecture, not decoration:

| Genre | FrontComposer commitment |
| --- | --- |
| Tutorials | Guided first project and first domain flow. Learning-oriented, sequential, minimal branching. |
| How-to | Task recipes such as customization gradient, CLI inspection, migration, generated-output debugging, override testing. |
| Reference | API docs, attribute catalog, diagnostics, CLI schemas, MCP resources, generated paths, IDE matrix. |
| Concepts | Why FrontComposer uses source generation, eventual consistency UX, customization gradient rationale, MCP/human docs split, deprecation policy. |

Do not collapse concepts into reference or how-to. Developers need both "what exactly is the API" and "why does this framework behave this way".

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 8-5 | Story 9-5 | Skill corpus Markdown, marker/front matter validation, MCP resource boundaries, benchmark prompt set, and generated-code validator docs. |
| Story 9-1 | Story 9-5 | Drift diagnostics, baseline update guidance, generated output shape, and docs-link expectations. |
| Story 9-2 | Story 9-5 | CLI inspect/migrate docs, generated-output path contract, migration report schema, dry-run/apply semantics, and migration-guide links. |
| Story 9-3 | Story 9-5 | IDE parity matrix, evidence manifests, generated-output debugging workflow, VS Code Dev Kit prerequisites, and contributor/adopter debugging split. |
| Story 9-4 | Story 9-5 | Diagnostic registry, docs stubs, HelpLinkUri policy, deprecation messages, migration IDs, and API compatibility governance. |
| Story 10-x | Story 9-5 | Later visual/specimen/accessibility docs screenshots and docs quality expansion. |

### Scope Guardrails

Do not implement these in Story 9-5:

- A Blazor-native documentation site or custom docs shell.
- Custom DocFX theme work beyond minimal branding/navigation needed for usable v1 docs.
- New public diagnostics/deprecation governance not already owned by Story 9-4.
- CLI inspect/migrate behavior beyond documentation publication. Owner: Story 9-2.
- IDE conformance harness behavior beyond documentation publication. Owner: Story 9-3.
- Runtime MCP server changes beyond reference extraction validation. Owner: Stories 8-1 through 8-6.
- Recursive submodule initialization, nested submodule scans, or docs generation from external submodule internals.
- Broad Roslyn, Fluent UI, .NET SDK, or package train upgrades.
- A complete docs corpus for every future v1.x feature. Add story-specific deferrals instead.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Visual docs examples and screenshot regression for generated UI pages. | Story 10-2 |
| Mutation testing for docs validators and marker extractor. | Story 10-4 |
| Public release-hosting domain and canonical base URL finalization. | Release automation story / product decision |
| Full localization of documentation site. | Deferred v1.x product decision |
| Custom DocFX theme or search customization beyond default usability. | Deferred v1.x docs design decision |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-9-developer-tooling-documentation.md#Story-9.5`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR68`] - four Diataxis documentation genres as generated documentation site.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR69`] - migration guide required when shipped skill corpus examples break.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md#Documentation-Strategy`] - DocFX decision, single-source docs strategy, Diataxis genres, migration guide trigger, customization cookbook.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#Maintainability-Sustainability`] - deprecation, migration, diagnostic ID, and solo-maintainer constraints.
- [Source: `_bmad-output/implementation-artifacts/8-5-skill-corpus-and-build-time-agent-support.md`] - skill corpus and MCP resource handoff.
- [Source: `_bmad-output/implementation-artifacts/9-1-build-time-drift-detection.md`] - drift diagnostics and generated-output handoff.
- [Source: `_bmad-output/implementation-artifacts/9-2-cli-inspection-and-migration-tools.md`] - CLI and migration handoff.
- [Source: `_bmad-output/implementation-artifacts/9-3-ide-parity-and-developer-experience.md`] - IDE matrix and evidence handoff.
- [Source: `_bmad-output/implementation-artifacts/9-4-diagnostic-id-system-and-deprecation-policy.md`] - diagnostic registry/stubs and deprecation handoff.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review vs. elicitation sequencing.
- [Source: `README.md`] - current minimal repository README.
- [Source: `Directory.Packages.props`] - package pin constraints and Roslyn version guardrail.
- [Source: DocFX Config](https://dotnet.github.io/docfx/docs/config.html) - `docfx.json`, content/resource globs, metadata, URL behavior.
- [Source: DocFX .NET API Docs](https://dotnet.github.io/docfx/docs/dotnet-api-docs.html) - XML docs/API metadata generation behavior.
- [Source: NuGet `docfx` package](https://www.nuget.org/packages/docfx) - current stable tool version and local tool install command.
- [Source: Diataxis](https://diataxis.fr/index.html) - four documentation needs and genre model.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-04: Story created via `/bmad-create-story 9-5-diataxis-documentation-site` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### File List

(to be filled in by dev agent)
