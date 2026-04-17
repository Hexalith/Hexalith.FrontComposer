# Epic 2: Command Submission & Lifecycle Feedback

Business user can submit commands through auto-generated forms (inline, compact, or full-page based on field count) and see a five-state lifecycle with guaranteed exactly-one-outcome semantics. **Scope: happy path (stable connection). Epic 5 extends for degraded/disconnected conditions.**

### Story 2.1: Command Form Generation & Field Type Inference

As a developer,
I want the source generator to produce form components from [Command]-annotated records with automatic field type inference and validation,
So that business users get correctly typed, validated input forms without manual component authoring.

**Acceptance Criteria:**

**Given** a record type annotated with [Command]
**When** the source generator runs
**Then** a Razor form component is emitted with input fields for each non-derivable property
**And** the generated file follows naming convention {CommandName}Form.g.razor.cs

**Given** a generated command form with various property types
**When** the form renders
**Then** string properties render as FluentTextField
**And** bool properties render as FluentCheckbox (toggle style)
**And** DateTime/DateOnly properties render as FluentDatePicker
**And** enum properties render as FluentSelect with humanized option labels
**And** int/long properties render as FluentNumberField
**And** unsupported types render as FcFieldPlaceholder with build-time warning HFC1002

**Given** a generated command form
**When** field labels render
**Then** the label resolution chain applies: [Display(Name)] > IStringLocalizer > humanized CamelCase > raw field name
**And** every field has an associated <label for=""> element
**And** required fields are visually marked
**And** validation messages use aria-describedby for accessibility

**Given** FluentValidation rules exist for the command type
**When** the form is submitted with invalid input
**Then** validation messages appear inline via FluentValidationMessage
**And** the form does not submit until validation passes
**And** EditContext is wired to FluentValidation rules

**Given** the v0.1 milestone scope (Epics 1-2 only, EventStore abstractions not yet available)
**When** command forms submit
**Then** a stub ICommandDispatcher is used that simulates the command lifecycle (Idle -> Submitting -> Acknowledged -> Syncing -> Confirmed) with configurable delays
**And** the stub is replaceable with the real EventStore dispatcher (Story 5.1) without code changes
**And** the Counter/Task Tracker sample demonstrates the full lifecycle against the stub

**References:** FR1, UX-DR22, UX-DR21 (label resolution), UX-DR3 (field placeholder), NFR30 (accessibility labels)

---

### Story 2.2: Action Density Rules & Rendering Modes

As a business user,
I want commands to render at the appropriate density -- inline buttons for simple actions, compact forms for moderate actions, and full-page forms for complex actions,
So that I can take action quickly on simple commands without navigating away, while complex commands get the space they need.

**Acceptance Criteria:**

**Given** a [Command] with 0-1 non-derivable fields
**When** the command appears on a DataGrid row
**Then** it renders as an inline button with Secondary appearance and a leading action icon
**And** clicking the button submits the command immediately (0 fields) or shows a single inline input (1 field)

**Given** a [Command] with 2-4 non-derivable fields
**When** the business user clicks the command action
**Then** a compact inline form slides open below the DataGrid row within the expand-in-row space
**And** derivable fields are pre-filled from: current projection context, last-used value (session-persisted), or command definition default
**And** the expand-in-row scroll stabilization pins the expanded row's top edge to the current viewport position (scrollIntoView block:'nearest' + requestAnimationFrame)
**And** only one row is expanded at a time (v1 constraint)

**Given** a [Command] with 5+ non-derivable fields
**When** the business user clicks the command action
**Then** a full-page form renders at max 720px width, centered
**And** breadcrumb shows the navigation path back to the DataGrid
**And** DataGrid state (scroll position, filters, sort, expanded row) is preserved in a per-view memory object for restoration on return

**Given** the action density determination
**When** the generator analyzes a command's fields
**Then** a field is classified as "derivable" if resolvable from: current projection context, system values (timestamp, user ID), or command definition defaults
**And** only non-derivable fields count toward the density threshold

**References:** FR8, UX-DR16, UX-DR17, UX-DR19 (DataGrid state preservation), UX-DR36 (button hierarchy)

---

### Story 2.3: Command Lifecycle State Management

As a developer,
I want a lifecycle state service that tracks each command through five states with ULID-based idempotency and guarantees exactly one user-visible outcome,
So that every command submission is traceable, replay-safe, and never produces silent failures or duplicate effects.

**Acceptance Criteria:**

**Given** ILifecycleStateService is registered in DI
**When** the service is inspected
**Then** it exposes: Observe(Guid correlationId) returning IObservable<LifecycleState>, GetState(Guid), Transition(Guid, LifecycleState), and ConnectionState property
**And** the service scope is per-circuit in Blazor Server, per-user in Blazor WebAssembly

**Given** a command submission begins
**When** the lifecycle is initialized
**Then** a ULID message identifier is generated for the command
**And** the lifecycle transitions: Idle -> Submitting -> Acknowledged -> Syncing -> Confirmed (or Rejected)
**And** each transition is observable via Observe(correlationId)

**Given** the lifecycle state machine
**When** any command reaches a terminal state (Confirmed or Rejected)
**Then** exactly one user-visible outcome is produced (success notification, rejection message, or error notification)
**And** the CommandLifecycleState is ephemeral (evicted on terminal state, not persisted to IStorageService)
**And** no silent failures occur -- every submission path produces a visible outcome

**Given** a command with a ULID message ID
**When** a duplicate submission with the same ULID arrives
**Then** deterministic duplicate detection identifies it
**And** the duplicate does not produce a second user-visible effect

**Given** the lifecycle state machine under property-based testing
**When** random lifecycle event sequences are generated
**Then** the state machine never enters an invalid state (e.g., Confirmed after Rejected, or Submitting after Confirmed)

**References:** FR23, FR30, FR36, UX-DR12, NFR44, NFR45, NFR47

---

### Story 2.4: FcLifecycleWrapper - Visual Lifecycle Feedback

As a business user,
I want to see clear, progressive visual feedback during command submission so I know the system is working and never wonder "did it work?",
So that I have 100% confidence in every command outcome without needing to manually refresh or double-submit.

**Acceptance Criteria:**

**Given** a command is submitted
**When** the lifecycle enters Submitting state
**Then** FcLifecycleWrapper displays FluentProgressRing
**And** the submit button is disabled to prevent double-submission
**And** aria-live="polite" announces "Submitting..."

**Given** the lifecycle enters Acknowledged -> Syncing
**When** the Confirmed state arrives within 300ms of Acknowledged
**Then** the sync pulse animation never fires (brand-signal fusion frequency rule)
**And** the lifecycle resolves invisibly to the user (NFR11)

**Given** the lifecycle enters Syncing
**When** 300ms-2s elapses without Confirmed
**Then** a subtle sync pulse animation displays on the affected element (accent color)
**And** the sync pulse threshold is configurable via FrontComposerOptions.SyncPulseThresholdMs (default 300)

**Given** the lifecycle remains in Syncing
**When** 2s-10s elapses without Confirmed
**Then** explicit "Still syncing..." inline text is displayed (NFR13)

**Given** the lifecycle remains in Syncing
**When** >10s elapses without Confirmed
**Then** an action prompt with manual refresh option is displayed (NFR14)

**Given** the lifecycle reaches Confirmed
**When** the success notification renders
**Then** FluentMessageBar (Success) auto-dismisses after 5 seconds
**And** aria-live="polite" announces the confirmation

**Given** the user has prefers-reduced-motion enabled
**When** the sync pulse would normally animate
**Then** the pulse is replaced with an instant state indicator (no animation)
**And** focus ring is never dimmed or suppressed during any lifecycle state

**Given** end-to-end command performance under stable connection
**When** latency is measured via Playwright task timer on localhost Aspire topology
**Then** command click-to-confirmed state P95 cold actor < 800ms (NFR1)
**And** command click-to-confirmed state P50 warm actor < 400ms (NFR2)

**References:** FR23, FR30, UX-DR2, UX-DR48 (sync pulse rule), UX-DR49 (focus ring coexistence), NFR1-2, NFR11-14, NFR88 (zero "did it work?" hesitations)

---

### Story 2.5: Command Rejection, Confirmation & Form Protection

As a business user,
I want domain-specific rejection messages that tell me what went wrong, destructive action confirmation dialogs that prevent accidents, and form abandonment protection that saves my work,
So that I never lose data to accidental navigation, never misunderstand an error, and never accidentally destroy something.

**Acceptance Criteria:**

**Given** a command is rejected by the backend
**When** the rejection message renders
**Then** it follows the format: "[What failed]: [Why]. [What happened to the data]." (e.g., "Approval failed: insufficient inventory. The order has been returned to Pending.")
**And** no generic "Action failed" messages are used
**And** FluentMessageBar (Danger) renders with no auto-dismiss
**And** aria-live="assertive" announces the rejection
**And** form input is preserved on rejection (never clear form on error)

**Given** a command produces an idempotent outcome (rejected but intent fulfilled)
**When** the outcome renders
**Then** the message acknowledges success, not failure (e.g., "This order was already approved (by another user). No action needed.")
**And** FluentMessageBar (Info) renders with 3-second auto-dismiss

**Given** a [Command] is annotated or identified as destructive (Delete, Remove, Purge)
**When** the business user clicks the action
**Then** a FluentDialog confirmation appears with: action name as title, description of what will be destroyed, "This action cannot be undone." text
**And** Cancel button has Secondary appearance and is auto-focused (prevents accidental Enter confirmation)
**And** destructive action button has Danger appearance with domain-language label
**And** Escape closes the dialog
**And** destructive actions never appear as inline buttons on DataGrid rows

**Given** a business user is on a full-page form for >30 seconds
**When** the user attempts navigation (breadcrumb, sidebar, command palette)
**Then** FluentMessageBar (Warning) appears at the form top: "You have unsaved input. [Stay on form] [Leave anyway]"
**And** "Stay on form" is Primary and auto-focused
**And** "Leave anyway" is Secondary
**And** the threshold is configurable via FrontComposerOptions.FormAbandonmentThresholdSeconds (default 30)

**Given** non-destructive commands (Approve, Create, Update)
**When** the user submits
**Then** no confirmation dialog is shown (lifecycle wrapper provides feedback)

**Given** the button hierarchy across all command rendering modes
**When** buttons render
**Then** Primary appearance is used for the main action (one per visual context)
**And** Secondary for supporting actions
**And** Outline for tertiary/filter toggles
**And** Danger only for destructive actions (always with confirmation)
**And** all buttons use domain-language labels (e.g., "Send Create Order" not "Submit")
**And** Primary and DataGrid row action buttons include leading icons

**Given** a business user experiences their first command rejection or error
**When** the error state resolves
**Then** the user can clearly understand: what happened, what state their data is in, and what action to take next (retry, modify input, or abandon)
**And** the recovery path requires zero external documentation -- the UI itself guides recovery
**And** after recovery, the user's confidence is restored (no lingering "did it work?" uncertainty)

**References:** FR30, UX-DR36, UX-DR37, UX-DR38, UX-DR39, UX-DR46, UX-DR58, NFR46, NFR47, NFR103

---

**Epic 2 Summary:**
- 5 stories covering all 5 FRs (FR1, FR8, FR23, FR30, FR36)
- Relevant NFRs woven into acceptance criteria (NFR1-2, NFR11-14, NFR44-45, NFR47, NFR88)
- Relevant UX-DRs addressed (UX-DR2, UX-DR3, UX-DR12, UX-DR16, UX-DR17, UX-DR19, UX-DR21, UX-DR22, UX-DR36-39, UX-DR46, UX-DR48-49, UX-DR58)
- Scope boundary: all stories assume stable connection; degraded-path handling is Epic 5
- Stories are sequentially completable: 2.1 (form generation) -> 2.2 (density rules) -> 2.3 (lifecycle state) -> 2.4 (visual feedback) -> 2.5 (rejection/confirmation)

---
