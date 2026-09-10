---
source: _bmad-output/planning-artifacts/epics.md
reconciled_into:
  - DESIGN.md
  - EXPERIENCE.md
reconciled: 2026-09-09
status: reconciled-with-open-items
---

# Reconciliation — `epics.md`

This record reconciles the UX-relevant content of `_bmad-output/planning-artifacts/epics.md` into the draft UX spines. `epics.md` is chronologically layered: its canonical requirements, amendments, signed decisions, and active remediation records were retained; provenance-only and expressly superseded material was not revived.

## Carried-forward decisions

| Source decision | Spine location | Reconciliation |
|---|---|---|
| FrontComposer + Blazor Fluent UI V5, Fluent 2 tokens only; Aspire patterns translated rather than copied | `DESIGN.md` Brand & Style, Colors, Do's and Don'ts (lines 65–84, 142–152); `EXPERIENCE.md` Foundation (lines 20–28) | Retained as the inherited UI-system boundary. |
| Nine `FcTypoToken` mappings, `TypographyMappingVersion = "3.1.0"`, and ownership in `Hexalith.FrontComposer.Contracts.UI` with the existing rendering namespace | `DESIGN.md` Typography (lines 86–90) | Retained without redefining Fluent typography. |
| Configurable `FcShellOptions.AccentColor`, default `#0097A7`, used as an accent thread and never as chrome fill | `DESIGN.md` token `colors.accent-thread` (line 16), Brand & Style and Colors (lines 65–84), shell/navigation component rows (lines 120–123) | Retained as a configurable default, not a fixed palette. |
| Full-width page default, constrained `75rem` maximum; `72px` labelled and `48px` icon-only rails; `32px` compact grid rows; sticky grid header | `DESIGN.md` spacing tokens (lines 30–34), Layout & Spacing (lines 92–100), component rows (lines 120–139); `EXPERIENCE.md` Responsive & Platform (lines 262–270) | Exact source dimensions retained; numeric viewport breakpoints were not invented. |
| Neutral header/footer, optional single logo fragment, compact default density, navigation icon/label stack, count/New overlays, page toolbar, and lightweight status icons | `DESIGN.md` Brand & Style (lines 65–71), Components (lines 118–140) | Retained at visual-contract level; Fluent implementation anatomy remains inherited. |
| Status values use semantic icon + accessible label + hover/focus tooltip; numeric counts remain badge pills | `DESIGN.md` Shapes and Components (lines 108–139); `EXPERIENCE.md` status pattern (line 109) and Accessibility Floor (lines 237–251) | Retained; pill-only semantic status was rejected. |
| One primary Module entry per bounded context; required default Module Tab; `/{module}/{tab}` tab routes; secondary projection flyout; one active item by longest segment-prefix | `EXPERIENCE.md` Information Architecture and navigation invariants (lines 30–61) | Retained as signed FC-IA-1 behavior. `/{module}` is documented as the default-tab alias. |
| Generated command route `/commands/{BoundedContext}/{CommandTypeName}` from palette, CTA, and direct activation | `EXPERIENCE.md` IA (lines 41–42, 60), UJ-3 (lines 308–318) | Retained as the sole current generated-command route family. |
| Shell frame, skip links, always-present account menu, settings/palette shortcuts, `/` and `/home`, and valid empty-shell behavior | `EXPERIENCE.md` IA (lines 32–51), component patterns (lines 93–101), shell/home states (lines 119–132) | Retained, including `/authentication/challenge` and `/authentication/sign-out`. |
| `FcHomeDirectory` progressive home states and urgency ordering | `DESIGN.md` home-directory row (line 123); `EXPERIENCE.md` home pattern and state table (lines 98, 119–132), UJ-2 (lines 295–306) | Retained as No Modules, Hydrating, Partially Ready, and Ready. |
| Projection Loading/Empty/Data, filtering/reset, status, row detail, hidden-by-filter announcement, >15-column prioritization, slow/max notices, live reconciliation, and connection recovery | `DESIGN.md` projection component rows (lines 130–134); `EXPERIENCE.md` component patterns (lines 105–109), projection state matrix (lines 150–167), UJ-2 (lines 295–306) | Retained as observable surface/state behavior. SignalR nudges never count as row-level success proof. |
| Command form density (`0–1` Inline, `2–4` CompactInline, `5+` FullPage), hidden server/derived fields, unsupported-field placeholder, authorization, destructive confirmation, abandonment guard, and FC-CNC one-at-a-time behavior | `DESIGN.md` command rows (lines 135–138); `EXPERIENCE.md` component patterns (lines 110–114), command states (lines 169–187), UJ-3 (lines 308–318) | Retained. A later local submit is blocked, never queued, batched, or raced. |
| Lifecycle vocabulary `Submitting`, `Acknowledged`, `Syncing`, `Confirmed`, `Rejected`, `IdempotentConfirmed`, `NeedsReview`, `Warning`, `Degraded`; HTTP acceptance is not confirmation | `EXPERIENCE.md` lifecycle matrix (lines 169–187), Voice and Tone (lines 74–87), UJ-3 (lines 308–318) | Complete state set retained, including `Warning`. |
| Lifecycle budgets: Degraded at `10,000ms`, poll every `1,000ms` for at most `120,000ms`, lifecycle retry budget zero; one separate transient dispatch retry after `250ms` with the same `MessageId` | `EXPERIENCE.md` lifecycle matrix and budget paragraph (lines 175–187), UJ-3 (lines 315–318) | Retained with dispatch retry explicitly separated from lifecycle polling. |
| Fresh-row identity is explicit, immutable, tenant/user/lane scoped, resolver-owned, materiality-gated, live-observable, and atomic first-wins for `(ViewKey, EntityKey)`; nudges, diffs, EventStore aggregate IDs, and untyped results are forbidden identity sources | `DESIGN.md` fresh-row row (line 139); `EXPERIENCE.md` fresh-row pattern and state (lines 114, 189–195), anti-patterns (lines 272–280), UJ-2/UJ-3 (lines 295–318) | Retained as the current behavior contract; later canonical sources supply details beyond the historical Epic 9 status. |
| WCAG 2.2 AA, skip links, accessible names, visible/unobscured focus, keyboard reachability, row-detail region, useful live regions, reduced motion, forced colors, and stable test selectors | `EXPERIENCE.md` Accessibility Floor (lines 237–251); relevant component and state rows | Retained as the behavioral floor; visual contrast/focus treatment remains in `DESIGN.md`. |
| Framework-owned authentication/security, tenant/user scoping, service-boundary command authorization, server-controlled fields, and support-safe/redacted outputs | `EXPERIENCE.md` Foundation (lines 26–28), MCP matrix (lines 197–210), Security, Tenancy, and Support Safety (lines 253–260), UJ-4/UJ-6 (lines 320–351) | Retained across human, MCP, and evidence surfaces. |
| MCP, customization, CLI inspect/migrate, generated output, and Testing package are user-facing nonvisual product surfaces | `EXPERIENCE.md` Foundation and IA (lines 20–51), developer/tooling states (lines 212–221), UJ-4–UJ-6 (lines 320–351) | Retained so the adopter, agent-integrator, maintainer, and test-engineer actors have closed journeys. |
| `<AuditTimeline>` and `<ConsequencePreview>` are out of v1 scope | `EXPERIENCE.md` Interaction Primitives and anti-patterns (lines 223–235, 272–280) | Retained as an explicit v1 exclusion. |

## Historical or superseded material intentionally excluded

- `LEGACY-FR-*` and `LEGACY-NFR-*` were not copied as current identifiers; `epics.md` labels them provenance-only.
- The earlier pill-only status model was excluded. The current colored-icon/accessible-label model wins.
- The earlier “no Desktop hamburger” decision was excluded. The always-visible hamburger and labelled/icon-only Desktop rail win.
- Early navigation wording that could promote projections into the primary tree was not retained as primary IA. Signed FC-IA-1 makes the Module entry primary and the projection flyout secondary.
- Historical projection links `/{bc-lower}/{proj-kebab}` and palette/CTA links `/domain/{kebab}/{kebab}` were excluded. They appear only as pre-decision alternatives in Story 11.0.
- Story 9.2 was not treated as accepted composed proof. Its implementation is historical; the 2026-08-11 retrospective rejected its end-to-end evidence.
- Projection nudges, visible-row diffs, EventStore `AggregateId`, existing-row cascade, and untyped result payloads were excluded as fresh-row identity producers.
- Epic 8's framework visual-refresh work was not used to claim that Tenants.UI page-body adoption is complete; the source explicitly leaves that to separate Host-A work.
- Contract-confirmation “escalated with an owner” wording was not treated as Done. A confirmed decision or tracked, dated, owned blocking follow-up is required.
- Release-governance, analyzer burn-down, logging migrations, mechanical file splits, and story-evidence process mechanics were excluded from the UX spines unless they created a user-visible state, disclosure rule, visual-conformance requirement, or actor journey.
- FC-DOC validation commands, Public API baseline mechanics, generator file counts, and individual HFC diagnostic catalog entries remain implementation/documentation governance rather than visual or behavioral spine content. Their user-visible failure/recovery semantics were retained where relevant.

## Qualitative ideas dropped

None. The source's qualitative direction—Aspire-grade polish, neutral chrome, accent as a thread, dense readable data, restrained status, and support-safe operational feedback—was retained. Only material expressly marked historical, superseded, implementation-only, or out of scope was omitted from the spines.

## Unresolved items and blockers

1. **Exact state presentation and final microcopy.** Warning, NeedsReview, Degraded, missing tenant, FallbackPolling, SlowQuery, MaxItems, and fresh-row dismissal/forced-colors treatments remain unspecified. Recorded in `DESIGN.md` Open visual questions (lines 154–158) and `EXPERIENCE.md` Open Questions (lines 353–358).
2. **Responsive breakpoints and compact navigation.** `epics.md` fixes Desktop rail widths and says Mobile/Compact opens the drawer, but supplies no numeric breakpoints. The spines preserve semantic Desktop/Compact/Narrow modes. `EXPERIENCE.md` lines 267–268 currently use “may” for compact/drawer behavior, which is weaker than Epic 8 and requires an explicit final disposition.
3. **Fluent V5 pin and exact token/API names.** The source requires Fluent V5/Fluent 2 but does not settle the final package pin or supported replacement for the legacy accent alias. Recorded at `DESIGN.md` lines 84 and 158 and `EXPERIENCE.md` line 357.
4. **Epic 9 source status versus later contract detail.** `epics.md` still reports Stories 9.3–9.8 as active remediation and Story 9.8 as the composed/live gate. `EXPERIENCE.md` lines 189–195 record a resolved target/materiality model supplied by later planning sources. Final traceability must cite those later sources; `epics.md` alone cannot prove Epic 9 closure.
5. **Exact EventStore status endpoint.** The draft records the confirmed polling behavior but not the source's literal `GET /api/v1/commands/status/{id}`. Decide whether the endpoint is part of the UX behavioral contract or remains an architecture/API reference before finalization.
6. **Localization ownership detail.** `epics.md` assigns shell chrome strings to `FcShellResources.resx` through `AddHexalithShellLocalization(...)`, keeps domain labels host-owned with no shell fallback, and excludes density-preview samples from localization. The draft states localized behavior but does not preserve this exact ownership map; it needs an explicit disposition.
7. **Accessibility release gate.** The spine preserves WCAG behavior, but does not explicitly record the Epic 1 three-layer automated ready-gate or Product/UX + Release Owner visual/manual sign-off before v1.0 RC. Decide whether that evidence ownership belongs in the handoff/checklist or in `EXPERIENCE.md` before finalization.
8. **Implementation symbols intentionally abstracted.** `FcColumnPrioritizer`, `FcProjectionGlobalSearch`, `GET /api/v1/commands/status/{id}`, and HFC1050–HFC1055 are represented by behavior rather than always named. Confirm whether adopter-facing API discoverability requires these exact names in the final spine.

