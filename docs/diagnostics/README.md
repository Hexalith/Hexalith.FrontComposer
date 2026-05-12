# FrontComposer Diagnostic Registry

Story 9-4 makes `diagnostic-registry.json` the authoritative source for HFC ownership, lifecycle, canonical help links, release-row metadata, runtime channel severity, and deprecation linkage. The validation entry point is the `DiagnosticRegistryTests` suite in `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics`, which loads this registry, analyzer descriptors, runtime constants, release rows, docs stubs, obsolete attributes, submodule boundaries, and compatibility evidence from checked-in files only.

The supported registry schema is exactly `1.0`. Newer or malformed schema versions fail closed with a named unsupported-schema category. Diagnostics docs slugs must remain `diagnostics/HFCxxxx`; encoded traversal, case variants, query strings, fragments, backslashes, whitespace, and zero-width characters are invalid.

Normal CI must not call live package feeds, GitHub, or the public docs site to validate this contract. Package/API compatibility gates use .NET package validation / ApiCompat for packable FrontComposer packages, with `FrontComposerPackageValidationBaselineVersion` defined and applied conditionally in `Directory.Build.targets` only when `EnableFrontComposerPackageValidation=true` is set (default off). Suppression evidence is checked in at `compatibility-suppressions.json`. Reports are normalized so unavailable network/cache state cannot silently skip diagnostics governance.

The `samples/` folder contains stable blocking-report examples for registry drift, docs-stub drift, release-row drift, compatibility drift, unsupported schema, reserved/retired misuse, lifecycle transition errors, encoded docs-root escape, unsafe front matter, duplicate IDs, suppression scope, and HFCM release governance. These examples intentionally avoid timestamps, absolute paths, machine names, SDK banners, and live feed URLs.

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

`migrationId` is the registry pointer used by Story 9-2 (CLI `migrate`) and Story 9-5 (DocFX migration pages) to resolve the canonical migration page for a deprecated diagnostic or API. CLI-only HFCM findings are governed by `docs/diagnostics/migration-findings.json`; they are not Roslyn `DiagnosticDescriptor` IDs and must not appear in analyzer release-tracking rows. Two registry pointer shapes are supported:

- `migrationId == id` — the diagnostic page **is** the migration page (the "obsolete API X, replaced by Y" stub doubles as the migration story). This is the default for simple field-level deprecations.
- `migrationId == "HFCMxxxx"` (or another distinct id) — the migration story is a separate page (Story 9-2's `HFCM*` namespace, or another diagnostic's docs slug). Used when the migration spans multiple APIs, requires multi-step upgrade actions, or carries security/compliance prose that does not belong in the diagnostic stub.

For `migration-findings.json`, `introducedIn` records the FrontComposer CLI/tooling release that introduced the CLI-emitted finding, not the first product release that contained the migrated API. The six initial HFCM rows are batch-approved as the Story 9-2 migration cohort and remain outside Roslyn analyzer release tracking.

`migrationId` must always resolve to an existing registry entry's `docsSlug`. Self-reference is permitted; pointing to a non-existent or removed id is a registry validation failure.

## Cross-package range exceptions

`ranges` defines exact numeric ID ownership: Contracts 1-999, SourceTools 1000-1999, Shell 2000-2999, EventStore 3000-3999, Mcp 4000-4999, Aspire 5000-5999. `externalBoundaries` is structured data, not a string list: each boundary records its package, owner, range policy, provenance, update policy, and rationale. `Hexalith.EventStore` owns its range; `Hexalith.Tenants` is a no-range-reserved external boundary until a future story allocates one. Every diagnostic id must fall inside its `ownerPackage`'s declared range *unless* it is listed in the top-level `allowedExceptions.crossPackageRange` array with package, consuming package, numeric-range owner, canonical help link, reason, approving story, and approval version. Exceptions are reviewed at registry time and must be motivated by stable-public-id constraints (e.g., already-shipped diagnostic ids consumed by external telemetry or adopter SIEM rules).

## Stub authoring contract

Every diagnostic must ship a Markdown stub at `docs/diagnostics/<id>.md`. Stubs are skeletal artefacts that DocFX consumes; full prose authoring is owned by Story 9-5 (Diataxis docs). The chunk-B test `DocsStubs_ArePresentBoundedAndRegistryBacked` enforces this contract.

### Required front-matter keys (active / reserved / deprecated lifecycles)

| Key | Source | Notes |
| --- | --- | --- |
| `id` | registry `id` | Strict shape `HFC[0-9]{4}`; case-sensitive. |
| `title` | registry `title` | Quoted; em-dashes and other non-ASCII glyphs are rejected. |
| `ownerPackage` | registry `ownerPackage` | One of `Contracts`, `SourceTools`, `Shell`, `EventStore`, `Mcp`, `Aspire`. |
| `severity` | derived (see below) | Canonical token `Info` (not `Information`), `Warning`, `Error`. Retired entries omit this field. |
| `lifecycle` | registry `lifecycle` | One of `reserved`, `active`, `deprecated`, `retired`, `removed-in-major`. |
| `introducedIn` | registry `introducedIn` | Semver string. |
| `docsSlug` | registry `docsSlug` | Always `diagnostics/<id>` (lowercase prefix, uppercase id). |
| `relatedIds` | registry `relatedIds` | Flow-style YAML array. May be empty. |
| `storyOwner` | registry `ownerStory` | Slug matching the originating story's filename stem. |

### Severity canonicalization

The stub `severity` field uses the Roslyn `DiagnosticSeverity.Info` short token (`Info`, `Warning`, `Error`). Registry channel fields use the formal names (`compilerSeverity` / `panelSeverity` / `runtimeLogLevel`) and accept the long form `Information`. The stub generator maps `Information` → `Info` so the public docs surface stays consistent. Splitting the stub field into a per-channel triplet was rejected at chunk-C review (Decision-2) to keep stubs skeletal.

### Registry-only fields (NOT duplicated in stub front-matter)

The following fields live in `diagnostic-registry.json` only. Stubs intentionally do not surface them, to keep the stub artefact bounded and to preserve Story 9-5's authoring scope:

- `migrationId`, `releaseRow`, `compilerSeverity`, `runtimeLogLevel`, `panelSeverity`, `cliExitBehavior`, `mcpCategory`, `messageTemplate`, `helpLinkUri`, `redactionClass`, `suppressionPolicy`, `lifecycleNote`, `deprecatedIn`, `removedIn`, `ownerStory` (the stub's `storyOwner` mirrors this), and `relatedIds`'s expansion.

The Story 9-5 DocFX pipeline reads these from the registry directly when rendering the public docs site.

### Required body markers and sections

- `<!-- story-9-5:metadata-start -->` … `<!-- story-9-5:metadata-end -->` — bounded metadata block consumed by DocFX. Contains the canonical help link (Markdown autolink), `Registry owner:` line, and (for non-retired entries) `Suppression policy:` line.
- `<!-- story-9-5:narrative-start -->` … `<!-- story-9-5:narrative-end -->` — bounded narrative block authored by Story 9-5. Until then, the body carries auto-synthesized placeholder prose per `docsStubProsePolicy` declared in the registry header.
- Required H2 sections (chunk-B test enforces presence): `## Problem`, `## Common Causes`, `## How To Fix`, `## Example`, `## Suppression Guidance`, `## Migration/Deprecation`, `## Related Diagnostics`.

### Retired-stub convention (`lifecycle: retired`)

Retired diagnostics are not emitted by any channel. Their stubs:

- Omit the `severity` front-matter field.
- Omit the body `Suppression policy:` line and `Canonical help link:` Markdown autolink (no help link is published for a retired ID).
- Use the `## Migration/Deprecation` section to instruct maintainers to allocate a new ID rather than reuse the retired one.
- Carry `compilerSeverity`, `runtimeLogLevel`, `panelSeverity` all `null` in the registry, and `cliExitBehavior: non-blocking`.

### Encoding / hygiene rules

- Files must be UTF-8 without BOM (chunk-B test `raw[0..3] == 0xEF 0xBB 0xBF` is rejected).
- Front-matter values must not start with `=`, `+`, or `@` (CSV/spreadsheet formula-injection guard).
- Front-matter must use the strict `^---\n` opener and the first `\n---` close delimiter.
- No zero-width / RLM / LRM / fullwidth-confusable characters in any field.

### Generated docs prose policy

Per `docsStubProsePolicy` in the registry header, the prose currently between `narrative-start` and `narrative-end` is auto-synthesized placeholder content awaiting Story 9-5 authoring. AC5 stub presence and AC15 docs-link consistency are enforced by chunk-B tests; full authoring (real `Common Causes`, real `How To Fix`, real emitted-message `Example`) is Story 9-5's responsibility.
