---
baseline_commit: adc3e0aad30479e5acf46cc2a53411934e356231
---

# Story 6.1: Level-2 ProjectionTemplate overrides

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **Brownfield reality — read this first.** This is a **CONFIRM-AND-PIN + CONTRACT-PRODUCING** story,
> **not** a feature build. The entire Level-2 `[ProjectionTemplate]` override pipeline already exists
> and is already proven in the **default (blocking) test lane**:
>
> - The `[ProjectionTemplate]` attribute, the `ProjectionTemplateMarkerParser` (Parse → pure IR), and
>   the `ProjectionTemplateManifestEmitter` (emits `__FrontComposerProjectionTemplatesRegistration.g.cs`).
> - All AC2 diagnostics (`HFC1033`/`HFC1034`/`HFC1035`/`HFC1036`/`HFC1037`, plus `HFC1024` for an
>   invalid role) are defined, documented under `docs/diagnostics/`, reported at the live call sites,
>   and asserted by `ProjectionTemplateMarkerTests`.
> - `IProjectionTemplateRegistry` / `ProjectionTemplateRegistry`, `ProjectionTemplateAssemblySource`,
>   `AddHexalithProjectionTemplates<TMarker>` (and the descriptor-list overload), and the render-time
>   substitution seam in `RazorEmitter` (`ProjectionTemplateRegistry.Resolve(...)` → `FcProjectionTemplateHost<T>`).
> - End-to-end AC1 substitution is proven by `CounterStoryVerificationTests` and exercised live in
>   `samples/Counter/Counter.Web/Program.cs`.
>
> **Story-number drift (reconcile honestly, do not churn).** The implementation and its tests were
> written under an earlier, more granular planning numbering and carry comments like `Story 6-2 T5`,
> `Story 6-3`, `Story 6-6 P18`, `Story 9-4`. In the **current** `epics.md` plan, that Level-2 work IS
> this story (6.1). Treat those labels as historical. Per Epic 5 retro action **E5-AI-5**, only clean
> a stale label if you are already editing that exact line for a real reason — no unrelated churn.
>
> **Default expectation:** the genuine net-new deliverable is the **FC-CUST contract artifact**
> (override-resolution precedence + Level-2 contract + non-goals — Epic 5 retro action **E5-AI-4**),
> plus a small **default-lane** pin for the one real coverage gap, plus honest re-verification. Expect
> **zero or near-zero `src/**` behavior change** and **no `.verified.txt` changes** unless you prove a
> real contract gap and record why.

## Story

As an adopter developer,
I want to register a custom view template for a projection,
so that I can replace the generated layout without forking.

## Acceptance Criteria

> AC1 and AC2 are the verbatim Epic 6 acceptance criteria (confirm-and-pin them against live source).
> AC3 and AC4 are the contract-producing + no-regression framing required by the Epic 5 retrospective
> readiness constraints for Epic 6.

**AC1 — A registered `[ProjectionTemplate]` renders in place of the generated view for its projection+role.** *(Epic 6 AC; FR8, FR5, FR4 — projection-template manifest emission)*
**Given** a Blazor component annotated `[ProjectionTemplate]` with a typed `Context` parameter
(`[Parameter] ProjectionTemplateContext<TProjection> Context`),
**When** it is registered via `AddHexalithProjectionTemplates<TMarker>` (or the descriptor-list overload),
**Then** the generated projection view resolves it through `IProjectionTemplateRegistry.Resolve(projectionType, role)`
and renders the custom template (via `FcProjectionTemplateHost<TProjection>`) **instead of** the generated
default body for that projection+role,
**And** this is proven in the **default blocking test lane** (not an excluded lane),
**And** exact-role match wins, with fall-back to the any-role (`Role: null`) template when no exact-role
template is registered.

**AC2 — Invalid templates report the catalog diagnostics at build.** *(Epic 6 AC; FR6)*
**Given** an invalid template,
**When** the project is built,
**Then** the following are reported at the documented severities and the descriptor is suppressed/retained
exactly as the catalog specifies, each proven in the **default blocking test lane**:
- **HFC1033** (Error) — projection type is not a non-abstract, non-generic `[Projection]`-annotated class; descriptor suppressed.
- **HFC1034** (Warning) — template is not a valid typed template component (missing `Context` property, or `Context` not marked `[Parameter]`, or non-class/abstract/generic/static); descriptor suppressed.
- **HFC1037** (Error) — duplicate `[ProjectionTemplate]` for the same (projection, role) tuple; **all** duplicates suppressed deterministically (no order-dependent winner).
- **HFC1035** (Warning) — contract **major** version mismatch; descriptor suppressed (runtime selection skips it).
- **HFC1036** (Warning) — contract **minor** drift; descriptor **retained** (still selectable); build-only drift reports nothing.
- (Adjacent) **HFC1024** (Warning) — unknown/unsafe-cast `ProjectionRole` value; descriptor suppressed.

**AC3 — Produce the FC-CUST override-resolution + Level-2 template contract artifact.** *(Epic 5 retro E5-AI-4)*
**Given** the Level-2 pipeline and the documented render precedence,
**When** the dev agent audits the live source and writes the contract,
**Then** `_bmad-output/contracts/fc-cust-override-resolution-and-level2-template-contract-2026-06-05.md`
exists and records, with source + test citations for each item:
- the **override-resolution precedence** the generator emits today: **Level 4 view-override → Level 2 template → generated default body**, and that Level 3 field slots compose *within* whichever body renders (this is the deterministic-precedence record Story 6.3 will build on);
- the **Level-2 contract**: the `[ProjectionTemplate](projectionType, expectedContractVersion, Role?)` attribute shape, the required `[Parameter] ProjectionTemplateContext<TProjection> Context` surface, the `AddHexalithProjectionTemplates<TMarker>` / descriptor-list registration seam, the `__FrontComposerProjectionTemplatesRegistration` generated manifest, `ProjectionTemplateContractVersion.Current` (packed `Major*1_000_000 + Minor*1_000 + Build`, currently `1_000_000`), and the descriptor **cache-safety** invariant (manifest is type-metadata only — no timestamps, file paths, tenant/user IDs, payloads, or localized strings);
- the **diagnostics disposition** for HFC1033/1034/1035/1036/1037 (+HFC1024, +runtime HFC2115) as `confirmed-stable`, with the suppress/retain rule for each;
- the **non-goals**: Level-3 field-slot behavior (Story 6.2), Level-4 full-view behavior beyond stating precedence (Story 6.3), accessibility-safety diagnostics HFC1050–HFC1055 and `FcCustomizationDiagnosticPanel` (Story 6.4), and the Epic-5 hard boundaries below;
- every `open` item (if any) carries an owner, reason, and follow-up story/backlog reference (no vague "needs review").

**AC4 — Behavior is unchanged and pinned; evidence is reconciled.** *(Epic 5 retro E5-AI-1 / readiness "File List reconciliation as a review gate")*
**Given** the pipeline is already implemented,
**When** this story completes,
**Then** all existing ProjectionTemplate pins still pass and a **default-lane** pin closes the one real
coverage gap (explicit `ProjectionTemplateMarkerInfo` value-equality / cache-key test — see Dev Notes),
**And** any new tests are contract/boundary pins, not duplicate feature tests,
**And** `.verified.txt` snapshots and owned `PublicAPI*.Shipped.txt` baselines are byte-for-byte unchanged
unless an intentional, reviewed contract change is made and explained,
**And** the **File List is reconciled against `git` changed files before review promotion** (compare
changed files to the File List; this is a gate, not an optional checklist item),
**And** none of the Epic-5 boundaries in "Technical constraints" are weakened.

## Tasks / Subtasks

- [x] **Task 1 — Re-audit the live Level-2 pipeline against AC1/AC2 (AC: #1, #2)**
  - [x] Confirm the attribute + parser + emitter chain: `ProjectionTemplateAttribute`, `ProjectionTemplateMarkerParser`, `ProjectionTemplateManifestEmitter`, and the `ForAttributeWithMetadataName` registration in `FrontComposerGenerator`. Confirm the IR (`ProjectionTemplateMarkerInfo`) is Roslyn-free and fully equatable (no `ISymbol` escapes parse).
  - [x] Confirm the render seam: `RazorEmitter` emits `ProjectionViewOverrideRegistry.Resolve(...)` (Level 4) → `ProjectionTemplateRegistry.Resolve(...)` (Level 2) → default body, hosting the template via `FcProjectionTemplateHost<TProjection>` with a per-render `ProjectionTemplateContext<TProjection>`.
  - [x] Confirm the registration seam: `AddHexalithProjectionTemplates<TMarker>` and the descriptor-list overload in `ServiceCollectionExtensions`, `ProjectionTemplateAssemblySource.ResolveDescriptors`, and that `AddHexalithFrontComposerQuickstart` registers `IProjectionTemplateRegistry` (+ slot/view-override registries) as singletons.
  - [x] Confirm the adopter end-to-end usage in `samples/Counter/Counter.Web/Program.cs` (both overloads exercised).
  - [x] Re-run the existing pins and record which prove AC1/AC2 **in the default lane** (see "Existing coverage" table). Do not duplicate them.

- [x] **Task 2 — Close the one real coverage gap with a default-lane pin (AC: #4)**
  - [x] Add an explicit value-equality / incremental-cache-key test for `ProjectionTemplateMarkerInfo` (and `ProjectionTemplateMarkerResult`): equal field sets compare equal and share a hash; a single differing field (e.g. `Role`, `ExpectedContractVersion`, `FilePath`, `Line`, `Column`, any type-name field) compares unequal. This protects the documented "IR must be pure & fully equatable" cache invariant, which is currently only proven *indirectly* by the drift incremental-cache test. Place it beside the existing SourceTools ProjectionTemplate tests.
  - [x] Only add further pins if Task 1 surfaces a genuine **default-lane** gap in AC1/AC2 (e.g. an assertion that today only lives in an excluded `Category` lane). Justify any addition in the Dev Agent Record; do not pad with duplicate feature tests.

- [x] **Task 3 — Write the FC-CUST contract artifact (AC: #3)**
  - [x] Create `_bmad-output/contracts/fc-cust-override-resolution-and-level2-template-contract-2026-06-05.md` covering precedence, the Level-2 contract, diagnostics disposition, cache-safety, and non-goals (full content list in AC3 and "Contract content outline" below).
  - [x] Cite the exact source files + tests that prove each item. Mark every slice `confirmed-stable`, `internalized`, or `open`; `open` requires owner/reason/risk/follow-up.

- [x] **Task 4 — Verify no regression (AC: #4)**
  - [x] `dotnet build Hexalith.FrontComposer.slnx -c Release` → require 0 warnings / 0 errors under TWAE (use `-m:1 /nr:false` only if node reuse/parallelism flakes).
  - [x] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`. If sandboxed VSTest hits the known local `SocketException (13): Permission denied`, fall back to the xUnit v3 in-process runner per the Story 2.4–5.5 pattern and record that solution-level VSTest remains the CI gate.
  - [x] Run focused lanes for touched areas: `ProjectionTemplateMarkerTests`, `ProjectionTemplateRegistryTests`, `ProjectionTemplateAssemblySourceTests`, `ProjectionTemplateContractsTests`, `FcProjectionTemplateHostTests`, `CounterStoryVerificationTests`, the new marker-equality pin, and any `RazorEmitter` render pins if generator files were touched.
  - [x] Confirm `.verified.txt` and owned `PublicAPI*.Shipped.txt` baselines are byte-for-byte unchanged (or explain the intentional change in the contract + Change Log).

- [x] **Task 5 — Honest record-keeping + File List reconciliation gate (AC: #4)**
  - [x] Record in the Dev Agent Record: exact disposition (confirm-and-pin vs. any source change), tests run, before/after failure counts, and the test-lane caveat if VSTest was socket-blocked.
  - [x] Reconcile the File List against `git diff --name-only` BEFORE flipping to review. Scan changed story-owned files for stray authoring/tool-call sentinels.

## Dev Notes

### Prior-story / epic intelligence (carry these constraints)

This is the first story of Epic 6. There is no in-epic previous story, but the **Epic 5 retrospective**
recorded explicit Epic-6 readiness constraints that apply here:

- **E5-AI-4 (Architecture):** record Epic-6 override **precedence** and accessibility diagnostic
  **boundaries** before implementation — "Story 6.1/6.3/6.4 notes name precedence, diagnostics, and
  non-goals." → satisfied by AC3's contract artifact.
- **Override precedence must be deterministic and documented before Level-2 and Level-4 are reasoned
  about together.** The generator already emits Level 4 → Level 2 → default; capture that as the
  contract record so Story 6.3 builds on it rather than re-deciding it.
- **Keep customization diagnostics independent of MCP resource security.** HFC1033–1037 (and 1050–1055,
  owned by 6.4) must not change MCP projection visibility or the skill-resource bypass semantics.
- **Do not weaken tenant/resource visibility.** Override rendering must not create a backdoor around
  projection gates. (Level-2 templates render generated, already-gated projection state; do not add a
  data path that bypasses it.)
- **Evidence hygiene is a gate, not a checklist item** (File-List drift was a repeated defect across
  Epics 3–5). → AC4 makes File List reconciliation a review gate.

### Existing implementation surface (audit these; do not rebuild)

| Slice | Anchors (source) | Notes |
|---|---|---|
| Attribute | `src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionTemplateAttribute.cs` (`AttributeTargets.Class`, ctor `(Type projectionType, int expectedContractVersion)`, optional `Role`); `ProjectionRole.cs` (`ActionQueue/StatusOverview/DetailRecord/Timeline/Dashboard`) | `Role` presence is detected by named-arg presence at the call site, not the property default. |
| Parse → IR | `src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerParser.cs`; IR `ProjectionTemplateMarkerInfo` / `ProjectionTemplateMarkerResult` (`.../Parsing/ProjectionTemplateMarkerInfo.cs`) | Pure, equatable, string/int-only IR (cache invariant). Validates projection type, Context param + `[Parameter]`, role, contract version. **No Transform stage** — parser output flows straight to the emitter. |
| Discovery | `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` — `ForAttributeWithMetadataName("Hexalith.FrontComposer.Contracts.Attributes.ProjectionTemplateAttribute")`, tracking name `ParseProjectionTemplate`; major-mismatch markers suppressed before emit | One of the three attribute providers (`[Projection]`, `[Command]`, `[ProjectionTemplate]`). |
| Emit (manifest) | `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionTemplateManifestEmitter.cs` → `__FrontComposerProjectionTemplatesRegistration.g.cs` (`Descriptors` list + `ContractVersion`) | Deterministic sort `(ProjectionTypeFullName, Role null-first, TemplateTypeFullName)`; HFC1037 dedup excludes all collisions; manifest is type-metadata only. |
| Descriptor / context | `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateDescriptor.cs`, `ProjectionTemplateContext.cs` (`#if NET10_0_OR_GREATER`), `ProjectionTemplateContractVersion.cs` (`Current = 1_000_000`) | Context carries items, columns, sections, `defaultBody`, and section/row/field renderers; constructed per render, never cached. |
| Registry | `src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionTemplateRegistry.cs`; impl `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs`; `ProjectionTemplateAssemblySource.cs` | `Resolve(Type, ProjectionRole?)`: exact-role wins, null-role fallback; duplicate/ambiguous → `null` (fail-closed); major-version reject + minor-drift accept at registration. |
| Render seam | `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` ~L1297–1364 (ViewOverride L1308 → Template L1337 → default L1362); host `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs` (error-boundary → runtime `HFC2115` fault isolation) | Generated view `[Inject]`s the three registries. `roleExpr` derives from the view's render strategy (`Default` → `null` role). |
| Registration | `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` — `AddHexalithProjectionTemplates<TMarker>` (L537) + descriptor-list overload (L560); registries wired in `AddHexalithFrontComposer`/Quickstart | `samples/Counter/Counter.Web/Program.cs:66,71` exercises both overloads. |
| Customization contract gate | `src/Hexalith.FrontComposer.Shell/Services/Customization/` — `ICustomizationContractRejectionLog`, `CustomizationContractValidationGate`, `CustomizationContractVersion` | Optional fail-closed-on-major-mismatch startup gate; logs HFC1035/HFC1036 at runtime. |
| Diagnostics | IDs `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`; descriptors `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`; docs `docs/diagnostics/HFC{1024,1033,1034,1035,1036,1037,2115}.md` + `docs/diagnostics/diagnostic-registry.json` | All defined, documented, registered, reported. Messages are structured What/Expected/Got/Fix/Fallback/DocsLink. |

### Existing coverage (already proves AC1/AC2 in the DEFAULT lane — do not duplicate)

| Requirement | Status | Test |
|---|---|---|
| AC1 end-to-end substitution (registry consulted; custom template replaces generated `FluentDataGrid` body) | PROVEN (default lane) | `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` — `CounterProjectionView_SelectedTemplate_RendersInsideGridEnvelopeAndUsesFieldRenderer` |
| AC1 registry resolution (exact-role wins, null-role fallback, ambiguous→null, major reject) | PROVEN (default lane) | `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionTemplates/ProjectionTemplateRegistryTests.cs` |
| AC1 assembly-source reflection (manifest present → descriptors; absent → empty) | PROVEN (default lane) | `.../ProjectionTemplates/ProjectionTemplateAssemblySourceTests.cs` |
| AC1 host component (passes context, isolates fault, no payload leak, diagnostic once) | PROVEN (default lane) | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionTemplateHostTests.cs` |
| AC2 HFC1033/1034/1035/1036/1037/1024 emission + suppress/retain | PROVEN (default lane) | `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/ProjectionTemplateMarkerTests.cs` |
| Manifest determinism / cache-safety (no timestamps/paths/tenant/user) | PROVEN (default lane) | `ProjectionTemplateMarkerTests.RunGenerators_ManifestIsDeterministic_NoTimestampsOrPaths` |
| Contracts invariants (version packing, null-safety, structural equality) | PROVEN (default lane) | `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionTemplateContractsTests.cs` |
| Incremental cache: `ParseProjectionTemplate` stays cached on unrelated edits | PROVEN (default lane, indirect) | `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Incremental/DriftIncrementalCacheTests.cs` |

**The one real gap (Task 2):** there is no **explicit** unit test of `ProjectionTemplateMarkerInfo`
value-equality / `GetHashCode` (the cache key). It is only proven indirectly by the drift cache test.
Add a direct pin — cheap, and it guards the documented "a missing equatable field silently breaks
caching" invariant.

### Story-number reconciliation note (record this in the contract + Dev Agent Record)

Source/test comments reference `Story 6-2`, `Story 6-3`, `Story 6-6`, `Story 9-4`, `GB-P10`, `D15/AC15`,
etc. These come from an earlier, finer-grained planning decomposition. Under the current `epics.md`,
**Level-2 ProjectionTemplate overrides = this Story 6.1**. State this mapping plainly so reviewers do
not think the code belongs to a different story. Do not rename or re-label the historical comments
unless you are already editing that exact line for an independent reason (E5-AI-5).

### Contract content outline (FC-CUST artifact)

1. **Scope & version:** FC-CUST v1; this artifact owns the customization override-resolution precedence
   record and the Level-2 template contract. Date `2026-06-05`.
2. **Override-resolution precedence (deterministic):** Level 4 view-override → Level 2 template →
   generated default body; Level 3 field slots compose within whichever body renders. Cite
   `RazorEmitter` render block + `CounterStoryVerificationTests`.
3. **Level-2 attribute + registration contract:** attribute signature, required `[Parameter]
   ProjectionTemplateContext<TProjection> Context`, `AddHexalithProjectionTemplates<TMarker>` and
   descriptor-list overloads, `__FrontComposerProjectionTemplatesRegistration` manifest, contract
   version packing + `Current`, descriptor cache-safety (type-metadata only), resolution semantics
   (exact-role wins, null-role fallback, ambiguous/major-mismatch fail-closed).
4. **Diagnostics disposition:** HFC1033/1034/1035/1036/1037 (+1024 build, +2115 runtime) — id, severity,
   suppress/retain rule, build vs runtime call site, doc link. Mark `confirmed-stable`.
5. **Non-goals / boundaries:** Level-3 slot behavior (6.2), Level-4 full-view behavior beyond precedence
   (6.3), accessibility diagnostics HFC1050–HFC1055 + `FcCustomizationDiagnosticPanel` (6.4); no changes
   to `CanonicalSchemaMaterial`/fingerprints/manifest compatibility, no alternate MCP projection URI
   forms, no weakening of tenant/resource visibility, no new `IStorageService.SetAsync` call sites in
   `Shell/State/` (NFR17 tripwire).
6. **Open items:** none expected; if any, owner + reason + follow-up story.

### Project structure notes

- Contract artifact goes under `_bmad-output/contracts/` — **never** `docs/` (published, CI-gated DocFX site).
- Contracts (attributes, rendering models, registry interface) live in `src/Hexalith.FrontComposer.Contracts/`.
- Generator (parser/emitter) lives in `src/Hexalith.FrontComposer.SourceTools/`; never hand-edit `obj/**/generated/HexalithFrontComposer/**`.
- Registry impl + host + DI extensions live in `src/Hexalith.FrontComposer.Shell/`.
- New tests stay beside existing coverage: `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/` (marker), `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionTemplates/` and `.../Components/Rendering/` (registry/host), `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/` (contract invariants).

### Technical constraints

- .NET 10; FluentUI v5 RC `5.0.0-rc.3-26138.1`; Fluxor `6.9.0`; Roslyn `5.3.0`; xUnit v3 + Shouldly + NSubstitute + bUnit + Verify (`Verify.XunitV3`). Versions are pinned centrally in `Directory.Packages.props` — do not add `Version=` to a `.csproj` or bump libraries in this story.
- `TreatWarningsAsErrors=true` everywhere; `ConfigureAwait(false)` on awaits (except Blazor UI code that intentionally resumes the renderer context); validate public-boundary args; file-scoped namespaces; Allman braces; **no copyright headers**; ULIDs never GUIDs.
- Multi-TFM guard: any net10/FluentUI-only code added to `Contracts` must sit behind `#if NET10_0_OR_GREATER`; `SourceTools` references **only** `Contracts` and must stay netstandard2.0-clean (no `ISymbol` in IR).
- Run **solution-level** `dotnet test` with the trait filter and `DiffEngine_Disabled=true`. `.slnx` only.
- New diagnostics are out of scope here; do not add or renumber HFC ids. Do not touch `CanonicalSchemaMaterial`, fingerprint algorithms, or the generated-output-path contract.
- Keep `.verified.txt`, owned `PublicAPI*.Shipped.txt`, and pacts byte-for-byte unchanged unless an intentional, documented contract change is made.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 6.1: Level-2 ProjectionTemplate overrides] — story statement + the two epic ACs (FR8, FR5, FR6).
- [Source: _bmad-output/implementation-artifacts/epic-5-retro-2026-06-05.md#4. Next Epic Preparation - Epic 6] — E5-AI-4 precedence/diagnostic-boundary record; dependency constraints; File-List gate.
- [Source: _bmad-output/project-docs/architecture.md] — generator pipeline + "Levels 2–4 customization … inject alternate render fragments" (precedence not yet documented there; this story's contract fills that).
- [Source: _bmad-output/project-context.md] — generator/IR cache invariant, diagnostic-band rules, testing rules, NFR17 storage tripwire, docs-vs-_bmad-output rule.
- [Source: _bmad-output/implementation-artifacts/2-8-confirm-the-fc-tbl-table-api-contract.md] — confirm-and-pin + contract-artifact pattern this story follows.
- [Source: src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionTemplateAttribute.cs] — attribute contract.
- [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerParser.cs] — validation + diagnostics call sites.
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionTemplateManifestEmitter.cs] — manifest emission + HFC1037 dedup.
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs] — render-time precedence (L4→L2→default) and host wiring.
- [Source: src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs] — runtime resolution semantics.
- [Source: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs] — render host + HFC2115 fault isolation.
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs] — `AddHexalithProjectionTemplates` + registry registration.
- [Source: samples/Counter/Counter.Web/Program.cs] — live adopter registration (both overloads).
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/ProjectionTemplateMarkerTests.cs] — AC2 diagnostic pins + manifest determinism.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs] — AC1 end-to-end substitution pin.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionTemplates/] — registry + assembly-source pins.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionTemplateHostTests.cs] — host isolation pins.
- [Source: docs/diagnostics/diagnostic-registry.json] — HFC1033–1037/1024/2115 catalog entries.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-05: Activation resolved with no prepend/append steps; loaded `_bmad-output/project-context.md`, config, story, and sprint status.
- 2026-06-05: Re-audited the Level-2 live pipeline: attribute/parser/emitter/generator discovery, render precedence, registration seams, runtime registry resolution, host fault isolation, and Counter sample registration.
- 2026-06-05: Red phase: added explicit marker cache-key tests. `ProjectionTemplateMarkerInfo_ValueEquality_UsesEveryCacheKeyField` failed because `ProjectionTemplateMarkerInfo.GetHashCode()` omitted namespace/type-name/file-path fields that `Equals` already compared.
- 2026-06-05: Green/refactor phase: updated `ProjectionTemplateMarkerInfo.GetHashCode()` to include every field used by equality; reran the new pins and existing marker tests successfully.
- 2026-06-05: Required solution-level VSTest command was attempted with `DiffEngine_Disabled=true` and `-m:1 /nr:false`; local VSTest remains socket-blocked with `System.Net.Sockets.SocketException (13): Permission denied`. CI remains the authoritative solution-level VSTest gate; local evidence uses the established xUnit v3 in-process runner fallback.
- 2026-06-05: File List reconciled against story-owned changed files. `git status` also shows pre-existing unrelated `_bmad-output/story-automator/orchestration-1-20260604-140358.md` changes from before this dev-story run; not included in this story File List.
- 2026-06-05 (automated review correction): The pre-review reconciliation above was incomplete — it missed two Story-6.1-owned changed files surfaced by `git status --porcelain`: the QA-generated browser spec `tests/e2e/specs/projection-template-overrides.spec.ts` and its `_bmad-output/implementation-artifacts/tests/test-summary.md` evidence. Both are now added to the File List, closing the AC4 reconciliation gate. The `orchestration-1-20260604-140358.md` exclusion remains correct (pre-existing, unrelated).

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Confirm-and-pin story completed with one small source fix: `ProjectionTemplateMarkerInfo.GetHashCode()` now matches the full equality surface, closing the explicit incremental-cache-key gap.
- Created the FC-CUST contract artifact with confirmed-stable precedence, Level-2 contract, diagnostics disposition, cache-safety invariant, story-number reconciliation, non-goals, and no open items.
- No new diagnostics, dependencies, package versions, schema/fingerprint changes, MCP URI/security changes, `IStorageService.SetAsync` call sites, `.verified.txt` updates, or `PublicAPI*.Shipped.txt` updates.
- Validation summary: Release solution build passed 0 warnings / 0 errors. Focused Release in-process lanes passed: SourceTools ProjectionTemplate marker tests 15/15, Contracts ProjectionTemplate contracts 6/6, Shell registry/assembly/host tests 18/18, Counter Level-2 substitution pin 1/1, Counter Level-2 + Level-3 composition pin 1/1.
- Broad Shell `CounterStoryVerificationTests` class run reproduced 2 existing snapshot/culture-format failures outside the Level-2 story surface; the story-specific Counter pins passed when run directly.
- E2E coverage (recorded in `tests/test-summary.md`): the QA step authored `tests/e2e/specs/projection-template-overrides.spec.ts` for browser-level Level-2 substitution on the `type` and `data-formatting` specimens. `npm --prefix tests/e2e run typecheck` passed and `playwright --list` discovered 2 tests; browser execution is sandbox-blocked by the known Kestrel `SocketException (13): Permission denied`, so CI remains the execution gate for this spec (same caveat as solution-level VSTest). Its selectors, specimen routes, and `.fc-specimen-generated-template` counts were statically verified against the committed `Counter.Specimens` markup during review.

### File List

- `_bmad-output/contracts/fc-cust-override-resolution-and-level2-template-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/6-1-level-2-projectiontemplate-overrides.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerInfo.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/ProjectionTemplateMarkerTests.cs`
- `tests/e2e/specs/projection-template-overrides.spec.ts`

### Change Log

- 2026-06-05: Marked Story 6.1 in progress and then review in story/sprint tracking.
- 2026-06-05: Added explicit `ProjectionTemplateMarkerInfo` and `ProjectionTemplateMarkerResult` cache-key/value-equality pins.
- 2026-06-05: Fixed `ProjectionTemplateMarkerInfo.GetHashCode()` to include all equality fields.
- 2026-06-05: Added FC-CUST override-resolution and Level-2 template contract artifact.
- 2026-06-05: Verified Release build, focused default-lane pins, baseline unchanged checks, and File List reconciliation.
- 2026-06-05 (automated review): Reconciled the File List against `git status` — added the missed `tests/e2e/specs/projection-template-overrides.spec.ts` and `_bmad-output/implementation-artifacts/tests/test-summary.md`; recorded the E2E coverage and its sandbox-blocked execution caveat; re-ran the two marker cache-key pins in-process (2/2 pass).

## Senior Developer Review (AI)

- **Reviewer:** Jérôme Piquot · **Date:** 2026-06-05 · **Outcome:** Approve (after auto-fix)
- **Scope reviewed:** AC1/AC2 confirm-and-pin audit, the `ProjectionTemplateMarkerInfo.GetHashCode()` fix + new equality pins, the FC-CUST contract artifact (AC3), and AC4 evidence reconciliation. `_bmad/` and `_bmad-output/` excluded from code-quality review per skill rules (validated only as AC deliverables/evidence).

**Findings**

- 🔴 **CRITICAL (fixed) — AC4 File List reconciliation gate failed.** Task 5 was marked `[x]` and the Debug Log claimed reconciliation was complete, but the File List omitted two Story-6.1-owned changed files: `tests/e2e/specs/projection-template-overrides.spec.ts` (QA-generated Level-2 override browser spec, auto-discovered by Playwright `testDir: './specs'`) and `_bmad-output/implementation-artifacts/tests/test-summary.md` (diff is 100% Story 6.1 content). Fixed by adding both to the File List and correcting the Debug Log. The `orchestration-1-20260604-140358.md` exclusion was verified correct.
- 🟡 **MEDIUM (fixed) — E2E deliverable under-recorded.** The Dev Agent Record never referenced the Playwright spec; Task 4's validation list and Completion Notes omitted it though `test-summary.md` documents it as the story's E2E coverage. Fixed with an honest Completion Note (typecheck pass, `--list` 2 tests, browser run sandbox-blocked → CI gate).
- 🟢 **LOW (no change) — historical drift labels.** `Story 6-2 T3` comments remain in `ProjectionTemplateMarkerInfo.cs`; correctly left untouched per E5-AI-5 (edit-only-if-already-touching-the-line).

**Verified correct (no action):**

- `GetHashCode()` now hashes all 11 fields that `Equals` compares (Template/Projection namespace + type-name + full-name, `Role`, `ExpectedContractVersion`, `FilePath`, `Line`, `Column`); previously it omitted 5, silently weakening the incremental-cache key. New pins `ProjectionTemplateMarkerInfo_ValueEquality_UsesEveryCacheKeyField` and `ProjectionTemplateMarkerResult_ValueEquality_UsesMarkerAndDiagnostics` pass **2/2** in-process (xUnit v3; VSTest socket-blocked locally — CI is the gate).
- AC1 precedence holds in live source: `RazorEmitter.cs:1308` (L4 view-override) → `:1337` (L2 template) → `:1355` (`FcProjectionTemplateHost`) → default body; specimens register all three templates.
- AC3 contract artifact exists, covers precedence / Level-2 contract / diagnostics disposition / cache-safety / non-goals / open-items, and its citations match source (`ProjectionTemplateContractVersion.Current = (Major*1_000_000)+(Minor*1_000)+Build = 1_000_000`).
- AC4 snapshot sub-claim true: no `.verified.txt` or `PublicAPI*.Shipped.txt` changes in the working tree.

**Outcome:** No CRITICAL issues remain after fixes (no `src/**` behavior change beyond the already-correct `GetHashCode` fix; only documentation/File-List reconciliation was applied). Status → **done**.
