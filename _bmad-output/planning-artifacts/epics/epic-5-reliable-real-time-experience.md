# Epic 5: Reliable Real-Time Experience

Business user gets instant feedback on command outcomes, sees live data updates via SignalR, and experiences graceful recovery from network interruptions -- with batched reconnection sweeps, preserved form state, ETag-based caching, idempotent handling, domain-specific rejection messages, polling fallback, structured logging with distributed tracing, and a SignalR fault injection test harness. **Extends Epic 2's happy path for degraded/disconnected conditions.**

### Story 5.1: EventStore Service Abstractions

As a developer,
I want all event-store communication abstracted behind swappable service contracts,
So that the framework is decoupled from infrastructure providers and I can swap implementations without changing application code.

**Acceptance Criteria:**

**Given** the framework's EventStore communication layer
**When** the service contracts are inspected
**Then** ICommandDispatcher is defined with a method to dispatch commands returning a correlation ID
**And** IQueryExecutor is defined with methods to execute queries with ETag support (If-None-Match headers)
**And** IProjectionSubscription is defined with methods to subscribe/unsubscribe to projection groups and receive change nudges
**And** all three contracts are in the Contracts package (no infrastructure dependencies)

**Given** the service contracts
**When** the default implementations are inspected
**Then** ICommandDispatcher sends POST /api/v1/commands and expects 202 Accepted
**And** IQueryExecutor sends POST /api/v1/queries and expects 200 + ETag
**And** IProjectionSubscription connects to /projections-hub via SignalR
**And** all communication uses camelCase JSON wire format

**Given** a consumer project references the framework
**When** it registers EventStore services
**Then** services are registered via DI with swappable implementations
**And** the consumer can replace any contract implementation without modifying framework code

**Given** the DAPR infrastructure strategy
**When** infrastructure is accessed
**Then** DAPR is the abstraction layer (no custom wrapper on top of DAPR)
**And** all infrastructure (state, pubsub, secrets) goes through DAPR component bindings
**And** DAPR itself is a permitted direct dependency

**Given** wire format constraints
**When** messages are exchanged
**Then** no colons appear in ProjectionType, TenantId, or domain names (DAPR actor ID separator)
**And** max 10 If-None-Match headers per request
**And** max 1MB request body
**And** ULID message IDs are used for command idempotency

**References:** FR32, NFR74 (zero direct infrastructure coupling), Architecture EventStore Communication

---

### Story 5.2: HTTP Response Handling & ETag Caching

As a business user,
I want the framework to handle all server responses gracefully and cache data intelligently,
So that I see appropriate feedback for every situation and the application feels fast even with repeated queries.

**Acceptance Criteria:**

**Given** the framework receives an HTTP response
**When** the response status is evaluated
**Then** 200 OK: data renders normally with ETag stored
**And** 202 Accepted: command acknowledged, lifecycle transitions to Acknowledged
**And** 304 Not Modified: cached data is current, no re-render
**And** 400 Bad Request: validation errors surface inline on form fields
**And** 401 Unauthorized: redirect to authentication flow
**And** 403 Forbidden: "You don't have permission to [action]" FluentMessageBar (Warning)
**And** 404 Not Found: "This [entity] no longer exists" FluentMessageBar (Warning)
**And** 409 Conflict: domain-specific rejection with entity name and resolution
**And** 429 Too Many Requests: "Please wait before retrying" with retry-after indication

**Given** a successful query response with an ETag
**When** the result is cached
**Then** the cache entry is scoped to {tenantId}:{userId} with the projection snapshot
**And** cache entries are stored via IStorageService (LocalStorageService in WASM, InMemoryStorageService in Server)
**And** SetAsync is fire-and-forget (does not block render)
**And** LRU eviction applies when max entries is reached (configurable)
**And** cache key follows pattern: {tenantId}:{userId}:{featureName}:{discriminator}

**Given** the ETag cache
**When** a subsequent query is made for the same projection
**Then** the cached ETag is sent via If-None-Match header
**And** if 304 is returned, the cached data is used without network data transfer
**And** correctness comes from server queries (cache is opportunistic)

**Given** the application is closing
**When** the beforeunload event fires
**Then** FlushAsync is called via JS interop in App.razor
**And** pending cache writes are flushed to storage

**Given** zero PII requirements (NFR17)
**When** cache contents are inspected
**Then** only projection snapshots (business data from server, not user-entered data) are cached
**And** no PII is stored at the framework layer

**Given** ETag cache key security
**When** cache keys are constructed
**Then** all cache key components (tenantId, userId, featureName, discriminator) are framework-controlled values derived from JWT claims and compile-time projection type names
**And** no user-supplied input is used in cache key construction
**And** cache key manipulation by client-side code cannot access another user's cached data within the same tenant

**References:** FR33, FR34, NFR17-19, Architecture IStorageService Contract

---

### Story 5.3: SignalR Connection & Disconnection Handling

As a business user,
I want the application to detect when my connection drops and preserve my in-progress work,
So that network interruptions don't disrupt my workflow or lose my unsaved form data.

**Acceptance Criteria:**

**Given** the SignalR hub connection to /projections-hub
**When** the connection is established
**Then** the client subscribes to projection groups using pattern {projectionType}:{tenantId}
**And** the connection receives lightweight ProjectionChanged nudges (never full data payloads)
**And** on receiving a nudge, the client re-queries via REST with ETag for actual data

**Given** a stable SignalR connection
**When** HubConnectionState transitions to Disconnected
**Then** a warning-colored inline note displays immediately (NFR15)
**And** the note does not disrupt the user's in-flight workflow (no modal, no overlay)
**And** auto-reconnect begins with exponential backoff (NFR39)

**Given** a command is in the Syncing state (300ms-2s window)
**When** SignalR disconnects during this window
**Then** FcLifecycleWrapper escalates immediately to timeout message: "Connection lost -- unable to confirm sync status"
**And** the sync pulse does NOT continue indefinitely waiting for confirmation (UX-DR50)
**And** ETag polling fallback is activated

**Given** a business user has an in-progress command form
**When** the SignalR connection drops
**Then** all unsaved field values are preserved in component state
**And** on reconnection, the form state is restored without data loss
**And** the user can continue editing immediately after reconnection

**Given** the command lifecycle timeout
**When** 30 seconds elapse without confirmation (configurable via FrontComposerOptions.CommandTimeoutSeconds)
**Then** the lifecycle transitions to timeout state with a manual refresh option

**Given** a business user experiences their first connection loss during active work
**When** the disconnection indicator appears
**Then** the user understands: their in-progress work is safe (form state preserved), the system is attempting to reconnect, and no action is required from them
**And** the messaging tone is reassuring, not alarming ("Connection interrupted -- your work is saved. Reconnecting...")
**And** on reconnection, a brief confirmation restores confidence without requiring the user to verify data manually

**Implementation note:** This story covers 4 distinct features (hub connection, loss detection, lifecycle escalation, form preservation). It may be split into sub-stories during implementation if scope proves too large for a single dev agent session.

**References:** FR22, FR24, UX-DR2 (Disconnected state), UX-DR50, NFR15, NFR39, NFR43

---

### Story 5.4: Reconnection, Reconciliation & Batched Updates

As a business user,
I want the application to seamlessly recover after a network interruption -- rejoining subscriptions, catching up on missed changes, and showing me what changed in one smooth sweep,
So that I trust the data I see is current without needing to manually refresh.

**Acceptance Criteria:**

**Given** SignalR reconnection succeeds
**When** HubConnectionState transitions to Reconnected
**Then** the client automatically rejoins all previously subscribed projection groups (NFR40)
**And** for each visible projection, an ETag-conditioned GET is issued to reconcile stale state

**Given** ETag-conditioned catch-up queries return changed projections
**When** stale rows are identified
**Then** all stale rows receive a single batched CSS animation sweep simultaneously (not per-row flashes, NFR41)
**And** the animation respects prefers-reduced-motion (instant update if enabled)

**Given** the batched reconciliation completes with changes found
**When** FcSyncIndicator processes the results
**Then** a "Reconnected -- data refreshed" FluentMessageBar (Info) auto-dismisses after 3 seconds (NFR42)
**And** the header reconnection indicator clears

**Given** the batched reconciliation completes with NO changes found
**When** FcSyncIndicator processes the results
**Then** the header indicator silently clears
**And** no toast or notification is shown

**Given** FcSyncIndicator in the shell header during disconnection
**When** the connection is lost
**Then** "Reconnecting..." text with subtle pulse displays in the header
**And** aria-live="polite" announces the header status
**And** role="status" is set on any toast notifications

**Given** a schema evolution mismatch is detected at startup or after reconnection
**When** projection types don't match expected schemas
**Then** a clear diagnostic is logged
**And** "This section is being updated" message shows to business users instead of empty/stale data
**And** all cached ETags for the affected projection type are invalidated

**Given** schema bidirectional compatibility requirements (NFR48-50)
**When** event schemas or projection types evolve within a major version
**Then** new code can deserialize events from prior minor versions (backward-compatible reads)
**And** old code tolerates unknown fields from newer versions (forward-compatible serialization)
**And** schema evolution tests cover bidirectional deserialization matrix for shipped minor versions

**Implementation note:** This story covers 5 distinct features (group rejoin, ETag catch-up, batched animation, toast notification, schema evolution). It may be split into sub-stories during implementation if scope proves too large for a single dev agent session.

**References:** FR25, FR26, FR27, UX-DR5, UX-DR39 (Info toast 3s), UX-DR51, NFR40-42, NFR48-50

---

### Story 5.5: Command Idempotency & Optimistic Updates

As a business user,
I want commands that land during a disconnection to resolve correctly on reconnection, with optimistic badge updates that show me the expected state immediately,
So that I'm never confused by duplicate outcomes, stale badges, or missing status changes after network recovery.

**Acceptance Criteria:**

**Given** a command was submitted before disconnection
**When** the command lands on the backend during the disconnect
**Then** on reconnection, the correct terminal state (Confirmed or Rejected) is produced
**And** no user-visible duplication occurs (no double success notifications, no phantom state changes)
**And** deterministic duplicate detection via ULID message ID prevents replay issues

**Given** a command is submitted (stable or reconnected)
**When** the optimistic update pattern activates
**Then** FcDesaturatedBadge transitions the status badge immediately to the target state with desaturated color (filter: saturate(0.5))
**And** on SignalR Confirmed: 200ms CSS transition restores full saturation
**And** on Rejected: badge reverts to the pre-optimistic confirmed state
**And** on IdempotentConfirmed: badge skips revert animation and saturates directly
**And** aria-label includes state during Syncing (e.g., "[Status] (confirming)")
**And** text label is always present (color never sole signal)
**And** prefers-reduced-motion makes the transition instantaneous

**Given** a new entity is created via command and does not match current filter criteria
**When** the creation is confirmed
**Then** FcNewItemIndicator renders the new row at the top of the DataGrid with subtle highlight (Fluent info-background at 10% opacity)
**And** text "New -- may not match current filters" displays
**And** auto-dismisses after 10 seconds or on next filter change with 300ms fade-out
**And** aria-live="polite" on the row, aria-describedby for indicator text

**Given** a domain-specific command rejection during degraded network conditions
**When** the rejection reaches the client on reconnection
**Then** the rejection message format applies: "[What failed]: [Why]. [What happened to the data]."
**And** form input is preserved (never cleared on error)
**And** FluentMessageBar (Danger) with no auto-dismiss

**Given** SignalR is completely unavailable
**When** the polling fallback activates
**Then** ETag-gated polling queries at configured intervals maintain projection correctness
**And** the polling preserves the same user-visible behavior as SignalR (updates appear, lifecycle resolves)
**And** the polling fallback is transparent to the business user

**Given** a business user returns from a disconnection where commands were in flight
**When** the reconciliation completes
**Then** the user can see a clear summary of what happened to each pending command (confirmed, rejected, or idempotent)
**And** no manual verification or page refresh is needed to confirm the final state
**And** the experience feels like "the system handled it" rather than "something went wrong"

**Implementation note:** This story covers 5 distinct features (idempotent outcomes, optimistic updates, new item indicator, polling fallback, rejection during disconnect). It may be split into sub-stories during implementation if scope proves too large for a single dev agent session.

**References:** FR28, FR29, FR31, UX-DR6, UX-DR8, UX-DR46, UX-DR47, NFR44-47

---

### Story 5.6: Build-Time Infrastructure Enforcement & Observability

As a developer,
I want build-time guarantees that no framework code directly couples to infrastructure providers, and structured logging across the full lifecycle,
So that the framework remains portable across deployment targets and I can trace any operation end-to-end.

**Acceptance Criteria:**

**Given** the framework assemblies
**When** a CI build runs
**Then** an automated check asserts zero direct references to Redis, Kafka, Postgres, CosmosDB, or DAPR SDK types from framework assemblies
**And** all infrastructure access routes through DAPR component bindings
**And** violations fail the build with a descriptive error message

**Given** the framework's runtime services
**When** structured logging is inspected
**Then** OpenTelemetry semantic conventions are followed
**And** every log entry includes: CommandType or ProjectionType, TenantId, CorrelationId
**And** message template + parameters are used (never string interpolation)
**And** log levels follow: Debug (dev), Information (flow), Warning (degraded), Error (intervention)

**Given** the framework's distributed tracing
**When** FrontComposerActivitySource is used
**Then** it is a shared static ActivitySource name across all framework packages
**And** traces span: user click -> backend command -> projection update -> SignalR nudge -> UI update
**And** tracing is compatible with Grafana, Jaeger, and Application Insights

**Given** the framework runs on different deployment targets
**When** it is deployed to on-premise (bare Aspire), sovereign cloud (Kubernetes), Azure Container Apps, AWS ECS/EKS, or GCP Cloud Run
**Then** behavior is identical across all targets (NFR73)
**And** CI validates Aspire, local Kubernetes, and Azure Container Apps configurations

**References:** FR48, FR72, NFR73, NFR74, NFR79, NFR80

---

### Story 5.7: SignalR Fault Injection Test Harness

As a developer,
I want a test harness that simulates SignalR connection faults without requiring a live server,
So that I can write reliable unit and integration tests for all reconnection and resilience behaviors.

**Acceptance Criteria:**

**Given** the SignalR fault injection test harness
**When** a test configures a fault scenario
**Then** the following fault types are supported: connection drop, connection delay, partial message delivery, and message reorder
**And** faults can be triggered programmatically at precise points in the command lifecycle

**Given** a test using the fault harness
**When** a connection drop is simulated during a command's Syncing state
**Then** the test can assert: FcLifecycleWrapper escalates to timeout, form state is preserved, and reconnection triggers catch-up query

**Given** a test using the fault harness
**When** a message reorder is simulated (ProjectionChanged arrives before command acknowledgment)
**Then** the test can assert: the lifecycle state machine handles the out-of-order events correctly without invalid state transitions

**Given** the fault harness
**When** used alongside the existing test infrastructure
**Then** it integrates with xUnit + bUnit test patterns
**And** it does not require a live SignalR server, live EventStore, or live network
**And** 90% of reconnection behaviors (FR24-29) are testable at unit level via the harness (NFR59)

**Given** the fault harness as a test utility
**When** an adopter references it
**Then** it is available as part of the framework's test host package (FR71, Epic 10)
**And** test data follows the Builder pattern for domain models

**References:** FR82, NFR53, NFR59, Architecture testing infrastructure

---

**Epic 5 Summary:**
- 7 stories covering all 14 FRs (FR22, FR24-29, FR31-34, FR48, FR72, FR82)
- Relevant NFRs woven into acceptance criteria (NFR15-19, NFR39-47, NFR53, NFR59, NFR73-74, NFR79-80)
- Relevant UX-DRs addressed (UX-DR2, UX-DR5-6, UX-DR8, UX-DR39, UX-DR46-47, UX-DR50-51)
- Layered story ordering: 5.1-5.2 (basic communication) -> 5.3 (connectivity) -> 5.4 (reconciliation) -> 5.5 (resilience) -> 5.6 (enforcement/observability) -> 5.7 (test harness)
- Each story explicitly states which degraded condition it handles

---
