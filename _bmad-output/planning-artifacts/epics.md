---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
  - step-04-final-validation
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-design.md
  - _bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-09-09/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-09-09/EXPERIENCE.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-09-22.md
updated: 2026-09-23
numberingNote: >-
  Epics 1-11 are completed delivery history restored from epics.md at commit
  aeff9f83 with the approved 2026-09-22 section 9.4 annotations. The 2026-09-22
  sprint-change-proposal backlog is numbered after the current sequence as
  Epics 12-17. ux-design.md is the canonical UX authority; the 2026-09-09
  DESIGN.md/EXPERIENCE.md pair is supplementary.
---

# frontcomposer - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for frontcomposer, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

> **Numbering and delivered history (2026-09-23).** Epics 1–11 are completed delivery history and
> keep their original numbers, stories, and acceptance criteria in the Delivered History sections
> below, before Epic 12. New work from `sprint-change-proposal-2026-09-22.md` §7 is numbered after
> the current sequence as Epics 12–17 (Stories 12.1–17.16). Requirement, NFR, UX-DR, and AR
> identifiers used inside Epics 1–11 refer to the Historical inventory in the Delivered History
> section; identifiers used in Epics 12–17 refer to the Requirements Inventory below. The canonical
> UX authority for Epics 12–17 is `ux-design.md`; the 2026-09-09 `DESIGN.md`/`EXPERIENCE.md` pair
> supplements it.

## Requirements Inventory

### Functional Requirements

FR1: For each valid projection type, generate the projection view, Fluxor feature/actions/reducers, and registration artifacts; emit the governed diagnostic for invalid declarations and render Loading, Empty, and Data by ProjectionRole.

FR2: For each valid command type, generate its form, lifecycle, renderer, registration, subscriber, bridge, and optional FullPage route artifacts; fail invalid constructor, MessageId, or CommandTarget declarations with the governed diagnostics.

FR3: Support the documented attribute vocabulary for projection roles, bounded contexts, badges, column priority, field groups, empty-state actions, confirmation, policies, derived fields, icons, relative time, currency, display metadata, defaults, templates, and command targets, with synchronized behavior, diagnostics, snapshots, and documentation.

FR4: Select command density from the non-derivable property count: Inline for 0–1, CompactInline for 2–4, and FullPage for 5 or more, with generator tests and snapshots.

FR5: Resolve projection customization deterministically as Level 4 full-view override, Level 2 template, then generated default; compose Level 3 slots only through delegated generated renderers and surface governed accessibility/mismatch diagnostics.

FR6: Detect structural and metadata drift by comparing deterministic, bounded schema/generated material with opt-in checked-in baselines and emit the governed HFC1060–HFC1069 diagnostics.

FR7: Provide the validated AddHexalithFrontComposerQuickstart(), optional AddHexalithDomain<TMarker>(), and AddHexalithEventStore(...) bootstrap path; fail missing or misordered stages at startup, allow an empty shell, and preserve scoped lifetimes.

FR8: Render the complete Fluent shell frame with skip links, providers, header, one Module navigation entry per bounded context, content, footer, account access, keyboard shortcuts, deterministic route focus, and WCAG 2.2 AA reflow/zoom behavior.

FR9: Provide full-width and constrained FC-LYT modes, persisted theme/density preferences scoped through IStorageService, the exact 32px compact-grid metric, and correctly owned localized shell/domain strings.

FR10: Drive Module discovery, home cards, tabs, projection flyouts, routes, badges, counts, and authorization-aware command-palette entries from Domain Manifest data, using canonical routes, urgency ordering, one active navigation item, and deterministic tab/palette/dialog focus.

FR11: Render accessible projection grids with debounced/resettable filters, loading/empty/data and health states, expandable row details, column prioritization, virtualization, slow-query and max-item notices, semantic status cues, and deduplicated announcements.

FR12: Query EventStore over HTTP, subscribe over SignalR, expose reconnect/fallback/recovery state, never treat a nudge as command success, and recover automatically within the defined reconnect, restart, polling-lane, polling-interval, and notice-duration bounds.

FR13: Publish fresh-row indicators only through FC-NIP from an immutable pre-dispatch target identity and independently Material terminal result; suppress unknown, non-material, delete, rejected, review, or server-allocated-key outcomes; enforce tenant/user scope, first-wins identity, live invalidation, silent expiry, and observable resolution outcomes.

FR14: Validate, parse, and dispatch generated command forms; preserve useful input on retryable pre-accept failures; generate and reuse a ULID MessageId; link and focus client-validation summaries while keeping asynchronous server rejection as lifecycle feedback unless safely field-mapped.

FR15: Surface Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, NeedsReview, Warning, and Degraded without presenting transport acceptance as success, using the specified 10,000ms degradation, 1,000ms/120,000ms polling, zero pre-accept retry, and one 250ms transient-retry budgets.

FR16: Enforce policy authorization before and after BeforeSubmit plus at the service boundary, destructive confirmation, the 30-second form-abandonment guard, and FC-CNC one-at-a-time local execution with accessible blocked-submit feedback.

FR17: Expose every visible generated command as a dynamically listed MCP tool with descriptor-derived JSON schema, canonical naming, bounded acknowledgement output, and server-side injection of tenant, user, message, and correlation fields.

FR18: Expose descriptor-exact tenant-scoped projection resources and validated framework-global skill resources; serve only validated skill sections and fail oversized content closed rather than truncating it.

FR19: Require MCP tenant-tool and resource-visibility gates, compatible schema negotiation, host authentication, non-Development prohibition of AllowAll gates, exact opaque failure/disclosure shapes, and response/log redaction as defined by the request-class matrix.

FR19a: Preserve command lifecycle across MCP requests and DI scopes through singleton lifecycle state with a scoped tracker, including the opaque unknown-tool behavior for hidden or unknown lifecycle lookups.

FR20: Provide deterministic frontcomposer inspect text and frontcomposer.cli.inspect.v1 JSON output covering generated forms, grids, registrations, manifests, warnings, and errors, with filtering, fail flags, stable ordering, and relative paths.

FR21: Provide dry-run-by-default frontcomposer migrate planning and atomic apply for allowlisted Roslyn migration edges, refusing generated, submodule, symlinked, or out-of-root targets and emitting frontcomposer.cli.migrate.v1 JSON.

FR22: Provide a FrontComposer Testing package with a bUnit host, deterministic command/query/projection fakes, redacted evidence, builders, and assertions for success, failure, authorization, query variants, validation, announcements, focus, blocked submits, and fresh-row expiry.

FR23: Keep component, diagnostic, migration, and skill-corpus documentation synchronized with executable inventories, generated/runtime surfaces, the selected Fluent catalog, DocFX validation, and content-level parity checks.

FR24: Publish only an inventory-, consumer-, checksum-, SBOM-, symbols-, provenance-, and policy-validated exact package set through a secretless/read-only builder and a distinct protected candidate-free publisher; require a sealed publish_authorized manifest before side effects and append-only, attempt-total verification afterward.

FR25: Evolve public APIs, schemas, CLI JSON, generated-output paths, diagnostics, and analyzer policy intentionally through baselines, documentation, and migration/deprecation plans; keep AnalysisMode=Recommended and TreatWarningsAsErrors enabled.

FR26: Preserve completed FC-NIP producer/consumer composition through the approved base and successor contracts; never infer row identity from SignalR nudges or EventStore lifecycle data, while retaining the server-allocated-key case as an explicit non-goal.

FR27: Preserve the closed Epic 10 tooling-governance outcomes as regression traceability through FR20–FR23, NFR6, NFR10, synthetic/manual-only HFCM9002 evidence, and Testing redaction; create no new implementation work solely for FR27.

FR28: Preserve the closed generated-command-route and Contracts-kernel split decisions as regression traceability through FR2, FR10, NFR2, D-3, and D-5; create no new implementation work solely for FR28.

FR29: Maintain an operator- or adopter-visible outcome, fail-closed rule, and verification artifact for every architecture-review defect class, without reopening completed remediation rows.

FR29.1: Preserve completed sign-out token invalidation/eviction and prevent singleton capture of scoped authentication.

FR29.2: Preserve completed projection realtime recovery within FR12/NFR8 bounds so a circuit does not remain degraded after backend recovery.

FR29.3: Preserve completed MCP cross-request lifecycle behavior defined by FR19a.

FR29.4: Preserve completed direct coverage of ReturnPathValidator and the single StorageKeys builder.

FR29.5: Preserve completed generated-code hygiene through one literal-escaping implementation, one slug algorithm, linked stylesheets, and compiling nullable numeric fields.

FR29.6: Preserve the completed netstandard2.0-clean Contracts boundary, Contracts.UI split, analyzer activation, narrow audit exceptions, and exclusive logging ownership.

FR29.7: Bind the release candidate to one exact owner-approved FrontComposer/EventStore/Builds/package tuple with live Pact provider/AppHost evidence and separate named migration approval; prior identity records remain immutable history.

FR30: Scope every operator-facing query, subscription, count, pending state, and persisted preference to the resolved tenant and user; clear stale scope and present an explicit fail-closed state rather than querying or rendering empty-looking data.

### NonFunctional Requirements

NFR1: Build with .NET SDK 10.0.400 using latestPatch roll-forward, C# latest, the .slnx solution only, nullable and centralized package versions, TreatWarningsAsErrors=true, and AnalysisMode=Recommended with built-in analyzers only.

NFR2: Enforce dependency direction toward Contracts; SourceTools may reference only the UI-clean Contracts kernel, and net10/Fluent-only code in multi-targeted projects must be guarded.

NFR3: Meet WCAG 2.2 AA across generated and hand-authored UI, including names/roles, keyboard operation, deterministic focus, linked validation, deduplicated live regions, 320 CSS-pixel reflow, 400% zoom, resilient text spacing, target size, unobscured focus, reduced motion, and forced-colors meaning.

NFR4: Use FrontComposer and Blazor Fluent UI V5 components plus Fluent 2 tokens; forbid raw interactive controls and legacy Fluent V4/FAST tokens except documented carve-outs.

NFR5: Fail Shell and MCP security closed within FR19/FR30 guarantees; never accept server-controlled fields from clients and directly test return paths, storage keys, tenant/user scope, authentication state, and API-key handling.

NFR6: Prevent UI, logs, telemetry, MCP responses, evidence, and snapshots from exposing raw tokens, JWT payloads, EventStore metadata, raw event payloads, stack traces, or unrestricted PII.

NFR7: Treat canonical schema/evidence material, fingerprint algorithms, baseline identity, and provenance validation as byte-unique public contracts backed by cross-language hostile and golden vectors.

NFR8: Enforce FR15 command lifecycle budgets and FR12 realtime recovery bounds; expose and recover degraded/reconnecting/fallback states without turning transport or nudges into confirmation.

NFR9: Keep palette scoring, generated rendering, and cache-backed hot paths within the named benchmark suites and FcShellOptions caps; threshold changes require benchmark evidence and Release Owner approval.

NFR10: Use FrontComposerActivitySource, source-generated LoggerMessage sites gated after IsEnabled, and sanitized structured logs for operator-relevant failures, with proof that sensitive values are absent.

NFR11: Run the mandatory default, Governance, Contract, snapshot, PublicAPI, ApiCompat, Pact consumer/provider, property, semantic documentation, Epic 9 live-proof, and accessibility/e2e lanes; execute test projects individually and use .slnx only for restore/build.

NFR12: Permit only the protected candidate-free publisher to authenticate the candidate, establish provenance, seal and classify hexalith.release-evidence.v4, and authorize exact-byte publication; classify every attempt and keep append-only post-release evidence immutable.

NFR13: Establish shared-catalog compatibility from versioned semantic profiles and affected-module standalone Release/NuGet restore/build evidence over the exact depth-1/2 dependency graph; never use historical commit/fingerprint allowlists or recursively initialize nested submodules.

### Additional Requirements

- **Starter template:** Architecture specifies no greenfield starter template. This is a brownfield framework and remediation program, so Epic 12 Story 12.1 must not introduce a starter project unless a later approved requirement does so.
- Keep the UI-clean Contracts kernel on net10.0/netstandard2.0, place Blazor/Fluent rendering contracts in the net10-only Contracts.UI assembly, keep SourceTools netstandard2.0-clean, and preserve the documented consumer dependency direction.
- Enforce Shell folder/namespace architecture: Components may render, Routing stays pure, State never depends on Components, and concrete polling/background workers remain in Infrastructure; only the documented legacy ProjectionSchemaMismatchException exception is allowed.
- Keep Roslyn symbols inside the SourceTools parse stage and emit only pure equatable intermediate representation into transform/emit stages.
- Preserve the public generated-output path and byte-deterministic schema canonicalization using the pinned encoder, sentinel, source-generation context, and ordinal comparer.
- Preserve QueryRequest/ProjectionQuery ownership plus HFC0001/CS0618 flattened source and JSON compatibility through 2.x, with removal targeted only for 3.0.0.
- Use only root-declared external submodules and never recursively initialize, update, or execute nested submodules while collecting dependency evidence.
- Prove FR30 end to end through TEN-SCOPE-1 across production EventStore queries, subscriptions, counts, storage, pending state, and fresh-row rendering; stale or missing tenant identity must fail closed before old-scope data renders.
- Keep IPendingCommandOutcomeResolver as the single terminal pending-command owner and the only eligible fresh-row publisher; generated callbacks and infrastructure adapters may emit observations but may not mutate terminal state.
- Resolve command target identity only from explicit generated command-to-projection metadata with a typed target provider or declared SameAsSource snapshot; ambient rows, nudges, aggregate IDs, visible diffs, and untyped payloads are forbidden identity sources.
- Capture and validate exactly one immutable target snapshot before asynchronous dispatch, associate MessageId only after acceptance, and never overwrite capture time with terminal observation time.
- Keep terminal materiality independent and closed to Material, NoOp, or Unknown; suppress indicators for NoOp, Unknown, delete, Rejected, NeedsReview, and unknown identity, while retaining eligible material idempotent confirmation.
- Make every effective fresh-indicator add, dismiss, expiry, clear, filter/requery, and scope mutation observable to subscribed generated consumers; enforce scoped disposal and atomic first-wins identity by ViewKey/EntityKey.
- Preserve the Module/default-tab/projection-flyout information architecture and canonical generated-command route family across shell, palette, empty-state CTA, and direct navigation.
- Preserve the exact command timing contract: 10,000ms to Degraded, 1,000ms status polling for at most 120,000ms, zero pre-accept lifecycle retries, and one transient dispatch retry after 250ms.
- Collect hexalith.dependency-graph.v1 as the exact root gitlinks at depth 1 plus direct gitlinks from each root-selected commit at depth 2; record every edge before deduplication and reject deeper/unbounded interpretations.
- Resolve graph repositories through the root .gitmodules closed world, read explicit committed objects offline, acquire exact base/candidate objects into isolated temporary bare stores, and never clone or run candidate-supplied commands during graph collection.
- Treat eng/dependency-graph-policy.json from the active base/before revision as immutable executable authority; candidate policy changes activate only in a later change, and missing base policy fails closed with no bootstrap fallback.
- Require every Builds-selector owner to map to exactly one semantic profile and every target to exact standalone restore/build argv or an explicit evidence-only disposition; missing or ambiguous mappings fail closed.
- Materialize only the bounded safe regular-file Builds contract tree for edge-bound consumers, verify its graph hash, enforce the file/blob/total-size ceilings, and run affected-module restore/build in isolated Release/NuGet mode.
- Enforce the closed dependency-graph envelope, ordinal edge ordering, lowercase SHA-1 identities, strict duplicate/unknown-member rejection, canonical ASCII JSON encoding, bounded reads, and offline plus live digest verification.
- Implement FR24 as a secretless/read-only candidate builder followed by a distinct protected candidate-free publisher and an independent post-release verifier; pre-publication authorization and post-publication verification remain separate.
- Give the builder no environment, publication credentials, OIDC/attestation authority, or write scope; it may execute only the authenticated candidate to create and validate one closed publication-candidate artifact.
- Let only the protected publisher mutate NuGet or GitHub Release state; it must execute pinned active-policy-authorized owner code, treat candidate packages as non-executable data, independently validate provenance/fallback, and require publish_authorized=true before the first side effect.
- Keep GitHub package assets byte-identical to sealed candidates; for NuGet downloads allow only the valid root repository signature entry and require every other normalized ZIP member to remain byte-equivalent.
- Emit an always-uploaded authenticated release-verification handoff and a total attempt classification covering gate-frozen, no-releasable, rejected, compliant, deferred, missing-artifact, partial-publish, and other non-compliant outcomes.
- Keep release-ledger observations append-only and attempt-keyed; a rerun or later verification may append evidence but may never replace, weaken, or relabel an incident.
- Keep production releases halted while the legacy publication-capable path is selected; HEXALITH_RELEASE_PUBLISH_ENABLED=false is a deny-only emergency stop and cannot authorize publication.
- Close the GOV-1 split implementation gate only with the canonical conformance JSON, authenticated live checks, five review lenses, a closed nonconformance register, an unchanged evidence PR, and a distinct approval-projection PR; stale or mixed evidence, direct push, squash, or rebase fails the gate.
- Freeze each EventStore successor identity as an exact tuple of FrontComposer HEAD, EventStore gitlink, Builds gitlink, catalog package, provider/AppHost evidence, and artifact hashes; tuple drift creates a new record rather than rewriting history.
- Separate EVT-ID-1 evidence capture from EVT-APP-1 migration approval; require EventStore maintainer, FrontComposer maintainer, and Release Owner signatures, or first record an explicit Product/Architecture EVT-XFER-1 ownership transfer.
- Preserve the approved residual work boundaries: implementation precedes independent acceptance for MCP security; FrontComposer owns the adopter kit while the selected adopter owns external proof; upstream Builds acceptance, GOV implementation, evidence, and owner approval remain separate statuses.
- Treat the approved Section 7 aliases as stable handoff identifiers rather than final story numbers; numeric epic/story identifiers are assigned only by this workflow.
- Classify every residual as implementable repository work (I), explicit approval/evidence (A), or a documented non-sprint external dependency (X); mapping work to an epic never closes its gate.
- Preserve completed Epic 9 and Epic 11 delivery history. E9-APP-1 remains a separate Product acceptance task, Story 11.25 remains historical technical capture only, and later drift creates new work rather than reopening accepted stories.
- Deliver PLAN-INT-2 as new current-state artifact-integrity work: repair the declared Story 11.32 validation scope, make the current validator pass, and prove an intentional stale-status/missing-File-List fixture fails without rewriting historical acceptance.
- Keep PLAN-INT-1 closed as the approved 2026-09-22 canonical-artifact reconciliation; do not recreate it as implementation work.
- Sequence EVT-APP-1 after EVT-ID-1 and create EVT-XFER-1 only when the conditional ownership-transfer path is invoked; missing receipts leave G-3 open.
- Sequence EXT-ADOPTER-1 after ADOPT-KIT-1. FrontComposer may track the external dependency but cannot complete it for Tenants or a Product-selected Parties substitute.
- Sequence MCP-APP-1 only after MCP-SEC-1 and MCP-SEC-2 produce one immutable negative-evidence packet reviewed by an independent security reviewer.
- Keep UX-A through UX-F independently completable, but require all applicable UX, documentation, Fluent, and accessibility rows before PRD-APP-1 can approve product readiness.
- Sequence GOV-B after EXT-BUILDS-1, GOV-J after GOV-B through GOV-I, and GOV-ACCEPT-2 after GOV-SRC-1; unavailable upstream evidence remains external/open rather than being inferred.
- Sequence REL-LEDGER-2 after REL-LEDGER-1 and preserve visibly missing evidence for v4.1.1 through v4.5.0 and any later discovered release rather than inferring or relabelling it.
- Treat PRD-APP-1 as a digest-bound final Product readiness decision after every Product-owned G-1 through G-8 prerequisite; it never grants publication authority.
- Follow the approved order: planning integrity and external starts, independent implementation, evidence convergence, then owner decisions after their exact evidence dependencies exist.

### UX Design Requirements

UX-DR1: Use FrontComposer plus Blazor Fluent UI V5 for every design-system-owned visual and interaction; inherit Fluent theme, anatomy, type, spacing, focus, elevation, and semantic states instead of redefining them.

UX-DR2: Keep FcShellOptions.AccentColor configurable with default #0097A7 and use it only as a thread for active navigation, focus emphasis, primary actions, links, selected emphasis, and fresh rows; validate every load-bearing placement to WCAG 2.2 AA and fall back to inherited semantic roles when contrast fails.

UX-DR3: Use Fluent component parameters, Fluent 2 tokens, and the nine existing FcTypoToken mappings for page title, section title, body, and caption; preserve TypographyMappingVersion 3.1.0 and do not recreate typography or foreground roles in CSS.

UX-DR4: Preserve the confirmed layout metrics: 72px labelled rail, 48px icon-only rail, 32px compact projection rows, sticky projection headers, full-width default pages, 75rem constrained pages, and no invented numeric responsive breakpoints.

UX-DR5: Restrict custom CSS to layout/browser behavior Fluent does not own; forbid legacy Fluent V4/FAST tokens, custom module themes, hard-coded semantic palettes, decorative gradients, bespoke shadows, nested card stacks, and custom interactive controls when a FrontComposer/Fluent equivalent exists.

UX-DR6: Group two or more sibling titled regions in one Fluent accordion, expand a primary item by default when included, and never hide the only primary content region; keep titles, breadcrumbs, toolbars, navigation chrome, and a single primary region outside.

UX-DR7: Implement FrontComposerShell as one main landmark with skip links, providers, neutral header/footer, route content, shortcuts, and an always-present account control; successful navigation focuses the real route h1 and failed navigation preserves stable focus and announces once.

UX-DR8: Implement FrontComposerNavigation and FcHamburgerToggle with exactly one primary entry per Module and one active item; keep the hamburger available in all modes, use labelled/icon-only desktop rails, and expose the same Module list in compact/narrow navigation without promoting subpages.

UX-DR9: Implement FcAccountMenu as an always-present header affordance unaffected by adopter header customization; provide challenge/sign-out routes, keep the display name inside the menu, evict token state on sign-out, expose support-safe failure, and restore focus on close.

UX-DR10: Implement FcHomeDirectory and FcHomeCard for No Modules, Hydrating, Partially Ready, Ready, and Missing Tenant states with geometry-preserving skeletons, deterministic urgency ordering, stable focus during reordering, and entry into each Module's default tab.

UX-DR11: Implement FcCommandPalette as an ARIA combobox/dialog opened by Ctrl+K, with a 150ms authorization-aware search debounce, keyboard result navigation, useful non-noisy result announcements, canonical route activation, invoker-focus restoration on close, and destination-h1 focus on navigation.

UX-DR12: Implement FcSettingsDialog from the header and Ctrl+, with tenant/user-scoped theme and density hydration, preview, reset, confirm, body density update, one change announcement, skipped persistence without scope, and in-session preservation plus safe feedback when persistence fails.

UX-DR13: Implement FcPageHeader and FcPageLayout with a unique focusable route h1, full-width default/constrained opt-in content, neutral hierarchy without an accent title band, and a stable focus/status target on route failure.

UX-DR14: Implement FcPageTabs as deep-linkable route-backed Module Tabs using /{module}/{tab}; retain inherited arrow-key behavior, keep focus on the active tab as its labelled tabpanel changes, and never style tabs as primary navigation.

UX-DR15: Implement FcPageToolbar with leading search/filter/view/overflow affordances, end-aligned authorized actions, optional / shortcut to page search, a stable public behavior contract, and implementation-owned internal Fluent composition.

UX-DR16: Implement generated Fluent projection grids with debounced/resettable filtering, sorting, keyboard row expansion/actions, sticky headers, column prioritization above 15 columns, server virtualization from 500 rows, a 10,000-row unfiltered cap, and one labelled grid context.

UX-DR17: Implement FcProjectionLoadingSkeleton and FcProjectionEmptyPlaceholder so skeleton geometry matches Card/Timeline/Grid and is marked busy; distinguish no data from no filter matches, preserve filter context, and show an authorized create CTA only when available.

UX-DR18: Implement FcProjectionConnectionStatus, FcSlowQueryNotice, and FcMaxItemsCapNotice for Stale, Reconnecting, FallbackPolling, Reconnected, SlowQuery after 2,000ms, and MaxItems at 10,000; preserve safe reads, announce meaningful entry/recovery once, and keep retries/poll ticks silent.

UX-DR19: Implement FcExpandInRowDetail and FcExpandedRowHiddenBanner as labelled nested regions; when filtering hides expanded content, announce once and move or preserve focus on a valid grid control without implying deletion.

UX-DR20: Implement FcStatusIcon plus an inherited icon/tooltip and FcDesaturatedBadge plus an inherited badge so status always has icon/shape, accessible name, keyboard-focusable tooltip, and text; keep counts contextual and never rely on color or hover.

UX-DR21: Implement generated command forms and FcFieldPlaceholder with Fluent inputs only, density derived from editable property count, no gaps for server-controlled/derived fields, neutral bounded placeholders for unsupported types, and named domain actions instead of generic Submit where known.

UX-DR22: Implement client validation with a focused linked error summary, navigation from every item to its Fluent input, preserved useful values, keyboard access to the first invalid field, and a distinction between client validation and asynchronous server Rejected lifecycle outcomes.

UX-DR23: Implement FcAuthorizedCommandRegion with stable Pending, Authorized, and NotAuthorized geometry; never flash or enable protected content before authorization, evaluate policy before and after BeforeSubmit, and enforce the same authorization at the service boundary.

UX-DR24: Implement FcDestructiveConfirmationDialog and FcFormAbandonmentGuard with explicit confirm/cancel behavior, no dispatch on cancel, a 30-second dirty-form guard, one modal layer, semantic destructive styling, and focus restoration to the invoker/form.

UX-DR25: Implement FcLifecycleWrapper and FcPendingCommandSummary for the exact lifecycle vocabulary; keep Acknowledged distinct from success, use polite/coalesced progress, focus Rejected validation/error paths, block rather than queue a second local command, and announce each meaningful state once.

UX-DR26: Present Degraded after 10,000ms while status polling may continue every 1,000ms to 120,000ms; keep the single 250ms same-MessageId transient dispatch retry distinct from lifecycle polling and preserve correctable form state.

UX-DR27: Implement FcNewItemIndicator only for resolver-owned eligible Material outcomes with immutable pre-dispatch identity; update already-rendered grids, scope before read/render, enforce first-wins ViewKey/EntityKey identity, announce materialization at most once, and expire silently after ten seconds.

UX-DR28: Suppress fresh-row presentation for unknown identity/materiality, NoOp, delete, Rejected, NeedsReview, and server-allocated keys; never infer identity from SignalR nudges, row diffs, aggregate IDs, or untyped results, and retain meaning in reduced motion and forced colors.

UX-DR29: Implement FcCustomizationDiagnosticPanel only in Development for override mismatch/render fault, with bounded corrective guidance and no raw payload, tenant/user value, token, stack trace, or unsafe replacement of operator data.

UX-DR30: Preserve the information architecture of one Module per bounded context, exactly one primary entry, one required default Module Tab, /{module} as its alias, secondary projection flyouts, canonical /commands/{BoundedContext}/{CommandTypeName} routes, and no top-level projection/command explosion.

UX-DR31: Implement explicit accessible shell/home states for No Modules, Hydrating, Partially Ready, Ready, Route Failure, No Module Access, Missing/Stale Tenant, and Signed Out/In, with terminal/non-terminal meaning, recovery, focus, and non-noisy announcements as specified.

UX-DR32: Implement supporting state matrices for Module tabs, projection flyouts, palette, settings, account, row detail, generated commands, customization diagnostics, MCP, inspect/migrate, and the adopter test harness; unknown tabs and unsafe actions must fail explicitly rather than silently select or write.

UX-DR33: Implement projection Loading, Empty, Data, Stale, Reconnecting, FallbackPolling, SlowQuery, MaxItems, Reconnected, and Query/Permission Error as combinable accessible states with entry evidence, permitted actions, recovery/terminal classification, and deduplicated announcements.

UX-DR34: Preserve visible usable data during Stale/Reconnecting/FallbackPolling where safe; retry SignalR with unbounded jittered exponential backoff capped at 30,000ms, restart closed connections within 10 seconds, poll every 15 seconds across at most eight lanes, and show Reconnected for 3,000ms.

UX-DR35: Preserve MCP UX/protocol shapes exactly: caller-relative hidden/absent tool equivalence, the disclosed static resource catalog, registered-hidden unknown_resource behavior, SDK unregistered-resource distinction, schema-mismatch side-effect blocking, bounded skill failures, and cross-request lifecycle polling.

UX-DR36: Keep inspect/migrate/generated-output interactions deterministic: repository-relative/redacted inspect output, dry-run migration by default, atomic apply, and fail-closed refusal of generated, submodule, symlink, out-of-root, bin, obj, or .git targets.

UX-DR37: Support Ctrl+K, Ctrl+,, conditional / search focus, and Esc close/restore behavior; ensure no projection or command action is hover-only, keep one modal layer, and prefer route/tab/detail/full-page surfaces over nested dialogs.

UX-DR38: Provide one main landmark, skip links, unique h1, semantic navigation, labelled tab relationships, combobox/listbox/dialog/grid semantics, labelled row-detail regions, accessible names for every control, and reading-order focus that never lands in removed, hidden, or obscured content.

UX-DR39: Use polite live status for progress/non-blocking changes and a focused linked summary/alert for submit-blocking validation and Rejected outcomes; deduplicate by operation/entity/state, coalesce intermediate progress, and suppress retry, polling, rerender, and expiry noise.

UX-DR40: Verify 320 CSS-pixel reflow and 400% zoom without nonessential two-dimensional scrolling; apply WCAG text-spacing overrides, 24×24 CSS-pixel pointer targets or documented exceptions, keyboard equivalence, focus-not-obscured behavior, reduced motion, and forced-colors preservation.

UX-DR41: Resolve tenant/user scope before every query, subscription, count, preference, pending-state, or fresh-row operation; clear old scope before new rendering, block stale SignalR groups, use scoped storage keys, and never invent a default tenant.

UX-DR42: Keep TenantId, UserId, MessageId, CorrelationId, timestamps, and derived values out of editable UI and inject them server-side; treat hidden controls as presentation only, not authorization.

UX-DR43: Keep UI, MCP, logs, telemetry, snapshots, diagnostics, and evidence support-safe by excluding raw tokens, JWTs, EventStore metadata/payloads, stack traces, unrestricted PII, and hidden identifiers.

UX-DR44: Implement semantic Desktop, Compact, and Narrow-browser behavior through the shared breakpoint watcher: desktop labelled/icon-only rail, compact reachable tabs/palette/settings/actions, and narrow one-entry-per-Module drawer plus single reading order and bounded table scrolling only where essential.

UX-DR45: Expand the Testing package and bUnit/e2e evidence to cover deterministic command success/rejection/stall/auth denial/query variants, linked validation, lifecycle truth, blocked submit, fresh-row scope/first-wins/silent expiry, route/tab/palette/dialog focus, keyboard recovery, announcements, reflow/zoom, text spacing, target size, unobscured focus, forced colors, and reduced motion.

UX-DR46: Keep stable data-testid selectors on behavior governed by automated evidence while retaining accessible semantics as the primary contract; redact tenant/user values, secrets, tokens, raw paths, payloads, and stack traces from evidence.

UX-DR47: Resolve and approve the exact Fluent V5 catalog identity and supported accent token/API role before final UX handoff; do not introduce a replacement alias or hard-coded token while the decision remains open.

UX-DR48: Obtain explicit dispositions for the unresolved exact visual treatment and final microcopy of Warning, NeedsReview, Degraded, missing tenant, FallbackPolling, SlowQuery, MaxItems, and fresh-row dismissal/forced-colors states; until then inherit the nearest Fluent semantic treatment without inventing final copy.

UX-DR49: Keep numeric responsive breakpoints out of the product contract until approved; implementations must preserve the semantic viewport behaviors without fabricating contract widths.

UX-DR50: Run the opt-in UX reviewer gate, or record an explicit skip, and close or disposition all source-owned gaps before changing the paired UX contract status from draft.

### FR Coverage Map

FR1: Epic 12 - Generate projection artifacts for adopter domain types.
FR2: Epic 12 - Generate command artifacts and canonical routes.
FR3: Epic 12 - Honor the documented attribute vocabulary.
FR4: Epic 12 - Apply command density from editable-property count.
FR5: Epic 15 - Support deterministic, diagnosable customization levels.
FR6: Epic 15 - Detect schema and generated-output drift.
FR7: Epic 12 - Provide validated three-call domain-shell bootstrap.
FR8: Epic 12 - Render the accessible Fluent application shell.
FR9: Epic 12 - Manage layout, theme, density, persistence, and localization.
FR10: Epic 12 - Provide registry-driven Module discovery and navigation.
FR11: Epic 13 - Render accessible projection grids and complete state feedback.
FR12: Epic 13 - Maintain projection freshness and realtime recovery.
FR13: Epic 13 - Mark fresh rows only through eligible FC-NIP outcomes.
FR14: Epic 13 - Validate and submit commands through generated forms.
FR15: Epic 13 - Surface truthful command lifecycle states and budgets.
FR16: Epic 13 - Enforce command authorization, confirmation, abandonment, and concurrency safety.
FR17: Epic 14 - Expose visible generated commands as secure MCP tools.
FR18: Epic 14 - Expose projection and skill resources under their defined boundaries.
FR19: Epic 14 - Enforce fail-closed MCP admission, compatibility, disclosure, and redaction.
FR19a: Epic 14 - Preserve MCP command lifecycle across requests and scopes.
FR20: Epic 15 - Provide deterministic generated-output inspection.
FR21: Epic 15 - Provide safe, dry-run-first migration tooling.
FR22: Epic 15 - Provide deterministic adopter testing and redacted evidence support.
FR23: Epic 15 - Keep component, diagnostic, migration, and skill documentation synchronized.
FR24: Epic 17 - Publish only exact, evidence-classified artifacts through privilege separation.
FR25: Epic 15 - Preserve public contracts, baselines, analyzer policy, and migration paths.
FR26: Epic 13 - Preserve the completed FC-NIP producer/consumer composition.
FR27: Epic 15 - Preserve closed tooling-governance outcomes as regression traceability.
FR28: Epic 15 - Preserve closed route and Contracts-boundary decisions as regression traceability.
FR29: Epic 15 - Maintain verified closure of architecture-review defect classes.
FR29.1: Epic 12 - Preserve authentication token lifecycle and scoped-lifetime safety.
FR29.2: Epic 13 - Preserve projection realtime resilience.
FR29.3: Epic 14 - Preserve MCP cross-request lifecycle behavior.
FR29.4: Epic 13 - Preserve return-path and storage-key safety.
FR29.5: Epic 15 - Preserve generated-code hygiene.
FR29.6: Epic 15 - Preserve Contracts boundaries and enforcement.
FR29.7: Epic 16 - Reconcile and approve the exact EventStore runtime identity.
FR30: Epic 13 - Enforce tenant/user scope across every operator-facing surface.

## Epic List

### Epic 12: Adopters Launch an Operations-Ready Domain Shell

Adopter developers can turn annotated domain types into a coherent, accessible FrontComposer shell and prove a named domain-module adoption without bespoke framework plumbing.

**FRs covered:** FR1, FR2, FR3, FR4, FR7, FR8, FR9, FR10, FR29.1

**Implementation notes:** Preserve the delivered generator, bootstrap, shell, account, and information-architecture baseline. Deliver ADOPT-KIT-1 before tracking EXT-ADOPTER-1; the latter is owned by the selected external adopter and was accepted on 2026-09-26 (record below). Require optional ADOPT-APP-1 only for a D-7 substitute. Adoption work uses the current pinned Fluent identity but does not own its final approval. Completed behavior is regression traceability rather than reimplementation.

### Epic 13: Operators Trust Tenant-Scoped Data and Command Outcomes

Operators can browse projections, execute commands, recover from transport failures, and understand lifecycle and fresh-row state without stale-tenant leakage or false success.

**FRs covered:** FR11, FR12, FR13, FR14, FR15, FR16, FR26, FR29.2, FR29.4, FR30

**Implementation notes:** Complete TEN-SCOPE-1 through real production seams, UX-A through UX-F, the required Testing assertions and accessibility evidence, E9-APP-1, and then FLUENT-APP-1 by the Product Owner and Architect against the exact tested catalog identity while preserving the delivered projection, lifecycle, and FC-NIP runtime baseline. This orders accessibility evidence before the Fluent decision and removes a cross-epic acceptance cycle.

### Epic 14: AI Integrators Expose Domain Operations Safely

AI integrators can expose generated commands, projections, skills, and lifecycle polling through a host-authenticated MCP surface with tested disclosure boundaries.

**FRs covered:** FR17, FR18, FR19, FR19a, FR29.3

**Implementation notes:** Deliver MCP-SEC-1 and MCP-SEC-2, the named SM-4 audit evidence, and independent MCP-APP-1 acceptance. Implementation and negative evidence must precede security sign-off.

### Epic 15: Maintainers Evolve FrontComposer Without Contract Drift

Framework maintainers can customize, inspect, migrate, document, test, and upgrade FrontComposer while preserving public contracts and exact runtime identity.

**FRs covered:** FR5, FR6, FR20, FR21, FR22, FR23, FR25, FR27, FR28, FR29, FR29.5, FR29.6

**Implementation notes:** Complete PLAN-INT-2, DOC-A/DOC-B/DOC-C parity, REL-BASE-1, and Testing/documentation gaps without reopening completed Epic 9–11 decisions. PLAN-INT-1 remains closed. Delivered baselines remain regression traceability rather than reimplementation.

### Epic 16: Maintainers Verify and Approve Exact Runtime Compatibility

FrontComposer, EventStore, and Release maintainers can bind one exact runtime tuple to live compatibility evidence and named approval without rewriting historical identities.

**FRs covered:** FR29.7

**Implementation notes:** Complete EVT-ID-1 and obtain EVT-APP-1 from the EventStore maintainer, FrontComposer maintainer, and Release Owner, with conditional EVT-XFER-1 before approval when EventStore ownership must transfer. Exact-tuple capture, live Pact/AppHost evidence, artifact hashes, and named approval remain separate outcomes; tuple drift creates new work.

### Epic 17: Release Owners Establish Safe Publication and Trustworthy Release Records

Release owners can authorize and verify exact package bytes through a privilege-separated pipeline, maintain truthful append-only attempt records, preserve incidents, and support an independent evidence-backed Product readiness decision.

**FRs covered:** FR24

**Implementation notes:** Track two independently visible completion streams. The publication-boundary stream contains EXT-BUILDS-1, GOV-B through GOV-H, GOV-J, GOV-SRC-1, and GOV-ACCEPT-1/2. The release-evidence/decision stream contains GOV-I, REL-LEDGER-1/2, REL-A3-APP-1 owned by the Release Owner, and PRD-APP-1. Upstream acceptance, implementation, deterministic evidence, independent approval, publication authorization, post-publication verification, and milestone reapproval remain separate statuses; Product reapproval never authorizes publication.

### Epic Allocation and Closure Guardrails

- Epics 12–16 may make repository implementation progress independently against the delivered foundation; their acceptance tasks wait for the exact evidence and earlier decisions named above.
- Epic 17 may begin source reconciliation, ledger, incident-readiness, and other unblocked work, but the caller switch remains blocked on EXT-BUILDS-1 and the final milestone decision remains blocked on the required evidence from Epics 12–16.
- Architecture requirements follow their owning epic: adoption/shell in Epic 12, tenant-safe operator behavior in Epic 13, MCP security in Epic 14, planning/tooling/documentation integrity in Epic 15, runtime identity in Epic 16, and publication/dependency governance in Epic 17.
- UX-DR and NFR obligations are allocated to every affected story rather than counted as satisfied by FR mapping. Epic 12 owns adoption, the shell frame, and information-architecture structure; Epic 13 owns projection/command/accessibility behavior, shell focus and information-architecture interaction behavior through its UX-A slice (Story 13.2), the UX testing helpers through its UX-F slice (Story 13.7), and the exact Fluent decision; Epic 14 owns agent-surface security behavior; Epic 15 owns tooling/documentation/testing semantics other than the Epic 13 UX-F helpers; Epic 16 owns compatibility evidence; and Epic 17 owns evidence determinism, dependency governance, incident readiness, and release safety.
- An external dependency, missing receipt, or rejected approval remains visibly open and cannot be converted into repository-owned completion. A green unrelated lane never substitutes for the focused evidence required by an epic.
- Every materialized item must declare its classification as I, A, or X. Only I items may claim repository implementation completion; A items close only through their named durable decision/evidence receipt, and X items remain controlled by the named external owner.
- Completed-baseline requirements are acceptance-criteria traceability only unless an approved open alias below explicitly creates new work.
- Evidence must be minimum-sufficient and risk-proportionate. Reuse an existing focused test result, immutable artifact, or lane output whenever it proves the exact acceptance boundary; one artifact may satisfy multiple criteria when its provenance and scope are explicit.
- Do not create duplicate evidence packets, wrapper reports, schemas, validators, workflows, or test lanes merely to restate proof that already exists. Add a new evidence mechanism only when no existing artifact can prove a named requirement, and use manual evidence only for behavior that cannot be established deterministically through automation.
- Approval records cite the immutable evidence they reviewed rather than reproducing it. Lean evidence never permits inference of a missing receipt or relaxation of the exact security, runtime-identity, accessibility, or publication-boundary guarantees required by the source documents.
- Materialize exactly one item for each approved alias below. Do not add generic evidence, integration, coordination, or sign-off stories unless a source requirement identifies a distinct unowned acceptance boundary; extend the owning alias's acceptance criteria and reuse its focused proof instead.
- A rejected approval leaves its gate open and creates a new bounded residual item only when corrective work is required. Rejection never rewrites completed implementation, invalidates immutable evidence history, or reopens a delivered epic by itself.

### Approved Alias Allocation

| Epic | Implementable repository work (I) | Approval/evidence work (A) | External dependencies (X) |
|---|---|---|---|
| Epic 12 | ADOPT-KIT-1 | ADOPT-APP-1 (conditional) | EXT-ADOPTER-1 |
| Epic 13 | TEN-SCOPE-1; UX-A; UX-B; UX-C; UX-D; UX-E; UX-F | E9-APP-1; FLUENT-APP-1 | — |
| Epic 14 | MCP-SEC-1; MCP-SEC-2 | MCP-APP-1 | — |
| Epic 15 | PLAN-INT-2; DOC-A; DOC-B; DOC-C; REL-BASE-1 | — | — |
| Epic 16 | EVT-ID-1 | EVT-APP-1; EVT-XFER-1 (conditional) | — |
| Epic 17 | GOV-B; GOV-C; GOV-D; GOV-E; GOV-F; GOV-G; GOV-H; GOV-I; GOV-SRC-1; REL-LEDGER-2 | GOV-J; GOV-ACCEPT-1; GOV-ACCEPT-2; REL-LEDGER-1; REL-A3-APP-1; PRD-APP-1 | EXT-BUILDS-1 |

PLAN-INT-1 remains closed reconciliation history and is not materialized as a new story.

## Delivered History

> **Delivered history (restored 2026-09-23).** The sections from here through Cross-Cutting
> Governance Work are the completed Epics 1–11 delivery record, restored from `epics.md` at commit
> `aeff9f83` after the 2026-09-23 sprint-planning readiness gate found that the new backlog reused
> their numbers. Stories, titles, acceptance criteria, and the Epic 11 workstream and sequence
> sections are unchanged; only the annotations below, the story cross-references they name, and the
> parser-compatibility headings described below were added. The historical inventory in this
> section keeps its own identifiers (legacy FR/NFR, AR1–AR12, UX-DR1–UX-DR8, canonical FR-1–FR-30)
> and does not redefine the Epic 12–17 inventory above.
>
> **Approved 2026-09-22 annotations** (`sprint-change-proposal-2026-09-22.md` §3 and §9.4):
>
> - **Epic 9 — delivery done.** Stories 9.1–9.8, E9-AI-1 through E9-AI-6, and the retrospective are
>   completed delivery history. E9-APP-1 (G-5/OI-1 Product acceptance of the Story 9.8 live proof) is
>   separate approval work, materialized as Story 13.8, and does not reopen Epic 9.
> - **Epic 11 — delivery done** through Story 11.32. Later tuple or validator drift is new work:
>   current artifact-integrity repair is PLAN-INT-2 (Story 15.1).
> - **Story 11.25 — technical capture only.** It stays done, neither approves migration nor closes
>   G-3, and is not reused for later repository tuples.
> - **E11R-AI-1 — carried forward** to EVT-ID-1 (Story 16.1, exact-tuple capture) and EVT-APP-1
>   (Story 16.3, owner approval), with conditional EVT-XFER-1 (Story 16.2) before approval.
> - **GOV-1 — open parent obligation** (Cross-Cutting Governance Work below). It closes only through
>   EXT-BUILDS-1 and the Epic 17 slices and approvals, never by bookkeeping alone.
>
> **Parser compatibility (2026-09-23).** The nonimplementable decomposition parents 11.17, 11.18, and
> 11.19 are headed "Decomposition Parent" so that sprint planning never queues them, and each of their
> 11 materialized children carries a `#### Story 11.1x:` heading whose title matches its existing
> sprint-status key (the letter suffix stays in the unchanged child bullet, for example 11.17a). The
> child bullets, parent text, and acceptance criteria are unchanged.
>
> Aliases that the historical text below calls *proposed* are materialized as: ADOPT-KIT-1 → 12.1;
> ADOPT-APP-1 → 12.2 (conditional); EXT-ADOPTER-1 → Epic 12 external dependency; TEN-SCOPE-1 → 13.1;
> UX-A–UX-F → 13.2–13.7; E9-APP-1 → 13.8; FLUENT-APP-1 → 13.9; MCP-SEC-1 → 14.1; MCP-SEC-2 → 14.2;
> MCP-APP-1 → 14.3; PLAN-INT-2 → 15.1; DOC-A–DOC-C → 15.2–15.4; REL-BASE-1 → 15.5; EVT-ID-1 → 16.1;
> EVT-XFER-1 → 16.2 (conditional); EVT-APP-1 → 16.3; GOV-SRC-1 → 17.1; GOV-I → 17.2; GOV-B–GOV-H →
> 17.3–17.9; GOV-J → 17.10; GOV-ACCEPT-1 → 17.11; GOV-ACCEPT-2 → 17.12; REL-LEDGER-1 → 17.13;
> REL-LEDGER-2 → 17.14; REL-A3-APP-1 → 17.15; PRD-APP-1 → 17.16; EXT-BUILDS-1 → Epic 17 external
> dependency. PLAN-INT-1 remains closed reconciliation history.

### Historical Source Note

This document provides the complete epic and story breakdown for Hexalith.FrontComposer, decomposing the requirements from the available inputs into implementable stories.

> **Source note.** Canonical planning sources now exist under
> `_bmad-output/planning-artifacts`: `prd.md`, `architecture.md`,
> `ux-design.md`, and this `epics.md`. The PRD is brownfield-derived from
> `_bmad-output/project-docs/*` plus `frontcomposer-readiness-request-2026-06-03.md`.
> Treat the FR/NFR sections as a capability inventory and acceptance baseline,
> and the Additional Requirements (`FC-*`) as the forward roadmap to plan epics
> around. Requirements must retain source traceability back to the PRD and
> brownfield source artifacts.


### Legacy Functional Requirements (Provenance Only)

> These identifiers predate the canonical PRD and are retained only to explain brownfield
> provenance. They are not planning identifiers. New and corrected traceability uses the canonical
> `FR-1` through `FR-30` requirements in `prd.md` and the canonical coverage map below.

**Source generator (`Hexalith.FrontComposer.SourceTools`)**

- LEGACY-FR-1: From each `[Projection]`-annotated `partial` type, generate 5 files — projection view (`{T}.g.razor.cs` with Loading/Empty/Data states dispatched by `ProjectionRole`), `{T}Feature.g.cs`, `{T}Actions.g.cs`, `{T}Reducers.g.cs`, `{T}Registration.g.cs`.
- LEGACY-FR-2: From each `[Command]`-annotated type (public parameterless ctor + `MessageId`), generate seven non-page files (`CommandForm`, `CommandActions`, `CommandLifecycleFeature`, `CommandRegistration`, `CommandRenderer`, `CommandLastUsedSubscriber`, `CommandLifecycleBridge`), plus `CommandPage` when density = `FullPage`.
- LEGACY-FR-3: Apply the spec-locked command **density rule** — non-derivable property count ≤1 → `Inline`, 2–4 → `CompactInline`, ≥5 → `FullPage` — excluding derivable fields (`MessageId`, `CommandId`, `CorrelationId`, `TenantId`, `UserId`, `Timestamp`, `CreatedAt`, `ModifiedAt`, `[DerivedFrom]`).
- LEGACY-FR-4: Emit compilation-level `FrontComposerMcpManifest.g.cs` and `FrontComposerProjectionTemplateManifest.g.cs`, each carrying schema fingerprints.
- LEGACY-FR-5: Honor the full attribute vocabulary: `[BoundedContext]`, `[ProjectionRole]`, `[ProjectionBadge]`, `[ColumnPriority]`, `[ProjectionFieldGroup]`, `[ProjectionEmptyStateCta]`, `[Destructive]`, `[RequiresPolicy]`, `[DerivedFrom]`, `[Icon]`, `[RelativeTime]`, `[Currency]`, `[ProjectionTemplate]` (plus `[Display]`, `[Description]`, `[DefaultValue]`, `[Flags]`).
- LEGACY-FR-6: Emit the HFC1001–HFC1070 diagnostic catalog (build-time `HFC1xxx`) for invalid annotation/usage, with severities as cataloged.
- LEGACY-FR-7: Provide opt-in **drift detection** (`HfcDriftDetectionEnabled=true`) comparing the current snapshot to a checked-in JSON baseline `AdditionalText` → structural HFC1065 / metadata HFC1066; pipeline must not depend on `CompilationProvider`.
- LEGACY-FR-8: Support 4-level customization (Level-2 `ProjectionTemplate`, Level-3 field-slot, Level-4 full-view overrides) so external assemblies can inject alternate render fragments.

**Blazor Shell (`Hexalith.FrontComposer.Shell`)**

- LEGACY-FR-9: Compose generated UI into a complete app frame via `<FrontComposerShell>@Body</FrontComposerShell>` — `FluentLayout` Header/Navigation/Content/Footer, skip links, `FluentProviders`, global shortcuts (`Ctrl+,` settings, `Ctrl+K` palette).
- LEGACY-FR-10: Provide the DI bootstrap path: `AddHexalithFrontComposerQuickstart()` → `AddHexalithDomain<TMarker>()` → `AddHexalithEventStore(...)`.
- LEGACY-FR-11: Render projections in `FluentDataGrid` with column filtering, expand-in-row detail, status badges, empty/loading states, slow-query/max-items notices, and column prioritization for >15-column projections.
- LEGACY-FR-12: Drive the command lifecycle UI (`Idle→Submitting→Acknowledged→Syncing→Confirmed/Rejected`) with form-abandonment guard and destructive-command confirmation dialog.
- LEGACY-FR-13: Connect to EventStore via SignalR (projection subscriptions) and HTTP (commands/queries), surfacing reconnect/reconciliation status.
- LEGACY-FR-14: Provide registry-driven navigation, home directory (urgency-sorted bounded-context cards), command palette (ARIA combobox), and badge counts.
- LEGACY-FR-15: Manage theme, density, and settings, persisted via `IStorageService` (`LocalStorageService`).

**MCP server (`Hexalith.FrontComposer.Mcp`)**

- LEGACY-FR-16: Expose each generated command as an MCP tool (built dynamically at every `tools/list`) plus a fixed `frontcomposer.lifecycle.subscribe` polling tool.
- LEGACY-FR-17: Expose projections (`frontcomposer://<bounded-context>/projections/<projection-name>`, tenant-scoped Markdown) and skill-corpus docs (`frontcomposer://skills/<id>`) as MCP resources.
- LEGACY-FR-18: Enforce fail-closed security — both `IFrontComposerMcpTenantToolGate` and `IFrontComposerMcpResourceVisibilityGate` required or startup throws; opaque error shape; server-controlled fields (`TenantId`/`UserId`/`MessageId`/`CorrelationId`) blocked from tool input.
- LEGACY-FR-19: Negotiate schema compatibility (`McpSchemaNegotiator`: Exact / CompatibleAdditive / CompatibleWarning / Incompatible) and block side-effects on mismatch.

**CLI (`Hexalith.FrontComposer.Cli`)**

- LEGACY-FR-20: `frontcomposer inspect` reads generated output + `*.diagnostics.json` sidecars and reports forms/grids/registrations/manifest entries/warnings/errors in text or JSON (`frontcomposer.cli.inspect.v1`).
- LEGACY-FR-21: `frontcomposer migrate` plans/applies allowlisted Roslyn code-fixes across catalog version edges (dry-run default, atomic apply, path-safety refusals), JSON `frontcomposer.cli.migrate.v1`.

**Testing library (`Hexalith.FrontComposer.Testing`)**

- LEGACY-FR-22: Provide a pre-wired bUnit host + deterministic fakes (command/query/projection/configurable outcomes), evidence recorders, and assertion helpers for adopters testing generated components.

### Legacy Nonfunctional Requirements (Provenance Only)

> These `LEGACY-NFR-*` identifiers are likewise provenance-only. Canonical nonfunctional
> requirements are the `NFR-*` entries in `prd.md`.

- LEGACY-NFR-1: `TreatWarningsAsErrors=true` everywhere; built-in .NET/Roslyn analyzers only (no Sonar/StyleCop/Roslynator).
- LEGACY-NFR-2: ULIDs (26-char Crockford base32) via `IUlidFactory` — never GUIDs — for `messageId`/`correlationId`.
- LEGACY-NFR-3: Incremental-cache invariant — pure, fully-equatable IR; no `ISymbol` escapes the parse stage; `EquatableArray<T>` for collections.
- LEGACY-NFR-4: Schema fingerprint determinism — `CanonicalSchemaMaterial` pins `JavaScriptEncoder.Create(UnicodeRanges.All)`, STJ source-gen context, `AbsentValueSentinel="<absent>"`, `StringComparer.Ordinal`; changing any invalidates all baselines.
- LEGACY-NFR-5: Contracts kernel split — `SourceTools` and the `Contracts` kernel stay netstandard2.0-clean; net10/Blazor/Fluent rendering contracts move to `Contracts.UI`.
- LEGACY-NFR-6: **Accessibility (WCAG)** — `aria-label`/`role`/`aria-live`/`data-testid` on every interactive element; focus visibility, reduced-motion and forced-colors fallbacks; override-accessibility diagnostics HFC1050–HFC1055.
- LEGACY-NFR-7: Generated-output path is a public contract (`GeneratedOutputPathContract.Template`) validated in Debug **and** Release.
- LEGACY-NFR-8: Ships as signed NuGet packages (`.nupkg`+`.snupkg`); semantic-release from Conventional Commits; no Dockerfiles/containers.
- LEGACY-NFR-9: Fluxor single-writer discipline per slice (ADR-007); scoped-lifetime discipline for storage/effects/auth/tenant accessors (ADR-030).
- LEGACY-NFR-10: Test discipline — solution-level `dotnet test` + trait filters, `DiffEngine_Disabled=true`, Governance + Contract lanes blocking; committed `.verified.txt`, `PublicAPI.Shipped.txt`, pacts updated intentionally.
- LEGACY-NFR-11: Telemetry via `FrontComposerActivitySource` (OpenTelemetry `ActivitySource`).
- LEGACY-NFR-12: Dependency direction points down to the `Contracts` kernel; `SourceTools` references only `Contracts`, while Shell/UI consumers may reference `Contracts.UI`.
- LEGACY-NFR-13: **Confirmed (2026-06-21)** Trim/AOT readiness — `PublishTrimmed`/`PublishAot` enable the HFC1070 advisory; reflection projection catalog needs an `IActionQueueProjectionCatalog` override.
- LEGACY-NFR-14: Root-declared Hexalith submodules live under `references/Hexalith.*`; initialize only those root `.gitmodules` entries, never recurse into nested submodules, and never modify submodule files without explicit approval. Debug/source builds consume Hexalith libraries through local `ProjectReference`s, while Release/package builds consume published NuGet packages.

### Historical Additional Requirements

> **This is the forward roadmap** — drawn from `frontcomposer-readiness-request-2026-06-03.md`.
> Priorities: 🔴 1 = blocks read-only MVP / bootstrap · 🟠 2 = blocks command epics (3–5) ·
> 🟡 3 = confirm-stable (existing surface), not build-new.

- AR1 (🔴 **FC-LYT**): Confirm the full-width vs constrained `<PageLayout>` contract (`Shell/Components/Layout/FrontComposerShell.razor`). Blocks even the read-only MVP.
- AR2 (🔴 **FC-A11Y**): Confirm accessibility primitives (the WCAG attribute/role/live-region patterns) as a reusable, documented contract — part of every story's ready-gate.
- AR3 (🔴 **FC-L10N**): Confirm shell-vs-Tenants localized-string ownership (`FcShellResources.resx`).
- AR4 (🔴 **FC-DOC**): Confirm component documentation contract for the shell components.
- AR5 (🔴 **Shell-integration spike**): Verify `AddHexalithFrontComposer*` / manifest / projection-routing / `FC-TBL` (table) APIs — the bootstrap spike (Story 1.0) that unblocks Story 1.1.
- AR6 (🟠 **FC-CMD**): Confirm the command-lifecycle contract — pending-identity / correlation-key shape (the 26-char checkout shape **not yet approved**), uniqueness scope (per-tenant / user / circuit?), lifecycle ownership, `alreadyApplied` semantics, reconciliation. Blocks all command epics.
- AR7 (🟠 **FC-CNC**): Confirm one-at-a-time command execution is the v1 contract (fallback approved; batching = fast-follow).
- AR8 (🟠 **Numeric budgets**): ✅ **Confirmed (2026-06-21).** confirming→degraded (`TimeoutActionThresholdMs=10_000`), polling (cadence `1_000` / max `120_000`), and retry (Epic 3 `0`; Epic 4 `1×250ms`) budgets ratified in `fc-cmd-command-budget-contract` + `fc-cmd-retry-degraded-state-contract`.
- AR9 (🟡 **EventStore status contract**): Confirm-stable `GET /api/v1/commands/status/{id}` as the command-status query the polling coordinator binds to (exists already — confirm, don't build).
- AR10 (**Out of scope for v1 / fast-follow**): Do **not** build `<AuditTimeline>` or `<ConsequencePreview>` rich components now — approved fallbacks stand; track as fast-follow.
- AR11 (**FC-NIP**): Confirm and implement the row-level new-item producer contract for `FcNewItemIndicator`. The producer must come from command outcome context with precise row identity (`EntityKey` or an approved equivalent), not from the current projection nudge seam that carries only projection type and tenant id.
- AR12 (**FC-TOOL-GOV**): Preserve Epic 7 authoring-tooling follow-through as explicit backlog work: mechanical story evidence reconciliation, adopter-facing historical-label cleanup, CLI text/JSON parity coverage, HFCM9002 production-emission decisioning, and default-lane Testing redaction coverage.

> 📋 **Contract-confirmation Definition-of-Done (2026-06-21 process amendment).** A contract-confirmation
> story (the `Confirm …`/`Establish …` stories: 1.2, 1.3, 1.4, 1.5, 2.8, 3.3, 3.5, 3.6, 4.3) MUST NOT reach
> **Done** on *"escalated with an owner"* alone. "Escalated" is a valid intermediate state, but Done requires
> either (a) the decision **confirmed**, or (b) a **tracked, dated, owned blocking follow-up** in the sprint
> backlog. This amends the AC2-style *"confirmed OR escalated with an owner"* wording that previously let
> decisions close silently — the root cause of the FC-LYT / AR8 / UX-DR confirmation debt closed in
> `sprint-change-proposal-2026-06-21`.

> **Epic 1 residual wording disposition (2026-07-05).** The residual FC-A11Y / FC-L10N / FC-DOC /
> FC-SETTINGS wording action is closed by
> `sprint-change-proposal-2026-07-05-epic-1-residual-wording-decisions.md`. FC-L10N confirms that density
> preview sample strings are out of localization scope and domain labels are host-owned with no shell
> fallback. FC-DOC confirms the inline-summary + published-sibling link convention and records that
> DataGrid/settings docs are authored. FC-SETTINGS confirms the AC3 reading as one persistence writer per
> slice plus one DOM writer per side-effect. FC-A11Y confirms the three-layer automated story ready-gate
> and routes visual/manual release sign-off to Product/UX + Release Owner, due before v1.0 RC readiness
> classification.

### Historical UX Design Requirements

> **Confirmed 2026-06-21** (sprint-change-proposal-2026-06-21). Originally reverse-engineered from the
> implemented component catalog (`component-inventory.md`) + readiness request, these UX contracts are now
> confirmed against the shipped, `FluentConformanceTests`-guarded, bUnit/e2e-tested behaviour and refreshed
> to match `architecture.md` §4.

- UX-DR1: **Design tokens** — `Typography` (9 `FcTypoToken` role constants → FluentUI v5 `TextSize`/`TextWeight`/`TextTag`, pinned `TypographyMappingVersion="3.1.0"`); `DensityLevel`/`DensitySurface` density tokens applied via `<body data-fc-density>`.
- UX-DR2: **Semantic status slots** — `[ProjectionBadge]` enum-member → a status indicator with a mandatory accessible name. **Amended 2026-06-25 (Epic 8 / Story 8.7 — `sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md`):** *status* members render as a **colored Fluent icon** (success = green checkmark, error = red cross, unknown/neutral = grey question; warning/info as extensions) with the status label revealed **on hover _and_ keyboard focus** via `FluentTooltip`, plus an always-present `aria-label` so the accessible name is never hover-only (NFR-3 / WCAG 2.2 AA preserved). Numeric **count** slots keep the `FluentBadge` pill (`FcDesaturatedBadge` desaturated variant for non-urgent counts). This **supersedes the prior pill-only status model** (`FcStatusBadge` `FluentBadge` Color/Appearance) and is a contract amendment that touches the `[ProjectionBadge]` generator emit.
- UX-DR3: **Responsive layout** — breakpoint behaviour (`FcLayoutBreakpointWatcher`) with the unified `FrontComposerNavigation` rail rendered at 72px labelled or 48px icon-only width. `FcHamburgerToggle` is **always visible** and at Desktop toggles labelled ↔ icon-only rail (`SidebarToggledAction`) — **supersedes the earlier "D9 / no Desktop hamburger" decision** (architecture §4). The framework sidebar keeps **exactly one active item** (longest segment-prefix, `NavLinkMatch.Prefix`).
- UX-DR4: **Reusable interaction components** — `FcCommandPalette` (ARIA combobox, keyboard nav), `FcSettingsDialog`, `FcDestructiveConfirmationDialog`, `FcFormAbandonmentGuard`, `FcLifecycleWrapper`.
- UX-DR5: **Status & empty/loading UX** — `FcProjectionLoadingSkeleton` (Card/Timeline/Grid), `FcProjectionEmptyPlaceholder`, `FcProjectionConnectionStatus`, `FcPendingCommandSummary` (`aria-live`).
- UX-DR6: **Accessibility patterns** — skip links, focus indicators, `role="region"` row-detail with live-region for filter-hidden expansions (WCAG 4.1.2), keyboard reachability, reduced-motion/forced-colors fallbacks.
- UX-DR7 (FC-LYT): **Page layout contract** — full-width vs constrained `<PageLayout>` (ties to AR1). ✅ Confirmed 2026-06-21 (FullWidth default + `75rem` max-measure).
- UX-DR8: **Account control & server security** (architecture §4) — a framework-owned `FcAccountMenu` (`FluentAvatar` → Sign in/Sign out, wired to `/authentication/{challenge,sign-out}`) rendered **always** so it survives adopter `HeaderEnd` customization; backed by framework-owned server-side security wiring (`AddHexalithFrontComposerServerSecurity`). Domain modules supply only domain-specific security *configuration*.

> 🔗 **UX-DR story-traceability note (added 2026-06-21).** Two UX-DR refinements shipped through
> sprint-change-proposals rather than numbered Epic stories. They are **accepted as
> change-proposal-of-record** — architected in `architecture.md` §4 and enforced by
> `FluentConformanceTests` + bUnit/e2e coverage — so **no synthetic backfill story is created**; this
> note is their requirement-level traceability record:
> - **UX-DR3** — the *always-visible Desktop hamburger* (superseding the "D9 / no Desktop hamburger"
>   decision) and the *single-active-nav-item* rule shipped via
>   `sprint-change-proposal-2026-06-09-shell-account-hamburger` and
>   `sprint-change-proposal-2026-06-19-nav-single-active-item`. The **base** responsive
>   rail/breakpoint/hamburger-collapse behaviour remains traced to **Story 2.2** (AC `*(UX-DR3)*`).
> - **UX-DR8** — `FcAccountMenu` (always-rendered account control) + framework-owned server security
>   (`AddHexalithFrontComposerServerSecurity`) shipped via
>   `sprint-change-proposal-2026-06-09-shell-account-hamburger` and
>   `sprint-change-proposal-2026-06-14-shell-security-helper`. It has **no dedicated numbered story**;
>   this note is its sole story-level traceability link.

### Historical FR Coverage Map

This is the sole planning coverage map. Requirement semantics and identifiers come from canonical
`prd.md`; the legacy inventory above is provenance only.

| Canonical requirement | Planning ownership |
| --- | --- |
| FR-1 | Epic 2: Stories 2.1 and 7.3 diagnostic support |
| FR-2 | Epic 3: Stories 3.1 and 3.2 |
| FR-3 | Epic 2: Stories 2.1, 2.3, 2.5; Epic 4: Stories 4.1, 4.4; Epic 6: Stories 6.1–6.4 |
| FR-4 | Epic 3: Story 3.2 |
| FR-5 | Epic 6: Stories 6.1–6.4 |
| FR-6 | Epic 7: Stories 7.3 and 7.4; Epic 5: Story 5.5 |
| FR-7 | Epic 1: Stories 1.0 and 1.1; Epic 11: scoped-lifetime remediation |
| FR-8 | Epic 1: Stories 1.1 and 1.3; UX-DR8; Epic 8 refinements |
| FR-9 | Epic 1: Stories 1.2, 1.4, 1.6; Epic 8: Story 8.4 |
| FR-10 | Epic 2: Stories 2.2 and 2.7; Epic 8: Story 8.5; Epic 11: Stories 11.0 and 11.7 |
| FR-11 | Epic 2: Stories 2.3–2.5; Epic 8: Stories 8.4 and 8.7 |
| FR-12 | Epic 2: Story 2.6; Epic 11: Story 11.2 |
| FR-13 | Epic 9: historical Stories 9.1-9.2 plus remediation Stories 9.3-9.8; Story 2.6 preserves the ownership boundary |
| FR-14 | Epic 3: Stories 3.1–3.3; Epic 4: Story 4.5 |
| FR-15 | Epic 3: Stories 3.4–3.6 |
| FR-16 | Epic 4: Stories 4.1–4.5 |
| FR-17 | Epic 5: Stories 5.1 and 5.2 |
| FR-18 | Epic 5: Story 5.3 |
| FR-19 | Epic 5: Stories 5.4 and 5.5; Epic 11: Story 11.3 |
| FR-20 | Epic 7: Stories 7.1 and 7.3; Epic 10: Story 10.3 |
| FR-21 | Epic 7: Story 7.2; Epic 10: Stories 10.3 and 10.4 |
| FR-22 | Epic 7: Story 7.5; Epic 10: Story 10.5; Epic 11: Story 11.6 |
| FR-23 | Stories 1.5, 5.3, 7.2–7.4, 10.2, 10.4, and 11.14 |
| FR-24 | Release Governance Gate RG-1; REL-AI-1 remains open; REL-3 owns correction, REL-4 the technical freeze, REL-5 Release Owner enablement, and GOV-1 the complete defined depth-1/2 dependency/workflow provenance correction; REL-2 is completed evidence, not closure |
| FR-25 | Epics 7 and 10; Epic 11: Stories 11.8, 11.11–11.14, the 11.19 children, and staged analyzer-policy/burn-down/activation Stories 11.20–11.23 |
| FR-26 | Epic 9: remediation Stories 9.3-9.8; Story 9.2 remains historical delivery evidence only |
| FR-27 | Epic 10: Stories 10.1–10.5 |
| FR-28 | Epic 11: completed decision records 11.0 and 11.8 |
| FR-29 | Epic 11: Stories 11.1–11.32, with 11.17–11.19 represented only through their materialized children |
| FR-30 | Proposed TEN-SCOPE-1: end-to-end tenant separation and fail-closed proof across EventStore command, query, subscription, count, and storage paths |

**Release Governance Gate RG-1 (FR-24):** before any NuGet or GitHub package publication, the Release
Owner must prove that the exact expected package artifacts passed inventory, tests, package-consumer
validation, symbol/SBOM generation, checksum coverage, mandatory provenance attestation or the
run-bound approved-unsupported fallback, manifest-v4 sealing, offline/live verification, and final
classification by the candidate-free protected publisher. Passing evidence requires
`publish_authorized=true`; the same authorized bytes must be published and independently verified from
GitHub and NuGet.org, including repository-signature/normalized-member equivalence. Durable attempt and
incident evidence is required. Product work may continue while the gate is open, but production package
publication may not.

**Update (correct-course 2026-07-19):** **`GOV-1: Validate shared-catalog compatibility and seal
dependency provenance`** separates compatibility from provenance. Product Governance validates the
semantic catalog selected by every Builds edge in the complete defined depth-1/2 v1 graph and contains
no expected SHA allowlist. Pointer changes emit a deterministic graph diff and run exact affected-module
Release/NuGet gates. The finalized 2026-09-09 spine preserves AD-1 through AD-19, requires the
secretless candidate-builder and protected candidate-free publisher split, advances the
Release-to-verifier handoff to v3 and the manifest to v4, binds fallback authorization to one Release
run/attempt, and requires append-only attempt evidence plus immutable incident recovery. BUILD-CAT-1
still owns the semantic catalog marker. Hexalith.Builds revision `a8a50859…` is the accepted AD-16
lineage predecessor, not the split-reusable revision. Release remains ineligible until owner
acceptance, implementation convergence, and the spine-defined GOV-1 split implementation gate pass.
Source of record:
`_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`; focused spine:
`_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`.

**Historical update (correct-course 2026-07-13; superseded as an authorization model by the 2026-07-15
pre-publication correction and 2026-07-19 GOV-1 architecture):** FR-24 implementation was assigned to
**`REL-2`** (Tenants reusable-workflow alignment), and `REL-1` is closed as superseded. The accepted
historical split placed package inventory/consumer validation in shared CI, publication in the reusable
release, and supplemental post-publication evidence in `release-evidence.yml`. It no longer defines
FR-24 authorization. Under the current authority, literal-40-hex-pinned primary CI emits the
authenticated handoff; the secretless builder executes the exact authenticated candidate and emits
publication-candidate data; the protected candidate-free publisher performs final attestation/fallback,
manifest-v4 sealing, classification, and publication; and `release-evidence.yml` remains an
independently authorized read-only verifier that cannot authorize retroactively. See
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-13-rel-ai-1-fr24-rehome-into-rel-2.md`.

**Historical update (correct-course 2026-07-15; mechanism superseded by AD-19):** keep `REL-2` done
against its accepted G1 criteria, but do not use it to close FR-24. Live v3.2.2 evidence proved that a
green post-release workflow could coexist with blocked/invalid authorization evidence. REL-3 owns the
pre-publication correction and historical ledger reconciliation, but its former publication mechanism
is not implementation authority. The current target is the split builder/publisher contract above. See
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md`.

**Update (truth-state reconciled 2026-08-02):** `REL-4` remains the stop-the-line predecessor to `REL-3`; publication authorization remains closed. Hexalith.Builds issue 17 closed without a qualifying GOV-1 revision. FrontComposer-local GOV-1 work is unblocked, while a reopened issue 17 or successor and its owner-accepted immutable revision still gate reusable-workflow integration, end-to-end exact-candidate/evaluator-handoff proof, completion, release eligibility, and unfreeze. Follow-ups: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-release-freeze-enforcement.md` and `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md`.

**Update (2026-09-09, upstream governed contract):** BUILD-REL-1 now means the opt-in/default-off
`split-publication-v1` contract in the G2 request: one secretless candidate builder, one protected
candidate-free publisher, exact handoff-v3/manifest-v4/fallback/evidence outputs, backward
compatibility, and root-only non-recursive dependency initialization. REL-5's 2026-08-04 decision
removed author signing, production PFX custody, and RFC 3161 author timestamps from the requirements;
mandatory GitHub provenance attestation or approved fallback and NuGet.org repository-signature
verification remain. The deny-only variable can freeze but never authorize. See
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md`.

**Update (approved correct-course 2026-09-22):** completed Epic 9 and Epic 11 delivery history is
closed independently of still-open Product or migration approval. Every G-1 through G-8 and OI-1
through OI-19 obligation is classified in the PRD and the 2026-09-22 sprint change proposal. The
aliases below are an approved proposed-backlog handoff, not final story numbers;
create-epics-and-stories owns final epic grouping and numbering.

| Proposed epic | Implementable story aliases | Approval/evidence tasks | Non-sprint external dependencies |
| --- | --- | --- | --- |
| Planning Truth and Traceability | PLAN-INT-2; PLAN-INT-1 is completed by this reconciliation | — | — |
| Runtime Identity, Tenant Safety, and Adoption | EVT-ID-1, TEN-SCOPE-1, ADOPT-KIT-1 | EVT-APP-1; conditional EVT-XFER-1 and ADOPT-APP-1 | EXT-ADOPTER-1 |
| MCP Production Security | MCP-SEC-1, MCP-SEC-2 | MCP-APP-1 | — |
| UX Conformance and Documentation Parity | UX-A through UX-F, DOC-A through DOC-C | FLUENT-APP-1, PRD-APP-1 | — |
| Governed Split Publication | GOV-B through GOV-I, GOV-SRC-1 | GOV-J, GOV-ACCEPT-1, GOV-ACCEPT-2 | EXT-BUILDS-1 |
| Release Evidence and Decisions | REL-LEDGER-2, REL-BASE-1 | REL-LEDGER-1, REL-A3-APP-1, E9-APP-1 | — |

Each alias has one independently testable boundary in the approved proposal §7. Implementation may
complete without closing its parent gate; owner approvals and external receipts remain open until
their named evidence exists.

**Additional-requirement coverage:** AR1–AR5 → Epic 1 · AR6 (FC-CMD) → Epic 3 · AR7 (FC-CNC) → Epic 4 · AR8 (budgets) → Epic 3 + Epic 4 · AR9 (EventStore status) → Epic 3 · AR10 (rich components) → out of scope (fast-follow, tracked, not an epic) · AR11 (FC-NIP) → Epic 9 · AR12 (FC-TOOL-GOV) → Epic 10.
**Cross-cutting canonical NFRs** apply to every epic as ready-gate constraints, anchored by FC-A11Y (AR2) and FC-DOC (AR4) in Epic 1. Telemetry is owned cross-cutting rather than per-AC — emitting through `FrontComposerActivitySource` on Shell command-lifecycle/projection paths and MCP tool/resource paths.
**Epic 11 (Release Readiness Remediation Program)** traces canonical FR-7, FR-10, FR-12, FR-19, FR-22, FR-25, FR-28, and FR-29 plus the 2026-07-04 architecture-quality-review findings. The epic is done through Story 11.32. Story 11.0 and Story 11.8 are completed decision records. Stories 11.17, 11.18, and 11.19 are decomposition parents, not implementation candidates; their child stories carry delivery status. Story 11.19d approved staged adoption of `AnalysisMode=Recommended` and materialized sequential, separately approval-gated Stories 11.20–11.23. Story 11.25 is completed technical capture rather than G-3 approval; residual exact-tuple evidence is carried forward to EVT-ID-1 (Story 16.1) and EVT-APP-1 (Story 16.3).

## Delivered History List

### Epic 1: Shell Foundation & Bootstrap
An **adopter developer** can stand up a FrontComposer admin shell that boots through the
`Quickstart → AddHexalithDomain → AddHexalithEventStore` path, renders the full-width/constrained
page layout, and is accessible, localized, and documented from day one. Delivers the read-only-MVP
enabler: the confirmed `FC-LYT` layout contract, `FC-A11Y` accessibility primitives, `FC-L10N`
string ownership, `FC-DOC` component docs, and the Shell-integration spike (Story 1.0) + bootstrap
(Story 1.1).
**Canonical FRs covered:** FR-7–FR-9, FR-23 · **ARs:** AR1 (FC-LYT), AR2 (FC-A11Y), AR3 (FC-L10N), AR4 (FC-DOC), AR5 (spike)
**Standalone:** a bootable, accessible, empty shell — no later epic required to function.

### Epic 2: Read-Only Projection Experience  *(the read-only MVP)*
An **operator** can browse domain read-models: registry-driven navigation, an urgency-sorted home
directory, the command palette, and projections rendered from `[Projection]` types into a
`FluentDataGrid` with filtering, expand-in-row detail, status badges, and column prioritization —
fed live from EventStore over SignalR/HTTP, with row-level fresh-item indicators delegated to
Epic 9 / FC-NIP. Confirms the `FC-TBL` table API.
**Canonical FRs covered:** FR-1, FR-3, FR-10–FR-13
**UX-DRs:** UX-DR1, UX-DR2, UX-DR3, UX-DR5, UX-DR6, UX-DR7
**Standalone:** complete read-only operations console; builds on Epic 1, needs no command epic.

### Epic 3: Command Authoring & Lifecycle
An **operator** can submit a command from a generated form and watch it through its full lifecycle
(`Submitting → Acknowledged → Syncing → Confirmed/Rejected`), with the form shape driven by the
density rule. Pins the **FC-CMD** contract (pending-identity / correlation-key shape, uniqueness
scope, `alreadyApplied`, reconciliation), binds the polling coordinator to the confirmed EventStore
status endpoint, and applies the agreed numeric budgets.
**Canonical FRs covered:** FR-2, FR-4, FR-14, FR-15
**ARs:** AR6 (FC-CMD), AR8 (budgets — confirming→degraded, polling), AR9 (EventStore status contract)
**Standalone:** single-command submit→confirm works end-to-end; builds on Epics 1–2.

### Epic 4: Safe & Concurrent Command Execution
An **operator** can run destructive and rapid command sequences safely: destructive-confirmation
dialogs, unsaved-form abandonment guard, the **one-at-a-time** (`FC-CNC`) v1 execution policy with
approved fallback, policy-gated authorization (`[RequiresPolicy]`), and degraded/retry behavior from
the numeric budgets.
**Canonical FRs covered:** FR-16
**ARs:** AR7 (FC-CNC), AR8 (retry/degraded budgets)
**Standalone:** safe command UX layered on Epic 3.
> ✅ *Split accepted (2026-06-21).* Epics 3 & 4 both touch the generated command pipeline + `FcAuthorizedCommandRegion`, split on a genuine **risk boundary** (FC-CMD identity contract vs. FC-CNC concurrency policy). This is the **final v1 structure**: both epics shipped and retro'd (2026-06-04 / 2026-06-05), the dependency is backward (4→3, allowed), and retro-consolidating completed work would be churn with no benefit. Consolidation offer withdrawn.

### Epic 5: AI-Agent (MCP) Surface
An **AI agent** can discover every generated command as an MCP tool (rebuilt at each `tools/list`),
read tenant-scoped projections and the skill corpus as resources, poll command lifecycle via
`frontcomposer.lifecycle.subscribe`, and operate within fail-closed tenant/resource security with
schema-fingerprint negotiation blocking side-effects on mismatch.
**Canonical FRs covered:** FR-17–FR-19
**Standalone:** the same domain surface exposed to agents; builds on the generated manifest, independent of the human UI epics.

### Epic 6: Customization & Extensibility
An **adopter developer** can override the generated UI without forking: Level-2 `ProjectionTemplate`,
Level-3 field slots, and Level-4 full-view overrides from external assemblies, with the
override-accessibility diagnostics (HFC1050–HFC1055) and customization-contract version checks
keeping overrides safe.
**Canonical FRs covered:** FR-3 and FR-5
**Standalone:** extension surface on top of the generated baseline; builds on Epics 2–3.

### Epic 7: Authoring Tooling & Drift Safety
An **adopter developer** can inspect generated output and diagnostics (`frontcomposer inspect`),
migrate across version edges (`frontcomposer migrate`), test generated components with the Testing
library's bUnit host + deterministic fakes, and catch structural/metadata drift against a checked-in
baseline (HFC1065/66) before it ships.
**Canonical FRs covered:** FR-6, FR-20–FR-22, FR-25
**Standalone:** the developer-confidence toolchain; usable against any annotated domain, independent of runtime epics.

### Epic 8: Aspire-grade Visual Refresh  *(post-MVP chrome parity)*
An **operator** experiences shell chrome matching the polish of the **.NET Aspire Dashboard** while the
codebase stays strictly on **Fluent UI v5 components + Fluent 2 tokens**: a **neutral header/footer** (brand
accent demoted to a *thread* — active nav, focus, primary, links, badges — never a surface fill), an
**icon+label navigation rail** with outline→filled active swap + a projection flyout, **compact default
density** + sticky-header grids, a reusable **`FcPageToolbar`** (search + filter + view-menu + underline
tabs), and **colored-icon status** (green check / red cross / grey question, hover+focus label). Aspire runs
Fluent v4/FAST tokens that §4.1 bans here, so every pattern is **translated**, not copied.
**Canonical refinements:** FR-8–FR-11 · **UX-DRs:** UX-DR1, UX-DR2 (amended), UX-DR3 (refined) · **introduces no new FRs**
**Standalone:** each story (8.1–8.7) ships independently; Story 8.1 (header/footer) is a Minor change shippable on its own.
**Source of record:** `sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md` (Correct Course, 2026-06-25).
**Out of framework scope:** Tenants.UI page-body adoption (neutral page titles, `FcPageToolbar` adoption) is a separate **Host-A** Tenants correct-course under submodule approval.

### Epic 9: Fresh-Row Producer and Row Identity  *(post-MVP follow-up)*
An **operator** can see newly materialized projection rows marked after command outcomes, using a
framework-controlled row identity payload and the confirmed `FcNewItemIndicator` component.
**Canonical FRs covered:** FR-13 and FR-26
**ARs:** AR11 (FC-NIP)
**Standalone:** post-MVP enhancement; builds on Epics 2 and 3, and does not reopen the projection nudge seam.
**Source of record:** `sprint-change-proposal-2026-07-01.md` (Correct Course, 2026-07-01).
**Delivery status:** done (reconciled 2026-09-22). The 2026-08-11 retrospective rejected composed
acceptance; Stories 9.3-9.8 delivered the remediation and the live Story 9.8 proof passed 2026-08-27.
Stories 9.1 and 9.2 remain done historical records. E9-APP-1 (Story 13.8) is separate Product
acceptance and does not reopen this epic. Remediation source: `sprint-change-proposal-2026-08-12.md`.

### Epic 10: Tooling Governance Follow-Through *(post-MVP quality hardening)*
An **adopter developer** can trust FrontComposer's authoring-tooling evidence because story file
lists are mechanically reconciled, CLI text output is covered like JSON output, migration sidecar
promises stay honest, and Testing package evidence remains redacted by default.
**Canonical FRs covered:** FR-20–FR-23 and FR-27
**ARs:** AR12 (FC-TOOL-GOV)
**Standalone:** post-MVP quality hardening; builds on Epic 7 and does not reopen completed stories.
**Source of record:** `sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md` (Correct Course, 2026-07-01).

### Epic 11: Release Readiness Remediation Program  *(post-MVP quality hardening)*
An **adopter developer / operator** gets a FrontComposer whose worst blind-spot defects are closed:
circuit-safe EventStore auth and self-healing projection realtime (no silent production-circuit
degradation), fail-closed MCP paths that log and survive across requests, a hardened
open-redirect/storage-key security surface, dead scoped-CSS remediated behind durable
visual-conformance guards, a genuinely fault-injectable Testing harness (the key Tenants-adoption
unblock), a unified command/projection route contract (so palette command activation lands on a page
that exists), a leaner Contracts kernel, and consolidated shell layering + convention alignment.
Remediation-framed, but each story is justified by operator/adopter/security impact and organized into bounded release workstreams.
**Canonical FRs covered:** FR-7, FR-10, FR-12, FR-19, FR-22, FR-25, FR-28, FR-29 · **Introduces:** architecture-review-finding requirements H1–H12 / M-series · **no net-new user-facing FRs**
**Delivery status:** done through Story 11.32 (reconciled 2026-09-22). Story 11.25 is technical capture only; E11R-AI-1 is carried forward to EVT-ID-1 (Story 16.1) and EVT-APP-1 (Story 16.3).
**Delivery model:** Stories 11.0–11.24 are completed history. Stories 11.17, 11.18, and 11.19 are nonimplementable decomposition parents; only their named children enter the queue. The approved 2026-09-12 retrospective-remediation extension adds Stories 11.25–11.32 in the order current identity, immediate gates, acceptance checkpoint, runtime/evidence hardening, artifact integrity, and final acceptance. Epic 11 consumes completed Epic 10 evidence where referenced and does not reopen completed Epics 1–10.
**Source of record:** `sprint-change-proposal-2026-07-04.md` (Correct Course, 2026-07-04), amended by `sprint-change-proposal-2026-09-11.md` (approved 2026-09-12) after `_bmad-output/implementation-artifacts/epic-11-retro-2026-09-10.md` rejected acceptance. A Minor-scope quick-win fix batch was applied in-tree under the original proposal (PR #48).
**Decisions (contract-confirmation DoD — tracked, owned, dated blocking gates):** **11.0** route-contract decision → **Architect + Product**, assigned 2026-07-05, resolved 2026-07-05 with `/commands/{BoundedContext}/{CommandTypeName}`; **11.8** Contracts kernel split decision and compatibility plan → **Architect + PM**, assigned 2026-07-04, resolved 2026-07-05 with the approved `Contracts` kernel + `Contracts.UI` target. Stories 11.11–11.14 are completed delivery records for that package-boundary change.

> **Out of scope (fast-follow, not an epic):** `<AuditTimeline>` and `<ConsequencePreview>` rich
> components (AR10) — approved fallbacks stand; tracked for a later cycle.

## Epic 1: Shell Foundation & Bootstrap

An adopter developer can stand up a FrontComposer admin shell that boots, lays out, and is accessible, localized, and documented from day one — delivering the read-only-MVP enabler (FC-LYT, FC-A11Y, FC-L10N, FC-DOC, the Shell-integration spike, and the bootstrap). Covers canonical FR-7–FR-9 and FR-23; AR1–AR5; ready-gated by canonical accessibility and scoped-lifetime NFRs.

### Story 1.0: Shell-integration spike — verify the bootstrap & table APIs

As an adopter developer (with FrontComposer support),
I want a time-boxed spike that exercises the `AddHexalithFrontComposer*` registration, the generated manifest, projection routing, and the `FC-TBL` table API against a throwaway host,
So that the bootstrap story starts from confirmed, answered API questions instead of assumptions.

**Acceptance Criteria:**

**Given** a throwaway consuming host project referencing the Shell,
**When** the spike wires `AddHexalithFrontComposerQuickstart()`, `AddHexalithDomain<TMarker>()`, and a stub `AddHexalithEventStore(...)`,
**Then** the app starts, the registry is populated from at least one generated `*Registration` type,
**And** each open API question (manifest discovery, projection-route reachability, `FC-TBL` column/filter surface) is recorded as answered/blocked in a short spike note under `_bmad-output/`.

**Given** the spike note,
**When** review completes,
**Then** every 🔴-priority API question from the readiness request (AR5) is marked resolved or escalated with an owner,
**And** the throwaway host is discarded (no spike code merged into `src/`).

### Story 1.1: Bootstrap a minimal, bootable shell

As an adopter developer,
I want my app's `MainLayout` to reduce to `<FrontComposerShell>@Body</FrontComposerShell>` with the three-call DI bootstrap,
So that I get the complete Header/Navigation/Content/Footer frame with zero hand-written layout.

**Acceptance Criteria:**

**Given** an app calling `AddHexalithFrontComposerQuickstart()` → `AddHexalithDomain<TMarker>()` → `AddHexalithEventStore(...)` in that order,
**When** the app starts,
**Then** Fluxor (with `StoreInitializer`), `IStorageService`, `IFrontComposerRegistry`, command/query stubs, and badge/lifecycle/slot/template/view registries are all registered,
**And** the shell renders `FluentLayout` with skip links, `FluentProviders`, and the global shortcuts `Ctrl+,` (settings) and `Ctrl+K` (palette) active. *(FR-7, FR-8)*

**Given** the registration calls are made out of order or one is missing,
**When** the app starts,
**Then** startup fails fast with a message naming the missing/mis-ordered registration rather than failing later at first render.

**Given** the empty shell (no domain types yet),
**When** it renders,
**Then** the content area shows the home directory in an empty state without throwing.

### Story 1.2: Confirm and apply the FC-LYT page-layout contract

As an adopter developer,
I want a confirmed full-width vs. constrained `<PageLayout>` contract on `FrontComposerShell`,
So that every page renders at the correct measure without per-page layout hacks.

**Acceptance Criteria:**

**Given** the `FC-LYT` contract is documented (full-width vs constrained, default, opt-in mechanism),
**When** a page declares constrained layout,
**Then** content renders within the constrained max-measure; full-width pages span the content area. *(AR1, FR-8)*

**Given** the contract document,
**When** Product/UX reviews it,
**Then** it is marked confirmed — or, per the Contract-confirmation Definition-of-Done (2026-06-21), the open question is recorded as a tracked, dated, owned blocking follow-up ("escalated with an owner" alone is **not** Done) — and linked from the component docs (FC-DOC).

**Given** a bUnit render of `FrontComposerShell` in each layout mode,
**Then** the rendered DOM exposes the expected layout container/data attribute for each mode.

### Story 1.3: Establish FC-A11Y accessibility primitives as a ready-gate

As an adopter developer,
I want the shell's accessibility primitives (skip links, focus visibility, `aria-label`/`role`/`aria-live` patterns, keyboard reachability) confirmed and documented as a reusable contract,
So that every later story can satisfy a single, testable accessibility ready-gate.

**Acceptance Criteria:**

**Given** the shell frame,
**When** rendered,
**Then** skip links target the content region, every interactive element carries an accessible name, and focus indicators are visible (no suppressed focus). *(AR2, NFR-3)*

**Given** the documented FC-A11Y primitive set,
**When** a custom override violates one (missing accessible name, keyboard trap, suppressed focus, missing `aria-live` parity, motion without reduced-motion, color without forced-colors),
**Then** the corresponding HFC1050–HFC1055 diagnostic is the agreed enforcement mechanism referenced by the contract.

**Given** the e2e a11y lane (`npm run test:a11y`),
**When** run against the bootstrapped shell,
**Then** it passes with no critical violations.

### Story 1.4: Establish FC-L10N shell-string ownership

As an adopter developer,
I want clear ownership of localized strings between the shell (`FcShellResources.resx`) and the Tenants layer,
So that shell text is localizable without colliding with host-owned strings.

**Acceptance Criteria:**

**Given** the FC-L10N ownership map,
**When** a string is shell-chrome (nav, settings, status, palette),
**Then** it resolves from `FcShellResources.resx` via `AddHexalithShellLocalization(...)`; host/domain strings stay host-owned. *(AR3)*

**Given** a non-default culture is configured,
**When** the shell renders,
**Then** shell-chrome strings display in that culture and no hard-coded English chrome string remains.

**Given** the ownership map,
**When** the Tenants author reviews it,
**Then** it is confirmed — or, per the Contract-confirmation Definition-of-Done (2026-06-21), the boundary question is recorded as a tracked, dated, owned blocking follow-up ("escalated with an owner" alone is **not** Done).

### Story 1.5: Produce the FC-DOC component documentation contract

As an adopter developer,
I want each shell-facing component documented to a confirmed FC-DOC contract,
So that I can adopt components without reading their source.

**Acceptance Criteria:**

**Given** the FC-DOC documentation contract (required sections per component),
**When** a shell component is published,
**Then** its doc page satisfies the contract and is validated by `eng/validate-docs.ps1` (Gate 2d) under `docs/`. *(AR4)*

**Given** the read-only-MVP component set (layout, navigation, DataGrid surface, settings),
**When** Epic 1 closes,
**Then** each has a conforming doc page, or the gap is a dated, owned, blocking backlog item that
names the missing page, owner, due date, and release gate it blocks; an undated owner note is not Done.

### Story 1.6: Theme, density, and settings persistence

As an operator,
I want to set theme and density in a settings dialog and have it persist,
So that the shell remembers my display preferences across sessions.

**Acceptance Criteria:**

**Given** the shell is running,
**When** I press `Ctrl+,` or activate the settings button,
**Then** `FcSettingsDialog` opens with a density radio group, theme toggle, and a density preview panel. *(FR-9, UX-DR4)*

**Given** I change theme or density and confirm,
**When** I reload the app,
**Then** the chosen theme and `data-fc-density` are restored from `IStorageService` (`LocalStorageService`),
**And** density changes are announced via the `aria-live` density announcer. *(NFR-3)*

**Given** the Theme and Density Fluxor slices,
**When** a preference changes,
**Then** exactly one effect owns persistence + JS interop (single-writer discipline, ADR-007).

## Epic 2: Read-Only Projection Experience *(the read-only MVP)*

An operator can browse domain read-models through registry-driven navigation, an urgency-sorted home directory, the command palette, and projections rendered into a filterable, accessible `FluentDataGrid` fed live from EventStore, with row-level fresh-item indicators delegated to Epic 9 / FC-NIP. Covers canonical FR-1, FR-3, and FR-10–FR-13; UX-DR1, 2, 3, 5, 6, 7; confirms FC-TBL.

### Story 2.1: Render a projection from a `[Projection]` type

As an adopter developer,
I want a `[Projection]`-annotated `partial` type to generate a complete projection view,
So that operators get a working read-model page with no hand-written UI.

**Acceptance Criteria:**

**Given** a `partial` class annotated `[Projection]` with a `[ProjectionRole]`,
**When** the project builds,
**Then** the 5 generated files appear under the public generated-output path, and the view dispatches Loading / Empty / Data states per the role. *(FR-1, FR-25)*

**Given** the projection declares `[ProjectionRole.WhenState]`, `[ProjectionEmptyStateCta]`, and badge/format attributes,
**When** rendered,
**Then** the role strategy, empty-state CTA, and Level-1 display formats (`[RelativeTime]`, `[Currency]`) apply. *(FR-3, UX-DR1)*

**Given** a `[Projection]` type that is not `partial`,
**When** built,
**Then** HFC1003 is reported and the build fails under TWAE. *(FR-1, FR-25, NFR-1)*

### Story 2.2: Registry-driven navigation and home directory

As an operator,
I want a navigation tree and a home landing page generated from the registered domain manifests,
So that I can find every bounded context and projection without a hand-built menu.

**Acceptance Criteria:**

**Given** registered `DomainManifest`s,
**When** the shell renders,
**Then** `FrontComposerNavigation` shows a `FluentNav` tree grouped by bounded context with per-projection count and "New" badges. *(FR-10, UX-DR2)*

**Given** the home route (`/`, `/home`),
**When** loaded,
**Then** `FcHomeDirectory` shows urgency-sorted bounded-context cards across its four progressive states. *(FR-10)*

**Given** a compact viewport,
**When** the shell renders,
**Then** navigation collapses to the 48px `FcCollapsedNavRail` / hamburger per the breakpoint watcher. *(UX-DR3)*

### Story 2.3: DataGrid filtering, status, and empty/loading states

As an operator,
I want to filter projection rows and see clear loading/empty/status feedback,
So that I can narrow large read-models and always know the grid's state.

**Acceptance Criteria:**

**Given** a projection grid,
**When** I type in a column filter,
**Then** a debounced `ColumnFilterChangedAction` filters rows, with a filter summary and reset button shown. *(FR-11)*

**Given** the query is loading or returns no rows,
**When** rendered,
**Then** `FcProjectionLoadingSkeleton` (Card/Timeline/Grid) or `FcProjectionEmptyPlaceholder` shows respectively. *(FR-11, UX-DR5)*

**Given** status-enum columns mapped via `[ProjectionBadge]`,
**When** rendered,
**Then** status members render as colored Fluent icons with hover and keyboard-focus tooltip labels plus
an always-present `aria-label`; numeric count slots remain `FluentBadge` / `FcDesaturatedBadge` pills.
*(UX-DR2, NFR-3)*

**Given** a query exceeding the slow-query threshold or the max-items cap,
**When** rendered,
**Then** a non-blocking slow-query / max-items-truncation notice is surfaced above the grid. *(FR-11)*

### Story 2.4: Accessible expand-in-row detail

As an operator using assistive technology,
I want row-detail panels that are always announced correctly,
So that expanded content and filter-hidden expansions are perceivable.

**Acceptance Criteria:**

**Given** a row with detail,
**When** I expand it,
**Then** the detail renders in an always-present `role="region"` panel. *(FR-11, NFR-3, UX-DR6)*

**Given** an expanded row that a filter then hides,
**When** the filter applies,
**Then** a live region announces the hidden expansion (WCAG 4.1.2) via `FcExpandedRowHiddenBanner`.

**Given** the e2e a11y lane,
**When** run against the grid,
**Then** no critical violations are reported.

### Story 2.5: Column prioritization for wide projections

As an operator,
I want wide projections to prioritize the most important columns,
So that >15-column grids stay usable without horizontal overload.

**Acceptance Criteria:**

**Given** a projection with more than 15 columns,
**When** rendered,
**Then** `FcColumnPrioritizer` activates and HFC1029 is reported as info at build. *(FR-11, FR-25)*

**Given** `[ColumnPriority(n)]` annotations,
**When** rendered,
**Then** columns order by priority; a priority collision reports HFC1028 (info).

### Story 2.6: Live projection updates with reconnect & reconciliation

As an operator,
I want projection grids to update live and recover gracefully from connection loss,
So that I see current data and know when the stream is degraded.

**Acceptance Criteria:**

**Given** an active projection subscription over SignalR,
**When** the backend emits a projection change,
**Then** the grid refreshes or reconciles the affected projection lane and surfaces read-path freshness
without marking individual rows as new. *(PRD FR-12, UX-DR5)*

**Given** automatic row-level fresh-item marking is required,
**When** a command outcome carries the confirmed FC-NIP row metadata,
**Then** Story 2.6 does not infer row identity from projection nudges. *(PRD FR-13, FR-26)*

**Given** the SignalR connection drops,
**When** it reconnects,
**Then** `FcProjectionConnectionStatus` surfaces reconnect/reconciliation state and the grid reconciles missed changes. *(UX-DR5)*

**Historical delivery dependency:** Epic 9 / Story 9.2 delivered the row-level implementation, but the
2026-08-11 retrospective rejected it as composed acceptance proof. Stories 9.3-9.8 own the active
dependency. This remains outside Story 2.6 acceptance ownership.

### Story 2.7: Command palette discovery and global search

As an operator,
I want a keyboard-driven command palette,
So that I can jump to any projection or action quickly.

**Acceptance Criteria:**

**Given** the shell is focused,
**When** I press `Ctrl+K`,
**Then** `FcCommandPalette` opens as an ARIA combobox with a search input and keyboard-navigable results. *(FR-10, UX-DR4)*

**Given** a search query,
**When** I type,
**Then** results filter live from the registry and `FcProjectionGlobalSearch` surfaces matching projections.

### Story 2.8: Confirm the FC-TBL table API contract

As an adopter developer,
I want the table/column/filter API surface (`FC-TBL`) confirmed stable,
So that I can build on the DataGrid without breaking-change risk.

**Acceptance Criteria:**

**Given** the FC-TBL API surface exercised by the Story 1.0 spike,
**When** documented and reviewed,
**Then** the column/filter/expand API is marked confirmed-stable — or, per the Contract-confirmation Definition-of-Done (2026-06-21), any open items are recorded as tracked, dated, owned blocking follow-ups ("escalated with owners" alone is **not** Done) — and reflected in `PublicAPI.Shipped.txt` if public. *(NFR-11)*

## Epic 3: Command Authoring & Lifecycle

An operator can submit a command from a generated form and watch it through its full lifecycle, with form shape driven by the density rule and confirmation bound to the EventStore status endpoint. Covers canonical FR-2, FR-4, FR-14, and FR-15; AR6 (FC-CMD), AR8 (budgets), AR9 (status contract).

### Story 3.1: Generate a command form from a `[Command]` type

As an adopter developer,
I want a `[Command]`-annotated type to generate a complete command form and registration,
So that operators get a working submit form with no hand-written UI.

**Acceptance Criteria:**

**Given** a `[Command]` type with a public parameterless ctor and a `MessageId`,
**When** built,
**Then** exactly seven non-page files appear: `CommandForm`, `CommandActions`,
`CommandLifecycleFeature`, `CommandRegistration`, `CommandRenderer`, `CommandLastUsedSubscriber`, and
`CommandLifecycleBridge`; `CommandPage` is additionally emitted only when density is `FullPage`.
*(PRD FR-14)*

**Given** a `[Command]` missing the parameterless ctor or `MessageId`,
**When** built,
**Then** HFC1009 / HFC1006 are reported respectively. *(FR-2, FR-25)*

**Given** an unsupported field type,
**When** rendered,
**Then** `FcFieldPlaceholder` renders it and HFC1002 is reported. *(FR-14)*

### Story 3.2: Apply the density rule to command forms

As an operator,
I want command forms sized to their field count,
So that simple commands are inline and complex ones get a full page.

**Acceptance Criteria:**

**Given** a command's non-derivable property count,
**When** generated,
**Then** ≤1 → `Inline`, 2–4 → `CompactInline`, ≥5 → `FullPage` (with `CommandPage`). *(FR-4)*

**Given** derivable fields (`MessageId`, `CorrelationId`, `TenantId`, `UserId`, timestamps, `[DerivedFrom]`),
**When** the form renders,
**Then** they are excluded from the form and injected server/infrastructure-side. *(FR-4, FR-14)*

**Given** a command exceeding the property thresholds,
**When** built,
**Then** HFC1007 (warn >30 / error >100) and HFC1011 (error >200) apply. *(FR-2, FR-25)*

### Story 3.3: Confirm the FC-CMD pending-identity and correlation contract

As a FrontComposer maintainer,
I want the command-lifecycle identity contract pinned,
So that all command epics share one agreed pending-identity / correlation model.

**Acceptance Criteria:**

**Given** the FC-CMD contract draft,
**When** reviewed,
**Then** the correlation-key shape (the 26-char checkout shape, ASCII ULID), uniqueness scope (per-tenant / user / circuit), lifecycle ownership, `alreadyApplied` semantics, and reconciliation responsibility are each decided — or, per the Contract-confirmation Definition-of-Done (2026-06-21), recorded as a tracked, dated, owned blocking follow-up ("escalated with an owner" alone is **not** Done). *(AR6)*

**Given** the confirmed contract,
**When** a command is dispatched,
**Then** its `messageId`/`correlationId` are ULIDs generated via `IUlidFactory` (never GUIDs). *(FR-14)*

### Story 3.4: Command lifecycle UI

As an operator,
I want to see a command progress through its lifecycle,
So that I know whether it was acknowledged, confirmed, or rejected.

**Acceptance Criteria:**

**Given** a submitted command,
**When** it progresses,
**Then** `FcLifecycleWrapper` surfaces `Submitting → Acknowledged → Syncing` and the terminal or
degraded outcomes `Confirmed`, `IdempotentConfirmed`, `Rejected`, `NeedsReview`, `Warning`, and
`Degraded` via badge/message-bar without treating HTTP acceptance as projection confirmation.
*(PRD FR-15, UX-DR4)*

**Given** a rejection,
**When** received,
**Then** the typed rejection (errorCode/reasonCategory/suggestedAction/docsCode) is shown and the form remains correctable.

**Given** the lifecycle Fluxor slice,
**When** state transitions,
**Then** a single dispatch source owns each transition (single-writer, architecture invariant).

### Story 3.5: Bind the polling coordinator to the EventStore status endpoint

As an operator,
I want command confirmation driven by the real EventStore status query,
So that confirmed/rejected outcomes reflect backend truth.

**Acceptance Criteria:**

**Given** an acknowledged command,
**When** the coordinator polls,
**Then** it binds to `GET /api/v1/commands/status/{id}` (confirmed-stable, not newly built) and transitions to Confirmed/Rejected on the result. *(FR-15, AR9)*

**Given** the endpoint contract,
**When** EventStore maintainers review,
**Then** it is marked confirm-stable — or, per the Contract-confirmation Definition-of-Done (2026-06-21), the gap is recorded as a tracked, dated, owned blocking follow-up ("escalated with an owner" alone is **not** Done).

### Story 3.6: Apply confirming→degraded and polling budgets

As an operator,
I want sensible timing budgets for confirmation,
So that slow commands degrade gracefully instead of hanging.

**Acceptance Criteria:**

**Given** agreed numeric budgets,
**When** a command stays unconfirmed past the confirming→degraded threshold,
**Then** the UI shows a degraded state while continuing to poll within the polling budget. *(AR8)*

**Given** the AR8 budgets confirmed on 2026-06-21,
**When** Product/UX and EventStore evidence is reviewed,
**Then** the implementation verifies the recorded values: confirming-to-degraded threshold `10_000` ms,
polling cadence `1_000` ms, polling max `120_000` ms, Epic 3 retry budget `0`, and Epic 4 retry
budget `1 x 250` ms, all deterministic and testable via `FakeTimeProvider`. *(NFR-8, NFR-11)*

## Epic 4: Safe & Concurrent Command Execution

An operator can run destructive and rapid command sequences safely — confirmation, abandonment guard, one-at-a-time execution, policy-gated authorization, and degraded/retry handling. Covers canonical FR-16; AR7 (FC-CNC), AR8 (retry/degraded).

### Story 4.1: Destructive-command confirmation

As an operator,
I want destructive commands to require explicit confirmation,
So that I can't trigger irreversible actions by accident.

**Acceptance Criteria:**

**Given** a `[Destructive]` command,
**When** I submit it,
**Then** `FcDestructiveConfirmationDialog` shows the configured title/body and requires confirm before dispatch. *(FR-16, UX-DR4)*

**Given** a destructive-verb-named command without `[Destructive]`,
**When** built,
**Then** HFC1020 (info) advises adding it; a `[Destructive]` command with zero non-derivable properties reports HFC1021 (error). *(FR-16, FR-25)*

### Story 4.2: Unsaved-form abandonment guard

As an operator,
I want to be warned before navigating away from an unsaved command form,
So that I don't lose in-progress input.

**Acceptance Criteria:**

**Given** a dirty command form,
**When** I attempt to navigate away,
**Then** `FcFormAbandonmentGuard` (`NavigationLock`) intercepts and shows a `FluentMessageBar` to confirm or cancel. *(FR-16, UX-DR4)*

**Given** a clean form,
**When** I navigate,
**Then** no guard interrupts.

### Story 4.3: One-at-a-time execution policy (FC-CNC)

As an operator,
I want commands to execute one at a time in v1,
So that rapid sequences stay predictable while batching is deferred.

**Acceptance Criteria:**

**Given** an in-flight command,
**When** I submit another local command,
**Then** FC-CNC v1 blocks the later local submit with support-safe feedback rather than queueing,
batching, or racing. *(AR7, PRD FR-16)*

**Given** the FC-CNC contract,
**When** reviewed,
**Then** one-at-a-time is confirmed as the v1 contract and batching is recorded as fast-follow.

### Story 4.4: Policy-gated command authorization

As an operator,
I want commands gated by authorization policy,
So that I only see and run actions I'm permitted to.

**Acceptance Criteria:**

**Given** a `[RequiresPolicy]` command,
**When** the region renders,
**Then** `FcAuthorizedCommandRegion` shows Pending/Authorized/NotAuthorized per `CommandAuthorizationDecisionKind`. *(FR-16)*

**Given** an invalid or duplicate `[RequiresPolicy]`,
**When** built,
**Then** HFC1056 / HFC1057 (errors) are reported. *(FR-16, FR-25)*

**Given** the command service,
**When** dispatching,
**Then** authorization is evaluated before `BeforeSubmit`, `BeforeSubmit` runs only when authorized,
and protected commands are authorized again after `BeforeSubmit` immediately before dispatch; the
`AuthorizingCommandServiceDecorator` remains the service-boundary fail-closed enforcement.
*(PRD FR-16)*

### Story 4.5: Retry and degraded-state handling

As an operator,
I want failed or slow commands to retry within budget and surface a clear degraded state,
So that transient faults recover without manual resubmission.

**Acceptance Criteria:**

**Given** a transient dispatch fault,
**When** it occurs,
**Then** the command performs exactly one retry after exactly `250` ms using the same pre-accept
`MessageId`; a second failure surfaces a retryable degraded state. *(AR8, PRD FR-14)*

**Given** pending/rejected commands,
**When** present,
**Then** `FcPendingCommandSummary` lists them in an `aria-live` region. *(NFR-3)*

### Command FR subclause traceability (PRD FR-14 / FR-15 / FR-16)

Added by correct course 2026-07-05 to make the partial-trace subclauses explicit. Epics 3 and 4 are
done and these behaviors are implemented; this addendum pins each subclause to its owning story and
named symbol. Before v1.0 RC classification, the owning story's evidence/change-log must cite the exact
passing test method(s), or add a short AC-refinement note. This addendum does not reopen any done story.

| PRD subclause | Owning story | Implementation symbol | AC status | Evidence action before RC |
| --- | --- | --- | --- | --- |
| FR-14 unsupported field types render placeholders, do not break the form | 3.1 | `FcFieldPlaceholder` + HFC1002 | In AC | Cite generator/`CommandRenderer*` test. |
| FR-14 supported field-type parsing | 3.1 / 3.2 | generated `CommandForm` parsers | Implicit | Cite `Generated/Level1FormatRuntimeTests.cs`, `CommandRendererTestFixtures.cs`. |
| FR-14 nullable numeric fields compile + round-trip culture-aware | 3.1 | nullable-numeric codegen (PR #48 minor batch) | Implicit | Cite `Generated/Level1FormatRuntimeTests.cs`. |
| FR-14 form state preserved on retryable pre-accept failures | 4.5 | retry/degraded path | Implicit | Cite retry test + `FcFormAbandonmentGuardTests`. |
| FR-14 `MessageId` is a ULID reused across pre-accept retry attempts | 3.3 + 4.5 | FC-CMD identity + `IUlidFactory` | Implicit | Cite `LifecycleStateServiceTests` / pending-command tests. |
| FR-15 Submitting / Acknowledged / Syncing / Confirmed / Rejected | 3.4 | `FcLifecycleWrapper` | In AC | Covered. |
| FR-15 IdempotentConfirmed, NeedsReview, Warning | 3.4 (+ runtime) | `ILifecycleStateService` + `LifecycleStateService` | In AC | Cite `FcLifecycleWrapperRejectionTests` / `FcLifecycleWrapperThresholdTests` in delivery evidence. |
| FR-15 Degraded / accepted-HTTP is not projection-confirmed | 3.5 / 3.6 | `GET /api/v1/commands/status/{id}` confirmed-stable | In AC | Covered (3.5 + 3.6 budgets). |
| FR-16 `[RequiresPolicy]` evaluated before `BeforeSubmit` and again after for protected commands | 4.4 | `AuthorizingCommandServiceDecorator` + `CommandDispatchAuthorizationGate` | In AC | Cite `RequiresPolicyAttributeTests` + authorization tests in delivery evidence. |
| FR-16 service boundary enforces authorization | 4.4 | `AuthorizingCommandServiceDecorator` | In AC | Covered. |
| FR-16 FC-CNC v1 blocks later local submits (no queue/batch) | 4.3 | FC-CNC one-at-a-time | In AC | Covered. |
| FR-16 destructive confirmation / abandonment guard | 4.1 / 4.2 | `FcDestructiveConfirmationDialog` / `FcFormAbandonmentGuard` | In AC | Covered. |

The AC refinements are applied in Stories 3.4 and 4.4: the lifecycle terminals and the
`[RequiresPolicy]` before/after `BeforeSubmit` sequence are now explicit. Both reference existing code
and tests; neither changes implemented behavior.

## Epic 5: AI-Agent (MCP) Surface

An AI agent can discover generated commands as MCP tools, read projections and skill docs as resources, poll lifecycle, and operate within fail-closed security with schema negotiation. Covers canonical FR-17–FR-19.

### Story 5.1: Expose generated commands as MCP tools

As an AI agent,
I want each generated command available as an MCP tool,
So that I can invoke domain commands through the protocol.

**Acceptance Criteria:**

**Given** a generated `McpManifest`,
**When** I call `tools/list`,
**Then** one tool per `McpCommandDescriptor` is built dynamically with its per-descriptor JSON schema. *(FR-17)*

**Given** a `tools/call`,
**When** the args pass admission → schema negotiation → validation,
**Then** the command instantiates, derivable values inject server-side, and dispatch returns an `McpCommandAcknowledgement`. *(FR-17)*

**Given** server-controlled fields (`TenantId`/`UserId`/`MessageId`/`CorrelationId`) in tool input,
**When** received,
**Then** they are blocked/ignored. *(FR-19)*

### Story 5.2: Lifecycle subscription tool

As an AI agent,
I want to poll a command's lifecycle,
So that I can await confirmation after invoking it.

**Acceptance Criteria:**

**Given** the fixed `frontcomposer.lifecycle.subscribe` tool,
**When** I pass a `correlationId`/`messageId` (ULID, ≤64 ASCII),
**Then** I receive an `McpLifecycleSnapshot` (state, terminal, outcome, bounded transitions, nested `retry.retryAfterMs` and `retry.maxLongPollMs`). *(FR-17)*

**Given** a command is invoked in one MCP request/service scope,
**When** `frontcomposer.lifecycle.subscribe` is called from a later, separate request/service scope,
**Then** it resolves the same lifecycle snapshot from cross-request storage without relying on scoped
in-memory state, and the hosting test creates and disposes both scopes independently. *(PRD FR-17)*

**Given** a malformed identifier,
**When** passed,
**Then** the call is rejected without leaking internal state.

### Story 5.3: Projection and skill-corpus resources

As an AI agent,
I want projections and skill docs as MCP resources,
So that I can read tenant data and reference material.

**Acceptance Criteria:**

**Given** a projection resource URI `frontcomposer://<bounded-context>/projections/<projection-name>`,
**When** read,
**Then** tenant-scoped results render as Markdown via `McpMarkdownProjectionRenderer`. *(FR-18)*

**Given** a skill resource `frontcomposer://skills/<id>`,
**When** read,
**Then** only the `agent-reference` section of the conforming doc is served, within the 32 KB cap (oversized → `SkillResourceTooLarge`). *(FR-18)*

### Story 5.4: Fail-closed security gates

As a platform owner,
I want the MCP server to fail closed,
So that missing gates or auth failures never leak the domain surface.

**Acceptance Criteria:**

**Given** startup without both `IFrontComposerMcpTenantToolGate` and `IFrontComposerMcpResourceVisibilityGate`,
**When** the server starts,
**Then** startup throws. *(FR-19)*

**Given** an auth/tenant/unknown failure,
**When** it occurs,
**Then** a single opaque shape is returned (callers can't fingerprint the cause); `tools/list` returns an empty list, not an error. *(FR-19)*

### Story 5.5: Schema fingerprint negotiation

As an AI agent,
I want schema compatibility checked before side-effects,
So that incompatible clients can't dispatch commands.

**Acceptance Criteria:**

**Given** a client `x-frontcomposer-schema-fingerprint` header,
**When** a `tools/call` arrives,
**Then** `McpSchemaNegotiator` classifies the pair (Exact / CompatibleAdditive / CompatibleWarning / Incompatible). *(FR-19)*

**Given** an Incompatible classification,
**When** the call would cause a side-effect,
**Then** it is blocked. *(FR-19, NFR-7)*

## Epic 6: Customization & Extensibility

An adopter developer can override the generated UI at three levels from external assemblies, with accessibility-safety diagnostics keeping overrides correct. Covers canonical FR-3 and FR-5 plus the canonical accessibility NFR.

### Story 6.1: Level-2 ProjectionTemplate overrides

As an adopter developer,
I want to register a custom view template for a projection,
So that I can replace the generated layout without forking.

**Acceptance Criteria:**

**Given** a Blazor component annotated `[ProjectionTemplate]` with a typed `Context` parameter,
**When** registered via `AddHexalithProjectionTemplates<TMarker>`,
**Then** it renders in place of the generated view for its projection+role. *(FR-3, FR-5)*

**Given** an invalid template (bad projection type, missing context, duplicate, version mismatch),
**When** built,
**Then** HFC1033 / HFC1034 / HFC1037 / HFC1035–HFC1036 are reported respectively. *(FR-5, FR-25)*

### Story 6.2: Level-3 field-slot overrides

As an adopter developer,
I want to override individual field rendering,
So that I can customize one field without replacing the whole view.

**Acceptance Criteria:**

**Given** a registered field-slot override with a valid selector and component,
**When** the projection renders,
**Then** the slot's custom fragment replaces the default field render via the slot registry. *(FR-5)*

**Given** an invalid/duplicate slot selector or component,
**When** the corresponding registration or render phase executes,
**Then** HFC1038 is reported at adopter call-site/startup for an invalid selector; HFC1039 is reported
at startup or render for an incompatible component/field type; HFC1040 is reported at startup for a
duplicate projection/role/field tuple; and HFC1041 is reported at startup for an incompatible slot
contract version. Call-site tests, registry-startup tests, and `FcFieldSlotHost` render tests pin those
phases; these are not claimed as SourceTools build diagnostics. *(PRD FR-5)*

### Story 6.3: Level-4 full-view overrides

As an adopter developer,
I want to override an entire projection view from an external assembly,
So that I have a final escape hatch for bespoke pages.

**Acceptance Criteria:**

**Given** a registered Level-4 view override,
**When** the projection route resolves,
**Then** the override registry supplies the full custom view in place of the generated one. *(FR-5)*

**Given** both a Level-2 template and a Level-4 override exist,
**When** resolved,
**Then** precedence is deterministic: Level-4 full-view override, then Level-2 projection template,
then the generated default. Level-3 field slots are consulted only inside a renderer that delegates
field rendering; they do not outrank or compose into a Level-4 replacement. *(PRD FR-5)*

### Story 6.4: Override-accessibility safety diagnostics

As an adopter developer,
I want overrides checked for accessibility regressions,
So that customization can't silently break a11y.

**Acceptance Criteria:**

**Given** a custom override,
**When** built,
**Then** HFC1050–HFC1055 flag missing accessible name, keyboard reachability, suppressed focus, missing `aria-live` parity, motion-without-reduced-motion, and color-without-forced-colors. *(FR-5, NFR-3)*

**Given** DEBUG + `IsDevelopment()`,
**When** a customization-contract mismatch exists,
**Then** `FcCustomizationDiagnosticPanel` displays it.

## Epic 7: Authoring Tooling & Drift Safety

An adopter developer can inspect generated output, migrate across version edges, test generated components, and catch drift before it ships. Covers canonical FR-6, FR-20–FR-22, and FR-25.

### Story 7.1: `frontcomposer inspect`

As an adopter developer,
I want to inspect generated output and diagnostics from the CLI,
So that I can verify what the generator produced without opening `obj/`.

**Acceptance Criteria:**

**Given** generated files + `*.diagnostics.json` sidecars,
**When** I run `frontcomposer inspect [--build] [--format json]`,
**Then** it reports generatedFiles/forms/grids/registrations/mcpManifestEntries/warnings/errors (schema `frontcomposer.cli.inspect.v1`). *(FR-20)*

**Given** `--fail-on-warning` / `--fail-on-error`,
**When** matching diagnostics exist,
**Then** the exit code reflects ActionableFindings (1); unavailable output → 3. *(FR-20)*

### Story 7.2: `frontcomposer migrate`

As an adopter developer,
I want to apply allowlisted code-fix migrations across version edges,
So that I can upgrade safely with a dry-run preview.

**Acceptance Criteria:**

**Given** `--from`/`--to` matching a `MigrationCatalog` edge,
**When** I run `migrate` (dry-run default),
**Then** it plans entries (safe-fix/unchanged/skipped/failed/manual-only/conflict) without writing; `--apply` writes atomically. *(FR-21)*

**Given** a target inside `bin`/`obj`/`.git`/`/generated/`/submodule roots,
**When** `--apply` runs,
**Then** the write is refused (path-safety) and out-of-root paths are `[redacted-path]`. *(FR-21)*

### Story 7.3: Surface the HFC diagnostic catalog

As an adopter developer,
I want generator diagnostics surfaced consistently at build and via inspect,
So that I can act on annotation/usage problems.

**Acceptance Criteria:**

**Given** any HFC1001–HFC1070 condition,
**When** built under TWAE,
**Then** the diagnostic appears with its cataloged severity (errors break the build). *(FR-20, FR-25, NFR-1)*

**Given** `inspect --severity`,
**When** filtered,
**Then** only diagnostics at/above the level are reported.

### Story 7.4: Opt-in drift detection vs. a baseline

As an adopter developer,
I want to detect structural/metadata drift against a checked-in baseline,
So that unintended generated-surface changes are caught before release.

**Acceptance Criteria:**

**Given** `HfcDriftDetectionEnabled=true` and a valid baseline `AdditionalText`,
**When** the generated surface changes structurally or in metadata,
**Then** HFC1065 / HFC1066 are reported at the configured severity. *(FR-6)*

**Given** a missing/malformed/oversized/unsupported baseline,
**When** built,
**Then** HFC1058–HFC1064 are reported per the catalog; the drift pipeline does not depend on `CompilationProvider`. *(FR-6, FR-25)*

### Story 7.5: Testing library — bUnit host and deterministic fakes

As an adopter developer,
I want a pre-wired test host with deterministic fakes,
So that I can unit-test generated components reliably.

**Acceptance Criteria:**

**Given** `FrontComposerTestBase` / `AddFrontComposerTestHost()`,
**When** I write a bUnit test,
**Then** the host auto-registers fakes (`TestCommandService`/`TestQueryService`/`TestProjectionPageLoader`) with `JSInterop.Mode = Loose`. *(FR-22)*

**Given** the configurable command/query/projection fakes,
**When** I drive a scenario,
**Then** rejection, timeout, stall, paging, filtering, sorting, authorization, and async-initialization
outcomes are deterministic and assertable. `TestFaultEvidenceRecorder` records redacted
Drop/Delay/PartialDelivery/Reorder/ReconnectNudge evidence only; it does not claim to inject those
faults. *(PRD FR-22)*

**Given** the Testing library's `PublicAPI.Shipped.txt`,
**When** its exported surface drifts,
**Then** `PackageBoundaryTests` fails until the baseline is intentionally updated. *(NFR-11)*

## Epic 8: Aspire-grade Visual Refresh *(post-MVP chrome parity)*

> **Source of record:** `sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md` (Correct Course,
> 2026-06-25). Raises shell chrome to .NET Aspire Dashboard polish using **Fluent v5 components + Fluent 2
> tokens only** (Aspire's v4/FAST tokens are §4.1-banned, so every pattern is translated). **Framework-only;**
> Tenants.UI page-body adoption is a separate **Host-A** Tenants correct-course. Suggested order:
> 8.1 → 8.2 → 8.3 → 8.4 → 8.5 → 8.6 → 8.7. Each story keeps both light AND dark themes verified and the
> `FluentConformanceTests` Governance lane green (no legacy v4/FAST tokens).

### Story 8.1: Neutral header chrome + footer framing *(Minor — ship first)*

As an operator,
I want the shell header and footer to be neutral chrome with the brand accent used only as an accent,
So that the app looks modern instead of a saturated colored band.

**Acceptance Criteria:**

**Given** the shell header,
**When** rendered in light or dark theme,
**Then** the header band uses `--colorNeutralBackground2` with a `--colorNeutralStroke2` bottom divider, the
app title + action icons read in neutral foreground with sufficient contrast, and no brand-accent surface
fill remains. *(FR-8; §4.1)*

**Given** the shell footer,
**Then** it renders a matching top divider + subtle (`Color.Lightweight`) text on the same neutral chrome.

**Given** the §4.1 Fluent-token guard,
**Then** no legacy v4/FAST token is introduced and the Governance lane stays green; `FrontComposerShellTests`
+ Verify snapshots + a11y/visual baselines are updated intentionally.

### Story 8.2: Accent-as-thread policy + regression guard

As an adopter developer,
I want a documented + guarded rule that the brand accent is never a chrome surface fill,
So that the neutral-header design cannot silently regress.

**Acceptance Criteria:**

**Given** architecture.md §4.1,
**Then** it states the accent (`FcShellOptions.AccentColor`, default `#0097A7`) is a *thread* (active nav,
focus, primary, links, badges) and MUST NOT fill header/nav/footer surfaces (which stay `--colorNeutralBackground*`).

**Given** a `…FluentConformanceTests` Governance guard,
**When** Shell chrome CSS uses `--fc-color-accent`/`--fc-accent-base-color` in a `background`/`background-color`,
**Then** the build fails; the guard ships with an empty, shrink-only allowlist (§4.1 discipline).

### Story 8.3: Brand/logo cell in header-start

As an operator,
I want a proper brand lockup at the top-left,
So that the header reads as a branded product surface like the Aspire logo cell.

**Acceptance Criteria:**

**Given** the header-start cluster,
**When** an adopter supplies a logo-mark fragment,
**Then** it renders exactly once before `AppTitle` with tightened lockup spacing and an accessible name.

**Given** no logo-mark fragment is supplied,
**When** the header renders,
**Then** no logo placeholder or default icon is injected, `AppTitle` remains aligned, and the no-logo
DOM/accessibility baseline is deterministic.

### Story 8.4: Compact default density + grid polish

As an operator,
I want a compact default density and Aspire-dense projection grids,
So that more data is readable at a glance, while I can still change density.

**Acceptance Criteria:**

**Given** a fresh session with no stored preference,
**Then** the default `data-fc-density` is **Compact**, and the choice remains changeable in `FcSettingsDialog`. *(FR-9)*

**Given** a projection grid,
**Then** Compact density resolves to an exact `32px` row metric through
`DataGridDensityMetrics.ResolveRowHeightPx(DensityLevel.Compact)`, row hover uses
`--colorSubtleBackgroundHover`, and the generated `FluentDataGrid` header remains sticky while the
grid body scrolls; rendered DOM/computed-style evidence and regenerated Verify snapshots pin both.

### Story 8.5: Icon+label navigation rail + projection flyout

As an operator,
I want an icon+label navigation rail with an outline→filled active state and a projection flyout,
So that navigation is compact and scannable like the Aspire app-bar while keeping the registry hierarchy.

**Acceptance Criteria:**

**Given** Desktop,
**Then** the primary nav is one rail rendered at **72px labeled** or **48px icon-only**, toggled by the
always-visible hamburger via the existing `SidebarToggledAction`/`SidebarCollapsed`; Mobile/Compact opens the drawer. *(UX-DR3)*

**Given** a bounded-context tile in the 72px labeled rail,
**Then** the tile content stacks the `FluentIcon` above the short label through a Fluent layout
primitive, while aggregate count and "New" badges render outside that icon/label stack as an overlay
indicator row; the active context uses the filled icon, accent left-bar, and `aria-current`.

**Given** a bounded-context tile in the 48px icon-only rail,
**Then** the icon remains centered and the tile keeps an accessible name through `aria-label`/tooltip;
badges remain outside the icon content stack.

**Given** a tile is activated (click/Enter),
**Then** a flyout (`FluentMenu`/`FluentPopover`) lists that context's projections (count + "New" badges); the
single-active-item rule lights the current projection; the flyout is fully keyboard-navigable (Enter/Space,
arrows, Esc, focus-return) with `role="menu"`. *(UX-DR6)*

**Historical composite delivery record:** Story 8.5 is done. The rail, badges, flyout, keyboard/focus
behavior, and accessibility pins were delivered as one composite story. Fluent UI version authority is
the repository’s central package declaration (currently `5.0.0-rc.4-26180.1`), not this historical
story; upgrades must re-run flyout anchoring/keyboard and `data-testid`/`role`/`aria-*` splatting tests.

### Story 8.6: Reusable `FcPageToolbar`

As an adopter developer,
I want a reusable page-toolbar component matching the Aspire toolbar pattern,
So that every page presents a consistent search/filter/view/tab strip.

**Acceptance Criteria:**

**Given** `FcPageToolbar`,
**Then** it renders a `FluentToolbar` with leading `FluentSearch`, a filter `FluentButton`→`FluentPopover`, a
view/overflow `FluentMenuButton`, and a right-aligned actions slot, plus an optional underline `FluentTabs`
strip for multi-view pages; it composes under `FcPageHeader`.

**Given** the FC-DOC contract (Story 1.5),
**Then** the component has a conforming doc page. *(AR4)*

### Story 8.7: Status as colored icon *(UX-DR2 amendment)*

As an operator,
I want status shown as a colored icon with the label on hover/focus,
So that statuses are scannable and lightweight like the Aspire dashboard, without losing accessibility.

**Acceptance Criteria:**

**Given** a `[ProjectionBadge]` status member,
**When** rendered,
**Then** it shows a colored Fluent icon (success = green checkmark, error = red cross, unknown/neutral = grey
question; warning/info extensions) emitted by the generator (`[ProjectionBadge]` emit; regenerated Verify snapshots). *(UX-DR2 amended)*

**Given** the status icon,
**Then** the label is revealed on **hover and keyboard focus** via `FluentTooltip`, and an `aria-label` is
**always** present (never hover-only), preserving NFR-3/WCAG 2.2 AA; numeric count slots keep the `FluentBadge` pill.

**Given** architecture.md §4.1 + epics.md UX-DR2,
**Then** both are amended to record the colored-icon status model superseding the pill-only model.

## Epic 9: Fresh-Row Producer and Row Identity *(post-MVP follow-up)*

**Delivery status:** done. Stories 9.1–9.8, E9-AI-1 through E9-AI-6, and the retrospective are
completed delivery history. The live Story 9.8 proof passed 2026-08-27. G-5/E9-APP-1 remains an
open Product approval task and does not reopen this epic.

> **Source of record:** `sprint-change-proposal-2026-07-01.md` (Correct Course, 2026-07-01). This epic
> resolves the accepted-deferred Story 2.6 AC1(b) gap by giving the row-level new-item producer a current
> backlog home. It does not reopen completed Epics 2 or 3, and it must not fabricate row identity from the
> current projection nudge seam.
> Story 9.1 is done as of 2026-07-05: the approved source is FrontComposer-owned pending-command row
> metadata populated from generated grid/command runtime context. Story 9.2 is also done as a historical
> delivery record, but the 2026-08-11 retrospective rejected its evidence as proof of composed behavior.
> Stories 9.3-9.8 own remediation, and Story 9.8 is the release regression gate. Approved remediation:
> `sprint-change-proposal-2026-08-12.md` (Correct Course, 2026-08-12).

### Story 9.1: FC-NIP row-identity producer decision record

Decision status: **done 2026-07-05**. Approved payload source is FrontComposer-owned pending-command row
metadata populated from generated grid/command runtime context. EventStore status remains lifecycle/status by
`MessageId`; it is not the row-identity source. Contract:
`_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`.

As a FrontComposer maintainer,
I want the closed row-identity payload decision retained for fresh-row indicators,
So that future work does not reopen the source or guess from projection nudges.

**Acceptance Criteria:**

**Given** the approved 2026-07-05 contract,
**When** its payload is consumed,
**Then** FrontComposer-owned pending-command row metadata supplies the exact fields required to call
`INewItemIndicatorStateService.Add(...)`: `ViewKey` or lane key, row `EntityKey`, command `MessageId`,
projection type, and any status-slot metadata needed to avoid ambiguity.

**Given** the current EventStore status endpoint and projection nudge contracts,
**When** row identity is resolved,
**Then** neither is used as the producer: EventStore remains lifecycle/status by `MessageId`, and
projection nudges must not be diffed or used for broad row marking.

**Given** the closed decision record,
**Then** the contract artifact remains the approved base authority; Story 9.2 remains historical delivery
evidence, Story 9.3 owns the successor target-identity decision, and this story does not return to
implementation status.

### Story 9.2: Wire `FcNewItemIndicator` producer and generated-grid consumer

Delivery status: **done**. Retained as historical producer/consumer implementation evidence; the
2026-08-11 retrospective rejected composed acceptance, which is now owned by Stories 9.3-9.8. Story
9.2 is not a queue candidate.

As an operator,
I want rows created or materially changed by a confirmed command outcome to be marked as new,
So that live command results are discoverable in projection grids.

**Acceptance Criteria:**

**Given** the FC-NIP payload contract from Story 9.1,
**When** a command reaches the relevant terminal outcome,
**Then** the command outcome path calls `INewItemIndicatorStateService.Add(...)` with the confirmed
view/lane, `EntityKey`, `MessageId`, and timestamp.

**Given** a generated projection grid for that view/lane,
**When** `INewItemIndicatorStateService.Snapshot(viewKey)` contains entries,
**Then** the grid or shell-level grid wrapper renders `FcNewItemIndicator` with localized copy,
`role="status"`, and `aria-live="polite"` for the matching lane only.

**Given** the row materializes, the filter changes, the TTL expires, or tenant/user scope changes,
**Then** the indicator is dismissed through the existing state-service semantics.

**Given** SourceTools output changes,
**Then** generated Verify snapshots and FC-TBL public-surface tests are updated intentionally.

### Story 9.3: Define explicit command target identity

As a FrontComposer maintainer,
I want every material command outcome to carry immutable target projection-row metadata,
So that fresh-row behavior works for create, same-row, cross-row, and status-move commands without ambient guesses.

**Acceptance Criteria:**

**Given** a command can create or materially change projection rows,
**When** Architect + Product approve the successor FC-NIP contract,
**Then** it names the authoritative source for projection type, view/lane, target `EntityKey`, material-change kind, prior status, expected status, and capture time.

**Given** a standalone create command has no existing row context,
**When** pending metadata is registered,
**Then** its target identity comes from an explicit framework-owned command-to-projection contract, not an existing-row cascade, EventStore `AggregateId`, projection nudge, visible-row diff, or untyped result payload.

**Given** a cross-row, status-move, delete, idempotent, rejected, or no-op outcome,
**When** target semantics are evaluated,
**Then** the intended target and indicator/no-indicator disposition are explicit and the source row is never silently reused as the target.

**Given** the decision is not approved,
**Then** Stories 9.4-9.6 remain blocked and no best-effort identity is implemented.

### Story 9.4: Converge terminal outcomes on one producer boundary

As an operator,
I want every confirmed command path to use the same terminal-outcome boundary,
So that callback and polling confirmations produce identical pending and fresh-row behavior.

**Acceptance Criteria:**

**Given** generated lifecycle callbacks, EventStore polling, reconnect reconciliation, or any other terminal adapter,
**When** a terminal observation arrives,
**Then** it routes through `IPendingCommandOutcomeResolver`; generated adapters do not call `PendingCommandState.ResolveTerminal(...)` directly.

**Given** a lifecycle callback arrives before accepted pending registration is durable,
**When** registration completes,
**Then** the bounded callback is buffered and replayed exactly once through the resolver, preserving cancellation, disposal, and `MessageId` matching.

**Given** the stub callback path and EventStore status path confirm the same contract,
**When** composed tests run,
**Then** each produces one eligible lane entry and duplicate terminal observations do not create another publication.

**Given** SourceTools emits terminal adapters,
**When** Governance scans generated output,
**Then** direct terminal mutation outside the approved owner boundary fails the test.

### Story 9.5: Make indicator state observable and scope-safe

As an operator,
I want fresh-row indicators to appear and disappear immediately in my active scope,
So that the UI never waits for an unrelated render or exposes a previous tenant/user entry.

**Acceptance Criteria:**

**Given** add, materialization, filter/re-query dismissal, TTL expiry, explicit clear, or scope transition mutates indicator state,
**When** the mutation completes,
**Then** generated grids receive one change notification, marshal rendering through `InvokeAsync(StateHasChanged)`, and unsubscribe/dispose safely.

**Given** a generated grid rendered before the mutation,
**When** each mutation scenario occurs,
**Then** bUnit proves automatic DOM appearance/removal without calling `cut.Render()` manually.

**Given** tenant or user scope changes before another producer mutation,
**When** state is read or rendered,
**Then** previous-scope entries are cleared or rejected before `Snapshot` returns and cannot render.

**Given** concurrent timer, clear, and disposal operations,
**When** tests run,
**Then** notification delivery remains race-safe, bounded, and free of disposed-component callbacks.

### Story 9.6: Enforce atomic per-row first-wins

As an operator,
I want one stable fresh-row indication for each row,
So that later confirmations cannot replace its provenance or extend its lifetime unexpectedly.

**Acceptance Criteria:**

**Given** duplicate terminal observations for one `MessageId`,
**When** they reach the producer boundary,
**Then** only the first eligible observation can publish.

**Given** distinct confirmed message IDs target the same `(ViewKey, EntityKey)` while an entry is active,
**When** publication races or occurs sequentially,
**Then** the first entry, `MessageId`, `CreatedAt`, and original expiry win atomically; later attempts do not replace data or reset TTL.

**Given** the first entry expires or is dismissed,
**When** a later material command targets the row,
**Then** a new entry may be accepted under a newly defined active-entry window.

**Given** concurrent publication tests,
**Then** one active entry and one original timer/provenance pair are observed.

### Story 9.7: Add story-ID and commit-scope evidence

As a QA automation maintainer,
I want review completion to prove which commits and files belong to a story,
So that future epic evidence is auditable, bisectable, and isolated from unrelated work.

**Acceptance Criteria:**

**Given** a story baseline and candidate head,
**When** artifact validation runs before review completion,
**Then** it reports every non-merge commit, story-ID match, changed path, File List disposition, and unrelated/interleaved commit.

**Given** implementation, review, or done-transition commits do not map to the story,
**When** the report is evaluated,
**Then** review completion fails until scope is corrected or an explicit shared/process disposition is recorded; published history is not rewritten.

**Given** pre-existing unrelated workspace changes,
**When** validation runs,
**Then** they remain separately reported and are not forced into story ownership.

**Given** the validator changes,
**Then** fixture coverage includes subject-less, wrong-story, shared-process, merge, and interleaved ranges.

### Story 9.8: Prove composed and live Epic 9 acceptance

As an operator and release owner,
I want generated create/update paths proven through a running FrontComposer system,
So that Epic 9 closes on observable behavior rather than isolated implementation tests.

**Acceptance Criteria:**

**Given** Stories 9.3-9.7 are done,
**When** automated composition tests run,
**Then** standalone create, row-context update, cross-row/status move, callback confirmation, and status polling traverse generated command, pending registration, resolver, indicator service, and generated grid boundaries with the intended indicator/no-indicator result.

**Given** a grid is already rendered,
**When** add, materialization, filter/re-query, TTL, clear, or tenant/user transition occurs,
**Then** the DOM updates automatically, remains lane/scoped, preserves first-wins provenance, and retains accessible localized `role="status"`/`aria-live="polite"` behavior.

**Given** the FrontComposer AppHost can build without shared-output locks,
**When** the browser acceptance run executes,
**Then** a durable command log and browser artifact record the repaired scenarios against a running system without stopping unrelated AppHosts.

**Given** live verification is environment-blocked,
**Then** the exact command and blocker are recorded and this story, Epic 9, FR-13, and FR-26 remain open; passing unit lanes are not substituted.

## Epic 10: Tooling Governance Follow-Through

> **Source of record:** `sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md` (Correct
> Course, 2026-07-01). This epic carries forward Epic 7 retrospective actions without reopening
> completed Stories 7.1-7.5.

### Story 10.1: Mechanical story evidence reconciliation

As a QA automation maintainer,
I want changed-file, story File List, and task-completion reconciliation to run before review promotion,
So that story review no longer discovers omitted story-owned files or stale completion claims.

**Acceptance Criteria:**

**Given** a story has a `baseline_commit`,
**When** the reconciliation check runs,
**Then** it compares story-owned changed files against the story File List and reports omitted,
extra, or undocumented files before the story can move to review.

**Given** a workspace has pre-existing unrelated changes,
**When** they predate the story baseline or are explicitly documented as unrelated,
**Then** the check reports them separately without forcing the story to claim ownership.

**Given** story tasks are marked complete,
**When** the check runs,
**Then** it verifies task claims against changed files, test summaries, or explicit documented blockers.

### Story 10.2: Adopter-facing historical-label cleanup

As a technical writer,
I want adopter-facing CLI, diagnostics, and Testing docs free of stale historical story ownership labels,
So that adopters are not sent to obsolete Story 9 provenance when Epic 7 owns the current contract.

**Acceptance Criteria:**

**Given** CLI, migration, diagnostics, Testing README, and published how-to docs,
**When** they describe current Epic 7 behavior,
**Then** adopter-facing text names the current contract or feature, not stale historical story ownership.

**Given** source comments or generated diagnostic registry metadata retain old Story 9 labels as
provenance,
**When** they are not adopter-facing and do not misstate current ownership,
**Then** they may remain documented as brownfield provenance.

### Story 10.3: CLI text-output parity guard

As a Test Architect,
I want text output covered at the same behavioral boundary as JSON output for CLI commands,
So that summaries, filtering, and budgets cannot drift between machine and human output.

**Acceptance Criteria:**

**Given** a CLI command has JSON summary, filtering, fail-flag, or diff-budget behavior,
**When** tests are added or changed,
**Then** text-output pins cover the same shared behavior unless the story explicitly documents why text
does not expose that field.

**Given** a migration or inspect output budget changes,
**When** JSON caps are updated,
**Then** text output caps and omitted-budget markers are updated and tested intentionally.

### Story 10.4: HFCM9002 production-emission decision

As a Product Owner and Architect,
I want an explicit decision on production HFCM9002 migration sidecar emission,
So that adopter docs either promise a real SourceTools emitter or clearly keep HFCM9002 synthetic-only.

**Acceptance Criteria:**

**Given** the current CLI migrate contract,
**When** Product and Architecture review HFCM9002,
**Then** they choose one of two paths: implement a SourceTools production sidecar emitter with tests, or
remove/de-emphasize adopter-facing promises beyond synthetic/manual sidecar evidence.

**Given** production emission is approved,
**Then** SourceTools emits the sidecar, CLI migrate reads it, docs describe it, and tests prove path
safety, redaction, and text/JSON output parity.

**Given** production emission is not approved,
**Then** CLI README and contract docs keep the synthetic-only boundary prominent.

### Story 10.5: Testing evidence redaction default-lane guard

As a developer,
I want Testing package evidence redaction to stay in the default lane,
So that assertion helpers cannot leak tenant, user, token, secret, password, oversized, or
punctuation-heavy secret values.

**Acceptance Criteria:**

**Given** Testing package evidence formatters or fakes change,
**When** the default Testing lane runs,
**Then** it includes redaction cases for tenant/user IDs, token/secret/password keys, oversized payloads,
and punctuation-heavy string secret values.

**Given** a new public Testing helper emits evidence,
**Then** `PublicAPI.Shipped.txt`, README guidance, and redaction tests are updated intentionally.

## Epic 11: Release Readiness Remediation Program *(post-MVP quality hardening)*

**Delivery status:** done. All implementable Stories 11.0–11.9 and 11.11–11.32 are completed
history. Story 11.25 is a completed technical capture, not migration approval; E11R-AI-1 remains
open under EVT-ID-1 (Story 16.1) and EVT-APP-1 (Story 16.3). Later tuple or validator drift creates new work and does not
rewrite this epic.

> **Source of record:** `sprint-change-proposal-2026-07-04.md` (Correct Course, 2026-07-04), triggered by the
> full-repo architecture/engineering-quality review (`_bmad-output/project-docs/architecture-quality-review-2026-07-04.md`:
> no Critical, 12 High, ~28 Medium). A Minor-scope quick-win fix batch (UI-host `FcPageHeader` params, orphaned
> empty-state stylesheet, nullable-numeric codegen, `GeneratedLiteral` escaping incl. a latent quote-injection
> bug, nav-slug unification, theme-watcher disposal race, `@key` on reordering loops, hygiene) was applied
> directly under the proposal (PR #48); the stories below carry the Moderate/Major remainder. **Does not reopen
> completed Epics 1–10.** Epic 11 consumes completed Epic 10 governance evidence where a story cites it. Each story references the review finding IDs it closes in
> its Change Log (proposal success criterion), and the four blind-spot guard classes (unlinked stylesheets,
> dead scoped CSS, parameter-splat surfaces, cross-request lifetimes) each gain a durable Governance test.
> The workstream/current-state table below is authoritative. Do not infer an implementation candidate
> from file order, numeric sort, or a decomposition-parent heading. Stories 11.0 and 11.8 are completed
> decision records; Stories 11.11–11.14 are completed delivery records. Stories 11.17, 11.18, and
> 11.19 are nonimplementable decomposition parents. Only their materialized children carry queue state.
> Story 11.19d approved staged adoption of `AnalysisMode=Recommended` and materialized implementable
> Stories 11.20–11.23 as sequential, separately approval-gated phases; all are done. Story 11.24 is a
> completed historical EventStore authorization record. The approved 2026-09-12 retrospective-remediation
> extension added Stories 11.25–11.32; all eight are completed delivery history as of 2026-09-22.
>
> **Decision gates (contract-confirmation DoD, 2026-06-21 amendment - tracked, owned, dated):** **Story 11.0**
> (command/projection route contract) - owner **Architect + Product**, assigned **2026-07-05**, resolved
> **2026-07-05** with `/commands/{BoundedContext}/{CommandTypeName}` as the canonical generated command route
> family, recorded in `_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`.
> **Story 11.8** (Contracts kernel split
> decision and compatibility plan, amends the multi-TFM decision) - owner **Architect + PM**, assigned **2026-07-04**,
> resolved **2026-07-05** by approving the split and recording package-compat requirements in
> `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`.
> **IA gate FC-IA-1** (module-tab route encoding + projection-flyout IA) was resolved and signed off
> on **2026-07-05** by **Product/UX + Architect**. Canonical module/tab routes are `/{module}/{tab}`;
> the projection flyout is secondary IA. The decision is recorded in
> `_bmad-output/contracts/fc-ia-1-module-tab-ia-decision-2026-07-05.md`.

### Epic 11 Workstreams And Current State

| Workstream | Stories | Current state on 2026-09-22 |
| --- | --- | --- |
| Completed delivery history | 11.0–11.9, 11.11–11.24 | Done; 11.24 and identity v1 remain historical authorization, not the current release target. |
| Identity capture and immediate gate recovery | 11.25–11.28 | Done. Story 11.25 captured technical identity history only; G-3 approval remains separate. |
| Runtime and evidence hardening | 11.29–11.31 | Done. |
| Artifact integrity | 11.32 | Done as accepted delivery history; current validator drift is proposed PLAN-INT-2. |
| Residual exact-tuple approval | EVT-ID-1, EVT-APP-1 | New work under G-3/E11R-AI-1, materialized as Stories 16.1 and 16.3; not part of completed Epic 11. |

Within logging remediation, ownership precedence is deterministic: 11.18a security/fail-closed sites
first, 11.18c command-lifecycle/projection/polling hot paths second, and 11.18b residual
Warning/Error/Critical sites last. A site belongs to exactly one child. Parent Stories 11.17, 11.18,
and 11.19 must never receive backlog or ready-for-dev status.

### Story 11.0: Command/projection route-contract decision gate

Decision status: **done 2026-07-05**. Canonical generated command route family is
`/commands/{BoundedContext}/{CommandTypeName}`. Contract:
`_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`.

As a Product Owner and Architect,
I want the command route family selected before Epic 11 implementation starts,
So that command activation from the palette and empty-state CTA targets real generated pages.

**Acceptance Criteria:**

**Given** the current route families — projection links `/{bc-lower}/{proj-kebab}`, palette/CTA command links (`/domain/{kebab}/{kebab}`), and generated command pages (`/commands/{BC}/{TypeName}`),
**When** Architect + Product review the route contract,
**Then** they select one canonical command route family and record the decision in a contract artifact or `architecture.md` section. *(H10 remainder; refines FR-10 / UX-DR4.)*

**Given** the route decision is recorded,
**When** Story 11.7 is created,
**Then** it implements only the selected route contract and adds the e2e route-activation pin.

**Given** Story 11.0 is not done,
**When** any Story 11.1+ `create-story` request is made,
**Then** the request is blocked with the dated owner and decision status.

### Story 11.1: Token lifecycle and circuit-safe EventStore auth

As a FrontComposer operator,
I want EventStore auth tokens stored, expired, and evicted on sign-out, and acquired safely from an interactive Blazor circuit,
So that the app does not silently lose its EventStore connection whenever there is no `HttpContext`.

**Acceptance Criteria:**

**Given** `FrontComposerUserTokenStore`,
**When** a token is stored,
**Then** its expiry is retained, expired entries are evicted, and the currently-dead `Remove` path is wired into the sign-out endpoint. *(H2)*

**Given** `FrontComposerAccessTokenProvider` running inside an interactive circuit (`HttpContext` null),
**When** it acquires a token,
**Then** it falls back to the `CircuitServicesAccessor`/token-store seam its siblings already have — or, if no circuit-safe source is configured, it fails fast at registration instead of throwing HFC2013 at read time. *(H2, M1)*

**Given** any token path,
**When** token storage, acquisition, eviction, or sign-out paths execute,
**Then** no raw token value is logged, and expired/sign-out eviction and circuit-context acquisition are pinned by tests.
*(Refines FR-7 and FR-12; closes H2, M1.)*

### Story 11.2: Projection realtime resilience

As an operator,
I want projection realtime to recover from outages instead of silently degrading to slow polling,
So that live grids keep updating after a hub disconnect longer than the default retry ladder.

**Acceptance Criteria:**

**Given** the projection hub,
**When** the connection drops for longer than the ~42 s default retry ladder,
**Then** an unbounded jittered `IRetryPolicy` plus restart-on-`Closed` (gated by the fallback driver) reconnects, instead of dying permanently and silently degrading to 15 s polling. *(H6)*

**Given** `ProjectionSubscriptionService.DisposeAsync`,
**When** the service is disposed while startup, polling, or cache seeding work is in flight,
**Then** its gate wait is bounded, the two polling drivers' disposal is aligned, `FrontComposerRegistry` live-list reads are locked, and the `ETagCacheService` seeding race is fixed (`Lazy<Task>`/semaphore, reset on failure). *(M2, M3, M4)*

**Given** the realtime wire contract,
**When** SignalR projection subscriptions are created and messages are handled,
**Then** hub method-name literals (`ProjectionChanged`, `JoinGroupScoped`, …) are pinned and `SignalRProjectionHubConnectionFactory` gains direct unit tests.
*(Refines FR-12; closes H6, M2, M3, M4.)*

### Story 11.3: MCP cross-request lifecycle and operability

As an AI agent,
I want the MCP lifecycle tracker to work across separate requests and every fail-closed branch to leave a trace,
So that a `subscribe → poll` sequence returns real transitions and operators can diagnose silent denials.

**Acceptance Criteria:**

**Given** `FrontComposerMcpLifecycleTracker`,
**When** it is registered,
**Then** it is split into a **Singleton state store + Scoped facade** (a naive Singleton flip is a captive-dependency error — it constructor-injects the Scoped admission service), and the test-side Singleton re-registrations that masked the bug are removed. *(H4, corrected per proposal §1)*

**Given** an agent lifecycle `subscribe` then `poll` across two requests,
**When** the MCP lifecycle tools are invoked through separate service scopes,
**Then** real transitions are returned (cross-scope hosting test).

**Given** the zero-signal fail-closed sites (`FrontComposerMcpProjectionReader` bare catch, tools-list, lifecycle auth),
**When** those branches deny, hide, or downgrade a request,
**Then** each logs exactly one sanitized `[LoggerMessage]` event, `BuildServiceProvider()` is removed from `AddFrontComposerMcp` (ASP0000), and API-key hashes are stored (or dev-only is documented). *(M9, M10, M12)*
*(Refines FR-17 and FR-19; closes H4, M9, M10, M12.)*

### Story 11.4: Security-validation hardening

As a FrontComposer maintainer,
I want the open-redirect funnel and storage-key builders exhaustively tested and the wire formats pinned,
So that a redirect-validation gap or storage-key collision cannot slip through untested.

**Acceptance Criteria:**

> Structure the story file as **three independently verifiable task groups** (redirect theory · storage-key convergence · wire-format pins).

**Given** `ReturnPathValidator` (today with zero direct tests),
**When** the security theory runs,
**Then** it covers every documented attack class — protocol-relative, backslash prefixes, percent-decode bypass, traversal, BiDi/zero-width, the Unix file-scheme carve-out, and non-root base href. *(H7)*

**Given** the two storage-key builders,
**When** scope keys are produced for whitespace, colon, NFD/NFC, and mixed-case-email inputs,
**Then** they converge on the canonicalizing `FrontComposerStorageKey` semantics with an FsCheck equivalence property (whitespace/colon/NFD-NFC/mixed-case-email). *(H9)*

**Given** the SignalR/HTTP wire DTOs,
**When** they serialize to or deserialize from JSON,
**Then** `ProjectionChangedDetail`, `CommandResult`, and `ProblemDetailsPayload` gain golden-JSON pins (or `[JsonPropertyName]`) and `CommandResultStatus` gets string constants. *(M11)*
*(Anchored to PRD NFR-5 Security and NFR-6 Privacy/support safety; closes H7, H9, M11.)*

### Story 11.5: Dead-CSS remediation and visual-conformance guards

As an operator,
I want components whose styling is silently dead to actually render their styles, guarded so the defect class cannot regenerate,
So that connection status (incl. the reconnect pulse), the column prioritizer, settings-dialog mobile controls, and density preview look as designed.

**Acceptance Criteria:**

**Given** the seven scoped-CSS files whose rules are dead because the class sits on a Fluent component (`FcProjectionConnectionStatus` — all rules incl. the reconnect pulse, `FcColumnPrioritizer` gear pinning, `FcSettingsDialog` mobile Done, `FcDensityPreviewPanel`, three DevMode files),
**When** they are fixed via a raw scoped root + `::deep` or inline Style (Story 8.6 precedent),
**Then** the intended styling applies, proven by rendered-DOM / computed-style evidence per E8-AI-1. *(M6)*

**Given** the undefined/FAST-era tokens (`--error`, `--error-foreground-rest`),
**When** Shell component CSS is scanned and migrated,
**Then** they are replaced with Fluent 2 tokens. *(M5)*

**Given** three new Governance guards,
**When** the Governance lane scans Shell stylesheet references and scoped CSS patterns,
**Then** every `wwwroot/css` file must be referenced by a `<link>`, a scoped-CSS-class-on-Fluent-component detector fails the build, and `error-` is added to the legacy-token regex.

> **Guard-first:** build the three guards before/with the CSS fixes — bUnit cannot detect dead CSS or silent splats, so the defect class regenerates otherwise.
*(Amends NFR-3 and NFR-4 visual conformance; closes M5, M6 + the unlinked-stylesheet and dead-scoped-CSS guard gaps.)*

### Story 11.6: Testing harness failure modes

As an adopter developer (starting with Hexalith.Tenants),
I want the Testing harness to model rejection/timeout/stall and per-request query outcomes,
So that adopters can genuinely test failure paths and paging/filter/sort of generated components.

**Acceptance Criteria:**

**Given** `TestCommandService`,
**When** adopter tests configure command and query outcomes,
**Then** it exposes configurable rejection / timeout / stall-at-`Syncing` outcomes;
`TestQueryService` / `TestProjectionPageLoader` accept per-request callbacks
(`SucceedWith(Func<QueryRequest, QueryResult<T>>)`) so paging/filter/sort are testable; and the
evidence-only `TestFaultEvidenceRecorder` records redacted observations without claiming fault
injection. *(M21)*

**Completed delivery clarification (2026-07-15):** configurable fake outcomes own rejection,
timeout, stall, query paging/filter/sort, authorization states, and async initialization.
`TestFaultEvidenceRecorder` is deliberately evidence-only: it captures redacted fault observations and
does not claim to inject runtime faults. The former `TestFaultInjectionProvider` name is retired.

**Given** the Counter sample's authorization-policy toggles,
**When** those scenarios are promoted into the Testing harness,
**Then** the harness exposes equivalent configurable authorization-policy states, and the constructor
`GetAwaiter().GetResult()` is replaced with an async factory.

**Given** the shipped Testing surface (currently 2 test files for 11 files),
**When** builders, assertions, or fakes are changed,
**Then** `Builders` / `Assertions` / fakes get direct surface tests and `PublicAPI.Shipped.txt` is updated intentionally.

**Given** Story 10.5's Testing evidence privacy findings and the Testing host contract,
**When** Story 11.6 changes fake services, per-request callbacks, builders, assertions, or fault/evidence
paths that emit diagnostic or assertion evidence,
**Then** the default Testing lane preserves redaction for configured tenant/user identifiers in JSON values
and property names, including dictionary keys, preserves structural redaction of token/secret/password keyed
values, and proves raw external/local paths are absent or replaced with bounded repository-relative or redacted
markers wherever the harness emits paths.
*(Refines FR-22; closes M21 — the key Tenants-adoption unblock.)*

### Story 11.7: Command/projection route-contract implementation

As an operator,
I want command activation from the palette/CTA to land on a page that actually exists,
So that the "jump to any action" journey does not dead-end on an unresolvable route.

**Acceptance Criteria:**

**Given** Story 11.0 has selected `/commands/{BoundedContext}/{CommandTypeName}` as the canonical generated command route family, and FC-IA-1 has fixed module-page/tab routes to `/{module}/{tab}` with the projection flyout strictly secondary (`_bmad-output/contracts/fc-ia-1-module-tab-ia-decision-2026-07-05.md`),
**When** palette command entries, projection empty-state CTAs, and generated command pages are rendered,
**Then** every generated command activation targets a route that exists and uses the selected route contract. *(H10 remainder; proposal §1 correction #3.)*

**Given** the selected route contract is implemented,
**When** the e2e command-palette activation pin runs,
**Then** an e2e pin asserts palette command activation lands on the generated page, and the route contract is recorded in a contract (`fc-*` or architecture.md §4), not only in the story.

**Given** the contract-confirmation DoD,
**When** Story 11.0 is not done **or** the FC-IA-1 module-tab route encoding / projection-flyout IA gate
is not Product/UX-signed-off,
**Then** this story remains blocked and may not move to ready-for-dev.
*(Refines FR-10 / UX-DR4; closes the H10 remainder + the unresolvable-route finding — the single most user-visible open defect in the plan.)*

### Story 11.8: Contracts kernel split decision and compatibility plan

Decision status: **done 2026-07-05**. Approved package-boundary target: keep `Contracts` as the
netstandard2.0-clean wire/attribute/schema/diagnostic kernel, move the net10/Blazor/Fluent rendering
surface to `Contracts.UI`, and complete package compatibility/public API/deprecation evidence in the
pre-v1.0 window before Stories 11.11-11.14 are marked done. Contract:
`_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`.

As an Architect and Product Manager,
I want the Contracts kernel split and compatibility path explicitly approved,
So that package-impacting implementation stories do not start without a v1.0 migration plan.

**Acceptance Criteria:**

**Given** the net10/Blazor surface (`Typography`/`FcTypoToken`, `RenderFragment` contexts, `KeyboardEventArgs` members),
**When** Architect + PM review the kernel split,
**Then** they approve, defer, or narrow the split and record the compatibility plan before Story 11.11 starts. *(H11)*

**Given** the decision is recorded,
**When** implementation stories are created,
**Then** they are split into Contracts.UI assembly work, misplaced-type relocation, `QueryRequest` migration, and documentation/package-compat updates.

**Given** the decision amends the documented multi-TFM decision,
**When** the plan is approved,
**Then** the decision names the affected packages, expected public API baseline changes, deprecation path, and release compatibility posture.
*(Amends NFR-2 and FR-25; gates H11, M24, M25 implementation.)*

### Story 11.11: Create Contracts.UI assembly and migrate Blazor rendering surface

Delivery status: **done**. Retained as the historical acceptance contract for the completed
Contracts.UI split; it is not a queue candidate.

As an adopter developer (Hexalith.Tenants first),
I want the net10/Blazor rendering surface split out of the netstandard Contracts kernel,
So that referencing Contracts stops inheriting the pinned Fluent RC.

**Acceptance Criteria:**

**Given** the approved Story 11.8 decision,
**When** the net10/Blazor surface is moved,
**Then** `Typography`/`FcTypoToken`, `RenderFragment` contexts, and `KeyboardEventArgs` members live in a net10-only Contracts.UI assembly or approved equivalent. *(H11)*

**Given** a consumer references only the Contracts kernel,
**When** package and project-reference validation runs,
**Then** it no longer inherits the pinned Fluent RC through Contracts.

**Given** public surfaces move,
**When** package boundary tests run,
**Then** public API baselines and docs are updated intentionally.

### Story 11.12: Relocate runtime and testing-owned types out of Contracts

Delivery status: **done**. Retained as the historical acceptance contract for completed type
relocation; it is not a queue candidate.

As a framework maintainer,
I want runtime services and test fakes removed from the Contracts kernel,
So that Contracts remains a stable wire/attribute package instead of a runtime grab bag.

**Acceptance Criteria:**

**Given** misplaced runtime and testing types,
**When** relocation is implemented,
**Then** `InMemoryStorageService` moves to Testing, `InlinePopoverRegistry` implementation moves to Shell while its contract remains where approved, `FcShellOptions` moves to Shell/options, and Fluxor action records move to Shell. *(M24)*

**Given** `LoadPageAction` currently carries a `TaskCompletionSource`,
**When** the action records move,
**Then** value-semantics and serialization concerns no longer leak into Contracts.

**Given** the relocation changes package surfaces,
**When** tests and package validation run,
**Then** consumer-facing packages still build and public API deltas are intentional.

### Story 11.13: Decompose `QueryRequest` through the HFC0001 migration path

Delivery status: **done**. Retained as the historical acceptance contract for the completed
QueryRequest migration; it is not a queue candidate.

As an adopter developer,
I want UI query concerns separated from transport and caching concerns,
So that query contracts are stable, composable, and migratable before v1.0.

**Acceptance Criteria:**

**Given** the 19-parameter `QueryRequest`,
**When** decomposition is implemented,
**Then** UI-facing query criteria and EventStore transport/caching envelope concerns are separated via the existing HFC0001 deprecation pipeline. *(M25)*

**Given** existing generated and runtime consumers,
**When** they compile against the migrated contracts,
**Then** source compatibility, obsolete diagnostics, and migration guidance are covered by tests.

**Given** the CLI and MCP surfaces serialize query-related shapes,
**When** wire-shape tests run,
**Then** public JSON shape changes are either avoided or explicitly versioned.

### Story 11.14: Update architecture, project context, UX trace, and package compatibility docs

Delivery status: **done**. The package-boundary, public-API, planning, and compatibility evidence was
completed with Stories 11.11–11.13. This section is release history, not open documentation work.

As a release owner,
I want the kernel split and query migration reflected in planning and published references,
So that adopters understand the new package boundaries and compatibility story.

**Acceptance Criteria:**

**Given** Stories 11.11-11.13 change package boundaries or public contracts,
**When** documentation is updated,
**Then** `project-context.md`, architecture layer documentation, UX-DR1's home for `Typography`/`FcTypoToken`, release notes, and package-compat guidance reflect the approved shape.

**Given** docs are changed,
**When** documentation validation runs,
**Then** generated planning docs remain under `_bmad-output/` and published docs under `docs/` are updated only where product references require it.

### Story 11.9: Shell layering declaration and route/label relocation

As a FrontComposer maintainer,
I want the real Shell layering declared and route/label helpers moved out of the render layer,
So that dependency direction is visible and enforceable.

**Acceptance Criteria:**

**Given** shell layering,
**When** architecture boundaries are updated,
**Then** Telemetry is declared cross-cutting, connection/polling workers move to Infrastructure, and folder dependency direction is documented. *(M18)*

**Given** navigation route and label helpers live on the Razor component,
**When** the helpers are relocated,
**Then** `BuildRoute` and `ProjectionLabel` move into `Routing/` and render components call the shared helpers.

**Given** the declared layering,
**When** architecture tests run,
**Then** folder dependency directions are pinned.
*(closes M18 subset; depends only on the already-landed Minor batch — PR #48 — so it is backward, not forward.)*

### Story 11.15: Storage scope and snapshot publisher consolidation

As a FrontComposer maintainer,
I want duplicated scope-resolution and snapshot-publisher helpers consolidated,
So that tenant/user hardening and subscription behavior are applied uniformly.

**Acceptance Criteria:**

**Given** multiple `TryResolveScope` implementations exist,
**When** storage scope resolution is consolidated,
**Then** all Shell persisted features use a single `StorageScopeResolver` and tests cover tenant/user fail-closed behavior. *(M19)*

**Given** several hand-rolled snapshot pub/sub containers exist,
**When** `SnapshotPublisher<T>` or an approved equivalent is introduced,
**Then** subscription, disposal, fault isolation, and snapshot behavior are covered once and reused by former duplicate sites. *(M19)*

**Given** duplicated call sites are removed,
**When** the story validation runs,
**Then** before/after call-site reduction is documented in the story File List or Change Log.

### Story 11.16: Fatal, hydration, JSON, and generated-literal helper consolidation

As a FrontComposer maintainer,
I want small duplicated helpers consolidated by defect class,
So that hardening fixes do not depend on remembering every copy.

**Acceptance Criteria:**

**Given** fatal-exception filters exist in multiple variants,
**When** `ExceptionGuard.IsFatal` or an approved equivalent is introduced,
**Then** all former fatal-filter sites use the same helper and focused tests cover cancellation, fatal, and non-fatal cases. *(M19)*

**Given** repeated hydration enums and JSON options exist,
**When** they are consolidated,
**Then** a single `HydrationState` enum and shared `FcJson` options are used where the semantics match.

**Given** `RoleBodyHelpers` still owns an escaping path,
**When** generated literal escaping is consolidated,
**Then** it delegates to the shared `GeneratedLiteral` path without regressing generated-source parsing.

### Decomposition Parent 11.17: Mechanical one-type-per-file split

> **Nonimplementable decomposition parent.** Queue state belongs only to 11.17a–d; this parent must
> never move to backlog, ready-for-dev, or review.

**Decomposition (correct course 2026-07-05).** Split by package into independently reviewable child
stories. Each keeps the parent constraint: mechanical only — behavior and public-API shape unchanged
except intentional file organization and any documented API-baseline update. A durable one-type-per-file
Governance guard (the "multi-type file" blind-spot guard class) is added or extended so the convention
is enforced, not merely applied.

#### Story 11.17: CLI package split

- **11.17a — CLI package split (`11-17-cli-package-split.md`, done).** `MigrationCommand.cs` (23 types), `InspectCommand.cs` (14 types) →
  one-type-per-file. Validation lane: CLI in-process xUnit lane + `frontcomposer.cli.inspect.v1` /
  `frontcomposer.cli.migrate.v1` contract pins + CLI `PublicAPI.Shipped.txt` unchanged.
#### Story 11.17: SourceTools package split

- **11.17b — SourceTools package split (`11-17-sourcetools-package-split.md`, done).** `DriftDetection.cs` (17 types) → one-type-per-file.
  Validation lane: SourceTools drift lane + HFC parity + generated-output byte stability (P12
  no-`CompilationProvider` isolation preserved).
#### Story 11.17: MCP/runtime split and benchmark relocation

- **11.17c — MCP/runtime split + benchmark-harness relocation (`11-17-mcp-runtime-split-and-benchmark-relocation.md`, done).** `SkillCorpus.cs` (~45 types) →
  one-type-per-file, and move the LLM benchmark harness out of the runtime package into
  `Shell.Tests.Bench` (`[Trait("Category","Performance")]`). Validation lane: MCP in-process lane +
  Testing package-boundary tests + `Shell.Tests.Bench` builds; the runtime package no longer ships the
  benchmark harness.
#### Story 11.17: Shell bundle split

- **11.17d — Shell interface+impl+DTO bundle split (`11-17-shell-bundle-split.md`, done).** Shell multi-type files (interface + impl + DTO
  bundles) → one-type-per-file, retaining the documented Fluxor action-group exception. Validation
  lane: focused Shell one-type-per-file Governance guard + broad Shell non-Contract lane +
  `PublicAPI.FcTbl.Shipped.txt` unchanged.

As a FrontComposer maintainer,
I want the worst multi-type files split mechanically,
So that the codebase matches the documented one-type-per-file convention before broader refactors.

**Acceptance Criteria:**

**Given** the worst multi-type files (`MigrationCommand.cs` 23 types, `SkillCorpus.cs` ~45 — move the LLM benchmark harness out of the runtime package, `DriftDetection.cs` 17, `InspectCommand.cs` 14, plus the Shell interface+impl+DTO bundles),
**When** the mechanical split runs,
**Then** they are split one-type-per-file (the Fluxor-action-group exception documented if retained). *(M14)*

**Given** the split is mechanical,
**When** tests and generated-output checks run,
**Then** behavior and public API shape remain unchanged except for intentional file organization and any documented API baseline updates.

### Decomposition Parent 11.18: LoggerMessage migration for warnings and hot paths

> **Nonimplementable decomposition parent.** Queue state belongs only to 11.18a–c; this parent must
> never move to backlog, ready-for-dev, or review.

**Decomposition (correct course 2026-07-05).** Split by defect class, security-adjacent work first.
Each child preserves the parent's sanitization constraint: no raw token, tenant-secret, payload, stack
trace, or sensitive identifier is emitted.

#### Story 11.18: Fail-closed security log sites

- **11.18a — Fail-closed / security log sites (`11-18-fail-closed-security-log-sites.md`, done).** MCP + Shell fail-closed branches →
  `[LoggerMessage]`. Validation lane: MCP + Shell Governance sanitized-logging lane (ties to
  NFR-6/NFR-11); sanitization tests prove no sensitive value is emitted.
#### Story 11.18: Warning-and-above log sites

- **11.18b — Residual warning-and-above log sites (`11-18-warning-and-above-log-sites.md`, done).** After 11.18a security and 11.18c hot-path ownership is frozen, all residual Warning/Error/Critical direct sites in the 49-file census →
  `[LoggerMessage]`. Validation lane: Shell unit lane + a guard that Warning+ sites use
  source-generated logging.
#### Story 11.18: Hot-path log sites

- **11.18c — Hot-path log sites (`11-18-hot-path-log-sites.md`, done).** Command-lifecycle, projection-refresh, and polling hot-path sites →
  `[LoggerMessage]`. Validation lane: LoggerMessage guard; remaining direct calls are below the
  migration threshold or documented intentional.

As a FrontComposer maintainer,
I want warnings and hot logging paths migrated to source-generated logging,
So that logging follows the project's performance and analyzer conventions.

**Acceptance Criteria:**

**Given** the post-11.18a census has 208 direct log calls across exactly 49 Shell files — 117 at
Warning/Error/Critical and 91 at Trace/Debug/Information —
**When** warning-and-above and hot-path log sites are migrated,
**Then** the exact inventory is frozen before edits, each site is assigned once by the security → hot
path → residual Warning+ precedence, and owned sites migrate to `[LoggerMessage]`. *(M15)*

**Given** MCP and Shell fail-closed branches log sanitized details,
**When** the logging tests run,
**Then** no raw token, tenant-secret, payload, stack trace, or sensitive identifier values are emitted.

**Given** direct logger calls remain,
**When** review checks run,
**Then** remaining direct calls are either below the migration threshold or documented as intentional.

### Decomposition Parent 11.19: Enforcement and policy alignment

> **Nonimplementable decomposition parent.** Queue state belongs only to 11.19a–d; this parent must
> never move to backlog, ready-for-dev, or review.

**Decomposition (correct course 2026-07-05).** Split by defect class. Each child names its validation
lane and does not disable warnings or analyzer findings globally.

#### Story 11.19: Doc-comment enforcement realignment

- **11.19a — Doc-comment (CS1591) enforcement realignment (`11-19-doc-comment-enforcement-realignment.md`, done).** Restore documented CS1591 enforcement on
  the Contracts public API-freeze folders (the `.editorconfig` re-raise is currently dead under the
  src-wide NoWarn). Validation lane: Release build under `TreatWarningsAsErrors=true` + a guard proving
  CS1591 is enforced on the API-freeze surface.
#### Story 11.19: AppHost NuGet audit suppression

- **11.19b — AppHost NuGet audit suppression (`11-19-apphost-nuget-audit-suppression.md`, done).** Replace the blanket `NU1902-04` NoWarn with
  per-advisory `NuGetAuditSuppress` (CI-verifiable). Validation lane: CI audit lane / Governance test.
#### Story 11.19: Localization and identifier alignment

- **11.19c — Localization + identifier alignment (`11-19-localization-and-identifier-alignment.md`, done).** Localize the `FcHomeCard` aria-label and the UI
  host `lang="en"`/English strings; rename `HFC2106_ThemeHydrationEmpty` (ID string unchanged; obsolete
  alias if the constant is public). Validation lane: Shell localization/Governance lane +
  diagnostic-catalog parity.
#### Story 11.19: Analyzer-elevation decision

- **11.19d — Analyzer-elevation decision gate (`11-19-analyzer-elevation-decision.md`, done).** Architecture and Product approved staged
  adoption of `AnalysisMode=Recommended` with unchanged TWAE, built-in analyzers only, and narrow
  owner-bound exceptions. The decision is recorded in
  `_bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md` and materialized sequential,
  separately approval-gated Stories 11.20–11.23. This decision story does not activate policy.

As a release owner,
I want documented enforcement policies to match what the build and governance lanes actually enforce,
So that readiness claims are verifiable instead of aspirational.

**Acceptance Criteria:**

**Given** the inert CS1591 config for Contracts public folders (the `.editorconfig` re-raise is dead under the src-wide NoWarn),
**When** enforcement policy is aligned,
**Then** documented enforcement is put back in force; the AppHost blanket `NU1902-04` NoWarn is replaced with per-advisory `NuGetAuditSuppress` (CI-verifiable only); `FcHomeCard` aria-label + the UI host `lang="en"`/English strings are localized; `HFC2106_ThemeHydrationEmpty` is renamed (ID string unchanged; obsolete alias if the constant is public). *(M16, H12)*

**Given** the no-third-party-analyzer policy,
**When** Architect reviews elevating built-in analyzers (`AnalysisMode Recommended`),
**Then** a decision is recorded — it adds no packages, but the burn-down cost must be owned.

**Given** enforcement changes can create broad churn,
**When** implementation stories are created from this policy story,
**Then** each story names its validation lane and does not disable warnings or analyzer findings globally.
*(closes M16, H12 + policy-decision parts of the convention-drift cluster.)*

### Story 11.20: Recommended analyzer policy and exception ledger

**Status:** done. **Owner:** Architect + Framework Maintainer. **Due:** 2026-07-24.
**Approval gate:** separate Architecture/Product approval.

As an Architect and Framework Maintainer,
I want every current analyzer suppression and Naming diagnostic classified into a narrow exception or an actionable fix,
So that `AnalysisMode=Recommended` can be adopted without breaking public compatibility or hiding findings globally.

**Given** the Story 11.19d census,
**When** the policy audit runs,
**Then** all 2,958 Naming findings and every effective warning control are recorded in a versioned, owner-bound exception/fix ledger.

**Given** CA1707 conflicts with required underscore-separated test names and public diagnostic constants,
**When** dispositions are recorded,
**Then** compatibility is preserved with the narrowest supported mechanism and no repository/category-wide CA suppression.

**Given** a Naming finding lacks an approved exception,
**When** the candidate lane runs,
**Then** it is fixed or moved to a separately approved owner-bound defect story.

**Given** warning controls have different sources and owners,
**When** the audit completes,
**Then** each is classified as remain, narrow, move, or fix without absorbing Story 11.19a documentation or package-audit policy.

**Given** the built-in-analyzers-only policy,
**When** Governance runs,
**Then** no analyzer package or broad CA disable exists, TWAE remains unchanged, and the ledger matches effective configuration.

**Given** this phase changes policy boundaries rather than product behavior,
**When** validation completes,
**Then** normal Release, focused policy, and default lanes pass and baselines change only when explicitly approved.

### Story 11.21: Recommended analyzer product and generator burn-down

**Status:** done. **Depends on:** 11.20. **Owner:** Framework Maintainer + SourceTools Maintainer.
**Due:** 2026-08-14. **Approval gate:** separate Architecture/Product approval.

As a Framework and SourceTools Maintainer,
I want product-source and generator-emission findings fixed by defect class,
So that every shipped package and generated consumer can build cleanly under the approved `Recommended` policy.

**Given** Story 11.20's approved ledger,
**When** product projects build under Recommended with unchanged TWAE,
**Then** all 367 product findings are fixed or covered by pre-approved narrow compatibility exceptions.

**Given** 503 findings occur in SourceTools output,
**When** generator findings are remediated,
**Then** fixes occur in emitters or annotated source, never `obj/`, and generated consumers prove the correction.

**Given** CA1848/CA1873 dominate logging findings,
**When** logging work runs,
**Then** it follows the source-generated `LoggerMessage` convention without reopening Story 11.18 ownership.

**Given** remaining product findings span several CA categories,
**When** grouped,
**Then** every change has a named diagnostic/package scope and preserves public API, schema, wire, lifecycle, MCP, and artifact contracts.

**Given** netstandard2.0 compiler-host compatibility is load-bearing,
**When** kernel/analyzer projects are validated,
**Then** the Contracts/Schema/SourceTools TFM boundaries and netstandard gate remain intact.

**Given** the burn-down is complete,
**When** validation runs,
**Then** owned product/generated consumers have zero actionable findings and all required Release, focused, default, Governance, Contract, and baseline gates pass.

### Story 11.22: Recommended analyzer test and sample burn-down

**Status:** done. **Depends on:** 11.21. **Owner:** Test Architect + Framework Maintainer.
**Due:** 2026-09-04. **Approval gate:** separate Architecture/Product approval.

As a Test Architect and Framework Maintainer,
I want test and sample analyzer debt burned down without weakening intentional fixture semantics,
So that the complete repository can approach `Recommended` activation with trustworthy verification.

**Given** the original 3,500 test and 203 sample findings,
**When** this phase starts after 11.20-11.21,
**Then** every remaining diagnostic is assigned by project, ID, and approved disposition.

**Given** underscore-separated test names are required,
**When** CA1707 is handled,
**Then** the approved narrow 11.20 mechanism is used without mass rename or global suppression.

**Given** tests intentionally contain invalid code and specialized fixtures,
**When** a suppression remains necessary,
**Then** it is minimal and ledgered with rationale, owner, review date, and revalidation trigger.

**Given** generated Shell specimens contributed findings,
**When** Story 11.21 emitter fixes are consumed,
**Then** test projects validate the output without editing `obj/` or duplicating fixes.

**Given** samples are adopter guidance,
**When** sample findings are fixed,
**Then** samples still teach supported APIs and security/package boundaries without hiding genuine warnings.

**Given** the phase is complete,
**When** validation runs,
**Then** test/sample projects have zero actionable findings and default, Governance, Contract, snapshot, compatibility, and Release gates pass without unapproved drift.

### Story 11.23: Recommended analyzer repository activation

**Status:** done. **Depends on:** 11.22. **Owner:** Architect + Framework Maintainer + Release Owner.
**Due:** 2026-09-11. **Approval gate:** separate Architecture/Product approval. **Release gate:** v1.0.

As an Architect, Framework Maintainer, and Release Owner,
I want the approved `AnalysisMode=Recommended` posture activated and governed repository-wide,
So that analyzer strictness becomes a durable v1.0 build invariant.

**Given** Stories 11.20-11.22 are done with zero actionable findings,
**When** activation begins,
**Then** `AnalysisMode=Recommended` is declared centrally without new analyzer packages, weaker TWAE, or broad CA suppression.

**Given** netstandard2.0 compiler-host compatibility is explicit,
**When** the property is evaluated across Contracts, Schema, and SourceTools,
**Then** their TFM/analyzer boundaries remain preserved and documented.

**Given** the benchmark project has a warning-policy exception,
**When** the repository gate is finalized,
**Then** it is reconciled and the forced Release solution build reports zero warnings and zero errors.

**Given** analyzer policy can regress,
**When** Governance runs,
**Then** it proves the central setting, built-in-only rule, unchanged TWAE, no broad suppression, ledger/config parity, and candidate/current build parity.

**Given** activation can affect emitted/public surfaces,
**When** validation runs,
**Then** all required default, Governance, Contract, package/PublicAPI, schema, generated-output, Verify, Pact, docs, and artifact lanes pass without unapproved drift.

**Given** this is a v1.0 release gate,
**When** the story reaches review,
**Then** Release Owner evidence is linked and rollback requires a separately approved policy change that cannot weaken TWAE or hide diagnostics globally.

### Story 11.24: Adopt the Owner-Approved EventStore Runtime Identity

**Historical status:** done 2026-08-29. **Owner:** FrontComposer Maintainer + EventStore Maintainer.
**Supersession:** this story and its v1 identity/evidence remain immutable delivery history. Its
historical tuple is not the active release target. Story 11.25 owns the current
`059f6a89… / 3.103.0 / a32cb422…` release identity, exact-tuple evidence, and named approval.
The historical acceptance text below is retained for audit and must not be used to select the current
runtime.

As a FrontComposer maintainer,
I want source and package modes aligned to the owner-approved EventStore runtime identity,
So that FrontComposer validates and releases against one auditable backend contract without mixed or
unapproved dependency identities.

**Given** EventStore Story 1.20 remains blocked, non-authorizing, incomplete, or lacks any required
source/package/approval identity,
**When** FrontComposer backlog selection runs,
**Then** this story remains `backlog`, no EventStore or Builds gitlink is changed, and current command,
query, projection, realtime, rollback, and topology behavior remains intact.

**Given** Story 1.20 authorizes consumer migration and names the approved EventStore source SHA,
**When** Debug/source mode is adopted,
**Then** `references/Hexalith.EventStore` gitlink and checkout both equal that SHA, the EventStore
submodule is not edited, and only FrontComposer-root-declared submodules are initialized.

**Given** Story 1.20 names the approved 14-package version and hashes,
**When** Release/package mode restores from an isolated cache,
**Then** `Hexalith.EventStore.Aspire` and every resolved `Hexalith.EventStore*` asset use that exact
version, the fetched package bytes match the approved hashes, no EventStore project reference enters
the Release asset graph, and the selected `Hexalith.Builds` gitlink already exposes that version.

**Given** FrontComposer's committed EventStore consumer pacts,
**When** provider verification runs against the exact approved EventStore SHA over real loopback TCP,
**Then** every interaction passes with deterministic provider-state setup/teardown and a bounded,
redaction-clean report, and `eng/validate-contract-artifacts.ps1 -RequireProviderVerification` passes
using the real EventStore provider-test project rather than the obsolete solution handoff.

**Given** source and package identities are aligned,
**When** adoption validation runs,
**Then** Debug/source AppHost build, Release/package build, Governance, the default solution lane with
`DiffEngine_Disabled=true`, and an Aspire smoke covering EventStore health, command submit/status,
query/provenance, and projection SignalR all pass.

**Given** this story only adopts an approved runtime identity,
**When** compatibility evidence is reviewed,
**Then** it does not remove or redesign FrontComposer adapters, rollback paths, topology, or deploy an
EventStore container; any behavioral migration is routed to a separately approved compatibility story.

### Story 11.25: Current EventStore Release Identity and Evidence

**Status:** done. **Owner:** Architect + EventStore Maintainer + FrontComposer Maintainer + Release
Owner. **Retrospective action:** E11R-AI-1.

**Completion boundary (reconciled 2026-09-22):** this story delivered the v2 technical capture and
remains done. It did not obtain migration receipts, set migration approval true, or close G-3. Its
tuple is historical and must not be projected onto the current EventStore/Builds gitlinks. E11R-AI-1
therefore points to EVT-ID-1 (Story 16.1) for a fresh exact-tuple packet and EVT-APP-1 (Story 16.3) for separate owner
approval; this story is not reopened or reused.

As a Release Owner and framework maintainer,
I want one approved identity record and live proof for the EventStore runtime selected by the current
repository,
So that release compatibility is auditable without rewriting historical authorization.

**Given** Story 11.24 identity v1 and its evidence,
**When** the current identity is recorded,
**Then** v1 remains byte-for-byte historical, and a successor record identifies v1 as superseded only
for active release selection.

**Given** the current repository selects EventStore source `059f6a8917bfab26b85775be464840a1610dfdeb`,
EventStore package `3.103.0`, and Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`,
**When** the successor record is validated,
**Then** it contains exactly that active tuple, a dated decision selecting recapture rather than a
semantic exception, and no changed gitlink or package version is attributed to the approval.

**Given** the committed FrontComposer consumer pacts and current AppHost,
**When** exact-tuple evidence is captured,
**Then** all 19 provider interactions pass over real loopback, all ten AppHost resources become healthy,
the authenticated health/command/status/query/provenance/SignalR observations pass, shutdown is clean,
and the bounded redaction-clean reports bind the exact source, package, Builds, candidate, and artifact
hashes.

**Given** the successor evidence is complete,
**When** migration approval is claimed,
**Then** the EventStore maintainer, FrontComposer maintainer, and Release Owner are named and dated; if
there is no distinct EventStore maintainer, OI-18 is approved first and the transferred role is explicit.

**Given** Governance evaluates runtime identity,
**When** the current Builds gitlink changes in a future candidate,
**Then** the gate compares the candidate with its active identity/evidence record and fails closed on
unreconciled provenance rather than embedding an unexplained historical SHA.

### Story 11.26: Analyzer Identifier Inventory Reconciliation

**Status:** done. **Owner:** Analyzer Policy Owner. **Retrospective action:** E11R-AI-2.

As the analyzer policy owner,
I want the two-identifier CA1707 scope delta reviewed and intentionally resolved,
So that the inventory seal detects accidental drift without concealing legitimate declarations.

**Given** the sealed count/hash and the current `3327` / `e33cb6e8…` inventory,
**When** the delta is reviewed,
**Then** the two added declarations, owning changes, and in-scope rationale are named in evidence.

**Given** the declarations are intended,
**When** the inventory is resealed,
**Then** the exact generated count/hash matches the reviewed source; if either declaration is
unintended, the source is corrected instead of blessing the drift.

**Given** the correction,
**When** focused and default analyzer lanes run,
**Then** `AnalysisMode=Recommended`, `TreatWarningsAsErrors=true`, built-in-analyzer scope, and narrow
ledger exceptions remain unchanged, with no broad suppression.

### Story 11.27: Generated Command Route Acceptance Locator

**Status:** done. **Owner:** QA Engineer. **Retrospective action:** E11R-AI-3.

As a QA engineer,
I want route acceptance to target the route-level heading unambiguously,
So that the test proves command activation without colliding with shell chrome.

**Given** command-palette activation of `ConfigureCounterCommand`,
**When** the route test asserts navigation,
**Then** the URL remains exactly `/commands/Counter/ConfigureCounterCommand` and the command form is
visible.

**Given** both shell chrome and page content contain `Counter`,
**When** heading visibility or focus is asserted,
**Then** the locator is scoped to the route content or exact heading level/identity and resolves to one
element without weakening the route assertion.

**Given** the AppHost-backed e2e lane,
**When** the focused Story 11.7 test runs,
**Then** it passes without `strict mode violation` and retains tenant setup and palette activation.

### Story 11.28: FC-NIP Semantic Fixture Alignment

**Status:** done. **Owner:** Technical Writer + SourceTools Maintainer. **Retrospective action:**
E11R-AI-4.

As the SourceTools maintainer,
I want the semantic documentation fixture to describe current FC-NIP delivery truth,
So that documentation drift is caught without requiring obsolete PRD prose.

**Given** the canonical PRD records D-4 complete, Stories 9.3–9.8 done, and the 9.8 live proof passed,
**When** the FC-NIP manifest is updated,
**Then** its positive and negative fragments assert those current outcomes and retain the explicit
command-target identity, typed materiality, and server-allocated-key non-goal.

**Given** older text such as `Resolved 2026-08-12` and `Stories 9.4-9.8 still block`,
**When** the correction is reviewed,
**Then** the obsolete fixture requirements are removed and the canonical PRD is not changed back to
match them.

**Given** the updated fixture,
**When** the SourceTools documentation and browserless FC-NIP guards run,
**Then** they pass against the same language-neutral manifest.

### Story 11.29: Fallback Refresh and View Registration Correctness

**Status:** done. **Owner:** Shell Maintainer. **Retrospective action:** E11R-AI-5.

As a shell maintainer,
I want fallback refresh and view registration to detect material scope/data changes,
So that equal counts, reused validators, or reused view keys cannot leave stale operator state.

**Given** a no-ETag fallback response with the same row count but changed row values,
**When** change detection runs,
**Then** its deterministic signature includes bounded canonical row content and dispatches the changed
state.

**Given** an equal ETag while a reducer page required by visible state is missing,
**When** fallback reconciliation runs,
**Then** it dispatches or rebuilds the required page instead of treating the missing reducer state as
unchanged.

**Given** an existing ViewKey is registered again with a different tenant or query contract,
**When** registration is attempted,
**Then** the runtime rejects the conflict or atomically replaces it only after prior ownership is
disposed; it never silently retains the old scope.

**Given** deterministic regression fixtures for all three cases,
**When** focused Shell and applicable default lanes run,
**Then** changed and unchanged cases are distinguished without cross-tenant leakage or duplicate work.

### Story 11.30: Testing and MCP Boundary Hardening

**Status:** done. **Owner:** Testing + MCP Maintainers. **Retrospective action:** E11R-AI-6.

As an adopter and MCP host maintainer,
I want deterministic identifiers and evidence handling to be canonical, bounded, and fail safe,
So that test evidence is reproducible and malformed input or formatting failures cannot escape the
boundary.

**Given** deterministic Testing command dispatch,
**When** message and correlation identifiers are allocated,
**Then** they are canonical 26-character ULIDs, stable for the configured deterministic sequence, and
distinct where the contract requires distinct identities.

**Given** an MCP lifecycle identifier,
**When** it is parsed,
**Then** canonical ULIDs through `7ZZZZZZZZZZZZZZZZZZZZZZZZZ` are accepted and overflow encodings
starting with `8` through `Z`, noncanonical text, and invalid lengths are rejected before side effects.

**Given** evidence serialization throws for an arbitrary command payload,
**When** Testing records dispatch evidence,
**Then** dispatch behavior is not failed by evidence formatting and a bounded redacted sentinel is
recorded instead.

**Given** objects or dictionaries contain credential-bearing keys including `Authorization`, `ApiKey`,
`Cookie`, `PrivateKey`, or `ConnectionString` in any casing,
**When** evidence is formatted,
**Then** their property/key names and values are redacted before truncation, while benign assertion
values remain useful.

### Story 11.31: Canonical Correlation Pseudonymization

**Status:** done. **Owner:** Shell Observability Owner. **Retrospective action:** E11R-AI-7.

As an observability owner,
I want one correlation pseudonymization contract across diagnostic, lifecycle, readiness, and hot-path
logs,
So that related events are joinable without emitting raw identifiers.

**Given** DW-1769 and DW-1770,
**When** the logging helpers are consolidated,
**Then** one shared implementation emits the approved token shape `sha256:` plus 16 lowercase hex
characters for non-empty correlation identifiers, and duplicate private digest implementations are
removed.

**Given** the same normalized identifier reaches diagnostic, lifecycle, readiness, and hot-path logs,
**When** events are emitted,
**Then** every family records the same token; different fixture identifiers produce different tokens;
no raw correlation identifier is present.

**Given** null, empty, whitespace, oversized, or Unicode input,
**When** pseudonymization runs,
**Then** normalization and bounded behavior are explicit, deterministic, allocation-conscious on hot
paths, and covered by tests.

**Given** the implementation and focused evidence pass,
**When** deferred work is reconciled,
**Then** DW-1769 and DW-1770 close with links to the canonical contract and tests.

### Story 11.32: Epic 11 Artifact Integrity Enforcement

**Status:** done. **Owner:** QA Automation Maintainer. **Retrospective action:** E11R-AI-8.

As a QA automation maintainer,
I want story and sprint artifacts validated against repository truth,
So that a done status cannot conceal nonexistent revisions, incomplete tasks, or contradictory queue
state.

**Given** Stories 11.17, 11.18, and 11.19 are nonimplementable parents,
**When** sprint state is reconciled,
**Then** development status is carried by 11.17a–d, 11.18a–c, and 11.19a–d, while parent summaries do
not masquerade as implementable queue entries.

**Given** `final_revision` values for Stories 11.7, 11.9, and 11.12,
**When** each value is checked,
**Then** it resolves to an existing repository commit that supports the story or is removed/corrected
with an evidence-backed explanation.

**Given** unchecked required tasks in Story 11.6 or 11.17c and the stale Story 11.15 shadow-spec state,
**When** artifacts are reconciled,
**Then** task completion and status agree with evidence without marking unperformed work complete.

**Given** a done story with a nonexistent final revision, unchecked required task, conflicting shadow
status, missing materialized child, or parent/child queue mismatch,
**When** story validation runs,
**Then** it fails closed with the exact story and conflict.

**Given** all eight remediation stories,
**When** sprint state is finalized,
**Then** every E11R action has the correct `implementation_story`, evidence link, and status; no manual
bypass is needed for validation.

### Epic 11 Remediation Sequence and Acceptance

The approved historical order was 11.25; then 11.26–11.28; the acceptance checkpoint; 11.29–11.31;
and 11.32. All eight stories are now done and the bounded Epic 11 delivery is closed. This preserves
the rejected 2026-09-10 retrospective and every later story record without treating later repository
movement as unfinished Epic 11 work.

Two residuals remain explicit outside the epic:

- E11R-AI-1/G-3 is open under EVT-ID-1 (Story 16.1) and EVT-APP-1 (Story 16.3) because Story 11.25 did not grant migration
  approval and the repository moved beyond its exact tuple.
- The current Story 11.32 validator failure is proposed PLAN-INT-2. It repairs current artifact
  integrity without retroactively changing the completed status of Story 11.32 or Epic 11.

## Cross-Cutting Governance Work

### GOV-1: Validate Shared-Catalog Compatibility and Seal Dependency Provenance

**Status:** in-progress; the finalized 2026-09-09 spine is authoritative, AD-1 through AD-19 are stable,
and production release eligibility remains blocked. **Owners:** Product Owner + Architect + Developer +
Release Owner. **Priority:** before the next governed production release. Story 11.17d remains closed
under its recorded one-story waiver and is not reopened by GOV-1.
**Decision:** `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`.
**Architecture:** `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`.
**Course correction:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md`.

The approved 2026-09-22 correction decomposes this parent obligation without marking it complete:

| Alias | Class | Independently completable boundary |
| --- | --- | --- |
| EXT-BUILDS-1 | External dependency | Hexalith.Builds owner accepts an immutable split-reusable revision before FrontComposer activation. |
| GOV-B | Implementable | Caller pins the accepted revision and implements the exact two-job topology with delayed activation and rollback proof. |
| GOV-C | Implementable | Candidate/build execution has no production environment, publication secret, write scope, OIDC/attestation authority, signing material, or equivalent ambient capability. |
| GOV-D | Implementable | One authenticated run-bound publication candidate passes replay, mutation, and hostile-candidate fixtures. |
| GOV-E | Implementable | Handoff v3 and a typed append-only attempt ledger reject malformed, duplicate, retroactive, or invalid retry transitions. |
| GOV-F | Implementable | Candidate-free pinned code performs manifest-v4 classification/publication without checking out or executing candidate source. |
| GOV-G | Implementable | A pinned post-release evaluator and durable incident/recovery evidence work without ambient or candidate helpers. |
| GOV-H | Implementable | Duplicate or ambiguous destination asset names fail before publication with redacted diagnostics. |
| GOV-I | Implementable evidence | The incident runbook and dated tabletop prove acknowledgement, containment, preservation, recovery, and re-enable handling. |
| GOV-J | Approval/evidence | Deterministic checks and five review lenses pass against one unchanged authenticated candidate with every finding dispositioned. |
| GOV-SRC-1 | Implementable documentation | Every governance source distinguishes owner revision, execution pin, current gitlink, and evidence provenance. |
| GOV-ACCEPT-1 | Approval | Product, Architecture, and Release accept D-16, the halt, emergency stop, and no-exception posture. |
| GOV-ACCEPT-2 | Approval | Product and Release accept exact reconciled source digests without weakening publication invariants. |

GOV-B through GOV-H are the seven FrontComposer-controlled implementation slices paired with
EXT-BUILDS-1 as the eighth closure bundle. GOV-I/J and the two acceptances are additional gate work.
No implementation slice may absorb the external dependency or claim an owner approval.

As a framework maintainer and Release Owner,
I want compatibility validated from the catalogs selected by actual gitlinks while exact identities
are sealed as provenance,
So that legitimate pointer advances remain reviewable and reproducible without false-red SHA pins.

**Given** the FrontComposer root and its root-declared modules,
**When** Governance collects every root gitlink at the explicit root commit and every direct gitlink at
each exact root-selected commit,
**Then** it records the complete defined depth-1/2 v1 graph, evaluates every Builds selector under its
explicit semantic profile while caching exact catalog bytes by distinct commit, and contains no expected
historical Builds-commit or catalog-fingerprint allowlist. Deeper historical edges require a separately
approved schema; immutable workflow/action provenance intentionally uses approved full 40-hex pins.

**Given** a compatible catalog at a different commit,
**When** focused Governance runs,
**Then** compatibility passes and the changed identity appears only in dependency-graph diff/evidence.

**Given** a selected catalog that is missing or changes a required package/import/marker contract,
**When** Governance runs,
**Then** it fails with the owning gitlink path, actual commit, and semantic mismatch.

**Given** a root or nested gitlink change,
**When** PR CI requires the event base to equal the computed merge-base and compares it with the exact
`github.sha` merge revision, or push CI compares a non-zero `github.event.before` with `github.sha`,
**Then** it emits the normalized graph diff and runs each affected module once through its closed static
Release/NuGet build or evidence-only disposition, with bounded exact Builds contract-tree materialization
and no recursive submodule initialization. Depth-1 changes are classified first and subsume descendant
depth-2 churn; remaining depth-2 changes map to a surviving candidate owner or FrontComposer root.
Zero/unavailable push bases take the full-affected path, fail the gate, and are non-release-eligible.

**Given** a candidate changes the dependency policy,
**When** the candidate graph is evaluated,
**Then** both base and candidate use the immutable base/before policy; the change can activate only from
a later base revision. The one-time v1 bootstrap was consumed on 2026-07-19 and no bootstrap path remains.
CI, Release, post-release, and incident-recovery evaluator sources each project one policy-authorized
closure; sealed but unapproved literal revisions fail closed.

**Given** the authenticated candidate is ready for release preparation,
**When** the selected `split-publication-v1` reusable runs,
**Then** candidate-controlled code executes only in the secretless/read-only
`build-publication-candidate` job and crosses into the protected stage only as the authenticated
`hexalith.publication-candidate.v1` archive. The candidate-free
`publish-publication-candidate` job independently authenticates every byte, obtains and verifies
attestation or the run-bound approved fallback, prepares and seals manifest v4, performs final
offline/live classification, and alone may publish. Builder output can deny early but cannot authorize.

**Given** an exact candidate has successful push CI and quality runs,
**When** release authorization begins,
**Then** operator `workflow_dispatch` authenticates the exact live `main` SHA against exactly one
completed successful push run of both `ci.yml` and `quality.yml`, verifies the run-bound CI handoff, and
uses its candidate as the sole authority. No tag, ambient checkout, second-hop SHA, later default branch,
or diagnostic source proof can replace it.

**Given** a Release attempt completes, fails, or partially publishes,
**When** the post-release verifier runs through the second `workflow_run` hop,
**Then** `hexalith.release-verification-handoff.v3` preserves the selected quality run, original CI
handoff/candidate, policy, publication-candidate coordinate, release state, final manifest v4,
attestation/fallback, assets, denial reason, and Release evaluator. The post-release verifier
independently authenticates both artifacts and its own pinned closure, maps the attempt to exactly one
closed disposition, appends durable evidence, and cannot substitute identity, green-no-op, or erase an
incident. An incomplete product Release after publication starts invokes the separately authorized
immutable incident-evidence preservation path before retry.

**Given** the owner-accepted Hexalith.Builds revision `a8a50859…` is only the AD-16 lineage predecessor,
**When** GOV-1 delivery is assessed,
**Then** the Hexalith.Builds owner and Release Owner must accept a later immutable split-reusable
revision into that lineage before FrontComposer activates it. GOV-1 remains in-progress until the eight
implementation closure bundles, incident runbook, owner decisions, and the exact spine-defined split
implementation gate pass. FrontComposer does not edit the Builds submodule or infer a contingency.

**Given** production remains halted,
**When** owner readiness is reviewed,
**Then** Product Owner and Release Owner acceptance of AD-19 and the halt/no-exception posture, Release
Owner verification of `HEXALITH_RELEASE_PUBLISH_ENABLED=false`, the approved incident response target
and runbook, and the no-bypass protected-main process remain explicit open decisions/actions. The
deny-only variable can block but never authorize, and no current exception exists.

**Given** Hexalith.Builds has no semantic catalog contract version,
**When** GOV-1 is handed off,
**Then** BUILD-CAT-1 is routed upstream; FrontComposer validates semantic contents during migration and
does not use an exact catalog fingerprint allowlist as a replacement compatibility test.


## Epic 12: Adopters Launch an Operations-Ready Domain Shell

Adopter developers can turn annotated domain types into a coherent, accessible FrontComposer shell and prove a named domain-module adoption without bespoke framework plumbing.

### Story 12.1: [I · ADOPT-KIT-1] Publish a Deterministic Three-Call Adopter Proof Kit

As an adopter developer,
I want a versioned, candidate-bound bootstrap proof kit,
So that I can demonstrate a generated projection and command without repository-private knowledge or bespoke framework plumbing.

**Acceptance Criteria:**

**Given** a clean consumer fixture and an exact FrontComposer candidate identity
**When** the documented AddHexalithFrontComposerQuickstart(), AddHexalithDomain<TMarker>(), and AddHexalithEventStore(...) sequence is applied
**Then** the consumer starts through the supported bootstrap path and renders at least one generated projection and one generated command
**And** the procedure requires no FrontComposer-repository-private paths, unpublished assumptions, or hand-authored replacement UI.

**Given** a required bootstrap stage is absent or misordered
**When** the consumer starts
**Then** startup fails before first render with the named missing or misordered stage
**And** an empty domain registry remains a valid shell state.

**Given** proof is captured for an external adopter
**When** the kit records its result
**Then** it binds the candidate SHA, relevant package/runtime identity, generated projection assertion, generated command assertion, execution date, and pass/fail result
**And** it redacts tenant/user data, tokens, payloads, stack traces, and machine-specific paths.

**Given** existing focused generator, bootstrap, Shell, and authentication regression tests already prove delivered FR1–FR4, FR7–FR10, and FR29.1 behavior
**When** the kit is validated
**Then** those existing results are referenced where applicable rather than duplicated into a new evidence framework
**And** no completed baseline behavior is reimplemented solely for this story.

**Given** FrontComposer and Fluent UI V5 already provide the required shell components
**When** the consumer fixture renders the proof surface
**Then** it uses the current pinned components and theme roles
**And** it introduces no raw replacement controls, custom theme, legacy Fluent tokens, or new responsive breakpoints.

### Story 12.2: [A · ADOPT-APP-1 · Conditional] Select a Substitute Adopter

As the Product Owner,
I want to record a dated decision when the obligated adopter cannot supply bootstrap proof,
So that any substitute adopter inherits the same evidence obligation without silently weakening the readiness milestone.

**Acceptance Criteria:**

**Given** Hexalith.Tenants cannot produce EXT-ADOPTER-1 proof and the D-7 fallback is invoked
**When** the Product Owner evaluates the fallback
**Then** the decision explicitly selects Hexalith.Parties or holds the readiness milestone
**And** it records the rationale, date, accountable adopter maintainer, and unchanged proof obligation.

**Given** Hexalith.Parties is selected
**When** EXT-ADOPTER-1 is transferred to the Parties maintainer
**Then** the dependency still requires dated, candidate-bound three-call bootstrap, generated-projection, and generated-command proof
**And** the selection itself does not satisfy G-6 or claim that the external proof exists.

**Given** no dated substitute decision exists
**When** readiness is evaluated
**Then** Hexalith.Tenants remains the obligated adopter
**And** G-6 remains open without an inferred fallback.

**Given** the decision cites the approved adopter kit and existing milestone sources
**When** its durable record is created
**Then** it references those artifacts rather than copying their evidence
**And** no additional evidence schema, test lane, or wrapper report is introduced.

**External dependency — [X · EXT-ADOPTER-1]:** After Story 12.1—and after Story 12.2 only when Parties is selected—the named external adopter maintainer executes the kit and publishes dated, candidate-bound proof. FrontComposer records the dependency state but cannot complete it on the adopter's behalf. **Accepted 2026-09-26:** Tenants supplied the independent evidence at `references/Hexalith.Tenants/_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.md` and `references/Hexalith.Tenants/_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.json`; the explicit Product decision is `_bmad-output/implementation-artifacts/tests/tenants-g6-product-decision-2026-09-26.md`. D-7 was not invoked, so Story 12.2 remains conditional.

## Epic 13: Operators Trust Tenant-Scoped Data and Command Outcomes

Operators can browse projections, execute commands, recover from transport failures, and understand lifecycle and fresh-row state without stale-tenant leakage or false success.

### Story 13.1: [I · TEN-SCOPE-1] Prove Tenant-Safe Operator State End to End

As an authenticated operator,
I want every projection, subscription, count, preference, pending command, and fresh-row state scoped to my resolved tenant and user,
So that I never observe another tenant's data or mistake missing context for an empty result.

**Acceptance Criteria:**

**Given** two tenants have distinguishable projection data and counts
**When** each tenant uses the production query, subscription, count, and storage adapters
**Then** each receives only its own scoped state
**And** the focused integration proof exercises the real EventStore seams rather than unit-only substitutes.

**Given** tenant identity is absent, invalid, mismatched, or becomes stale under a live surface
**When** a query, subscription, count, preference, pending-state, or fresh-row operation would run
**Then** the operation fails closed before prior-scope data can render
**And** the operator sees an explicit blocking state rather than an empty-looking result.

**Given** a SignalR projection group or persisted preference belongs to a prior scope
**When** tenant or user context changes
**Then** the old group is blocked and old state is cleared before the next scope renders
**And** storage uses the tenant/user/feature key or is skipped with the governed diagnostic when scope is unavailable.

**Given** focused tenant, storage-key, return-path, and auth-state tests already exist
**When** this story is verified
**Then** they are reused with the minimum additional production-seam scenario needed for FR30
**And** output contains no tenant payload, token, JWT, stack trace, or unrestricted PII.

### Story 13.2: [I · UX-A] Make Shell Navigation and Route Focus Deterministic

As a keyboard or assistive-technology operator,
I want shell, account, Module navigation, search, and route focus to behave predictably,
So that I can move through the application without losing context.

**Canonical rows:** Implements/evidences FM-01, FM-02, FM-03, FM-04, FM-05 (palette/settings), FM-10 (authentication-redirect return only), FM-12; OF-01, OF-02; AM-23, AM-25, AM-28, AM-29, AM-31; SS-01, SS-02, SS-03, SS-04, SS-27, SS-28, SS-29, SS-30, SS-31, SS-32, SS-33, SS-40, SS-41, SS-42, SS-43, SS-46 and SS-47 (palette/settings). Preserves the delivered shell frame, providers, registry navigation, routes, tabs, palette, settings Live-update/Restore-defaults/Done, toolbar keyboard contract, breakpoint watcher, framework sign-out route, and single-accordion page-section baseline. Authority: ux-design.md canonical rows (ux-design.md:394-395); DESIGN.md/EXPERIENCE.md supplement only and lose on conflict. AM-01/AM-02/AM-03/AM-26 channel and coalescing rules are owned by Story 13.4.

**Acceptance Criteria:**

**Given** a successful client-side navigation, a direct deep link, an authorized CTA, or a palette activation (FM-01)
**When** the destination view is ready
**Then** focus moves programmatically to its unique route-level h1, and the labelled tabpanel, active Module item, and canonical route agree
**And** the heading's bounding box is entirely outside sticky chrome and overlays.

**Given** navigation from shell navigation, a Module tab, the palette, or a CTA cannot activate a safe destination (FM-01 failure, SS-29)
**When** the failure is surfaced
**Then** focus stays on the invoker or the current route heading, and AM-23 "Could not open {safe destination label}. You remain on {current page label}." is announced once per activation attempt through the polite status channel
**And** no internal route or error detail is exposed and no hidden, removed, or unrelated tab receives focus.

**Given** a keyboard operator selects a Module tab (FM-02, SS-31)
**When** the selection changes
**Then** the selected tab keeps focus, its labelled tabpanel changes, and the change is intentionally silent
**And** the active tab and its full focus ring stay visible inside the tab-strip scrollport.

**Given** a route names a Module that exists but a tab that is absent or unavailable (FM-02, SS-32)
**When** the route resolves
**Then** the default Module tab is shown and exposed as selected, the invalid target is never selected, and AM-31 "That page is unavailable. Showing {default tab label}." is announced once per activation attempt, requested safe route, and resolved default through the polite status channel
**And** canonicalizing the same valid default route is silent, and a disabled tab (SS-33) stays visibly and programmatically disabled and silent until an external route attempt, which uses AM-31 or AM-26.

**Given** `/` is pressed on an active route outside editable controls, IME composition, and component-owned chord handling (FM-12)
**When** the route exposes exactly one enabled `FcPageToolbar` page-search input
**Then** that input receives focus with its value unchanged, fully visible outside sticky chrome and expanded toolbar content
**And** when the input is absent, disabled, or ambiguous, or the shortcut is disabled, the key does nothing and focus stays where it was; the current first-DataGrid-column-filter targeting is removed.

**Given** the command palette opens from `Ctrl+K` or its visible button, from shell navigation or from page content (OF-01, FM-03, SS-40)
**When** it opens and later closes
**Then** the shell has captured the connected, enabled `document.activeElement` (or the visible pointer invoker) as a direct origin handle with its stable evidence locator before activation, and initial focus goes to the query input, or else the first enabled result, or else the close control
**And** query, results, and close follow the inherited combobox/listbox model, Escape closes without navigation, and close returns focus to the captured origin (FM-04); if that origin was removed, disabled, or disconnected, focus goes to the current route h1 and never to `body`; successful palette navigation uses FM-01 and failure uses AM-23.

**Given** a debounced 150ms authorized palette search yields zero results, or a previously visible result becomes unauthorized or fails to open (SS-41, SS-42)
**When** the result settles or the activation fails
**Then** AM-28 "No commands or pages match." is emitted once through the single palette-owned `role="status" aria-live="polite" aria-atomic="true"` node, deduped by palette session, normalized query, and zero-result identity, coalesced by a trailing 250ms window per palette session with stale query results discarded, and any inherited result-count speech is omitted
**And** a denied result does not open and uses AM-26, a navigation failure uses AM-23, and the query stays editable while the current route remains usable.

**Given** the settings dialog opens from `Ctrl+,` or its visible action (OF-02, FM-05, SS-43)
**When** the operator changes settings and then closes the dialog with Done, Escape, or close
**Then** the origin was captured as in OF-01, initial focus goes to the dialog heading (`tabindex="-1"`) and then the first setting in tab order, Tab/Shift+Tab cycle within the modal, and background content is not focusable
**And** changes apply live and are not rolled back on close, the session makes no announcement, any exposed setting error focuses a complete local summary as in AM-18, preferences persist only under a resolved tenant/user scope, and close returns focus to the captured origin, falling back to the current route h1 when that origin was removed, disabled, or disconnected (SS-46, SS-47).

**Given** application bootstrap and the Home directory (SS-27, SS-28, SS-01, SS-02, SS-03, SS-04)
**When** the shell starts and Home resolves
**Then** Bootstrap (SS-27) does not imply that data is ready and resolves to no registrations, Home Loading, or startup failure; no registrations (SS-01) shows and announces AM-25 "No modules are available." once per bootstrap identity and settled state; and startup failure (SS-28) focuses the AM-29 heading "FrontComposer could not start. Review the configuration." with no live attributes and no exception detail
**And** Home Loading (SS-02) shows a skeleton that matches the directory layout, Empty (SS-03) shows no accessible Modules without an error, and Data (SS-04) orders Module cards by readiness, then descending actionable count, then ordinal Module name.

**Given** an adopter customizes the header, replaces `FrontComposerShell.HeaderStart`, sets `ShowAccountMenu` to false, or omits conditional navigation in the current public baseline
**When** the shell renders in Desktop, Compact, or Narrow-browser mode
**Then** the framework-owned account control and the hamburger remain rendered and reachable, and account sign-out uses the framework route and evicts token state without exposing token details (SS-01 permitted actions; ux-design.md:64-71 and :134-142; FR-8 ledger row)
**And** removing or neutralizing the `ShowAccountMenu`/`HeaderStart`/conditional-navigation opt-outs updates the intentional public API baseline, and closing any transient header UI returns focus to its invoker.

**Given** Desktop, Compact, and Narrow-browser shell modes
**When** primary navigation is presented
**Then** each bounded context has exactly one Module entry, projection flyouts remain secondary navigation into `/{module}/{tab}`, and exactly one item is current by longest segment-prefix match (ux-design.md:32-42 and :64-71)
**And** the implementation uses the shared breakpoint watcher without inventing product-contract widths or raw replacement controls.

**Given** authentication redirects away from a replaced surface (FM-10)
**When** the platform login completes and returns to the application
**Then** the platform login owns focus during the redirect, and on return focus goes to the destination route h1 as in FM-01
**And** the replacement-state heading and AM-26 behavior before the redirect stay owned by Story 13.4.

**Given** the delivered settings, page frame, Module tabs, toolbar, and section patterns
**When** their focused shell regression scenarios run
**Then** tabs and the toolbar keep their keyboard contracts, and two or more sibling titled sections use one Fluent accordion with the primary item expanded, while a single primary content region stays directly visible (ux-design.md:128-132)
**And** the scenarios reuse the existing focused tests and add only the missing FM/OF/AM/SS row assertions.

**Given** this story's implementation and evidence are complete
**When** readiness status is evaluated
**Then** OI-16, G-4, FLUENT-APP-1, and Product approval stay open and are not closed or inferred by this story
**And** each closes only through its own evidence record and owner decision (ux-design.md:356-359).

### Story 13.3: [I · UX-B] Preserve Focus and Input Through Command Safety Outcomes

As an operator submitting a generated command,
I want validation, rejection, confirmation, abandonment, and blocked-submit behavior to preserve useful context,
So that I can correct mistakes safely without duplicate execution.

**Canonical rows:** Implements/evidences VR-01, VR-02, VR-03, VR-04, VR-05, VR-06; FM-05 (destructive confirmation), FM-06, FM-07, FM-08, FM-09, FM-11; OF-03, OF-04; AM-18, AM-19, AM-20, and AM-14 as consumed by VR-03; SS-20, SS-25, SS-36, SS-37, SS-38, SS-39, SS-44, SS-45, SS-46 and SS-47 (destructive confirmation). Preserves core generated validation and retry preservation, authorization enforcement, destructive-confirmation Cancel autofocus and explicit confirm, the in-flow abandonment warning with Stay autofocus and Escape-stays, and the one-at-a-time FC-CNC gate. Authority: ux-design.md canonical rows (ux-design.md:394-395); DESIGN.md/EXPERIENCE.md supplement only and lose on conflict. AM-14 and AM-26 copy, channel, and dedupe are owned by Story 13.4.

**Acceptance Criteria:**

**Given** a generated form has client-validation errors (VR-01, FM-06, FM-07, AM-18, SS-37)
**When** submission is attempted
**Then** the complete error summary (`role="group"`, an accessible error-summary label, `tabindex="-1"`, no `aria-live`, no `role="alert"`) is inserted before it receives programmatic focus, and it states AM-18 "Correct the errors before submitting." with the error count, so that focus is its only speech path and there is exactly one speech event
**And** every declared field group keeps its visible label, programmatic group name and description, declared order, and stable error targets; each Fluent input exposes its invalid state and references its error; summary links appear in declared DOM order and move focus to their target control, or to the next invalid control when the target has disappeared; if the summary cannot render, focus goes to the first invalid Fluent input; the summary and focused control stay clear of sticky chrome; and correct values are preserved.

**Given** the server rejects a command with a support-safe field map (VR-02, AM-19)
**When** the rejection renders
**Then** the lifecycle stays `Rejected` with its identity retained, the mapped errors use the VR-01 relationships, and focus moves to the mapped summary once through the focused-summary path
**And** AM-14 is cancelled or suppressed for that mapped outcome so that no second live announcement occurs, and values are preserved.

**Given** the server returns an asynchronous Rejected lifecycle outcome with no support-safe field map (VR-03, FM-08, AM-14, SS-20)
**When** the rejection renders
**Then** the lifecycle rejection region shows a support-safe reason and recovery actions without inventing a field error, focus stays in the lifecycle/recovery context and is not moved just because the polite message updated, and the first recovery action follows the message in tab order
**And** AM-14 "Command rejected. Review the message and try again." is announced once through the polite status channel (not a focused summary or alert); edit-and-retry, return, copy safe support reference, and cancel all work without a pointer; values are preserved where editing or retrying is meaningful; the recovery message and actions are not covered by banners or pending summaries; and raw backend metadata, payloads, stack traces, and unrestricted PII stay hidden.

**Given** authorization denies a command (VR-04, SS-38)
**When** a fail-closed panel replaces the requested surface
**Then** no field is marked invalid, the panel names the unavailable action without policy internals, and focus moves to the denied-state heading through the AM-26 focus-only path with no live attributes
**And** return and re-authentication (when offered) work from the keyboard, and no sensitive command payload is kept beyond the owned form lifetime.

**Given** an authorized destructive action requires confirmation (OF-03, FM-05, SS-44)
**When** the `FcDestructiveConfirmationDialog` modal opens
**Then** its invoker is captured as the origin, initial focus goes to Cancel, the dialog has a programmatic name from its visible heading and a description from its visible consequence text, Tab/Shift+Tab cycle within the modal, and background content is not focusable
**And** a validation or policy failure focuses a complete local summary or the denied heading; Escape cancels and never confirms; cancel or close dispatches nothing and returns focus to the captured origin, or to the current route h1 when that origin was removed, disabled, or disconnected (SS-46, SS-47); confirmation dispatches once; and modal depth never exceeds one.

**Given** a form edited for at least 30 seconds, and a close, back, or navigation is intercepted (VR-06, OF-04, FM-11, SS-39, SS-45)
**When** `FcFormAbandonmentGuard` exposes its in-flow warning
**Then** the warning is not a dialog, does not trap focus, and has no modal cycle; its actions stay in page order with a programmatic name and description; initial focus goes to "Stay on form"; and the guard session is intentionally silent
**And** Stay or Escape hides the warning, dispatches no navigation, preserves input, and returns focus to the captured edited control, or to the form heading when that control was removed, disabled, or disconnected; only an explicit "Leave anyway" discards input and proceeds, using FM-01 at the destination; no implicit timeout decides for the operator; and the warning, focused action, and restored control with its focus indicator stay entirely outside sticky chrome and messages.

**Given** one local command is already in flight (VR-05, FM-09, AM-20, SS-25)
**When** another submit is attempted
**Then** FC-CNC blocks the later command without queueing, batching, or racing it; the dispatch count stays at one; no validation error is added; and AM-20 "This command did not run. Another command is already in progress." is announced once per blocked attempt through the polite status channel
**And** focus stays on the attempted submit control, "View active command" explicitly moves focus to the active lifecycle heading, both visible forms are preserved, the original command remains the only lifecycle allowed to advance under its own operation ID, and both the retained control and the optional destination are unobscured.

**Given** a protected generated command contains server-controlled or derived values (SS-36, SS-38)
**When** authorization is Pending, Authorized, or NotAuthorized
**Then** no protected form flashes before authorization, only editable Fluent inputs render, controlled values are injected server-side, and policy runs before and after BeforeSubmit plus at the service boundary
**And** unsupported types render a bounded placeholder without leaving broken controls or accepting hidden values as authorization.

**Given** this story's implementation and evidence are complete
**When** readiness status is evaluated
**Then** OI-16, G-4, FLUENT-APP-1, and Product approval stay open and are not closed or inferred by this story
**And** each closes only through its own evidence record and owner decision (ux-design.md:356-359).

### Story 13.4: [I · UX-C] Announce Projection and Command State Without Noise

As an operator monitoring projections and commands,
I want meaningful state changes announced once with truthful timing,
So that I understand progress and recovery without hearing retries, polling ticks, or false success.

**Canonical rows:** Implements/evidences AM-01 through AM-17, AM-22, AM-24, AM-26, AM-27, AM-30, and the UX-AM-1 dedupe/coalescing rule; SS-05, SS-06, SS-07 through SS-24, SS-26, SS-34, SS-35, SS-49. Preserves the delivered projection grid, loading/empty placeholders, row detail, status icon/badge, connection-recovery, and core lifecycle truth semantics. Authority: ux-design.md canonical rows (ux-design.md:394-395); DESIGN.md/EXPERIENCE.md supplement only and lose on conflict. AM-18/AM-19/AM-20 are owned by Story 13.3, AM-21 by Story 13.5, and AM-23/AM-25/AM-28/AM-29/AM-31 by Story 13.2.

**Acceptance Criteria:**

**Given** each announcement row this story owns
**When** it is emitted
**Then** it uses exactly its canonical AM-row copy (final microcopy), localized and free of support-sensitive values, through its named channel: the polite status channel (one shared `role="status"`/`aria-live="polite"` node per surface) for AM-01 through AM-17, AM-22, AM-24, AM-27, and AM-30, and the focused heading with no live attributes (focus is the only speech path) for AM-26
**And** no owned row uses `role="alert"`, an assertive region, or both a live region and focus for the same event.

**Given** the UX-AM-1 dedupe and coalescing rules
**When** events arrive for one group key (lifecycle operation ID; surface plus connection epoch; or surface plus operator-initiated load/filter operation)
**Then** each row's dedupe key includes its state or result identity, an eligible non-terminal change restarts the group's trailing 250ms timer, stale async results are discarded, and only the last eligible message is announced when the window closes
**And** a terminal outcome, focused summary, or navigation failure cancels the group's pending intermediate message and announces immediately.

**Given** fake time and operation O, with Submitting at t=0ms and Acknowledged at t=100ms (two different intermediate states in one group)
**When** time advances to t=349ms and then to t=350ms
**Then** no message has been announced at t=349ms (249ms after the last change), and at t=350ms (250ms) exactly one message, AM-11 "Command accepted. Waiting for confirmation.", has been announced and AM-10 never was
**And** when Syncing arrives and Confirmed follows within its 250ms window, the pending AM-12 is cancelled and AM-13 "Command confirmed." is announced immediately, so the exact sequence for O is [AM-11, AM-13] with a count of 2, and equivalent 249ms/250ms assertions cover one connection-epoch group and one load/filter group.

**Given** a projection enters Loading, Empty, Data, Stale, Reconnecting, FallbackPolling, SlowQuery, MaxItems, filter-no-results, query failure, or filter-hidden detail (SS-07 through SS-15, SS-34, SS-49)
**When** its meaningful state changes
**Then** the visible state exposes the meaning, permitted actions, recovery, and terminal/non-terminal class of its SS row, and announces once through AM-01, AM-02, AM-03 (only for an operator-initiated load or filter), AM-04, AM-05, AM-06, AM-08, AM-09, AM-27, AM-30 "Data could not be loaded." (with no empty-state substitution), or AM-22
**And** skeleton frames, retry and backoff attempts, poll ticks, virtualization batches, background refreshes with no meaningful change, and re-renders of the same result stay silent.

**Given** a query remains pending (SS-13, AM-08)
**When** fake time reaches 1,999ms and then 2,000ms
**Then** SlowQuery is not shown or announced at 1,999ms, and at 2,000ms it shows "slow, not failed" meaning and announces AM-08 "This is taking longer than expected." exactly once for that query
**And** elapsed-time ticks stay silent and SlowQuery clears when a result or failure settles.

**Given** the browser or network is offline and no current query can complete (SS-35)
**When** the surface detects offline
**Then** it shows that data cannot be refreshed, labels any cached or stale content, and announces AM-30 "You are offline. Data cannot be refreshed." once per connection epoch
**And** retry ticks and repeated identical failures stay silent, and recovery in the same connection epoch starts query reconciliation.

**Given** realtime connectivity fails and later recovers (SS-11, SS-12, AM-05, AM-06, AM-07)
**When** the resilience path runs
**Then** retries remain unbounded with jittered backoff capped at 30,000ms, a closed connection restarts within 10 seconds, and fallback polling runs every 15 seconds across at most eight lanes, with each backoff attempt and poll silent
**And** successful reconciliation announces AM-07 "Connection restored. Data refreshed." once per connection epoch and shows the Reconnected notice for 3,000ms, whose expiry is silent.

**Given** a command advances through Submitting, Acknowledged, Syncing, and a terminal outcome (SS-16 through SS-23)
**When** lifecycle feedback renders
**Then** Submitting announces AM-10, Acknowledged announces AM-11 and is never styled or announced as Confirmed, Syncing announces AM-12, and Confirmed and IdempotentConfirmed share AM-13 "Command confirmed." while keeping their distinct machine identity under first-terminal-wins, with NeedsReview using AM-15 and Warning AM-16
**And** Rejected without a safe field map announces AM-14 once through the polite status channel while focus stays in the lifecycle/recovery context (VR-03, FM-08); a safely field-mapped rejection uses the Story 13.3 AM-19 focused summary instead; and later duplicate terminal observations stay silent.

**Given** confirmation has not arrived (SS-18, SS-24, SS-26)
**When** fake time reaches 9,999ms and 10,000ms, and later 119,999ms and 120,000ms
**Then** at 10,000ms the UI enters Degraded with polling active (SS-24) and announces AM-17 once, while confirmed-status polling continues every 1,000ms silently
**And** at 120,000ms polling stops and the lifecycle enters the terminal Degraded, polling-exhausted state (SS-26), announcing AM-24 "Confirmation was not received. Review status later or continue working." once, immediately, and with no success claim; every later poll or result for that closed local lifecycle is silent, the local lifecycle makes no automatic transition, and later backend evidence appears only as a newly correlated update, never a retroactive mutation.

**Given** dispatch and acknowledgement timing (SS-16, SS-17)
**When** a transient failure occurs before or after acknowledgement
**Then** zero pre-accept retries occur, and exactly one transient retry runs 250ms after acknowledgement, verified at the 249ms and 250ms boundaries with deterministic time
**And** the retry tick is silent (AM-11 silent behavior), and a retryable failure preserves input.

**Given** tenant context is missing or stale, or authorization denies visibility or activation (SS-05, SS-06, AM-26)
**When** the surface would render
**Then** an explicit fail-closed context or denied state replaces the surface instead of empty-looking data, focus moves to its exact visible heading with no live attributes once per activation attempt and outcome, and entries hidden by policy stay silent
**And** the copy exposes no tokens, policy internals, raw EventStore metadata, stack traces, event payloads, or unrestricted PII.

**Given** the delivered projection grid, loading/empty placeholders, row detail, status icon, and badge components
**When** their focused regression scenarios run
**Then** filtering is debounced and resettable (SS-15, AM-27); virtualization begins at 500 rows and unfiltered results cap at 10,000 (SS-14, AM-09); expanded detail stays a labelled region and a filter-hidden expanded row recovers focus at a valid grid control (SS-49, AM-22); and column priority above 15 columns is preserved as delivered baseline behavior
**And** skeletons match the expected layout (SS-07), empty results (SS-08) are distinguished from no filter matches (SS-15), and status meaning stays available through icon or shape plus text (ux-design.md:57-62, AE-07).

**Given** the visual treatment of Warning, NeedsReview, Degraded, missing tenant, FallbackPolling, SlowQuery, MaxItems, or fresh-row dismissal is still unresolved
**When** those states are implemented or documented
**Then** they use the canonical AM-row copy as final microcopy and inherit the nearest Fluent semantic treatment without inventing a new visual language
**And** the unresolved question covers visual treatment only, and its disposition is recorded by the UX and Product owners, not by this story.

**Given** this story's implementation and evidence are complete
**When** readiness status is evaluated
**Then** OI-16, G-4, FLUENT-APP-1, and Product approval stay open and are not closed or inferred by this story
**And** each closes only through its own evidence record and owner decision (ux-design.md:356-359).

### Story 13.5: [I · UX-D] Preserve Fresh-Row Meaning Across Visual and Data Changes

As an operator watching a projection after a command,
I want a fresh-row marker only on the material row my command changed,
So that I can trust the indicator across filtering, paging, accessibility modes, and expiry.

**Canonical rows:** Implements/evidences AM-21, SS-48, and the fresh-row portions of AE-07 and AE-08. Preserves the FC-NIP live composition baseline (2026-08-27 candidate `7a573763`) and the DW-679 server-allocated-key non-goal. Authority: ux-design.md canonical rows (ux-design.md:394-395); DESIGN.md/EXPERIENCE.md supplement only and lose on conflict.

**Acceptance Criteria:**

**Given** an eligible terminal Material outcome has an immutable pre-dispatch target identity
**When** the resolver publishes the result
**Then** an already-rendered scoped grid updates the matching ViewKey/EntityKey without an unrelated render, using atomic first-wins behavior
**And** the appearance announces AM-21 "Updated row: {accessible row label}." through the polite status channel without moving focus.

**Given** the same tenant, user, and entity transition is published or visible in two views (AM-21)
**When** the operator changes view, or the transition is published again under another ViewKey
**Then** AM-21 is announced once per tenant plus user plus entity transition, and changing view never re-announces the same transition, even though publication may stay view-keyed
**And** suppressed duplicate publications, expiry, and removal stay silent, proven by an FC-NIP bUnit/e2e assertion across two views plus a silent-expiry assertion.

**Given** identity or materiality is Unknown, the result is NoOp, delete, Rejected, or NeedsReview, or the key is server allocated
**When** terminal resolution completes
**Then** no fresh-row indicator is published
**And** no SignalR nudge, visible-row diff, aggregate identifier, or untyped result is used as substitute identity.

**Given** a marked row is filtered, paged, requeried, dismissed, or expires
**When** generated consumers invalidate
**Then** the grid stays consistent and scope-safe, provenance is neither replaced nor extended by a later message, and expiry is silent
**And** filter removal does not imply that the entity was deleted.

**Given** a marked row's tenant or user scope is cleared or changes (SS-48)
**When** the next scope renders
**Then** the prior-scope cue and its state are removed before the new scope renders, so no prior-scope marker is ever visible in the new scope
**And** the removal is silent and does not re-announce in either scope.

**Given** forced-colors or reduced-motion mode is active (AE-07, AE-08)
**When** the fresh-row state appears
**Then** shape and text preserve its meaning without color, background image, or animation, and state changes stay immediate
**And** the existing ten-second active window is tested with deterministic time rather than a new evidence mechanism.

**Given** this story's implementation and evidence are complete
**When** readiness status is evaluated
**Then** OI-16, G-4, FLUENT-APP-1, and Product approval stay open and are not closed or inferred by this story
**And** each closes only through its own evidence record and owner decision (ux-design.md:356-359).

### Story 13.6: [I · UX-E] Verify Responsive and Assistive Accessibility

As an operator using zoom, text spacing, keyboard navigation, forced colors, reduced motion, or assistive technology,
I want generated shell workflows to remain perceivable and operable,
So that accessibility does not depend on a preferred viewport or input mode.

**Canonical rows:** Implements/evidences AE-01 through AE-09 for every changed shell, navigation, tab, toolbar, projection, form, lifecycle, palette, and dialog surface. Preserves the compact `32px` default row metric, the `75rem` constrained measure, the nine `FcTypoToken` mappings with `TypographyMappingVersion = "3.1.0"`, and inherited Fluent component visuals (ux-design.md:49-55, :73-79, :128-132, :386-387). Authority: ux-design.md canonical rows (ux-design.md:394-395); DESIGN.md/EXPERIENCE.md supplement only and lose on conflict.

**Acceptance Criteria:**

**Given** a 320 CSS-pixel viewport (AE-01)
**When** every operation and overlay on each changed surface is exercised
**Then** there is no page-level horizontal scrolling and no content or operation loss, and only a labelled grid region and the tab strip may each own bounded, labelled horizontal scrolling
**And** reading and focus order stay logical and the selected tab and focus indicator stay visible.

**Given** browser zoom at 400% on a 1280 CSS-pixel reference viewport (AE-02)
**When** the AE-01 flows are repeated
**Then** the AE-01 outcome holds
**And** no control is clipped, no overlay is inaccessible, and no fixed-content trap exists.

**Given** WCAG 1.4.12 text-spacing overrides of line height 1.5×, paragraph spacing 2×, letter spacing 0.12×, and word spacing 0.16× the font size (AE-03)
**When** the same journeys run
**Then** there is no loss, clipping, overlap, or control truncation, and keyboard behavior stays equivalent
**And** compact grid rows are exactly `32px` only at default text settings, and expand or reflow without loss under AE-02 zoom and AE-03 spacing, so `32px` is never a clipping ceiling.

**Given** every pointer target on every changed surface, including skip, account, settings, and menu controls, Home cards and CTAs, rail, tabs, toolbar, grid and detail, forms, lifecycle and recovery actions, palette, dialogs, and the abandonment guard (AE-04)
**When** targets are inventoried and measured
**Then** each target is at least 24 by 24 CSS px or records exactly one Inline, Spacing, Equivalent, User Agent Control, or Essential exception; a Spacing exception proves that a 24 CSS-pixel-diameter circle centered on the undersized target intersects no other target or neighboring circle, and an Equivalent exception names a separate conforming control for the same function
**And** the evidence records the target, exception, measurement or equivalent control, rationale, and keyboard path.

**Given** keyboard traversal of each surface with sticky chrome, scroll regions, popovers, drawers, messages, and dialogs active (AE-05)
**When** each focus stop is reached
**Then** the entire target and its focus indicator are visible within the active viewport or scrollport
**And** focus is never behind app-owned content; this product floor is stricter than the WCAG 2.4.11 minimum and is asserted by e2e geometry.

**Given** each UX-VC-1 foreground/background pair in both active themes (AE-06)
**When** contrast is measured
**Then** normal text meets 4.5:1, large text meets 3:1, and meaningful non-text and focus boundaries meet 3:1 against adjacent colors
**And** Fluent-inherited pairs cite inheritance plus visual/conformance evidence, while every changed pair has computed-style proof.

**Given** `forced-colors: active` and `prefers-reduced-motion: reduce` emulation (AE-07, AE-08)
**When** status, lifecycle, navigation, reconnecting, and fresh-row states render
**Then** system color, text, border, icon, shape, and current-state cues survive without authored color or background images, non-essential transitions, pulse, and smooth scrolling stop, and state text/icon/shape and focus changes stay immediate
**And** expiry stays silent, and no hover-only or motion-only meaning is introduced.

**Given** the axe helper and the semantic DOM, keyboard, focus, and announcement-count assertions (AE-09)
**When** they run on each changed surface
**Then** axe runs with WCAG 2.2 AA tags (`wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, `wcag22aa`) in place of the current WCAG 2.1-only tag set, and product-specific assertions pass
**And** there are zero critical accessibility findings and no hover-only action.

**Given** the configured accent, typography mappings, rail widths, compact row height, constrained measure, and inherited Fluent component visuals
**When** light/dark, zoom, text-spacing, and contrast checks run
**Then** `--fc-color-accent`, if present, is only an alias of the active Fluent V5 accent role and never owns an independent seed or palette, the accent stays a thread rather than chrome fill, and the nine `FcTypoToken` mappings and `TypographyMappingVersion` 3.1.0 remain intact
**And** the 72px/48px rail widths and the `75rem` measure stay exact at default settings, and no legacy token, hard-coded semantic palette, custom type ramp, decorative theme, or fabricated numeric breakpoint is introduced.

**Given** behavior that automation cannot establish, such as actual screen-reader speech of focus-only and live-region paths and real-device zoom and reflow
**When** evidence is assembled for each changed surface
**Then** a manual assistive-technology and real-device pass is recorded naming the assistive technology, browser, and device combination, the surfaces and journeys covered, and each outcome
**And** automated evidence supplements but never replaces this required manual evidence, and missing manual evidence leaves this story incomplete.

**Given** existing bUnit/e2e accessibility lanes can deterministically prove a requirement
**When** evidence is assembled
**Then** those focused results are reused, and manual checks are limited to behavior that automation cannot establish
**And** one candidate-bound result references the evidence instead of duplicating it into new reports or workflows.

**Given** this story's AE-01 through AE-09 evidence exists
**When** the opt-in UX reviewer gate is reached
**Then** the gate runs on this story's evidence without waiting for results from UX-A through UX-F, or an explicit authorized skip is recorded, and every Critical/High finding receives a disposition
**And** owner decisions, including FLUENT-APP-1, Product approval, and G-4, are excluded from the reviewer disposition and stay with their named owners.

**Given** this story's implementation and evidence are complete
**When** readiness status is evaluated
**Then** OI-16, G-4, FLUENT-APP-1, and Product approval stay open and are not closed or inferred by this story
**And** each closes only through its own evidence record and owner decision (ux-design.md:356-359).

### Story 13.7: [I · UX-F] Provide Reusable UX Assertions for Adopters

As an adopter test engineer,
I want deterministic FrontComposer Testing helpers for the canonical interaction and accessibility matrices,
So that downstream modules can verify generated failure and recovery UX without app-specific selectors or hidden timing.

**Canonical rows:** Expresses assertions for UX-AM-1 (AM-01 through AM-31 and the dedupe/coalescing rule), UX-VR-1 (VR-01 through VR-06), UX-FM-1 (FM-01 through FM-12), UX-OF-1 (OF-01 through OF-04 and origin capture), UX-SS-1 (SS-01 through SS-49), and UX-AE-1 (AE-01 through AE-09), and satisfies the FR-22 ledger row (ux-design.md:334). Preserves the delivered core failure-state Testing package harness. Authority: ux-design.md canonical rows (ux-design.md:394-395); DESIGN.md/EXPERIENCE.md supplement only and lose on conflict.

**Acceptance Criteria:**

**Given** the FrontComposer Testing host and deterministic fakes
**When** an adopter configures validation, rejection, timeout/stall, authorization denial, offline, paging, filtering, sorting, or lifecycle scenarios
**Then** helpers can assert linked summaries and field relationships (VR-01, VR-02), input preservation, state truth per SS row, blocked-submit feedback (VR-05, AM-20), and deduplicated announcements
**And** stable `data-testid` selectors supplement rather than replace accessible roles and names.

**Given** a surface emits announcements
**When** a message-sequence helper runs
**Then** it asserts the exact ordered message sequence and exact count per channel and coalescing group (for example [AM-11, AM-13] with a count of 2 for one operation), identified by AM row ID and canonical copy
**And** it fails on any extra, missing, reordered, or duplicate message, including silent-behavior rows that emit.

**Given** fake time and a coalescing group
**When** a coalescing-boundary helper advances time to 249ms and then 250ms after the last eligible change
**Then** it asserts that no message has been emitted at 249ms and exactly the last eligible message at 250ms, with stale async results discarded
**And** it asserts that a terminal outcome, focused summary, or navigation failure cancels the pending intermediate message and announces immediately.

**Given** an announcement or focus event
**When** a channel-distinction helper runs
**Then** it distinguishes the polite status channel, the palette's polite combobox status (single `aria-atomic` owner with inherited result-count speech omitted), the focused summary (`role="group"`, accessible label, `tabindex="-1"`, no `aria-live` or `role="alert"`), and the focused heading with no live attributes
**And** it fails when one event reaches both a live region and focus, or reaches the wrong channel for its AM row.

**Given** a palette, settings dialog, destructive dialog, or abandonment guard opened from shell navigation or from page content
**When** an origin-capture helper runs
**Then** it records the captured origin as a direct handle plus its stable evidence locator before activation, and asserts return to that origin on close
**And** it can remove, disable, or disconnect the origin before close and assert the FM-04/FM-05 route-h1 fallback (or the FM-11 form-heading fallback), never `body`.

**Given** a focused element under sticky chrome, scroll regions, popovers, drawers, messages, or dialogs
**When** an unobscured-focus geometry helper runs
**Then** it asserts that the focused target's bounding box and its full focus indicator lie within the active viewport or scrollport and intersect no app-owned sticky, overlay, or message content (AE-05 and the FM-row unobscured-focus column)
**And** it reports the measured geometry in redacted evidence.

**Given** lifecycle, retry, reconnect, SlowQuery, and expiry behavior
**When** helpers control time
**Then** deterministic time drives the 250ms coalescing and post-acknowledgement retry, the 2,000ms SlowQuery threshold, the 3,000ms Reconnected notice, the 10,000ms Degraded entry, the 1,000ms and 15s polls, the 30,000ms backoff cap, the 120,000ms polling ceiling, and the ten-second fresh-row window, with N-1/N boundary assertions at 1,999/2,000ms and 119,999/120,000ms
**And** no downstream app-specific selector or machine-dependent wait is required.

**Given** route, tab, palette, dialog, row-detail, and fresh-row journeys
**When** helpers assert focus and accessibility behavior
**Then** they support keyboard recovery, invoker restoration, removed-content avoidance, first-wins scope, cross-view single announcement (AM-21), and silent expiry
**And** they express reflow/zoom, text-spacing, target-size (including exception records), focus-not-obscured, forced-colors, reduced-motion, and WCAG 2.2 AA axe outcomes (AE-01 through AE-09), with output redacted by default.

**Given** each public or internal helper added or extended by this story
**When** its consumer test runs
**Then** the test exercises the helper against a realistic failure or policy state (for example an unmapped server rejection, an authorization denial, offline, or polling exhaustion) rather than a synthetic happy path
**And** the recorded evidence is redacted, containing no token, JWT, tenant payload, stack trace, raw backend metadata, or unrestricted PII.

**Given** equivalent deterministic helpers or evidence recorders already exist
**When** this story is implemented
**Then** they are extended rather than duplicated, and any public API change updates its intentional baseline
**And** no downstream app-specific selector or machine-dependent wait is required.

**Given** this story's implementation and evidence are complete
**When** readiness status is evaluated
**Then** OI-16, G-4, FLUENT-APP-1, and Product approval stay open and are not closed or inferred by this story
**And** each closes only through its own evidence record and owner decision (ux-design.md:356-359).

### Story 13.8: [A · E9-APP-1] Accept the Completed Fresh-Row Live Proof

As the Product Owner,
I want to accept or reject the completed Story 9.8 live proof explicitly,
So that Epic 9 delivery history remains distinct from Product acceptance.

**Acceptance Criteria:**

**Given** the immutable Story 9.8 live record and its candidate identity
**When** the Product Owner reviews the proof
**Then** a dated decision cites the exact record and states accept or reject
**And** absence of the decision leaves G-5 open.

**Given** the proof is accepted
**When** readiness status is evaluated
**Then** E9-APP-1 closes without changing the completed status of Stories 9.1 through 9.8
**And** the decision does not claim coverage for server-allocated-key commands or other accepted non-goals.

**Given** the proof is rejected
**When** corrective work is required
**Then** a new bounded residual item identifies the specific deficiency
**And** completed Epic 9 implementation and immutable evidence history are not rewritten.

### Story 13.9: [A · FLUENT-APP-1] Decide the Exact Fluent UI V5 Posture

As the Product Owner and Architect,
I want to accept an exact catalog-owned Fluent UI V5 identity and upgrade posture,
So that UX readiness is tied to the components and tokens actually tested.

**Acceptance Criteria:**

**Given** UX-A through UX-F evidence for one exact catalog identity
**When** the Product Owner and Architect review the Fluent posture
**Then** a dated decision identifies the exact package/catalog revision, RC or GA status, and supported token/API names
**And** no floating label, legacy token, or unverified component name substitutes for that identity.

**Given** the selected identity is accepted
**When** its lifecycle rules are recorded
**Then** the decision defines the breaking-change rule, GA re-decision trigger, and accessibility/visual revalidation trigger
**And** documentation may cite the decision rather than reproduce its evidence.

**Given** the identity is rejected or cannot be approved
**When** the decision is recorded
**Then** G-4 remains open and any corrective implementation is captured as a new bounded residual
**And** completed UX evidence remains immutable history rather than being relabelled.

## Epic 14: AI Integrators Expose Domain Operations Safely

AI integrators can expose generated commands, projections, skills, and lifecycle polling through a host-authenticated MCP surface with tested disclosure boundaries.

### Story 14.1: [I · MCP-SEC-1] Enforce Production MCP Authorization Composition

As an AI-agent integrator,
I want production MCP hosting to reject permissive or incomplete authorization composition,
So that generated tools and resources cannot be exposed without tenant gates and host authentication.

**Acceptance Criteria:**

**Given** a non-Development host registers either shipped `AllowAllMcpTenantToolGate` or `AllowAllResourceVisibilityGate`
**When** FrontComposer MCP startup validation runs
**Then** startup fails closed with a support-safe error naming the invalid gate type
**And** no MCP endpoint becomes available. Custom host-supplied gate semantics remain the host's responsibility under FR-19.

**Given** either IFrontComposerMcpTenantToolGate or IFrontComposerMcpResourceVisibilityGate is missing
**When** the host starts
**Then** startup throws the documented InvalidOperationException naming the missing registration
**And** the error exposes no tenant, tool, fingerprint, token, or internal exception detail.

**Given** MapFrontComposerMcp is mapped in a production-like host
**When** endpoint metadata and unauthenticated access are inspected
**Then** host authorization is required through RequireAuthorization() or an equivalent policy before dispatch
**And** an unprotected mapping fails the focused production-composition assertion.

**Given** valid host authentication, both restrictive gates, a visible generated command, and a compatible schema class
**When** the caller lists and invokes the tool
**Then** the normal operation succeeds and TenantId, UserId, MessageId, and CorrelationId are injected server-side
**And** Exact, CompatibleAdditive, and CompatibleWarning are the only negotiation classes permitted to reach a side effect.

### Story 14.2: [I · MCP-SEC-2] Prove MCP Leak and Oracle Resistance

As a security QA maintainer,
I want one focused MCP audit to exercise every documented disclosure boundary,
So that hidden tools, tenant data, lifecycle state, and internal diagnostics do not leak through inconsistent public shapes.

**Acceptance Criteria:**

**Given** the same caller requests an absent, hidden, unauthorized, tenant-less, or policy-denied tool
**When** the named SM-4 audit class captures tools/call results
**Then** the hidden and absent shapes are byte-identical and use unknown_tool, HFC-MCP-UNKNOWN-TOOL, the bounded caller-visible tool list, and Request failed.
**And** the response does not echo the requested name, tenant, fingerprint, token, or internal exception.

**Given** unauthenticated, missing-tenant, and authenticated callers
**When** tools/list, resources/list, and resources/read are compared
**Then** tools/list fails closed to the documented empty collection, resources/list exposes only the intentionally static generated catalog, and registered hidden projection reads return unknown_resource
**And** the audit does not falsely claim that the disclosed catalog or credential-validity signal is secret.

**Given** a registered hidden resource, an unregistered URI dispatched through the SDK, a skill resource, and a visible incompatible projection
**When** each resource is read
**Then** registered hidden reads, SDK not-found behavior, bounded skill failures, and schema-mismatch behavior match their distinct documented contracts
**And** hidden-resource admission is evaluated before schema detail where required.

**Given** two tenants, two users, and command lifecycle state created in one request scope
**When** later scopes query tools, projections, or frontcomposer.lifecycle.subscribe
**Then** lifecycle remains available only to its authorized scope, cross-tenant and cross-user access fails through the opaque public shape, and normal same-scope polling still works
**And** singleton lifecycle storage with a scoped tracker remains covered as regression behavior.

**Given** existing ToolAdmissionTests, ProjectionReaderTaxonomyTests, SchemaNegotiationPrecedenceMatrixTests, and AuthRedactionStressTests already prove part of the matrix
**When** MCP-SEC-2 is implemented
**Then** the named audit reuses those focused results and adds only missing endpoint-authentication, allow-all, SDK-dispatch, isolation, and oracle cases
**And** one candidate-bound test result is referenced rather than copied into duplicate security packets or workflows.

### Story 14.3: [A · MCP-APP-1] Record Independent MCP Security Acceptance

As an independent security reviewer,
I want to accept or reject the immutable MCP production-security result,
So that G-7 closes only after its remaining disclosure and oracle risks receive an explicit disposition.

**Acceptance Criteria:**

**Given** MCP-SEC-1 and MCP-SEC-2 are complete for one immutable candidate
**When** a reviewer who did not implement MCP-SEC-1 evaluates the focused results
**Then** the dated decision cites the exact candidate and test artifacts and records accept or reject
**And** an unsigned or non-independent review leaves G-7 open.

**Given** the documented tools/list credential-validity signal, static resources/list catalog, SDK unregistered-URI distinction, and host-authentication responsibility
**When** residual risks are reviewed
**Then** each receives an explicit accept, reject, or bounded-follow-up disposition
**And** timing side channels remain the stated out-of-scope item rather than being silently claimed as tested.

**Given** acceptance is granted
**When** readiness is evaluated
**Then** MCP-APP-1 closes without expanding the exact FR19 guarantees
**And** the approval references existing immutable proof instead of reproducing it.

**Given** acceptance is rejected
**When** corrective work is required
**Then** a new bounded residual item identifies the failed guarantee
**And** completed implementation and prior evidence history remain unchanged.

## Epic 15: Maintainers Evolve FrontComposer Without Contract Drift

Framework maintainers can customize, inspect, migrate, document, test, and upgrade FrontComposer while preserving public contracts and exact runtime identity.

### Story 15.1: [I · PLAN-INT-2] Restore Current Artifact-Integrity Validation

As a planning and QA maintainer,
I want the existing story-artifact validator to detect current contradictions and incomplete File Lists,
So that new planning drift fails closed without rewriting accepted delivery history.

**Acceptance Criteria:**

**Given** the current Story 11.32 validation scope and repository state
**When** the existing validation command runs
**Then** its declared story-owned files and current changed artifacts reconcile and the command passes
**And** the repair does not change the completed status or historical acceptance of Story 11.32.

**Given** a fixture with stale epic/story/action status or a missing or empty File List
**When** the validator evaluates it
**Then** validation fails with a deterministic support-safe diagnostic identifying the contradiction class
**And** the failure does not depend on machine-specific paths or unrelated working-tree changes.

**Given** completed Epic 9–11 outcomes, closed FR27/FR28 decisions, and FR29.5/FR29.6 regression gates
**When** planning integrity is checked
**Then** completed history remains closed and points to its existing focused regression coverage
**And** only current residual aliases remain represented as open work.

**Given** the repository already contains the validator and its focused tests
**When** this story is implemented
**Then** those sources are corrected and extended rather than replaced by a second validator, schema, or report
**And** the exact command and focused result are recorded as the minimum-sufficient proof.

### Story 15.2: [I · DOC-A] Reconcile Component and Page-Pattern Documentation

As an adopter and framework maintainer,
I want component, index, status, toolbar, and tab documentation to match the implemented public surface,
So that customization and Testing guidance does not describe stale or internal-only APIs.

**Acceptance Criteria:**

**Given** the implemented public component surface and docs/reference/components inventory
**When** documentation parity is evaluated
**Then** every in-scope public component page is indexed and names the implemented API, state behavior, accessibility contract, and current status
**And** stale, missing, or unclassified entries fail the existing documentation-conformance check.

**Given** FcPageToolbar and FcPageTabs documentation
**When** their contracts are described
**Then** public behavior, routing, focus, and supported customization seams are explicit
**And** implementation-owned internal Fluent composition is not promoted into a public API.

**Given** projection status, lifecycle, customization, and Testing documentation
**When** pages are reconciled
**Then** they describe the canonical state vocabulary, development-only diagnostic behavior, and deterministic/redacted adopter assertions
**And** they do not expose raw payloads, tenant/user values, tokens, stack traces, or app-specific selectors.

**Given** a projection override mismatch or render fault
**When** customization diagnostic behavior is documented and regression-tested
**Then** FcCustomizationDiagnosticPanel appears only in Development with bounded corrective guidance and deterministic Level 4, Level 2, then generated-default resolution
**And** it does not replace operator data with raw payload, tenant/user values, tokens, or stack traces.

**Given** existing DocFX, inventory, and component-documentation tests
**When** this story is verified
**Then** those checks are extended only for missing semantic parity
**And** no duplicate documentation registry or evidence report is created.

### Story 15.3: [I · DOC-B] Align Migration and Classification Guidance

As a framework maintainer upgrading a consumer,
I want migration documentation and classification to agree with executable tooling,
So that I can distinguish supported automatic migrations from manual-only changes before writing files.

**Acceptance Criteria:**

**Given** MigrationCatalog and the published migration index
**When** executable and documented version edges are compared
**Then** every executable edge has matching guidance and every manual-only package/API edge is explicitly classified
**And** an unclassified or falsely executable edge fails the existing parity check.

**Given** frontcomposer migrate is invoked for a supported edge
**When** dry-run and apply behavior are documented
**Then** dry-run remains the default, apply remains atomic, and generated, submodule, symlinked, out-of-root, bin, obj, and .git targets are refused
**And** frontcomposer.cli.migrate.v1 remains the named machine-readable contract.

**Given** frontcomposer inspect documentation
**When** maintainers compare generated forms, grids, registrations, manifests, warnings, and errors
**Then** text/JSON ordering, severity/fail behavior, relative path rewriting, and frontcomposer.cli.inspect.v1 are accurately described
**And** machine-specific paths and sensitive values remain redacted.

**Given** existing CLI behavior and documentation tests already establish most of the contract
**When** this story is completed
**Then** only missing classification/parity assertions and documents are changed
**And** the CLI or generated output is not reimplemented solely for documentation evidence.

### Story 15.4: [I · DOC-C] Correct the Fluent UI V5 Contingency Identity

As an adopter evaluating FrontComposer's Fluent dependency,
I want contingency documentation to identify the exact catalog-owned Fluent UI V5 version currently selected,
So that I do not follow guidance written for a stale release candidate.

**Acceptance Criteria:**

**Given** the selected Hexalith.Builds catalog and docs/fluent-ui-v5-contingency.md
**When** the documented Fluent identity is resolved
**Then** the document names the exact catalog-owned package identity and supported API/token posture
**And** it does not retain the stale rc.2 label when the selected catalog resolves another identity.

**Given** FLUENT-APP-1 is pending, accepted, or rejected
**When** contingency status is documented
**Then** the page reports that actual decision state without implying approval from package selection alone
**And** it cites the dated decision when one exists.

**Given** a future catalog change
**When** existing documentation validation runs
**Then** a focused mechanical drift check detects identity mismatch
**And** the check reuses the catalog/document validation path instead of introducing a second package-version registry.

**Given** an exact update cannot be made and Product grants a bounded exception
**When** the exception is recorded outside story completion
**Then** it names owner, rationale, expiry, and revisit trigger
**And** absence of either an updated document or valid exception leaves DOC-C open.

### Story 15.5: [I · REL-BASE-1] Reconcile the Published Compatibility Baseline

As a package maintainer,
I want ApiCompat to compare against the latest evidence-backed published release or an explicitly approved bounded lag,
So that breaking-change detection reflects the public package history consumers actually depend on.

**Acceptance Criteria:**

**Given** the release ledger and available published-package evidence
**When** the latest evidence-backed release is identified
**Then** PUBLISHED_BASELINE_VERSION advances from 4.4.0 to that release, currently expected to be v4.5.0
**And** the baseline is not advanced from a tag or version label without the required package evidence.

**Given** the baseline advances
**When** ApiCompat and affected public-surface checks run
**Then** intentional differences are handled through existing baseline, documentation, and migration/deprecation mechanisms
**And** schema canonicalization, diagnostic bands, CLI JSON, generated-output paths, analyzer policy, and Contracts boundaries remain governed public contracts.

**Given** evidence does not support advancement
**When** the Release Owner chooses a temporary lag
**Then** a dated decision records the reason, owner, expiry, and blocking evidence
**And** the story remains open until either the baseline advances or that bounded decision exists.

**Given** existing PublicAPI, ApiCompat, drift, snapshot, and migration lanes already prove contract behavior
**When** this story is verified
**Then** those focused results are reused
**And** no duplicate compatibility scanner, package reconstruction, or evidence bundle is introduced.

## Epic 16: Maintainers Verify and Approve Exact Runtime Compatibility

FrontComposer, EventStore, and Release maintainers can bind one exact runtime tuple to live compatibility evidence and named approval without rewriting historical identities.

### Story 16.1: [I · EVT-ID-1] Capture the Current EventStore Runtime Tuple

As a FrontComposer runtime maintainer,
I want one immutable record of the exact FrontComposer, EventStore, Builds, and package tuple tested for release,
So that compatibility evidence cannot be projected onto a later repository identity.

**Acceptance Criteria:**

**Given** the repository state at story execution
**When** the successor identity is captured
**Then** it binds the exact FrontComposer HEAD, EventStore source gitlink, Builds gitlink, catalog-owned EventStore package version, and schema/version identity
**And** every repository identity is recorded as the exact lowercase commit selected at execution.

**Given** the exact tuple is frozen
**When** provider and AppHost verification run
**Then** live Pact provider evidence and the AppHost smoke result execute against that same tuple and their artifact hashes are recorded
**And** a result from Story 11.24, Story 11.25, v3, or any other prior tuple is retained as history but cannot satisfy the successor.

**Given** the successor record is created before owner approval
**When** its initial state is inspected
**Then** migrationApprovalClaimed remains false and required approval receipts remain absent
**And** technical capture alone does not close G-3.

**Given** FrontComposer HEAD, either gitlink, the catalog package, provider evidence, AppHost evidence, or an artifact hash changes
**When** governance compares the active identity with the candidate
**Then** validation fails closed and requires a new successor record
**And** no existing immutable identity or evidence packet is edited to describe the new tuple.

**Given** existing identity, Pact, AppHost, hashing, and governance mechanisms can produce the required proof
**When** this story is implemented
**Then** those mechanisms are reused for one candidate-bound packet
**And** no parallel identity schema, duplicate compatibility suite, or reconstructed evidence bundle is introduced.

### Story 16.2: [A · EVT-XFER-1 · Conditional] Transfer Migration-Approval Ownership Explicitly

As the Product Owner, Architect, and Release Owner,
I want to record an explicit ownership transfer if no distinct EventStore maintainer can approve migration,
So that role equivalence is decided rather than inferred.

**Acceptance Criteria:**

**Given** the required EventStore maintainer role is unavailable or proposed to move
**When** the conditional transfer path is invoked
**Then** a dated decision names the receiving role and accountable person or group
**And** it identifies the exact trigger, required evidence, due condition, and milestone effect.

**Given** EVT-ID-1 exists for an exact tuple
**When** ownership is transferred
**Then** the receiving owner inherits the same evidence-review and durable-receipt obligation
**And** the transfer itself does not approve migration or close G-3.

**Given** no valid transfer record exists
**When** EVT-APP-1 is evaluated without the original EventStore maintainer receipt
**Then** approval fails closed
**And** FrontComposer or Release ownership is not treated as implicit EventStore authority.

**Given** the original ownership remains available
**When** no transfer is necessary
**Then** EVT-XFER-1 is recorded as not invoked rather than fabricated
**And** no additional approval artifact is required.

### Story 16.3: [A · EVT-APP-1] Approve or Reject the Exact Runtime Migration

As the EventStore maintainer, FrontComposer maintainer, and Release Owner,
I want to approve or reject the exact EVT-ID-1 tuple through durable receipts,
So that migration readiness reflects named owner judgment over the evidence actually tested.

**Acceptance Criteria:**

**Given** EVT-ID-1 contains a complete immutable tuple and live provider/AppHost results
**When** each required owner reviews it
**Then** each durable receipt cites the exact record digest and states approve or reject
**And** a missing, stale, role-inferred, or tuple-mismatched receipt leaves G-3 open.

**Given** a valid EVT-XFER-1 exists
**When** approval receipts are evaluated
**Then** the named receiving owner substitutes only for the transferred role and all other required owners still sign
**And** the transfer record is cited without being treated as migration approval.

**Given** all required receipts approve the same current tuple
**When** the successor record is finalized
**Then** migrationApprovalClaimed may become true and governance can recognize that exact tuple as approved
**And** later tuple drift immediately requires new evidence and approval rather than inheriting this decision.

**Given** any owner rejects the tuple
**When** corrective work is required
**Then** a new bounded residual identifies the compatibility or evidence deficiency
**And** EVT-ID-1, prior identity records, and completed historical stories remain immutable.

**Given** the owners review the existing candidate-bound packet
**When** the decision is recorded
**Then** receipts reference its artifacts and hashes rather than reproducing them
**And** no additional evidence workflow is created solely for approval.

## Epic 17: Release Owners Establish Safe Publication and Trustworthy Release Records

Release owners can authorize and verify exact package bytes through a privilege-separated pipeline, maintain truthful append-only attempt records, preserve incidents, and support an independent evidence-backed Product readiness decision.

### Story 17.1: [I · GOV-SRC-1] Reconcile Publication-Governance Sources

As an architect,
I want every governance source to distinguish current execution, dependency, lineage, and evidence identities,
So that release decisions cannot substitute labels or stale commits for exact provenance.

**Acceptance Criteria:**

**Given** the PRD, architecture, FC-DEP-1, G2 request, GOV-1 spine/story, identity register, workflow caller, and current root gitlinks
**When** their publication-governance claims are compared
**Then** they consistently distinguish the owner-accepted lineage anchor, workflow execution pin, current Builds gitlink, future split revision, and evidence-bound candidate identities
**And** no source describes the active legacy caller as G-8 conformant.

**Given** one source changes a governed identity, trust boundary, halt posture, or evidence contract
**When** the existing source-consistency validation runs
**Then** incompatible projections fail with the specific source and field identified
**And** a descriptive label cannot satisfy an exact digest or commit comparison.

**Given** production releases remain halted
**When** the reconciled sources describe HEXALITH_RELEASE_PUBLISH_ENABLED
**Then** they identify literal false as a deny-only emergency stop that cannot authorize publication
**And** they record that no bounded risk exception is currently approved.

**Given** existing planning, governance, and source-reconciliation checks can prove alignment
**When** this story is verified
**Then** those checks and exact source digests are reused
**And** no duplicate governance catalog, source mirror, or evidence packet is created.

### Story 17.2: [I · GOV-I] Publish and Exercise the Release Incident Runbook

As a Release Owner,
I want an approved incident runbook exercised through a focused tabletop,
So that partial publication or credential exposure can be contained without destroying evidence.

**Acceptance Criteria:**

**Given** a suspected credential exposure, unauthorized capability, package mismatch, or partial publication
**When** the runbook is followed
**Then** it assigns acknowledgement and containment targets plus stop/revoke, evidence preservation, external-effect inventory, credential rotation when plausible, recovery, communication, and re-enable actions
**And** it prefers unlisting and a new corrected version over rewriting an existing release.

**Given** the deny-only emergency stop and protected publisher controls
**When** containment begins
**Then** the authoritative stop mechanism is identified and publication authority is removed without executing candidate code
**And** immutable candidate, run, manifest, handoff, asset, and ledger evidence is preserved.

**Given** a dated tabletop scenario
**When** Release, Security, and the required operational participants execute it
**Then** one support-safe record captures timestamps, decisions, observed gaps, owners, and bounded follow-up items
**And** unresolved containment-critical findings keep GOV-I open.

**Given** existing incident, ledger, and release artifacts can record the exercise
**When** the tabletop result is retained
**Then** those mechanisms are referenced rather than wrapped in a second evidence system
**And** no live publication, real credential exposure, or package mutation is required.

**External dependency — [X · EXT-BUILDS-1]:** The Hexalith.Builds owner must publish and accept one immutable revision implementing the split-publication reusable contract in the approved lineage. FrontComposer may reconcile sources and perform other unblocked work, but Story 17.3 and caller activation remain blocked until this external revision and its owner acceptance exist.

### Story 17.3: [I · GOV-B] Select the Accepted Split Publication Topology

As a Release Owner,
I want the FrontComposer caller pinned to the accepted immutable two-job Builds revision,
So that candidate construction and protected publication execute under structurally separate authority.

**Acceptance Criteria:**

**Given** EXT-BUILDS-1 supplies an owner-accepted immutable split revision
**When** the FrontComposer release caller is updated
**Then** it pins that exact revision and selects the fixed build-publication-candidate and publish-publication-candidate topology
**And** no branch, tag, floating label, ambient checkout, or unaccepted revision can replace it.

**Given** release is dispatched
**When** the unprotected gate selects a candidate
**Then** it requires refs/heads/main, a lowercase 40-hex commit matching the live main ref, exactly one successful push CI run, and one successful quality run for that SHA
**And** it fetches only the authenticated run/attempt-named handoff through read-only APIs.

**Given** the new mode is not fully accepted or validation fails
**When** activation is evaluated
**Then** the caller remains on the halted deny-only posture and no product-publication side effect occurs
**And** delayed activation and rollback to the prior non-publishing state are tested.

**Given** missing, duplicated, truncated, paginated, malformed, or mismatched run data
**When** candidate selection runs
**Then** the gate fails before the protected publisher
**And** no later workflow-run value or default-branch helper can repair the failed selection.

### Story 17.4: [I · GOV-C] Remove Publication Authority from Candidate Execution

As a security-conscious Release Owner,
I want candidate/build execution to have no publication capability,
So that candidate-controlled code cannot obtain credentials or mutate release state.

**Acceptance Criteria:**

**Given** the build-publication-candidate job and every candidate-executing child process
**When** permissions, environment, credentials, tokens, and capabilities are inspected
**Then** they have no production environment, publication secret, write scope, OIDC/attestation authority, signing material, or equivalent ambient capability
**And** the job is limited to the minimum read-only operations required to build and validate the candidate.

**Given** hostile candidate code probes environment variables, token endpoints, filesystem mounts, workflow outputs, caches, artifacts, and network-accessible identity services
**When** the focused security fixtures run
**Then** no publication or attestation authority is available
**And** support-safe diagnostics contain no credential or internal-token content.

**Given** candidate validation succeeds
**When** builder-side manifests, plans, or classifications are produced
**Then** they remain diagnostic denial evidence only
**And** none can authorize publication or be treated as a publisher seal.

### Story 17.5: [I · GOV-D] Produce an Authenticated Run-Bound Publication Candidate

As a Release Owner,
I want one closed publication-candidate artifact bound to the selected run and exact bytes,
So that the protected publisher can authenticate data without rebuilding or executing candidate source.

**Acceptance Criteria:**

**Given** the authenticated candidate, CI handoff, active policy, and builder run/attempt
**When** the secretless builder prepares packages
**Then** it packs once and runs the existing inventory, package, consumer, checksum, SBOM, symbols, and early-denial checks against that one set
**And** it uploads exactly one publication-candidate-<run_id>-<run_attempt> artifact using the closed hexalith.publication-candidate.v1 descriptor.

**Given** the publication-candidate archive is downloaded
**When** its raw ZIP, descriptor, policy coordinates, evaluator identity, plan, inventory, and declared files are authenticated
**Then** every byte, path, run coordinate, and digest matches the selected handoff
**And** mutation, replay, extra/missing members, unsafe paths, duplicate names, or a different run/attempt fail closed.

**Given** dependency evidence is included
**When** hexalith.dependency-graph.v1 is verified
**Then** it contains the exact depth-1 root gitlinks and depth-2 direct gitlinks from selected commits under the active immutable policy
**And** collection uses committed objects without recursively initializing or executing nested submodules.

**Given** existing pack, inventory, consumer, graph, and checksum tooling supplies the required values
**When** this story is implemented
**Then** those outputs are bound into the candidate rather than regenerated in a parallel evidence pipeline
**And** the builder artifact remains evidence data, not authorization.

### Story 17.6: [I · GOV-E] Validate Handoff V3 and Typed Append-Only Attempt State

As a Release Owner,
I want every release attempt represented by a total authenticated handoff and typed append-only state,
So that failures, retries, deferrals, and partial effects cannot disappear or be relabelled.

**Acceptance Criteria:**

**Given** any authenticated Release run, including failure, cancellation, rejection, no-releasable, or publication paths
**When** the workflow completes or terminates
**Then** it uploads hexalith.release-verification-handoff.v3 under an always-run condition
**And** the handoff records selected quality/CI coordinates, candidate and policy identity, publication-candidate coordinates, release state, denial reason, evaluator, and available manifest/asset data.

**Given** a valid or malformed handoff
**When** the total classifier evaluates it
**Then** it assigns exactly one permitted state such as gate-frozen, no-releasable, rejected, compliant, deferred, missing-artifact, partial-publish, or other non-compliant
**And** a missing, duplicate, malformed, or unauthenticated artifact becomes missing-artifact rather than deferred.

**Given** publication is about to mutate NuGet or GitHub Release state
**When** the first product-publication action begins
**Then** publication_started is recorded immediately beforehand
**And** attestation registration or protected-job start alone does not set it.

**Given** a retry or later verification observes the same attempt
**When** ledger state is appended
**Then** the observation cannot delete, replace, weaken, or relabel an earlier incident
**And** the existing frontcomposer.release-ledger-record.v2 contract is extended rather than replaced.

### Story 17.7: [I · GOV-F] Publish with Candidate-Free Pinned Code

As a Release Owner,
I want only the protected candidate-free publisher to authorize and publish the authenticated package data,
So that candidate source never executes while publication authority is present.

**Acceptance Criteria:**

**Given** the protected publisher receives an authenticated publication-candidate archive
**When** it processes the candidate
**Then** it executes only active-policy-authorized owner-controlled pinned code, never checks out or executes candidate source, and treats all candidate files as non-executable data
**And** it independently validates the descriptor, policy, evaluator, inventory, plan, and every declared byte before extraction or use.

**Given** GitHub provenance attestation is supported or the approved unsupported fallback applies
**When** provenance is established
**Then** the publisher mints and verifies the attestation or validates the run-bound fallback against the same candidate, handoff, policy, run/attempt, and package digests
**And** attestation alone does not authorize publication.

**Given** manifest preparation completes
**When** hexalith.release-evidence.v4 is sealed and verified offline/live
**Then** it binds exact assets, inventory, consumers, symbols, SBOM, dependency graph/policy, selected runs, evaluator identities, and provenance result
**And** publish_authorized=true is required before the first NuGet, GitHub Release, tag, changelog, or equivalent product-publication side effect.

**Given** authorized candidate bytes are published
**When** GitHub and NuGet artifacts are compared
**Then** GitHub assets remain byte-identical and NuGet downloads may add only a valid root .signature.p7s while every other normalized ZIP member remains byte-equivalent
**And** rebuilding or repacking is never accepted as equivalent evidence.

### Story 17.8: [I · GOV-G] Pin Post-Release Verification and Preserve Incident Evidence

As a Release Owner,
I want post-release verification to use immutable owner-controlled code and durable evidence,
So that later branch changes cannot rewrite the truth of an earlier attempt.

**Acceptance Criteria:**

**Given** publication completes or partially starts
**When** independent post-release verification runs
**Then** it uses the exact active-policy-authorized evaluator closure and original authenticated candidate rather than ambient, candidate, or later-default-branch helpers
**And** it verifies GitHub assets, NuGet repository signatures, normalized package members, attempt disposition, and mandatory release assets.

**Given** a missing asset, signature failure, content mismatch, or external effect inconsistent with publication_started
**When** verification classifies the attempt
**Then** it appends an incident observation and preserves the failed state
**And** post-publication evidence cannot authorize retroactively or relabel the attempt green.

**Given** publication started without a complete immutable product Release
**When** incident recovery is required
**Then** only the separately authorized candidate-free recovery stage preserves authenticated quarantined evidence in the reserved namespace
**And** run artifacts remain supplemental replay material rather than durable authorization.

**Given** durable GitHub Release and ledger artifacts already hold the required evidence
**When** verification completes
**Then** those artifacts are referenced and appended
**And** no duplicate archive or secondary truth store is introduced.

### Story 17.9: [I · GOV-H] Reject Ambiguous Publication Asset Names

As a Release Owner,
I want duplicate or ambiguous destination asset names rejected before publication,
So that one manifest entry cannot overwrite or masquerade as another package or evidence asset.

**Acceptance Criteria:**

**Given** candidate packages, symbols, SBOM, manifests, or evidence files resolve to the same destination name
**When** the publication plan is validated
**Then** validation fails before attestation or product-publication side effects
**And** the diagnostic identifies the conflicting normalized destination without exposing unsafe paths or sensitive values.

**Given** names differ only through case, separator, normalization, escaping, or another destination-equivalent representation
**When** collision detection runs
**Then** they are treated as ambiguous under the destination's canonical comparison
**And** traversal, encoded aliasing, and duplicate JSON members cannot bypass the check.

**Given** all destination names are unique
**When** the plan is serialized and consumed
**Then** assets retain deterministic ordinal ordering and their sealed path/digest bindings
**And** existing inventory and manifest validation are extended rather than duplicated.

### Story 17.10: [A · GOV-J] Revalidate GOV-1 on One Unchanged Candidate

As the Architect, Release Owner, and Security reviewer,
I want deterministic checks and all required review lenses applied to one unchanged authenticated candidate,
So that publication conformance cannot be assembled from mixed revisions or selectively refreshed evidence.

**Acceptance Criteria:**

**Given** GOV-B through GOV-I are complete for one candidate and policy/evaluator set
**When** the canonical frontcomposer.gov1-split-conformance.v1 check runs
**Then** its source digests, authenticated runs, handoffs, candidate archive, manifest, incident result, and closure rows all bind the same immutable candidate
**And** stale spine hashes, mixed candidates, unavailable source evidence, or open rows fail the gate.

**Given** the deterministic result is available
**When** adversarial, edge-case, verification-gap, acceptance, and structure/prose reviews run
**Then** each review cites that same candidate and every finding receives a recorded disposition
**And** any unresolved Critical or High finding blocks acceptance.

**Given** the evidence candidate is submitted through the protected-main protocol
**When** the evidence and approval projection are reviewed
**Then** the evidence PR remains unchanged after Release Owner approval and a distinct approval-projection PR records the outcome
**And** direct push, squash, rebase, or candidate mutation fails the gate.

**Given** the focused checks and five reviews already produce their own durable outputs
**When** GOV-J is recorded
**Then** it cites those outputs rather than copying them into another review packet
**And** no separate sixth review or wrapper report is required.

### Story 17.11: [A · GOV-ACCEPT-1] Accept the Publication Boundary and Incident Posture

As the Product Owner, Release Owner, and Architect,
I want to accept or reject the adopted publication boundary and interim halt explicitly,
So that implementation does not infer authority from architecture prose or environment approval.

**Acceptance Criteria:**

**Given** AD-19, D-16, the legacy caller state, and the approved incident runbook
**When** the named owners review the posture
**Then** a dated decision addresses the secretless builder, candidate-free publisher, production halt, deny-only emergency stop, no-exception posture, and incident acknowledgement/containment target
**And** it states accept or reject for the exact source identities reviewed.

**Given** the posture is accepted
**When** governance evaluates GOV-ACCEPT-1
**Then** the approval satisfies only its named architecture/incident decision
**And** it does not activate the caller, authorize publication, replace EXT-BUILDS-1, or close implementation/evidence rows.

**Given** the posture is rejected or any owner receipt is absent
**When** G-8 is evaluated
**Then** the gate remains open and production remains halted
**And** any corrective work is created as a bounded residual without rewriting completed evidence.

### Story 17.12: [A · GOV-ACCEPT-2] Accept Reconciled Governance Sources

As the Product Owner and Release Owner,
I want to accept the exact reconciled governance-source digests,
So that future execution begins from one agreed publication-safety contract.

**Acceptance Criteria:**

**Given** GOV-SRC-1 has reconciled all canonical publication sources
**When** the owners review them
**Then** a dated decision cites each exact source path and digest and states accept or reject
**And** labels, summaries, or later branch content cannot substitute for the reviewed bytes.

**Given** the sources are accepted
**When** downstream GOV stories use them
**Then** every secretless-builder, candidate-free-publisher, exact-byte, policy, provenance, halt, incident, and append-only invariant remains intact
**And** acceptance cannot weaken a requirement to make existing implementation pass.

**Given** any source digest changes after acceptance
**When** governance re-evaluates the source set
**Then** GOV-ACCEPT-2 becomes stale and requires a new decision
**And** the prior decision remains immutable history.

### Story 17.13: [A · REL-LEDGER-1] Accept the Release-Ledger State Model

As the Release Owner and Architect,
I want to accept the typed release-ledger schema and total classifier,
So that every publication-capable attempt has one durable state model before historical rows are completed.

**Acceptance Criteria:**

**Given** the implemented handoff and frontcomposer.release-ledger-record.v2 behavior
**When** the schema and classifier are reviewed
**Then** the decision identifies their exact versions, evaluator identity, permitted states/transitions, immutable attempt key, append-only rule, and evidence required by each disposition
**And** every failed, cancelled, deferred, no-releasable, partial-publication, blocked, and successful path has a total outcome.

**Given** deferred-no-ci-handoff
**When** the accepted classifier evaluates it
**Then** it is valid only as the sole deferred sentinel and remains terminal, incident-bearing, and permanent
**And** missing, duplicated, malformed, or unauthenticated handoffs classify as missing-artifact instead.

**Given** an incident or partial publication has been recorded
**When** a rerun or later verification succeeds
**Then** the accepted transitions allow only an appended observation
**And** they forbid replacement, weakening, or relabelling of the incident.

**Given** the state model is accepted or rejected
**When** the decision is stored
**Then** it cites the existing schema, classifier tests, and exact implementation rather than copying them
**And** REL-LEDGER-2 remains blocked until acceptance exists.

### Story 17.14: [I · REL-LEDGER-2] Complete Evidence-Backed Release Ledger Rows

As a Release Owner,
I want every release from v4.1.1 onward represented by downloaded-byte evidence and a permanent disposition,
So that published tags cannot outrun or rewrite the release ledger.

**Acceptance Criteria:**

**Given** REL-LEDGER-1 is accepted and the v4.1.1 row already exists
**When** ledger reconciliation runs
**Then** v4.1.1 receives its pending owner hash/sign-off without replacing its earlier observations
**And** the row retains its original immutable attempt identity.

**Given** v4.2.0, v4.3.0, v4.4.0, v4.5.0, and any later release present at execution
**When** each release is reconciled
**Then** an append-only row or observation records downloaded GitHub/NuGet bytes, valid repository signature status, normalized non-signature member equivalence, external effects, and a classifier disposition
**And** a tag without sufficient evidence remains visibly missing or non-compliant rather than inferred green.

**Given** historical wording conflicts with verified current release truth
**When** a correction is necessary
**Then** a new observation explains the correction while preserving prior bytes and incident history
**And** no release is retroactively described as FR24-compliant without its required authorization evidence.

**Given** existing release downloads, verification commands, manifests, and ledger tooling can supply the result
**When** the rows are completed
**Then** those artifacts are referenced directly
**And** packages are not rebuilt, repacked, or copied into a new evidence archive.

### Story 17.15: [A · REL-A3-APP-1] Decide the Sample-Host Container Boundary

As the Release Owner,
I want to accept, revise, or reject the sample-host container assumption,
So that local end-to-end use cannot be mistaken for a published FrontComposer product image.

**Acceptance Criteria:**

**Given** the Hexalith.FrontComposer.UI sample host, AppHost, package inventory, and current product-form statement
**When** the Release Owner reviews assumption A3
**Then** a dated decision states accept, revise, or reject and identifies the exact sample-host/container boundary
**And** it records any impact on PRD §4, package inventory, public documentation, CI, and release policy.

**Given** local/e2e-only use is accepted
**When** release artifacts are classified
**Then** no FrontComposer-owned container image is published to a registry
**And** local SDK-built images remain non-product test assets.

**Given** the assumption is revised or rejected
**When** corrective work is required
**Then** a bounded residual updates the affected product and release contracts
**And** the approval decision itself does not silently mutate package or publication behavior.

### Story 17.16: [A · PRD-APP-1] Record Final Product Readiness

As the Product Owner,
I want one digest-bound decision over every readiness gate and prerequisite,
So that document approval, milestone status, and publication authorization cannot be confused.

**Acceptance Criteria:**

**Given** the exact PRD and addendum digests plus every Product-owned prerequisite across G-1 through G-8
**When** final readiness is reviewed
**Then** the dated record names each satisfied, rejected, and still-open gate and cites its minimum-sufficient immutable evidence or approval
**And** a missing external receipt, owner decision, focused proof, or unresolved Critical/High reviewer finding remains visibly open.

**Given** OI-4, OI-10, OI-16, OI-19, the required UX/documentation decisions, and the digest-bound reviewer gate are complete
**When** the Product Owner approves the exact document pair
**Then** product_approval may change to approved for those digests
**And** any later digest change requires a new decision.

**Given** every readiness gate is closed
**When** milestone status is evaluated
**Then** v1_readiness_milestone_reached may become true
**And** the milestone remains false while any gate is open, rejected, stale, or unsupported.

**Given** PRD-APP-1 is approved
**When** release execution is considered
**Then** the decision grants no publication permission and cannot replace publish_authorized=true, protected publisher controls, or release-owner authorization
**And** it cites existing gate artifacts rather than assembling a duplicate readiness evidence bundle.
