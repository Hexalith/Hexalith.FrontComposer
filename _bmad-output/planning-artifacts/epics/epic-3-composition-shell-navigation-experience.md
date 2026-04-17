# Epic 3: Composition Shell & Navigation Experience

Business user can navigate bounded contexts via a collapsible sidebar, toggle Light/Dark/System themes, set display density, invoke a command palette (Ctrl+K), resume prior sessions, and discover new capabilities via "New" badges.

### Story 3.1: Shell Layout, Theme & Typography

As a business user,
I want a well-structured application shell with a configurable theme and consistent typography,
So that the composed application feels professional and I can switch between light and dark modes based on my preference.

**Acceptance Criteria:**

**Given** the application shell renders
**When** the layout is inspected
**Then** FluentLayout + FluentLayoutItem compose three areas: Header (48px), Navigation sidebar (~220px expanded, ~48px collapsed), Content area
**And** the Header contains: app title, breadcrumbs, Ctrl+K command palette trigger icon, theme toggle, settings icon
**And** forms in the Content area constrain to max 720px width
**And** DataGrids render at full content area width

**Given** the shell's accent color
**When** the developer inspects the configuration
**Then** the default accent color is #0097A7 (--accent-base-color)
**And** the accent color is overridable at deployment via configuration
**And** no custom CSS overrides are applied on Fluent UI components (zero-override strategy)

**Given** the theme toggle in the header
**When** the business user selects Light, Dark, or System
**Then** the theme switches instantly via Fluent UI <fluent-design-theme> at the shell layer
**And** System mode follows OS preference via prefers-color-scheme media query
**And** the selected theme is persisted in LocalStorage
**And** the theme is restored on return visits

**Given** the color system
**When** semantic tokens are inspected
**Then** six semantic color slots are defined: Accent (#0097A7), Neutral (shell chrome/borders), Success (confirmed/approved), Warning (pending/stale), Danger (rejected/destructive), Info (informational/"New" badges)
**And** command lifecycle states map to these slots: Idle=Neutral, Submitting=Accent, Acknowledged=Neutral, Syncing=Accent, Confirmed=Success, Rejected=Danger

**Given** the Typography API
**When** a component references Hexalith.FrontComposer.Typography constants
**Then** the following mappings are available: AppTitle=Title1, BoundedContextHeading=Subtitle1, ViewTitle=Title3, SectionHeading=Subtitle2, FieldLabel=Body1Strong, Body=Body1, Secondary=Body2, Caption=Caption1, Code=Body1+monospace
**And** no mapping changes occur in patch versions; minor version changes are documented with before/after screenshots

**Given** the IStorageService abstraction for real persistence
**When** the application runs in Blazor WebAssembly mode
**Then** LocalStorageService is available as the IStorageService implementation
**And** it supports LRU eviction with configurable max entries
**And** SetAsync is fire-and-forget (does not block render)
**And** FlushAsync is called via beforeunload JS interop hook in App.razor

**Given** framework-generated UI strings
**When** localization is configured
**Then** English and French resource files are provided via IFluentLocalizer (UX-DR60)
**And** the label resolution chain applies to all typographic elements
**And** adopters can provide additional language translations via IStringLocalizer

**References:** FR14, FR15, UX-DR14, UX-DR23, UX-DR25, UX-DR26, UX-DR57, UX-DR60

---

### Story 3.2: Sidebar Navigation & Responsive Behavior

As a business user,
I want a collapsible sidebar with bounded context navigation groups that adapts to my screen size,
So that I can quickly navigate between domains and still have a usable experience on smaller screens.

**Acceptance Criteria:**

**Given** bounded contexts are registered with the framework
**When** the sidebar renders
**Then** each bounded context appears as a collapsible FluentNav group
**And** nav groups support up to two levels of hierarchy depth
**And** nav items are projection views only (commands are not nav items)
**And** collapsed-group state is persisted in LocalStorage

**Given** the sidebar toggle
**When** the business user clicks the FluentLayoutHamburger toggle
**Then** the sidebar collapses to icon-only (~48px) or expands (~220px)
**And** the preference is persisted in LocalStorage
**And** all new users start with the sidebar expanded

**Given** a desktop viewport (>=1366px)
**When** the shell renders
**Then** the sidebar is expanded by default with full labels and group hierarchy

**Given** a compact desktop viewport (1024-1365px)
**When** the shell renders
**Then** the sidebar auto-collapses to icon-only (~48px)
**And** the hamburger toggle expands it as an overlay
**And** breadcrumbs may truncate

**Given** a tablet viewport (768-1023px)
**When** the shell renders
**Then** the sidebar renders as a drawer navigation
**And** density is forced to comfortable for 44px touch targets
**And** drawer nav items are at least 48px tall

**Given** a phone viewport (<768px)
**When** the shell renders
**Then** the layout is single-column with drawer navigation
**And** dev-mode overlay is not supported at this viewport

**Given** keyboard navigation
**When** the user tabs through sidebar items
**Then** focus is visible with Fluent --colorStrokeFocus2
**And** all nav items are reachable via keyboard in DOM order

**References:** FR17, UX-DR15, UX-DR28, UX-DR29, UX-DR30 (keyboard parity), NFR89 (<=2 clicks)

---

### Story 3.3: Display Density & User Settings

As a business user,
I want to choose my preferred display density and access settings easily,
So that the application matches my work style -- compact for scanning, roomy for detailed work.

**Acceptance Criteria:**

**Given** the density system
**When** the user has not set a preference
**Then** the 4-tier precedence applies: (1) user preference in LocalStorage, (2) deployment-wide default via config, (3) factory hybrid defaults (compact for DataGrids/dev-mode, comfortable for detail views/forms/nav sidebar), (4) per-component default
**And** the density is applied via --fc-density CSS custom property on <body>

**Given** the settings icon in the header (Ctrl+, shortcut)
**When** the user opens the settings panel
**Then** a FluentDialog renders with: density radio options (Compact, Comfortable, Roomy), theme selector, and a live preview showing one DataGrid row + one form field + one nav item
**And** changes take effect immediately in the preview

**Given** the user selects a density preference
**When** the preference is applied
**Then** all generated views update to the selected density
**And** the preference is stored in LocalStorage
**And** the preference persists across sessions

**Given** a viewport < 1024px
**When** the responsive density override applies
**Then** density is forced to comfortable regardless of user preference
**And** this ensures 44x44px minimum touch targets
**And** components not inheriting density (command palette results, filter badges) apply responsive CSS padding

**Given** the Roomy density level
**When** it is selected
**Then** it is a permanent first-class feature (never removed)
**And** it is designed to support accessibility scenarios requiring larger touch targets and more whitespace

**References:** FR16, UX-DR27, UX-DR29, UX-DR30 (Roomy as accessibility feature)

---

### Story 3.4: FcCommandPalette & Keyboard Shortcuts

As a business user,
I want a command palette that lets me fuzzy-search across all projections, commands, and recent views from anywhere in the application,
So that I can navigate and take action in 2 keystrokes instead of clicking through menus.

**Acceptance Criteria:**

**Given** the user is anywhere in the application
**When** Ctrl+K is pressed or the header palette icon is clicked
**Then** the FcCommandPalette overlay opens with FluentSearch auto-focused
**And** the overlay has role="dialog" with aria-label, focus trap, and aria-activedescendant tracking

**Given** the command palette is open
**When** the user types a search query
**Then** results appear after 150ms debounce with fuzzy matching against bounded context names, projection names, and command names
**And** results are categorized: Projections, Commands, Recent
**And** badge counts from IBadgeCountService appear on ActionQueue-hinted projection results
**And** search response time is < 100ms (NFR5)

**Given** the command palette is invoked from within a bounded context
**When** results render
**Then** commands for the current bounded context appear first (contextual mode)
**And** cross-context results follow

**Given** the command palette results
**When** the user navigates with keyboard
**Then** arrow keys move between results, Enter selects, Escape closes
**And** results use role="listbox" / role="option"
**And** screen reader announces result count per keystroke

**Given** the user types "shortcuts" in the command palette
**When** results render
**Then** a complete shortcut reference is displayed

**Given** IShortcutService is registered
**When** framework shortcuts are inspected
**Then** Ctrl+K (open command palette), Ctrl+, (open settings), g h (go to home) are registered at shell level
**And** adopter custom components that register conflicting shortcuts produce a build-time warning
**And** native keyboard behaviors (Escape for dialogs, arrows for lists) rely on ARIA roles and DOM focus, not IShortcutService

**Given** IBadgeCountService may not yet be available (Story 3.5)
**When** the command palette renders before Story 3.5 is implemented
**Then** badge counts gracefully degrade to not shown (no errors, no empty badges)
**And** once IBadgeCountService is registered (Story 3.5), counts appear automatically without palette changes

**References:** FR18, UX-DR1, UX-DR43, UX-DR67, UX-DR68, NFR5, NFR89

---

### Story 3.5: Home Directory, Badge Counts & New Capability Discovery

As a business user,
I want a home page that shows me what needs attention across all domains, with live badge counts and subtle indicators for newly available capabilities,
So that I can prioritize my work and notice new features without being disrupted by announcements.

**Acceptance Criteria:**

**Given** IBadgeCountService is registered
**When** the application starts
**Then** the service fetches initial ActionQueue badge counts via parallel lightweight queries
**And** subscribes to SignalR hub for projection update events filtered to ActionQueue-hinted types
**And** Counts (IReadOnlyDictionary<ProjectionType, int>), CountChanged (IObservable<BadgeCountChangedArgs>), and TotalActionableItems (int) are available
**And** scope is per-circuit in Blazor Server, singleton in Blazor WebAssembly

**Given** the FcHomeDirectory renders as the v1 home page
**When** there are items needing attention
**Then** a global subtitle displays: "Welcome back, [user name]. You have [N] items needing attention across [M] areas."
**And** bounded context cards are sorted by badge count descending (urgency ranking)
**And** each card shows: group name, projection entries with badge counts, click-through arrow
**And** zero-urgency contexts are listed in a collapsed "Other areas" section

**Given** the home directory
**When** no items need attention across any context
**Then** "All caught up" message displays

**Given** the home directory
**When** no bounded contexts are registered
**Then** an empty state displays with a getting-started guide link

**Given** the home directory
**When** data is loading
**Then** FluentSkeleton cards render as loading placeholders

**Given** the home directory has role="main" landmark
**When** keyboard navigation is used
**Then** cards are navigable with role="link"
**And** badge counts are included in aria-label
**And** sort order is communicated via aria-description="Sorted by urgency"

**Given** a new bounded context or projection becomes available
**When** it appears in navigation for the first time
**Then** a subtle "New" badge (Info color slot) is shown
**And** the badge is removed after the user's first visit
**And** the nav entry appears only when at least one projection contains data (no empty nav entries)

**Given** badge counts in the sidebar nav
**When** the SignalR hub emits projection update events
**Then** sidebar badge counts update in real-time via IBadgeCountService
**And** command palette results also reflect updated counts

**References:** FR21, UX-DR10, UX-DR13, UX-DR52, UX-DR69, UX-DR70, NFR87

---

### Story 3.6: Session Persistence & Context Restoration

As a business user,
I want to return to exactly where I left off -- same navigation section, same filters, same scroll position,
So that context switches (lunch, meetings, browser restarts) don't cost me time re-establishing my workspace.

**Acceptance Criteria:**

**Given** a returning user with prior session state in LocalStorage
**When** the application loads
**Then** the user lands on their last active navigation section
**And** last applied filters per DataGrid are restored
**And** last sort order is restored
**And** last expanded row is restored
**And** the experience matches NFR90 (session resumption)

**Given** a first-visit user with no session state
**When** the application loads
**Then** the user sees the FcHomeDirectory sorted by badge count descending
**And** no error or empty state is shown

**Given** LocalStorage is unavailable (IT policy, full, private browsing)
**When** the application loads
**Then** the user starts from the home directory without error
**And** no error messages, warnings, or degraded UI indicators are shown
**And** the application functions normally without persistence

**Given** session state is being persisted
**When** state changes occur (navigation, filter change, sort change, row expansion)
**Then** state is written to LocalStorage with compact JSON schema
**And** keys follow the pattern bounded-context:projection-type for per-DataGrid state
**And** only UI preference state is stored -- zero PII, zero business data (NFR17)

**Given** the user navigates to a DataGrid, applies filters, sorts, and expands a row
**When** the user navigates away and returns
**Then** all DataGrid state (scroll position, filters, sort, expanded row, selected row highlight) is restored from the per-view memory object (within-session)
**And** cross-session state (filters, sort, expanded row) is restored from LocalStorage

**Given** filter values persisted in LocalStorage
**When** the persistence mechanism stores filter state
**Then** only filter metadata is stored (column name, operator, filter text) -- not full business entity data
**And** if filter text contains business data (e.g., customer name used as filter), this is acknowledged as user-initiated browser-local storage with the same trust model as browser history
**And** no server-side business data is proactively written to LocalStorage by the framework

**References:** FR19, UX-DR20, UX-DR53, NFR17, NFR90

---

**Epic 3 Summary:**
- 6 stories covering all 7 FRs (FR14, FR15, FR16, FR17, FR18, FR19, FR21)
- Relevant NFRs woven into acceptance criteria (NFR5, NFR17, NFR29-34, NFR87, NFR89, NFR90)
- Relevant UX-DRs addressed (UX-DR1, UX-DR10, UX-DR13-15, UX-DR20, UX-DR23, UX-DR25-30, UX-DR43, UX-DR52-53, UX-DR57, UX-DR67-70)
- Stories are sequentially completable: 3.1 (shell/theme) -> 3.2 (sidebar) -> 3.3 (density/settings) -> 3.4 (command palette) -> 3.5 (home/badges) -> 3.6 (session persistence)

---
