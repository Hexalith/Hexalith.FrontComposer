# UX Consistency Patterns

This section formalizes the patterns that are not yet codified elsewhere in the spec. Patterns already established (action density rules, lifecycle feedback, empty states, navigation, error handling, badge palette, form validation, session persistence, scroll stabilization) are documented in their respective sections and are not repeated here. This section covers the remaining consistency gaps.

### Button Hierarchy

**Four button appearances, strictly ordered:**

| Appearance | Fluent UI | Usage rule | Examples |
|---|---|---|---|
| **Primary** | `ButtonAppearance.Primary` | One per visual context. The single most important action the user should take next. | "Send Create Order", "Approve", "Complete Task" |
| **Secondary** | `ButtonAppearance.Secondary` (default) | Supporting actions that are valid but not the primary call-to-action. | "Cancel", "Save Draft", "Export" |
| **Outline** | `ButtonAppearance.Outline` | Tertiary actions that should be available but not prominent. Filter toggles, "More columns", settings. | "More columns (5 hidden)", "Reset filters", column visibility toggles |
| **Danger** | `ButtonAppearance.Primary` + Danger color slot | Destructive actions only. Always requires confirmation dialog. Never inline on DataGrid rows. | "Delete Order", "Remove Microservice", "Clear All Data" |

**Enforcement rules:**
- **One Primary per visual context.** A DataGrid view has one Primary ("+ Send Create Order" in the header). Inline row actions use Secondary appearance. When an expand-in-row form opens, it becomes a **new visual context**: the form's submit button is Primary; all other buttons in the expanded section become Secondary. This means the DataGrid header's Primary and the expanded form's Primary coexist without conflict -- they are in different visual contexts.
- **Domain-language labels always.** "Send Create Order" not "Submit." "Approve" not "OK." "Complete Task" not "Done." The label is the command name, humanized via the label resolution chain.
- **Destructive actions require confirmation.** A `FluentDialog` with the action name, a description of what will be destroyed, and two buttons: "Cancel" (Secondary, auto-focused) and the destructive action (Danger). Auto-focus on Cancel prevents accidental confirmation. Destructive actions never appear as inline buttons on DataGrid rows -- they require expand-in-row or full-page context.
- **Icons on Primary and DataGrid row actions.** Primary buttons may include a leading `FluentIcon` for visual reinforcement (e.g., `+` icon for creation commands). Secondary buttons on DataGrid rows may also include a small leading icon for scan speed -- when scanning 20 rows, the icon creates a visual anchor faster to target than text alone (e.g., "✓ Approve", "↻ Reopen"). All other Secondary, Outline, and Danger buttons use text only. Exception: icon-only buttons are permitted in the shell header (theme toggle, settings, dev-mode overlay toggle) where space is constrained and the icons are universally recognized.

**Auto-generation button mapping:**

| Context | Action density | Button appearance | Icon | Placement |
|---|---|---|---|---|
| DataGrid header | Creation command (5+ fields) | Primary | Leading `+` icon | Right-aligned in content area header |
| DataGrid row | 0-1 field command | Secondary | Leading action icon | Inline on row, right-aligned |
| Expand-in-row form | 2-4 field command submit | Primary (new visual context) | Leading action icon | Bottom of expanded form |
| Full-page form | 5+ field command submit | Primary | Leading action icon | Bottom of form, centered |
| Expand-in-row detail | Available commands | Secondary | Leading action icon | Grouped below detail fields |

### Data Formatting

**Consistent rendering rules for common data types across all auto-generated views:**

| Data type | C# type | Rendering | Fluent component | Notes |
|---|---|---|---|---|
| **Text** | `string` | As-is, single line | `FluentLabel` | Truncated with ellipsis at column width in DataGrid; full text in detail view |
| **Long text** | `string` (>100 chars) | Truncated in DataGrid, full in detail | `FluentLabel` with `title` attribute | Tooltip shows full text on hover in DataGrid |
| **Integer** | `int`, `long` | Locale-formatted (e.g., "12,450") | `FluentLabel` | Right-aligned in DataGrid columns |
| **Decimal / Currency** | `decimal` | Locale-formatted with currency symbol when annotated `[Currency]` | `FluentLabel` | Right-aligned; currency annotation triggers `CultureInfo` formatting |
| **Date** | `DateTime`, `DateOnly` | Short date per `CultureInfo` (e.g., "09/04/2026" or "09 Apr 2026") | `FluentLabel` | Sortable by underlying value, not display string |
| **Date + Time** | `DateTime`, `DateTimeOffset` | Absolute: short date + time (e.g., "09 Apr 2026, 14:32") | `FluentLabel` | Always absolute by default. Relative time ("2h ago") is opt-in via `[RelativeTime]` annotation (see below) |
| **Boolean** | `bool` | Toggle in forms; "Yes"/"No" text in DataGrid/detail | `FluentCheckbox` (form) / `FluentLabel` (read-only) | Localizable via resource files |
| **Enum** | `enum` | Humanized name, max 30 chars with truncation | `FluentSelect` (form) / `FluentBadge` (when annotated with `[ProjectionBadge]`) | See enum label rules below |
| **Identifier / ID** | `string`, `Guid` | Monospace font (`Typography.Code`), truncated to 8 chars in DataGrid with copy-on-click | `FluentLabel` with monospace class | Full ID in detail view and tooltip |
| **Collection** | `IEnumerable<T>` | Count in DataGrid ("[N] items"); expandable list in detail | `FluentLabel` (DataGrid) / `FluentAccordion` (detail) | Not editable in auto-generated forms (triggers `FcFieldPlaceholder`) |

**Timestamp timezone rule:** All timestamps render in the **browser's local timezone** via JavaScript `Intl.DateTimeFormat`. The original timezone offset is available in the tooltip (e.g., hover shows "09 Apr 2026, 14:32 UTC+1"). This ensures a European HQ user managing US orders sees times in their local context, while the original offset is preserved for audit purposes.

**Relative time is opt-in, not default:** Absolute timestamps are the default for all `DateTime` and `DateTimeOffset` fields. Adopters who want social-media-style relative time ("2h ago") annotate the field with `[RelativeTime]`. When active, relative time uses fixed-width abbreviations ("2h ago", "3d ago", "1w ago") to maintain DataGrid column scan-ability. Relative time switches to absolute format for timestamps older than 7 days. This avoids the mixed-format column problem where some rows show "2 hours ago" and others show "09 Apr 2026, 14:32" in the same column.

**Enum label truncation:** Humanized enum names are capped at 30 characters for badge display. Names exceeding 30 characters are truncated with ellipsis, and the full name is available via tooltip. The `[Display(Name="...")]` annotation is the escape hatch for long enums (e.g., `[Display(Name="Awaiting Partner")]` for `WaitingForExternalPartnerApprovalBeforeShippingCanProceed`).

**Formatting override:** Adopters can override any formatting rule per field via the customization gradient (Level 3 slot override). The auto-generation engine applies these defaults; the developer replaces them when the default isn't right.

**Display name precedence rule:** The `[Display(Name="...")]` annotation takes precedence over **all** auto-formatting rules, including enum humanization, label resolution chain fallbacks, and type-specific formatting. If a developer explicitly names a field or enum value, that name is used verbatim -- never overridden by the humanization or formatting pipeline. This ensures developer intent is always respected. The label resolution chain (annotation → resource file → humanized CamelCase → raw field name) and the data formatting pipeline are two independent systems; `[Display(Name)]` sits at the top of both.

**Null handling:** Null values render as an em dash ("—") in DataGrid cells and detail views. Never show "null", "N/A", or empty cells. The em dash is a visual signal that the field exists but has no value, distinct from an empty string (which renders as blank).

**Data formatting specimen (CI-enforced):** A data formatting specimen view is rendered alongside the type specimen in CI. It contains a single DataGrid with one row per data type, exercising all formatting rules: locale-formatted numbers, absolute and relative timestamps, truncated IDs, null em dashes, collection counts, currency formatting, boolean rendering, and truncated enum labels. The specimen is rendered per-theme × per-density, compared against committed baselines, and regression beyond tolerance fails CI. Without this, a Fluent UI update could change number formatting or text truncation and the change would go unnoticed until production.

### Keyboard Shortcut System

**Framework shortcuts (registered by FrontComposer via `IShortcutService`):**

| Shortcut | Action | Component | Context |
|---|---|---|---|
| `Ctrl+K` | Open command palette | `FcCommandPalette` | Global |
| `Ctrl+Shift+D` | Toggle dev-mode overlay (dev only) | `FcDevModeOverlay` | Global |
| `Ctrl+,` | Open settings panel | Shell settings dialog | Global |
| `g h` | Go to Home | Navigate to `FcHomeDirectory` | Global |
| `/` | Focus first column filter input | DataGrid column filter | DataGrid has focus |

**Native keyboard behavior (standard HTML/ARIA, not registered by framework):**

These behaviors work because FrontComposer uses correct ARIA roles and focus management. They are documented here for completeness but are not custom shortcuts -- they are browser-native activation patterns.

| Key | Context | Behavior | Why it works |
|---|---|---|---|
| `Escape` | Any overlay/dialog/panel open | Close the overlay | Standard dialog dismissal |
| `↑ / ↓` | DataGrid with focus | Move row selection | Standard table navigation via `role="grid"` |
| `Enter` | DataGrid row focused | Expand selected row or trigger inline action | Standard activation of focused interactive element |
| `Space` | Inline action button focused | Trigger the focused button | Native button activation -- NOT a framework shortcut |
| `Tab` | Anywhere | Move focus to next interactive element | Standard tab order |

**Why `g [1-9]` was removed:** An earlier draft included `g [1-9]` shortcuts to navigate to the nth bounded context. This was removed because: (1) it covers at most 9 bounded contexts while the spec acknowledges >15 is common, (2) the command palette (Ctrl+K) is already the primary navigation method at scale with badge counts and fuzzy matching, (3) `g h` (go to Home) plus Ctrl+K covers all navigation needs without memorizing position-based numbers. Fewer shortcuts to remember, same coverage.

**Shortcut discoverability:** The command palette (Ctrl+K) shows keyboard shortcuts next to their associated actions. Typing "shortcuts" in the command palette shows a complete shortcut reference. No separate keyboard shortcut cheat sheet is needed -- the command palette IS the reference.

**Shortcut conflict prevention:** FrontComposer registers framework shortcuts at the shell level via `IShortcutService`. Adopter custom components cannot accidentally override framework shortcuts. If a conflict is detected at registration time, a build-time warning is emitted. Native keyboard behaviors do not go through `IShortcutService` -- they rely on correct ARIA roles and DOM focus management.

### Confirmation Patterns

**When to confirm vs. when to act immediately:**

| Action type | Confirmation? | Rationale |
|---|---|---|
| **Non-destructive command** (Approve, Create, Update) | No | Eventual consistency already provides rollback via domain events. The lifecycle wrapper provides feedback. Confirmation dialogs would add friction to the 50-items-per-day queue processing workflow. |
| **Destructive command** (Delete, Remove, Purge) | Yes, always | Destructive commands cannot be undone via domain events (or recovery is expensive). Confirmation prevents accidental data loss. |
| **Navigation away from full-page form** | Conditional: if user has been on the form >30 seconds | Below 30 seconds, the user likely just glanced and is leaving intentionally. Above 30 seconds, they've invested meaningful effort and accidental navigation would lose that work. The threshold tracks time, not field count (simpler to implement, more accurate proxy for user investment). |
| **Bulk actions** (v2) | Yes, with count | "Approve 12 orders?" Bulk actions affect multiple entities and warrant a count confirmation. |

**Idempotent outcome and badge behavior:** When a command is rejected but the user's intent was already fulfilled (e.g., approving an already-approved order), the `FcDesaturatedBadge` must handle this as a special case. The normal rejection path would revert the badge to its pre-optimistic state. But in the idempotent case, the target state is already the confirmed state. The badge should **skip the revert animation and saturate directly** from desaturated-Approved to fully-saturated-Approved. The user sees their intent confirmed, not a confusing flash back to Pending. The `ILifecycleStateService` communicates idempotent outcomes via a distinct `LifecycleState.IdempotentConfirmed` state that subscribers handle differently from `Rejected`.

**Form abandonment protection (>30 second threshold):**

When a user has been on a full-page command form for more than 30 seconds and attempts to navigate away (breadcrumb click, sidebar click, command palette selection), the navigation is intercepted with a lightweight `FluentMessageBar` at the top of the form:

```
⚠ You have unsaved input. [Stay on form] [Leave anyway]
```

- `FluentMessageBar` (Warning appearance) with two action buttons
- "Stay on form" (Primary, auto-focused) cancels the navigation
- "Leave anyway" (Secondary) proceeds with navigation, discarding input
- No countdown timer -- the user decides at their own pace
- The 30-second threshold is configurable via `FrontComposerOptions.FormAbandonmentThresholdSeconds`
- Not applied to compact inline forms (2-4 fields) -- only full-page forms

**Confirmation dialog pattern (for destructive actions):**

```
┌─────────────────────────────────────────┐
│  Delete Order ORD-1847?                 │
│                                         │
│  This will permanently remove the order │
│  and all associated events.             │
│  This action cannot be undone.          │
│                                         │
│           [Cancel]  [Delete Order]      │
│           (focused)  (Danger)           │
└─────────────────────────────────────────┘
```

- `FluentDialog` with `aria-label` matching the action
- Cancel is auto-focused (prevents accidental confirmation via Enter)
- Destructive button uses Danger appearance with domain-language label
- Description includes what will be destroyed and that it cannot be undone
- `Escape` closes the dialog (same as Cancel)

### Notification & Message Bar Patterns

**Two message channels, distinct purposes:**

| Channel | Component | Scope | Auto-dismiss | Use case |
|---|---|---|---|---|
| **Inline (per-row)** | Lifecycle feedback (badge, pulse, text) | Single command/row | Yes (on confirmation) | Command lifecycle states -- submitting, syncing, confirmed, rejected |
| **Global (content area top)** | `FluentMessageBar` | Current view | Depends on severity | Cross-cutting events -- reconnection, schema evolution, destructive action results |

**FluentMessageBar severity mapping:**

| Severity | Appearance | Auto-dismiss | Use case |
|---|---|---|---|
| **Success** | Success | 5 seconds | Destructive action completed ("Order ORD-1847 deleted") |
| **Info** | Info | 3 seconds | Reconnection toast, idempotent outcome ("Already approved by another user") |
| **Warning** | Warning | No (manual dismiss) | Schema evolution ("This section is being updated"), form abandonment protection, stale data warning |
| **Error** | Error | No (manual dismiss) | Command rejection with rollback message, system error |

**Stacking rule:** Maximum 3 visible `FluentMessageBar` instances at the same time. If a 4th message arrives, the oldest auto-dismissible message is removed. Non-dismissible messages (Warning, Error) always remain until manually dismissed or the condition resolves.

**Error aggregation for rapid rejections:** During queue processing, a user may trigger multiple commands in rapid succession and receive multiple rejections. If more than 2 Error-severity message bars arrive within a 5-second window, they are **aggregated** into a single bar: "[N] commands failed. [Show details]". Clicking "Show details" expands an inline list of individual rejection messages. This prevents a wall of stacked error bars during rapid processing and keeps the content area usable. Individual bars are still shown for isolated errors (≤2 within 5 seconds).

**No toast notifications in v1.** `IToastService` was removed in Fluent UI Blazor v5. All notifications use `FluentMessageBar` at the top of the content area or inline lifecycle feedback on rows. This is intentional: toasts are ephemeral and easy to miss; message bars are persistent and positioned in the content flow.

### Search & Filtering Patterns

**DataGrid filtering model:**

| Filter type | UI element | Behavior |
|---|---|---|
| **Column filter** | `FluentSearch` input in column header | Filters rows by that column's value. Debounced at 300ms. Server-side filtering via ETag-cached query parameters. Keyboard shortcut: `/` focuses the first column filter when DataGrid has focus. |
| **Status filter** | `FluentBadge` toggle chips above DataGrid | Click a status badge to toggle that status on/off in the filter. Multiple statuses can be active simultaneously. Active filters shown as filled badges; inactive as outline. |
| **Global search** | `FluentSearch` in content area header (below view title) | Searches across all visible columns. **Framework hook, not framework feature** (see below). |
| **Command palette search** | `FcCommandPalette` (Ctrl+K) | Navigates to views, not filters within views. Cross-context scope. |

**Global search as framework hook:** FrontComposer provides the search UI pattern (search input, result highlighting, debouncing) but does NOT provide a built-in full-text search implementation. Full-text search requires infrastructure (Elasticsearch, Azure Cognitive Search, PostgreSQL full-text) that varies by adopter and is not a DAPR building block.

The adopter registers a search implementation via `IProjectionSearchProvider`:

```csharp
public interface IProjectionSearchProvider
{
    Task<IQueryable<T>> SearchAsync<T>(string query, CancellationToken ct);
}
```

**If no implementation is registered, the global search input does not appear.** The framework does not show a search UI that would fail on use. Column filters and status filters (which operate on known fields via structured queries) are always available regardless of search provider registration.

**Filter and search persistence:**
- Per-DataGrid filter state (column filters + status filters + sort order + global search query) is stored in LocalStorage keyed by `bounded-context:projection-type`
- Restored on return to that view (within-session and cross-session)
- "Reset filters" Outline button clears all filters, sort, and search query to defaults; also clears the persisted state
- Search query follows the same persistence pattern as filters -- if the user typed a search query, it is restored when they return to that view

**Filter visibility:**
- When any filter is active, a summary appears below the DataGrid header: "Filtered: Status = Pending, Approved | 12 of 47 orders"
- When the `FcNewItemIndicator` is active, the filter summary accounts for it: "Filtered: Status = Pending | 0 of 47 orders + 1 new item"
- This prevents the "where did my data go?" confusion when filters hide expected rows

**Empty filtered state:** When filters produce zero results, the empty state message reflects the filter context: "No orders match the current filters. [Reset filters] to see all 47 orders." This is distinct from the `FcEmptyState` component (which shows when the projection has zero items total).

### Pagination & Virtual Scrolling

**Default: Virtual scrolling, not pagination.**

FrontComposer uses Fluent UI's `<Virtualize>` component for all DataGrid views by default. Virtual scrolling:
- Renders only visible rows plus a small buffer
- Maintains the illusion of a complete scrollable list
- Avoids the "Page 1 of 47" pattern that breaks the flow of serial queue processing
- Works with server-side data via `ItemsProvider` delegate

**When virtualization applies:**

| Scenario | Strategy | Notes |
|---|---|---|
| **Projection with <500 items** | Client-side virtualization | All data loaded, virtualized in DOM |
| **Projection with 500+ items** | Server-side virtualization | `ItemsProvider` fetches pages on scroll; ETag-cached queries |
| **Slow query detected** | Performance prompt | See below |

**Performance-based prompt (not hard item count cap):** The `ItemsProvider` knows how long its query took. If the initial page fetch exceeds a configurable threshold (`FrontComposerOptions.SlowQueryThresholdMs`, default: 2000ms), a `FluentMessageBar` (Info) suggests: "Loading is slow. Add filters to narrow results." This is a suggestion, not a block -- the DataGrid continues loading.

A configurable safety rail (`FrontComposerOptions.MaxUnfilteredItems`, default: 10,000) caps the total rows fetched without filters. This is a hard limit that shows: "Showing first 10,000 items. Use filters to find specific records." Adopters whose infrastructure can handle larger datasets raise the limit; those with constrained infrastructure lower it. The threshold is about infrastructure capacity, not a UX opinion.

**Loading indicator during scroll:** When the user scrolls into a range that requires server-side data fetch, a `FluentSkeleton` row appears at the scroll boundary while data loads. The skeleton row has the same height as a real row to prevent layout shift. On fast connections (<200ms fetch), the skeleton may not be visible.

**Scroll position restoration:** When returning to a DataGrid (via breadcrumb, sidebar, or command palette), the scroll position is restored from the per-view memory object. This works with both client-side and server-side virtualization by storing the scroll offset and re-requesting the appropriate data page on return.

**No "load more" buttons.** Infinite scroll via virtualization is the only pagination pattern. "Load more" buttons add a manual step that interrupts flow. The `<Virtualize>` component handles the loading boundary automatically.

**Row count display:** The DataGrid header shows the total row count: "47 orders" (or "12 of 47 orders" when filtered). For server-side virtualized views, the count is fetched as a lightweight count query separate from the data query, so it's available before all rows are loaded.

**Small viewport note:** On viewports below 768px (phone-size, documented as "functional but not optimized"), virtual scrolling behavior is best-effort. The DataGrid may not have a fixed-height container, which affects virtualization accuracy. The framework does not guarantee smooth virtualization on these viewports.
