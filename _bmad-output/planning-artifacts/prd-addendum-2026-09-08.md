---
title: Hexalith.FrontComposer PRD Addendum
prd: _bmad-output/planning-artifacts/prd.md
created: 2026-09-08
updated: 2026-09-08
---

# PRD Addendum: Mechanism, Rejected Alternatives, And Source Inventory

This addendum carries depth that belongs beside the PRD but not in it: release and dependency-governance mechanism, qualitative UX rules, rejected alternatives, and the source inventory behind the 2026-09-08 reconciliation. The PRD (`_bmad-output/planning-artifacts/prd.md`) states product outcomes and gates; `architecture.md` and `ux-design.md` remain the canonical technical and UX planning sources. The 2026-07-05 addendum (`prd-addendum-2026-07-05.md`) is retained as historical source inventory.

## 1. Release Evidence Mechanism (moved from FR-24 / NFR-12 / D-6 / D-11)

Delivery model as of `architecture.md` (updated 2026-08-16) and REL-3/REL-4/REL-5 (done):

- **Trigger.** Publication runs only through exact-SHA `workflow_dispatch` on the Hexalith.Builds reusable `domain-release.yml`, after a successful exact-source main-push CI run and protected production-environment approval — that approval is the human pre-publication control. The transitional auto-`workflow_run` path, FrontComposer's caller-side `freeze-guard` (REL-4), and the `HEXALITH_RELEASE_PUBLISH_ENABLED` variable path are all retired (REL-5, done 2026-08-14: "There is exactly one publication path, and it is the operator-dispatched protected production job"). REL-4's freeze wording survives in `deployment-guide.md` and should be reconciled.
- **Signing.** Candidates are published unsigned by the author; NuGet.org repository-signs the upload. Author signing, a production PFX, and an RFC 3161 author timestamp were removed as release requirements on 2026-08-04 (REL-5).
- **Manifest.** `hexalith.release-evidence.v3` seals the exact candidate bytes, checksums, package inventory, consumer-validation results, symbols, SBOM, the complete depth-1/2 `hexalith.dependency-graph.v1`, the immutable dependency-policy coordinates and digest, and canonical authenticated CI/release workflow provenance. Legacy v1/v2 manifests are audit-only and never publishable or fallback-eligible.
- **Evaluator identity.** Evaluator closures are authorized by the active base/before policy, never by self-recorded hashes. Every Release attempt emits a versioned verification handoff carrying the authenticated CI run/attempt/raw handoff hash, exact policy projection, original candidate, and exact manifest/assets. The post-release verifier re-authenticates both handoffs and their policy/candidate agreement even on pre-manifest failure and never treats its second-hop/default-branch SHA as the published candidate.
- **Classification.** `classify-release --require-publishable` must yield `publish_authorized=true`; `classification` may be `ready` or `fallback-approved` (the fallback digest binds graph, policy, and canonical workflow-definition digest). `blocked` or `publish_authorized=false` fails before any NuGet or GitHub side effect.
- **Durable evidence.** Attached during GitHub Release creation or retained in an approved equivalent; a 30-day Actions artifact is supplemental.
- **Ledger state (2026-08-14).** v4.1.1: v3 manifest, `fallback-approved`, `publish_authorized=true`, published-byte-verified, pending Release Owner FR-24 sign-off. v4.1.0: same model, superseded by v4.1.1 after a failed Release conclusion. v3.2.1, v3.2.2, v4.0.0, v4.0.1: historical non-compliant (invalid manifests or rebuilt bytes). Tags v4.2.0 (2026-08-30), v4.3.0 (2026-09-05), v4.4.0 (2026-09-07) exist without ledger rows, and were cut while the ledger and `sprint-status.yaml` still said "further publication remains unauthorized" (a sentence that predates REL-5's model); each needs a retroactive row plus a disposition (PRD G-1 / OI-8). `spec-actions-33264036185-33264035739-fix-cicd-release.md` (done, 2026-08-30) records v4.2.0 as a governed release following "both normal approvals".
- **REL-AI-1 role.** Post-publication audit closure: the Release Owner confirms the downloaded published bytes against the manifest and signs the ledger row. It is not a publication control; the protected-environment approval is.

## 2. Dependency Governance Mechanism (moved from NFR-13 / §7 / D-11)

- **Compatibility vs provenance.** Compatibility is established from versioned semantic shared-catalog profiles and affected-module standalone Release/NuGet restore/build evidence. Exact commits and raw catalog fingerprints are sealed as provenance only and never become compatibility allowlists.
- **Graph definition.** `hexalith.dependency-graph.v1` contains every gitlink at the explicit FrontComposer commit (depth 1) and every gitlink in each exact root-selected repository commit (depth 2), with no deeper edges. Collection reads exact committed objects under the immutable base/before policy, emits deterministic graph diffs, and never recursively initializes nested submodules.
- **Ceilings (fail closed above).** 4,096 edges; 64 MiB raw `ls-tree` output per owner commit; 1 MiB per committed `.gitmodules` blob; 4 MiB per catalog blob. A materialized Builds contract tree permits only bounded regular files: at most 16,384 files, 16 MiB per blob, 256 MiB total; unsafe paths/modes and exceeded limits fail before extraction.
- **Policy activation.** `eng/dependency-graph-policy.json` is the sole executable authority for profile values, dispositions, limits, and evaluator authorizations. PR evaluation uses the exact base-commit policy; push evaluation uses the exact non-zero before-commit policy. Candidate policy or workflow changes cannot authorize themselves and activate only from a later base, apart from the one-time frozen, digest-approved bootstrap. Missing or unknown mappings, profiles, commands, objects, selected catalogs, or evaluator closures fail closed.
- **Upstream state.** Hexalith.Builds issue 17 was closed 2026-07-20 without the GOV-1 amendment, then reopened 2026-08-08 with the complete amendment and the owner-accepted immutable revision `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` (`g2-hexalith-builds-inline-pre-publish-gate-request.md`).
- **Builds identity table (one SHA per purpose, 2026-09-08).**

  | Purpose | SHA | Source |
  | --- | --- | --- |
  | Owner-accepted BUILD-REL-1 revision | `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` | g2 request body, 2026-08-08; also `buildsCatalogGitlink` in the EventStore identity contract |
  | Execution pin (reusable workflow + `BUILDS_EXECUTION_SHA`) | `4eb33928a1d8c7775f97221cf9edc171db0cb5f8` | `.github/workflows/release.yml` lines 17, 233, 286, 321, 329; advanced from `3f0e3595…` (`spec-align-latest-hexalith-modules-and-simplify-ci.md`, 2026-08-11) |
  | Gitlink `references/Hexalith.Builds` | `35c3d1e5b8a55a74a440b9c2cad4c5e18747b241` | `git ls-tree HEAD`; also `currentCompatibility.buildsCatalogSha` in the identity contract |

- **What G-2 still needs.** Not code: `evaluator_authorizations` in `eng/dependency-graph-policy.json` already holds 7 `ci`, 7 `release`, and 5 `post_release` entries (including both `a8a50859…` and `4eb33928…`). The GOV-1 story body ("`evaluator_authorizations` remains empty", dated 2026-08-08) is stale relative to the policy file; derive remaining work from `release.yml` and the policy, not from that checklist. Remaining: a dated Release Owner acceptance that the execution pin satisfies BUILD-REL-1, the v4.2.0 Release / Release-Evidence run IDs cited as the AD-13/AD-15 end-to-end proof (the v4.2.0 spec states "AD-13 is policy-authenticated"), and closure of the GOV-1 sprint action.

## 3. FC-NIP Composition Invariants (supporting FR-13 / FR-26)

- One terminal producer boundary: every generated-command terminal path routes through `IPendingCommandOutcomeResolver`; early callbacks are buffered until pending registration is durable.
- Target identity: `[CommandTarget]` on the command type plus a typed `ICommandTargetIdentityProvider<TCommand>` or `CommandTargetResolutionMode.SameAsSource`; an immutable snapshot is captured before dispatch. Standalone create, cross-row, delete/no-op, and status-move commands are covered by the successor contract.
- Materiality: `Material` / `NoOp` / `Unknown` is classified independently from identity; `Unknown` in either dimension suppresses publication.
- Scope and atomicity: indicator mutations are observable by generated grids; tenant/user clearing is independent of the next add; publication is atomic first-wins per `ViewKey` and `EntityKey` across distinct message IDs.
- Telemetry: EventId 5912 `CommandFormTargetResolutionFailed` (Warning, closed failure category only) and 5913 `CommandFormTargetResolutionSucceeded` (Information, payload-free); suppression rate documented in `docs/reference/components/datagrid.md`.
- Evidence: `tests/9-8-live-acceptance.md` — strict live acceptance on clean candidate `7a5737630611b4d54b0180a3fa4c9c4ccd23a28c` at `2026-08-27T22:35:38Z`, browser result 1 passed.
- Residuals: DW-679 (server-allocated keys need a typed post-dispatch identity proof); Story 9.8 deferred items (serialized AppHost fallback should rebuild the complete candidate dependency graph; structured fallback build evidence; serialized-build-log redaction).
- Diagnostic ID note: the successor contract instructed allocation of the next free build-time ID (HFC1071). Delivery reused HFC1005 (invalid attribute argument) for invalid `[CommandTarget]` declarations; the PRD records the shipped behavior rather than the contract's instruction.

## 4. Qualitative UX Rules (supporting FR-8 / FR-9 / FR-10 / FR-11)

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

## 5. MCP Contract Detail (supporting FR-17–FR-19a)

- Contracts: `fc-mcp-fail-closed-security-contract-2026-06-05.md`, `fc-mcp-schema-fingerprint-negotiation-2026-06-05.md`, `fc-mcp-lifecycle-subscription-contract-2026-06-05.md`, `fc-mcp-resources-contract-2026-06-05.md`.
- Token spelling: contract prose sometimes writes "unknown-tool"; the wire category is `unknown_tool` (underscore). Resource token is `unknown_resource`. Generic text is `Request failed.`.
- Full `unknown_tool` structured shape (`BuildUnknownToolStructuredContent`): `category: "unknown_tool"`, `docsCode: "HFC-MCP-UNKNOWN-TOOL"`, `suggestion` (nearest visible name or null), `visibleTools` (the caller's own visible catalog), optional `continuation: "visible-list-truncated"`. Hidden and absent are byte-identical only for the same caller, because `visibleTools` is caller-specific.
- `tools/list` success path returns `[.. catalog.Tools, ToLifecycleTool(options)]`; the lifecycle tool is appended unconditionally for authenticated callers, so an empty list is distinguishable from an authenticated empty catalog (credential-validity signal, disclosed in FR-19).
- `resources/read` for a URI matching no registered `FrontComposerMcpResource.IsMatch` (exact string) never reaches `FrontComposerMcpProjectionReader`; the ModelContextProtocol SDK answers with its default not-found error. Every existing `unknown_resource` test calls the reader directly. The G-7 suite must exercise SDK dispatch.
- `MapFrontComposerMcp` returns `endpoints.MapMcp(route)` without `RequireAuthorization()`; host authentication is an FR-19 consequence on the adopter and is verified by G-7.
- Startup messages name the missing gate interface and point to `AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>()` / `AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>()` "explicitly for sample/dev hosts". No analyzer or startup check currently forbids those registrations in production (PRD OI-3).
- `resources/list` registers projection and skill resources statically into the `ResourceCollection` (URI, name, title, description); only `tools/list` has an empty-list fail-closed behavior. Descriptors are generated from domain types and are identical for every tenant. The PRD discloses this in FR-19 and requires a security sign-off (OI-2) before it can be called accepted; the alternative (owning the list/read handlers and filtering per tenant) is a story option named in OI-2.
- Existing audit-class tests: `ToolAdmissionTests`, `ToolAdmissionSpecGapTests`, `CommandInvokerCoverageTests`, `CommandLifecycleTests`, `McpCommandToolAdapterTests`, `ProjectionReaderTaxonomyTests`, `ProjectionReaderSchemaGateTests`, `ProjectionReaderSchemaTaxonomyTests`, `SchemaNegotiationPrecedenceMatrixTests`, `SkillResourceTests`, `FailClosedLoggingGovernanceTests`, `AuthContextAccessorTests` (Mcp.Tests); `AuthRedactionStressTests` (Shell.Tests); `DriftDiagnosticRedactionTests` (SourceTools.Tests); `EventStorePactContractTests` redaction scan.

## 6. Rejected Alternatives (2026-09-08)

| Alternative | Rejected because |
| --- | --- |
| Keep author signing and RFC 3161 timestamps as FR-24 requirements | REL-5 dropped them 2026-08-04; architecture and the ledger describe unsigned candidates with NuGet.org repository signing. Restoring them would require a dated new decision, not PRD inertia. |
| Define a separate non-customer "gated evidence release" to close REL-AI-1 | Unnecessary: v4.1.1 is already a published governed release with `publish_authorized=true`; REL-AI-1 only lacks owner sign-off. |
| Drop REL-AI-1 from the milestone | It is the post-publication audit that proves the published bytes match the manifest; cheap, and the ledger is the artifact G-1 and SM-2 read. It is not a publication control (that is the protected-environment approval). |
| Roll the EventStore identity back to `3.91.1` to match the approved tuple | Two gitlinks and twelve minor versions of Pact rework for no consumer benefit; live provider evidence already passes at `3.103.0`. The honest remaining act is the approval record (D-12). |
| Treat OI-3 (allow-all ban enforcement) as a regression baseline | Undelivered enforcement is not a baseline; it is gated by G-7. |
| Keep "Default: accepted" on approval-type open items | A gate with a default of pass is a checkbox; approval gates now cite the artifact the approver read and carry no default. |
| Keep Stories 11.20–11.23 as publication gates with re-dated critical path | All four phases are done; 11.23 landed 2026-08-08. |
| Restate v1.0 as a developer-framework-only release and drop operator freshness | FC-NIP composition landed with live proof; both theses are now evidenced, so the PRD keeps both with operator SMs (SM-7, SM-8). |
| Allocate HFC1071 in the PRD for invalid `[CommandTarget]` | Code reuses HFC1005; the PRD records shipped behavior. A new ID would be a change request, not documentation. |
| Gate `resources/list` behind the visibility gate before v1.0 | Descriptors are not tenant data; treated as an accepted residual pending security confirmation (OI-2). |
| Renumber FRs to remove FR-27/FR-28 from the active list | `epics.md` FR coverage map depends on stable IDs; they are demoted to closed decision records (§5.0 Table C) instead. |
| Keep a second "run copy" of the PRD in the BMad run folder | Dual canonical copies caused the dead-path finding; one canonical file plus archived immutable snapshots. |

## 7. Source Inventory For The 2026-09-08 Update

Change signal: `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-09-08/validation-report.md` and its reviewer files (`review-rubric.md`, `review-adversarial.md`, `review-source-reconciliation.md`).

Delivery truth verified (read-only):

- `_bmad-output/implementation-artifacts/sprint-status.yaml` (last_updated 2026-08-29) and story specs 9.3–9.8, 11.2, 11.3, 11.23, 11.24, REL-3/4/5, GOV-1, `rel-ai-1-release-evidence-ledger.md`, `deferred-work.md`, `tests/9-8-live-acceptance.md`, `spec-pact-provider-reconciliation*.md`, `spec-low-logging-governance-hardening.md`, `spec-make-release-compatibility-gates-enforcing.md`.
- `_bmad-output/planning-artifacts/architecture.md` (updated 2026-08-16), `epics.md`, `ux-design.md`, `ux-experience-2026-07-05.md`, `g2-hexalith-builds-inline-pre-publish-gate-request.md`.
- `_bmad-output/contracts/`: FC-NIP base and successor, FC-IA-1, MCP fail-closed / schema / lifecycle / resources, shared-catalog governance, HFCM9002 decision, `frontcomposer-eventstore-approved-runtime-identity-v1.json`.
- Repository: `eng/release-package-inventory.json`, `eng/dependency-graph-policy.json`, `Directory.Build.props`, `global.json`, `.github/workflows/release.yml`, `src/**/PublicAPI*.txt`, `FcDiagnosticIds.cs`, `CommandParser.cs`, `CommandFormEmitter.cs`, `FcShellOptions.cs`, `ProjectionHubRetryPolicy.cs`, MCP admission/negotiation sources; `references/Hexalith.Builds/Props/Directory.Packages.props` and `.github/workflows/domain-release.yml` (read only).

## 8. Validation Finding → Change Map

| Finding (2026-09-08 report) | Change |
| --- | --- |
| Status launders unfinished release | Frontmatter split (`product_approval`, `v1_readiness_milestone_reached`); §0.1 milestone gates split into evidence and approval gates; §0 states that 4.x publication is governed per release and is not switched by this document; D-9 re-approval pending (G-4). |
| REL-AI-1 circular | FR-24 redefines REL-AI-1 as post-publication audit closure; G-1 requires dispositions for the three tags cut around the ledger. |
| MCP fail-closed under-specified | FR-19 table with an exact per-class guarantee (tools vs resources, catalog disclosure, SDK not-found, credential signal); FR-19a; D-14; SM-4 audit as gate G-7; OI-2 sign-off by a non-author; OI-3 story. |
| Post-update adversarial/source-fit findings (2026-09-08) | G-2 rewritten to the actual Builds SHAs with a one-SHA-per-purpose table; D-12/G-3 admit the approval is the only remaining act and drop rollback; freeze wording corrected to REL-5; Roslyn literal dropped; 11.10 non-existence and `epic-11: in-progress` recorded; v3.2.1 added; live Pact evidence for 3.103.0 cited; no "Default: accepted" anywhere; SM-10 added; SM-C1/C3 retired, SM-C5 added; glossary for H/M, AD-n, DW-n, E9-AI-n, retry classes, urgency ordering, `ProjectionRole`. |
| 11.20–11.23 overdue gate | Recorded delivered; D-10 closed; dates removed. |
| Epic 9 stories treated as open | FR-13/FR-26 complete; D-4 amended; G-5/OI-1. |
| Signed v2 model stale | FR-24/NFR-12/SM-2/D-6 restated to unsigned/v3/dispatch. |
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
| REL-4 freeze model stale | FR-24 describes Builds-hosted freeze + dispatch. |
| EventStore identity / Pact | D-12; NFR-11 live Pact lane; G-3. |
| Medium/low: HFCM9002 row, catalog FR specifics, IA nouns, lifecycle vocabulary, Fluent pin, generated-path window, "sensitive cases", addendum missing | D-15; FR-1/FR-9/FR-11 specifics; glossary + UJ-2/UJ-3 rewrites; §7 catalog reference + D-13; §11 path window; FR-19 table replaces vague words; this addendum. |
