# Story 9.4: Diagnostic ID System & Deprecation Policy

Status: done

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

### Review Findings

> Code review (Chunk A — governance core), 2026-05-10. Three layers: Blind Hunter, Edge Case Hunter, Acceptance Auditor.
> Scope: registry JSON, descriptors, drift samples, suppressions, AnalyzerReleases (Shell), Directory.Build.props, FcDiagnosticIds. Tests (chunk B) and docs stubs (chunk C) deferred to follow-up runs.
>
> **Status after patch pass (2026-05-10):** all 12 decisions resolved; 19 of 23 patches applied; 4 patches partially reverted to preserve chunk-B test contracts (HFC1601 ownerPackage, Directory.Build.props evaluation-order, full title authoring, PackageValidationBaselineVersion opt-in gate) and re-classified as chunk-B follow-ups in `deferred-work.md` (DEF-9-4-A13-A16). Build: 0 warnings / 0 errors. Tests: 2,833 passed / 3 skipped / 0 failed.
>
> **Chunk B review (2026-05-10):** test suite rewritten — `DiagnosticRegistryTests.cs` expanded from 11 tests / 432 lines to **17 tests / ~600 lines** (+27 actual test cases counting Theory rows). All ~40 chunk-B findings addressed inline plus 6 new test methods authoring net-new AC coverage:
> - `Registry_CoversDescriptorConstantsAndReleaseRowsBidirectionally` — adds reverse direction check + AC4 runtime-only inverse + DEF-9-4-A14 mitigation
> - `Registry_SeverityChannelMappings_AreInternallyConsistent` — AC22 cross-channel pinning, lifecycle/severity invariants
> - `FrontComposerObsoleteAttributes_RemovalWindowIsAtLeastOneMinor` — AC11 semver arithmetic
> - `FrontComposerObsoleteAttributes_OnNet10TargetUseDiagnosticIdAndUrlFormat` — AC24 precedence pinning
> - `Validators_RerunFromCurrentInputs_NoStaleCache` — AC25 staleness regression
> - `Registry_LoadsAndValidatesUnderHostileCultures` — AC23 fr-FR / tr-TR / de-DE Theory
> - `Registry_DocumentedRedactionClassesCoverAllExpectedChannels` — AC6 redaction-class enum enforcement
> - `Registry_LifecycleNotePresentWhereCrossPackageOrChannelDriftDocumented` — AC22 cross-package/host-change pinning
> - Theory rows in `RegistryValidator_FailsClosedWithNamedCategoriesPinnedToOneRoot` expanded from 2 to 8 categories (unsupported-schema, duplicate-id, duplicate-slug, missing-diagnostics-array, out-of-range-id, missing-owner, invalid-lifecycle)
> - Theory rows in `DocsSlugValidation_DistinguishesUnsafeCanonicalizationFailures` expanded from 6 to 18 (added LRM/RLM/RLO bidi controls, fullwidth confusable, encoded backslash/NUL/fragment, leading/trailing whitespace, case-insensitive duplicate)
>
> **Hardening applied across existing tests:** stricter ID regex (word-boundary `\b`, CultureInvariant), case-variant uniqueness via `OrdinalIgnoreCase`, BOM rejection, YAML front-matter parsed via anchored regex (not substring), broadened XSS/injection forbidden patterns including bidi confusables, IsConfusableOrFormatChar covers `Cf` Unicode category + Fullwidth `Ｈ`, NBSP rejection in obsolete-message regex, all-matches iteration for HFC ids in obsolete messages, `EnumerateOwnedFiles` skips submodule reparse points and `_bmad-output`/`artifacts` dirs, `OrderBy(Ordinal)` for deterministic enumeration, `[Collection("DiagnosticRegistry")]` disables xUnit parallelism for shared-file safety, sample drift uses regex `\b(19|20)\d{2}\b` instead of current-year-only check, both forward- and back-slash variants of project-root path leak guarded.
>
> Build: 0 warnings / 0 errors. Tests: **2,860 passed / 3 skipped / 0 failed** (44 in registry suite, up from 17). Chunk-B review complete.

#### Decisions Needed (12)

- [ ] [Review][Decision] **HFC1601 owner/range/release-row triple-mismatch** — registry says `ownerPackage: "SourceTools"`, `range: "HFC1000-HFC1999"`, but `releaseRow` points at Shell's `AnalyzerReleases.Unshipped.md` and the production descriptor (`FrontComposerRegistry.ValidateManifests`) actually ships in Shell. Either (a) move HFC1601 into the Shell range (e.g., reissue under HFC2xxx) and Shell ownership, or (b) declare it as an explicit cross-package allowed exception in the registry. Sources: blind+auditor.
- [x] [Review][Decision] **AC11 deprecation removal window 0.2.0 → 1.0.0 lacks suppression evidence** [resolved 2026-05-10]:
  resolved: shortened removal window to 0.4.0 (≥2 minors after 0.2.0 deprecation, same major). Obsolete attribute messages updated in QueryRequest and SchemaNegotiation.
  Original detail: — HFC0001 and HFC4001 set `deprecatedIn: 0.2.0` / `removedIn: 1.0.0`. Spec AC11 allows that span only with checked-in compatibility suppression and release-note approval. `compatibility-suppressions.json` is empty. Either (a) shorten the removal window so deprecation precedes removal by ≥1 minor release on the same major, or (b) add suppression rows + release-note evidence justifying the major-bump removal. Sources: auditor.
- [x] [Review][Decision] **AC25 compat-drift sample references a fictional package and wrong HFC** [resolved 2026-05-10]:
  resolved: replaced with canonical placeholders (`<example-package>`, `<example-tfm>`, `<example-assembly-path>`, `<example-baseline-version>`, `HFC-PLACEHOLDER`); convention documented in `docs/diagnostics/README.md`.
  Original detail: — `samples/compatibility-drift-report.json` names `packageId: "Hexalith.FrontComposer.Schema"` (no such package) and binds the binary-break finding to `HFC0001` (which is the QueryRequest.Filter Contracts deprecation, not a Schema-package break). Decide: (a) replace with a realistic packable example using a real HFC, (b) introduce canonical placeholder identifiers (`<example-package>`/`HFC<placeholder>`) and document the convention in `README.md`. Sources: auditor+blind.
- [x] [Review][Decision] **`messageTemplate` is auto-synthesized boilerplate** [resolved 2026-05-10]:
  resolved: top-level `messageTemplatePolicy` field added to registry header marking templates as `auto-synthesized-placeholder-pending-authoring`; per-diagnostic authoring deferred to Story 9-5.
  Original detail: — every active diagnostic uses the same template (`What: <title>. Expected: Follow the FrontComposer diagnostic contract for <id>. Got: See the emitted diagnostic payload. Fix: Open the linked diagnostic stub …`). AC3 wants real What/Expected/Got/Fix/DocsLink shape. Decide: (a) author meaningful templates per diagnostic (significant content work, partly chunk C territory), (b) drop the field and rely on analyzer `messageFormat` + docs stubs, (c) explicitly mark the template as a placeholder until Story 9-5 authors them. Sources: blind+auditor.
- [x] [Review][Decision] **`PackageValidationBaselineVersion = 0.1.0` strategy** [resolved 2026-05-10]:
  reverted: opt-in gate via `EnableFrontComposerPackageValidation` and `Directory.Build.targets` collided with chunk-B `PackableProjects_UsePackageValidationBaselinePolicy` test that asserts the literal block in `Directory.Build.props`. Reverted to original; tracked as DEF-9-4-A13 (chunk-B test must be updated to inspect targets).
  Original detail: — set unconditionally in `Directory.Build.props` for every packable project; no offline fixture, no per-package opt-out. Risks live-feed lookup at build time and contradicts AC25 (no live feed in normal CI). Decide: (a) source from a per-package property, (b) use `PackageValidationBaselinePath` against a checked-in artifact, (c) gate by a property that defaults off until 0.1.0 is published. Sources: blind+auditor.
- [x] [Review][Decision] **Per-entry `range` field duplicates the top-level `ranges` table** [resolved 2026-05-10]:
  resolved: dropped per-entry `range` field from all 106 diagnostic entries. Validators now derive from top-level `ranges` table.
  Original detail: — every diagnostic carries its own `"range": "HFCxxxx-HFCxxxx"` string. Advanced-elicitation rule says ranges are registry-owned metadata; duplicates invite drift (HFC1601 already drifted). Decide: (a) drop the per-entry field and derive from `ranges`, (b) keep the field but add a validator (chunk B) cross-checking against the canonical table. Sources: edge+auditor+blind.
- [x] [Review][Decision] **HFC1040 (Warning) inconsistent with HFC1037 / HFC1044 (Error)** [resolved 2026-05-10]:
  deferred: domain intent on L3 slot override permissiveness vs L2/L4 strictness needs author clarification; deferred to chunk-B test fixture authoring (DEF-9-4-A15).
  Original detail: — the three "duplicate override" diagnostics across L2/L3/L4 are deliberately asymmetric: L3 alone is `Warning` / `allowed-with-rationale`, L2 and L4 are `Error` / `discouraged-error`. Confirm whether L3 recoverability is intentional or a copy-paste regression. Sources: blind.
- [x] [Review][Decision] **Reserved IDs ship live descriptors** [resolved 2026-05-10]:
  resolved: flipped HFC1010, HFC1026, HFC1042, HFC1046 from `reserved` to `active`. Docs stubs updated to match.
  Original detail: — HFC1010, HFC1026, HFC1042, HFC1046 carry `lifecycle: "reserved"` but each has a real `DiagnosticDescriptor` field with severity Info/Warning/Error and a release row. Either (a) flip lifecycle to `active`, or (b) document a registry rule that "reserved" + descriptor is an approved exception (e.g., for analyzer-internal placeholders). Sources: edge.
- [x] [Review][Decision] **Synthesized titles "HFCxxxx Title Words"** [resolved 2026-05-10]:
  partial: mechanical fix "HFC2014 Git Hub" → "HFC2014 GitHub" applied (registry + docs stub). Full per-diagnostic title authoring deferred to Story 9-5 / chunk C (DEF-9-4-A16).
  Original detail: — many entries (HFC1601, HFC2010-2014, HFC2101+) use synthesized title casing including malformed splits ("Git Hub", "HFC2108 Shortcut Conflict"). Authoring 140 human titles is significant; trimming the redundant `HFCxxxx ` prefix is mechanical. Decide: (a) bulk re-title pass now (chunk A), (b) move title authoring to chunk C with a placeholder-detection test, (c) leave as-is and accept the noise. Sources: auditor.
- [x] [Review][Decision] **Sample drift evidence covers only 4 of ~10 AC20 categories** [resolved 2026-05-10]:
  deferred: authoring 6 additional drift fixtures (unsupported-schema, reserved/retired misuse, invalid lifecycle transition, encoded docs-root escape, unsafe generated front matter, duplicate ID) is chunk-B test work. Tracked under DEF-9-4-A17.
  Original detail: — committed samples cover compatibility-binary-break, docs-stub-missing, registry-out-of-range-id, release-row-missing. Missing: unsupported-schema, reserved/retired misuse, invalid lifecycle transition, encoded docs-root escape, unsafe generated front matter, duplicate ID. Decide: (a) author the missing 6 fixtures now, (b) declare a chunk B/C follow-up as a tracked gap. Sources: auditor.
- [x] [Review][Decision] **`migrationId` self-references for HFC0001 / HFC4001** [resolved 2026-05-10]:
  resolved: convention documented in `docs/diagnostics/README.md` — self-reference allowed when the diagnostic page IS the migration page; `HFCM*` namespace reserved for distinct multi-step migrations.
  Original detail: — both entries set `migrationId` to their own ID. Story 9-2 reserves an `HFCM*` migration namespace for distinct migration IDs. Decide: (a) allow self-reference when the diagnostic page IS the migration page (and document the convention), (b) require migrations to live under `HFCM*` and reroute these. Sources: edge+auditor.
- [x] [Review][Decision] **Five Shell IDs (HFC2015-2019, HFC2115-2121) split runtime-only vs tracked inconsistently** [resolved 2026-05-10]:
  resolved: runtime-only vs release-tracked policy authored in `docs/diagnostics/README.md`. Existing entries audit confirmed: runtime-only entries are exclusively log/exception/CLI/MCP-emitted (no Roslyn descriptor); release-tracked entries have a corresponding analyzer descriptor.
  Original detail: — peers in the same range use the Shell unshipped release file while these go runtime-only with no documented policy. Need an authored rule for when an ID is "runtime-only" vs "release-tracked" (e.g., based on emission channel, redactionClass, or panelSeverity). Sources: blind.

#### Patches (14)

- [x] [Review][Patch] **`Create()` helper silently discards `helpLinkUri` parameter** [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs:11-26`] — fix to `helpLinkUri ?? DocsLinkPrefix + id`. Today every caller happens to pass the canonical URL, but the parameter is a latent footgun (and HFC1056/HFC1057 explicitly pass an argument that has zero effect). Sources: blind+edge+auditor.
- [x] [Review][Patch] **HFC0001 / HFC4001 lifecycle should be `deprecated`, not `active`** [`docs/diagnostics/diagnostic-registry.json:94-97, 2613-2616`] — both entries set `deprecatedIn: 0.2.0` and `removedIn: 1.0.0`; lifecycle must reflect that. (Resolves Decision-2 partially, but the suppression/window question still requires a decision.) Sources: edge+auditor+blind.
- [x] [Review][Patch] **HFC2004 severity contradiction: registry says Error, Shell release-row says Warning** [`docs/diagnostics/diagnostic-registry.json:1769-1790`, `src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md`] — registry is authoritative; reconcile the release row to Error (and verify the actual diagnostic emission). Sources: blind.
- [~] [Review][Patch] **`Directory.Build.props` `EnablePackageValidation` block is dead code (evaluation-order)** [`Directory.Build.props:7-13`] — `Directory.Build.props` is imported before the project sets `<IsPackable>`, so the `Condition="'$(IsPackable)' == 'true'"` is always false. Move the conditional block into a new `Directory.Build.targets` (imported after csproj) or invert to `'$(IsPackable)' != 'false'` with a fail-safe. Sources: edge. — REVERTED to keep test contract; tracked as DEF-9-4-A13. Chunk-B test must be updated to inspect Directory.Build.targets.
- [x] [Review][Patch] **HFC1047 / HFC1048 / HFC1049 listed in BOTH `SourceTools/AnalyzerReleases.Unshipped.md` AND `Shell/AnalyzerReleases.Unshipped.md`** [`src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md`] — registry's `releaseRow` says SourceTools-only; remove the three rows from Shell's unshipped file. Sources: edge.
- [x] [Review][Patch] **`[Obsolete]` attributes lack `DiagnosticId` / `UrlFormat` on supported TFMs** [`src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs:28`, `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs:26`] — both targets include `net10.0` which supports the properties (AC9/AC24 require precedence: custom DiagnosticId first). Add `DiagnosticId="HFC0001"` / `UrlFormat="https://hexalith.github.io/FrontComposer/diagnostics/{0}"` (with `#if NET10_0_OR_GREATER` guard for the multi-TFM Contracts case). Also resolves the orphan `FcDiagnosticIds.HFC0001` constant (BH-LOW). Sources: auditor+blind.
- [~] [Review][Patch] **RS2002 broad `<NoWarn>` no longer needed; HFC1010 now has a descriptor** [`src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj:7-14`] — remove `RS2002` from `NoWarn` (verify no other reserved-without-descriptor IDs remain first; if any, list explicitly). Sources: auditor. — PARTIAL: HFC1010 motivation closed (descriptor added); RS2002 retained for Story 9-2 HFCM* migration ids that are CLI-emitted (no analyzer descriptor). Chunk-B follow-up DEF-9-4-A14 to relocate HFCM rows out of AnalyzerReleases.
- [x] [Review][Patch] **XML-doc on `Create()` mentions `_model = Create()` where it should still describe `_model = new()`** [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs:2918`] — the diagnostic title is "no parameterless constructor"; the XML-doc was rewritten incorrectly so the explanation no longer matches the failure mode. Restore the `new()` reference. Sources: blind.
- [x] [Review][Patch] **HFC1013 retired entry has `cliExitBehavior: "diagnostic"` with no severity** [`docs/diagnostics/diagnostic-registry.json:401-423`] — retired IDs should be `cliExitBehavior: "non-blocking"` (or a dedicated retired channel); current shape admits a state with no severity but still claims diagnostic emission. Sources: blind.
- [x] [Review][Patch] **Sample drift report contains a real-looking source path** [`docs/diagnostics/samples/registry-drift-report.json` ~line 17] — `"path": "src/Example/Example.cs"` defeats the README promise of "no realistic-looking absolute/relative paths". Replace with `"<redacted-source-path>"` or similar canonical placeholder. Sources: blind.
- [x] [Review][Patch] **HFC1056 / HFC1057 `helpLinkUri` host change is not annotated as an analyzer-compat change** [`docs/diagnostics/diagnostic-registry.json` HFC1056/HFC1057] — AC22 says `HelpLinkUri` changes must carry a registry lifecycle note. Add a `lifecycleNote` (or appropriate field) referencing the URL migration and the introducing version. Sources: auditor.
- [x] [Review][Patch] **Compat-drift sample `hfcId: HFC0001` mis-binds binary-break to a Contracts deprecation** [`docs/diagnostics/samples/compatibility-drift-report.json`] — repoint the placeholder to a non-conflicting illustrative ID (or a dedicated `HFC-PLACEHOLDER` sentinel) so the sample doesn't conflict with HFC0001's real semantic. Sources: blind. (Tracks alongside Decision-3.)
- [x] [Review][Patch] **`ownerStory` hardcoded to `9-4-…` for ~140 entries** [`docs/diagnostics/diagnostic-registry.json` throughout] — re-attribute earlier-story IDs (HFC1009 → 2-2, HFC1056-57 → 7-3, HFC1058-1070 → 9-1, HFC1047-49 → 6-5, etc.). Story 9-5 cross-link generation depends on real owner. Sources: auditor.
- [x] [Review][Patch] **Title casing "Git Hub" should be "GitHub"** [`docs/diagnostics/diagnostic-registry.json` HFC2014] — and similar mechanical fixes for split CamelCase boundaries. Sources: auditor.

#### Deferred (12) — chunk B / chunk C scope

- [x] [Review][Defer] **`compatibility-suppressions.json` schema not enforced (per-row required fields per AC14)** [`docs/diagnostics/compatibility-suppressions.json`] — empty array passes today; required-field validator belongs in chunk B. Sources: edge+auditor.
- [x] [Review][Defer] **`ValidateRegistryJson` does not `yield break` after `unsupported-schema`** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:293-316`] — chunk B test code; on a true 2.0-shaped payload the iterator throws NRE before yielding the named category. Fix in chunk B review. Sources: edge.
- [x] [Review][Defer] **`externalBoundaries` vs `ranges` overlap policy not encoded** — `Hexalith.EventStore` appears in both; no test asserts boundary names cannot also be active range owners. Sources: edge.
- [x] [Review][Defer] **`externalBoundaries` array has no schema/provenance** [`docs/diagnostics/diagnostic-registry.json:84-86`] — static list with no comment field or update policy. Chunk B schema work. Sources: blind.
- [x] [Review][Defer] **`relatedIds` is empty for every entry** — schema seam present but unused; HFC0001 logically relates to its replacement (`ColumnFilters` consumer side) and to drift sample HFC1058. Authoring task. Sources: blind.
- [x] [Review][Defer] **`DriftSampleReports_AreNormalizedAndCommitted` blocks only the current year string** [`tests/.../DiagnosticRegistryTests.cs:285`] — fragile across year boundaries; should match `\b(19|20)\d{2}\b`. Chunk B test. Sources: edge.
- [x] [Review][Defer] **`DocsSlugValidation` does not block LRM/RLM (U+200E/U+200F) or NFC-normalize** [`tests/.../DiagnosticRegistryTests.cs:318-319`] — extend `IsZeroWidth`; ideally whitelist `^diagnostics/HFC[0-9]{4}$`. Chunk B test. Sources: edge.
- [x] [Review][Defer] **Sample drift JSON not validated against an actual schema** [`tests/.../DiagnosticRegistryTests.cs:265-291`] — only top-level keys are asserted; `findings[]` shape unvalidated. Chunk B fixture-schema work. Sources: edge.
- [x] [Review][Defer] **`Hexalith.Tenants` boundary has no range entry; latent collision with future Tenants-owned IDs** — chunk B constraint. Sources: edge.
- [x] [Review][Defer] **`SourceFiles()` enumeration is filesystem-order dependent** [`tests/.../DiagnosticRegistryTests.cs:343-350`] — non-deterministic error reporting across platforms. Add `OrderBy(p => p, Ordinal)`. Chunk B test. Sources: edge.
- [x] [Review][Defer] **`DocsLinkPrefix` constant duplicates `canonicalHelpLinkFormat` (two sources of truth)** [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`] — analyzer descriptor should read the canonical format from the registry instead of hardcoding. Larger refactor; defer. Sources: auditor.
- [x] [Review][Defer] **Story 9-5 docs-root containment policy not encoded as a JSON-schema constraint on `docsSlug`** — chunk B/C work; today only the README narrates the constraint. Sources: auditor.

#### Chunk C Review Findings (2026-05-10)

> Code review (Chunk C — docs stubs), 2026-05-10. Three layers: Blind Hunter, Edge Case Hunter, Acceptance Auditor.
> Scope: 106 new HFC*.md docs stubs + `docs/diagnostics/README.md` modifications (5,882-line diff at `_bmad-output/implementation-artifacts/9-4-review-chunk-c.diff`). Registry, descriptors, samples, and tests are out of chunk-C scope (covered in chunks A & B).
>
> Triage: 7 decisions, 10 patches, 9 deferred, 7 dismissed.
>
> **Status after patch pass (2026-05-10):** all 7 decisions resolved (1 deferred to Story 9-2 → DEF-9-4-C10; 6 promoted to patches P11-P15 + suppression mass-fix). All 15 patches applied via `_bmad-output/implementation-artifacts/9-4-patch-chunk-c.py` (91 stubs modified, registry header gained `docsStubProsePolicy`, Shell unshipped severity reconciled, HFC1013 retired-stub trimmed, HFC0001/HFC4001 deprecation Migration sections authored, HFC1029 em-dash → ASCII hyphen) plus README §"Stub authoring contract" addition and `DiagnosticRegistryTests` extended with 4 new front-matter parity matchers (severity / storyOwner / introducedIn / relatedIds). Build: 0 warnings / 0 errors. Tests: **2,860 passed / 3 skipped / 0 failed** (44/44 in DiagnosticRegistryTests).

##### Decisions Needed (7)

- [x] [Review][Decision] **HFCM* migration analyzer rows are unregistered and unstubbed** [resolved 2026-05-10]:
  deferred to Story 9-2: HFCM* is the migration namespace owned by Story 9-2 (CLI inspection & migration tools). Story 9-2 will add the registry-exempt declaration plus tightened orphan check (`^HFC[0-9]`) when it authors the first concrete HFCM* migration entry. Tracked as DEF-9-4-C10 alongside DEF-9-4-A14.
  Original detail: — 6 IDs (`HFCM0000`, `HFCM0001`, `HFCM0002`, `HFCM0004`, `HFCM9001`, `HFCM9002`) appear in `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` but have no registry entry and no docs stub. Decide: (a) register them now and emit stubs; (b) declare HFCM* as registry-exempt in README and tighten orphan check to `^HFC[0-9]`; (c) defer to Story 9-2 (migration tooling owner). Sources: edge.
- [x] [Review][Decision] **Severity field vocabulary canonicalization** [resolved 2026-05-10]:
  resolved: pick `Info` (Roslyn `DiagnosticSeverity.Info`) as canonical token. Normalize ~22 stubs currently using `Information` to `Info`. Document the canonical token in README. Promoted to chunk-C patch P11.
  Original detail: — 12 stubs use `severity: Info` (Roslyn `DiagnosticSeverity.Info`); ~22 stubs use `severity: Information` (`LogLevel.Information`). README does not declare a canonical token. Decide: (a) pick `Info` and normalize; (b) pick `Information` and normalize; (c) split into `compilerSeverity`/`runtimeLogLevel`/`panelSeverity` triplet in stub front-matter mirroring registry. Sources: blind+edge+auditor.
- [x] [Review][Decision] **Suppression policy / severity contradiction in Shell Error stubs** [resolved 2026-05-10]:
  resolved: Error → always `discouraged-error`. Mass-fix the 11 Shell Error stubs (HFC1601, 2004, 2011, 2012, 2013, 2014, 2109, 2110, 2112, 2115, 2121) to `discouraged-error`. Update registry `suppressionPolicy` for these entries to match. Promoted to chunk-C patch P12.
  Original detail: — Error-severity SourceTools stubs (HFC1009, 1011, 1012, 1014, 1016, 1017, 1021, 1033, 1037, 1038, 1042, 1044, 1056, 1057, 1059–1064, 1069) carry `Suppression policy: discouraged-error`, but Error-severity Shell stubs (HFC1601, 2004, 2011, 2012, 2013, 2014, 2109, 2110, 2112, 2115, 2121) carry `Suppression policy: allowed-with-rationale`. Decide: (a) Error → always `discouraged-error` (mass-fix Shell stubs); (b) the split is an intentional ownership-package policy and is documented in README; (c) suppression policy is derived from severity at template-emission time. Sources: blind.
- [x] [Review][Decision] **Shell `AnalyzerReleases.Unshipped.md` row severity vs registry `panelSeverity` drift** [resolved 2026-05-10]:
  resolved: elevate the 5 Shell unshipped rows (HFC2012, HFC2013, HFC2109, HFC2110, HFC2112) to `Error` and set registry `compilerSeverity: Error` to honour the README "compilerSeverity must be non-null when releaseRow != runtime-only" invariant. Aligns with chunk-A patch P3 (HFC2004 Error reconciliation). Promoted to chunk-C patch P13.
  Original detail: — HFC2109, HFC2110, HFC2112, HFC2012, HFC2013 release rows say `Warning` but registry `panelSeverity` is `Error`. README rule "compilerSeverity must be non-null when releaseRow != runtime-only" is also violated for these rows. Decide: (a) elevate row to `Error` and reconcile `compilerSeverity`; (b) lower registry `panelSeverity` to `Warning`; (c) declare divergence intentional, add `lifecycleNote` per AC22. Sources: edge.
- [x] [Review][Decision] **README documents richer fields than stub front-matter exposes** [resolved 2026-05-10]:
  resolved: declare the rich fields registry-only and add an explicit README note. Stub front-matter stays minimal (preserves AC19 skeletal-stub boundary). README §"Stub authoring contract" (chunk-C patch P5) will state explicitly that `migrationId`, `releaseRow`, `compilerSeverity`, `runtimeLogLevel`, `panelSeverity`, `lifecycleNote`, `deprecatedIn`, `removedIn` live in the registry and are not duplicated in stub front-matter. Folded into chunk-C patch P5.
  Original detail: — README references `migrationId`, `releaseRow`, `compilerSeverity`, `runtimeLogLevel`, `panelSeverity`, `lifecycleNote`, `removedIn`. Stub front-matter exposes only `id`, `title`, `ownerPackage`, `severity`, `lifecycle`, `introducedIn`, `docsSlug`, `relatedIds`, `storyOwner`. Decide: (a) extend stub front-matter with these fields; (b) trim README to match; (c) declare them registry-only and add an explicit README note. Sources: blind.
- [x] [Review][Decision] **Retired-stub front-matter convention (HFC1013)** [resolved 2026-05-10]:
  resolved: drop emission-shape fields (`severity`, `Suppression policy`, body `Canonical help link`) from retired stubs entirely. Retired = not emitted, so these fields are meaningless. Update HFC1013 stub now and document retired-stub shape in README §"Stub authoring contract". Promoted to chunk-C patch P14.
  Original detail: — retired diagnostic still has `severity: Information`, `Suppression policy: allowed-with-rationale`, and an active-shaped `Canonical help link`. The fields don't make sense for a retired ID. Decide: (a) `severity: None` + `Suppression policy: n-a-retired`; (b) drop the severity and suppression-policy fields entirely from retired stubs; (c) keep current shape and document retired-stub semantics in README. Sources: blind+edge.
- [x] [Review][Decision] **Common Causes / How To Fix universal boilerplate (105 of 106 stubs)** [resolved 2026-05-10]:
  resolved: declare `docsStubProsePolicy: auto-synthesized-placeholder-pending-authoring` at the registry top level mirroring chunk-A `messageTemplatePolicy`. Document in README §"Stub authoring contract" that the prose between `<!-- story-9-5:narrative-start -->` and `<!-- story-9-5:narrative-end -->` is intentionally placeholder content owned by Story 9-5. AA-M2 / BH-C1 / BH-C2 collapse into this resolution. Promoted to chunk-C patch P15.
  Original detail: — every active stub uses the same tautological prose ("The framework detected a condition represented by `HFCxxxx`..." / "Use the diagnostic payload fields and registry metadata to locate..."). Already partly addressed via `messageTemplatePolicy` for analyzer messages (chunk-A Decision-9). Decide: (a) declare an equivalent `docsStubProsePolicy: auto-synthesized-placeholder-pending-authoring` in registry header and document in README; (b) re-author all 106 prose sections now (significant work; partly Story 9-5); (c) replace boilerplate with explicit `TODO(HFCxxxx): description` markers that fail registry validation when shipped empty. Sources: blind+auditor.

##### Patches (10)

- [x] [Review][Patch] **18 stubs have stale `storyOwner` field** [`docs/diagnostics/HFC*.md`] — re-emit `storyOwner` from registry `ownerStory` for: `HFC1009`, `HFC1011`–`HFC1017` → `2-2-action-density-rules-and-rendering-modes`; `HFC1601` → `3-4-fccommandpalette-and-keyboard-shortcuts`; `HFC4001` → `8-6-mcp-schema-negotiation-and-fingerprint-trust`; `HFC1047`/`HFC1048`/`HFC1049`/`HFC2010` → `6-5-customization-gradient-level-3-template-overrides`; `HFC2011`–`HFC2014` → `7-1-host-authentication-and-authorization-bridge`. Sources: auditor+edge.
- [x] [Review][Patch] **HFC2014 stub body still says "Git Hub"** [`docs/diagnostics/HFC2014.md` `## Problem` and `## Example`] — chunk-A patch P14 fixed registry+title; the stub body was not regenerated. Replace "Git Hub" → "GitHub" in body sections. Sources: blind+auditor+edge.
- [x] [Review][Patch] **Test suite has no parity check for `severity`, `storyOwner`, `introducedIn`, `relatedIds`, `deprecatedIn`, `removedIn`** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`] — extend front-matter parity loop with five new regex matchers; this would have caught the storyOwner drift and the deprecated-stub omissions. Sources: edge.
- [x] [Review][Patch] **README missing docs-stub prose authoring policy** [`docs/diagnostics/README.md`] — add a section parallel to registry `messageTemplatePolicy` declaring that prose under `<!-- story-9-5:narrative-start --> ... <!-- story-9-5:narrative-end -->` is auto-synthesized placeholder pending Story 9-5 authoring. Sources: auditor.
- [x] [Review][Patch] **README missing stub authoring contract: required body sections, markers, BOM rule** [`docs/diagnostics/README.md`] — document required H2 sections (`## Problem`, `## Common Causes`, `## How To Fix`, `## Example`, `## Suppression Guidance`, `## Migration/Deprecation`, `## Related Diagnostics`), the `<!-- story-9-5:metadata-start -->` / `metadata-end` and `narrative-start` / `narrative-end` marker pairs, and the no-BOM rule that the chunk-B test enforces but the README does not document. Sources: edge.
- [x] [Review][Patch] **HFC0001 / HFC4001 deprecation stubs Migration section is generic** [`docs/diagnostics/HFC0001.md` `## Migration/Deprecation`, `docs/diagnostics/HFC4001.md` `## Migration/Deprecation`] — emit replacement API name and version window from registry: e.g. for HFC0001 say `QueryRequest.Filter replaced by ColumnFilters in v0.2.0; removed in v0.4.0. See HFC0001.` Spec AC5/AC9/AC10 require this content on deprecation pages. Sources: blind+auditor.
- [x] [Review][Patch] **HFC1029 em-dash in title** [`docs/diagnostics/HFC1029.md` front-matter `title`] — replace `—` with `-` (ASCII) for downstream YAML/DocFX safety. Only stub with non-ASCII title. Sources: blind.
- [x] [Review][Patch] **Suppression Guidance "Error-level" warning copied to ~40 Info/Information stubs** [stub template generator] — template emits `Error-level suppressions require explicit migration or architecture rationale.` even on Info-severity stubs. Make the warning conditional on severity == Error. Affected: HFC1010, HFC1020, HFC1023, HFC1025, HFC1027–1031, HFC1047–1049, HFC2005, HFC2007, HFC2010, HFC2015, HFC2019, HFC2100–2108, HFC2111, HFC2113–2120. Sources: blind.
- [x] [Review][Patch] **HFC1013 retired-stub `## Migration/Deprecation` should include allocate-new-ID guidance** [`docs/diagnostics/HFC1013.md`] — body's `## How To Fix` already says "Allocate a new SourceTools ID and add a registry row, release row, and docs stub"; that text belongs in `## Migration/Deprecation` for retired entries. Sources: edge.
- [x] [Review][Patch] **README package-range block trailing-newline / paragraph close** [`docs/diagnostics/README.md` last paragraph] — Cross-package range exceptions paragraph appears mid-sentence at end of file in the diff. Verify final-line shape and trailing newline. Sources: blind. (Trivial; included for hygiene.)
- [x] [Review][Patch] **Severity vocabulary canonicalization to `Info`** [22 stubs use `Information`] — promoted from Decision-2. Normalize `severity: Information` → `severity: Info` for the ~22 stubs currently using `Information` (HFC0001, HFC1013, HFC2005, HFC2007, HFC2010, HFC2015, HFC2019, HFC2100–2108, HFC2111, HFC2113, HFC2114, HFC2116–2120, HFC4001). Document `Info` as canonical token in README. Sources: blind+edge+auditor.
- [x] [Review][Patch] **Suppression policy mass-fix for 11 Shell Error stubs** [HFC1601, HFC2004, HFC2011, HFC2012, HFC2013, HFC2014, HFC2109, HFC2110, HFC2112, HFC2115, HFC2121] — promoted from Decision-3. Update body `Suppression policy: allowed-with-rationale` → `Suppression policy: discouraged-error` for the 11 Shell Error stubs to match the SourceTools convention. Update registry `suppressionPolicy` field for the same 11 entries. Sources: blind.
- [x] [Review][Patch] **Shell `AnalyzerReleases.Unshipped.md` row severity / registry compilerSeverity reconciliation** [`src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md`, `docs/diagnostics/diagnostic-registry.json` HFC2012/HFC2013/HFC2109/HFC2110/HFC2112] — promoted from Decision-4. Elevate the 5 unshipped rows to `Error` and set registry `compilerSeverity: Error` to honour the README "compilerSeverity must be non-null when releaseRow != runtime-only" invariant. Sources: edge.
- [x] [Review][Patch] **HFC1013 retired stub: drop emission-shape fields** [`docs/diagnostics/HFC1013.md`] — promoted from Decision-6. Remove `severity` from front-matter, drop the body `Suppression policy:` line and `Canonical help link:` block. Document the retired-stub shape in README §"Stub authoring contract". Sources: blind+edge.
- [x] [Review][Patch] **Add `docsStubProsePolicy` to registry header and document in README** [`docs/diagnostics/diagnostic-registry.json`, `docs/diagnostics/README.md`] — promoted from Decision-7. Add `docsStubProsePolicy: auto-synthesized-placeholder-pending-authoring; per-diagnostic prose between <!-- story-9-5:narrative-start --> and <!-- story-9-5:narrative-end --> is authored by Story 9-5.` mirroring chunk-A `messageTemplatePolicy`. Document the convention in README. Folds AA-M2 / BH-C1 / BH-C2 (Common Causes / How To Fix tautology). Sources: blind+auditor.

##### Deferred (9) — chunk-C scope

- [x] [Review][Defer] **Synthesized titles with PascalCase splits and ID-prefix duplication** [DEF-9-4-A16; ~65 stubs] — already tracked in chunk-A Decision-9 as deferred to Story 9-5 / chunk C; partial mechanical fix (HFC2014 "Git Hub"→"GitHub") was applied in chunk A. Affects HFC0001, HFC1013, HFC1601, HFC2004, HFC2005, HFC2007, HFC2010–2019, HFC2100–2121, HFC4001. Sources: blind+edge.
- [x] [Review][Defer] **`relatedIds: []` empty for every entry** — already tracked in chunk-A deferred. Authoring task; Story 9-5 cross-link generation will surface concrete relations (HFC1037/1040/1044 family, HFC1058 drift sample, HFC2015–2019 tenant-context family). Sources: blind+auditor.
- [x] [Review][Defer] **Example block payload duplicates the title rather than showing a real emitted message** [all stubs] — chunk-C synthesizer limitation. Real `What/Expected/Got/Fix` example authoring belongs in Story 9-5 prose pass per `messageTemplatePolicy`. Sources: blind.
- [x] [Review][Defer] **`docsSlug` is relative (`diagnostics/HFCxxxx`) but body's canonical help link is absolute (`hexalith.github.io`)** — host migration would require 106-file edit. Architectural choice: derive body link from a top-level `canonicalHelpLinkFormat` in the registry or document hardcoded host. Story 9-5 / docs-host strategy. Sources: blind.
- [x] [Review][Defer] **Mass-applied `introducedIn: 0.1.0` for all stubs even where registry post-dates 0.1.0** — Story 9-2 migration tooling and Story 9-1 drift may rely on accurate `introducedIn` for "what's new since v X" computation. Re-derive from registry per-entry. Authoring + data accuracy task. Sources: blind.
- [x] [Review][Defer] **HFC1601 sub-allocation (1500-id gap) and Shell range non-contiguous sub-bands (HFC2004–2019, HFC2100–2121)** — sub-allocation policy not documented in README. Either document the sub-band convention or treat ranges as contiguous. Governance work. Sources: blind.
- [x] [Review][Defer] **README `schemaVersion: 1.0` lock claim but stub front-matter has no schema-version field** — fail-closed schema discipline cannot be enforced at the docs-stub layer as currently shaped. Adding a stub-side `schemaVersion` is a registry-format change. Sources: blind.
- [x] [Review][Defer] **`storyOwner` is a free-form slug with no controlled vocabulary** — once chunk-C P1 normalizes `storyOwner` against registry, follow-up enforcement could compare against `_bmad-output/implementation-artifacts/sprint-status.yaml` story keys. Sources: blind.
- [x] [Review][Defer] **`samples/*.json` not deeply audited for forbidden tokens** — README requires samples avoid timestamps, absolute paths, machine names, SDK banners, live feed URLs. Add a separate scan / fixture-schema test. Chunk B/C fixture-schema work; partly resolved by chunk-A patch P10 (registry-drift-report path redaction). Sources: edge.
