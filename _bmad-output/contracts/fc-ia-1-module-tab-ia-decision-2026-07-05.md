# FC-IA-1 Module-Tab Route Encoding and Projection-Flyout IA Decision

Status: approved for Story 11.7 implementation
Date: 2026-07-05
Owner: Product/UX + Architect
Story: FC-IA-1 gate (recorded in `ux-experience-2026-07-05.md`), consumed by Story 11.7
Sign-off: approved-by-administrator-2026-07-05 (recommended answers accepted as-is)
Source: `sprint-change-proposal-2026-07-05-readiness-major-issues.md` (Issue 4)

## Decision

The four open UX information-architecture questions are resolved as follows. These answers are the
canonical IA contract for module workspaces and navigation-route work; Story 11.7 and any other
navigation/module-tab route story must implement to them.

### 1. Required default tab

Every module workspace opens on a **required default tab**.

- Default tab name = the **module plural label** (e.g. `Parties`), falling back to `Overview` when a
  module does not define a plural label.
- The default tab preserves the **one-entry-per-module** rule: the module has exactly one primary
  application-level navigation entry; tabs switch UX pages *inside* that entry.

### 2. Tab selection encoding

Module tab selection is encoded as a **route-path segment**:

```text
/{module}/{tab}
```

- This is deep-linkable and shareable (browser refresh, support links, CTAs, command-result links, and
  e2e tests can land on a specific tab).
- It is **consistent with the Story 11.0 command route contract**
  `/commands/{BoundedContext}/{CommandTypeName}` (path-encoded, not query-encoded).
- Tab selection is **not** encoded in the query string and **not** held only in internal router state.
- The default tab route may render at the module root `/{module}` (equivalent to `/{module}/{default}`)
  so the module root is a valid, shareable entry point.

### 3. Projection flyout

The shell rail **keeps a secondary projection flyout**, constrained as strictly secondary:

- The flyout routes **into module workspace tabs**; it never becomes a second primary IA.
- It preserves the one-entry-per-module rule — a module may expose counts, badges, or flyout links only
  if they route into the module workspace/tab model.
- This reconciles the Epic 8 projection-flyout behavior with the UX experience spine: any flyout is
  secondary and routes into module workspace tabs, not a competing top-level menu.

### 4. First reference module

The **first visual reference implementation is `Tenants`** (already mid Fluent v5 conversion).

- Provisional; `Parties` remains an acceptable alternative if Product later prefers a domain-richer
  exemplar. Changing the reference module does not reopen decisions 1–3.

## Route Shape Summary

| Segment | Contract |
| --- | --- |
| Module root | `/{module}` renders the module workspace on its required default tab. |
| Tab | `/{module}/{tab}` selects a module UX page tab; path-encoded, casing per the module route-segment convention. |
| Default tab | Module plural label, fallback `Overview`. `/{module}` ≡ `/{module}/{default}`. |
| Flyout target | Rail projection-flyout links resolve to `/{module}/{tab}` inside the module workspace; never a new top-level entry. |
| Command activation | Unchanged — `/commands/{BoundedContext}/{CommandTypeName}` per FC-ROUTE (Story 11.0). Palette/CTA command activation lands there; palette/CTA module-page activation lands on `/{module}/{tab}`. |

## Reconciliation

- Supersedes the older navigation patterns that exposed individual projections or module UX pages as
  primary shell menu items (see `ux-experience-2026-07-05.md` Brownfield Reconciliation).
- Epic 8 projection flyout is retained but re-scoped as strictly secondary routing into tabs.
- FC-ROUTE (`fc-route-generated-command-route-contract-2026-07-05.md`) remains the command route
  authority; this contract owns module-page/tab routes and the flyout constraint, and is deliberately
  path-consistent with it.

## What Story 11.7 must do

- Consume this contract alongside FC-ROUTE.
- Ensure palette command entries, projection empty-state CTAs, and generated command pages target real
  routes, and that module-page/tab activation targets `/{module}/{tab}`.
- Keep the projection flyout secondary and routing into module workspace tabs.
- Record the e2e route pin against both the command route contract and this module-tab contract.
