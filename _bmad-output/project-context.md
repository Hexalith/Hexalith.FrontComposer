---
project_name: 'Hexalith.FrontComposer'
user_name: 'Jerome'
date: '2026-05-10'
sections_completed:
  ['technology_stack', 'language_rules', 'framework_rules', 'testing_rules', 'quality_style_rules', 'workflow_rules', 'critical_dont_miss_rules']
status: 'complete'
rule_count: 76
optimized_for_llm: true
existing_patterns_found: 8
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- Use .NET SDK `10.0.103` from `global.json`; roll forward only to latest patch.
- C# uses `LangVersion=latest`, nullable enabled, implicit usings enabled, and `TreatWarningsAsErrors=true` from `Directory.Build.props`.
- Package versions are centrally managed in `Directory.Packages.props`; do not add inline `Version=` attributes to project `PackageReference`s.
- Roslyn SourceTools target `netstandard2.0`, set `IsRoslynComponent=true`, and must use Microsoft.CodeAnalysis `4.12.0` with analyzer dependencies marked `PrivateAssets="all"`.
- Contracts multi-target `net10.0;netstandard2.0`; anything unavailable on `netstandard2.0` must be guarded or isolated.
- Shell and Razor/test helper projects target `net10.0` and use `Microsoft.NET.Sdk.Razor` where components are involved.
- Fluent UI Blazor is pinned to `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`; treat Fluent UI API usage as RC-sensitive.
- Fluxor is pinned to `Fluxor.Blazor.Web` `6.9.0`; generated actions/state must align with Shell reducers and features.
- MCP uses `ModelContextProtocol.AspNetCore` `1.2.0`; MCP tool/resource contracts must stay schema-driven and tenant-aware.
- Aspire AppHost uses `Aspire.Hosting.AppHost` `13.2.1` for samples and local orchestration.
- Tests use xUnit v3 packages, bUnit `2.7.2`, Shouldly, NSubstitute, Verify.XunitV3, FsCheck.Xunit.v3, PactNet, and BenchmarkDotNet.
- Playwright E2E lives under `tests/e2e`, requires Node `>=24.0.0`, Playwright `^1.49.0`, TypeScript `^5.6.0`, and `@axe-core/playwright`.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`.

## Critical Implementation Rules

### Language-Specific Rules

- Use file-scoped namespaces and keep `using` directives outside namespaces; `.editorconfig` warns on violations.
- Interfaces must use `I` prefix; private fields use `_camelCase`; async methods end with `Async`.
- Treat nullable annotations as contract surface. Do not silence nullable warnings with `!` unless the invariant is obvious and local.
- Keep SourceTools logic split into parse, transform, and emit stages. Prefer pure transform functions returning models over generator side effects.
- Source generator code must preserve incremental behavior; use Roslyn incremental APIs such as `ForAttributeWithMetadataName` and avoid reflection or broad syntax scans.
- Analyzer/source-generator diagnostics must use existing `HFC` IDs and update `AnalyzerReleases.*.md`, diagnostic docs, and registry tests when adding or changing IDs.
- Public contracts should remain low-dependency and cross-target safe. Guard `net10.0`-only APIs with target framework checks when code also compiles for `netstandard2.0`.
- Generated C# and Razor-related output must be deterministic, snapshot-testable, and avoid environment-specific paths or timestamps.
- CLI and evidence output must sanitize local absolute paths, raw payloads, tokens, tenant/user identifiers, and unbounded logs.

### Framework-Specific Rules

- Blazor Auto is a first-class constraint. Components and services must tolerate prerender, Server circuit lifetime, WASM lifetime, reconnect, and state handoff.
- Do not access browser-only storage directly during prerender. Use the project storage abstractions and test doubles instead.
- Shell UI should use Fluent UI Blazor v5 components and existing Shell components before adding custom UI. Treat Fluent UI APIs as RC-sensitive and keep customization minimal.
- Fluxor state is organized by feature folders under `src/Hexalith.FrontComposer.Shell/State`; generated actions/state must match the Shell reducer/effect conventions.
- Command lifecycle behavior must preserve the five-state model and existing diagnostics. Runtime failures should surface bounded diagnostics or fallback UI, not silent failures.
- EventStore communication has two channels: REST for commands/queries and SignalR for projection change nudges. A nudge triggers re-query; do not treat SignalR payloads as source-of-truth projection data.
- Tenant and user context must flow through commands, queries, SignalR subscriptions, MCP visibility, cache keys, and evidence. Cross-tenant visibility is a security bug.
- MCP commands/resources must be generated or registry-backed from typed descriptors. Reject unknown tools at the contract boundary with suggestions rather than accepting free-form names.
- Customization follows the existing gradient: annotations, templates, slots, view overrides, then full replacements. Do not skip directly to hard-coded custom behavior when a registry extension point exists.
- Accessibility is part of the framework contract. Generated/customized UI must preserve labels, keyboard reachability, focus visibility, live-region parity, reduced-motion, and forced-colors behavior.

### Testing Rules

- Mirror production boundaries: Contracts tests in `tests/Hexalith.FrontComposer.Contracts.Tests`, Shell/bUnit tests in `tests/Hexalith.FrontComposer.Shell.Tests`, SourceTools tests in `tests/Hexalith.FrontComposer.SourceTools.Tests`, MCP tests in `tests/Hexalith.FrontComposer.Mcp.Tests`.
- SourceTools changes need focused parse/transform/emit coverage. Prefer pure model assertions for transforms and Verify snapshots for generated output.
- New parser field-type support must extend the existing field type coverage expectations, including nullable variants where relevant.
- bUnit tests should use existing test infrastructure and storage/service doubles; do not rely on real browser storage or previous-test state.
- Use Shouldly/NSubstitute consistently with existing tests. Avoid introducing a new assertion or mocking library.
- Playwright specs must use `data-testid` contracts or accessible role/label selectors. Do not use CSS class selectors or arbitrary text selectors for framework behavior.
- E2E tests should wait for observable state, not time. Avoid committed `waitForTimeout`.
- Accessibility checks use `@axe-core/playwright` and should surface the full violation set with soft assertions where existing helpers do.
- Quarantined tests require `[Trait("Category", "Quarantined")]` plus the required `frontcomposer-quarantine` metadata comment.
- Performance, nightly property, visual, palette, and quarantined tests are filtered out of the main blocking lane unless a task explicitly targets those lanes.
- Submodule test suites under `Hexalith.EventStore/**` and `Hexalith.Tenants/**` are out of scope unless the user explicitly asks to work inside those submodules.

### Code Quality & Style Rules

- Keep line endings CRLF, UTF-8, four-space indentation, trimmed trailing whitespace, and final newlines.
- Prefer existing project/folder patterns over new abstractions. Add abstractions only when they match established boundaries or remove real duplication.
- Keep `Hexalith.FrontComposer.Contracts` stable and low-dependency; changes there are high blast-radius and should include contract tests.
- Keep SourceTools dependency direction clean: parser/transform/emitter code may reference Contracts, but generated-facing contracts should not depend on Shell runtime behavior.
- Do not bypass central package management. Add or update package versions only in `Directory.Packages.props`.
- Preserve root detection and dependency import behavior in `Directory.Build.props`, `deps.local.props`, and `deps.nuget.props`.
- Public package projects should maintain API compatibility artifacts such as `PublicAPI.Shipped.txt` and package validation expectations.
- Diagnostic messages should follow the project style: what happened, expected behavior, actual result, fix, and docs link where applicable.
- When adding diagnostics, update code, tests, generated docs under `docs/diagnostics`, and any registry/manifests that validate diagnostic completeness.
- Generated output should carry existing generated markers, stable names, deterministic ordering, and no local machine paths.
- Comments should explain non-obvious invariants or story/ADR context. Do not add narration that restates the code.

### Development Workflow Rules

- Default validation is `dotnet test`; use narrower project tests when the change is scoped.
- For main-lane CI parity, use `dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
- Playwright E2E commands run from the root via `npm --prefix tests/e2e ...`; the E2E web server defaults to `http://127.0.0.1:5070`.
- Do not run recursive submodule initialization or updates. Only root-level submodules may be initialized/updated unless nested submodules are explicitly requested.
- Treat `Hexalith.EventStore` and `Hexalith.Tenants` as separate roots. Do not mix their generated artifacts, tests, or package governance into FrontComposer changes unless the task says so.
- Commit messages are governed by commitlint conventional commits; use conventional commit style when preparing commits.
- Release work is evidence-driven and fail-closed: package inventory, symbols, SBOM/checksums/signatures, benchmark evidence, commit/tag/run metadata, and release manifest checks must stay synchronized.
- Generated docs site artifacts under `docs/_site` should only be updated intentionally as part of docs generation or docs validation work.
- Avoid broad generated-artifact churn. Scope edits to source, tests, and docs required by the task.
- When modifying GitHub workflows or governance scripts, preserve bounded artifact output and redaction rules.

### Critical Don't-Miss Rules

- Do not silently skip source-generator inputs. Emit generated output or an explicit `HFC` diagnostic.
- Do not use runtime reflection as the primary discovery mechanism for generated contracts when compile-time descriptors or registries already exist.
- Do not put tenant, user, command payload, bearer token, local absolute path, or raw log content into public evidence, diagnostics, docs, snapshots, or benchmark artifacts.
- Do not treat SignalR projection nudges as durable data. Re-query REST with the proper tenant/user/cache context.
- Do not add direct Redis, Kafka, Postgres, CosmosDB, or other infrastructure coupling to framework code. DAPR is the permitted infrastructure abstraction where infrastructure access is needed.
- Do not introduce browser-storage coupling into components or services that can run during Blazor prerender.
- Do not change `Contracts` APIs, schema fingerprints, MCP descriptors, or generated manifests without updating compatibility tests and downstream consumers.
- Do not create new diagnostic IDs without docs and release/registry coverage.
- Do not update Fluent UI, Fluxor, Roslyn, xUnit, or Playwright versions casually; each has compatibility implications in this repo.
- Do not create E2E tests that depend on CSS classes, sleeps, previous-test state, or non-contract text.
- Do not normalize away accessibility behavior when customizing generated UI; labels, keyboard, focus, live-region, reduced-motion, and forced-colors behavior are contractual.
- Do not initialize or update nested submodules recursively unless the user explicitly asks for nested submodules.

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any code.
- Follow all rules exactly as documented.
- When in doubt, prefer the more restrictive option.
- Update this file if new non-obvious project patterns emerge.

**For Humans:**

- Keep this file lean and focused on agent needs.
- Update it when technology stack or project conventions change.
- Review periodically for outdated rules.
- Remove rules that become obvious over time.

Last Updated: 2026-05-10
