# UX Pattern Analysis & Inspiration

### Inspiring Products Analysis

**1. Azure Portal**

*Why it's relevant:* Azure Portal is the closest analog to FrontComposer's composition problem -- it composes dozens of independent services into one coherent management experience. Business users (ops teams) and developers both use it daily.

| UX Strength | How It Achieves It | FrontComposer Relevance |
|---|---|---|
| **Resource blade navigation** | Click a resource → blade slides in from the right, preserving parent context. No full-page navigation for drill-down. | Directly relevant to the list-detail inline pattern. FrontComposer's expand-in-row is the same principle -- keep parent context visible during drill-down. |
| **Command bar** | Top-of-page action bar with contextual commands that change based on the selected resource type. | Maps to action density rules -- commands contextual to the current projection, not a fixed global toolbar. |
| **Resource groups as composition** | Independent Azure services grouped into logical "resource groups." Users see their mental model, not Azure's service architecture. | Identical to FrontComposer's bounded context grouping -- business users see "Orders" not "Hexalith.Orders.Microservice." |
| **Activity log** | Every action logged with timestamp, user, status. Always accessible. | Supports the "real-time activity awareness" design opportunity. Could inform v2 activity feed design. |
| **Notifications panel** | Top-right bell icon showing async operation progress. Deployment in progress, succeeded, failed -- all visible. | **Directly relevant** to eventual consistency UX. Azure Portal's notification model for long-running operations is the closest existing pattern to FrontComposer's five-state lifecycle. |

*Key UX weakness to learn from:* Azure Portal's blade navigation becomes unwieldy at depth >3. Blades stack horizontally and the user loses orientation. FrontComposer should enforce 2-level max depth (as already spec'd) and never let drill-down create a horizontal scroll of contexts.

**2. GitHub**

*Why it's relevant:* GitHub is the gold standard for developer-first UX that also serves non-developer stakeholders (PMs, designers using issues/PRs). Its command palette and keyboard-first design are directly applicable.

| UX Strength | How It Achieves It | FrontComposer Relevance |
|---|---|---|
| **Command palette (Ctrl+K)** | Universal search and navigation. Type anything -- repo, file, command, user -- and get instant results. | Already spec'd for FrontComposer. GitHub proves this pattern works at scale (millions of repos). Implementation should follow GitHub's approach: fuzzy matching, recent items first, categorized results. |
| **Contextual actions on list items** | Issue/PR lists show inline status badges, assignee avatars, and quick-action buttons without expanding the row. | Validates FrontComposer's action density rules. GitHub proves that inline actions on list rows reduce context switches dramatically. |
| **Progressive disclosure in detail views** | Issue detail shows title + description by default; timeline, linked PRs, labels are visible but not overwhelming. Tabs for code changes, checks, etc. | Pattern for projection detail views -- lead with the essential fields, group secondary fields into collapsible sections or tabs. Avoid the "wall of fields" anti-pattern. |
| **Keyboard shortcuts everywhere** | `g i` → go to issues, `g p` → go to PRs, `/` → focus search. Power users navigate without mouse. | FrontComposer should support keyboard shortcuts for top navigation targets. The composition shell should provide a consistent shortcut system across all bounded contexts. |
| **Empty states with CTAs** | Empty repo → "Quick setup" guide. Empty issues → "Create your first issue." Always actionable, never blank. | Already spec'd. GitHub's empty states are the benchmark -- domain-specific, actionable, not generic. |

*Key UX weakness to learn from:* GitHub's notification system is overwhelming for active users. Hundreds of unread notifications with no intelligent prioritization. FrontComposer's v2 activity feed should learn from this: prioritize by user relevance (projections the user subscribes to), not by chronological order.

**3. Notion**

*Why it's relevant:* Notion proves that a composition-based architecture (blocks composing into pages) can feel simple and delightful. Its progressive disclosure and "everything is a block" metaphor parallel FrontComposer's "everything is a projection/command" model.

| UX Strength | How It Achieves It | FrontComposer Relevance |
|---|---|---|
| **Sidebar navigation with nested pages** | Collapsible tree in the left sidebar. Drag to reorder. Infinite nesting but practically used at 2-3 levels. | Validates FrontComposer's collapsible sidebar with bounded context groups. Notion proves this scales well when depth is naturally limited. |
| **Inline editing everywhere** | Click any text to edit. No "edit mode" vs "view mode." The content IS the editor. | Philosophical inspiration: FrontComposer's command forms should feel native to the projection view, not like a separate "edit mode." The command+context pattern supports this -- command form appears within the projection context, not on a separate page. |
| **Slash commands for actions** | Type `/` to see all available actions in context. Discoverable, fast, keyboard-friendly. | Analogous to the command palette. Notion's slash menu is contextual (different options in different block types); FrontComposer's command palette should be contextual too (different commands available depending on current bounded context). |
| **Calm, minimal aesthetic** | Generous whitespace, muted colors, content-first design. The tool recedes; the content dominates. | Aligned with FrontComposer's visual direction: "functional, systematic, professional, clean, native." Fluent UI's design language supports this when used without overrides. |
| **Real-time collaboration indicators** | Colored cursors showing who else is editing. Subtle, non-intrusive, always-visible. | Inspiration for v2 real-time activity awareness. When another user modifies a projection the current user is viewing, a subtle indicator could show "Updated by [user] just now." |

*Key UX weakness to learn from:* Notion's performance degrades with large databases. A Notion database with 10,000 rows becomes sluggish. FrontComposer's DataGrid views will face the same challenge -- projection lists could be very large. Virtual scrolling (Fluent UI's `<Virtualize>`) and server-side pagination via ETag-cached queries are essential from v1.

### Transferable UX Patterns

**Navigation Patterns:**

| Pattern | Source | FrontComposer Application |
|---------|--------|--------------------------|
| **Command palette (Ctrl+K)** | GitHub | Universal navigation across all bounded contexts. Fuzzy match, recent items first, categorized by context. v1 requirement. |
| **Collapsible sidebar with groups** | Azure Portal, Notion | Bounded contexts as collapsible nav groups. 2-level max depth. Persistent across sessions. |
| **Breadcrumbs for orientation** | Azure Portal | Content area breadcrumbs: Home → Orders → Order #1234. Always visible, always clickable. |
| **Keyboard shortcuts for power users** | GitHub | `g o` → go to Orders, `g i` → go to Inventory. Consistent system across all bounded contexts. |

**Interaction Patterns:**

| Pattern | Source | FrontComposer Application |
|---------|--------|--------------------------|
| **Inline actions on list items** | GitHub | Action density rules: 0-1 field commands as inline buttons on DataGrid rows. No context switch for simple actions. |
| **Expand-in-place for details** | Azure Portal blades | List-detail inline pattern: click row to expand detail view within the list. Parent list stays visible. |
| **Async operation notifications** | Azure Portal bell icon | Five-state lifecycle mapped to a notification model: submitted → processing → complete. Notification panel accessible globally. |
| **Contextual command availability** | Notion slash menu | Commands available in the command palette change based on current bounded context. Orders context shows Order commands; Inventory context shows Inventory commands. |
| **Progressive disclosure in detail views** | GitHub issues | Projection detail views: essential fields visible by default, secondary fields in collapsible sections. Never show all 30 fields flat. |

**Visual Patterns:**

| Pattern | Source | FrontComposer Application |
|---------|--------|--------------------------|
| **Content-first, tool-recedes** | Notion | Fluent UI's minimal aesthetic. Data and actions dominate; framework chrome is minimal. |
| **Status badges inline** | GitHub | Projection status indicators as colored badges directly on list rows: "Pending" (amber), "Approved" (green), "Rejected" (red). Domain-language labels, not generic statuses. |
| **Empty states with domain CTAs** | GitHub | "No orders found. Send your first order command." Every empty state is actionable and domain-specific. |

### Anti-Patterns to Avoid

| Anti-Pattern | Source | Why to Avoid | FrontComposer Mitigation |
|---|---|---|---|
| **Blade stacking at depth >3** | Azure Portal | Users lose spatial orientation. Horizontal scrolling through stacked contexts is disorienting. | Enforce 2-level max navigation depth. Expand-in-place for details, never stack. |
| **Notification flood** | GitHub | Chronological notifications without prioritization become noise. Users stop checking. | v2 activity feed must prioritize by user relevance (subscribed projections, own commands) not by chronological order. |
| **Performance cliff with large datasets** | Notion | Large lists become sluggish, breaking the "fast by default" principle. | Virtual scrolling from v1. Server-side pagination via ETag-cached queries. Never load unbounded projection lists into memory. |
| **Modal dialogs for actions** | Many enterprise apps | Modals break context. User loses sight of the data they're acting on. | Inline forms and expand-in-place. Commands render within projection context, not in modals. Exception: destructive commands (delete) may use a confirmation dialog for safety. |
| **Generic empty states** | Enterprise CRUD apps | "No records found" tells the user nothing. Creates a dead end. | Every empty state names the domain entity and provides a creation action. "No orders found. Send your first order command." |
| **Separate edit/view modes** | Legacy admin panels | "Click Edit to modify" → form appears → "Click Save" → back to view mode. Adds ceremony to every change. | Inline editing where action density allows. Command forms appear within the projection context, not as a separate mode. |
| **Loading spinners blocking entire pages** | Many web apps | Full-page spinner breaks flow and creates anxiety. User wonders "did something break?" | Per-component loading skeletons. Each projection view loads independently. The shell and navigation are always responsive even if one projection is still loading. |

### Design Inspiration Strategy

**Adopt directly:**

- **Command palette (Ctrl+K)** from GitHub -- proven at scale, essential for 10+ bounded contexts
- **Collapsible sidebar with groups** from Azure Portal/Notion -- natural mapping to bounded contexts
- **Inline actions on list rows** from GitHub -- validates action density rules
- **Async operation notifications** from Azure Portal -- closest existing pattern to five-state lifecycle
- **Empty states with domain CTAs** from GitHub -- benchmark quality for auto-generated empty states
- **Per-component loading** from modern web apps -- never block the entire shell

**Adapt for FrontComposer:**

- **Azure Portal blade navigation** → FrontComposer's expand-in-place (same principle, but vertical expansion within a list instead of horizontal blade stacking, respecting 2-level depth limit)
- **Notion's inline editing** → FrontComposer's command+context pattern (commands render within projection context, not as separate "edit mode," but still as distinct command forms, not free-form text editing)
- **GitHub's keyboard shortcuts** → FrontComposer's bounded-context-aware shortcuts (same concept but dynamically scoped to current context)
- **Notion's slash commands** → FrontComposer's contextual command palette (commands available change based on active bounded context)

**Avoid explicitly:**

- Blade stacking / deep navigation hierarchies
- Chronological notification floods without relevance filtering
- Full-page loading spinners
- Modal dialogs for standard commands (reserve for destructive actions only)
- Generic empty states
- Separate edit/view modes
- Unbounded data loading without virtualization
