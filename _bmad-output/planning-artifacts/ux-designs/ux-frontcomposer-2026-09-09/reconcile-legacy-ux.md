---
title: Legacy UX Input Reconciliation — Hexalith FrontComposer
status: draft
created: 2026-09-09
updated: 2026-09-09
inputs:
  - _bmad-output/planning-artifacts/ux-design.md
  - _bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md
  - _bmad-output/planning-artifacts/ux-experience-2026-07-05.md
  - _bmad-output/planning-artifacts/archive/ux-designs/ux-frontcomposer-2026-07-05/.memlog.md
outputs:
  - DESIGN.md
  - EXPERIENCE.md
---

# Legacy UX Input Reconciliation

## Outcome

The legacy UX artifact group has been reconciled into the draft peer spines:

- `DESIGN.md` owns appearance.
- `EXPERIENCE.md` owns information architecture, behavior, states, interactions, accessibility behavior, developer/agent surfaces, and journeys.

The peer spines jointly supersede the legacy sole-canonical/supplement authority chain. Within their respective domains, the spines win over the legacy files, mockups, wireframes, imports, historical sprint-change notes, and implementation examples. This reconciliation records migration, not Product approval or implementation evidence.

## Carried Decisions

| Legacy decision or quality | Current spine location | Reconciliation |
|---|---|---|
| Common UX for Hexalith applications composed through FrontComposer | `DESIGN.md` Brand & Style; `EXPERIENCE.md` Foundation | Carried, narrowed to the current operational audience and supported product surfaces. |
| FrontComposer + Blazor Fluent UI V5 inheritance; no custom theme | `DESIGN.md` Brand & Style, Colors, Typography, Components, Do's and Don'ts | Carried. The spine specifies only FrontComposer deltas and does not copy Fluent theme roles. |
| Professional, precise, quiet, trustworthy operational posture | `DESIGN.md` Brand & Style | Carried. |
| Neutral chrome and accent-as-thread visual direction | `DESIGN.md` Brand & Style and Colors | Carried. `FcShellOptions.AccentColor` remains configurable with default `#0097A7`; no other Fluent theme role is hard-coded. |
| Fluent-owned typography and existing nine `FcTypoToken` mappings, version `3.1.0` | `DESIGN.md` Typography | Carried with `Contracts.UI` ownership and public namespace continuity. |
| Full-width default and constrained `75rem` maximum | `DESIGN.md` Layout & Spacing; `EXPERIENCE.md` page-frame behavior | Carried exactly. |
| Desktop-first responsive web; labelled/icon-only rail at `72px`/`48px`; always-visible hamburger | `DESIGN.md` Layout & Spacing and navigation-rail; `EXPERIENCE.md` Foundation, component behavior, Responsive & Platform | Carried exactly. |
| Compact projection grid rows at `32px` with sticky header | `DESIGN.md` Layout & Spacing and projection-grid; `EXPERIENCE.md` projection-grid and UJ-2 | Carried exactly. |
| One primary shell entry per Module; required default Module Tab; projection flyout secondary | `EXPERIENCE.md` Information Architecture and navigation-rail/module-tabs patterns | Carried and made route-exact. |
| Default tab uses module plural label, fallback `Overview`; `/{module}` aliases `/{module}/{default}`; tabs use `/{module}/{tab}` | `EXPERIENCE.md` Information Architecture and module-tabs | Carried exactly. |
| Generated command route `/commands/{BoundedContext}/{CommandTypeName}` | `EXPERIENCE.md` Information Architecture, command-form pattern, UJ-3 | Carried exactly for palette, CTA, and direct activation. |
| Exactly one active navigation item | `EXPERIENCE.md` Information Architecture and navigation-rail | Carried with longest segment-prefix behavior. |
| Home directory progressive loading/data behavior and urgency ordering | `EXPERIENCE.md` home-directory pattern and Shell/home states | Carried as No Modules, Hydrating, Partially Ready, and Ready; ordering is ready first, actionable count descending, then ordinal name. |
| Registry-driven projection discovery, filtering, status, row detail, empty/loading/connection feedback | `EXPERIENCE.md` IA, Component Patterns, Projection state-by-surface matrix | Carried and expanded to the full current state vocabulary and thresholds. |
| Projection states Loading, Empty, Data, Stale, Reconnecting, FallbackPolling, SlowQuery, and MaxItems | `EXPERIENCE.md` Projection state-by-surface matrix | Carried with `2,000ms` SlowQuery, `10,000` MaxItems, server virtualization from `500` rows, reconnect/fallback/recovery budgets, actions, announcements, and terminality. |
| Status icon with hover/focus tooltip and persistent accessible name; count badges remain pills | Both spines, status-affordance row; `DESIGN.md` Colors | Carried. Status never depends on color, motion, hover, or tooltip alone. |
| Reusable command palette, settings, destructive confirmation, abandonment guard, lifecycle, projection-state, and fresh-row components | Both spines, matching Component tables | Carried with substantive visual and behavioral contracts and exact current FrontComposer names. |
| Page-section accordion rule | `DESIGN.md` Layout & Spacing/page-sections; `EXPERIENCE.md` page-sections | Carried: two or more sibling titled sections share one accordion; primary is initially expanded; a sole primary region is never hidden. |
| Command density rule | `EXPERIENCE.md` command-form | Carried exactly: 0–1 Inline, 2–4 CompactInline, 5+ FullPage after excluding derived/server-controlled fields. |
| Command lifecycle truth and accepted-transport distinction | `EXPERIENCE.md` Command authorization, validation, and lifecycle; UJ-3 | Carried and expanded to Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, NeedsReview, Warning, and Degraded. |
| Lifecycle budgets and FC-CNC one-at-a-time behavior | `EXPERIENCE.md` lifecycle-feedback, lifecycle matrix, Interaction Primitives | Carried: `10,000ms` to Degraded, `1,000ms` polls up to `120,000ms`, and later local submits blocked rather than queued/batched. |
| Destructive confirmation and dirty-form abandonment guard | `EXPERIENCE.md` command-safety and UJ-3 | Carried; the `30s` edit threshold and deterministic focus return are explicit. |
| Accessibility baseline: WCAG 2.2 AA, skip links, keyboard, focus, names, roles, live regions, reduced motion, forced colors | `EXPERIENCE.md` Accessibility Floor and state/component rules; `DESIGN.md` semantic visual rules | Carried and made measurable for `320 CSS px` reflow, `400%` zoom, WCAG text spacing, `24×24 CSS px` target size or exact exception, focus-not-obscured, light/dark, forced colors, and reduced motion. |
| Direct, support-safe, evidence-based microcopy; never expose internals | `EXPERIENCE.md` Voice and Tone; Security, Tenancy, and Support Safety | Carried. |
| Settings theme/density persistence and announcements | `EXPERIENCE.md` settings pattern and supporting states | Carried with tenant/user scope and persistence-failure behavior. |
| Always-rendered account access and framework-owned server security | `DESIGN.md` account-control; `EXPERIENCE.md` account behavior/states and Security | Carried with challenge/sign-out routes and token eviction on sign-out. |
| FC-NIP fresh-row behavior | `DESIGN.md` fresh-row-indicator; `EXPERIENCE.md` fresh-row pattern/state and UJ-2/UJ-3 | Carried and corrected to immutable pre-dispatch target identity, independent Material/NoOp/Unknown disposition, resolver-owned terminal handling, scope-before-render, and atomic first-wins `(ViewKey, EntityKey)`. |
| Developer customization, diagnostics, generated-output inspection/migration, MCP, and Testing concerns | `EXPERIENCE.md` IA, Supporting surface states, MCP matrix, Developer/tooling states, UJ-4 through UJ-6 | Carried and expanded for current surface closure. |
| Aspire Dashboard as visual inspiration, translated rather than copied | `EXPERIENCE.md` Inspiration & Anti-patterns; `DESIGN.md` Brand & Style | Carried without importing Fluent V4/FAST tokens or Aspire-specific theme values. |

## Superseded or Conflicting Material

| Legacy material | Current disposition |
|---|---|
| `ux-design.md` declares itself the sole canonical UX contract and places the detailed/experience files beneath it | Superseded by the confirmed peer-spine model. `DESIGN.md` and `EXPERIENCE.md` own separate domains and jointly form the UX contract. |
| The detailed and experience supplements defer all conflicts to `ux-design.md` | Superseded by peer ownership and current PRD/architecture/source reconciliation. The legacy files remain inputs, not authorities over the spines. |
| Alex “Find and modify a party” and Maya “Add a new party” are the only Key Flows | Obsolete. Replaced by the exact source journey set UJ-1 through UJ-6 with Nina, Marc, Ravi, Camille, and Sophie. Compatible find/modify/create behavior survives inside Marc's UJ-2/UJ-3. |
| Mixed consumer-facing and administrator audience | Superseded by the current PRD audience: adopter developers, operators, AI-agent integrators, framework maintainers, and release owners. Bespoke consumer marketing/transactional UX is out of scope. |
| Parties as the worked/provisional first reference Module | Superseded. Tenants is the obligated first adopter; Parties is only a dated D-7 fallback with equivalent evidence. |
| Any older shell navigation that promotes projections/module pages to top-level entries | Superseded by one primary entry per Module, required default tab, and secondary projection flyout. |
| Pill-only semantic status (`FcStatusBadge`) | Superseded by icon-plus-label status; only numeric counts remain badge pills. |
| Legacy toolbar descriptions that present `FluentToolbar`, `FluentSearch`, or other internals as promised FrontComposer API | Corrected. `FcPageToolbar` is the public contract; internal Fluent composition remains implementation-owned. |
| Legacy lifecycle set omits Warning or treats accepted transport as success | Corrected to the exact nine-state vocabulary and evidence-based confirmation. |
| Legacy fresh-row behavior permits incomplete pending metadata, ambient row context, unrelated renders, or broad nudge/diff inference | Superseded by the current FC-NIP resolver, target, materiality, scope, live invalidation, suppression, and first-wins rules. |
| Fresh markers for server-allocated create keys | Explicit v1 non-goal. No indicator is safer than a wrong row; DW-679 owns a future typed post-dispatch proof. |
| Legacy text that places the `250ms` retry inside lifecycle polling or ambiguously after acknowledgement | Superseded in the draft spine: lifecycle has zero pre-accept lifecycle retries; the separate transient dispatch retry occurs exactly once after `250ms` with the same `MessageId` and is never a polling retry. |
| `ux-design.md` UX-DR1 says an accent alias cannot carry an independent value | Reconciled to the current source set: `FcShellOptions.AccentColor` remains configurable with default `#0097A7`, while no other Fluent theme role is copied or hard-coded. The exact Fluent V5 role/alias remains open. |
| Historical status strings or component implementations as visual authority | Not carried as authority. State meaning and accessibility are contractual; final appearance inherits Fluent unless `DESIGN.md` defines a delta. |

## Qualitative Ideas Dropped

None. Every compatible qualitative idea—professional operational tone, quiet neutral chrome, restrained accent, dense readable data, support-safe copy, accessible state feedback, and Aspire-inspired discipline—was retained. Contradictory audience, authority, navigation, status, and journey material is classified above as superseded rather than silently dropped.

## Not Duplicated Into the Spines

- Approval/gate status, candidate-bound evidence ledgers, OI-16/SM-6 closure bookkeeping, and implementation-test inventories remain in their planning/governance sources. The spines define UX contracts; they do not claim implementation or Product approval.
- Historical decision chronology and story ownership remain in the legacy files and `epics.md`. Only enduring UX decisions were distilled.
- No legacy visual artifact was promoted: the current workspace contains no imported visual, mockup, or wireframe.

## Unresolved Items

1. **Exact exceptional-state presentation and final microcopy.** The draft spines intentionally retain notes for Warning, NeedsReview, Degraded, missing tenant, FallbackPolling, SlowQuery, MaxItems, and fresh-row dismissal/forced-colors treatment. The newly reconciled `ux-design.md` now proposes deterministic strings and `300ms` announcement coalescing, while the confirmed migration memlog and draft spines still classify final presentation/copy as source-unspecified. Product/UX must decide whether those matrix strings become the peer-spine contract.
2. **Retry wording conflict.** The revised `ux-design.md` describes a post-acknowledgement `250ms` transient retry; the current draft spine, architecture extraction, and story behavior scope `250ms` to a separate transient dispatch retry using the same `MessageId`, not lifecycle polling. This requires source reconciliation before final status.
3. **Responsive breakpoints.** Desktop, Compact, and Narrow-browser behavior is committed; numeric breakpoint widths are not.
4. **Fluent V5 identity.** The selected catalog pin and exact supported Fluent token/API role for the configurable accent must be confirmed. No new alias, theme role, or component promise should be invented.
5. **Reviewer/approval state.** The peer spines remain `draft` pending the opt-in reviewer gate or explicit skip and disposition of the source conflicts above. Product approval and implementation evidence remain external gates.
