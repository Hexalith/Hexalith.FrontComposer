---
project_name: 'Hexalith.FrontComposer'
user_name: 'Jerome'
date: '2026-05-10'
sections_completed: ['discovery', 'technology_stack', 'language_rules']
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
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`; do not initialize or update nested submodules unless explicitly requested.

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
