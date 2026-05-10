# FrontComposer Diagnostic Registry

Story 9-4 makes `diagnostic-registry.json` the authoritative source for HFC ownership, lifecycle, canonical help links, release-row metadata, runtime channel severity, and deprecation linkage. The validation entry point is the `DiagnosticRegistryTests` suite in `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics`, which loads this registry, analyzer descriptors, runtime constants, release rows, docs stubs, obsolete attributes, submodule boundaries, and compatibility evidence from checked-in files only.

The supported registry schema is exactly `1.0`. Newer or malformed schema versions fail closed with a named unsupported-schema category. Diagnostics docs slugs must remain `diagnostics/HFCxxxx`; encoded traversal, case variants, query strings, fragments, backslashes, whitespace, and zero-width characters are invalid.

Normal CI must not call live package feeds, GitHub, or the public docs site to validate this contract. Package/API compatibility gates use .NET package validation / ApiCompat for packable FrontComposer packages, with `FrontComposerPackageValidationBaselineVersion` defined in `Directory.Build.props` and applied conditionally in `Directory.Build.targets` only when `EnableFrontComposerPackageValidation=true` is set (default off). Suppression evidence is checked in at `compatibility-suppressions.json`. Reports are normalized so unavailable network/cache state cannot silently skip diagnostics governance.

The `samples/` folder contains stable blocking-report examples for registry drift, docs-stub drift, release-row drift, and compatibility drift. These examples intentionally avoid timestamps, absolute paths, machine names, SDK banners, and live feed URLs.

## Sample placeholder convention

Sample drift reports must not reference real package names, real source paths, or real HFC IDs that have semantic meaning in the production registry. Use these canonical placeholders (left-as-is when the validator emits its own report; the samples document the *shape*, not the content):

| Placeholder | Use in samples |
| --- | --- |
| `<example-package>` | Stand-in for a packable FrontComposer package id |
| `<example-tfm>` | Stand-in for a target framework moniker |
| `<example-assembly-path>` | Stand-in for a repo-relative current-assembly path |
| `<example-baseline-version>` | Stand-in for a package validation baseline version |
| `<redacted-source-path>` | Stand-in for any source-file path |
| `HFC-PLACEHOLDER` | Stand-in for an HFC ID that does not collide with a real registry entry |

Real HFC IDs may appear in samples only when the sample is illustrating a behavior that is bound to that specific ID's documented semantics (e.g., a docs-stub drift sample naming the registry id whose stub is missing). When in doubt, prefer the placeholder.

## Runtime-only vs. release-tracked diagnostics

Each diagnostic in the registry sets `releaseRow` to one of:

- `runtime-only` — emitted exclusively by runtime channels (logger templates, runtime exceptions, MCP/CLI payloads, panel models). The diagnostic does **not** appear in any `AnalyzerReleases.Unshipped.md` / `.Shipped.md` file because there is no Roslyn analyzer descriptor backing it.
- A repository-relative path to an `AnalyzerReleases.Unshipped.md` file — the diagnostic is emitted by a Roslyn analyzer descriptor in that project, and the row in the analyzer-releases file is authoritative for `category` and `severity` columns. The same diagnostic must also have a non-null `compilerSeverity` in the registry.

Mixed channels (analyzer-emitted + runtime-emitted under the same id) require both: a registry `compilerSeverity` and a non-`runtime-only` `releaseRow`. The non-compiler channel severities (`runtimeLogLevel`, `panelSeverity`) carry their own values and may differ; AC22 treats severity drift between channels as an analyzer-compatibility change requiring a `lifecycleNote`.

## Migration ID convention

`migrationId` is the registry pointer used by Story 9-2 (CLI `migrate`) and Story 9-5 (DocFX migration pages) to resolve the canonical migration page for a deprecated diagnostic or API. Two shapes are supported:

- `migrationId == id` — the diagnostic page **is** the migration page (the "obsolete API X, replaced by Y" stub doubles as the migration story). This is the default for simple field-level deprecations.
- `migrationId == "HFCMxxxx"` (or another distinct id) — the migration story is a separate page (Story 9-2's `HFCM*` namespace, or another diagnostic's docs slug). Used when the migration spans multiple APIs, requires multi-step upgrade actions, or carries security/compliance prose that does not belong in the diagnostic stub.

`migrationId` must always resolve to an existing registry entry's `docsSlug`. Self-reference is permitted; pointing to a non-existent or removed id is a registry validation failure.

## Cross-package range exceptions

`ranges` defines exact numeric ID ownership: Contracts 1-999, SourceTools 1000-1999, Shell 2000-2999, EventStore 3000-3999, Mcp 4000-4999, Aspire 5000-5999. Every diagnostic id must fall inside its `ownerPackage`'s declared range *unless* it is listed in the top-level `allowedExceptions.crossPackageRange` array with package, numeric-range owner, reason, and approval version. Exceptions are reviewed at registry time and must be motivated by stable-public-id constraints (e.g., already-shipped diagnostic ids consumed by external telemetry or adopter SIEM rules).
