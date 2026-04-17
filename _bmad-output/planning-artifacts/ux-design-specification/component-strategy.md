# Component Strategy

### Design System Components

**Fluent UI Blazor v5 provides the foundation layer.** The component mapping from the Design System Foundation section (Step 6) remains the authoritative reference for how Fluent UI components map to FrontComposer concepts. This section does not repeat that mapping; it documents only the **gap** -- what Fluent UI doesn't provide and FrontComposer must build.

**Zero-override commitment applies to custom components too.** Custom components are built *with* Fluent UI components, not *alongside* them. Every custom component's internal DOM uses Fluent UI primitives (`FluentCard`, `FluentButton`, `FluentIcon`, `FluentBadge`, etc.) and Fluent UI tokens for spacing, color, and typography. No custom CSS overrides, no design token hacking, no shadow DOM penetration -- even in custom components. The only CSS a custom component may introduce is layout-structural (flexbox/grid arrangement of Fluent UI children) and animation keyframes (sync pulse, saturation transition) that don't override Fluent UI's visual language.

**Naming convention:** All custom components use the `Fc` prefix (short for FrontComposer) to distinguish them from Fluent UI components (`Fluent` prefix) and adopter components (no prefix or adopter-chosen prefix). This prevents namespace collisions and makes it immediately clear in any Razor file which components are framework-provided.

### Supporting Services

Custom components communicate through shared services, not direct component-to-component coupling. These services are the architectural backbone that makes the component system composable.

#### ILifecycleStateService

**Purpose:** Scoped service that tracks per-command lifecycle states and exposes an observable stream. The state bus that `FcLifecycleWrapper` publishes to and that `FcDesaturatedBadge`, `FcSyncIndicator`, and `FcNewItemIndicator` subscribe to.

**Why a service, not just a component:** The lifecycle wrapper renders around command-triggering elements, but other components (badges, sync indicators) need to observe lifecycle state from different positions in the render tree. A scoped service with `event Action<LifecycleStateChangedArgs>` allows any component in the circuit to subscribe regardless of render tree position.

**API surface:**

```csharp
public interface ILifecycleStateService
{
    /// Observe lifecycle state changes for a specific command correlation ID
    IObservable<LifecycleState> Observe(Guid commandCorrelationId);

    /// Get current state for a command (snapshot)
    LifecycleState GetState(Guid commandCorrelationId);

    /// Publish a state transition (called by FcLifecycleWrapper)
    void Transition(Guid commandCorrelationId, LifecycleState newState);

    /// Monitor SignalR connection state for disconnection escalation
    HubConnectionState ConnectionState { get; }
}
```

**Scope:** Scoped per-circuit in Blazor Server, scoped per-user in Blazor WebAssembly. Command correlation IDs are generated on command dispatch and flow through the entire lifecycle.

#### IBadgeCountService

**Purpose:** Singleton service that provides a single source of truth for ActionQueue badge counts across all consumers: `FcHomeDirectory`, sidebar nav items, and `FcCommandPalette`.

**Why a shared service:** Without this, three independent query paths fetch the same data, producing stale counts in the command palette while the sidebar is current, and unnecessary server load on every navigation.

**API surface:**

```csharp
public interface IBadgeCountService
{
    /// Current badge counts for all ActionQueue-hinted projections
    IReadOnlyDictionary<ProjectionType, int> Counts { get; }

    /// Observable stream of count changes
    IObservable<BadgeCountChangedArgs> CountChanged { get; }

    /// Total items needing attention across all contexts
    int TotalActionableItems { get; }
}
```

**Data flow:**
1. Fetches initial counts for all ActionQueue-hinted projections on app startup (parallel lightweight queries)
2. Subscribes to the same SignalR hub that feeds projection updates
3. Filters for ActionQueue-hinted projection types and updates counts on change events
4. Emits `CountChanged` notifications to all subscribers

**Scope:** Scoped per-circuit in Blazor Server, singleton in Blazor WebAssembly.

### Custom Components

#### 1. FcCommandPalette

**Purpose:** Universal navigation and command search across all bounded contexts. The primary navigation method at scale (>15 bounded contexts) and a keyboard-first power user tool at any scale.

**Usage:** Triggered by Ctrl+K globally, or by clicking the search icon in the shell header. Always available regardless of current view.

**Anatomy:**
- **Overlay backdrop:** Semi-transparent dark overlay covering the full viewport (click to dismiss)
- **Search panel:** Centered card (max 600px wide) containing:
  - `FluentSearch` input with auto-focus on open
  - Categorized results list with keyboard navigation
  - Badge counts sourced from `IBadgeCountService` (not independently fetched)

**Content:**
- **Result categories:** Projections (nav targets), Commands (action shortcuts), Recent (last 5 visited views)
- **Result format:** `[Icon] [Bounded Context] > [View Name] ([Badge Count if ActionQueue])` -- e.g., `📋 Commerce > Orders (3 pending)`
- **Fuzzy matching:** Matches against bounded context names, projection names, and command names. "Ord" matches "Orders", "Create Order", "Order Status"
- **Contextual commands:** When invoked from within a bounded context, commands for that context appear first; cross-context results follow

**States:**

| State | Visual | Behavior |
|---|---|---|
| Closed | Not visible | Ctrl+K or click opens |
| Open / empty | Search input focused, recent items shown | Type to search |
| Open / results | Categorized results below search input | Arrow keys navigate, Enter selects |
| Open / no results | "No matches found" message | Suggest browsing sidebar |

**Keyboard interaction:**
- `Ctrl+K` → open (global shortcut, registered at shell level)
- `Escape` → close
- `↑ / ↓` → navigate results
- `Enter` → select highlighted result (navigate to view or open command form)
- Type → filter results (debounced at 150ms)

**Accessibility:**
- `role="dialog"` with `aria-label="Command palette"`
- `aria-activedescendant` tracks highlighted result
- Results are `role="listbox"` with `role="option"` items
- Screen reader announces result count on each keystroke: "3 results"
- Focus trap within the dialog while open

**Built with:** `FluentCard`, `FluentSearch`, `FluentIcon`, `FluentBadge`, custom `role="listbox"` results list. Badge counts from `IBadgeCountService`.

---

#### 2. FcLifecycleWrapper

**Purpose:** Cross-cutting composition wrapper that manages the five-state command lifecycle (Idle → Submitting → Acknowledged → Syncing → Confirmed/Rejected) for any inner component. Publishes state transitions to `ILifecycleStateService` so sibling components can react.

**Usage:** Automatically wraps every command-triggering component. Developers never instantiate this directly -- the auto-generation engine and the customization gradient both inject it. Custom components at Level 4 (full replacement) still receive the wrapper unless they explicitly opt out (documented as an advanced escape hatch).

**Architecture:** The wrapper is the **publisher** side of the lifecycle state bus. On each state transition, it calls `ILifecycleStateService.Transition(correlationId, newState)`. Subscriber components (`FcDesaturatedBadge`, `FcSyncIndicator`, `FcNewItemIndicator`) inject `ILifecycleStateService` and observe the relevant correlation ID. This clean split means the wrapper has no direct dependency on any subscriber component.

**Anatomy:**
- **Invisible wrapper** -- no DOM of its own in the Idle state
- **Submitting:** Adds `FluentProgressRing` overlay to the triggering button, disables the button
- **Acknowledged:** Removes progress ring, transitions to Syncing
- **Syncing (>300ms):** Publishes Syncing state; subscriber components apply their visual effects (pulse, desaturation)
- **Confirmed:** Publishes Confirmed; subscribers animate to confirmed state; wrapper returns to Idle
- **Rejected:** `FluentMessageBar` (Danger appearance) with domain-specific rollback message; publishes Rejected; badge reverts

**States:**

| State | Trigger | Duration | Visual | Exit condition |
|---|---|---|---|---|
| Idle | Default | Indefinite | None | User submits command |
| Submitting | Command dispatched | Until 202 received | ProgressRing on button, button disabled | HTTP 202 Accepted |
| Acknowledged | 202 received | Instant transition | ProgressRing removed | Automatic |
| Syncing | Acknowledged + >300ms without confirmation | Until SignalR confirms or timeout | Published to ILifecycleStateService; subscribers react | SignalR event or timeout |
| Confirmed | SignalR projection change event | 200ms (subscriber animation) | Published to service; subscribers animate | Animation complete → Idle |
| Rejected | Domain rejection event | Until user dismisses | FluentMessageBar + published to service | User dismisses or next action |
| Timeout (2-10s) | Syncing >2s | Until confirmed | "Still syncing..." text below element | Confirmation or escalation |
| Timeout (>10s) | Syncing >10s | Until confirmed | Action prompt with refresh option | User action or confirmation |
| Disconnected | SignalR down during Syncing | Until reconnect | Immediate escalation message, no pulse | Reconnection |

**SignalR awareness:** The wrapper monitors `ILifecycleStateService.ConnectionState`. If SignalR disconnects during Syncing, the wrapper escalates immediately to the Disconnected state rather than running the pulse indefinitely.

**Threshold configuration:** The 300ms sync pulse threshold is exposed as a deployment-level configuration setting: `FrontComposerOptions.SyncPulseThresholdMs`. Default: 300.

**Accessibility:**
- State changes announced via `aria-live="polite"` for Confirmed/Syncing, `aria-live="assertive"` for Rejected/Timeout
- Focus ring preserved during all states (never dimmed or suppressed)
- `prefers-reduced-motion: reduce` replaces pulse animation with instantaneous state indicator

**Built with:** `FluentProgressRing`, `FluentMessageBar`, `ILifecycleStateService` (publisher). CSS animation keyframes for pulse timing.

---

#### 3. FcFieldPlaceholder

**Purpose:** Renders a visible placeholder for fields the auto-generation engine cannot handle (complex nested types, `Dictionary<string, List<T>>`, custom value objects). Prevents silent omission -- the cardinal sin of auto-generation.

**Usage:** Automatically injected by the auto-generation engine when it encounters an unsupported field type. A build-time warning is also emitted.

**Anatomy:**
- `FluentCard` with a dashed border (visual distinction from real fields)
- `FluentIcon` (Warning) + field name + type annotation
- Clear message: "This field requires a custom renderer."
- `FluentAnchor` link to customization gradient documentation
- In dev-mode overlay: highlighted with distinct visual indicator and the exact unsupported type name

**States:**

| State | Visual |
|---|---|
| Default | Dashed-border card with warning icon and guidance message |
| Dev-mode active | Additional highlight showing the exact type that couldn't be resolved and the recommended override level |

**Accessibility:**
- `role="status"` with `aria-label="[Field name] requires custom renderer"`
- Focusable via tab order (same position as the field it replaces)
- Link to docs is keyboard-accessible

**Built with:** `FluentCard`, `FluentIcon`, `FluentAnchor`, `FluentLabel`

---

#### 4. FcEmptyState

**Purpose:** Domain-specific empty state with actionable creation CTA. Replaces generic "no data" messages with meaningful guidance.

**Usage:** Automatically rendered by projection views when the DataGrid or detail view has zero items. The message content is derived from the projection's domain context.

**Anatomy:**
- `FluentIcon` (large, muted) appropriate to the domain context
- Primary message: "[No {entity plural}] yet." (e.g., "No orders yet.")
- CTA button: "Send your first [Command Name]" -- only shown if the user has at least one available command for that projection
- Secondary text (optional): Additional guidance from resource files

**Content generation:**
- Entity name from projection type name (humanized via label resolution chain)
- Command name from the first available command associated with the projection's bounded context
- If no commands are available (read-only projection): show message without CTA

**States:**

| State | Visual |
|---|---|
| Empty with available command | Icon + message + CTA button |
| Empty without available command | Icon + message only (no button) |
| Empty with custom message | Adopter-provided resource file overrides default message |

**Accessibility:**
- `role="status"` with `aria-label="No [entities] found"`
- CTA button is keyboard-focusable and clearly labeled
- Screen reader announces the full message including the CTA action

**Built with:** `FluentIcon`, `FluentLabel`, `FluentButton`

---

#### 5. FcSyncIndicator

**Purpose:** Manages reconnection reconciliation visual feedback and the header-level connection status indicator. Subscribes to `ILifecycleStateService` for SignalR connection state awareness.

**Usage:** Singleton component in the shell header. Monitors `ILifecycleStateService.ConnectionState` and coordinates ETag-diffed batch updates on reconnection.

**Anatomy:**
- **Header indicator:** Small status text in the shell header (only visible when disconnected or reconnecting)
- **Reconnection toast:** `FluentMessageBar` (Info appearance) auto-dismissing after 3 seconds
- **Batch sweep animation:** Single CSS animation applied to all stale rows simultaneously on reconnect

**States:**

| State | Header indicator | Content area | Toast |
|---|---|---|---|
| Connected | Not visible | Normal | None |
| Disconnected | "Reconnecting..." text with subtle pulse | Cached data, interactive but stale | None |
| Reconnecting | "Reconnecting..." | ETag queries in flight | None |
| Reconciled (changes found) | Clears | Single batch sweep animation on stale rows | "Reconnected -- data refreshed" (3s auto-dismiss) |
| Reconciled (no changes) | Clears silently | No visual change | None |

**ETag reconciliation flow:**
1. On reconnect, issue ETag-conditioned GET for each visible projection
2. Server returns `304 Not Modified` (zero payload) or full response
3. Collect all changed projections
4. Apply single batch sweep animation to all changed rows
5. Show toast only if changes were found

**Accessibility:**
- Header indicator uses `aria-live="polite"` for status changes
- Toast uses `role="status"` with auto-dismiss (screen reader announces on appearance)
- Batch sweep animation respects `prefers-reduced-motion`

**Built with:** `FluentMessageBar`, `FluentIcon`, `ILifecycleStateService` (subscriber for connection state). CSS animation keyframes (batch sweep).

---

#### 6. FcDesaturatedBadge

**Purpose:** Extends `FluentBadge` with a desaturation state for optimistic updates during the syncing window. Subscribes to `ILifecycleStateService` to know when to desaturate and when to restore.

**Usage:** Automatically applied to projection status badges. The badge subscribes to `ILifecycleStateService.Observe(correlationId)` and reacts to Syncing → Confirmed/Rejected transitions.

**Anatomy:**
- Standard `FluentBadge` with an additional CSS class (`fc-badge--syncing`) that applies `filter: saturate(0.5)` during the syncing state
- 200ms CSS transition on the `filter` property for smooth saturation restoration

**States:**

| State | Visual | Trigger |
|---|---|---|
| Confirmed | Full-color badge (standard FluentBadge) | `ILifecycleStateService` publishes Confirmed |
| Syncing (optimistic) | Same badge with 50% desaturation | `ILifecycleStateService` publishes Syncing |
| Transitioning | 200ms animation from desaturated → full | Confirmed event received |
| Reverted | Badge returns to pre-optimistic state | `ILifecycleStateService` publishes Rejected |

**Design rationale:** Desaturation is the least intrusive visual distinction that maintains badge readability. The text label remains fully legible; only the color intensity changes. Users who cannot perceive color saturation differences are unaffected because the text label is always present (per accessibility commitment #1: color is never the sole signal).

**Accessibility:**
- Badge text label is always present (color is never the sole signal)
- `aria-label` includes state: "[Status] (confirming)" during Syncing, "[Status]" when Confirmed
- `prefers-reduced-motion`: saturation transition becomes instantaneous

**Built with:** `FluentBadge`, `ILifecycleStateService` (subscriber), single CSS class with `filter` and `transition` properties

---

#### 7. FcColumnPrioritizer

**Purpose:** DataGrid wrapper that automatically manages column visibility when projections have more than 15 fields.

**Usage:** Automatically wraps every auto-generated `FluentDataGrid` when the projection has >15 visible fields. Transparent when field count is ≤15.

**Anatomy:**
- Standard `FluentDataGrid` with first 8-10 columns visible
- "More columns" toggle button (`FluentButton`, Outline appearance) at the end of the column header row
- Expandable column panel showing hidden columns as checkboxes
- Column priority determined by: (1) `[ColumnPriority]` annotation, (2) declaration order in the projection type

**States:**

| State | Visible columns | Toggle button |
|---|---|---|
| Default (≤15 fields) | All columns | Not shown |
| Prioritized (>15 fields) | First 8-10 by priority | "More columns ([N] hidden)" |
| Expanded panel | First 8-10 + user-selected | Panel open with checkboxes |

**Session persistence:** User's column visibility selections are stored in LocalStorage per projection type, restored on return.

**Accessibility:**
- Toggle button is keyboard-accessible
- Column panel uses `role="dialog"` with checkbox list
- Screen reader announces: "[N] columns hidden. Activate to show more."

**Built with:** `FluentDataGrid`, `FluentButton`, `FluentCheckbox`, `FluentPopover`

---

#### 8. FcNewItemIndicator

**Purpose:** Temporarily shows a newly created entity in the DataGrid regardless of current filters. Subscribes to `ILifecycleStateService` to detect successful command completions that create new entities.

**Usage:** Automatically applied after a successful full-page form submission that creates a new entity, when the entity's initial status doesn't match the current DataGrid filter criteria.

**Anatomy:**
- New row rendered at the top of the DataGrid with a subtle highlight background (Fluent info-background token at 10% opacity)
- Small text indicator below the row: "New -- may not match current filters"
- Auto-dismisses after 10 seconds or on the next filter change

**States:**

| State | Visual | Duration |
|---|---|---|
| Active | Highlighted row + indicator text | 10 seconds or until filter change |
| Dismissing | Fade-out animation (300ms) | Automatic |
| Dismissed | Row removed from view (unless it matches filters) | Permanent |

**Accessibility:**
- Row uses `aria-live="polite"` to announce new item
- Indicator text is associated with the row via `aria-describedby`
- `prefers-reduced-motion`: fade-out becomes instant removal

**Built with:** `FluentDataGrid` row styling, `FluentLabel`, `ILifecycleStateService` (subscriber), CSS animation (fade-out)

---

#### 9. FcDevModeOverlay

**Purpose:** Interactive diagnostic and customization discovery layer. The primary entry point for customization in v1. Shows which conventions produced each UI element, guides developers through the customization gradient, offers starter templates, and provides before/after comparison for active overrides.

**Usage:** Toggled via shell header icon or `Ctrl+Shift+D`. Only available in development mode (`IHostEnvironment.IsDevelopment()`). Never visible in production deployments.

**Architecture: Blazor-native, not JS interop.** The overlay does NOT use JavaScript interop for element tracking. Instead, the auto-generation engine injects `FcDevModeAnnotation` components alongside each auto-generated element at render time. These annotation components:
- Know their convention name, contract type, and customization level (because the engine that created them has this information)
- Render dotted outlines and info badges via Blazor `@onclick` handlers
- Handle clicks entirely in .NET -- no JS round-trips over the SignalR circuit

This design avoids the sluggishness of JS interop in Blazor Server dev mode and eliminates the fragility of reverse-engineering which Blazor component produced which DOM element. The only JS interop is the clipboard copy action in the starter template generator.

**Anatomy:**
- **Element annotations (`FcDevModeAnnotation`):** Dotted outlines around each auto-generated element with a small info badge showing the convention name. Injected by the auto-generation engine at render time.
- **Detail panel:** Right-side drawer (`FluentDrawer`, 360px wide) opened when an annotation is clicked, containing:
  - Convention name and description
  - Contract type (C# type name)
  - Current customization level (Default / Annotation / Template / Slot / Full)
  - Recommended override level for common changes
  - "Copy starter template" button (for Levels 2-4)
  - Before/after toggle (when a custom override is active)
- **Unsupported field highlights:** `FcFieldPlaceholder` elements are highlighted with a distinct red-dashed border

**States:**

| State | Visual | Interaction |
|---|---|---|
| Off | No overlay visible | Ctrl+Shift+D or header icon to enable |
| Active (no selection) | Dotted outlines on all auto-generated elements | Click any element to see details |
| Active (element selected) | Selected element highlighted, detail panel open | Panel shows convention info + customization guidance |
| Active (override comparison) | Split view: framework default vs custom override | Toggle between views in the detail panel |

**Production safety:** The overlay component and all `FcDevModeAnnotation` injections are excluded from production builds via `#if DEBUG` conditional compilation. Zero runtime cost in production.

**Accessibility:**
- Annotations are keyboard-navigable (Tab through annotated elements)
- Detail panel is a `role="complementary"` landmark
- Escape closes the detail panel; Ctrl+Shift+D toggles the overlay off
- Screen reader announces element convention name on focus

**Built with:** `FcDevModeAnnotation` (Blazor component), `FluentDrawer` (detail panel), `FluentButton`, `FluentIcon`, `FluentLabel`. CSS outlines (dotted borders). No JS interop except clipboard.

---

#### 10. FcHomeDirectory

**Purpose:** Urgency-sorted bounded-context directory with badge counts and global orientation subtitle. The v1 home page.

**Usage:** Rendered as the default content area when no specific bounded context is selected, or when the user navigates to "Home."

**Anatomy:**
- **Global orientation subtitle:** "Welcome back, [user name]. You have [N] items needing attention across [M] areas." sourced from `IBadgeCountService.TotalActionableItems`
- **Bounded context cards:** One card per registered bounded context group, sorted by `IBadgeCountService.Counts` descending
  - Group name (e.g., "COMMERCE")
  - Projection entries within the group, each with badge count
  - Click-through arrow to navigate to that projection view
- **Zero-urgency contexts:** Listed at the bottom in a collapsed "Other areas" section

**States:**

| State | Visual |
|---|---|
| Items needing attention | Orientation subtitle + urgency-sorted cards with badge counts |
| No items needing attention | "All caught up. No items need your attention." + all contexts listed alphabetically |
| No bounded contexts registered | Empty state: "No microservices registered. See the getting-started guide." |
| Loading | `FluentSkeleton` cards while `IBadgeCountService` initializes |

**Badge count sourcing:** All counts come from `IBadgeCountService` -- no independent queries. Cards render immediately with `FluentSkeleton` placeholders and populate as the service completes initial fetch (progressive rendering).

**Accessibility:**
- `role="main"` landmark
- Cards are keyboard-navigable with `role="link"`
- Badge counts announced as part of card label: "Orders, 3 items pending"
- Sort order communicated via `aria-description="Sorted by urgency"`

**Built with:** `FluentCard`, `FluentBadge`, `FluentIcon`, `FluentSkeleton`, `FluentLabel`, `IBadgeCountService` (subscriber)

---

#### 11. FcStarterTemplateGenerator

**Purpose:** Dev-time tooling that generates copy-and-modify Razor code from the auto-generation engine's current output. Eliminates the learning cliff between Level 1 (annotation) and Level 2 (template) customization.

**Usage:** Invoked from the `FcDevModeOverlay` detail panel via "Copy starter template" button. Not a runtime component -- a development-time code generation tool.

**Architecture: Component tree walking, not Roslyn.** The auto-generation engine maintains an in-memory component tree representing every element it produced. The starter template generator walks this tree and emits Razor syntax directly via an `IRazorEmitter` service. This is simpler and more maintainable than Roslyn source generation (which runs at compile time, not at runtime in dev mode).

**API surface:**

```csharp
public interface IRazorEmitter
{
    /// Generate Razor source from the auto-generated component tree
    /// for the specified element and customization level
    string EmitStarterTemplate(
        ComponentTreeNode node,
        CustomizationLevel level);
}
```

**Output levels:**
- **Level 2 (Template):** Full section layout Razor with typed `Context` parameter
- **Level 3 (Slot):** Single field renderer Razor with typed `FieldSlotContext<T>` parameter
- **Level 4 (Full):** Complete view Razor with all fields, lifecycle wrapper registration, and accessibility attributes

**Output includes:**
- Comments indicating contract type and registration pattern
- The exact Fluent UI components and parameters that reproduce the current output
- Typed `Context` parameter matching the override contract

**Production safety:** `IRazorEmitter` is registered only in development mode. The generated Razor code is copied to the developer's clipboard via JS interop -- the only JS interop in the overlay system.

**Built with:** `IRazorEmitter` (component tree walker), clipboard JS interop

### Component Implementation Strategy

**Build order principle:** Components are ordered by dependency graph, not by perceived importance. A component that other components depend on ships first, regardless of whether it's user-facing.

**Service-first architecture:** The two supporting services (`ILifecycleStateService`, `IBadgeCountService`) ship before any component that depends on them. This enables parallel component development once the services are stable.

**Shared contracts:** All custom components communicate through typed contracts (C# interfaces/records), not through component parameters directly. This enables the customization gradient: an adopter replaces a component by implementing the same contract. Contracts are versioned with the framework and checked at build time.

**Testing strategy -- tiered by blast radius:**

| Tier | Testing depth | Components | Rationale |
|---|---|---|---|
| **Tier 1: Critical path** | Full pyramid: unit + property-based + integration + visual regression + accessibility + E2E | `FcLifecycleWrapper`, `FcDesaturatedBadge`, `FcSyncIndicator`, `ILifecycleStateService` | Production components affecting every user interaction with complex state logic and concurrency hazards |
| **Tier 2: Quality surface** | Unit + integration + accessibility | `FcEmptyState`, `FcFieldPlaceholder`, `FcColumnPrioritizer`, `FcNewItemIndicator`, `FcHomeDirectory`, `FcCommandPalette`, `IBadgeCountService` | Production components with simpler state but important for UX quality |
| **Tier 3: Dev tooling** | Unit + smoke | `FcDevModeOverlay`, `FcDevModeAnnotation`, `FcStarterTemplateGenerator`, `IRazorEmitter` | Development-only, excluded from production builds, lower blast radius |

**Property-based testing for lifecycle state machine:** The `ILifecycleStateService` and `FcLifecycleWrapper` receive property-based tests (FsCheck or equivalent) that generate random sequences of lifecycle events (confirmed, rejected, timeout, disconnect, reconnect in arbitrary orders) and verify the state machine never enters an invalid state. The eventual consistency timing and SignalR connection state create concurrency hazards that example-based tests miss. Property-based testing catches the "what if SignalR reconnects during a timeout escalation?" class of bugs.

**Visual regression per tier:**

| Component | Visual regression scope |
|---|---|
| Tier 1 components | Per-theme × per-density × per-motion-preference screenshot comparison against committed baselines |
| Tier 2 components | Per-theme screenshot comparison (comfortable density only) |
| Tier 3 components | No visual regression (dev-only, appearance is not a production concern) |

### Implementation Roadmap

**Phase 0 -- Services (blocks all phases):**

| Service | Rationale |
|---|---|
| `ILifecycleStateService` | Every Tier 1 component depends on this. The state bus must be stable before any publisher or subscriber ships |
| `IBadgeCountService` | `FcHomeDirectory`, sidebar badge counts, and `FcCommandPalette` all depend on this single source of truth |

**Phase 1 -- Shell Foundation (blocks user-facing experience):**

| Component | Dependency | Rationale |
|---|---|---|
| `FcLifecycleWrapper` | `ILifecycleStateService` | Every command interaction depends on this. The five-state lifecycle is the framework's core UX differentiator |
| `FcEmptyState` | None | Every projection view needs this before it has data. First-render quality depends on it |
| `FcHomeDirectory` | `IBadgeCountService` | The first thing a user sees. Urgency sorting and badge counts drive the daily workflow |
| `FcDesaturatedBadge` | `ILifecycleStateService` | Lifecycle wrapper needs the badge to communicate optimistic state honestly |

**Phase 2 -- Auto-generation Quality (makes auto-generated views production-worthy):**

| Component | Dependency | Rationale |
|---|---|---|
| `FcFieldPlaceholder` | None | Auto-generation honesty. Without this, unsupported fields are silently omitted |
| `FcColumnPrioritizer` | None | DataGrid usability at scale. Prevents 40-column grids |
| `FcNewItemIndicator` | `ILifecycleStateService` | Post-creation confidence. Prevents "where did my new item go?" confusion |
| `FcSyncIndicator` | `ILifecycleStateService` | Reconnection reconciliation. Completes the lifecycle story for degraded network conditions |

**Phase 3 -- Power User & Developer Experience:**

| Component | Dependency | Rationale |
|---|---|---|
| `FcCommandPalette` | `IBadgeCountService` | Navigation at scale. Essential for >15 bounded contexts, valuable at any scale |
| `FcDevModeOverlay` + `FcDevModeAnnotation` | All auto-generation components | Customization entry point. Depends on understanding which conventions produced which elements |
| `FcStarterTemplateGenerator` + `IRazorEmitter` | `FcDevModeOverlay` | Customization bridge. Invoked from the overlay; depends on auto-generation engine's component tree |

**Phase rationale:** Phase 0 establishes the service backbone. Phase 1 establishes the emotional foundation -- lifecycle confidence, empty state quality, home page orientation. Phase 2 makes auto-generation honest and scalable. Phase 3 empowers power users and developers. A business user can use the app after Phase 1; the app is production-worthy after Phase 2; developers can customize efficiently after Phase 3.
