---
name: Architecture input reconciliation
type: ux-input-reconciliation
status: complete
updated: 2026-09-09
inputs:
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
outputs:
  - DESIGN.md
  - EXPERIENCE.md
---

# Architecture Input Reconciliation

## Result

The UX-bearing architecture decisions are represented in the draft peer spines. `DESIGN.md` owns
their visual expression and `EXPERIENCE.md` owns their information architecture, behavior, state,
interaction, accessibility, security, and journeys. Code-layer placement and public type ownership
remain architecture contracts and are referenced here rather than duplicated as product behavior.

The focused GOV-1 spine was reviewed in full. It explicitly changes no runtime, public API, package
inventory, or UX, so its dependency-graph and release-governance mechanisms were intentionally not
projected into either UX spine.

## Carried-forward decisions

| Architecture decision | Source | Spine location and disposition |
|---|---|---|
| FrontComposer is a generated Blazor framework; the UI system is the centrally pinned FrontComposer/Blazor Fluent UI V5 package and Fluent 2 tokens. Raw interactive HTML controls are forbidden outside documented carve-outs. | `architecture.md:15-17`, `45`, `82-84` | `DESIGN.md` Brand & Style, Colors, Components, and Do's and Don'ts; `EXPERIENCE.md` Foundation and Interaction Primitives. Fluent owns theme and component behavior; FrontComposer records only its deltas. |
| The dual-TFM `Hexalith.FrontComposer.Contracts` kernel remains UI-clean. The packable net10-only `Contracts.UI` assembly owns `Typography`, `FcTypoToken`, `RenderFragment` contexts, `KeyboardEventArgs`, and projection slot/template/view rendering contracts under their existing namespaces. | `architecture.md:17`, `21-24`, `39-40` | `DESIGN.md` Typography carries the `FcTypoToken`/`TypographyMappingVersion` rendering contract. `EXPERIENCE.md` Foundation, developer/tooling states, and UJ-5 preserve the generated-contract boundary. Exact assembly/type placement remains architecture-owned. |
| Shell dependencies flow from Components and Infrastructure toward pure Routing and State contracts. State owns Fluxor slices/effects, mutation coordinators, and polling scheduler lane models; `PendingCommandPollingDriver`, `ProjectionFallbackPollingDriver`, and `ProjectionFallbackRefreshScheduler` are scoped Infrastructure workers. Shell state is single-writer and scoped-lifetime. | `architecture.md:27-35`, `46` | `EXPERIENCE.md` State Patterns, Fresh-row state, Security/Tenancy, and UJ-2/UJ-3 carry the observable, scope-safe behavior. Namespace placement, the legacy `ProjectionSchemaMismatchException` exception, and source-guard mechanics remain in architecture and are not restated as UX. |
| One bounded context is one **Module**, with one primary shell entry and one required default **Module Tab**. Primary tab routes use `/{module}/{tab}`; projection flyouts stay secondary. | `architecture.md:76-79` | `EXPERIENCE.md` Information Architecture, Navigation invariants, Component Patterns, Supporting surface states, UJ-1, and UJ-2. The draft also records the confirmed `/{module}` default-tab alias and unknown-tab failure behavior inherited from other approved inputs. |
| Generated command pages use `/commands/{BoundedContext}/{CommandTypeName}`; palette entries and projection empty-state CTAs use the same route family. | `architecture.md:80-81` | `EXPERIENCE.md` Information Architecture, Navigation invariants, command-form pattern, and UJ-3. |
| User journeys and visual states meet WCAG 2.2 AA, including keyboard, focus, names, roles, live regions, reduced motion, and forced colors. | `architecture.md:82-84` | `DESIGN.md` Colors, Components, and Do's and Don'ts; `EXPERIENCE.md` Interaction Primitives, Accessibility Floor, and UJ-6. Semantic roles are explicit rather than implied. |
| Command transport acceptance is not projection/status-confirmed success. Lifecycle UI includes the core sequence plus `IdempotentConfirmed`, `NeedsReview`, `Warning`, and `Degraded`. | `architecture.md:48`, `85-86` | `EXPERIENCE.md` lifecycle component, lifecycle state table, Interaction Primitives, and UJ-3; `DESIGN.md` lifecycle feedback. All four architecture-added states are present. |
| FC-CNC allows one in-flight local command. A second submit is blocked, never queued or batched, receives localized accessible feedback that it did not run, and leaves the original command visible and unchanged. | `architecture.md:87-89` | `EXPERIENCE.md` lifecycle component/state text, Interaction Primitives, and UJ-3. |
| Default lifecycle timing is confirming-to-`Degraded` at `10_000ms`, polling every `1_000ms` for at most `120_000ms`, plus exactly one transient Epic 4 dispatch retry after `250ms`. | `architecture.md:90-91` | `EXPERIENCE.md` lifecycle table and timing paragraph distinguish the one dispatch retry from lifecycle polling; UJ-3 carries the same separation. |
| `IPendingCommandOutcomeResolver` is the sole terminal-state/fresh-row publisher. Target identity is immutable and explicit; there is one validated pre-dispatch snapshot; terminal materiality is exactly `Material`, `NoOp`, or `Unknown`; ineligible outcomes suppress the marker. | `architecture.md:58-62` | `EXPERIENCE.md` fresh-row component and Fresh-row state carry resolver ownership, explicit pre-dispatch identity, closed materiality, eligibility/suppression, and forbidden ambient inference. Public implementation type/member names and the snapshot field list remain source-of-record details in architecture. |
| Fresh-row state is observable and scope-safe. Tenant/user scope is enforced before read/render. Identity is `(ViewKey, EntityKey)` with atomic first-wins behavior across message IDs; later outcomes cannot replace provenance or extend expiry. Material idempotent confirmation retains the eligible ten-second TTL. | `architecture.md:61`, `63-65` | `EXPERIENCE.md` fresh-row component/state, Security/Tenancy, UJ-2, UJ-3, and UJ-6; `DESIGN.md` fresh-row-indicator visual contract. The draft's lane scope is additive and does not weaken tenant/user scope. |
| MCP security fails closed and requires both tenant-tool and resource-visibility gates. | `architecture.md:47` | `EXPERIENCE.md` Foundation, MCP state/disclosure matrix, Security/Tenancy, and UJ-4. |
| UX/layout policy is projected into the UX planning authority. | `architecture.md:53-54` | `DESIGN.md` and `EXPERIENCE.md` opening ownership clauses replace the legacy single-file precedence chain with peer visual and behavioral authorities. |

## Conflicts corrected

1. The legacy experience lifecycle component omitted `Warning` although canonical UX and architecture
   required it. `EXPERIENCE.md` now includes `Warning` in the lifecycle state table and UJ-3.
2. Legacy UX generalized the `250ms` retry. The draft now identifies it as one transient dispatch
   retry, separate from `1,000ms` lifecycle polling, matching the architecture's Epic 4 scope.
3. Legacy UX described fresh-row appearance and scope only broadly. The draft now carries resolver-only
   publication, immutable explicit target identity, independent materiality, suppression rules,
   forbidden ambient inference, pre-render scoping, live invalidation, `(ViewKey, EntityKey)` identity,
   first-wins provenance, and the no-expiry-extension rule.
4. Legacy accessibility text did not explicitly preserve architecture's general semantic-role
   requirement. The draft Accessibility Floor names landmarks, navigation, tabs/tabpanels, palette,
   dialogs, grids, and row-detail region semantics.
5. The legacy single-file canonical precedence rule is replaced by peer ownership: `DESIGN.md` for
   appearance and `EXPERIENCE.md` for behavior. Both continue to lose to architecture on architectural
   constraints.

No unresolved contradiction remains between these architecture inputs and the draft UX spines.

## GOV-1 material intentionally excluded

The GOV-1 spine states that GOV-1 is governance-only and changes no runtime/public API behavior,
generated output, package inventory, dependency versions, or UX (`ARCHITECTURE-SPINE.md:249-255`), and
the parent architecture likewise says the delivery architecture does not alter product behavior or UX
(`architecture.md:294`). Consequently, the following were reviewed but not imported:

- depth-1/depth-2 dependency graph collection and Git-object identity;
- repository normalization, canonical JSON, graph digests, schemas, resource ceilings, and fixtures;
- CI base/candidate comparison and affected-module build classification;
- policy activation, evaluator authorization, workflow-source closure, and Builds commit lineage;
- release handoffs, manifest migration/sealing, fallback approval, publication classification,
  post-release verification, ledger records, environment protection, and release actor evidence;
- GOV-1 deferred work for graph history, catalog markers, workflow identity, evaluator activation,
  incident response, retention, and provider topology.

Those subjects remain architecture/release governance. Their source references remain in each spine's
`sources:` list for provenance only.

## Qualitative ideas dropped

None. These architecture inputs are prescriptive contracts rather than optional qualitative design
directions. The excluded GOV-1 content is non-UX mechanism, not a dropped UX idea.

## Unresolved items and blockers

- Architecture requires Fluent UI V5 and Fluent 2 tokens but does not identify the selected exact V5
  catalog pin or the supported token/API role for the configurable accent. This remains recorded in
  `DESIGN.md` Colors/Open visual questions and `EXPERIENCE.md` Open Questions.
- Architecture names required lifecycle and fresh-row states but does not approve their exact visual
  treatment or final microcopy. `Warning`, `NeedsReview`, `Degraded`, missing tenant,
  `FallbackPolling`, `SlowQuery`, `MaxItems`, and fresh-row dismissal/forced-colors treatment remain
  explicit UX notes rather than invented decisions.
- Architecture defines responsive behavior semantically but no numeric breakpoints. Both spines retain
  Desktop/Compact/Narrow-browser behavior and explicitly avoid fabricating widths.
- The architecture inputs introduce no separate GOV-1 UX blocker. Final status still depends on
  disposing the UX-owned open items and completing or explicitly skipping the reviewer gate.
