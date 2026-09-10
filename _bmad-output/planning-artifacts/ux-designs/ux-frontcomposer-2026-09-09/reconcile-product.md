---
title: Product Input Reconciliation
status: draft-reconciliation
updated: 2026-09-09
sources:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/prd-addendum-2026-09-08.md
spines:
  - DESIGN.md
  - EXPERIENCE.md
---

# Product Input Reconciliation

This record reconciles the canonical product requirements and their 2026-09-08 addendum into the current FrontComposer UX spines. It records disposition rather than restating the source documents. Product scope, requirement IDs, exact protocol contracts, and milestone status remain owned by the sources.

## Source authority

- `_bmad-output/planning-artifacts/prd.md` is the sole canonical PRD; its addendum is `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md` (`prd.md:14-20`, `prd-addendum-2026-09-08.md:8-10`).
- The PRD currently calls `_bmad-output/planning-artifacts/ux-design.md` the canonical UX planning source and says it is sufficient (`prd.md:651-659`). The new peer spines state that they supersede the legacy UX precedence chain within their visual and behavioral domains (`DESIGN.md:61-63`, `EXPERIENCE.md:16-18`). This authority transition is intentional for this update but is not yet reflected in the PRD; it remains an explicit governance conflict before final status.
- Release evidence, dependency governance, package mechanics, and milestone bookkeeping remain inherited source context. They were not duplicated into the UX spines unless they produce a human-, agent-, or developer-visible state.

## Carried-forward product decisions

| Product decision | Source | Spine location and disposition |
|---|---|---|
| FrontComposer is an operations-ready Blazor shell and developer framework, with trustworthy projection freshness and command outcomes | `prd.md:43-49` | `DESIGN.md` **Brand & Style**; `EXPERIENCE.md` **Foundation**, **State Patterns**, and UJ-2/UJ-3 |
| Primary audiences are Adopter developer, Operator, AI-agent integrator, Framework maintainer, and Release owner; bespoke consumer marketing/transactional UX is excluded | `prd.md:51-75` | `DESIGN.md:65-71`; `EXPERIENCE.md:20-28` |
| Desktop-first responsive web; no native mobile/desktop product; CLI, generated output, MCP, and Testing are peer product surfaces | `prd.md:129-133,568-579` | `EXPERIENCE.md:20-28`, **Responsive & Platform**, and **Developer/tooling states** |
| FrontComposer + Blazor Fluent UI V5, Fluent 2 tokens, no raw controls or legacy V4/FAST theme recreation | `prd.md:524-528`; `prd-addendum-2026-09-08.md:53-65` | `DESIGN.md` **Brand & Style**, **Colors**, **Typography**, **Layout & Spacing**, **Components**, and **Do's and Don'ts**; `EXPERIENCE.md:22-24,272-280` |
| Neutral chrome, accent as thread, compact density, sticky grid headers, toolbar/search discipline, lightweight status icons | `prd.md:244-253`; `prd-addendum-2026-09-08.md:60-65` | `DESIGN.md:65-84,92-152`; `EXPERIENCE.md:93-115,272-280` |
| Full-width default, constrained maximum `75rem`, persisted theme/density, exact compact grid row `32px` | `prd.md:255-263`; `prd-addendum-2026-09-08.md:60` | `DESIGN.md:30-34,92-100`; `EXPERIENCE.md:100-107` |
| Bounded Context is operator-facing Module; one primary entry, required default Module Tab, `/{module}/{tab}`, root alias, plural-label default with `Overview` fallback, secondary Projection Flyout, exactly one active item | `prd.md:93-102,268-279`; `prd-addendum-2026-09-08.md:57-59` | `EXPERIENCE.md` **Information Architecture**, especially `:32-72`; represented visually in `DESIGN.md:120-130` |
| Home urgency ordering: ready first, descending actionable count, then ordinal Module name | `prd.md:102,270-278`; `prd-addendum-2026-09-08.md:66` | `EXPERIENCE.md:35,59,98,121-132`; `DESIGN.md:123` |
| Complete shell frame, always-present account menu and hamburger, `Ctrl+,`, `Ctrl+K`, and optional `/` page-search shortcut | `prd.md:244-253`; `prd-addendum-2026-09-08.md:61,63` | `EXPERIENCE.md:34,43-44,93-103,223-235`; `DESIGN.md:120-128` |
| Projection state set and thresholds: Loading, Empty, Data, Stale, Reconnecting, FallbackPolling, SlowQuery at `2,000ms`, MaxItems at `10,000`, server virtualization from `500` | `prd.md:281-300,531-532` | `EXPERIENCE.md` **Projection state-by-surface matrix** (`:150-167`); `DESIGN.md:130-134,156` |
| Realtime recovery: unbounded jittered retries capped at `30,000ms`, closed restart within `10s`, fallback polling every `15s` over at most eight lanes, `3,000ms` reconnected notice | `prd.md:292-300,531` | `EXPERIENCE.md:150-167` |
| Exact command lifecycle and truth: Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, NeedsReview, Warning, Degraded; acknowledgement is not confirmation | `prd.md:326-333` | `EXPERIENCE.md` **Command authorization, validation, and lifecycle** (`:169-187`); `DESIGN.md:138,156` |
| Command budgets and safety: Degraded at `10,000ms`, `1,000ms` polling up to `120,000ms`, zero pre-accept lifecycle retry, one transient dispatch retry after `250ms`, authorization, destructive confirmation, `30s` abandonment guard, FC-CNC one-at-a-time behavior | `prd.md:317-343,531` | `EXPERIENCE.md:110-114,169-187,223-235`; UJ-3; `DESIGN.md:135-139` |
| FC-NIP uses immutable pre-dispatch Command Target Identity and independently typed Material/NoOp/Unknown classification; unknowns and server-allocated keys suppress marking; no nudge/diff inference; tenant/user scope and atomic first-wins | `prd.md:301-311,466-473`; `prd-addendum-2026-09-08.md:42-51` | `EXPERIENCE.md:189-195,279,295-318`; `DESIGN.md:56-58,139,156` |
| Missing/stale tenant is explicit fail-closed state, never empty-looking data; no default tenant; reads, counts, subscriptions, and preferences are tenant/user scoped | `prd.md:511-520,526-533` | `EXPERIENCE.md:28,98-100,121-148,253-260,295-306`; `DESIGN.md:156` |
| MCP request-class disclosures, opaque tokens, host authentication, visibility gates, schema-side-effect classes, and cross-request lifecycle | `prd.md:345-396`; `prd-addendum-2026-09-08.md:70-80` | `EXPERIENCE.md:46-48,146,197-210,253-260`; UJ-4 |
| Inspect/migrate/testing generated-consumer experience and fail-closed developer states | `prd.md:398-437`; UJ-5/UJ-6 at `prd.md:89-91` | `EXPERIENCE.md:49-51,147-148,212-221`; UJ-5/UJ-6 |
| WCAG 2.2 AA, accessible names/roles/focus/keyboard/live regions/reduced motion/forced colors, color-independent status, support-safe output | `prd.md:196-201,283-290,526-534`; `prd-addendum-2026-09-08.md:62,65` | `EXPERIENCE.md` **Accessibility Floor** and **Security, Tenancy, and Support Safety**; `DESIGN.md:77-84,134,146-152` |
| Tenants is the obligated adopter; Parties is only a Product-selected D-7 fallback | `prd.md:32-34,542,657,687`; `prd-addendum-2026-09-08.md:67` | `EXPERIENCE.md:26`, UJ-1 and UJ-6 |
| Six canonical named journeys: Nina, Marc (projection), Marc (command), Ravi, Camille, Sophie | `prd.md:77-91` | `EXPERIENCE.md` **Key Flows**, UJ-1 through UJ-6 (`:282-351`) |

## Addendum qualitative-rule coverage

Every qualitative UX rule in `prd-addendum-2026-09-08.md:53-68` is represented:

- default tab naming, module-root alias, and flyout target: `EXPERIENCE.md:36-38,55-61`;
- exact `32px` compact row: `DESIGN.md:30-34,96,130`;
- always-visible hamburger and desktop rail modes: `DESIGN.md:96,121`; `EXPERIENCE.md:96,264-270`;
- useful, non-noisy live regions: `EXPERIENCE.md:85,107,113-114,152-167,223-251`;
- optional `/` shortcut: `EXPERIENCE.md:103,225`;
- Aspire-derived visual posture: `DESIGN.md:65-82`; `EXPERIENCE.md:272-280`;
- icon/tooltip/`aria-label` semantic statuses and badge counts: `DESIGN.md:81-82,134`; `EXPERIENCE.md:109,241-250`;
- urgency ordering: `EXPERIENCE.md:35,59,98,121-132`;
- Tenants/Parties reference-module disposition: `EXPERIENCE.md:26,284-351`;
- FC-NIP residual disposition, including server-allocated keys: `EXPERIENCE.md:189-195` and UJ-2/UJ-3.

## Intentional supersessions and corrections

- The current spines use the six canonical PRD journeys instead of treating the older Alex/Maya Parties examples as the complete journey set. Parties remains a fallback/example only; it does not replace Tenants without a dated D-7 decision.
- Module-entry activation now lands on the required plural-label default Module Tab. A Parties `Search` tab is non-default secondary navigation, correcting the older flow that implied direct Module selection landed on Search.
- Create-style outcomes no longer imply a fresh marker. Server-allocated target keys are explicitly suppressed; a create command is eligible only when it has the required immutable pre-dispatch identity and Material outcome.
- The full projection and lifecycle vocabularies replace abbreviated legacy state lists. Exact thresholds and recovery budgets are preserved rather than summarized as generic loading, connection, pending, or retry behavior.
- Story 9.8 / FC-NIP language is outcome-oriented: the delivery and live proof are treated as complete behavior, while Product acceptance remains a separate open gate.
- Tenants changed from provisional exemplar to obligated adopter. Parties changed from equal alternative to a no-default Product fallback.
- MCP `resources/list` disclosure is recorded as pending independent security disposition, not silently accepted. Hidden/absent equivalence is scoped exactly by request class.
- Invalid `[CommandTarget]` continues to use shipped HFC1005 behavior; the rejected HFC1071 allocation is not introduced. This diagnostic spelling remains source-owned and is not duplicated as a visual contract.
- Non-UX mechanism in addendum §§1-3 and §§5-8 is inherited by reference. Omitting release hashes, dependency graph ceilings, and evidence-ledger mechanics from the spines is deliberate separation of concerns, not a dropped product decision.

## Source authority conflicts and unsupported detail

- **UX authority conflict:** PRD D-2/D-8 still identifies legacy `ux-design.md` as canonical/sufficient, while the new spines declare themselves its successors. Finalization requires a recorded disposition: either amend the PRD/source chain or explicitly treat these spines as an approved successor snapshot.
- **Accent detail:** the PRD/addendum specify accent-as-thread but do not supply the `#0097A7` value or the exact Fluent V5 token/API role. `DESIGN.md` correctly treats the value as an `FcShellOptions.AccentColor` default derived from other project sources and leaves the Fluent role unresolved (`DESIGN.md:15-16,75-84,154-158`).
- **Responsive breakpoints:** product sources define responsive behavior but no numeric breakpoints. Both spines intentionally avoid inventing them (`DESIGN.md:100,157`; `EXPERIENCE.md:262-270,356`).
- **Expanded accessibility checks:** `EXPERIENCE.md:246-250` makes WCAG 2.2 AA criteria operational. These clauses clarify the PRD's accessibility floor and do not weaken or contradict it.

## Qualitative ideas dropped

None. Every qualitative UX rule in the addendum is carried into a named DESIGN.md or EXPERIENCE.md contract. Non-UX release, dependency, evidence, and source-inventory mechanics are intentionally inherited by reference rather than duplicated.

## Unresolved items and blockers

### Product/source-owned

- **UX authority transition:** unresolved conflict with D-2/D-8 as described above; blocker to claiming the two spines are the uncontested canonical UX authority.
- **Fluent V5 posture (OI-4 / D-13):** Product confirmation is still pending. The spines inherit the catalog pin and must not declare RC permanence (`prd.md:663,686`; `DESIGN.md:158`; `EXPERIENCE.md:357`).
- **MCP security disposition (OI-2/OI-3, G-7):** static resource-catalog disclosure, credential-validity signal, endpoint-authentication proof, and non-Development `AllowAll*` enforcement remain open. The spine records the boundary but must not describe it as security-approved (`prd.md:34,367-388,684-685`; `EXPERIENCE.md:197-210,259`).
- **Tenants evidence (G-6/OI-5):** the named bootstrap proof does not yet exist; Parties cannot substitute without a dated D-7 Product decision (`prd.md:33,587,687`; UJ-1/UJ-6).
- **FC-NIP Product acceptance (G-5/OI-1):** implementation/live proof is complete, but Product acceptance/bookkeeping remains open (`prd.md:40-41,683`). This does not reopen the UX behavior.
- **Product re-approval (G-4/D-9):** the 2026-09-08 PRD text remains pending re-approval (`prd.md:3-5,40,659`).
- **Sample-host container assumption (A3/OI-9):** local/e2e-only container use remains unconfirmed. No published-container promise was added to the spines (`prd.md:131,691,699`).

### UX-finalization gaps

- Exact visual treatment and final localized microcopy remain unresolved for Warning, NeedsReview, Degraded, missing tenant, FallbackPolling, SlowQuery, MaxItems, and fresh-row dismissal/forced-colors states (`DESIGN.md:154-158`; `EXPERIENCE.md:74-87,353-358`).
- Numeric responsive breakpoints remain unspecified; only semantic Desktop, Compact, and Narrow-browser behavior is committed.
- The exact supported Fluent V5 token/API role for the configurable accent remains unresolved.

