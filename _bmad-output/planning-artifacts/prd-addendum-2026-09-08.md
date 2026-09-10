---
title: Hexalith.FrontComposer PRD Addendum
prd: _bmad-output/planning-artifacts/prd.md
created: 2026-09-08
updated: 2026-09-09
---

# PRD Addendum: Mechanism, Rejected Alternatives, And Source Inventory

This addendum carries depth that belongs beside the PRD but not in it: release and dependency-governance mechanism, qualitative UX behavior, rejected alternatives, and the source inventory behind the 2026-09-08 and 2026-09-09 reconciliations. The PRD (`_bmad-output/planning-artifacts/prd.md`) states product outcomes and gates; `architecture.md` and `ux-design.md` remain the canonical technical and UX planning sources. The 2026-07-05 addendum (`prd-addendum-2026-07-05.md`) is retained as historical source inventory.

## 1. Release Evidence Mechanism (moved from FR-24 / NFR-12 / D-6 / D-11)

Current execution and adopted target architecture after the 2026-09-09 GOV-1 validation and AD-19 amendment:

- **Current caller.** `.github/workflows/release.yml` uses exact-SHA operator `workflow_dispatch` and protected production-environment approval, but leaves `governed-release` unset/false and therefore selects the Hexalith.Builds legacy reusable job. That path still consults the deny-by-default `HEXALITH_RELEASE_PUBLISH_ENABLED` repository variable. The 2026-09-08 statement that the variable was retired and the governed caller was active is superseded by the 2026-09-09 source/current-fit review.
- **Privilege boundary.** The current reusable job checks out and executes candidate-controlled restore/build/shell/Semantic Release code while holding write scopes and publication credentials. Approval is authorization, not isolation. Adopted AD-19 requires a secretless, read-only builder that produces one authenticated run-bound publication candidate and a distinct protected publisher/attester that treats packages as data, runs only pinned owner-controlled code, never checks out candidate source, and never gives candidate code publication secrets, write scopes, OIDC/attestation authority, or signing material.
- **Activation.** The split-phase Builds contract must be owner-accepted under the delayed-activation policy before the FrontComposer caller selects it. Flipping the current mode flag first is not acceptable. The adopted interim posture halts production releases and sets `HEXALITH_RELEASE_PUBLISH_ENABLED=false` as the deny-only emergency stop; no current bounded risk exception is authorized. Protected-environment approval cannot make a current attempt FR-24-compliant.
- **Signing.** Candidates are published unsigned by the author; NuGet.org repository-signs the upload. Author signing, a production PFX, and an RFC 3161 author timestamp were removed as release requirements on 2026-08-04 (REL-5).
- **Builder/publisher authority.** Builder-side readiness, manifest, check, or classification output is diagnostic early denial only and is never consumed as final authorization. The protected candidate-free publisher independently validates the artifact and descriptor, mints/verifies GitHub provenance attestation or the AD-9 approved fallback, prepares and seals the final manifest, executes the complete offline/live classification, and alone may require `publish_authorized=true` and perform side effects.
- **Manifest and canonical bytes.** `hexalith.release-evidence.v4` seals exact candidate assets, checksums, inventory, consumer validation, symbols, SBOM, complete depth-1/2 graph, immutable policy, selected quality run, authenticated coordinates, attestation/fallback, and evaluator identities. Candidate-derived GitHub assets are byte-identical to descriptor files. NuGet packages may differ only because repository signing adds root `.signature.p7s`; every other normalized ZIP member must be byte-equivalent. Every accepted scalar/value has one byte-unique representation with cross-language hostile/golden vectors. Legacy v1–v3 manifests are audit-only.
- **Evaluator identity.** Evaluator closures are authorized by the exact active base/before policy, never by self-recorded hashes or later ambient default-branch helpers. Release and post-release stages independently authenticate the original CI artifact, selected completed quality run, original candidate, policy projection, handoff bytes, and exact helper closure. Post-release verification is read-only and cannot authorize publication retroactively.
- **Attempt classification.** A total governed-attempt predicate maps every started attempt — success, block, failure, cancellation, deferral, no releasable artifact, and partial publication — to one disposition. `deferred-no-ci-handoff` is valid only as the sole deferred sentinel and is terminal, incident-bearing, and permanent; any malformed, duplicate, missing, or unauthenticated handoff maps to `missing-artifact`. Run uniqueness and conclusion projection are deterministic; every started publication is inspected for partial side effects.
- **Durable evidence.** A compliant product Release carries the mandatory publication-candidate archive, descriptor, CI handoff, final manifest, and attestation-or-fallback assets. If publication starts without a complete immutable product Release, the separately authorized incident-recovery stage preserves authenticated and quarantined bytes in an immutable reserved-namespace evidence prerelease before retry. Actions artifacts are short-retention replay material only.
- **Ledger state.** The target `frontcomposer.release-ledger-record.v2` is append-only and keyed by immutable Release-attempt identity; verification reruns append linked observations and never replace or weaken an incident. Its closed enums/nullability, total input-to-disposition mapping, nested environment-protection evidence, canonical CI-copy name, and duplicate-member/name rejection belong in the amended GOV-1 spine. v4.1.1 retains `fallback-approved`, published-byte verification, and pending owner sign-off. v4.2.0, v4.3.0, and v4.4.0 lack rows; backfill must preserve permanent incident truth and downloaded-byte evidence. v3.2.1, v3.2.2, v4.0.0, and v4.0.1 remain historical non-compliant attempts.
- **REL-AI-1 role.** Post-publication audit closure: the Release Owner confirms downloaded GitHub-asset hashes and the NuGet repository-signature/normalized-member equivalence result, then appends a signed observation. It does not repair a nonconforming privilege boundary or authorize publication retroactively.
- **Incident response.** `docs/release-incident-response.md` is the required D-16/OI-15 runbook. Before any retry, the runbook must require acknowledgement and recorded containment, disable the publish gate, preserve immutable evidence, rotate possibly exposed credentials, and inventory every external effect. It must prefer unlisting and permit correction only under a new version. Re-enablement requires documented cause and remediation, completed rotation, complete external-effect inventory, independent verification, and Release Owner approval.

## 2. Dependency Governance Mechanism (moved from NFR-13 / §7 / D-11)

- **Compatibility vs provenance.** Compatibility is established from versioned semantic shared-catalog profiles and affected-module standalone Release/NuGet restore/build evidence. Exact commits and raw catalog fingerprints are sealed as provenance only and never become compatibility allowlists.
- **Graph definition.** `hexalith.dependency-graph.v1` contains every gitlink at the explicit FrontComposer commit (depth 1) and every gitlink in each exact root-selected repository commit (depth 2), with no deeper edges. Collection reads exact committed objects under the immutable base/before policy, emits deterministic graph diffs, and never recursively initializes nested submodules.
- **Ceilings (fail closed above).** 4,096 edges; 64 MiB raw `ls-tree` output per owner commit; 1 MiB per committed `.gitmodules` blob; 4 MiB per catalog blob. A materialized Builds contract tree permits only bounded regular files: at most 16,384 files, 16 MiB per blob, 256 MiB total; unsafe paths/modes and exceeded limits fail before extraction.
- **Policy activation.** `eng/dependency-graph-policy.json` is the sole executable authority for profile values, dispositions, limits, evaluator authorizations, Release Owner logins, and attestation capability. PR evaluation uses the exact base-commit policy; push evaluation uses the exact non-zero before-commit policy. Candidate policy or workflow changes cannot authorize themselves and activate only from a later base. The one-time v1 bootstrap was consumed on 2026-07-19 and no bootstrap path remains. Missing or unknown mappings, profiles, commands, objects, selected catalogs, or evaluator closures fail closed.
- **Upstream state.** Hexalith.Builds issue 17 was closed 2026-07-20 without the GOV-1 amendment, then reopened 2026-08-08 with the complete amendment and owner-accepted immutable lineage anchor/predecessor `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` (`g2-hexalith-builds-inline-pre-publish-gate-request.md`). AD-19 requires a later owner-accepted revision that implements the split reusable before FrontComposer selects it.
- **Builds identity table (one SHA per purpose, reconciled 2026-09-09).**

  | Purpose | SHA | Source |
  | --- | --- | --- |
  | Owner-accepted BUILD-REL-1 lineage anchor/predecessor | `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` | g2 request body, 2026-08-08; also `buildsCatalogGitlink` in the EventStore identity contract; not the future split-reusable revision |
  | Execution pin (reusable workflow + `BUILDS_EXECUTION_SHA`) | `4eb33928a1d8c7775f97221cf9edc171db0cb5f8` | `.github/workflows/release.yml` lines 17, 233, 286, 321, 329; advanced from `3f0e3595…` (`spec-align-latest-hexalith-modules-and-simplify-ci.md`, 2026-08-11) |
  | Current gitlink `references/Hexalith.Builds` | `a32cb422749352cce8dec948aa3e78c8f00eb4cf` | Root `HEAD` on 2026-09-09 |
  | Prior evidence gitlink | `35c3d1e5b8a55a74a440b9c2cad4c5e18747b241` | 2026-09-08 identity contract and captured Pact evidence; historical provenance, not current state |

- **What G-2 and G-8 still need.** The graph/catalog core remains implemented, and the 2026-09-09 source reconciliation now projects AD-19, handoff v3, manifest v4, run-bound fallback, durable incident evidence, and the split implementation gate. It does not close Product/Release acceptance, the owner-accepted Builds revision, caller/implementation convergence, the register closure bundles, authenticated proof, incident readiness, or the deterministic plus five-lens revalidation. Missing proof cannot be declared equivalent to proof.
- **Eight implementation closure bundles.** G-2/G-8 require authenticated closure of: (1) the owner-accepted split reusable contract and delayed activation, (2) caller switch and exact two-job topology, (3) removal of the production environment from candidate/build jobs, (4) publication-candidate production, authentication, and hostile-candidate fixtures, (5) handoff-v3 and ledger migration, (6) candidate-free final classification/publication, (7) a pinned post-release helper without ambient or candidate helpers, and (8) duplicate destination asset-name rejection.

## 3. FC-NIP Composition Invariants (supporting FR-13 / FR-26)

- One terminal producer boundary: every generated-command terminal path routes through `IPendingCommandOutcomeResolver`; early callbacks are buffered until pending registration is durable.
- Target identity: `[CommandTarget]` on the command type plus a typed `ICommandTargetIdentityProvider<TCommand>` or `CommandTargetResolutionMode.SameAsSource`; an immutable snapshot is captured before dispatch. Standalone create, cross-row, delete/no-op, and status-move commands are covered by the successor contract.
- Materiality: `Material` / `NoOp` / `Unknown` is classified independently from identity; `Unknown` in either dimension suppresses publication.
- Scope and atomicity: indicator mutations are observable by generated grids; tenant/user clearing is independent of the next add; publication is atomic first-wins per `ViewKey` and `EntityKey` across distinct message IDs.
- Telemetry: EventId 5912 `CommandFormTargetResolutionFailed` (Warning, closed failure category only) and 5913 `CommandFormTargetResolutionSucceeded` (Information, payload-free); suppression rate documented in `docs/reference/components/datagrid.md`.
- Evidence: `tests/9-8-live-acceptance.md` — strict live acceptance on clean candidate `7a5737630611b4d54b0180a3fa4c9c4ccd23a28c` at `2026-08-27T22:35:38Z`, browser result 1 passed.
- Residuals: DW-679 (server-allocated keys need a typed post-dispatch identity proof); Story 9.8 deferred items (serialized AppHost fallback should rebuild the complete candidate dependency graph; structured fallback build evidence; serialized-build-log redaction).
- Diagnostic ID note: the successor contract instructed allocation of the next free build-time ID (HFC1071). Delivery reused HFC1005 (invalid attribute argument) for invalid `[CommandTarget]` declarations; the PRD records the shipped behavior rather than the contract's instruction.

## 4. Qualitative UX Rules (supporting FR-8–FR-16, FR-22, FR-23, NFR-3, and SM-6)

Quoted from `ux-design.md`, `ux-experience-2026-07-05.md`, and `fc-ia-1-module-tab-ia-decision-2026-07-05.md`:

- Default tab: "Default tab name = the module plural label (e.g. `Parties`), falling back to `Overview`."
- Module root: "The default tab route may render at the module root `/{module}` (equivalent to `/{module}/{default}`)."
- Flyout target: "Rail projection-flyout links resolve to `/{module}/{tab}` inside the module workspace; never a new top-level entry."
- Grid density: "Compact projection grids use the exact `32px` row metric from `DataGridDensityMetrics`."
- Hamburger: "The hamburger toggle is always visible; desktop toggles labeled and icon-only rail modes."
- Live regions: "use live regions only where the state change is useful and non-noisy."
- Search shortcut: "`/` may focus page search when that shortcut is enabled."
- Chrome: "Lifted from .NET Aspire Dashboard: neutral chrome, accent as thread, compact data density, sticky grid headers, toolbar/search discipline, and lightweight status icons."
- Status slots (UX-DR2, amended 2026-06-25): status members render as colored Fluent icons with the label on hover and keyboard focus via `FluentTooltip` plus an always-present `aria-label`; numeric counts keep the `FluentBadge` pill.
- Urgency ordering (`FcHomeDirectory.razor`): `OrderByDescending(IsReady).ThenByDescending(AggregateCount).ThenBy(Manifest.Name, Ordinal)`.
- Reference-module journeys: FC-IA-1 names Tenants the provisional first reference module and uses a Parties "Search" tab as the worked example of a non-default Module Tab; the PRD names Tenants as the obligated adopter (G-6) and keeps Parties as the D-7 fallback candidate.
- Residual DW closures on 2026-08-27: DW-672 (guard file/class names renamed around command-target identity, commit `d928f5f7`) and DW-678 (one command resolves one FC-NIP target, recorded as intentional) are closed; only DW-679 stays open.

### 4.1 State Announcement Contract (2026-09-09 validation delta)

| Transition family | Accessible outcome | Noise control |
| --- | --- | --- |
| Loading, stale, reconnecting, fallback, recovery | Meaningful state transition through a polite status channel | Announce once per surface/state; suppress polling and retry ticks; coalesce rapid intermediate progress. |
| Command Submitting, Acknowledged, Syncing, terminal/degraded | Progress through a polite status channel; one terminal outcome | Deduplicate by operation and lifecycle state; transport acceptance never announces Confirmed. |
| Client validation or submit-blocking rejection | Focused error summary/alert, linked to invalid controls where safe | Preserve useful input; distinguish client validation from asynchronous server rejection. |
| Blocked second submit | Localized accessible explanation while the original command remains active | Announce once per blocked attempt; never imply the second command was queued. |
| Fresh-row appearance and expiry | Appearance announced once per tenant/user/entity transition; status remains non-color | Expiry is silent; forced-colors and reduced-motion preserve meaning. |

The exact wording, live-region implementation, deduplication keys, and coalescing windows remain owned by UX and implementation. Evidence must prove the observable outcomes above without exposing support-sensitive values.

### 4.2 Focus, Validation, And Recovery Contract

- Successful client-side route activation or palette navigation places focus on the route-level `h1`; failed navigation leaves focus in a usable location and announces the failure.
- Keyboard tab selection retains focus on the active tab while the associated labelled tabpanel changes.
- Closing a palette or dialog returns focus to its invoker.
- Generated forms associate field errors with Fluent inputs, expose a summary linked to invalid controls, focus that summary after failed submit, preserve useful input, and support first-invalid navigation.
- Server rejection stays a lifecycle outcome unless a safe field mapping exists. Keyboard-only recovery is required for validation, rejection, close/cancel, and blocked-submit paths.

### 4.3 State-By-Surface And Responsive Evidence

The UX source must maintain a state-by-surface acceptance matrix covering every FR-11 projection state, every FR-15 command state, blocked submit, and no-tenant/no-access outcomes. Each row records entry evidence, visible meaning, permitted actions, recovery/timeout, announcement, and terminal/non-terminal classification.

Changed UI surfaces carry evidence for 320 CSS-pixel reflow, 400% zoom, resilient text spacing, WCAG 2.2 AA target size subject to standard exceptions, unobscured focus, light/dark/forced-colors, and reduced motion. Navigation-current, focus, status, stale/reconnect, lifecycle, and fresh-row meaning cannot rely on animation or color alone.

### 4.4 UX Source And Component Integrity

- `ux-design.md` remains canonical; its visual and experience supplements must cite every applicable UJ-1…UJ-6 or state why a journey is out of scope.
- Both supplements must replace the dead `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md` path with `_bmad-output/planning-artifacts/prd.md` and carry a reconciliation date/revision marker.
- Published/planning docs use exact public FrontComposer identifiers or Fluent APIs available in the selected catalog. `FcPageToolbar` is the product contract; its current internal composition (`FluentStack role="toolbar"` plus `FluentTextInput` configured for search) is not a separately promised public component set.
- `--fc-color-accent`, when documented, is only an alias of the active Fluent V5 accent role; it never creates an independent seed or palette. Full contrast/forced-colors fallback tables remain in UX sources.
- Replace vague navigation primitive names with the exact `FrontComposerNavigation` contract. The settled FC-IA-1 chronology belongs in reconciliation history, not the active behavior contract.

### 4.5 FR-3 Vocabulary-To-Behavior Matrix v1

| Attribute family | Observable generated consequence | Acceptance evidence |
| --- | --- | --- |
| Projection role and projection template | Selects the registered view shape and its Loading/Empty/Data rendering or the declared template boundary. | Generator snapshot plus role/template registration test. |
| Bounded context | Selects Module identity, registration boundary, labels, and canonical route segments. | Manifest and route snapshots. |
| Badge, icon, relative time, currency, display metadata | Produces localized semantic rendering with accessible text; state meaning is not color-only. | Renderer snapshots plus accessibility assertions. |
| Column priority | Determines deterministic responsive retention/order without losing access to omitted data. | Grid snapshot and reflow/keyboard test. |
| Field group | Preserves declared field grouping/order and accessible group identity in generated forms. | Form snapshot and accessible-name/relationship test. |
| Default and derived/server-controlled field | Applies the declared initial value; derived/server-controlled values do not render as editable input and are injected or computed at the owned boundary. | Parser/emitter tests plus command-input rejection tests. |
| Empty-state CTA | Renders the declared authorized action with a valid target, or no action when unavailable/unauthorized. | Empty-state authorization and navigation tests. |
| Destructive confirmation and required policy | Requires confirmation and evaluates the named policy at the FR-16 boundaries before dispatch. | Authorization/confirmation tests. |
| Command target | Resolves the immutable pre-dispatch identity used only by FR-13; invalid declarations emit the registry-owned diagnostic. | Command-target generator/resolver tests and FC-NIP live proof. |

`docs/diagnostics/diagnostic-registry.json` remains the executable diagnostic authority. A vocabulary change is incomplete until this matrix, the registry, generated snapshots, and public docs agree.

## 5. MCP Contract Detail (supporting FR-17–FR-19a)

- Contracts: `fc-mcp-fail-closed-security-contract-2026-06-05.md`, `fc-mcp-schema-fingerprint-negotiation-2026-06-05.md`, `fc-mcp-lifecycle-subscription-contract-2026-06-05.md`, `fc-mcp-resources-contract-2026-06-05.md`.
- Token spelling: contract prose sometimes writes "unknown-tool"; the wire category is `unknown_tool` (underscore). Resource token is `unknown_resource`. Generic text is `Request failed.`.
- Full `unknown_tool` structured shape (`BuildUnknownToolStructuredContent`): `category: "unknown_tool"`, `docsCode: "HFC-MCP-UNKNOWN-TOOL"`, `suggestion` (nearest visible name or null), `visibleTools` (the caller's own visible catalog), optional `continuation: "visible-list-truncated"`. Hidden and absent are byte-identical only for the same caller, because `visibleTools` is caller-specific.
- `tools/list` success path returns `[.. catalog.Tools, ToLifecycleTool(options)]`; the lifecycle tool is appended unconditionally for authenticated callers, so an empty list is distinguishable from an authenticated empty catalog (credential-validity signal, disclosed in FR-19).
- `resources/read` for a URI matching no registered `FrontComposerMcpResource.IsMatch` (exact string) never reaches `FrontComposerMcpProjectionReader`; the ModelContextProtocol SDK answers with its default not-found error. Every existing `unknown_resource` test calls the reader directly. The G-7 suite must exercise SDK dispatch.
- `MapFrontComposerMcp` returns `endpoints.MapMcp(route)` without `RequireAuthorization()`. Under FR-19, adopters must place the MCP endpoint behind host authentication; G-7 verifies this requirement.
- Startup messages name the missing gate interface and point to `AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>()` / `AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>()` "explicitly for sample/dev hosts". No analyzer or startup check currently forbids those registrations in production (PRD OI-3).
- `resources/list` registers projection and skill resources statically into the `ResourceCollection` (URI, name, title, description); only `tools/list` has an empty-list fail-closed behavior. Descriptors are generated from domain types and are identical for every tenant. The PRD discloses this behavior in FR-19 and requires security sign-off (OI-2) before the behavior can be classified as accepted. OI-2 names the alternative—owning the list/read handlers and filtering by tenant—as a story option.
- Existing audit-class tests: `ToolAdmissionTests`, `ToolAdmissionSpecGapTests`, `CommandInvokerCoverageTests`, `CommandLifecycleTests`, `McpCommandToolAdapterTests`, `ProjectionReaderTaxonomyTests`, `ProjectionReaderSchemaGateTests`, `ProjectionReaderSchemaTaxonomyTests`, `SchemaNegotiationPrecedenceMatrixTests`, `SkillResourceTests`, `FailClosedLoggingGovernanceTests`, `AuthContextAccessorTests` (Mcp.Tests); `AuthRedactionStressTests` (Shell.Tests); `DriftDiagnosticRedactionTests` (SourceTools.Tests); `EventStorePactContractTests` redaction scan.

## 6. Rejected Alternatives (through 2026-09-09)

| Alternative | Rejected because |
| --- | --- |
| Keep author signing and RFC 3161 timestamps as FR-24 requirements | REL-5 dropped them 2026-08-04; architecture and the ledger describe unsigned candidates with NuGet.org repository signing. Restoring them would require a dated new decision, not PRD inertia. |
| Define a separate non-customer "gated evidence release" to close REL-AI-1 | Unnecessary: v4.1.1 is already published with `publish_authorized=true` and downloaded-byte evidence; REL-AI-1 only lacks an append-only owner observation. This does not claim G-8 conformance. |
| Drop REL-AI-1 from the milestone | It is the post-publication audit that proves downloaded GitHub assets match the manifest-bound candidates and the NuGet download satisfies the sealed repository-signature/normalized-member equivalence rule; the ledger is the artifact G-1 and SM-2 read. It is neither publication authorization nor repair of the G-8 privilege boundary. |
| Roll the EventStore identity back to `3.91.1` to match the approved tuple | Two gitlinks and twelve minor versions of Pact rework for no consumer benefit; live provider evidence already passes at `3.103.0`. The remaining work is current-Builds provenance reconciliation plus approval (D-12/G-3). |
| Treat OI-3 (allow-all ban enforcement) as a regression baseline | Undelivered enforcement is not a baseline; it is gated by G-7. |
| Keep "Default: accepted" on approval-type open items | A gate with a default of pass is a checkbox; approval gates now cite the artifact the approver read and carry no default. |
| Keep Stories 11.20–11.23 as publication gates with re-dated critical path | All four phases are done; 11.23 landed 2026-08-08. |
| Restate v1.0 as a developer-framework-only release and drop operator freshness | FC-NIP composition landed with live proof; both theses are now evidenced, so the PRD keeps both with operator SMs (SM-7, SM-8). |
| Allocate HFC1071 in the PRD for invalid `[CommandTarget]` | Code reuses HFC1005; the PRD records shipped behavior. A new ID would be a change request, not documentation. |
| Treat the `resources/list` catalog and `tools/list` credential oracle as already accepted | OI-2 always requires an independent dated security disposition of every residual behavior after the final implementation. Gated-list work may change or eliminate a residual, but cannot replace that independent acceptance; observed behavior is not acceptance. |
| Flip `governed-release` before accepting the split-phase privilege boundary | The existing modes execute candidate-controlled code with publication authority. D-16/G-8 require architecture and owner acceptance before activation. |
| Renumber FRs to remove FR-27/FR-28 from the active list | `epics.md` FR coverage map depends on stable IDs; they are demoted to closed decision records (§5.0 Table C) instead. |
| Keep a second "run copy" of the PRD in the BMad run folder | Dual canonical copies caused the dead-path finding; one canonical file plus archived immutable snapshots. |

## 7. Source Inventory Through The 2026-09-09 Update

Change signal: `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-09-08/validation-report.md` and its reviewer files (`review-rubric.md`, `review-adversarial.md`, `review-source-reconciliation.md`).

The 2026-09-09 update additionally reconciles:

- PRD validation and source-current-fit: `prds/prd-frontcomposer-2026-09-08/validation-report.md`, `review-rubric-2026-09-09.md`, and `review-source-consistency-2026-09-09.md`.
- UX validation: `validation-report.md`, `review-rubric.md`, `review-accessibility.md`, and `review-fluent-ui-v5.md`, plus `ux-design.md`, `ux-design-detailed-2026-07-05.md`, and `ux-experience-2026-07-05.md`.
- Architecture validation: `architecture/architecture-gov-1-2026-07-19/validation-report-2026-09-09.md`, `ARCHITECTURE-SPINE.md`, its rubric/current-fit/adversarial/code-drift/security reviews, the nonconformance register, and canonical `architecture.md`.
- Reconciliation extracts in the PRD run folder: `reconcile-prd-validation-2026-09-09.md`, `reconcile-architecture-2026-09-09.md`, and `reconcile-ux-2026-09-09.md`.

Delivery truth verified (read-only):

- `_bmad-output/implementation-artifacts/sprint-status.yaml` (last_updated 2026-08-29) and story specs 9.3–9.8, 11.2, 11.3, 11.23, 11.24, REL-3/4/5, GOV-1, `rel-ai-1-release-evidence-ledger.md`, `deferred-work.md`, `tests/9-8-live-acceptance.md`, `spec-pact-provider-reconciliation*.md`, `spec-low-logging-governance-hardening.md`, `spec-make-release-compatibility-gates-enforcing.md`.
- `_bmad-output/planning-artifacts/architecture.md` (updated 2026-09-08), `epics.md`, `ux-design.md`, `ux-experience-2026-07-05.md`, `g2-hexalith-builds-inline-pre-publish-gate-request.md`.
- `_bmad-output/contracts/`: FC-NIP base and successor, FC-IA-1, MCP fail-closed / schema / lifecycle / resources, shared-catalog governance, HFCM9002 decision, `frontcomposer-eventstore-approved-runtime-identity-v1.json`.
- Repository: `eng/release-package-inventory.json`, `eng/dependency-graph-policy.json`, `Directory.Build.props`, `global.json`, `.github/workflows/release.yml`, `src/**/PublicAPI*.txt`, `FcDiagnosticIds.cs`, `CommandParser.cs`, `CommandFormEmitter.cs`, `FcShellOptions.cs`, `ProjectionHubRetryPolicy.cs`, MCP admission/negotiation sources; `references/Hexalith.Builds/Props/Directory.Packages.props` and `.github/workflows/domain-release.yml` (read only).

## 8. Validation Finding → Change Map

| Finding (2026-09-08 report) | Change |
| --- | --- |
| Status launders unfinished release | Frontmatter split (`product_approval`, `v1_readiness_milestone_reached`) and §0.1 separated milestone gates from publication. The 2026-09-09 reconciliation further corrected the former "4.x is governed" claim: current execution is legacy and no release is FR-24-compliant before G-8. |
| REL-AI-1 circular | FR-24 redefines REL-AI-1 as post-publication audit closure; G-1 requires dispositions for the three tags cut around the ledger. |
| MCP fail-closed under-specified | FR-19 table with an exact per-class guarantee (tools vs resources, catalog disclosure, SDK not-found, credential signal); FR-19a; D-14; SM-4 audit as gate G-7; OI-2 sign-off by a non-author; OI-3 story. |
| Post-update adversarial/source-fit findings (2026-09-08) | G-2 was rewritten to the then-current Builds SHAs; D-12/G-3 recorded the then-current view that approval was the only remaining act and dropped rollback; freeze wording was corrected to REL-5; the Roslyn literal was dropped; 11.10 non-existence and `epic-11: in-progress` were recorded; v3.2.1 and live Pact evidence for 3.103.0 were cited; default acceptance was removed; SM-10 and SM-C5 were added; SM-C1/C3 were retired; and the planning/lifecycle glossary was added. The 2026-09-09 Builds-provenance row below supersedes the approval-only view. |
| 11.20–11.23 overdue gate | Recorded delivered; D-10 closed; dates removed. |
| Epic 9 stories treated as open | FR-13/FR-26 complete; D-4 amended; G-5/OI-1. |
| Signed v2 model stale | FR-24/NFR-12/SM-2/D-6 retain unsigned candidates and NuGet repository signing; the 2026-09-09 AD-19 advance now targets manifest v4 and split publication. |
| Open-question closure hides tensions | §12.1 three lists; §12.2 open items. |
| SMs miss remaining bets | SM-7/8/9 added; SM-C4; states recorded per SM. |
| FR-29 program envelope | FR-29.1–29.7 rows mapped to H-ids and stories. |
| Projection bounds missing | FR-12/NFR-8 numbers from Story 11.2 options and retry policy. |
| Story 11.24 invisible | FR-29.7, D-12, G-3. |
| Bundled status map | §5.0 Tables A/B/C. |
| No public-surface inventory | §11 package table + identifiers; UI/AppHost resolved (A3). |
| Operator vision vs CI scoreboard | §1 dual thesis; operator SMs. |
| D-4 resolved vs open | D-4 amended to decision recorded + composition delivered. |
| A1/A2 as law | A1 → FR-22 text; A2 → §4 with Tenants named; both retired. |
| D-1 dead path | D-1 amended; single canonical. |
| Issue 17 status stale | D-11 corrected; OI-7. |
| REL-4 freeze model stale | FR-24 preserves exact-SHA operator dispatch and the deny-only gate, while D-16/G-8 supersede the same-job publication mechanism with split publication and the current halt posture. |
| EventStore identity / Pact | D-12; NFR-11 live Pact lane; G-3. |
| Medium/low: HFCM9002 row, catalog FR specifics, IA nouns, lifecycle vocabulary, Fluent pin, generated-path window, "sensitive cases", addendum missing | D-15; FR-1/FR-9/FR-11 specifics; glossary + UJ-2/UJ-3 rewrites; §7 catalog reference + D-13; §11 path window; FR-19 table replaces vague words; this addendum. |
| 2026-09-09 active legacy caller / candidate privilege | §0, FR-24, NFR-12, D-6, D-16, G-8 and Addendum §1 distinguish current execution from the target split-phase boundary. |
| 2026-09-09 GOV-1 convergence and implementation failure | G-2/G-8, SM-2a/SM-9, OI-13/OI-14/OI-17, and Addendum §2 reopen engineering, evidence, and review. |
| 2026-09-09 ledger/evaluator/canonicalization findings | G-1/G-8, FR-24, NFR-7/NFR-12, SM-2/SM-C4/SM-C5, OI-8/OI-15, and Addendum §1 define append-only attempt truth and exact evaluator identity. |
| 2026-09-09 Builds provenance advance | G-2/G-3, D-11/D-12/D-13, and Addendum §2 preserve `35c3d1e5…` as evidence provenance while recording current `a32cb422…`. |
| 2026-09-09 UX validation | FR-8/FR-10–FR-16/FR-22/FR-23, NFR-3/NFR-11, SM-5/SM-6/SM-7/SM-8/SM-10, D-8, and OI-16 define measurable state, focus, validation, announcement, source, and component integrity; Addendum §4 carries matrix schemas and obligations, while OI-16 requires the complete canonical UX matrices. |
| 2026-09-09 postfix reviewer gate | G-4 now requires a digest-bound synthesized report; G-8 requires OI-13's four-source architecture convergence; OI-2 remains independently mandatory after any gated-list work; `a8a50859…` is a lineage anchor rather than the future split revision; and OI-19 owns component/migration parity enforcement that current FR-23 tests do not yet prove. |
