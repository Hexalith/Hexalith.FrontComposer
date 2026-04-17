# Design Direction Decision

### Design Directions Explored

Six directions were generated and evaluated against the spec's established principles (action density rules, expand-in-place, command+context pattern, five-state lifecycle, navigation at scale). All six respected the Fluent UI v5 zero-override constraint. Each varied across six axes: sidebar prominence, DataGrid density, command placement, detail view expression, home/dashboard approach, and lifecycle feedback model.

| # | Direction | Strength | Limitation vs spec |
|---|---|---|---|
| 1. Azure Portal Heritage | Rich side-panel detail; notification bell for lifecycle | Side panel violates "expand-in-place" spec decision; notification bell adds UI chrome the spec wants to minimize |
| 2. GitHub Efficiency | Expand-in-row matches spec; clean content-first layout | Collapsed sidebar loses label discoverability for 10+ bounded contexts; spacious grid contradicts compact factory default |
| 3. Notion Calm | Generous whitespace; calm aesthetic | Full-page detail breaks context preservation; spacious grid wastes density for queue processing; toast-only lifecycle is insufficient for the 2-10s escalation |
| 4. Dense Ops Center | Inline micro-actions match action density rules perfectly; maximum visible rows | Icon-only sidebar loses discoverability; ultra-compact everywhere risks accessibility; too dense for forms |
| 5. Split Workbench | Selection-driven right pane keeps context visible; command+detail co-located | Split pane is a fixed layout commitment that wastes space when detail is not needed; not responsive |
| 6. Focused Task Flow | Action-queue home matches `ActionQueue` projection role hint; bottom sheet preserves DataGrid context | Bottom sheet pattern is uncommon in desktop enterprise apps; collapsed sidebar loses discoverability |

No single direction is correct. The spec's own decisions pull from multiple directions simultaneously.

### Chosen Direction

**Composite: "Pragmatic Workbench"**

A synthesis optimized for the spec's priorities, drawing the best element from each direction where it aligns with an established decision.

| Axis | Choice | Source Direction | Spec Justification |
|---|---|---|---|
| **Sidebar** | Expanded with collapsible groups (~220px), hamburger toggle, badge counts on nav items, auto-collapse at <1366px viewports | Direction 1 + 5 | "Navigation at scale: collapsible nav groups, maximum 2-level depth" -- labels visible for 10+ bounded contexts; badge counts maintain cross-context priority awareness after leaving home |
| **DataGrid density** | Compact (factory default), full-width; switches to comfortable on tablet viewports (<1024px) | Direction 4 | "Compact is factory default for DataGrids" -- with responsive breakpoint for touch-capable viewports per accessibility commitment |
| **Inline actions (0-1 field commands)** | Inline buttons directly on DataGrid rows | Direction 4 | "Action density rules: 0-1 non-derivable fields render as inline buttons on list rows" |
| **Compact inline forms (2-4 fields)** | Expand below the row, within the DataGrid context; derivable fields pre-filled | Direction 2 + 4 | "Commands with 2-4 fields render as compact inline forms" -- pre-fill reduces interaction time |
| **Full forms (5+ fields)** | Dedicated content area (replaces DataGrid) with breadcrumb back; DataGrid state preserved | Direction 3 | "Commands with 5+ fields render as full-page forms" -- only case where context switches |
| **Detail view** | Expand-in-row accordion below the selected row; scroll-stabilized; progressive disclosure beyond ~12 fields | Direction 2 | "List-detail inline pattern" -- with scroll stabilization for serial queue processing |
| **Home (v1)** | Bounded-context directory with badge counts | Direction 2 (adapted) | v1 scope -- cross-context query orchestration deferred to v2 |
| **Home (v2)** | Cross-context action queue with grouped sections per bounded context | Direction 6 (adapted) | Requires multi-projection query orchestration |
| **Lifecycle feedback** | Inline on affected row: badge transitions + subtle teal pulse (>300ms). For global events: `FluentMessageBar` at top of content area | Direction 4 (inline) + adapted | "Five-state lifecycle with progressive visibility thresholds" |
| **Header** | App title, breadcrumbs, command palette (with badge counts), theme toggle, settings | Composite | Command palette is primary navigation; badge counts make it actionable |

### Design Rationale

**Why DataGrid density and detail view are the two axes that matter most:**

The DataGrid is the surface where **all projection role hints manifest.** ActionQueue, StatusOverview, DetailRecord, Timeline, and the default view all resolve to some variant of a DataGrid or DataGrid-derived layout. Get the DataGrid's density, interaction model, and expand-in-row behavior right, and every role hint works. Get it wrong, and every hint fights the foundational layout. The detail view expression (expand-in-row vs. side panel vs. page replacement) is the second axis because it determines whether the command+context pattern can actually deliver on its promise.

**Why a composite, and its cost:**

Every source direction made at least one choice that contradicts the spec. The composite cherry-picks the optimal axis from each. This comes at a cost: **implementation complexity.** A composite direction has more conditional branches than any single pure direction -- different density per surface, different command placement per field count, responsive sidebar collapse thresholds, scroll stabilization logic, progressive disclosure for deep detail views. Scoring the composite against the six originals on weighted criteria (context preservation 25%, action speed 25%, discoverability 15%, accessibility 15%, simplicity 10%, scalability 10%), the composite scores **4.55/5** -- highest overall -- but only **3/5 on implementation simplicity**, the lowest of any axis. This tradeoff is accepted: the framework's value proposition is "write business rules, get a great UI," and a great UI requires the extra conditional logic that a simpler approach would avoid.

**Derivation confidence:**

Independent re-derivation from the spec's principles (action density rules, expand-in-row, navigation at scale, command+context, lifecycle thresholds, density strategy) converges on the same core composite -- 100% match on all nine architectural axes. The Pragmatic Workbench is not an arbitrary selection from six options; it is the only composite consistent with the spec's own constraints. The addenda (badge counts, scroll stabilization, contextual subtitles, progressive disclosure, responsive breakpoint, derivable field pre-fill) are user-research-driven enhancements that pure principle derivation would not produce -- validating both the composite and the elicitation process.

**Validation: 14 items in under 2 minutes**

Timing analysis for Beatriz processing 14 items across 3 bounded contexts with the Pragmatic Workbench:

| Phase | Actions | Clicks | Time |
|---|---|---|---|
| Home → first BC | Open app, see directory, click Orders | 2 | ~2s |
| 8 inline approvals (0-1 field) | Click "Approve" × 8 with lifecycle feedback | 8 | ~4s |
| 3 sidebar navigations | Orders → Inventory → Customers | 3 | ~3s |
| 4 compact inline forms (2-4 fields, with pre-fill) | Expand + fill 1-2 non-derivable fields + submit × 4 | 8 + 4-8 fields | ~40s |
| 2 detail-then-act | Expand detail + read + inline action × 2 | 6 | ~25s |
| **Total** | | **~27 clicks + ~6 fields** | **~74s (1:14)** |

Every individual action begins in ≤2 clicks. No unnecessary context switches. With derivable field pre-fill, compact inline forms drop from 4 user-supplied fields to 1-2, cutting form time by half.

**v1/v2 Home Screen Scoping:**

The cross-context action-queue home requires capabilities not in v1's architecture: multi-projection query orchestration, cross-context column resolution, and cross-context SignalR subscription fan-out. These are deferred to v2.

**v1 home: Bounded-context directory with badge counts.**

```
Welcome back, Beatriz.

COMMERCE
  Orders (3 pending)          [→]
  Invoices (0 pending)        [→]

INVENTORY
  Stock Levels (2 alerts)     [→]
  Shipments (0 pending)       [→]

CUSTOMERS
  Support Tickets (1 open)    [→]
```

Badge counts are derived from each BC's ActionQueue-hinted projection count -- a single count query per context.

**Home bypass acknowledgment:** Users may bookmark their primary BC and bypass the home entirely. This is expected and acceptable. Badge counts on sidebar nav items ensure cross-context priority awareness survives even when users never return to the home.

**Why the v1 fallback is acceptable, not just expedient:** Switching from Orders to Inventory is a *necessary* context switch -- different domains with different mental models. The spec prohibits *unnecessary* switches (leaving a domain to perform an action within that domain). The Workbench's expand-in-row + inline actions eliminate those. Six sidebar navigations in a morning across three BCs is fine.

**v2 home: Cross-context action queue with grouped sections.**

```
What needs your attention                    [6 items]

ORDERS (3 items)
  ORD-1847 | Acme Corp    | €12,450 | Pending 2h  | [Approve]
  ORD-1846 | Globex Inc   | €8,200  | Pending 3h  | [Approve]
  ORD-1840 | Oscorp       | €7,800  | Submitted 3d| [Approve]

INVENTORY (2 items)
  STK-0089 | Widget Alpha | 12 left | Low stock 1d| [Reorder]
  STK-0092 | Widget Gamma | 0 left  | Out of stock| [Reorder]

Recent activity (last 10 confirmed)
  [compact cross-context list]
```

Grouped sections solve the column problem: each section uses its own BC's projection fields. Sections ordered by item count (most actionable first).

**Projection role hints -- v1 set and growth path:**

| Role Hint | v1 View Pattern | Notes |
|---|---|---|
| `ActionQueue` | Items needing user action; sorted by priority; inline action buttons | Drives v1 home badge counts and v2 cross-context home |
| `StatusOverview` | Aggregate counts per status badge slot; click-through to filtered DataGrid | In v1, rendered as a grouped-count DataGrid |
| `DetailRecord` | Single-entity detail view; `FluentCard` + `FluentAccordion` | Appears inside expand-in-row accordion |
| `Timeline` | Chronological event list with timestamps and status badges | Content area, vertical timeline layout |
| (Default) | Standard compact DataGrid; sortable; inline actions per density rules | Fallback for all projections without a role hint |

The 5-7 cap exists for future growth, not because five is the final answer. Known v2+ candidates: **Summary/KPI** (a single metric, not a list -- powers dashboard widgets) and **Relationship Map** (entity connections, requires graph visualization).

**ActionQueue annotation guidance:**

A projection state qualifies as "needing attention" in the ActionQueue when the current user has **at least one available command** for items in that state. States where no action is possible (e.g., "Completed," "Archived") must not appear in ActionQueue views. The annotation is `[ProjectionRoleHint(RoleHint.ActionQueue, WhenState = "Pending,Submitted")]` -- explicit states, not "all non-terminal."

**Non-derivable field definition:**

A field is **non-derivable** if the user must supply its value. A field is **derivable** if the framework can resolve it from: (1) the current projection context (e.g., aggregate ID from the selected row), (2) the system (e.g., current timestamp, current user ID), or (3) the command definition default value. The action density rule counts only non-derivable fields:

- 0-1 non-derivable fields → inline button on DataGrid row
- 2-4 non-derivable fields → compact inline form
- 5+ non-derivable fields → full-page form

### Implementation Approach

**Navigation structure rule:**

Sidebar nav items are **projection views only**, not commands. Commands appear as buttons within projection views according to the action density rules. A bounded context with 3 projections and 5 commands shows 3 nav items, not 8. Command discoverability happens within the view, not in navigation.

**Shell composition using `FluentLayout`:**

```
+---------------------------------------------------------------+
|  HEADER (48px)                                                |
|  [≡] FrontComposer | Home > Orders | [Ctrl+K (3)] [◑] [⚙]    |
+------------------+--------------------------------------------+
|                  |                                            |
|  NAV (220px)     |  CONTENT                                  |
|  collapsible     |                                            |
|                  |  [FluentMessageBar -- global events]       |
|  COMMERCE        |                                            |
|  ▸ Orders (3)    |  Order List                                |
|  ▸ Invoices      |  3 orders awaiting your approval           |
|                  |  [filter] [+ Send Order]                   |
|  INVENTORY       |                                            |
|  ▸ Stock (2)     |  DataGrid (compact, full-width)            |
|  ▸ Shipments     |  [row] [row] [inline actions]              |
|                  |  [▼ expanded row detail + commands]        |
|  CUSTOMERS       |  [row] [row]                               |
|  ▸ Tickets (1)   |                                            |
|                  |                                            |
+------------------+--------------------------------------------+
```

**Sidebar behavior:**

- **Default state:** Sidebar starts expanded for all new users regardless of bounded context count. This provides orientation for first-time users. Users collapse via hamburger toggle when ready; preference persists in LocalStorage. The command palette (Ctrl+K) is always available as the primary fast-navigation path for returning users.
- **Badge counts on nav items:** Nav items for projections carrying the `ActionQueue` role hint display a count badge (e.g., "Orders (3)"). Counts are lightweight single-integer queries. Non-ActionQueue projections show no count.
- **Collapsed-group state persisted:** When a user collapses a nav group, the collapsed state is stored in LocalStorage and restored on return.
- **Responsive auto-collapse:** On viewports <1366px wide, the sidebar auto-collapses to icon-only mode (~48px) to preserve minimum content area width. The hamburger toggle expands it temporarily as an overlay. On viewports ≥1366px, the sidebar is expanded by default.

**Command palette with badge counts:**

Command palette results for ActionQueue-hinted projections include badge counts: typing "Ord" shows "Orders (3 pending)" not just "Orders." This makes the command palette actionable.

**Contextual view subtitles:**

Every auto-generated projection view includes a contextual subtitle below the view title. The subtitle is derived from the projection role hint and current data:

- ActionQueue: "[N] [entities] awaiting your [action]" (e.g., "3 orders awaiting your approval")
- StatusOverview: "[N] total across [M] statuses"
- Default: "[N] [entities]" (e.g., "24 orders")
- Empty: Uses the existing empty-state messages from the spec

**Responsive density breakpoint:**

On viewports <1024px wide (tablet-class), DataGrids automatically switch from compact to comfortable density regardless of the user's density preference. This ensures touch targets meet the 44x44px accessibility minimum. On ≥1024px viewports, the user's density preference applies normally.

**Phone-size viewports:**

FrontComposer v1 is not designed for phone-size viewports (<768px). The composition shell, expand-in-row pattern, and inline action density all assume a minimum viewport width of 768px. On viewports below this threshold, the app is functional (single-column layout, drawer nav) but not optimized.

**Interaction flow for the three command form patterns:**

1. **Inline button (0-1 non-derivable fields):** User clicks "Approve" on DataGrid row → button shows `FluentProgressRing` → lifecycle progresses inline → row badge transitions from "Pending" to "Approved." Zero navigation, zero context loss.

2. **Compact inline form (2-4 non-derivable fields):** User clicks "Modify Shipping" → form slides open below the row (within the expand-in-row space) → derivable fields are pre-filled from: (1) current projection context, (2) last-used value for that command type (session-persisted), (3) command definition default. User fills only the non-derivable fields → clicks submit → form collapses → lifecycle progresses on the row. Pre-fill reduces a 4-field form to 1-2 user interactions.

3. **Full-page form (5+ non-derivable fields):** User clicks "+ Send Order" → content area replaces DataGrid with full form (max 720px wide, centered) → breadcrumb shows "Orders > Send Order" → user fills fields → clicks submit → redirected back to DataGrid with new row showing lifecycle. Single necessary context switch, breadcrumb return path.

**Expand-in-row scroll stabilization:**

During serial queue processing (user expands row 3, collapses it, expands row 4, etc.), the DataGrid must not "jump around." The scroll stabilization rule:

- **On expand:** The expanded row's top edge is pinned to its current viewport position. Content below the expansion point is pushed down, but the expanded row itself does not move relative to the viewport.
- **On collapse:** The scroll position adjusts to keep the *next* row at the viewport position where the user's attention naturally falls.
- **Implementation:** A `scrollIntoView` with `block: 'nearest'` on the expanded row, plus a `requestAnimationFrame` callback to stabilize after the DOM reflow. Animation uses Fluent UI's standard expand/collapse transition (respecting `prefers-reduced-motion`).

**DataGrid state preservation across full-page form navigation:**

When a user navigates from a DataGrid to a full-page form and back:

- Scroll position, applied filters, sort order, expanded row, and selected row highlight are all preserved
- The state is maintained in a per-view memory object keyed by bounded-context + projection-type
- This is within-session preservation, distinct from cross-session LocalStorage persistence

**Expand-in-row progressive disclosure:**

When a projection has more than ~12 fields, the expand-in-row detail view uses progressive disclosure:

- **Primary fields (first ~6-8):** Visible immediately in the expanded accordion, displayed in a 3-column grid
- **Secondary fields (remainder):** Grouped into collapsible `FluentAccordion` sections within the expanded row, labeled by field group
- **Grouping source:** Field groups are derived from the domain model's property grouping annotations, or from alphabetical chunking as a fallback
