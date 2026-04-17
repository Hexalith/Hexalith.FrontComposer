# Epic 4: Rich DataGrid & Projection Interaction

Business user can filter, sort, and search DataGrid views; expand entity rows in-place for detail; see status badges with semantic palette, role-based rendering hints, empty states with CTAs, field descriptions as contextual help, and explicit placeholders for unsupported types.

### Story 4.1: Projection Role Hints & View Rendering

As a developer,
I want to annotate projections with role hints that change how the framework renders them, with contextual subtitles that orient business users,
So that each projection view is optimized for its purpose without writing custom rendering code.

**Acceptance Criteria:**

**Given** a projection annotated with [ProjectionRoleHint(RoleHint.ActionQueue, WhenState = "Pending,Submitted")]
**When** the view renders
**Then** items are sorted by priority with inline action buttons
**And** only items matching the specified explicit states appear
**And** the view drives home badge counts via IBadgeCountService

**Given** a projection annotated with [ProjectionRoleHint(RoleHint.StatusOverview)]
**When** the view renders
**Then** aggregate counts per badge slot are displayed
**And** clicking a status group navigates to the DataGrid filtered by that status

**Given** a projection annotated with [ProjectionRoleHint(RoleHint.DetailRecord)]
**When** the view renders
**Then** a single-entity detail view renders using FluentCard + FluentAccordion
**And** it appears inside the expand-in-row context

**Given** a projection annotated with [ProjectionRoleHint(RoleHint.Timeline)]
**When** the view renders
**Then** a chronological event list renders with timestamps and status badges in vertical timeline layout

**Given** a projection with no role hint annotation
**When** the view renders
**Then** it uses the Default rendering: standard compact DataGrid, sortable, with inline actions per density rules

**Given** any projection view
**When** the view title renders
**Then** a contextual subtitle appears below the title:
**And** ActionQueue: "[N] [entities] awaiting your [action]"
**And** StatusOverview: "[N] total across [M] statuses"
**And** Default: "[N] [entities]"

**Given** the role hint system
**When** the total hint count is evaluated
**Then** the set is permanently capped at 5-7 (ActionQueue, StatusOverview, DetailRecord, Timeline, Default)
**And** future hints use template overrides via the customization gradient

**Given** any auto-generated projection view
**When** data is being fetched
**Then** a Loading state renders with FluentSkeleton per-component placeholders matching the expected layout
**And** every generated view handles 3 states: Loading (FluentSkeleton), Empty (FcEmptyState from Story 4.6), and Data (normal rendering)

**References:** FR4, UX-DR44, UX-DR45

---

### Story 4.2: Status Badge System

As a business user,
I want to see color-coded status badges on projection items that communicate state at a glance,
So that I can scan a DataGrid and instantly identify which items need attention.

**Acceptance Criteria:**

**Given** an enum value annotated with [ProjectionBadge(BadgeSlot.Warning)]
**When** the badge renders in a DataGrid or detail view
**Then** it uses the Warning semantic color slot (--palette-yellow-*/amber)
**And** the badge includes both color AND text label (color is never the sole signal)

**Given** the 6-slot badge palette
**When** badges render
**Then** the following mappings apply:
**And** Neutral (Draft, Created, Unknown) -- neutral color
**And** Info (Submitted, InReview, Queued) -- blue
**And** Success (Approved, Confirmed, Completed, Shipped) -- green
**And** Warning (Pending, Delayed, Partial, NeedsAttention) -- amber
**And** Danger (Rejected, Cancelled, Failed, Expired) -- red
**And** Accent (Active, Running, Highlighted) -- teal

**Given** an enum value with no [ProjectionBadge] annotation or an unknown slot
**When** the badge renders
**Then** it falls back to Neutral appearance
**And** a build-time warning is emitted

**Given** a developer needs more than 6 badge states
**When** the escape path is used
**Then** a custom badge component can be provided via the customization gradient (Epic 6)
**And** the custom component must honor the accessibility contract (color + text/icon)

**Given** any badge in the application
**When** accessibility is evaluated
**Then** the badge text label is always present and readable
**And** color contrast meets WCAG AA (4.5:1 for normal text, 3:1 for UI components)

**References:** FR5, UX-DR24, UX-DR30 (color never sole signal), NFR32

---

### Story 4.3: DataGrid Filtering, Sorting & Search

As a business user,
I want to filter, sort, and search within DataGrid views with my preferences remembered across sessions,
So that I can quickly find the items I need without re-applying my filters every time.

**Acceptance Criteria:**

**Given** a DataGrid with column headers
**When** the user interacts with column filters
**Then** FluentSearch inputs appear in column headers
**And** filtering is debounced at 300ms
**And** filtering is server-side via ETag-cached query parameters
**And** keyboard shortcut "/" focuses the first column filter

**Given** a DataGrid with status badge columns
**When** status filter chips render above the DataGrid
**Then** FluentBadge toggle chips appear for each badge slot with items
**And** clicking toggles the filter: filled appearance = active, outline = inactive
**And** multiple status filters can be active simultaneously

**Given** a DataGrid with active filters
**When** the filter state is inspected
**Then** a filter visibility summary appears below the DataGrid header: "Filtered: Status = Pending, Approved | 12 of 47 orders"
**And** a "Reset filters" Outline button is available that clears all filters and persisted state

**Given** the IProjectionSearchProvider interface
**When** an adopter registers an implementation
**Then** a global search UI appears in the DataGrid header
**When** no implementation is registered
**Then** the search UI is hidden (no empty search box)

**Given** DataGrid filter state (column filters + status filters + sort order + search query)
**When** the user navigates away and returns
**Then** filter state is restored from LocalStorage keyed by bounded-context:projection-type
**And** sort order and column filter values persist across sessions

**Given** active filters produce zero results
**When** the empty filtered state renders
**Then** the message shows: "No orders match the current filters. [Reset filters] to see all 47 orders."
**And** this is visually distinct from the FcEmptyState for zero-total-items

**References:** FR12, UX-DR40, UX-DR41, NFR87

---

### Story 4.4: Virtual Scrolling & Column Prioritization

As a business user,
I want DataGrids to handle large datasets smoothly and auto-manage column visibility when projections have many fields,
So that performance stays fast and the view doesn't become unusable with wide data.

**Acceptance Criteria:**

**Given** any DataGrid
**When** virtual scrolling is configured
**Then** Fluent UI <Virtualize> is used as the default (no "load more" buttons, no page numbers)
**And** client-side virtualization is used for < 500 items
**And** server-side virtualization via ItemsProvider is used for 500+ items with ETag-cached queries
**And** a FluentSkeleton row renders at the scroll boundary during server fetch (same height as real row)

**Given** a DataGrid with 500 virtualized rows
**When** rendering performance is measured
**Then** P95 render time is < 300ms (NFR4)

**Given** an initial data fetch that takes > 2000ms
**When** the performance prompt evaluates
**Then** a FluentMessageBar (Info) suggests: "Loading is slow. Add filters to narrow results."

**Given** the MaxUnfilteredItems safety rail (default 10,000)
**When** a projection has more items than the cap
**Then** the message shows: "Showing first 10,000 items. Use filters to find specific records."
**And** a row count displays in the DataGrid header: "47 orders" or "12 of 47 orders"

**Given** a projection with > 15 fields
**When** the FcColumnPrioritizer activates
**Then** the first 8-10 columns are shown by priority ([ColumnPriority] annotation or declaration order)
**And** a "More columns ([N] hidden)" FluentButton (Outline) appears in the column header row
**And** clicking opens a panel with checkboxes to toggle column visibility
**And** column visibility selections are persisted in LocalStorage per projection type

**Given** a projection with <= 15 fields
**When** the DataGrid renders
**Then** FcColumnPrioritizer is transparent (all columns shown, no toggle)

**Given** the column toggle panel
**When** keyboard navigation is used
**Then** the toggle is keyboard-accessible
**And** the panel uses role="dialog" with a checkbox list
**And** screen reader announces "[N] columns hidden. Activate to show more."

**Given** scroll position within a DataGrid
**When** the user navigates away and returns (within-session)
**Then** scroll position is restored from the per-view memory object

**References:** FR12 (partial), UX-DR7, UX-DR42, UX-DR63, NFR4

---

### Story 4.5: Expand-in-Row Detail & Progressive Disclosure

As a business user,
I want to expand an entity row in place to see full details without losing my DataGrid context,
So that I can inspect and act on items without navigating away and losing my scroll position and filters.

**Acceptance Criteria:**

**Given** a DataGrid row
**When** the business user clicks to expand
**Then** the detail view opens in-place below the row
**And** only one row is expanded at a time (v1 constraint)
**And** the previously expanded row collapses

**Given** a projection with <= 12 fields in detail view
**When** the expanded detail renders
**Then** all fields are visible immediately in a 3-column grid within the expanded area

**Given** a projection with > 12 fields in detail view
**When** the expanded detail renders
**Then** primary fields (first 6-8) are visible immediately
**And** secondary fields are grouped into collapsible FluentAccordion sections
**And** sections are labeled by field group (from domain model property grouping annotations, or alphabetical chunking fallback)

**Given** a row expansion on desktop
**When** the expand animation runs
**Then** the expanded row's top edge is pinned to the current viewport position
**And** content below pushes down
**And** scrollIntoView with block:'nearest' plus requestAnimationFrame stabilizes the DOM reflow
**And** animation uses Fluent UI standard expand/collapse transition
**And** prefers-reduced-motion makes the transition instant

**Given** a row collapse
**When** the collapse completes
**Then** scroll position adjusts to keep the next row at natural attention position

**Given** a phone viewport (< 768px)
**When** inline action buttons would normally render on DataGrid rows
**Then** inline actions are hidden via CSS media query
**And** tapping a row expands it (expand-in-row pattern)
**And** action buttons appear inside the expanded detail view
**And** each row is at comfortable density (one row per item for scannability)

**References:** FR20, UX-DR17, UX-DR18, UX-DR62, UX-DR30 (prefers-reduced-motion)

---

### Story 4.6: Empty States, Field Descriptions & Unsupported Types

As a business user,
I want helpful empty states that guide me toward action, field descriptions that explain what columns mean, and clear indicators for any fields the framework can't auto-render,
So that I'm never confused by blank screens, cryptic column names, or missing data.

**Acceptance Criteria:**

**Given** a projection with zero total items
**When** the FcEmptyState renders
**Then** it displays: a large muted FluentIcon, primary message "[No {entity plural}] yet." with entity name humanized from projection type
**And** if the user has available commands, a CTA button "Send your first [Command Name]" appears
**And** if no commands are available (read-only projection), the message appears without CTA
**And** optional secondary text from resource files is shown if available
**And** accessibility: role="status", aria-label="No [entities] found", CTA keyboard-focusable

**Given** the empty filtered state (filters active, zero matches, but total items > 0)
**When** the state renders
**Then** it is visually distinct from FcEmptyState
**And** shows: "No orders match the current filters. [Reset filters] to see all 47 orders."

**Given** a developer adds [Description("...")] or [Display(Description="...")] to a projection property
**When** the DataGrid column header renders
**Then** the description surfaces as contextual help via tooltip on hover
**And** in detail/form views, the description appears as an inline label below the field

**Given** a projection with an unsupported field type (e.g., Dictionary<string, List<T>>)
**When** the auto-generated view renders
**Then** FcFieldPlaceholder renders: FluentCard with dashed border, FluentIcon (Warning), field name, type annotation, message "This field requires a custom renderer", and FluentAnchor link to customization gradient docs
**And** a build-time warning is emitted for each unsupported field
**And** the field is never silently omitted (zero silent omissions)
**And** accessibility: role="status", aria-label="[Field name] requires custom renderer", focusable in tab order

**Given** dev-mode overlay is active (Ctrl+Shift+D, Epic 6 scope)
**When** an unsupported field is present
**Then** it is highlighted with a red-dashed border with exact unsupported type name and recommended override level

**References:** FR9, FR10, FR11, UX-DR3, UX-DR4, UX-DR55 (auto-generation boundary protocol)

---

**Epic 4 Summary:**
- 6 stories covering all 7 FRs (FR4, FR5, FR9, FR10, FR11, FR12, FR20)
- Relevant NFRs woven into acceptance criteria (NFR4, NFR29-34, NFR87)
- Relevant UX-DRs addressed (UX-DR3-4, UX-DR7, UX-DR17-18, UX-DR24, UX-DR35, UX-DR40-42, UX-DR44-45, UX-DR55, UX-DR62-63)
- Stories are sequentially completable: 4.1 (role hints) -> 4.2 (badges) -> 4.3 (filtering) -> 4.4 (virtual scrolling) -> 4.5 (expand-in-row) -> 4.6 (empty states/descriptions)

---
