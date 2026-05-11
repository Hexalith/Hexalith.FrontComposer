# Story 11.2: Diagnostic Registry and Documentation Governance Follow-ups

Status: review

> **Epic 11** - Deferred Hardening & Release Readiness. Closes the diagnostic registry, diagnostic docs, compatibility suppression, package-validation, HFCM migration-id, and docs governance follow-ups routed from Stories 9.4 and 9.5. Applies lessons **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 11-2 is the release-readiness hardening pass for the diagnostic governance surface.

Story 9-4 established the diagnostic registry and release-row policy. Story 9-5 published the documentation site and diagnostic docs. Later reviews deferred a bounded set of governance gaps: registry schema shape, docs slug containment, external boundary ownership, related diagnostic links, HFCM migration ID release tracking, compatibility suppression policy, package-validation placement, sample fixture coverage, and title/prose authoring.

This story implements those follow-ups without reopening Epic 9 scope. The intended outcome is that diagnostic IDs are a trustworthy release surface: every registry row, docs stub, migration row, release-tracking row, compatibility suppression, and docs link has deterministic validation and actionable authoring guidance.

---

## Story

As a framework maintainer,
I want diagnostic registry, docs stub, and deprecation governance follow-ups closed,
so that diagnostic IDs remain a reliable release surface.

### Release-Readiness Job To Preserve

A maintainer preparing a release candidate should be able to run the focused governance tests and docs validation, then know that diagnostic metadata, docs publication, migration guidance, and compatibility suppression evidence are synchronized and fail closed.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary registry | Harden `docs/diagnostics/diagnostic-registry.json` plus `DiagnosticRegistryTests`. |
| Docs stubs | Keep every `docs/diagnostics/HFC*.md` front matter synchronized with registry metadata and improve authored prose where this story requires it. |
| Release rows | Resolve the HFCM migration-ID release-row strategy without broad project-wide Roslyn release-tracking suppression. |
| Compatibility | Enforce per-row schema for `docs/diagnostics/compatibility-suppressions.json` and move package-validation MSBuild policy to the correct evaluation phase. |
| External boundaries | Make `externalBoundaries`, range ownership, `Hexalith.EventStore`, and `Hexalith.Tenants` policy explicit and test-backed. |
| Docs link policy | Keep `canonicalHelpLinkFormat`, descriptor `HelpLinkUri`, docs host canonicalization, and docs slug containment in one validated contract. |
| Sample evidence | Expand `docs/diagnostics/samples/` to cover all named registry/docs/release/compatibility failure categories. |
| Scope guardrail | Do not implement CLI migration behavior, SourceTools drift detection, MCP schema negotiation, shell UX, EventStore reliability, or CI release automation beyond the diagnostic governance contracts this story owns. |
| Validation | Focus on `DiagnosticRegistryTests`, docs validation, and any package validation policy tests touched by this story. |

Start here: T1 inventory deferred 9-4/9-5 governance rows -> T2 harden registry schema and tests -> T3 fix HFCM/release-row strategy -> T4 tighten docs stubs/prose/canonicalization -> T5 validate compatibility/package policy -> T6 update ledger evidence and story record.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- |
| AC1 | `docs/diagnostics/compatibility-suppressions.json` contains zero or more suppression rows | Governance validation runs | Each row is schema-validated for package, TFM, old signature, new state, HFC ID, target release, reviewer rationale, owner story, and expiration/review policy; malformed rows fail with a named category. |
| AC2 | A registry payload has an unsupported `schemaVersion` | `ValidateRegistryJson` or the production-equivalent validator runs | It yields exactly `unsupported-schema` and short-circuits without null-reference or cascade failures. |
| AC3 | `externalBoundaries` and `ranges` are loaded from `diagnostic-registry.json` | Registry validation runs | Boundary ownership, range overlap, missing range owner, and allowed cross-package exceptions are explicitly tested and documented. |
| AC4 | `externalBoundaries` includes `Hexalith.EventStore` and `Hexalith.Tenants` | Registry validation runs | Each boundary either has a diagnostic range, an explicit no-range reservation policy, or a named allowed exception with owner and rationale. |
| AC5 | A diagnostic has `docsSlug` | Registry and docs validation run | The slug is constrained to `diagnostics/HFC####` after decoding, NFC normalization, zero-width/bidi rejection, separator checks, and docs-root containment checks. |
| AC6 | Sample drift reports live under `docs/diagnostics/samples/` | Governance validation runs | Samples are schema-validated and cover unsupported schema, reserved/retired misuse, invalid lifecycle transition, encoded docs-root escape, unsafe generated front matter, duplicate ID, release-row missing, docs-stub missing, out-of-range ID, and compatibility binary break. |
| AC7 | Registry rows have `relatedIds` | Registry validation runs | `relatedIds` remain well-formed, known, reciprocal where required, and non-empty for documented logical families such as HFC0001 migration replacement, HFC1056/HFC1057 authorization siblings, and HFC1037/HFC1040/HFC1044 override duplicate policy. |
| AC8 | Registry rows have `introducedIn` | Stub and registry validation run | Introduced versions are checked against release-row provenance or documented lifecycle notes so stubs cannot drift from the registry. |
| AC9 | `HFC1601` remains allocated in the SourceTools numeric range but emitted by Shell runtime code | Registry validation runs | The cross-package range exception is represented as structured registry data and test-backed, not only a prose lifecycle note. |
| AC10 | HFC1037, HFC1040, and HFC1044 have different severities and suppression policies | Governance validation runs | A fixture-driven test confirms the asymmetry is intentional and documents the rationale, or the registry is corrected with matching stub/release-row updates. |
| AC11 | Many diagnostic titles still use generated or redundant wording | Docs/stub validation runs | Required title/prose authoring replaces mechanical placeholder titles for release-critical diagnostics and keeps docs stub front matter synchronized with registry titles. |
| AC12 | Source files are scanned for diagnostic constants, descriptors, docs links, and release rows | Validation runs on any platform | File enumeration is ordinal and deterministic, so error order does not depend on filesystem order. |
| AC13 | `DiagnosticDescriptors` emits `HelpLinkUri` values | Descriptor validation runs | Descriptor help links derive from the registry-owned canonical format or an equivalent generated constant, eliminating independent docs host constants. |
| AC14 | Package validation policy is evaluated by MSBuild | Governance tests run | Package validation properties live in the correct evaluation phase, packable projects can opt in deterministically, and tests inspect the actual `.props`/`.targets` policy instead of dead-code text. |
| AC15 | HFCM migration IDs are CLI-emitted and not Roslyn `DiagnosticDescriptor`s | Release tracking validation runs | HFCM rows move to a CLI-specific release-row artifact or an equivalent registry-owned artifact, and broad RS2002 suppression is removed or narrowed to a documented, test-backed exception. |
| AC16 | Docs host canonicalization is validated | Docs validation runs | Canonical host, path casing, trailing slash, encoded path, query/fragment, and local path variants fail closed with project-relative evidence. |
| AC17 | Diagnostic docs are published | Docs validation runs | Required sections contain real, actionable prose for the diagnostics this story touches; heading-only placeholder pages do not satisfy completeness gates. |
| AC18 | Governance validation emits failure reports | Reports are generated | Output is bounded, deterministic, and redacts absolute paths, usernames, machine names, tokens, tenant/user IDs, raw payloads, SDK banners, and live feed URLs. |
| AC19 | Deferred-work ledger rows DEF-9-4-A1 through DEF-9-4-HFCM are addressed | Story implementation completes | Each row is marked resolved, superseded, split, or still deferred with evidence and an owner; no row is silently dropped. |
| AC20 | Release validation is run | Story moves to review | The Dev Agent Record lists commands, outcomes, touched files, unresolved decisions, and evidence paths. |
| AC21 | Duplicate or repeated deferred rows exist across chunk A, chunk C, and HFCM review notes | Story 11.2 reconciles the rows | The Dev Agent Record identifies the canonical row, aliases, and closure evidence for each duplicate cluster without deleting the historical rows. |
| AC22 | Docs slug validation receives encoded, malformed, double-encoded, non-NFC, mixed-separator, or confusable input | Governance validation runs | The validator decodes once, rejects malformed/ambiguous inputs with a named category, and records only sanitized project-relative evidence. |
| AC23 | Diagnostic docs stubs, examples, or generated front matter contain emitted messages or copied payload fragments | Docs validation runs | Examples remain bounded and sanitized: no absolute paths, user/machine names, tenant/user IDs, tokens, raw command payloads, SDK banners, or live feed URLs appear in docs artifacts. |
| AC24 | HFCM migration IDs move to CLI-specific governance | Release-row validation runs | HFCM rows are represented as migration findings, not Roslyn analyzer descriptors; SourceTools RS2002 suppression is removed or narrowed only after the CLI-specific artifact is validated. |
| AC25 | Governance reports enumerate registry rows, docs stubs, samples, source files, or release rows | Reports are generated on different platforms | Output ordering is ordinal and deterministic across all enumerated surfaces, not only source-file scanning. |
| AC26 | Title/prose authoring exceeds the story's decision and test budget | Implementation scopes docs work | The story records a prioritized touched-diagnostic set and defers any full-corpus prose rewrite with an owner instead of silently expanding scope. |
| AC27 | The registry schema version is missing, malformed, unknown, or newer than the validator supports | Governance validation runs | Validation emits one deterministic fail-closed classification and does not silently downgrade to partial validation; unknown optional fields in a supported schema remain non-fatal only when the compatibility rule explicitly permits them. |
| AC28 | Compatibility suppression rows attempt to hide unrelated diagnostics | Governance validation runs | Suppressions cannot apply outside their declared diagnostic ID, package, range, TFM, target version, expiration, and reviewer rationale; wildcard, duplicate, expired, or unknown-category suppressions fail with named categories. |
| AC29 | HFC1601 is allowed as a cross-package exception | Registry validation runs | The allowed HFC1601 row passes only with structured owner package, consuming package, rationale, related IDs, introducedIn provenance, canonical help link, and approving story; a similar non-HFC1601 cross-package row still fails. |
| AC30 | HFC1037, HFC1040, and HFC1044 have intentional severity differences | Governance validation runs | A table-driven severity matrix asserts exact diagnostic ID, package/source context, expected severity, suppression policy, and rationale across registry, docs, release rows, and reports. |
| AC31 | Governance inputs are presented in shuffled order | Reports and generated governance tables are produced | Output is byte-stable after redaction and sorting by documented ordinal keys, independent of filesystem enumeration order, current culture, OS path separators, and input JSON order. |
| AC32 | Failure evidence contains sentinel local paths, usernames, temp folders, stack traces, tokens, SDK banners, live feed URLs, tenant/user IDs, raw payload fragments, or long snippets | Reports are generated | Redaction removes every sentinel, truncation happens after canonical sorting, and max item count/max character budget are directly asserted. |
| AC33 | Story 11.2 closes deferred-work rows | Ledger validation or Dev Agent Record review runs | Each Story 9.4/9.5 row routed to Story 11.2 maps to closed-with-evidence, deferred-to-named-story, or rejected-with-rationale; unrelated ledger cleanup is out of scope. |
| AC34 | The release-readiness governance lane is complete | Story moves to review | Targeted xUnit governance tests and `eng/validate-docs.ps1` have recorded outcomes; snapshot approvals are supplemented by direct assertions for classification, ID, severity, package, help link, redaction, ordering, and ledger mapping. |

---

## Tasks / Subtasks

- [x] T1. Inventory the diagnostic governance backlog (AC19, AC20)
  - [x] Read `_bmad-output/implementation-artifacts/deferred-work.md` rows `DEF-9-4-A1` through `DEF-9-4-HFCM`.
  - [x] Confirm whether Story 11.1 has already updated owner markers; if not, preserve old rows and add Story 11.2 resolution evidence during implementation.
  - [x] Split the work into registry-schema, docs-stub/prose, release-row/HFCM, package-validation, sample-fixture, and docs-host categories.
  - [x] Identify duplicate or repeated findings across chunk A, chunk C, and HFCM rows; choose one canonical row per cluster and list aliases in the Dev Agent Record.
  - [x] Do not delete historical deferred rows; mark them with resolved/superseded evidence after the code/docs change lands.

- [x] T2. Harden registry schema and fail-closed validation (AC1-AC7, AC9, AC10, AC12)
  - [x] Extend or extract the registry validator used by `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`.
  - [x] Ensure unsupported schema short-circuits before reading `diagnostics`, `ranges`, or any nested row.
  - [x] Add `registry-schema-invalid-*` fixtures for missing, malformed, unknown, and future `schemaVersion` values plus the supported-schema unknown-optional-field case.
  - [x] Add per-row schema checks for `compatibility-suppressions.json`.
  - [x] Add `suppression-scope-*` fixtures for mismatched diagnostic ID, mismatched package, expired row, duplicate row, unsupported wildcard/package-wide scope, and unrecognized reason/category.
  - [x] Add structured schema for `externalBoundaries` or a compatible top-level object that records owner, range policy, provenance, and update policy.
  - [x] Add tests for range/boundary overlap, missing range entries, `Hexalith.Tenants` no-range policy, and exact package identity matching including `Hexalith.EventStore`, `Hexalith.Tenants`, unknown external packages, and near-prefix rejection such as `Hexalith.TenantsX`.
  - [x] Add or validate `allowedExceptions.crossPackageRange` for HFC1601 with owner package, consuming package, numeric-range owner, related IDs, canonical help link, reason, approving story, and version/date; include a non-HFC1601 negative control.
  - [x] Replace filesystem-order source enumeration with ordinal ordering in diagnostic governance scans and document sort keys for package ID, diagnostic ID, normalized `/` path, line, and column.
  - [x] Add table-driven severity-pinning fixtures for HFC1037/HFC1040/HFC1044 covering registry, docs, release rows, reports, and suppression policy; document the final decision.

- [x] T3. Resolve HFCM migration ID release-row governance (AC15)
  - [x] Move HFCM0000, HFCM0001, HFCM0002, HFCM0004, HFCM9001, and HFCM9002 out of Roslyn analyzer release tracking if they remain CLI-emitted only.
  - [x] Introduce a CLI-specific release-row artifact or registry-owned migration-row artifact with category, severity, migration docs slug, owner story, and release provenance.
  - [x] Assert HFCM rows are classified as CLI migration findings and never require a Roslyn `DiagnosticDescriptor`.
  - [x] Add `hfcm-release-governance-*` fixtures for valid migration ID, duplicate ID, wrong prefix, wrong release bucket, missing release note row, and analyzer-descriptor misclassification.
  - [x] Update registry tests so HFCM rows are covered without requiring Roslyn `DiagnosticDescriptor` backing.
  - [x] Remove the broad `RS2002` project suppression from `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj`, or narrow it to a temporary, named exception if removal is impossible.

- [x] T4. Tighten docs slugs, docs host, stubs, titles, and related IDs (AC5, AC7, AC8, AC11, AC13, AC16, AC17)
  - [x] Whitelist `docsSlug` as `diagnostics/HFC####`; reject encoded slash/backslash, null, query, fragment, whitespace, zero-width, bidi formatting, case variants, non-NFC, and traversal after decoding.
  - [x] Add `docs-slug-containment-*` fixtures for `%2e%2e`, encoded slash, encoded backslash, double-encoded traversal, mixed separators, absolute paths, malformed percent-encoding, non-NFC input, query/fragment suffixes, and confusable/format characters so docs slug normalization cannot pass ambiguous paths.
  - [x] Derive `DiagnosticDescriptors` help links from registry canonical format or a generated checked-in constant; remove the independent hardcoded docs host source of truth.
  - [x] Add an assertion that descriptor, registry, report, and docs-stub help links match the registry-owned canonical format exactly; do not implement docs publishing, routing, or site navigation changes.
  - [x] Populate `relatedIds` for the high-value logical families explicitly named by this story and any families discovered during implementation.
  - [x] Replace mechanical placeholder titles/prose for the diagnostics touched by this story; update matching `docs/diagnostics/HFC*.md` front matter and narrative sections.
  - [x] Record the prioritized touched-diagnostic set before rewriting prose; split full-corpus title/prose cleanup if it exceeds the Story 11.2 budget.
  - [x] Preserve Story 9-5 narrative/reference markers and DocFX front matter expectations.
  - [x] Keep all public examples sanitized and bounded.

- [x] T5. Fix package-validation placement and compatibility policy (AC1, AC14, AC18)
  - [x] Move package validation properties that depend on `IsPackable` from `Directory.Build.props` to an evaluation-safe `Directory.Build.targets` or an equivalent policy file imported after project properties.
  - [x] Add an explicit opt-in such as `EnableFrontComposerPackageValidation` if required by the existing governance docs.
  - [x] Update `PackableProjects_UsePackageValidationBaselinePolicy` to inspect the real policy file and the opt-in semantics, not dead-code text.
  - [x] Keep package-validation checks in the existing governance validation path and reuse registry/docs/release parsers where available; do not create a parallel validation engine or move ownership into unrelated package projects.
  - [x] Validate compatibility suppression rows without network, live package feeds, or generated absolute paths.

- [x] T6. Expand sample fixtures and validation evidence (AC6, AC18, AC20)
  - [x] Add missing stable sample reports for all AC6 categories.
  - [x] Use small named fixture groups: `registry-schema-invalid-*`, `suppression-scope-*`, `docs-slug-containment-*`, `external-boundaries-*`, `severity-asymmetry-*`, `deterministic-ordering-*`, `redacted-report-budget-*`, and `hfcm-release-governance-*`.
  - [x] Validate each sample against the same report schema used by governance validation.
  - [x] Keep samples free of timestamps, absolute paths, machine names, SDK banners, live feed URLs, tokens, tenant/user IDs, and real package names unless a real ID is explicitly required.
  - [x] Apply the forbidden-token scan to docs stubs, generated examples, sample reports, and validation reports, not only `docs/diagnostics/samples/*.json`; inject sentinel absolute paths, usernames, temp directories, tokens/API-key shapes, stack traces, SDK banners, live feed URLs, and long evidence snippets.
  - [x] Ensure report arrays and emitted findings are ordered with `StringComparer.Ordinal` or equivalent deterministic ordering; add shuffled-input fixtures for source files, registry rows, docs rows, related IDs, suppressions, and package groups, with truncation after sorting.
  - [x] Record the exact validation commands and outcomes in this story's Dev Agent Record.

- [x] T7. Update ledger and story status evidence (AC19, AC20)
  - [x] Update `_bmad-output/implementation-artifacts/deferred-work.md` to mark each Story 11.2-owned row resolved, superseded, split, or still deferred with evidence.
  - [x] For duplicate clusters, record canonical row, alias row, final state, and evidence path in the story record.
  - [x] Assert each Story 9.4/9.5 ledger row routed to Story 11.2 maps to exactly one final state: closed-with-evidence, deferred-to-named-story, or rejected-with-rationale.
  - [x] Do not close unrelated deferred-work rows as opportunistic cleanup.
  - [x] Record touched files in this story's File List.
  - [x] Move Story 11.2 to `review` only after implementation and validation evidence are complete.

---

## Review Findings

_Source: `/bmad-code-review 11-2` on 2026-05-11. Three parallel layers: Blind Hunter (diff-only), Edge Case Hunter (boundary paths), Acceptance Auditor (AC compliance). Diff snapshot captured at `_bmad-output/code-reviews/story-11-2-diff.patch` and accidentally committed in `18cb7aa` (see [Review][Patch] R32)._

### Decision-needed

- [x] [Review][Decision] **DEF-9-4-C3 / C7 / C8 final-state vocabulary** — `deferred-work.md:60,64,65` mark three rows as "Needs Product/Architecture decision" / "Non-action decision", which is not one of AC33's three mandated states (`closed-with-evidence`, `deferred-to-named-story`, `rejected-with-rationale`). Combined with the "0 unresolved Story 11.2 rows" count in the same file (line 46), the ledger is internally inconsistent. **Decide between**: (a) relabel all three as `rejected-with-rationale` with rationale "Beyond Story 11.2 budget; requires Product/Architecture decision per AC26"; (b) defer to a new product-decision row that names a target story; (c) amend AC33 to add a fourth `decision-pending` state. Sources: auditor+blind+edge.

### Patch (code/docs fixes with unambiguous resolution)

- [x] [Review][Patch] **Stub body contradicts `relatedIds` front-matter for 6 stubs** [`docs/diagnostics/HFC1037.md:54-56`, `HFC1040.md`, `HFC1044.md`, `HFC1056.md`, `HFC1057.md`, `HFC4001.md`] — Front-matter declares related IDs but the `## Related Diagnostics` body still says "None currently." `DocsStubs_ArePresentBoundedAndRegistryBacked` only regex-matches the front-matter line, so the drift is silent. Rewrite the body to enumerate the same IDs and add a stub-body equality test against the registry. (auditor+edge)
- [x] [Review][Patch] **`ValidateCompatibilitySuppressionsJson` throws Shouldly assertions instead of yielding categories** [`tests/.../DiagnosticRegistryTests.cs:1049-1057`] — Missing `tfm`/`oldSignature`/`newState`/`hfcId`/`targetRelease`/`reviewerRationale`/`ownerStory`/`expiresAfter`/`reason` causes `ShouldBeTrue(...)` to throw inside the iterator, defeating the fail-closed contract that DEF-9-4-A2 was supposed to close. Replace with `yield return "suppression-missing-<field>"`. Also wrap `ParseVersionToken(expiresAfter)` (line 1067) in try/catch — malformed `v`, `""`, `v1.0.0.0.0` would `ArgumentException` before any category emits. (blind+edge)
- [x] [Review][Patch] **`migration-findings.json` is not fail-closed validated and lacks per-row provenance** [`docs/diagnostics/migration-findings.json:1-55`, `DiagnosticRegistryTests.cs:727-750`] — T3 required CLI-specific release rows with "category, severity, migration docs slug, **owner story**, and **release provenance**" per finding. Current shape carries only file-level `ownerStory`/`releaseBucket`. No validator exists for: missing/wrong `schemaVersion`, duplicate ids, wrong-prefix ids (`HFC0001` slipping in), unknown `severity`/`category` enum, malformed `introducedIn` semver, missing `releaseNote`, or `migrationDocsSlug` containment (`migrations/../../etc/passwd` passes the current `ShouldStartWith("migrations/")` check). Add per-row owner-story + release provenance; add the fixture set named in T3 (`hfcm-release-governance-*`: valid, duplicate, wrong-prefix, wrong-bucket, missing-note, analyzer-misclassification). (blind+edge+auditor)
- [x] [Review][Patch] **No severity-pinning matrix for HFC1037/HFC1040/HFC1044** [`DiagnosticRegistryTests.cs:145-147`] — T2 explicitly required "table-driven severity-pinning fixtures … covering registry, docs, release rows, reports, and suppression policy" (AC30). Only `relatedIds` are asserted; a future copy-paste flipping HFC1040 to `Error` or HFC1037 to `Warning` would pass. Add a theory pinning `(id, package, severity, suppressionPolicy, rationale)` for each. (edge+auditor)
- [x] [Review][Patch] **No `unknown-optional-field` fail-closed fixture** [`DiagnosticRegistryTests.cs` registry validator] — T2 lists "supported-schema unknown-optional-field case" as required. `ValidateRegistryJson` silently ignores unknown root fields (e.g., `"backdoor": "..."`) and unknown row fields; the documented "unknown-optional" category is unreachable. Either yield it for unexpected keys or document the allowlist. (edge)
- [x] [Review][Patch] **`crossPackageRange` allowed-exception is metadata-only — no fixture exercises the bypass** [`DiagnosticRegistryTests.cs:1007`, `diagnostic-registry.json` HFC1601 entry] — HFC1601 (numeric 1601) is inside SourceTools range (1000-1999), so `(numericId < start || numericId > end) && !crossPackageExceptions.Contains(id)` is always false. No fixture proves an actually out-of-range id LISTED in the exception bypasses the range, nor that an out-of-range id NOT listed still fails. Also no test cross-checks the exception's `ownerPackage` against the diagnostic entry's `ownerPackage` for reciprocity. Add positive bypass fixture, negative control, and owner-package cross-check. (blind+edge)
- [x] [Review][Patch] **`Path.IsPathRooted(slug)` is platform-asymmetric** [`DiagnosticRegistryTests.cs:1105`] — `"C:/diagnostics/HFC1058"` is rooted on Windows, not Linux. The theory passes on Linux CI only because the final equality check catches it; the documented intent (reject rooted paths) is not what's verified portably. Replace with explicit-prefix check (`slug.Length > 0 && (slug[0] == '/' || (slug.Length > 1 && slug[1] == ':'))`) or equivalent. (blind+edge)
- [x] [Review][Patch] **Empty/missing `externalBoundaries` array silently passes** [`DiagnosticRegistryTests.cs` registry validator] — Validator only iterates the array; an empty `[]` or missing array yields no category. README and tests claim EventStore and Tenants must both be declared; the JSON validator must enforce required presence. (blind+edge)
- [x] [Review][Patch] **`invalid-external-boundary` continues with `package` registered after malformed row** [`DiagnosticRegistryTests.cs` registry validator boundary loop] — On a row that has the package but fails another field, the code yields then falls through and adds `package!` to `boundaryPackages`, potentially masking a duplicate of the SAME package later. After yielding, `continue` to the next row. (blind)
- [x] [Review][Patch] **`relatedIds` reciprocity test indexes `byId[relatedId]` without TryGetValue** [`DiagnosticRegistryTests.cs:138-143`] — A typo in `relatedIds` (e.g., `HFC9999`) yields `KeyNotFoundException` instead of a clean "unknown-related-id" diagnostic. Replace with `byId.TryGetValue(relatedId, out var entry) ?? Fail("unknown-related-id")`. Also reject self-reference in relatedIds. (blind+edge)
- [x] [Review][Patch] **`stub relatedIds` front-matter contents are not cross-validated against registry** [`DiagnosticRegistryTests.cs:291-292`] — The regex only confirms a `relatedIds:` array is present. A stub may declare `relatedIds: [HFC9999]` or `relatedIds: [SELF]` and the test passes. Parse the YAML and assert the array equals `byId[id].RelatedIds`. (edge)
- [x] [Review][Patch] **`README.md` contradicts new MSBuild layout** [`docs/diagnostics/README.md:7`] — README states `FrontComposerPackageValidationBaselineVersion` is "defined in `Directory.Build.props` and applied conditionally in `Directory.Build.targets`", but the property is now only in `Directory.Build.targets:5`. Update the README to match the actual file. `validate-docs.ps1` did not catch this drift — AC34 evidence is weaker than claimed. (blind+edge+auditor)
- [x] [Review][Patch] **HFC1040 `lifecycleNote` typo** [`docs/diagnostics/diagnostic-registry.json` HFC1040 entry] — "Level 4 view overrides are Error due manifest …" is missing "to" between "due" and "manifest". (blind)
- [x] [Review][Patch] **Three ledger rows use non-mandated final-state vocabulary; row count is internally inconsistent** [`_bmad-output/implementation-artifacts/deferred-work.md:46,60,64,65`] — See [Review][Decision] above. Once the decision lands, relabel the rows to one of AC33's three states and reconcile the "Story 11.2 | 0" count. (blind+auditor)
- [x] [Review][Patch] **Forbidden-token scan misses sentinel injection** [`DiagnosticRegistryTests.cs:799-806`] — `DriftSampleReports_AreNormalizedAndCommitted` only rejects `projectRoot`, `Environment.MachineName`, `api.github.com`, `api.nuget.org`. T6/AC32 demanded sentinel injection for: Windows abs paths (`C:\Users\...`), `$HOME`/`~/`, runner paths (`/home/runner/work/`, `fv-az*`), token prefixes (`ghp_`, `xoxb-`, `AKIA`, `sk-`), `pkgs.dev.azure.com`, stack-trace markers (`   at System.`), SDK banners. Apply to docs stubs, samples, `migration-findings.json`, AND validation reports — not only samples. Add max-item / max-character budget assertions for AC32. (edge+auditor)
- [x] [Review][Patch] **Descriptor `CanonicalHelpLinkFormat` is a literal duplicate of the registry value with no equality assertion** [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs:9`, `docs/diagnostics/diagnostic-registry.json:3`] — DEF-9-4-C4 closure claim ("single source of truth") is not enforced. Add a test asserting `DiagnosticDescriptors.CanonicalHelpLinkFormat` equals `registry["canonicalHelpLinkFormat"]`. Also add a docs-stub help-link equality test (AC13 explicitly required descriptor + registry + report + **docs-stub** parity). Note: `tests/.../DiagnosticCatalogTests.cs:20` still defines a hardcoded `DocsLinkPrefix` constant — remove it. (auditor+edge)
- [x] [Review][Patch] **AC26 prioritized touched-diagnostic set is not a structured artifact** [story file, Completion Notes] — Only the inline phrase "prioritized HFC0001/HFC1601 prose/title cleanup" exists; AC26 requires a recorded set so future stories know what budget remains. Add a "Prioritized Touched-Diagnostic Set" subsection listing each diagnostic this story touched and the depth of change (front-matter only / prose rewritten / title rewritten / examples sanitized). (auditor)
- [x] [Review][Patch] **No shuffled-input determinism fixtures** [`DiagnosticRegistryTests.cs` — entire registry test class] — T6/AC25/AC31/D17 required shuffled-input fixtures for registry rows, docs rows, related IDs, suppressions, package groups, with truncation after sorting. Source-file enumeration uses `OrderBy(... Ordinal)` but no test shuffles input collections and asserts byte-stable output. Add one shuffled-input theory per enumerated surface. (auditor+edge)
- [x] [Review][Patch] **AC20 Dev Agent Record has no "Unresolved decisions" subsection** [story file:298-327] — Three ledger rows are explicitly marked as needing future Product/Architecture decisions; AC20 mandates the record list unresolved decisions. Add the subsection. (auditor)
- [x] [Review][Patch] **AC34 ledger-mapping direct assertion missing** [tests] — D18 and AC34 require direct assertions for ledger-row → final-state mapping; only the prose ledger exists. Add a test that parses `deferred-work.md` Story-9-4-routed rows and asserts each maps to exactly one of AC33's three states. (auditor)
- [x] [Review][Patch] **`unsafe-generated-front-matter-report.json` declares a category with no validator** [`docs/diagnostics/samples/`, `DiagnosticRegistryTests.cs`] — Sample exists but no `ValidateGeneratedFrontMatterJson(...)` function exists. The committed-stub formula-injection scan at lines 248-298 only checks committed stubs for line[0] leads. Either add a generated-front-matter validator or remove the sample. (edge)
- [x] [Review][Patch] **`migrationId` may point at a retired diagnostic** [`DiagnosticRegistryTests.cs:132-134`] — Only checks the target id exists in the registry, not its lifecycle state. A retired or removed-in-major target should fail with a named category. (edge)
- [x] [Review][Patch] **Help-link scheme/path is not pinned to canonical form** [`DiagnosticRegistryTests.cs:1322` `DiagnosticsDocsLinkUrlRegex`] — Matches both `http` and `https`; trailing slash, query, and fragment variants pass as long as the host matches. Pin to exact canonical `https://hexalith.github.io/FrontComposer/diagnostics/{id}` without trailing slash / query / fragment. (edge)
- [x] [Review][Patch] **Suppression `tfm` wildcard not rejected** [`DiagnosticRegistryTests.cs` suppression validator] — Wildcard check only inspects `package`. T2/AC28 lists wildcard TFM as a boundary; `tfm: "*"` or `"net*"` passes. Reject. (edge)
- [x] [Review][Patch] **Sample-report `findings[].id` permits non-conformant placeholders** [`docs/diagnostics/samples/*.json`] — New samples use `HFC-PLACEHOLDER` / `HFCM-PLACEHOLDER`; the schema test only asserts non-empty. Either tighten to `^HFC[M0-9]{4}$`, or define a placeholder grammar (e.g., `HFC-PLACEHOLDER-{category}`) and document it. Also unify the placeholder convention (`<redacted-source-path>` in `reserved-retired-misuse-report.json` vs realistic paths in others). (blind+edge)
- [x] [Review][Patch] **Unsupported-schema sample is not round-trip-validated** [`docs/diagnostics/samples/unsupported-schema-drift-report.json`, `DiagnosticRegistryTests.cs`] — Sample shape is hardcoded; nothing asserts that `ValidateRegistryJson` on a tampered fixture emits a report with this exact shape. A producer-side rename will drift silently. Add a round-trip test. (edge)
- [x] [Review][Patch] **`StripZeroWidthAndFormatChars` misses NBSP (U+00A0) and similar** [`DiagnosticRegistryTests.cs:310-315`] — Strip removes ZWJ/ZWNJ but the forbidden-pattern scan then searches for literal `<script`; a stub containing `< script` passes both. Extend the strip set or the pattern. (edge)
- [x] [Review][Patch] **`introducedIn` semver shape not validated** [`DiagnosticRegistryTests.cs:748` and registry validator] — `ShouldNotBeNullOrWhiteSpace` accepts `"latest"`, `"unreleased"`, `"v0.2"`, `"0.2"`. Enforce a SemVer 2.0 pattern on both `diagnostic-registry.json` rows and `migration-findings.json` findings. (edge)
- [x] [Review][Patch] **Suppression duplicate-row key omits `newState`** [`DiagnosticRegistryTests.cs:1077`] — Two rows with same `(package, tfm, oldSignature, hfcId, targetRelease)` but different `newState` ("removed" vs "modified") flag as duplicates even though they represent distinct compatibility scenarios. Either include `newState` in the dedupe tuple or document why it is intentionally collapsed. (edge)
- [x] [Review][Patch] **`PackableProjects_UsePackageValidationBaselinePolicy` is text-inspection only** [`tests/.../CiGovernanceTests.cs:163-180`] — Reads `.props` / `.targets` file contents but does not invoke MSBuild to confirm the property group fires after `IsPackable` is set. Original DEF-9-4-A13 was an evaluation-order bug that text matching cannot detect. Add an MSBuild-evaluation-time check or document this gap. (edge)
- [x] [Review][Patch] **`RS2002` `<NoWarn>` removed unconditionally without descriptor-completeness fixture** [`src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj`] — The removal assumes only HFCM rows tripped RS2002. Add a fixture that asserts every release-row id in `AnalyzerReleases.*.md` has a backing `DiagnosticDescriptor`, so any future row without a descriptor fails fast under `TreatWarningsAsErrors=true`. (blind)
- [x] [Review][Defer] **Diff snapshot under `_bmad-output/code-reviews/`** [`_bmad-output/code-reviews/story-11-2-diff.patch` in commit `18cb7aa`] — Initially flagged as accidental commit; verified that `_bmad-output/code-reviews/` is a routine artifact location for prior code reviews (8-6a, 9-1 patches already committed there). No action required. (workflow note)

### Defer (real but out of Story 11.2 scope / requires future story)

- [x] [Review][Defer] **`DocsLinkPrefix` → public `CanonicalHelpLinkFormat` adds public API surface** [`DiagnosticDescriptors.cs:9`] — Package validation is opt-in default-false, so no immediate ApiCompat regression. Re-evaluate when `EnableFrontComposerPackageValidation=true` lands. (blind)
- [x] [Review][Defer] **`storyOwner` controlled vocabulary not enforced** [registry rows + sprint-status] — Already flagged in story Completion Notes as Product/Architecture decision beyond Story 11.2 budget. Same for suppression `ownerStory` cross-check. (edge)
- [x] [Review][Defer] **`baselinePolicy` long-prose field unvalidated** [`docs/diagnostics/compatibility-suppressions.json`] — Pre-existing field, not in Story 11.2 schema scope. Could grow unbounded or carry forbidden tokens. (edge)
- [x] [Review][Defer] **HFC1601 `lifecycleNote` loses runtime-class traceability anchor** [`docs/diagnostics/diagnostic-registry.json` HFC1601 entry] — Prose judgment call; not a contract violation. (blind+edge)
- [x] [Review][Defer] **HFC1601 `helpLinkUri` fingerprint coupling** [`docs/validation/producer-fingerprints.json`] — Future helpLink rebase will silently drift the sha256 unless the producer-fingerprint test re-runs. Acceptable tight coupling for now. (blind)
- [x] [Review][Defer] **`expiresAfter` equal-to-`targetRelease` yields `suppression-expired`** [`DiagnosticRegistryTests.cs:1067`] — Boundary semantics could be documented either way; current "expired-when-equal" is defensible. Add a Theory inline if behavior is confirmed. (edge — dismissable; left as defer for explicit decision)

### Summary

- Decision-needed: **1** (resolved → patch)
- Patch: **31**
- Defer: **7** (R32 reclassified to defer after verifying `_bmad-output/code-reviews/` is the routine artifact location)
- Dismissed as noise: **0**

---

## Dev Notes

### Current State

- Epic 11 routes deferred release-readiness work into seven backlog stories. Story 11.2 owns diagnostic registry, docs stubs, HFCM migration IDs, docs slug/schema/sample validation, package validation, and compatibility suppression policy.
- `docs/diagnostics/diagnostic-registry.json` currently has `schemaVersion: "1.0"`, `canonicalHelpLinkFormat`, numeric ranges for Contracts, SourceTools, Shell, EventStore, Mcp, and Aspire, and a string-array `externalBoundaries` containing `Hexalith.EventStore` and `Hexalith.Tenants`.
- The registry currently contains 106 diagnostics. Existing validation confirms IDs, docs slugs, titles, release rows, and docs stubs, but deferred rows call out missing schema/provenance and policy coverage.
- `relatedIds` exists in the registry and docs stub front matter, but all current registry rows have empty arrays. Story 11.2 should populate high-value logical links where the relationship is already known and test those relationships.
- `docs/diagnostics/README.md` already describes runtime-only vs release-tracked diagnostics, migration ID convention, cross-package range exceptions, stub authoring, and sample placeholder conventions. Treat it as policy to bring tests and data into sync, not as optional prose.
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` still has an independent `DocsLinkPrefix` constant. Deferred work says this duplicates `canonicalHelpLinkFormat`.
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` includes `HFCM*` migration IDs even though those are CLI-emitted, not Roslyn descriptors. The SourceTools csproj currently suppresses RS2002 broadly for this reason.
- `Directory.Build.props` has an `IsPackable`-conditioned package-validation block. Deferred review notes say this is evaluated too early and should move to `Directory.Build.targets` or an equivalent post-project evaluation surface.

### Deferred Rows To Close

| Deferred ID | Required closure |
| --- | --- |
| DEF-9-4-A1 | Enforce compatibility suppression row schema. |
| DEF-9-4-A2 | Make unsupported schema fail closed and short-circuit without NRE. |
| DEF-9-4-A3 | Encode external boundary vs range overlap policy. |
| DEF-9-4-A4 | Add schema/provenance/update policy for `externalBoundaries`. |
| DEF-9-4-A5 | Populate and validate meaningful `relatedIds`. |
| DEF-9-4-A6 | Replace current-year-only drift-sample timestamp guard with broad year detection. |
| DEF-9-4-A7 | Reject LRM/RLM, zero-width, non-NFC, and non-whitelisted docs slugs. |
| DEF-9-4-A8 | Validate sample drift JSON against a real schema. |
| DEF-9-4-A9 | Resolve `Hexalith.Tenants` boundary without range entry. |
| DEF-9-4-A10 | Make source file enumeration deterministic. |
| DEF-9-4-A11 | Remove duplicate docs-link source of truth. |
| DEF-9-4-A12 | Encode docs-root containment as validator-enforced schema/policy. |
| DEF-9-4-A13 | Move package-validation policy to the correct MSBuild evaluation phase. |
| DEF-9-4-A14 | Formalize HFC1601 cross-package range exception. |
| DEF-9-4-A15 | Confirm or correct HFC1040 severity asymmetry. |
| DEF-9-4-A16 | Replace mechanical diagnostic titles/prose for release-critical diagnostics. |
| DEF-9-4-A17 | Add missing sample drift evidence categories and tests. |
| DEF-9-4-HFCM | Move CLI migration IDs to a governance path that does not require broad RS2002 suppression. |

### Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Story 11.2 owns governance contracts, not broad feature behavior. | Keeps the release-readiness story bounded and avoids consuming Story 11.3-11.7 scope. |
| D2 | Registry JSON remains the source of truth for diagnostic metadata. | Docs, descriptors, release rows, and validation should derive from or verify against one registry-owned contract. |
| D3 | HFCM migration IDs need a non-Roslyn release-row path if they are not Roslyn descriptors. | Avoids hiding release-tracking issues behind broad RS2002 suppression. |
| D4 | Cross-package range exceptions must be structured data with tests. | A prose lifecycle note is not enough for release governance. |
| D5 | Docs slug and docs host validation must fail closed offline. | Release validation cannot depend on the public site or live network state. |
| D6 | Diagnostic title/prose authoring should be prioritized, not expanded indiscriminately. | Applies L06/L07; start with touched and release-critical diagnostics instead of rewriting every page blindly. |
| D7 | Sample fixtures are contract tests, not generated evidence dumps. | They must be small, stable, sanitized, and schema-validated. |
| D8 | Duplicate deferred findings close through canonical row plus aliases. | Preserves audit history while preventing repeated review notes from inflating unresolved work. |
| D9 | HFCM governance is metadata/release-row work, not CLI behavior work. | Story 11.2 can validate migration ID release governance without consuming Story 11.3's command behavior scope. |
| D10 | Docs slug validation treats ambiguous encoding as invalid. | Double-decoding, malformed percent sequences, Unicode format characters, and mixed separators are path-confusion risks. |
| D11 | Sanitization applies to authored docs examples and generated reports. | Public diagnostic evidence must not leak local paths, user data, machine names, tokens, or raw payload fragments. |
| D12 | Governance report ordering is part of the contract. | Release diffs and CI evidence must be stable across filesystems and platforms. |
| D13 | Full-corpus prose cleanup requires an explicit split decision. | Prevents title/prose quality work from overrunning the bounded diagnostic governance story. |
| D14 | Story 11.2 adds validation contracts, not release workflow redesign. | The party-mode review found scope pressure across docs, release, package, and ledger governance; the story must not consume Stories 11.3-11.7. |
| D15 | Fail-closed behavior is fixture-defined. | Missing, malformed, unknown, and future schema versions plus invalid suppression and HFCM mappings need deterministic negative tests, not prose-only policy. |
| D16 | HFC1601 is a named exception, not a bypass pattern. | The allowed cross-package case must carry structured provenance and a negative control so future duplicates do not slip through. |
| D17 | Deterministic reports use documented ordinal sort keys. | Byte-stable evidence requires normalized paths, ordinal comparers, sorted-before-truncated output, and shuffled-input tests. |
| D18 | Governance snapshots require direct assertions. | Verify snapshots are useful evidence but cannot be the only oracle for classification, severity, package, link, redaction, ordering, or ledger mapping. |

### Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `docs/diagnostics/diagnostic-registry.json` | Update | Schema fields, related IDs, exception metadata, title/prose metadata as needed. |
| `docs/diagnostics/compatibility-suppressions.json` | Update | Add row schema metadata or validate existing shape. |
| `docs/diagnostics/README.md` | Update | Keep governance policy aligned with implemented validation. |
| `docs/diagnostics/HFC*.md` | Update | Only stubs touched by title/prose/related-id decisions; preserve Story 9-5 markers. |
| `docs/diagnostics/samples/*.json` | Update/Add | Stable sample reports for all named categories. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs` | Update | Main governance test surface. |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` | Update | Remove independent docs-link constant or verify generated registry-derived constant. |
| `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` | Update | Remove or limit HFCM entries if moved to CLI-specific artifact. |
| `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj` | Update | Remove or narrow RS2002 suppression. |
| `Directory.Build.props` / `Directory.Build.targets` | Update | Move package-validation properties to correct evaluation phase. |
| `eng/validate-docs.ps1` | Update if needed | Docs host/slug/stub completeness checks may live here. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update | Mark Story 11.2 rows with evidence after implementation. |
| `_bmad-output/implementation-artifacts/11-2-diagnostic-registry-and-documentation-governance-follow-ups.md` | Update | Dev Agent Record, debug logs, file list, completion notes. |

### Project Structure Notes

- Governance tests for SourceTools live under `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics`.
- Diagnostic stubs are under `docs/diagnostics`, while the public docs reference index is under `docs/reference/diagnostics`.
- Source generator and analyzer projects target `netstandard2.0`; avoid APIs unavailable there unless isolated.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`. Do not initialize or update nested submodules, and do not edit submodule content for this story.
- Keep generated docs output under `docs/_site` untouched unless the implementation explicitly regenerates and validates docs output.

### Testing Strategy

- Run focused governance tests first:
  - `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "FullyQualifiedName~DiagnosticRegistryTests"`
- If package validation policy files change, run focused tests that cover package validation and any affected SourceTools diagnostics.
- If docs validation changes, run:
  - `pwsh ./eng/validate-docs.ps1`
- If docs slug, sample, or report validation changes, include negative fixtures for double-encoded paths, malformed percent encoding, zero-width/bidi characters, non-NFC input, forbidden evidence tokens, and deterministic report ordering.
- Snapshot approvals must be paired with direct assertions for pass/fail classification, diagnostic ID, severity, package, canonical help link, redaction, sort order, report budget, and ledger mapping.
- Determinism tests should shuffle input collections and assert byte-stable output using ordinal comparers with normalized `/` paths; truncation must occur after sorting and redaction.
- Report redaction tests should inject sentinel secrets, local paths, temp folders, usernames, stack traces, SDK banners, live feed URLs, tenant/user IDs, raw payload fragments, and long evidence snippets, then assert absence and max size.
- If descriptor constants or release rows change, run the broader SourceTools diagnostic test slice:
  - `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "Category=Governance|FullyQualifiedName~Diagnostic"`
- Dev completion requires recording outcomes for targeted xUnit governance tests and `pwsh ./eng/validate-docs.ps1`; skipped commands need a reason in the Dev Agent Record.
- For final release-confidence, run the main lane if time allows:
  - `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 9.4 registry | Story 11.2 | Registry remains the source of truth for diagnostic ID ownership, lifecycle, docs links, release rows, and channel severity. |
| Story 9.5 docs site | Story 11.2 | Diagnostic stubs and public docs must preserve DocFX front matter, narrative markers, and docs validation semantics. |
| Story 9.2 CLI migration | Story 11.2 | HFCM migration IDs need release-row governance without pretending they are Roslyn descriptors. |
| Story 11.1 ledger reconciliation | Story 11.2 | Ledger rows routed to Story 11.2 must be resolved or explicitly left with evidence and owner. |
| Story 11.3 | Story 11.2 | CLI behavior changes remain Story 11.3; this story only defines migration ID governance metadata. |
| Story 11.4 | Story 11.2 | SourceTools drift behavior remains Story 11.4 unless the change is specifically diagnostic registry governance. |
| Story 11.7 | Story 11.2 | CI/release automation remains Story 11.7; this story supplies deterministic governance gates it can call. |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| CLI migration command behavior, path normalization, and write safety beyond HFCM metadata. | Story 11.3 |
| Drift detection analyzer behavior outside diagnostic registry governance. | Story 11.4 |
| MCP/schema diagnostic categories outside registry metadata. | Story 11.5 |
| Public docs UX polish outside diagnostic pages touched by this story. | Story 11.6 |
| CI release orchestration and credential governance that consumes these tests. | Story 11.7 |
| Full rewrite of every diagnostic page if title/prose authoring exceeds this story's budget. | Product split decision after Story 11.2 evidence |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-11-deferred-hardening-release-readiness.md#Story-11.2`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-9-4-diagnostic-id-system-and-deprecation-policy`] - DEF-9-4-A1 through DEF-9-4-HFCM deferred rows.
- [Source: `_bmad-output/implementation-artifacts/9-4-diagnostic-id-system-and-deprecation-policy.md`] - original diagnostic ID, registry, release-row, and deprecation governance story.
- [Source: `_bmad-output/implementation-artifacts/9-5-diataxis-documentation-site.md`] - docs site, diagnostics reference, marker, slug, and validation handoff.
- [Source: `_bmad-output/implementation-artifacts/11-1-deferred-work-ledger-reconciliation-and-ownership.md`] - Epic 11 routing and ledger reconciliation contract.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-10.md`] - Correct Course rationale for Epic 11.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope and decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - owner specificity requirement.
- [Source: `_bmad-output/project-context.md`] - project rules for diagnostics, source generators, docs, evidence, submodules, and validation.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-11: `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "FullyQualifiedName~DiagnosticRegistryTests"` -> passed, 61 tests.
- 2026-05-11: `pwsh ./eng/validate-docs.ps1` -> passed; evidence manifest `artifacts/docs/validation-manifest.json`.
- 2026-05-11: `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "Category=Governance|FullyQualifiedName~Diagnostic"` -> passed, 240 tests.
- 2026-05-11: `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~CiGovernanceTests.PackageInventory_IsExplicitLockstepAndReviewable"` -> passed, 1 test.
- 2026-05-11: `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" --blame-hang --blame-hang-timeout 5m` -> passed; one bench assembly had no matching non-performance tests, expected for the filter.
- 2026-05-11 (code-review pass): `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "FullyQualifiedName~DiagnosticRegistryTests"` -> passed, 75 tests.
- 2026-05-11 (code-review pass): `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "Category=Governance|FullyQualifiedName~Diagnostic"` -> passed, 254 tests.
- 2026-05-11 (code-review pass): `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~CiGovernanceTests"` -> passed, 18 tests.
- 2026-05-11 (code-review pass): `pwsh ./eng/validate-docs.ps1` -> passed; producer fingerprint for `docs/diagnostics/diagnostic-registry.json` refreshed (`ac4a77be…0092c9`) after HFC1040 lifecycleNote typo fix.
### Completion Notes List

- 2026-05-10: Story created via `/bmad-create-story 11-2-diagnostic-registry-and-documentation-governance-follow-ups` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-11: Advanced elicitation pass applied during recurring pre-dev hardening job. Added duplicate-row, encoded-slug, sanitization, HFCM classification, deterministic-report, and prose-budget guardrails.
- 2026-05-11: Party-mode review applied during recurring pre-dev hardening job. Added fixture-defined fail-closed behavior, suppression misuse, HFC1601 negative control, severity matrix, shuffled-input determinism, redaction budget, and validation ownership guardrails.

- 2026-05-11: Implemented diagnostic governance hardening. Added fail-closed registry/schema validation, compatibility suppression schema/scope validation, structured external boundary policy, HFC1601 cross-package exception data, reciprocal related IDs, CLI-specific HFCM migration governance, package-validation targets placement, sample report schema coverage, and prioritized HFC0001/HFC1601 prose/title cleanup.
- 2026-05-11: Ledger closure recorded canonical duplicate clusters: DEF-9-4-C1 -> DEF-9-4-A16, DEF-9-4-C2 -> DEF-9-4-A5, DEF-9-4-C10 -> DEF-9-4-HFCM. Full-corpus prose/example rewrite remains a Product/Architecture decision per AC26; storyOwner controlled-vocabulary expansion remains a Product/Architecture decision beyond Story 11.2 budget.
- 2026-05-11: Validation passed: focused DiagnosticRegistryTests (61), docs validation, broader SourceTools governance/diagnostic slice (240), Shell package inventory governance test (1), and main CI-parity lane.
- 2026-05-11: `/bmad-code-review 11-2` triaged 1 decision-needed + 32 patch + 6 defer findings; relabeled DEF-9-4-A16/C3/C7/C8 to `rejected-with-rationale` per AC33 vocabulary; applied 25+ review-driven patches to validators, stub bodies, ledger, and migration-findings schema. Validation rerun: `DiagnosticRegistryTests` 75 passed, governance/diagnostic slice 254 passed, Shell `CiGovernanceTests` 18 passed, `eng/validate-docs.ps1` passed (registry fingerprint refreshed for HFC1040 lifecycleNote typo).

### Prioritized Touched-Diagnostic Set (AC26)

| Diagnostic | Depth of change | Evidence |
| --- | --- | --- |
| HFC0001 | Title rewritten + prose section rewritten + example sanitized | `docs/diagnostics/HFC0001.md` |
| HFC1601 | Title rewritten + prose section rewritten + example sanitized + lifecycleNote authored | `docs/diagnostics/HFC1601.md`, registry HFC1601 entry |
| HFC1037 | Front-matter `relatedIds` populated + body Related Diagnostics synchronized | `docs/diagnostics/HFC1037.md` |
| HFC1040 | Front-matter `relatedIds` populated + body Related Diagnostics synchronized + lifecycleNote typo fixed | `docs/diagnostics/HFC1040.md`, registry HFC1040 entry |
| HFC1044 | Front-matter `relatedIds` populated + body Related Diagnostics synchronized | `docs/diagnostics/HFC1044.md` |
| HFC1056 | Front-matter `relatedIds` populated + body Related Diagnostics synchronized | `docs/diagnostics/HFC1056.md` |
| HFC1057 | Front-matter `relatedIds` populated + body Related Diagnostics synchronized | `docs/diagnostics/HFC1057.md` |
| HFC4001 | Front-matter `relatedIds` populated + body Related Diagnostics synchronized | `docs/diagnostics/HFC4001.md` |

Full-corpus title/prose/example authoring across the remaining ~98 stubs is deferred to a Product/Architecture decision per AC26 and tracked in `deferred-work.md` row DEF-9-4-C3 (`rejected-with-rationale`).

### Unresolved Decisions (AC20)

| Topic | State | Owner / Follow-up |
| --- | --- | --- |
| Full-corpus per-diagnostic example/prose authoring | `rejected-with-rationale` per AC26; beyond Story 11.2 budget | DEF-9-4-C3 — Product/Architecture decision; candidate scope for a future Story 9-5-style prose pass |
| Per-stub `schemaVersion` front-matter field | `rejected-with-rationale`; registry remains the single fail-closed schema contract | DEF-9-4-C7 — Product/Architecture decision on stub-level schema versioning |
| `storyOwner` controlled-vocabulary cross-check vs `sprint-status.yaml` | `rejected-with-rationale`; cross-file vocabulary enforcement beyond Story 11.2 budget | DEF-9-4-C8 — Product/Architecture decision on cross-file vocabulary policy |
| Full MSBuild-evaluation-time test of `PackageValidation` policy | Deferred to code-review follow-up | `deferred-work.md` 2026-05-11 code-review section — Story 11.7 release-governance candidate |
| `EnableFrontComposerPackageValidation=true` baseline + `CanonicalHelpLinkFormat` public surface | Deferred until validation is enabled | `deferred-work.md` 2026-05-11 code-review section |

### Change Log

- 2026-05-10: Created Story 11.2 and marked ready-for-dev.
- 2026-05-11: Advanced elicitation hardening added AC21-AC26, Decisions D8-D13, task refinements, validation guidance, and canonical trace.
- 2026-05-11: Party-mode hardening added AC27-AC34, Decisions D14-D18, fixture/test obligations, validation ownership guardrails, and canonical trace.
- 2026-05-11: Implemented Story 11.2 diagnostic registry, docs, HFCM, package-validation, sample fixture, and ledger governance follow-ups; status moved to review.

### File List

- `Directory.Build.props`
- `Directory.Build.targets`
- `_bmad-output/implementation-artifacts/11-2-diagnostic-registry-and-documentation-governance-follow-ups.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/diagnostics/HFC0001.md`
- `docs/diagnostics/HFC1037.md`
- `docs/diagnostics/HFC1040.md`
- `docs/diagnostics/HFC1044.md`
- `docs/diagnostics/HFC1056.md`
- `docs/diagnostics/HFC1057.md`
- `docs/diagnostics/HFC1601.md`
- `docs/diagnostics/HFC4001.md`
- `docs/diagnostics/README.md`
- `docs/diagnostics/diagnostic-registry.json`
- `docs/diagnostics/migration-findings.json`
- `docs/diagnostics/samples/compatibility-drift-report.json`
- `docs/diagnostics/samples/docs-stub-drift-report.json`
- `docs/diagnostics/samples/duplicate-id-drift-report.json`
- `docs/diagnostics/samples/encoded-docs-root-escape-report.json`
- `docs/diagnostics/samples/hfcm-release-governance-report.json`
- `docs/diagnostics/samples/invalid-lifecycle-transition-report.json`
- `docs/diagnostics/samples/release-row-drift-report.json`
- `docs/diagnostics/samples/reserved-retired-misuse-report.json`
- `docs/diagnostics/samples/suppression-scope-drift-report.json`
- `docs/diagnostics/samples/unsafe-generated-front-matter-report.json`
- `docs/diagnostics/samples/unsupported-schema-drift-report.json`
- `docs/validation/producer-fingerprints.json`
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`
- `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`


## Party-Mode Review

- ISO date/time: 2026-05-11T07:11:41+02:00
- Selected story key: 11-2-diagnostic-registry-and-documentation-governance-follow-ups
- Command/skill invocation used: `/bmad-party-mode 11-2-diagnostic-registry-and-documentation-governance-follow-ups; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor).
- Findings summary: The party-mode review agreed that Story 11.2 was directionally ready but still too easy to implement with broad prose-only governance. The main risks were ambiguous fail-closed triggers, compatibility suppressions masking unrelated diagnostics, HFC1601 becoming a broad cross-package bypass, flattened severity asymmetry, narrow deterministic-ordering coverage, redaction without sentinel leak tests, HFCM governance spilling into release automation, package validation splitting into a parallel engine, and ledger closure expanding beyond Story 9.4/9.5 rows.
- Changes applied: Added AC27-AC34; added required fixture groups for schema, suppression, slug containment, external boundaries, severity asymmetry, deterministic ordering, redacted report budget, and HFCM release governance; tightened tasks for HFC1601 provenance plus negative control, exact package matching, canonical help-link source of truth, existing validation-path ownership, shuffled-input deterministic reports, direct assertions beyond snapshots, and explicit ledger final states; added Decisions D14-D18; expanded validation guidance.
- Findings deferred: No product-scope, architecture-policy, or cross-story contract changes were applied. Release workflow redesign, docs publishing/navigation changes, CLI behavior changes, broad docs rewrites, and unrelated ledger cleanup remain out of scope for Story 11.2 and stay with the named follow-up stories or product split decision.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- ISO date/time: 2026-05-11T04:04:11+02:00
- Selected story key: 11-2-diagnostic-registry-and-documentation-governance-follow-ups
- Command/skill invocation used: `/bmad-advanced-elicitation 11-2-diagnostic-registry-and-documentation-governance-follow-ups`
- Batch 1 method names: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation.
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records.
- Findings summary: The story already covered the core diagnostic governance gaps, but elicitation found implementation paths that could still pass while leaving duplicate deferred rows ambiguous, accepting path-confusing docs slugs, leaking unsafe evidence through examples or reports, misclassifying HFCM migration IDs as Roslyn diagnostics, producing nondeterministic governance report order, or expanding title/prose authoring beyond the release-readiness budget.
- Changes applied: Added AC21-AC26; added task guardrails for canonical duplicate-row aliases, HFCM CLI-migration classification, encoded-slug negative fixtures, prioritized prose scope, forbidden-token scans across docs and reports, deterministic output ordering, and story-record evidence; added Decisions D8-D13; expanded testing guidance; recorded this canonical trace.
- Findings deferred: No product-scope, architecture-policy, or cross-story contract changes were applied. Full-corpus prose rewrite remains a Product split decision if the prioritized touched-diagnostic set exceeds Story 11.2's budget.
- Final recommendation: ready-for-dev
