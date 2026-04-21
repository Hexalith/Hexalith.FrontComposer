# Epic 3 Context: Composition Shell & Navigation Experience

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 3 turns FrontComposer into a usable day-to-day application shell: a bounded-context navigation frame with persisted theme and density preferences, a command palette and keyboard shortcuts for fast movement, session restoration, and badge-driven discovery of what needs attention. The epic matters because it converts generated views from isolated screens into a coherent operational workspace with fast navigation, accessible interaction, and resilient state restoration across reconnects and return visits.

## Stories

- Story 3.1: Shell Layout, Theme & Typography
- Story 3.2: Sidebar Navigation & Responsive Behavior
- Story 3.3: Display Density & User Settings
- Story 3.4: FcCommandPalette & Keyboard Shortcuts
- Story 3.5: Home Directory, Badge Counts & New Capability Discovery
- Story 3.6: Session Persistence & Context Restoration

## Requirements & Constraints

Business users must be able to navigate bounded contexts quickly through a sidebar and a global command palette, change theme and density preferences, resume prior work, and discover new capabilities or urgent work with lightweight visual cues. Keyboard interaction is a first-class capability: shell navigation, palette invocation, result traversal, dialog dismissal, and settings access all need accessible keyboard parity. Command palette search must fuzzy-match projections, commands, and recent locations with fast response, contextual ranking, and graceful degradation when badge services or client storage are unavailable.

The shell is desktop-first but must adapt cleanly down to compact desktop and tablet, with touch-target guarantees on smaller viewports and no broken phone layouts. Generated and framework UI must meet WCAG 2.1 AA expectations, including visible focus, correct dialog/listbox semantics, live region announcements, and screen-reader-friendly state changes. Preference and session persistence may use browser-local storage, but only for UI-oriented state scoped by tenant and user; loss of storage must degrade silently to safe defaults.

Performance constraints that matter for this epic include sub-300ms first interactive shell render, sub-100ms command-palette search response, and responsive command lifecycle feedback with progressive visibility thresholds. Reliability constraints include reconnect-safe UI behavior, exactly one user-visible outcome per command flow, and recovery from SignalR interruption through rejoin and catch-up patterns.

## Technical Decisions

The shell uses Blazor Auto with Fluxor for client-facing state, so render-mode transitions and DI lifetime differences matter: state must remain safe across Server-to-WASM handoff, and storage abstractions must tolerate environments where browser APIs are temporarily unavailable. Shell concerns are organized as per-concern Fluxor features and effects under `Shell/State/{Concern}`. Generated components and hand-written shell components subscribe explicitly to `IState<T>` and dispose subscriptions rather than depending on a framework base class.

Persistent UI state goes through `IStorageService` with tenant/user-scoped keys, fire-and-forget writes, and graceful degradation when scope or storage is unavailable. Routing and navigation behavior stay in the shell layer; infrastructure seams must remain behind contracts and avoid direct provider coupling. Diagnostic logging uses structured messages and established `HFC2xxx` shell diagnostic IDs.

Epic 3 also depends on the registry-driven composition model: bounded contexts, projections, and commands come from `IFrontComposerRegistry`, and shell features should prefer existing canonical registration/routing helpers over ad-hoc route synthesis. Commands, projections, and navigation state must stay aligned with earlier shell stories rather than introducing parallel conventions.

## UX & Interaction Patterns

The shell header is the global interaction hub: title, breadcrumbs, command palette trigger, theme toggle, and settings entry live there. The sidebar is the primary navigation surface on desktop and becomes an overlay or drawer as space shrinks. The command palette is a focused dialog interaction with auto-focused search, categorized results, keyboard-first traversal, result-count announcements, and informational shortcut discovery. Focus should remain predictable during result navigation, and closing transient UI must return the user to a sensible focus target without leaving focus orphaned.

Smaller viewports force comfortable density and larger touch targets, while overlays such as the command palette move toward full-width presentation. Color is never the only signal: badges, lifecycle indicators, and “new” markers need textual or semantic support.

## Cross-Story Dependencies

Story 3.4 depends directly on Stories 3.1-3.3 for shell layout, header composition, navigation state, responsive behavior, settings entry, and keyboard handling conventions. It also establishes seams consumed by later stories: Story 3.5 provides `IBadgeCountService` data for palette and navigation badges, and Story 3.6 extends persistence and context restoration on top of the shell/navigation state introduced earlier in the epic. Outside Epic 3, the command palette and shell must use the registry and generated domain metadata from the broader FrontComposer architecture instead of bespoke per-domain wiring.
