# Story 9.4: Diagnostic ID System & Deprecation Policy

Status: review

> **Epic 9** - Developer Tooling & Documentation. Covers **FR66**, **FR67**, **NFR69**, **NFR76**, **NFR77**, and **NFR80**. Builds on Stories **9-1** through **9-3**, the existing HFC diagnostic catalog, runtime diagnostic constants, and the package train governance needed before public v1 docs. Applies lessons **L01**, **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 9-4 turns HFC diagnostics and deprecations into a governed framework contract:

- Publish one package-range ownership table for HFC0001-HFC5999 and enforce it in tests.
- Create a source-controlled diagnostic registry that binds every HFC ID to owner package, severity, lifecycle state, docs slug, message template, release note row, and migration/deprecation link when applicable.
- Require all analyzer descriptors and runtime diagnostics to resolve to canonical documentation pages.
- Standardize `DiagnosticDescriptor.HelpLinkUri`, message shape, docs slugs, and runtime log/docs-link payloads.
- Add deprecation policy checks for `[Obsolete]` messages, custom diagnostic IDs, `UrlFormat`, minimum one-minor-version removal windows, migration links, and release-train evidence.
- Add binary compatibility gates with package validation / API compatibility plus explicit suppression review for intentional major-version breaks.
- Keep DocFX site generation and full public prose pages in Story 9-5, while Story 9-4 creates the authoritative metadata and stub pages that Story 9-5 publishes.

---

## Story

As a developer,
I want every framework diagnostic to resolve to a documentation page, and deprecated APIs to have clear migration paths,
so that I can self-service resolve any issue and plan upgrades without surprises.

### Adopter Job To Preserve

An adopter should be able to see any `HFCxxxx` analyzer diagnostic, runtime diagnostic panel, CLI migration finding, IDE squiggle, or `[Obsolete]` warning and immediately know what happened, what was expected, how to fix it, whether the API or behavior is deprecated, and which version window applies. The framework must not reuse IDs, publish dead docs links, or remove public APIs inside a minor release without explicit compatibility evidence.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | The diagnostic ID range policy exists | IDs are assigned or validated | Reserved ranges are enforced exactly: Contracts HFC0001-0999, SourceTools HFC1000-1999, Shell HFC2000-2999, EventStore HFC3000-3999, Mcp HFC4000-4999, Aspire HFC5000-5999. |
| AC2 | Any HFC ID is introduced | The diagnostic registry validation runs | The ID is unique across all packages, belongs to the owning package range, is never reused for a different meaning, and has one lifecycle state: reserved, active, deprecated, retired, or removed-in-major. |
| AC3 | Any analyzer `DiagnosticDescriptor` is declared | Build/test validation runs | The descriptor has a matching `FcDiagnosticIds` constant or approved generator-only exception, a registry row, a `HelpLinkUri` using the canonical diagnostics URL, a non-empty title, category `HexalithFrontComposer`, stable severity, and message text shaped as What / Expected / Got / Fix / DocsLink or equivalent structured parameters. |
| AC4 | Any runtime diagnostic ID is logged, rendered, thrown, or included in MCP/CLI output | Diagnostic validation runs | The ID has a registry row, package owner, docs slug, severity semantics, redaction classification, and a canonical docs link. Runtime-only diagnostics must not require Roslyn analyzer release rows unless the registry says they are analyzer-emitted. |
| AC5 | A diagnostic docs page is required | Registry validation runs | A lookup-addressable stub exists at the canonical slug and includes at least: ID, title, owner package, severity, problem, common causes, resolution steps, code/config example placeholder, related diagnostics, migration/deprecation section if applicable, and generated metadata markers for Story 9-5. |
| AC6 | A diagnostic includes user-controlled values, local paths, source snippets, tenant/user/policy names, CLI output, IDE output, or runtime exception data | Messages, properties, logs, docs examples, JSON, Markdown, or generated issue bodies are rendered | Values are bounded, normalized, and sanitized with deterministic culture-invariant formatting, stable key ordering, a documented maximum length, and a visible truncation marker. Diagnostics never leak tokens, tenant IDs, user IDs, claims, raw command payloads, ETags, absolute user paths, machine names, package feed credentials, raw exception dumps, terminal control sequences, or Markdown/HTML/script injection. |
| AC7 | SourceTools diagnostics are validated | The current catalog is scanned | Existing `DiagnosticDescriptors`, `FcDiagnosticIds`, and `AnalyzerReleases.Unshipped.md` are reconciled through a deterministic table covering each known gap. The HFC1010/RS2002 gap is converted into an explicit allowed exception or fixed by targeted catalog tests so future descriptor/release-note drift fails with a named failure category. |
| AC8 | Shell, EventStore, Mcp, Aspire, or Contracts diagnostics are validated | The registry scan runs | Runtime constants, logger templates, exception messages, diagnostic panels, MCP error payloads, and CLI findings are checked against package range ownership without recursively initializing or scanning nested submodules. Root-level submodule boundaries are read only to exclude external ownership from in-repo HFC validation, and tests fail if excluded submodule content is scanned as FrontComposer-owned diagnostics. |
| AC9 | A framework API is deprecated | The deprecation is applied | `[Obsolete]` uses a message shaped as `<old> replaced by <new> in v<target>. See HFC<id>. Removed in v<removal>.`, carries a FrontComposer HFC diagnostic ID where target frameworks support custom IDs, and carries a URL format or docs link to the diagnostic/migration page where supported. The precedence is custom diagnostic ID first, then message HFC ID, then URL-derived ID only as an older-TFM fallback with a recorded fallback reason. |
| AC10 | A deprecation has no direct replacement | The deprecation is applied | The message says `No direct replacement` and links to a migration page explaining manual action, risk, and supported removal version. It still must satisfy the minimum window and registry rules. |
| AC11 | A deprecated API is scheduled for removal | Release validation runs | The removal version is at least one minor version after the first released deprecation unless the current version is a major bump with an explicit compatibility suppression and release note. |
| AC12 | A public API is removed, signature-changed, moved, made less visible, or behaviorally broken inside a minor train | Package/API compatibility validation runs | CI fails by default. Intentional breaks require a major version, checked-in compatibility suppression evidence, registry migration entry, and release-note approval. |
| AC13 | PublicApiAnalyzers or package validation baselines are introduced | CI runs | Public API compatibility is guarded for all packable FrontComposer packages and supported TFMs. The implementation may use .NET package validation / ApiCompat and/or PublicAPI files, but one authoritative baseline path must be documented and tested. |
| AC14 | A compatibility suppression is checked in | Validation runs | Suppressions are deterministic, reviewable, bounded to one API change, include package, TFM, old signature, new/removal state, owning HFC diagnostic or migration ID, target release, and reviewer rationale. Stale or broad suppressions fail validation. |
| AC15 | Diagnostic documentation links are generated | Link validation runs | Links are stable, lower-risk, and repo-owned. `HelpLinkUri`, runtime docs links, CLI JSON docs fields, IDE matrix references, and docs stubs all resolve to the same canonical slug for the ID. |
| AC16 | A diagnostic is renamed, severity-changed, or retired | The registry update is reviewed | Existing IDs are not reused. Severity changes require a registry lifecycle note, version effect, migration guidance if adopter behavior changes, and tests proving older docs links remain valid or redirect to the current page. |
| AC17 | Story 9-1 or 9-2 introduced provisional diagnostic/migration IDs | Story 9-4 executes | Provisional IDs are either finalized in the registry or marked reserved/deferred with owner, reason, and future story. No provisional migration ID may remain without a docs slug and migration policy owner. |
| AC18 | Story 9-3 matrix rows require diagnostic docs links | Story 9-4 completes | Matrix-facing HFC diagnostics have final docs slugs or registry-backed placeholders so IDE parity evidence can point to stable links. |
| AC19 | Story 9-5 builds the DocFX site later | Story 9-4 completes | Story 9-4 emits metadata and stub content that DocFX can consume, but it does not build the full Diataxis site, navigation IA, tutorial content, or full public docs publication. |
| AC20 | Registry, docs stubs, release rows, or compatibility reports are parsed | Malformed, duplicate, missing, path-traversing, absolute-path, unsupported URI, stale, schema-version-mismatched, confusable, encoded traversal, or out-of-range values appear | Validation fails closed with deterministic HFC diagnostics or test failures. It must distinguish duplicate ID, out-of-range ID, reserved/retired misuse, missing owner, missing release row, invalid slug, invalid lifecycle transition, unsupported schema version, docs-root escape, encoded docs-root escape, and unsafe generated front matter instead of collapsing them into one generic failure. It must not silently drop invalid rows, accept unknown future registry schema versions, or rewrite docs outside the approved docs root. |
| AC21 | A developer suppresses an HFC diagnostic | Suppression guidance is documented | The docs page explains whether suppression is allowed, the preferred `.editorconfig` or pragma shape, the risk of suppressing, and whether the diagnostic is Error/Warning/Info by default. Error suppressions require explicit migration or architecture rationale. |
| AC22 | Analyzer and runtime diagnostics have different severity channels | Registry validation runs | The registry distinguishes compiler severity, runtime log level, user-visible panel severity, CLI exit behavior, and MCP error category. The same HFC ID cannot silently mean Warning in one channel and Error in another unless the registry declares the mapping and tests cover it. Changes to severity, category, default enablement, HelpLinkUri, docs slug, or message shape must be treated as analyzer-compatibility changes with registry lifecycle notes and release evidence. |
| AC23 | Diagnostic catalog tests enumerate packages | The suite runs on Windows, Linux, or case-sensitive filesystems | Path normalization uses project-relative forward-slash paths, compares IDs ordinally, and avoids culture-sensitive sorting. IDs must canonicalize to uppercase ASCII `HFC` plus four digits; docs slugs must canonicalize to the approved lowercase/uppercase path shape without whitespace, zero-width characters, Unicode confusables, backslashes, encoded separators, or duplicate case variants. Tests do not depend on file enumeration order or local path casing. |
| AC24 | The deprecation analyzer scans `[Obsolete]` attributes | The target framework does not expose newer `ObsoleteAttribute.DiagnosticId` / `UrlFormat` properties | The analyzer falls back to validating message text and registry/docs linkage without forcing a TFM upgrade. Where the project target supports custom diagnostic IDs and URL format, validation requires them. |
| AC25 | Diagnostic registry and compatibility gates run in CI | The package feed, baseline package, cache, or network is unavailable or stale | The job fails or emits a deterministic blocking report with stable wording, stable exit code, explicit package/TFM/baseline inputs, and committed sample evidence for registry drift, docs-stub drift, release-row drift, and compatibility drift. It must not pass by silently skipping compatibility, docs-link, registry checks, or by reusing stale cached reports. Normal CI must use checked-in baselines/fixtures, not live package, GitHub, or docs availability checks. |

---

## Tasks / Subtasks

- [x] T1. Create the authoritative diagnostic registry (AC1-AC5, AC15-AC17, AC20, AC22)
  - [x] Add a source-controlled registry under a package-owned docs/diagnostics path, recommended `docs/diagnostics/diagnostic-registry.json` plus generated Markdown stubs under `docs/diagnostics/HFCxxxx.md`.
  - [x] Model top-level fields: `schemaVersion`, `ranges`, `canonicalHelpLinkFormat`, and `diagnostics`. Model diagnostic fields: `id`, `ownerPackage`, `range`, `title`, `lifecycle`, `introducedIn`, `deprecatedIn`, `removedIn`, `compilerSeverity`, `runtimeLogLevel`, `panelSeverity`, `cliExitBehavior`, `mcpCategory`, `messageTemplate`, `docsSlug`, `helpLinkUri`, `redactionClass`, `suppressionPolicy`, `releaseRow`, `migrationId`, `relatedIds`, and `ownerStory`.
  - [x] Treat the registry as the single source of truth for package ownership, API surface, ID range, lifecycle, docs slug, `HelpLinkUri`, severity/channel metadata, release row, and owner story. Every HFC ID must declare exactly one owning package and one owning API surface or be listed as an approved external/reserved exception.
  - [x] Enforce lifecycle values `reserved`, `active`, `deprecated`, `retired`, and `removed-in-major`.
  - [x] Validate `schemaVersion` exactly for the implementation-supported registry schema. Unknown newer versions must fail with an explicit unsupported-schema diagnostic instead of being partially parsed.
  - [x] Keep the package range ownership table in one source-controlled registry section and validate any duplicated constants/tests against that section so range drift cannot hide in copied tables.
  - [x] Add deterministic ordinal sorting and uniqueness validation for IDs, slugs, titles where required, and release rows.
  - [x] Preserve existing HFC IDs and explicitly document any approved exceptions such as generator-only legacy IDs or reserved placeholders.
  - [x] Document the validation entry point and deterministic failure categories before implementing docs stubs or compatibility gates.
  - [x] Do not rewrite existing generated code or runtime behavior while introducing the registry.

- [x] T2. Reconcile current diagnostic sources (AC2-AC8, AC15, AC17, AC22)
  - [x] Scan `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`.
  - [x] Scan `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`.
  - [x] Scan `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` and `.Shipped.md`.
  - [x] Scan package-specific release files such as `src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md`.
  - [x] Scan runtime templates for `{DiagnosticId}` and hardcoded `HFC` values in Shell, Contracts, SourceTools, Mcp, Aspire, and in-repo EventStore integration points.
  - [x] Treat `Hexalith.EventStore` and `Hexalith.Tenants` as root-level submodule/external boundaries unless their HFC ownership is explicitly mirrored in this repo. Do not initialize or update nested submodules.
  - [x] Map every found ID to the registry. Unknown IDs fail validation unless listed as approved external references with owner and reason.
  - [x] Add a deterministic reconciliation table for `HFC1010`, RS2002 suppression/fix rationale, release-tracking file location, descriptor output, docs stub, and expected failure category when those sources disagree.
  - [x] Reconcile the current docs-link mismatch between `https://hexalith.github.io/FrontComposer/diagnostics/HFCxxxx` and any newer `hexalith.io`/`hexalith.dev` references by choosing one canonical URL shape and adding tests.

- [x] T3. Harden analyzer descriptor validation (AC3, AC5, AC7, AC15, AC20, AC23)
  - [x] Extend `DiagnosticCatalogTests` or add a sibling suite to require registry rows for every `DiagnosticDescriptor`.
  - [x] Require `HelpLinkUri` for every new or active analyzer descriptor, with approved exceptions for legacy descriptors only if explicitly listed in the registry.
  - [x] Check category, title, default severity, docs link, message-template shape, and descriptor ID against registry metadata.
  - [x] Validate `AnalyzerReleases.Unshipped.md` / `.Shipped.md` rows match the registry for analyzer-emitted IDs.
  - [x] Replace broad reliance on RS2002 suppression with a targeted test that detects descriptor/release row drift.
  - [x] Ensure tests use ordinal/culture-invariant comparisons and stable project-relative paths.

- [x] T4. Harden runtime diagnostic validation (AC4, AC6, AC8, AC15, AC20, AC22, AC23)
  - [x] Add tests that reflect over `FcDiagnosticIds` constants and compare them to the registry.
  - [x] Add targeted text/parser tests for runtime log templates, exception messages, diagnostic panel models, MCP error payloads, and CLI output contracts that include HFC IDs.
  - [x] Require runtime entries to declare log level, user-visible severity, redaction class, docs link, suppression policy, and deterministic package/ID-range queryability.
  - [x] Prove diagnostics carrying tenant/user/path/policy/type-name/example values use sanitized placeholders or bounded project-relative forms.
  - [x] Cover null, empty, long, multiline, path-like, PII-like, control-character, high-cardinality, URL-with-query-string, culture-specific, Windows-path, POSIX-path, and submodule-path values. Assert max length, truncation marker, culture-invariant formatting, sanitized output, and stable ordering.
  - [x] Preserve existing runtime behavior except for docs-link/catalog metadata needed to make IDs resolvable.

- [x] T5. Generate diagnostic docs stubs (AC5, AC15, AC18, AC19, AC21)
  - [x] Generate or author one Markdown stub per active/reserved diagnostic under the canonical diagnostics docs root.
  - [x] Include metadata front matter: `id`, `title`, `ownerPackage`, `severity`, `lifecycle`, `introducedIn`, `docsSlug`, `relatedIds`, and `storyOwner`.
  - [x] Include sections: Problem, Common Causes, How To Fix, Example, Suppression Guidance, Migration/Deprecation, Related Diagnostics.
  - [x] Mark narrative/reference boundaries so Story 9-5 can publish DocFX human docs while MCP/agent projections can strip narrative sections later.
  - [x] Keep stubs short and factual. Full tutorials, how-to guides, navigation IA, and DocFX build remain Story 9-5.
  - [x] Add docs-link validation that every registry slug resolves to a file and no docs file has an orphan or duplicate ID.
  - [x] Validate stub presence, slug stability, front matter, and docs-root containment only. Generated front matter must escape YAML/Markdown-sensitive values, reject control characters and formula-like values where applicable, and preserve deterministic key ordering. Do not require rendered website availability, DocFX navigation, final examples, or public prose completeness.

- [x] T6. Define and enforce deprecation policy (AC9-AC11, AC16, AC21, AC24)
  - [x] Add a small SourceTools analyzer or build validation test for FrontComposer-owned `[Obsolete]` attributes.
  - [x] Validate message shape: `<old> replaced by <new> in v<target>. See HFC<id>. Removed in v<removal>.`
  - [x] Support `No direct replacement` wording when needed, but still require HFC ID, migration page, target/removal version, and owner.
  - [x] Validate `ObsoleteAttribute.DiagnosticId` and `UrlFormat` where the target framework/API surface supports them; fall back to message validation on older TFMs without forcing package TFM changes. Test the precedence matrix: custom diagnostic ID plus URL, message ID with no URL, URL with no custom ID, neither ID nor URL, malformed URL, and deprecated API moved between packages.
  - [x] Enforce at least one minor version between first deprecation release and removal unless the current change is a major version with approved compatibility evidence.
  - [x] Add tests for valid replacement, no-direct-replacement, missing ID, malformed version, removal too soon, missing docs page, unsupported TFM fallback, and custom diagnostic ID suppression behavior.

- [x] T7. Add package/API compatibility gates (AC12-AC14, AC25)
  - [x] Decide and document the authoritative compatibility mechanism: .NET package validation / ApiCompat, PublicAPI files, or a combination that fits the existing package train.
  - [x] Enable package validation for packable FrontComposer packages where feasible, with `PackageValidationBaselineVersion` or explicit baseline artifact policy.
  - [x] If PublicAPI files are used, create package-scoped `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` files and tests that prevent accidental public surface changes.
  - [x] Add compatibility suppression review rules and fail stale/broad suppressions.
  - [x] Distinguish source-compatible additions from binary-breaking changes such as removed members, changed signatures, optional parameter additions to existing public methods, narrowed visibility, TFM drops, or dependency removals.
  - [x] Add deterministic no-network/baseline-unavailable behavior for missing baseline artifact, malformed baseline, version mismatch, Roslyn analyzer package drift, stale cache, and network/feed unavailability. Normal CI uses checked-in fixture-backed baselines and fails or emits a blocking report instead of silently passing.
  - [x] Record normalized ApiCompat/package validation inputs in reports: package ID, TFM, current assembly path as repo-relative path, baseline source, baseline version, suppression file, and validator version where available. Reports must exclude timestamps, absolute paths, machine names, SDK banners, and live feed URLs.
  - [x] Commit normalized sample reports or golden snapshots for registry drift, compatibility drift, docs-stub drift, and release-row drift. Avoid whole-registry snapshots that include timestamps, absolute paths, SDK banners, or machine-specific ordering.

- [x] T8. Finalize provisional Story 9 handoffs (AC17-AC19)
  - [x] Review Story 9-1 drift diagnostics and reserve/finalize any new SourceTools HFC IDs after HFC1057.
  - [x] Review Story 9-2 migration diagnostics and reserve/finalize migration-specific HFC IDs, docs slugs, and migration page placeholders.
  - [x] Review Story 9-3 matrix docs-link needs and reserve/finalize any IDE parity diagnostic docs pages.
  - [x] Add cross-links from docs stubs to Story 9-5 follow-up sections for final publication.
  - [x] Update Known Gaps / deferred work only with story-specific owners, target story, concrete artifact, and validation trigger, not vague epic-level deferrals.

- [x] T9. Tests and verification (AC1-AC25)
  - [x] Registry schema tests for required fields, duplicate IDs/slugs, unsupported schema version, unsupported lifecycle, out-of-range owner package, invalid severity/log mappings, and missing docs files.
  - [x] Descriptor/constant/release-row reconciliation tests for SourceTools and runtime package IDs.
  - [x] Docs-link tests for `HelpLinkUri`, runtime docs links, CLI JSON docs fields, and Markdown stubs.
  - [x] Canonicalization tests for lower/upper-case variants, zero-width characters, Unicode confusables, percent-encoded traversal, backslash traversal, URL query fragments, symlink/junction docs-root escapes, and duplicate slugs on case-insensitive filesystems.
  - [x] Redaction/injection tests for diagnostics containing tenant/user/policy names, file paths, command payload-like values, exception text, Markdown/HTML/script payloads, terminal escapes, JSON-looking strings, and CSV formula payloads.
  - [x] Deprecation analyzer/tests for message shape, `DiagnosticId`, `UrlFormat`, replacement/no-replacement variants, minimum one-minor removal window, and older-TFM fallback.
  - [x] Compatibility gate tests or CI smoke proving accidental binary breaks fail and intentional major-version breaks require checked-in suppression evidence.
  - [x] Culture/path tests under `fr-FR` or `tr-TR`, case-sensitive/case-insensitive paths where feasible, and symlink/junction docs-path escape attempts.
  - [x] Submodule boundary tests using path fixtures or shallow root-level submodule fixtures only. Tests must not require `git submodule update --init --recursive`.
  - [x] Staleness tests proving validators do not reuse a previous registry/docs/compatibility report after the source registry, docs stub, release row, suppression file, or baseline fixture changes.
  - [x] Full regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.

---

## Dev Notes

### Party-Mode Clarifications

The 2026-05-04 party-mode review (Winston, Amelia, John, Murat) tightened Story 9-4 before development. Apply these decisions when implementing:

| Decision | Resolution |
| --- | --- |
| Registry contract first | Implement the versioned registry schema and validator before docs stubs, runtime metadata projection, deprecation analyzers, or compatibility gates. The registry is the single source of truth for HFC ownership and lifecycle. |
| Deterministic failure contract | Validation must emit stable categories for duplicate ID, out-of-range ID, reserved/retired misuse, missing owner, missing release row, invalid slug, invalid lifecycle transition, docs-root escape, unsanitized value, and compatibility drift. |
| Story 9-5 boundary | Story 9-4 owns slug reservation, metadata, stub presence, and docs-link validation. Story 9-5 owns DocFX navigation, rendered site, tutorials, how-to prose, final examples, and public documentation publication. |
| Deprecation fallback precedence | Prefer `ObsoleteAttribute.DiagnosticId` where supported, then parse the HFC ID from the required message, then derive from URL only as an older-TFM fallback with a recorded fallback reason. |
| Compatibility scope | Compatibility gates protect public API/deprecation/diagnostic behavior and package baselines. They must not become a general release-engine redesign or depend on live package/feed availability in normal CI. |
| Test evidence discipline | Prefer normalized fixture-backed assertions and sample blocking reports. Avoid overbroad snapshots that capture timestamps, absolute paths, SDK banners, local culture, or machine-specific ordering. |

### Advanced Elicitation Clarifications

The 2026-05-04 advanced elicitation pass focused on failure, abuse, staleness, and simplification risks after the party-mode review. Apply these additional guardrails:

| Area | Resolution |
| --- | --- |
| Registry schema versioning | The registry must declare a supported `schemaVersion`; validators fail closed on unknown newer versions instead of best-effort parsing. |
| Range-table drift | Treat package ID ranges as registry-owned metadata and validate copied constants/tests against that source so duplicated tables cannot diverge silently. |
| ID and slug canonicalization | Accept only canonical HFC IDs and approved docs slugs. Reject whitespace, zero-width characters, Unicode confusables, encoded separators, backslashes, duplicate case variants, URL fragments, and query-string tricks. |
| Generated docs safety | Generated front matter and Markdown stubs must escape or reject values that can alter YAML, Markdown, HTML, terminals, JSON, CSV, or future DocFX processing. |
| Compatibility evidence | ApiCompat/package validation reports must name normalized package, TFM, baseline, suppression, and validator inputs while excluding volatile machine-specific fields. |
| Cache and staleness | Registry, docs-stub, release-row, suppression, and compatibility validators must prove they rerun from current checked-in inputs and do not reuse stale reports. |

### Current Diagnostic State

- Architecture ADR-007 already defines HFC diagnostic policy for SourceTools: HFC1000-HFC1999, diagnostic-first generator behavior, and What / Expected / Fix / DocsLink style messages.
- Architecture also defines the package ranges:

| Range | Owner |
| --- | --- |
| HFC0001-HFC0999 | Contracts |
| HFC1000-HFC1999 | SourceTools |
| HFC2000-HFC2999 | Shell |
| HFC3000-HFC3999 | EventStore |
| HFC4000-HFC4999 | Mcp |
| HFC5000-HFC5999 | Aspire |

- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` currently contains many HFC1000-range descriptors. Only newer descriptors such as HFC1056/HFC1057 use `helpLinkUri`; older descriptors mostly embed DocsLink in message text or have no `HelpLinkUri`.
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` mixes build-time, runtime, reserved, and package-range IDs. Story 9-4 should not move constants blindly; it should introduce registry-backed validation first.
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` lists SourceTools rules through HFC1057. `src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md` lists Shell/runtime diagnostic rows. Story 9-4 should reconcile these with the registry instead of relying only on Roslyn RS2002.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticCatalogTests.cs` already checks uniqueness and HFC shape. It is the natural starting point for registry validation.
- `Hexalith.FrontComposer.SourceTools.csproj` suppresses RS2002 broadly because HFC1010 was reserved without a descriptor. Story 9-4 should close this governance gap with an explicit exception list or targeted tests.

### Deprecation and Compatibility Reality Check

- Microsoft documents `DiagnosticDescriptor.HelpLinkUri` as the descriptor field for a detailed diagnostic link. Story 9-4 should standardize that on analyzer descriptors instead of relying only on message text.
- Microsoft documents `ObsoleteAttribute` with `Message`, `IsError`, and newer `DiagnosticId` / `UrlFormat` properties. Story 9-4 must account for target frameworks where custom obsolete IDs and URL formatting are available while keeping older TFM fallback behavior explicit.
- Microsoft documents .NET package validation and API compatibility checks through `EnablePackageValidation`, `PackageValidationBaselineVersion`, `ApiCompat`, and compatibility suppression files. Story 9-4 should use these where they fit the packable FrontComposer packages, and avoid building a parallel binary-compatibility engine unless local constraints force it.
- The repository currently pins Roslyn `Microsoft.CodeAnalysis.CSharp` to `4.12.0` because higher versions risk IDE analyzer load-context breakage. Do not upgrade Roslyn broadly for this story.

### Registry Shape

Recommended initial JSON entry:

```json
{
  "id": "HFC1057",
  "ownerPackage": "SourceTools",
  "lifecycle": "active",
  "title": "Command declares duplicate authorization policies",
  "compilerSeverity": "Error",
  "runtimeLogLevel": null,
  "panelSeverity": null,
  "cliExitBehavior": "diagnostic",
  "mcpCategory": null,
  "messageTemplate": "What: ... Expected: ... Got: ... Fix: ... DocsLink: ...",
  "docsSlug": "diagnostics/HFC1057",
  "helpLinkUri": "https://hexalith.github.io/FrontComposer/diagnostics/HFC1057",
  "redactionClass": "source-metadata-only",
  "suppressionPolicy": "discouraged-error",
  "introducedIn": "0.1.0",
  "deprecatedIn": null,
  "removedIn": null,
  "ownerStory": "7-3-command-authorization-policies",
  "relatedIds": []
}
```

Final field names can differ, but the implementation must keep the registry deterministic, reviewable, and easy for Story 9-5 to transform into DocFX pages.

### Deprecation Message Contract

Required normal form:

```csharp
[Obsolete("OldApi replaced by NewApi in v1.2. See HFC0xxx. Removed in v1.3.")]
```

Required no-direct-replacement form:

```csharp
[Obsolete("OldApi has no direct replacement in v1.2. See HFC0xxx. Removed in v1.3.")]
```

When the target framework supports custom obsolete IDs and URL format, the implementation should also validate:

```csharp
DiagnosticId = "HFC0xxx"
UrlFormat = "https://hexalith.github.io/FrontComposer/diagnostics/{0}"
```

Do not force a TFM upgrade solely to get these properties. Older-target validation can still require the message text and registry/docs link.

### Compatibility Gate Boundaries

- Prefer .NET package validation / ApiCompat for binary compatibility because it catches runtime-breaking changes that source still compiles against, such as optional parameter changes on existing public methods.
- PublicAPI files are useful for PR review discipline and CS1591 scoping, but they are not a complete replacement for binary compatibility validation unless the team explicitly accepts that limitation.
- Compatibility suppressions must be checked in, narrow, and reviewed. Automatically generated broad suppressions are not acceptable without pruning.
- Story 9-4 should not redesign semantic-release or package publishing. It should add the validation metadata and CI gates needed to enforce the existing release train.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 9-1 | Story 9-4 | Drift diagnostics, new SourceTools IDs, HelpLinkUri requirements, and diagnostic property sanitization. |
| Story 9-2 | Story 9-4 | Migration diagnostics, CLI JSON docs fields, dry-run/apply migration ID linkage, and migration guide placeholders. |
| Story 9-3 | Story 9-4 | IDE parity matrix rows need stable diagnostic docs links and XML-doc quick-info links. |
| Story 9-5 | Story 9-4 | Story 9-5 consumes registry metadata and stub pages to publish DocFX documentation. |
| Stories 6-6 and 7-3 | Story 9-4 | Existing diagnostic catalog tests, HFC1050-HFC1057 descriptors, authorization diagnostics, and docs-link precedent. |
| All runtime packages | Story 9-4 | Runtime diagnostics must map to package ranges, redaction classes, docs links, and severity/channel semantics. |

### Scope Guardrails

Do not implement these in Story 9-4:

- Full DocFX documentation site, navigation, tutorials, or Diataxis IA. Owner: Story 9-5.
- Build-time drift comparison or baseline update workflow. Owner: Story 9-1 / Story 9-2.
- CLI inspect/migrate execution. Owner: Story 9-2.
- IDE conformance matrix implementation. Owner: Story 9-3.
- Visual/specimen accessibility CI gates. Owner: Story 10-2.
- Mutation testing rollout. Owner: Story 10-4.
- Recursive submodule initialization or nested submodule scans.
- Broad Roslyn package upgrades.
- New product-scope diagnostics unrelated to catalog/deprecation governance unless they are required to validate the governance itself.
- Registry browsing UI, full analyzer UX polish, historical diagnostic cleanup beyond named reconciliation gaps, or non-diagnostic package governance.
- Live package/feed/docs availability checks in normal CI.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Full public DocFX diagnostics/reference pages and site navigation. | Story 9-5 |
| Public migration guide prose for user-facing upgrade journeys. | Story 9-5 |
| Visual/specimen validation for diagnostic panels and docs screenshots. | Story 10-2 |
| Mutation testing for diagnostic registry validators and deprecation analyzer. | Story 10-4 |
| Final decision on exact compatibility mechanism if package validation cannot cover all packages. | Story 9-4 implementation decision |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-9-developer-tooling-documentation.md#Story-9.4`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/planning-artifacts/architecture.md#ADR-007-Generator-Diagnostic-Reporting-Policy`] - diagnostic policy, range ownership, and message shape.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR66`] - dedicated diagnostic ID range per package.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR67`] - deprecation with migration path via diagnostic ID.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#NFR77`] - one-minor-version deprecation window.
- [Source: `_bmad-output/implementation-artifacts/9-1-build-time-drift-detection.md`] - drift diagnostic handoff.
- [Source: `_bmad-output/implementation-artifacts/9-2-cli-inspection-and-migration-tools.md`] - migration/CLI diagnostic handoff.
- [Source: `_bmad-output/implementation-artifacts/9-3-ide-parity-and-developer-experience.md`] - IDE docs-link and diagnostic evidence handoff.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review vs. elicitation sequencing.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`] - current analyzer descriptors and `HelpLinkUri` pattern.
- [Source: `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`] - current shared HFC constants.
- [Source: `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`] - SourceTools release-row catalog.
- [Source: `src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md`] - Shell/runtime release-row catalog.
- [Source: `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticCatalogTests.cs`] - existing catalog validation starting point.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj`] - RS2002 suppression and Roslyn component constraints.
- [Source: `Directory.Packages.props`] - Roslyn 4.12.0 pin and package train constraints.
- [Source: Microsoft Learn `DiagnosticDescriptor.HelpLinkUri`](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.diagnosticdescriptor.helplinkuri) - official Roslyn docs-link property.
- [Source: Microsoft Learn `ObsoleteAttribute`](https://learn.microsoft.com/dotnet/api/system.obsoleteattribute) - deprecation attribute, message, custom diagnostic ID, and URL format behavior.
- [Source: Microsoft Learn `.NET package validation`](https://learn.microsoft.com/dotnet/fundamentals/apicompat/package-validation/overview) - baseline package validation and compatibility checks.
- [Source: Microsoft Learn `NuGet package compatibility rules`](https://learn.microsoft.com/dotnet/standard/library-guidance/nuget-package-compatibility-rules) - binary compatibility guidance for package authors.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test .\tests\Hexalith.FrontComposer.SourceTools.Tests\Hexalith.FrontComposer.SourceTools.Tests.csproj --filter FullyQualifiedName~DiagnosticRegistryTests --no-restore` - red phase failed on missing registry/evidence, then green phase passed 17/17.
- `dotnet build .\Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` - passed with 0 warnings, 0 errors.
- `dotnet test .\Hexalith.FrontComposer.sln --no-build` - passed 2,833 tests with 3 skipped, 0 failed.

### Completion Notes List

- 2026-05-04: Story created via `/bmad-create-story 9-4-diagnostic-id-system-and-deprecation-policy` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-04T18:05:56+02:00: Party-mode review completed via `/bmad-party-mode 9-4-diagnostic-id-system-and-deprecation-policy; review;`.
  - Participating BMAD agents: Winston (Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Test Architect).
  - Findings summary: registry ownership/versioning was too implicit; HFC1010/RS2002 reconciliation needed deterministic outcomes; diagnostic metadata sanitization needed hard bounds; deprecation fallback precedence needed a matrix; compatibility gates needed fixture-backed unavailable-baseline evidence; Story 9-5 documentation scope needed a sharper boundary; submodule boundary tests needed an explicit no-recursive guardrail.
  - Changes applied: tightened AC6-AC9, AC20, AC22, and AC25; added registry-first ownership, deterministic failure categories, HFC1010/RS2002 reconciliation, redaction/bounding matrix, docs-stub-only scope, deprecation fallback matrix, fixture-backed compatibility reports, story-specific handoff ownership, submodule boundary tests, Party-Mode Clarifications, and extra scope guardrails.
  - Findings deferred: full DocFX site/navigation/public prose remains Story 9-5; broad analyzer UX polish and historical cleanup beyond named gaps are out of scope; registry browsing UI and non-diagnostic package governance are out of scope; live package/feed/docs availability checks remain outside normal CI.
  - Final recommendation: ready-for-dev.
- 2026-05-04T20:03:18+02:00: Advanced elicitation completed via `/bmad-advanced-elicitation 9-4-diagnostic-id-system-and-deprecation-policy`.
  - Batch 1 methods: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation.
  - Batch 2 methods: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records.
  - Findings summary: registry schema versioning could fail open; duplicated range tables could drift; IDs/slugs needed stronger canonicalization against encoded and confusable inputs; generated front matter could become an injection channel; compatibility reports needed exact normalized inputs; validators needed stale-cache regression coverage.
  - Changes applied: tightened AC20, AC23, and AC25; added top-level registry `schemaVersion`, `ranges`, and `canonicalHelpLinkFormat`; added unsupported-schema and range-drift validation tasks; hardened docs-stub front matter requirements; added normalized ApiCompat/package-validation report inputs; expanded tests for canonicalization, docs-root escape, and stale validator output.
  - Findings deferred: future registry migration tooling and historical public docs redirects beyond current stub/link validation remain outside Story 9-4 unless implementation discovers they are required for the current acceptance criteria.
  - Final recommendation: ready-for-dev.

- 2026-05-09: Implemented Story 9-4 diagnostic governance. Added the versioned registry, canonical package range table, registry-backed docs stubs, deterministic drift/evidence samples, descriptor/runtime/deprecation/compatibility validation tests, canonical analyzer HelpLinkUri generation, runtime docs-link host reconciliation, deprecation messages with HFC migration IDs, Shell runtime release rows for HFC2004/HFC2005/HFC2007, and package validation policy for packable projects. Validation passed: focused DiagnosticRegistryTests 17/17; full solution build 0 warnings/0 errors; full solution tests 2,833 passed, 3 skipped, 0 failed.

### File List

- Directory.Build.props
- _bmad-output/implementation-artifacts/9-4-diagnostic-id-system-and-deprecation-policy.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- docs/diagnostics/HFC0001.md
- docs/diagnostics/HFC1001.md
- docs/diagnostics/HFC1002.md
- docs/diagnostics/HFC1003.md
- docs/diagnostics/HFC1004.md
- docs/diagnostics/HFC1005.md
- docs/diagnostics/HFC1006.md
- docs/diagnostics/HFC1007.md
- docs/diagnostics/HFC1008.md
- docs/diagnostics/HFC1009.md
- docs/diagnostics/HFC1010.md
- docs/diagnostics/HFC1011.md
- docs/diagnostics/HFC1012.md
- docs/diagnostics/HFC1013.md
- docs/diagnostics/HFC1014.md
- docs/diagnostics/HFC1015.md
- docs/diagnostics/HFC1016.md
- docs/diagnostics/HFC1017.md
- docs/diagnostics/HFC1020.md
- docs/diagnostics/HFC1021.md
- docs/diagnostics/HFC1022.md
- docs/diagnostics/HFC1023.md
- docs/diagnostics/HFC1024.md
- docs/diagnostics/HFC1025.md
- docs/diagnostics/HFC1026.md
- docs/diagnostics/HFC1027.md
- docs/diagnostics/HFC1028.md
- docs/diagnostics/HFC1029.md
- docs/diagnostics/HFC1030.md
- docs/diagnostics/HFC1031.md
- docs/diagnostics/HFC1032.md
- docs/diagnostics/HFC1033.md
- docs/diagnostics/HFC1034.md
- docs/diagnostics/HFC1035.md
- docs/diagnostics/HFC1036.md
- docs/diagnostics/HFC1037.md
- docs/diagnostics/HFC1038.md
- docs/diagnostics/HFC1039.md
- docs/diagnostics/HFC1040.md
- docs/diagnostics/HFC1041.md
- docs/diagnostics/HFC1042.md
- docs/diagnostics/HFC1043.md
- docs/diagnostics/HFC1044.md
- docs/diagnostics/HFC1045.md
- docs/diagnostics/HFC1046.md
- docs/diagnostics/HFC1047.md
- docs/diagnostics/HFC1048.md
- docs/diagnostics/HFC1049.md
- docs/diagnostics/HFC1050.md
- docs/diagnostics/HFC1051.md
- docs/diagnostics/HFC1052.md
- docs/diagnostics/HFC1053.md
- docs/diagnostics/HFC1054.md
- docs/diagnostics/HFC1055.md
- docs/diagnostics/HFC1056.md
- docs/diagnostics/HFC1057.md
- docs/diagnostics/HFC1058.md
- docs/diagnostics/HFC1059.md
- docs/diagnostics/HFC1060.md
- docs/diagnostics/HFC1061.md
- docs/diagnostics/HFC1062.md
- docs/diagnostics/HFC1063.md
- docs/diagnostics/HFC1064.md
- docs/diagnostics/HFC1065.md
- docs/diagnostics/HFC1066.md
- docs/diagnostics/HFC1067.md
- docs/diagnostics/HFC1068.md
- docs/diagnostics/HFC1069.md
- docs/diagnostics/HFC1070.md
- docs/diagnostics/HFC1601.md
- docs/diagnostics/HFC2004.md
- docs/diagnostics/HFC2005.md
- docs/diagnostics/HFC2007.md
- docs/diagnostics/HFC2010.md
- docs/diagnostics/HFC2011.md
- docs/diagnostics/HFC2012.md
- docs/diagnostics/HFC2013.md
- docs/diagnostics/HFC2014.md
- docs/diagnostics/HFC2015.md
- docs/diagnostics/HFC2016.md
- docs/diagnostics/HFC2017.md
- docs/diagnostics/HFC2018.md
- docs/diagnostics/HFC2019.md
- docs/diagnostics/HFC2100.md
- docs/diagnostics/HFC2101.md
- docs/diagnostics/HFC2102.md
- docs/diagnostics/HFC2103.md
- docs/diagnostics/HFC2104.md
- docs/diagnostics/HFC2105.md
- docs/diagnostics/HFC2106.md
- docs/diagnostics/HFC2107.md
- docs/diagnostics/HFC2108.md
- docs/diagnostics/HFC2109.md
- docs/diagnostics/HFC2110.md
- docs/diagnostics/HFC2111.md
- docs/diagnostics/HFC2112.md
- docs/diagnostics/HFC2113.md
- docs/diagnostics/HFC2114.md
- docs/diagnostics/HFC2115.md
- docs/diagnostics/HFC2116.md
- docs/diagnostics/HFC2117.md
- docs/diagnostics/HFC2118.md
- docs/diagnostics/HFC2119.md
- docs/diagnostics/HFC2120.md
- docs/diagnostics/HFC2121.md
- docs/diagnostics/HFC4001.md
- docs/diagnostics/README.md
- docs/diagnostics/compatibility-suppressions.json
- docs/diagnostics/diagnostic-registry.json
- docs/diagnostics/samples/compatibility-drift-report.json
- docs/diagnostics/samples/docs-stub-drift-report.json
- docs/diagnostics/samples/registry-drift-report.json
- docs/diagnostics/samples/release-row-drift-report.json
- src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs
- src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs
- src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotSelector.cs
- src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs
- src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md
- src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs
- src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs
- src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs
