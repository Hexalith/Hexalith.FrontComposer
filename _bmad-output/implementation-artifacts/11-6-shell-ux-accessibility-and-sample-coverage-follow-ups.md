# Story 11.6: Shell UX, Accessibility, and Sample Coverage Follow-ups

Status: ready-for-dev

> **Epic 11** - Deferred Hardening & Release Readiness. Closes shell UX, accessibility, localization/RTL/visual specimen, dev-mode overlay, customization-gradient sample, and Counter/sample guidance follow-ups routed from Epics 2, 3, 4, 6, and 10. Applies lessons **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 11-6 is the release-readiness hardening pass for user-facing shell polish and sample evidence.

Earlier stories delivered command forms, shell navigation, command palette, generated rendering, customization levels, dev-mode overlay, visual/accessibility specimens, and Counter sample evidence. Later reviews left a large set of focused follow-ups around dev-mode overlay accessibility and localization, starter-template generation, registry visibility, Counter sample fixture coverage, visual/specimen scope, localization/RTL decisions, and shell/customization edge cases.

This story does not reopen all Shell UX work. It inventories the Story 11.6-owned deferred rows, fixes the high-value release-readiness gaps, and records accepted constraints or splits with concrete owners and evidence. The intended outcome is that user-facing polish and sample guidance are no longer scattered across old review notes.

---

## Story

As a UX and quality owner,
I want shell, accessibility, and sample-domain deferrals routed into one release-readiness stream,
so that user-facing polish and sample guidance do not remain scattered across old reviews.

### Release-Readiness Job To Preserve

A release owner should be able to inspect Story 11.6 evidence and know which shell UX, dev-mode overlay, accessibility/specimen, localization/RTL, and Counter sample follow-ups are fixed, accepted, split, or blocked without rereading every Epic 2, 3, 4, 6, and 10 review note.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary Shell files | Harden dev-mode overlay, annotation, starter-template, projection override/slot/template registry, and rendering host code only where Story 11.6-owned rows require it. |
| Primary sample files | Keep Counter as the reference sample; add fixtures or docs only when they prove real customization-gradient behavior. Do not create a new sample domain solely for this story. |
| Primary tests | Extend focused Shell bUnit, SourceTools emitter, Counter sample verification, and E2E specimen tests where the selected rows require executable evidence. |
| Deferred ledger | Close or explicitly accept Story 11.6 rows in `_bmad-output/implementation-artifacts/deferred-work.md`, including the 287-row bucket summary, dev-mode rows DW-0561 through DW-0575, and sample/customization rows DW-0204 through DW-0229 and DW-0534 through DW-0560. |
| Accessibility | Preserve labels, keyboard reachability, focus visibility, live-region parity, reduced motion, forced-colors behavior, and deterministic axe/visual evidence. |
| Localization / RTL | Make v1 scope explicit. Fix small hardcoded Shell/dev-mode strings when bounded; split broad runtime localization/RTL matrices to named owners with evidence. |
| Scope guardrail | Do not absorb diagnostic registry governance, CLI/IDE hardening, SourceTools drift hardening, MCP schema work, EventStore reliability, or CI/release workflow work. |
| Validation | Start with focused Shell/Counter tests; run Playwright specimen checks only if specimen routes, manifest, or visual/a11y behavior changes. |

Start here: T1 inventory Story 11.6 rows -> T2 classify fix/accept/split -> T3 harden dev-mode overlay and starter-template seams -> T4 patch sample/customization evidence -> T5 update specimen/localization decisions -> T6 reconcile ledger and evidence.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- |
| AC1 | Story 11.6-owned deferred rows exist in `deferred-work.md` | Story 11.6 completes | Each row is marked resolved, superseded, split, accepted, or blocked with date, owner, rationale, and validation evidence; no row is silently deleted. |
| AC2 | The Story 11.6 bucket currently contains 287 routed rows | The implementer builds the starting inventory | The inventory groups rows by shell UX, dev-mode overlay, customization-gradient contracts, sample evidence, accessibility/specimens, localization/RTL, and adjacent handoffs, with canonical row IDs and aliases preserved. |
| AC3 | A row is accepted instead of fixed | The ledger and Dev Agent Record are updated | The acceptance names likelihood, impact, release risk, downstream consumer impact, evidence, owner, and revisit trigger. |
| AC4 | A row belongs more naturally to Story 11.2, 11.3, 11.4, 11.5, 11.7, 9.5, or product/architecture | The story triages it | The row is split or reaffirmed with a named owner and reason; Story 11.6 does not silently implement adjacent governance. |
| AC5 | `FcDevModeAnnotation` renders inside generated content or DataGrid cells | Keyboard and accessibility tests run | Dev-mode annotation controls do not create nested interactive-control traps in normal generated output, and any DEBUG-only limitation is documented with focused evidence. |
| AC6 | `FcDevModeToggleButton` renders | The UI is inspected or tested | The literal `i` placeholder is replaced with a stable Fluent icon path or explicitly accepted with visual/specimen evidence and an owner. |
| AC7 | Dev-mode overlay text renders in English and French contexts | Localization tests run | `DevModeStrings.resx` and `DevModeStrings.fr.resx` coverage is complete for overlay/toggle/starter-template UI strings, or missing strings are split with a concrete owner. |
| AC8 | `AddFrontComposerDevMode()` is called after an `IHostEnvironment` factory registration | Registration tests run | The no-arg overload either resolves factory-registered environments safely or keeps the fail-closed behavior with explicit docs/tests steering adopters to the explicit overload. |
| AC9 | `DevModeOverlayController` selection changes while JS interop or async continuations are active | Tests or analysis run | Selection and node mutation behavior is either locked/serialized or accepted as Blazor single-threaded scoped behavior with evidence. |
| AC10 | `RazorEmitter.EmitStarterTemplate` traverses component-tree metadata | Hostile or cyclic tree tests run | Emission is bounded by depth, fan-out, and visited-set behavior so cyclic or repeated child graphs cannot hang the overlay. |
| AC11 | Starter template names are generated for generic projection types | Emission tests run | Generic arity and type arguments cannot collide in generated component names, or the limitation is accepted with examples and owner. |
| AC12 | Dev-mode annotations are injected into generated surfaces | SourceTools snapshot or bUnit tests run | Annotation seams cover at least one Story 6.1 annotation, one Story 6.3 slot override, and one Story 6.4 full replacement path in Counter or an equivalent bounded fixture. |
| AC13 | Component-tree metadata becomes stale across a parent re-render | Dev-mode tests run | Re-registration is epoch-aware and selection either persists correctly or emits/records HFC1049-compatible stale behavior with no misleading starter source. |
| AC14 | Projection override, slot, or template descriptors are rejected, ambiguous, or stale | Dev tooling or tests inspect registries | Dev tooling can enumerate actionable rejected/ambiguous descriptors, or the lack of a public rejected-descriptor surface is accepted with a narrow rationale. |
| AC15 | `ProjectionViewOverrideRegistry` validates component compatibility | Malformed descriptor tests run | `MakeGenericType`, malformed packed versions, duplicate registrations, and component compatibility failures are bounded to the descriptor and cannot crash unrelated descriptors unless the hard-fail is deliberate. |
| AC16 | Contract-version packing is shared by templates, slots, and view overrides | Contract tests run | A shared helper/parser or equivalent tests prevent malformed packed versions and major/minor/build drift from being interpreted inconsistently. |
| AC17 | Counter sample fixtures are used as release evidence | Sample tests run | The sample proves one valid Level 2/3/4 path, one stale/contract-drift path, one accessibility-warning path, and one runtime-fault path, or records why a narrower test fixture is sufficient. |
| AC18 | Counter sample accessibility is assessed | bUnit or specimen tests run | `aria-labelledby`, non-interactive `aria-label`, forced-colors, reduced-motion, focus, live-region, and role semantics are either fixed or explicitly accepted with evidence. |
| AC19 | Counter sample CSS and markup are reviewed | Sample build/tests run | Inline styles, unstable Fluent design tokens, and CSP-sensitive sample patterns are fixed or documented as sample polish with no production contract impact. |
| AC20 | Counter sample analyzer and package references are inspected | Build/package hygiene tests run | Analyzer references use central package and `PrivateAssets` discipline, and the sample either teaches NuGet-style consumption or explicitly documents project-reference-only local development. |
| AC21 | Existing visual/accessibility specimen routes and manifest are assessed | Playwright specimen lane runs or is intentionally skipped | Required specimen entries, theme/density combinations, artifact names, route roots, and redaction rules remain deterministic; missing RTL/zoom/localization matrices are named deferrals. |
| AC22 | Runtime culture, localization, or RTL can affect generated labels, slot context, or sample output | Tests or docs are updated | Machine contracts remain invariant; user-facing labels either flow through localization-safe paths or are recorded as v1 constraints with Story 9.5/UX owner. |
| AC23 | Deferred rows reference SourceTools emitter behavior needed for Shell UX | Implementation proceeds | Only Shell-visible UX or sample evidence is changed here; broad generator drift, diagnostic registry, or schema-fingerprint behavior stays with Story 11.4/11.2/11.5. |
| AC24 | Validation completes | Story 11.6 moves to review | The Dev Agent Record lists commands, outcomes, touched files, unresolved accepted constraints, split rows, and evidence paths. |
| AC25 | The 287-row inventory is reconciled | The ledger and story evidence are updated | Every Story 11.6 row has exactly one final classification: fixed-in-11.6, accepted-with-risk, split-to-named-story, superseded, blocked, or non-action, with owner, rationale, evidence path or split destination, and no silent row deletion. |
| AC26 | Dev-mode is disabled or not registered | Shell and generated surfaces render | No dev-mode annotation, localized dev-mode string load, keyboard shortcut, focus target, overlay registration, or starter-template affordance leaks into normal production surfaces unless explicitly enabled for development. |
| AC27 | Dev-mode annotations wrap or sit near interactive generated content | Keyboard and accessibility tests run | The annotation path creates no nested interactive role, preserves tab order, leaves Enter/Escape behavior unchanged, avoids focus traps, and exposes an accessible activation name only when the development affordance is active. |
| AC28 | Blazor Auto hosts register dev-mode services | Server, WASM, and Auto-mode registration paths are reviewed or tested | `IHostEnvironment` resolution is supported or fails closed with documented guidance for explicit registration; no client-side path depends on an unavailable server-only abstraction. |
| AC29 | Starter-template and dev-mode source generation changes are made | SourceTools/Shell generator tests run | Cycle detection, depth/fan-out bounds, generic arity, invalid names, collisions, unsafe characters, namespace/path sanitization, and byte-stable deterministic output are covered or explicitly recorded as not impacted. |
| AC30 | Customization registry version parsing is touched | Registry contract tests run | Templates, slots, and view overrides use one shared packed-version parser or an equivalent parity fixture set covering malformed, duplicate, incompatible, and unreachable generic-component descriptors. |
| AC31 | Evidence artifacts are produced for samples, specimens, or ledger reconciliation | Redaction validation runs | Outputs are bounded and reject local absolute paths, home-directory aliases, environment/user/tenant identifiers, tokens, cookies, stack traces, raw DOM dumps, and unbounded snippets; allowed fields remain route, rule, impact, selector, artifact path, owner, rationale, and truncation markers. |
| AC32 | The 287-row Story 11.6 inventory is converted into implementation evidence | The implementer creates or updates the row-to-evidence matrix | The matrix reconciles deferred-row count, aliases, duplicate references, source review labels, final classification, AC coverage, evidence path, and split owner; any count mismatch blocks review until explained. |
| AC33 | Dev-mode overlay dependencies are missing, denied, stale, or unavailable during render | Shell bUnit or bounded analysis evidence runs | Missing localization resources, unavailable JS interop, clipboard denial, stale component metadata, and unregistered dev-mode services fail closed without breaking generated content or production Shell navigation. |
| AC34 | User-facing labels, descriptor metadata, or starter-template source can contain hostile text | Sanitization tests or review evidence runs | Razor comment terminators, HTML/script fragments, raw local paths, namespace-breaking characters, bidi controls, user/tenant strings, and oversized snippets are escaped, rejected, or truncated before display, generated source, or evidence output. |
| AC35 | Counter sample or specimen evidence is used to close a row | Evidence is recorded | Each artifact maps to at least one AC and one deferred row, names the validation command or `not impacted` reason, and has no orphan screenshots, raw DOM dumps, or unexplained baseline updates. |
| AC36 | A Story 11.6 row could be fixed, accepted, or split | The implementer chooses the final classification | The decision uses a lightweight score covering adopter-visible impact, accessibility/security/release risk, implementation cost, test cost, and story adjacency; medium or high accessibility/release risk cannot be accepted without a named product or UX owner. |
| AC37 | Visual, localization, or accessibility baseline output changes | Review evidence is produced | Baseline updates name viewport, theme, density, culture, direction, reduced-motion or forced-colors state when relevant, and explain why the change is intentional instead of snapshot churn. |

---

## Tasks / Subtasks

- [ ] T1. Inventory and classify Story 11.6 deferred rows (AC1-AC4, AC24)
  - [ ] Read `_bmad-output/implementation-artifacts/deferred-work.md` from top to bottom before code changes.
  - [ ] Capture all rows with `Owner: Story 11.6`, preserving canonical row IDs, aliases, related stories, evidence paths, and source review labels.
  - [ ] Group rows into dev-mode overlay, customization registries/contracts, SourceTools-to-Shell emission, Counter sample, visual/accessibility specimen, localization/RTL, and adjacent handoff buckets.
  - [ ] Create a starting row-to-evidence matrix naming the intended outcome for each high-value row: fix now, accept, split, supersede, or block.
  - [ ] Use the canonical classification vocabulary from AC25 for every Story 11.6 row; do not invent equivalent final-state labels.
  - [ ] Reconcile the starting and final Story 11.6 row counts against the 287-row bucket; record any alias or duplicate-row collapse explicitly instead of hiding it in prose.
  - [ ] Add AC and evidence-path columns to the row-to-evidence matrix so every final classification can be audited back to acceptance criteria.
  - [ ] For split rows, name the exact destination story or product/UX owner and explain why the row is outside Story 11.6.
  - [ ] For every accepted row, record likelihood, impact, release risk, owner, revisit trigger, and validation evidence.
  - [ ] Score fix/accept/split choices before implementation using adopter-visible impact, accessibility/security/release risk, implementation cost, test cost, and story adjacency.
  - [ ] Preserve historical review text; append resolution notes rather than rewriting or deleting old rows.

- [ ] T2. Harden dev-mode overlay accessibility, localization, and registration (AC5-AC9, AC13)
  - [ ] Evaluate `FcDevModeAnnotation` nested-button behavior inside generated surfaces; fix with non-interactive marker plus separate activation path, relocated control surface, or document DEBUG-only acceptance with bUnit evidence.
  - [ ] Add a disabled/prod-mode guardrail test or explicit evidence note proving annotations, shortcuts, localized dev-mode strings, overlay registrations, and starter affordances do not leak when dev-mode is not enabled.
  - [ ] Verify annotation activation does not create nested interactive roles, tab-order instability, Escape/Enter collisions, or focus traps around generated buttons, links, grids, and command surfaces.
  - [ ] Replace `FcDevModeToggleButton` literal `i` with a Fluent icon from `FcFluentIcons`, or record why visual/specimen evidence permits deferral.
  - [ ] Add or complete `DevModeStrings.resx` and `DevModeStrings.fr.resx` keys for toggle, overlay, drawer, copy, stale, unsupported, and starter-template messages.
  - [ ] Test localized fallback behavior without depending on developer machine culture, using a bounded EN/FR plus representative RTL/LTR proof for touched Shell/dev-mode text only.
  - [ ] Revisit `AddFrontComposerDevMode()` factory-registered `IHostEnvironment` behavior; either support it or add explicit fail-closed tests/docs.
  - [ ] Clarify the Blazor Auto registration contract for server, WASM, and Auto-mode hosts before changing service lifetimes or environment lookup.
  - [ ] Prove missing localizer resources, JS interop denial, clipboard failure, and unavailable browser APIs do not prevent the primary generated content from rendering.
  - [ ] Decide whether `DevModeOverlayController` selection mutations need a private lock or an explicit Blazor-scoped single-threaded acceptance.
  - [ ] Make `FcDevModeAnnotation.OnParametersSet` epoch-aware for same-key/new-epoch metadata or record the HFC1049 stale-selection behavior, naming the mutation scenario, invalidated state, and regression test.

- [ ] T3. Harden starter-template emission and component-tree contracts (AC10-AC13)
  - [ ] Add cyclic/repeated child graph tests for `RazorEmitter.AppendNode`; enforce visited-set, depth, and fan-out behavior.
  - [ ] Add generic-arity-aware short type names for starter component names, including examples with arity-1 and arity-2 generic projections sharing a simple name.
  - [ ] Keep generated starter source deterministic, timestamp-free, local-path-free, and sanitized for `*/`, Razor comment terminators, raw paths, invalid identifiers, namespaces, and user/tenant strings.
  - [ ] Add hostile metadata fixtures for descriptor labels, display labels, diagnostic text, namespaces, and starter-template comments; escape, reject, or truncate unsafe values before source or evidence emission.
  - [ ] Keep SourceTools generator tests separate from Shell UI tests: run unit/golden output checks only when starter-template or generated annotation seams change.
  - [ ] Expand SourceTools dev-mode annotation seams where bounded: DataGrid columns, empty-state body, Level 3 slot dispatch, and Level 4 view-override dispatch.
  - [ ] Populate dev-mode metadata fields that implementers need: `HasActiveOverride`, `DiagnosticId`, `Role`, `CurrentLevel`, and stale reasons.
  - [ ] Add bUnit or Counter smoke coverage for overlay activation, annotation appearance, red-dashed class, starter copy, stale message, and clipboard recovery where JS interop doubles exist.

- [ ] T4. Reconcile customization registry and contract-version evidence (AC14-AC16)
  - [ ] Decide whether `IProjectionViewOverrideRegistry` needs a public rejected/ambiguous descriptor enumeration for dev tooling.
  - [ ] Add bounded tests around `ProjectionViewOverrideRegistry` malformed packed versions, invalid component descriptors, and unreachable `MakeGenericType` failure behavior.
  - [ ] Extract a shared packed-contract-version helper across templates, slots, and view overrides, or add a parity fixture proving equivalent behavior for every malformed-version case.
  - [ ] Document the intentional eager enumeration ordering for `ProjectionSlotRegistry` and the test-base `AddSingleton` override behavior if left as-is.
  - [ ] Review `ProjectionTemplateAssemblySource` defensive-copy behavior and either fix it symmetrically with slot descriptor sources or record a bounded acceptance.

- [ ] T5. Close Counter sample and customization-gradient evidence gaps (AC17-AC20)
  - [ ] Add or explicitly accept the four Counter sample fixtures named by prior review: valid Level 2/3/4 path, stale-contract/contract-drift path, accessibility-warning path, and runtime-fault path.
  - [ ] Treat Counter sample changes as adopter-facing evidence: each fixture must show what a new adopter sees, copies, configures, or validates.
  - [ ] Maintain an evidence map from Counter fixtures and specimen artifacts back to AC IDs and deferred row IDs; reject orphan baselines or screenshots that do not close a named row.
  - [ ] Confirm sibling projection surfaces continue rendering when a Level 4 replacement faults, or document why existing ErrorBoundary evidence is enough.
  - [ ] Clarify `Context.FieldRenderer` unknown-field behavior through docs/tests or split to Story 9.5 if this story does not touch docs.
  - [ ] Review Counter slot/template markup for `aria-labelledby`, non-interactive `aria-label`, inline styles, strict CSP, and unstable Fluent design-token usage.
  - [ ] Decide whether sample-level repeated render, culture/density/read-only, `@key`, and throwing-slot tests are required or whether Shell-level tests already cover them.
  - [ ] Split non-blocking visual design polish or broad sample redesign to a named owner; keep only coverage needed for Story 11.6 release evidence.
  - [ ] Tighten deterministic test evidence where low cost: avoid random GUID correlation IDs and assertion blocks that obscure first failure.
  - [ ] Ensure sample analyzer references keep `PrivateAssets` discipline and do not teach a package consumption pattern that conflicts with release packaging guidance.

- [ ] T6. Review visual/accessibility specimen and localization/RTL scope (AC18, AC21-AC22)
  - [ ] Inspect `tests/e2e/specimens/frontcomposer-specimen-manifest.json` and `tests/e2e/specs/specimen-accessibility.spec.ts` before changing visual or accessibility behavior.
  - [ ] Keep existing Light/Dark x Compact/Comfortable/Roomy baselines deterministic if touched.
  - [ ] Add missing manifest ownership or route checks if specimen gaps are directly in Story 11.6 scope.
  - [ ] Record full RTL, broader zoom, and cross-assistive-technology matrices as named deferrals unless this story can produce stable evidence; require only a representative RTL/LTR proof for touched user-facing Shell/dev-mode text.
  - [ ] Keep artifact output bounded and redacted: route, rule, impact, selector, artifact path, and truncation markers only; no full DOM dumps, tokens, cookies, local paths, or environment secrets.
  - [ ] When a visual or accessibility baseline changes, record viewport, theme, density, culture, direction, reduced-motion or forced-colors state when relevant, and the intentional reason for the update.
  - [ ] Document culture-sensitive generated labels versus invariant machine contracts. Do not let localized display text become schema, diagnostic, or agent contract input.

- [ ] T7. Validate, reconcile, and record evidence (AC1, AC3, AC21, AC24)
  - [ ] Run focused Shell tests first:
    `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~DevMode|FullyQualifiedName~ProjectionViewOverride|FullyQualifiedName~ProjectionSlot|FullyQualifiedName~ProjectionTemplate|FullyQualifiedName~CounterStoryVerification"`
  - [ ] Run SourceTools emitter tests only if dev-mode annotation emission or starter-template source generation changes:
    `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "FullyQualifiedName~DevModeAnnotation|FullyQualifiedName~RazorEmitter|FullyQualifiedName~ProjectionTemplate"`
  - [ ] Run Playwright specimen checks only if specimen routes, manifest, visual baselines, or axe behavior changes:
    `npm --prefix tests/e2e test -- --grep @specimen`
  - [ ] Run a bounded forbidden-token/redaction scan across updated evidence surfaces before review, including docs, samples, specimen artifacts, ledger evidence, and generated starter-template output.
  - [ ] Run a consistency check across the story artifact, deferred ledger, evidence matrix, specimen manifest, and Dev Agent Record; fail review on missing AC links, missing row IDs, or unexplained count drift.
  - [ ] Record `not impacted` for SourceTools emitter tests, Playwright specimen checks, and advisory performance/nightly/visual/palette/quarantine lanes when their owned surfaces are not touched.
  - [ ] Update `_bmad-output/implementation-artifacts/deferred-work.md` with row-scoped resolution evidence.
  - [ ] Update this story's Dev Agent Record with commands, outcomes, file list, accepted constraints, split rows, and residual risks.

---

## Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Story 11.6 owns Shell-visible UX, dev-mode, accessibility/specimen, localization/RTL, and sample evidence, not broad SourceTools, registry, MCP, EventStore, or release workflow governance. | Keeps Epic 11 stories independently implementable. |
| D2 | The deferred ledger is the source of truth for this story's scope. | The story exists to close scattered review notes; silent row loss is a release-readiness failure. |
| D3 | Counter remains the canonical sample unless a row proves Counter cannot express the behavior. | Prior Epic 6 stories intentionally avoided sample-domain sprawl. |
| D4 | Accessibility behavior is contractual even for customization samples. | Generated/customized UI must preserve labels, keyboard, focus, live-region, reduced-motion, and forced-colors behavior. |
| D5 | Dev-mode overlay is DEBUG/development-only, but it still needs bounded accessibility and localization evidence. | Development tools shape adopter behavior and can hide broken customization guidance. |
| D6 | Visual/specimen scope must remain deterministic and redacted. | Artifact churn or local-environment leakage would weaken release confidence. |
| D7 | Localization/RTL broad matrices are accepted only when named with owner and evidence. | Vague "future localization" notes are not actionable release constraints. |
| D8 | Starter-template output is developer-facing generated source and must be deterministic, sanitized, and collision-resistant. | Developers copy this source into real projects; bad output becomes adopter code. |
| D9 | Runtime machine contracts remain invariant even when user-facing labels are localized. | Prevents localized prose from drifting schema, diagnostics, or agent contracts. |
| D10 | Accepted constraints must include likelihood, impact, owner, evidence, and reopen trigger. | "Low priority" closure is not sufficient for release readiness. |
| D11 | The 287-row inventory is a reconciliation artifact, not permission to resolve all historical backlog inside Story 11.6. | Prevents the story from becoming an unbounded deferred-work cleanup project. |
| D12 | Fix-vs-split decisions are made by adopter-visible Shell/sample impact first, then by implementation adjacency. | Keeps low-value internal seams from crowding out release-readiness outcomes. |
| D13 | Dev-mode affordances must be absent by default outside enabled development flows. | Development tooling must not change production keyboard, focus, localization, or generated surface behavior. |
| D14 | Blazor Auto service registration must be explicit about server, WASM, and Auto-mode environment availability. | Host-environment assumptions are easy to get wrong across render modes. |
| D15 | SourceTools starter-template contracts are validated with generator-focused tests separate from Shell UI tests. | Keeps failures local and avoids conflating generated-source determinism with runtime UX behavior. |
| D16 | Accessibility/localization evidence is representative and targeted, not a broad matrix expansion. | Preserves L07 test budget while still proving user-facing release risk is bounded. |
| D17 | The row-to-evidence matrix is the implementation control surface for Story 11.6. | A prose-only summary cannot safely close 287 routed rows without count drift or orphan evidence. |
| D18 | Dev-mode dependency failures must preserve generated content first. | Developer tooling is optional; generated Shell surfaces and production navigation must remain usable when tooling dependencies fail. |
| D19 | Descriptor metadata, labels, and starter-template text are treated as untrusted evidence/source inputs. | Generated source and review artifacts can leak or execute hostile text unless sanitized before emission. |
| D20 | Counter fixtures prove adopter workflows, not broad sample redesign. | Keeps the story focused on release evidence and avoids absorbing product polish that belongs to docs or UX owners. |
| D21 | Fix, accept, and split choices require lightweight scoring before implementation. | L06 and L07 require a defensible budget when many low-value rows compete with high-risk accessibility and release-readiness gaps. |
| D22 | Evidence cannot close a row unless it links row ID, AC, command or not-impacted reason, and redaction status. | Prevents screenshots, baselines, or notes from looking complete while failing to prove the release-readiness claim. |

### Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor` | Update likely | Nested interactive control behavior, epoch-aware registration, accessible names. |
| `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor` | Update likely | Replace literal icon placeholder and localized text coverage. |
| `src/Hexalith.FrontComposer.Shell/Components/DevMode/*.razor.css` | Update possible | Reuse existing placeholder/dev hooks; avoid strict-CSP surprises where possible. |
| `src/Hexalith.FrontComposer.Shell/Resources/DevMode/*` | Update likely | Complete EN/FR resource coverage and fallback behavior. |
| `src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs` | Update possible | Factory-registered `IHostEnvironment` handling or explicit fail-closed tests. |
| `src/Hexalith.FrontComposer.Shell/Services/DevMode/*` | Update likely | Starter-template emitter cycle/arity behavior, overlay controller selection behavior, clipboard recovery tests. |
| `src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/*` | Update possible | Rejected descriptor visibility, malformed version, descriptor failure containment. |
| `src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/*` | Update possible | Ordering/lifetime docs/tests; descriptor source discipline. |
| `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/*` | Update possible | Defensive-copy symmetry and template descriptor evidence. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` | Update possible | Dev-mode annotation seam expansion and slot/sample metadata if in scope. |
| `samples/Counter/Counter.Web/**` | Update likely | Counter sample fixtures, accessibility/sample polish, analyzer reference hygiene. |
| `tests/Hexalith.FrontComposer.Shell.Tests/**` | Update likely | Focused bUnit and Counter sample evidence. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/**` | Update possible | Dev-mode annotation/starter-template emission snapshots. |
| `tests/e2e/specimens/frontcomposer-specimen-manifest.json` | Update possible | Specimen ownership/route/manifest checks if visual/a11y scope changes. |
| `tests/e2e/specs/specimen-accessibility.spec.ts` | Update possible | Accessibility and visual specimen checks only when needed. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update | Mark Story 11.6 rows resolved/accepted/split after implementation. |
| `_bmad-output/implementation-artifacts/11-6-shell-ux-accessibility-and-sample-coverage-follow-ups.md` | Update | Dev Agent Record, validation, file list, completion notes. |

### Project Structure Notes

- Shell runtime code lives under `src/Hexalith.FrontComposer.Shell`; keep dev-mode and customization registry changes inside existing folders.
- Contract changes under `src/Hexalith.FrontComposer.Contracts` are high blast-radius and require contract tests plus package compatibility evidence.
- SourceTools still targets `netstandard2.0`; do not add Shell/Fluent UI/Fluxor runtime dependencies to SourceTools.
- E2E tests live under `tests/e2e`; use accessible role/label or `data-testid` selectors, not CSS class selectors or arbitrary text.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`; do not initialize or update nested submodules.

### Testing Strategy

- Run the focused Shell slice first:
  - `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~DevMode|FullyQualifiedName~ProjectionViewOverride|FullyQualifiedName~ProjectionSlot|FullyQualifiedName~ProjectionTemplate|FullyQualifiedName~CounterStoryVerification"`
- Run focused SourceTools emitter tests only when generated dev-mode annotations or starter-template source change:
  - `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "FullyQualifiedName~DevModeAnnotation|FullyQualifiedName~RazorEmitter|FullyQualifiedName~ProjectionTemplate"`
- Run E2E specimen checks only for specimen/visual/a11y route changes:
  - `npm --prefix tests/e2e test -- --grep @specimen`
- Run a redaction scan across changed docs, snapshots, E2E artifacts, and ledger evidence.
- For final release-confidence, run the main lane if time allows:
  - `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Stories 2.2, 3.4, 3.5 | Story 11.6 | Command-form, palette, home/shell UX follow-ups are either fixed, accepted, or routed with evidence. |
| Story 4.6 | Story 11.6 | Unsupported-field placeholder and generated rendering accessibility hooks remain visible and testable. |
| Stories 6.1-6.5 | Story 11.6 | Customization annotations, templates, slots, view overrides, and dev-mode overlay remain coherent as one gradient. |
| Story 10.2 | Story 11.6 | Visual/accessibility specimen scope is preserved; broader RTL/zoom/localization matrices are named deferrals. |
| Story 11.1 | Story 11.6 | Routed deferred rows must close with evidence or explicit accepted constraints. |
| Story 11.2 | Story 11.6 | Diagnostic registry/docs governance remains separate; Shell diagnostic IDs may be referenced but not re-governed here. |
| Story 11.4 | Story 11.6 | Broad SourceTools drift/generator hardening remains separate; only Shell-visible dev-mode/sample emission is in scope. |
| Story 11.5 | Story 11.6 | MCP/agent contracts remain separate; UI display-label parity is Story 11.6 only when it is not schema material. |
| Story 11.7 | Story 11.6 | EventStore, telemetry exporter, CI/release, and submodule governance remain separate unless a Shell UX sample needs a narrow handoff. |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Diagnostic registry schema, HFC release tracking, and docs stub governance. | Story 11.2 |
| CLI/IDE migration and help/reference semantics. | Story 11.3 |
| Broad SourceTools drift/generator diagnostics and snapshot governance. | Story 11.4 |
| MCP schema negotiation, agent categories, and schema fingerprint material. | Story 11.5 |
| EventStore reliability, telemetry/exporter guidance, CI race, release credentials, and release workflow governance. | Story 11.7 |
| Diataxis adopter docs for field-renderer unknown-field contract and package-consumption guidance. | Story 9.5 |
| Full RTL, cross-AT, broad localization, and zoom visual matrices beyond current deterministic specimen scope. | Product/UX accessibility roadmap after Story 11.6 evidence |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-11-deferred-hardening-release-readiness.md#Story-11.6`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md`] - Story 11.6 routed row bucket and row-level evidence.
- [Source: `_bmad-output/implementation-artifacts/6-3-level-3-slot-level-field-replacement.md`] - slot-context, Counter sample, and customization-gradient follow-ups.
- [Source: `_bmad-output/implementation-artifacts/6-4-level-4-full-component-replacement.md`] - Level 4 replacement accessibility, fallback, and Counter evidence.
- [Source: `_bmad-output/implementation-artifacts/6-5-fcdevmodeoverlay-and-starter-template-generator.md`] - dev-mode overlay and starter-template implementation baseline.
- [Source: `_bmad-output/implementation-artifacts/10-2-accessibility-ci-gates-and-visual-specimen-verification.md`] - specimen, accessibility, visual baseline, and RTL/localization deferrals.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#FcDevModeOverlay`] - UX shape for dev-mode overlay, annotation, and starter-template generator.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md`] - accessibility and responsive behavior matrix.
- [Source: `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor`] - current annotation markup and registration behavior.
- [Source: `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor`] - current toggle markup and localization fallback.
- [Source: `src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs`] - current dev-mode registration behavior.
- [Source: `src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs`] - current starter-template emitter.
- [Source: `src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs`] - current Level 4 registry behavior.
- [Source: `tests/e2e/specimens/frontcomposer-specimen-manifest.json`] - current specimen manifest.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope and decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08--Party-review-vs-elicitation--different-roles`] - this story should receive later party review and elicitation hardening.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - owner specificity requirement.
- [Source: `_bmad-output/project-context.md`] - project rules for Shell, accessibility, tests, generated output, evidence redaction, and submodules.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

- 2026-05-11: Story created via `/bmad-create-story 11-6-shell-ux-accessibility-and-sample-coverage-follow-ups` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-11T22:04:33+02:00: Party-mode review applied via `/bmad-party-mode 11-6-shell-ux-accessibility-and-sample-coverage-follow-ups; review;` with Winston, Amelia, John, and Murat. Added ledger classification, dev-mode leakage, nested interaction, Blazor Auto registration, starter-template, packed-version, evidence redaction, test-scope, and representative accessibility/localization guardrails.
- 2026-05-11T23:08:37+02:00: Advanced elicitation applied via `/bmad-advanced-elicitation 11-6-shell-ux-accessibility-and-sample-coverage-follow-ups`. Added row-evidence reconciliation, fail-closed dev-mode dependency, hostile metadata sanitization, evidence mapping, classification scoring, and baseline-intent guardrails.

### Change Log

- 2026-05-11: Created Story 11.6 and marked ready-for-dev.
- 2026-05-11T22:04:33+02:00: Party-mode review hardening applied; added AC25-AC31, Decisions D11-D16, and task guardrails for inventory final states, dev-mode production absence, Shell accessibility, Blazor Auto registration, SourceTools/UI test split, Counter evidence scope, specimen redaction, and validation lane gating.
- 2026-05-11T23:08:37+02:00: Advanced elicitation hardening applied; added AC32-AC37, Decisions D17-D22, and task guardrails for row-count reconciliation, evidence traceability, fail-closed tooling dependencies, hostile metadata sanitization, fixture/specimen evidence mapping, scored fix/accept/split decisions, and intentional baseline updates.

### File List

- `_bmad-output/implementation-artifacts/11-6-shell-ux-accessibility-and-sample-coverage-follow-ups.md`

## Party-Mode Review

- ISO date and time: 2026-05-11T22:04:33+02:00
- Selected story key: `11-6-shell-ux-accessibility-and-sample-coverage-follow-ups`
- Command/skill invocation used: `/bmad-party-mode 11-6-shell-ux-accessibility-and-sample-coverage-follow-ups; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary: The review agreed Story 11.6 is close to development readiness but had too many broad surfaces without enough execution gates. Key risks were an unbounded 287-row inventory, unclear fix-vs-split criteria, dev-mode affordances leaking into normal Shell behavior, nested interactive annotation traps, Blazor Auto `IHostEnvironment` ambiguity, SourceTools starter-template requirements being mixed into UI validation, vague localization/RTL proof, Counter sample scope creep, and insufficient redaction/specimen validation rules.
- Changes applied: Added AC25-AC31; added Decisions D11-D16; tightened tasks for canonical ledger final states, story-specific split owners, prod-disabled dev-mode absence, annotation focus/keyboard behavior, bounded EN/FR plus representative RTL/LTR proof, Blazor Auto registration, epoch mutation scenario naming, generator-focused starter-template tests, shared packed-version parser parity, adopter-facing Counter evidence, sample split rules, specimen redaction, and lane gating with explicit `not impacted` notes.
- Findings deferred: Full RTL, broad zoom, cross-assistive-technology matrices, broad sample redesign, diagnostic governance, CLI/IDE behavior, broad SourceTools drift, MCP schema, EventStore reliability, CI/release workflow, and advisory performance/nightly/visual/palette/quarantine lane expansion remain outside Story 11.6 unless a row is split to a named owner. No product-scope or architecture-policy change was applied beyond clarifying the existing release-readiness guardrails.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- ISO date and time: 2026-05-11T23:08:37+02:00
- Selected story key: `11-6-shell-ux-accessibility-and-sample-coverage-follow-ups`
- Command/skill invocation used: `/bmad-advanced-elicitation 11-6-shell-ux-accessibility-and-sample-coverage-follow-ups`
- Batch 1 method names: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records
- Findings summary: The elicitation found that the party-mode hardening made the story reviewable, but implementation could still fail by closing a 287-row bucket with prose-only evidence, accepting risky accessibility rows without scoring, leaking hostile metadata into starter-template or review artifacts, letting optional dev-mode dependencies break generated content, and updating specimen baselines without enough intent metadata.
- Changes applied: Added AC32-AC37; added Decisions D17-D22; tightened tasks for row-count reconciliation, row-to-AC evidence traceability, scored fix/accept/split classification, fail-closed localizer/JS/clipboard/browser dependency behavior, hostile descriptor/display-label/source sanitization, Counter/specimen evidence mapping, cross-artifact consistency checks, and intentional baseline metadata.
- Findings deferred: Full broad localization, broad RTL, cross-assistive-technology matrices, sample redesign, new architecture policy for registries, and broad SourceTools diagnostic governance remain deferred to the named owners already listed in the story unless a specific row is scored and split during implementation. No product-scope or cross-story contract change was applied.
- Final recommendation: ready-for-dev
