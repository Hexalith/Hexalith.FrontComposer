# Story 3.6: Session Persistence & Context Restoration

Status: ready-for-dev

> **Epic 3 § 255-299** · **FR19** · **UX-DR20** · **UX-DR53** · **NFR17** · **NFR90** · applies lessons **L01, L02, L03, L06, L07, L09, L10**. Closes the `StorageReady` late-scope-resolution hook opened by Story 3-4 deferred-work (`CommandPaletteEffects.HandleAppInitialized` prerender branch). Ships the **persistence side** of Story 2-2's `DataGridNavigationState` reducer-only contract (Decision D30 — "persistence and DataGrid capture-side wiring land in Story 4.3" is superseded: 3-6 lands the **persistence + hydrate** halves; Story 4.3 still owns the DataGrid **capture-side renderer wiring**). Introduces last-active-route restoration on the existing `FrontComposerNavigationState` blob schema.

---

## Executive summary (Feynman-level, ~30 sec)

Three things land in this story:

1. **Last-active-route restoration on app boot.** `FrontComposerNavigationState` gains a nullable `LastActiveRoute` field. `NavigationEffects` persists it on every `BoundedContextChangedAction` with a non-null bounded-context segment (filters home + non-domain routes) and hydrates it alongside the existing sidebar / collapsed-groups blob. `FrontComposerShell.OnAfterRenderAsync(firstRender)` consults the hydrated state: if the user landed on `/` AND a valid, still-registered last route exists, it issues `NavigationManager.NavigateTo(lastRoute)` once. First-visit users (empty blob) stay on the home directory from Story 3-5 (AC4b path) — no error, no warning.

2. **Per-view DataGrid persistence.** Story 2-2 shipped `DataGridNavigationState` + `GridViewSnapshot` + `{Capture,Restore,Clear,PruneExpired}GridStateAction` as reducer-only (Decision D30). 3-6 adds the effect half: hydrate all snapshots on `AppInitialized` under key prefix `{tenantId}:{userId}:datagrid:`, persist a snapshot on `CaptureGridStateAction` (debounced 250 ms per view key), and clear the stored key on `ClearGridStateAction`. `RestoreGridStateAction` gains an effect handler that reads storage and dispatches a new `GridViewHydratedAction(viewKey, snapshot)` when found — renderer mounts (Story 4.3 territory) can dispatch `RestoreGridStateAction` and the snapshot flows into state without the renderer touching storage directly. Persistence is best-effort; in-memory `ViewStates` is always authoritative for the current circuit.

3. **`StorageReadyAction` — late-scope-resolution hook.** Story 3-4 deferred-work line 43 called for this. Prerender-pass server circuits observe `IUserContextAccessor.TenantId == null` even for authenticated users (claims arrive with the interactive boot). A new **`IScopeReadinessGate`** service (D20, Winston) is injected into `ScopeFlipObserverEffect`; the effect's 8 `[EffectMethod]` handlers each delegate to `_gate.EvaluateAsync(dispatcher)`. The gate reads `IUserContextAccessor` + `state.Value.StorageReady`, short-circuits if the flag is true, and on the first empty→non-empty transition dispatches `StorageReadyAction` once per circuit. Every hydrate effect (Theme, Density, Navigation, CommandPalette, CapabilityDiscovery, DataGridNavigation) subscribes; each gates re-hydrate via an explicit **`HydrationState` enum** (D19, Amelia) — `Idle | Hydrating | Hydrated` — eliminating the silent false-negative of default-value proxy gates. The hook replaces the fail-closed-and-forget floor of 3-4 with a single, symmetric re-hydrate that doesn't touch the happy-path cost.

The design keeps FR19 satisfied end-to-end without introducing a new storage abstraction, holds decisions inside the L06 feature-story cap (21 binding decisions ≤ 25 after advanced-elicitation D21 addition), and leaves every cross-story seam explicit per L01.

---

## Story

As a business user,
I want to return to exactly where I left off — same navigation section, same filters, same sort order, same expanded row —,
so that context switches (lunch, meetings, browser restarts) don't cost me time re-establishing my workspace.

**Business value:** Converts FrontComposer from "a session-per-visit tool" to "an always-on operator shell". A user who processes 50 items across 3 bounded contexts daily cannot afford to re-apply filters and re-navigate every session. When LocalStorage is unavailable, the app starts from the home directory without error — the PRD Journey 4 skepticism-check moment ("1:10 — she refreshes the page … Session persistence lands her back on the same view, same filters, same sort, same expanded row. She sits back 2cm in her chair. *That's* the moment she trusts it.") is the UX target.

---

## Cross-story contract table (L01)

| Contract | Producer | Consumer | Binding |
|---|---|---|---|
| `FrontComposerNavigationState.LastActiveRoute` (new nullable string field, additive) | 3-6 `NavigationReducers.ReduceLastActiveRouteChanged` / `ReduceLastActiveRouteHydrated` | 3-6 `FrontComposerShell.OnAfterRenderAsync(firstRender)` for one-shot auto-navigation | Append-only — serialised into the existing `NavigationPersistenceBlob` via an additional nullable field. Pre-3-6 stored blobs deserialise with `LastActiveRoute == null`; feature defaults (no auto-navigation) apply. Post-3-6 blobs carry the field. **Null convention bound:** `null` = "no prior route captured" (first-visit or user-explicitly-on-home-at-capture-time); empty string `""` is INVALID and never persisted (reducer guards: if `action.Route` is null/empty/whitespace after trimming, treat as `null`). Consumers compare `LastActiveRoute is not null && LastActiveRoute.Length > 0` — never `string.IsNullOrEmpty` (guards against a future empty-string leak). Schema locked via `NavigationPersistenceBlobSchemaLockedTests.LastActiveRouteFieldPresent`. |
| `LastActiveRouteChangedAction(string? Route)` / `LastActiveRouteHydratedAction(string? Route)` Fluxor actions | 3-6 (`NavigationEffects.HandleBoundedContextChanged` emits `LastActiveRouteChangedAction`; `HandleAppInitialized` emits `LastActiveRouteHydratedAction` after reading the blob) | 3-6 `NavigationReducers` (update state), 3-6 `NavigationEffects.HandleLastActiveRouteChanged` (persist) | Additive — two new actions on the `FrontComposerNavigation` feature surface. The persist effect follows the same fail-closed L03 pattern that 3-2 ships for `SidebarToggledAction` / `NavGroupToggledAction`. |
| `DataGridNavigationEffects` concrete class (NEW) wiring `CaptureGridStateAction` / `RestoreGridStateAction` / `ClearGridStateAction` to `IStorageService` | 3-6 | Story 4.3 DataGrid renderer (future producer of `CaptureGridStateAction` from on-filter / on-sort / on-scroll / on-row-expand callbacks; future producer of `RestoreGridStateAction` from the renderer's `OnInitialized`) | The effect consumes the existing `CaptureGridStateAction(viewKey, snapshot)` record frozen by Story 2-2. 4.3's renderer wiring is **in-scope for 4.3 only** — 3-6 asserts the persistence layer works end-to-end via effect-level tests that dispatch the actions directly, no renderer required. |
| `GridViewHydratedAction(string ViewKey, GridViewSnapshot Snapshot)` Fluxor action (NEW) | 3-6 `DataGridNavigationEffects.HandleAppInitialized` (per-key hydrate), `HandleRestoreGridState` (on-demand hydrate) | 3-6 `DataGridNavigationReducers.ReduceGridViewHydrated` (adds the snapshot into `ViewStates` iff the key is absent — in-memory wins over storage) | Additive — one new reducer on the existing `DataGridNavigationState` feature. Reducer contract: present-key = no-op (preserves in-memory); absent-key = insert. |
| Storage key prefix `{tenantId}:{userId}:datagrid:{viewKey}` (4-segment canonical form) where `viewKey` is the existing Story 2-2 `"{boundedContext}:{projectionTypeFqn}"` string | 3-6 | Only 3-6 writes; only 3-6 reads (adopters don't touch storage directly) | Uses `StorageKeys.BuildKey(tenantId, userId, "datagrid", viewKey)` — the same 4-arg overload declared in `src/Hexalith.FrontComposer.Shell/State/StorageKeys.cs`. Nested colons inside `viewKey` are by design (Story 2-2 convention); no parser consumes the key, so the nesting is inert. |
| `StorageReadyAction(string CorrelationId)` Fluxor action (NEW) | 3-6 `NavigationEffects` observes scope-flip after each action; dispatches once per circuit when `IUserContextAccessor` transitions empty → non-empty | All hydrate effects: `ThemeEffects`, `DensityEffects`, `NavigationEffects`, `CommandPaletteEffects`, `CapabilityDiscoveryEffects`, `DataGridNavigationEffects` (new). Each checks its own hydration-state gate to avoid double-apply. | Closes the Story 3-4 deferred-work line 43 hook ("Story 3-6 will introduce a proper `StorageReady` event that re-fires hydrate on late scope resolution"). One-shot per circuit (gated via transient `FrontComposerNavigationState.StorageReady` bool — NEVER persisted). |
| `FrontComposerNavigationState.StorageReady` transient bool (NEW, never persisted) | 3-6 `NavigationReducers.ReduceStorageReady` sets to true on `StorageReadyAction` | 3-6 `IScopeReadinessGate` (D20) reads this flag to avoid re-dispatching `StorageReadyAction` on every subsequent action | **Lifecycle bound:** (a) initial state `false` on every circuit boot (feature's `GetInitialState`); (b) flips `true` exactly once per circuit via `ReduceStorageReady` on `StorageReadyAction` dispatch; (c) NEVER reset within a circuit — sign-out mid-circuit leaves it `true` (future writes fail-closed on the L03 scope guard, not on the flag); (d) NEVER serialised — explicitly excluded from `NavigationPersistenceBlob` (schema-lock test `NavigationPersistenceBlobSchemaLockedTests.BlobSchemaMatches` fails if field leaks into JSON); (e) circuit teardown discards the flag along with the rest of the Fluxor store. Mirrors ADR-037's precedent for `CurrentViewport` / `CurrentBoundedContext`. |
| One new diagnostic ID `HFC2114_DataGridHydrationEmpty` (Information severity; mirrors `HFC2106` / `HFC2107` / `HFC2111`) | 3-6 | Operators / telemetry dashboards; Story 9-4 will layer a build-time analyzer for higher-severity HFC3xxx equivalents | `Reason` payload: `Empty` (no blob at key), `Corrupt` (deserialization / shape failure), `OutOfScope` (stored viewKey's bounded-context is no longer registered in `IFrontComposerRegistry.GetManifests()` — pruned on hydrate). Structured payload appendable; existing fields frozen. |
| NFR17 compliance (zero PII, zero business data at framework layer) — framework writes ONLY: `ScrollTop` (double), `SortColumn` (column key string), `SortDescending` (bool), `ExpandedRowId` (row-identity token string), `SelectedRowId` (row-identity token string), `Filters` (`Dictionary<string,string>` column-name → filter-text), `CapturedAt` (`DateTimeOffset` UTC) | 3-6 | All storage writes | Trust model documented in **Dev Notes § NFR17 Compliance**: filter-text IS user-authored (may contain business data if a user types "ACME" as a filter); equivalent to browser autocomplete / history trust model. Framework NEVER proactively reads server entity bodies for storage. Re-asserted by `NFR17ComplianceAuditTests.BlobDoesNotCarryEntityData`. |

---

## Acceptance Criteria

| AC | Given / When / Then | Primary tasks |
|---|---|---|
| **AC1** | **Given** a returning user with prior session state in LocalStorage (non-empty `FrontComposerNavigationState` blob carrying `LastActiveRoute`, plus ≥ 1 persisted `GridViewSnapshot` under `{tenantId}:{userId}:datagrid:*`). **When** the application loads and lands on `/`. **Then** on first render, `FrontComposerShell` auto-navigates to `LastActiveRoute` **iff** the route's bounded-context segment resolves to a currently-registered manifest. **And** if the restored route is a DataGrid view, its persisted `GridViewSnapshot` (filters, sort column + direction, expanded row id, selected row id, scroll position, capturedAt) is already hydrated into `DataGridNavigationState.ViewStates` ready for the renderer (Story 4.3 renderer consumes on mount — 3-6's coverage is the state-layer round-trip). **And** the experience matches NFR90 (session resumption). | T2, T4, T8 |
| **AC2** | **Given** a first-visit user with no session state (no nav blob, no DataGrid keys). **When** the application loads. **Then** the user sees the Story 3-5 `FcHomeDirectory` first-visit state (AC4b path — `HomeFirstVisitText`). **And** no error, warning, or loading placeholder is shown beyond the existing `BadgeCountService` seeding skeletons. **And** no auto-navigation is attempted. | T4, T8 |
| **AC3** | **Given** LocalStorage is unavailable (IT policy lockdown, quota-full, private-browsing, JS interop failure). **When** the application loads. **Then** the user starts from the home directory without error. **And** no error messages, warnings, or degraded-UI indicators are surfaced to the user. **And** every persistence-layer read / write logs `HFC2105_StoragePersistenceSkipped` at Information with a `Direction` payload (`hydrate` or `persist`) so operators can distinguish hydrate-side from persist-side failures. **And** the application functions normally for the in-memory session — within-session `DataGridNavigationState.ViewStates` still captures filters / sort / scroll / expanded-row for return-from-form navigation. | T3, T4, T5 |
| **AC4** | **Given** session state is being persisted. **When** state changes occur (navigation BC-change, DataGrid filter change, sort change, row expansion, row selection, scroll). **Then** each write is issued against a tenant/user-scoped key: `{tenantId}:{userId}:nav` (sidebar + collapsed-groups + last-active-route) or `{tenantId}:{userId}:datagrid:{viewKey}` (per-view snapshot). **And** per-view DataGrid writes are debounced 250 ms via per-viewKey `CancellationTokenSource` so rapid user interaction (e.g., scroll-while-typing) does not thrash storage. **And** writes use compact JSON (camelCase property names; default values omitted via `JsonSerializerDefaults.Web` + `DefaultIgnoreCondition.WhenWritingDefault`). **And** only UI preference state is stored — zero PII, zero framework-initiated business data (NFR17). | T3, T5 |
| **AC5** | **Given** a user navigates to a DataGrid, applies filters, sorts, and expands a row, then navigates away and returns. **When** the navigation round-trip happens within the same circuit. **Then** the DataGrid state (scroll position, filters, sort, expanded row, selected row) is restored from `DataGridNavigationState.ViewStates` (in-memory — authoritative for within-session per Story 2-2 AC7 / D30). **When** the navigation round-trip spans a browser restart or a new circuit. **Then** the cross-session subset (filters, sort, expanded row, selected row — scroll position is best-effort since DOM height is DPR-dependent) is restored from LocalStorage via `DataGridNavigationEffects.HandleAppInitialized` hydrating every `{tenantId}:{userId}:datagrid:*` key into state on boot. **And** the hydrate-side DROPS any snapshot whose bounded-context segment is no longer registered in `IFrontComposerRegistry.GetManifests()` (logs `HFC2114` `Reason=OutOfScope` once per dropped key). | T2, T3, T6 |
| **AC6** | **Given** filter values persisted in LocalStorage. **When** the persistence mechanism serialises filter state. **Then** only filter **metadata** is stored (column key, operator if Story 4.3 surfaces one, filter text) — framework NEVER serialises the underlying entity data. **And** if filter text contains business data (e.g., a customer name the user typed as a filter), this is acknowledged in Dev Notes as a user-initiated browser-local storage with the same trust model as browser history / autocomplete. **And** no server-side business data is proactively written to LocalStorage by the framework (a blocking assertion in `NFR17ComplianceAuditTests.BlobDoesNotCarryEntityData` scans all persist-call sites under `src/Hexalith.FrontComposer.Shell/State/` for `IStorageService.SetAsync` invocations carrying non-whitelisted types). | T6, T7 |
| **AC7a** | **Given** the Blazor Auto prerender pass executes with an anonymous `IUserContextAccessor` (tenant / user not yet available — claims arrive with the interactive boot). **When** any of the 8 observed user-interaction actions dispatches AFTER `IUserContextAccessor` transitions to authenticated scope (non-null tenant + non-null user). **Then** `IScopeReadinessGate` (D20) observes the transition synchronously BEFORE reducers receive the action. **And** on the first observed transition (`StorageReady == false` && scope now non-null), the gate dispatches `StorageReadyAction(ulid)` exactly once. **And** the scope check must precede any other work in the handler chain (ordering asserted by `ScopeFlipObserverEffectTests.ScopeCheckPrecedesSubscriberWork`). | T5, T7 |
| **AC7b** | **Given** `StorageReadyAction` has been dispatched once in the current circuit. **When** any subsequent observed action fires (same circuit, same or different action type). **Then** the gate MUST NOT dispatch `StorageReadyAction` again (`state.Value.StorageReady == true` short-circuits). **And** every hydrate effect (`ThemeEffects`, `DensityEffects`, `NavigationEffects`, `CommandPaletteEffects`, `CapabilityDiscoveryEffects`, `DataGridNavigationEffects`) subscribes to `StorageReadyAction` and re-runs its hydrate path **iff** its per-feature `HydrationState` enum field equals `Idle` (see D19) — the enum gate prevents double-apply when the prerender pass happened to succeed. **And** a scope-flip back to anonymous (sign-out mid-circuit) does NOT reset `StorageReady`; future persist calls fail-closed on the L03 scope guard, not on the flag. | T3, T4, T5, T7 |
| **AC8** | **Given** the full state-layer round-trip (persist → dispose store → new store → hydrate → render). **When** the Fluxor store is disposed and reconstructed in-test (simulating a circuit teardown + reboot). **Then** `DataGridNavigationEffectsTests` asserts per-snapshot `GridViewSnapshot` structural equality across the round-trip (property already covered by Story 2-2's custom `Equals` override on the record). **And** `NavigationEffectsLastActiveRouteTests` asserts the same round-trip for the `LastActiveRoute` field. **And** `NavigationPersistenceBlobSchemaLockedTests` + `GridViewPersistenceBlobSchemaLockedTests` assert the JSON wire shape via inline-string equality so future field additions cannot silently break interop. Build-gate enforcement (`dotnet build --warnaserror` + `dotnet test` green) lives in Task 9, not in the AC contract. | T7 |

---

## Critical decisions (READ FIRST — do NOT revisit)

> **21 binding decisions.** Feature-story budget is ≤ 25 (L06). 3-6 lands at 21/25, leaving a 4-decision buffer for review rounds before Occam / Matrix trimming is required. Raise a spec-change proposal before revisiting any entry.
>
> **Post-review rotation:** Pre-trim relocated the original D19 (schema-lock snapshot tests → Testing standards §Schema-lock tests) and D20 (adopter opt-out → Dev Notes §Adopter opt-out) as they are testing-standards / documentation rather than binding code decisions. Added D19 `HydrationState` enums (Amelia blocker #3 — replaces proxy gates) and D20 `IScopeReadinessGate` service (Winston — localises the 8-action allowlist into one file). **Advanced-elicitation pre-mortem pass added D21** (`LastActiveRoute` hydrate-side prune — A1) to close the asymmetry with D14. Count 20 → 21; still within budget.

| # | Decision | Rationale | Consumed by |
|---|---|---|---|
| **D1** | **`LastActiveRoute` lives on the existing `FrontComposerNavigationState` record** as an additional nullable `string?` property (append-only). Serialised via an additional nullable property on `NavigationPersistenceBlob` (same record — 3-6 does NOT introduce a second blob). Pre-3-6 stored blobs deserialise with `LastActiveRoute == null` (feature default — no auto-navigation) because `System.Text.Json` treats missing properties as default. | Extending the existing blob avoids a second storage key + second effect + second set of fail-closed guards for a conceptually-adjacent concern (sidebar + collapsed-groups + last-visited-route are all "where the user is / was in the shell nav"). The additive field keeps the single-key persistence lifecycle intact — one read on hydrate, one write per BC-change or sidebar toggle (coalesced via existing `NavigationEffects.PersistAsync`). Rejected: (a) second blob `SessionRestorationBlob` — doubles the write path + fail-closed branches; (b) store-in-IStorageService-key-per-property — over-granular, no benefit; (c) compress blob on write — premature optimisation, blob is sub-1 KB. | T2; AC1, AC8 |
| **D2** | **Persist `LastActiveRoute` is driven off `BoundedContextChangedAction`** (the existing action dispatched by `FrontComposerShell.HandleLocationChanged` → `SyncCurrentBoundedContext(uri)` per Story 3-4 D7) **with a non-null `NewBoundedContext`**. `null` BC (home route, non-domain routes) does NOT persist — the intent is "remember where I was in a domain", not "remember I visited the home directory." The persist effect reads `NavigationManager.Uri` at the moment the action fires (via injected `NavigationManager`) to capture the full route — not just the BC segment — so returning restores the deep link, not only the BC landing page. **NFR17 boundary (A10):** `NavigationManager.Uri` captures query strings and fragments verbatim. If an adopter's routing convention includes sensitive query parameters (auth tokens, session IDs, PII in filter params), those land in the persisted blob. Adopter responsibility to strip sensitive query segments BEFORE the route surfaces to `NavigationManager` (use an adopter-side `NavigationInterceptor` or route-rewrite middleware). Framework does NOT strip — it trusts the URL shape it is given, consistent with the NFR17 "user-initiated storage" trust model. | Using an existing action is cheaper than introducing a new `LastActiveRouteVisitedAction`. The `NavigationManager.Uri` read inside the effect is idempotent and synchronous; no race with the reducer-side state update because the reducer only touches `CurrentBoundedContext`, not `LastActiveRoute`. Filtering null-BC keeps the "returning user lands on home" path trivial for first-visits and for users who explicitly navigated home before closing the tab. Rejected: (a) new dedicated action — unnecessary surface; (b) capture on every `NavigationManager.LocationChanged` — includes non-domain routes like `/settings` that shouldn't be restored as last-active; (c) drop the action and use `BoundedContextChangedAction.NewBoundedContext` alone as the route — loses deep-link fidelity (users returning to `/domain/counter/projection/counter-list` land on `/domain/counter`). | T2; AC1, AC4 |
| **D3** | **Auto-navigation on app init is one-shot per circuit, gated by four preconditions**: (a) first render has just completed (`FrontComposerShell.OnAfterRenderAsync(firstRender: true)`); (b) `NavigationManager.Uri` normalises to the home route (`/` or `/home` — matches Story 3-5 `FcHomeRouteView` registration D16); (c) `LastActiveRoute` is non-null AND its bounded-context segment resolves to a registered manifest via `IFrontComposerRegistry.GetManifests()` (stale routes for deleted BCs silently fail-soft — see D14 drop-on-hydrate pruning); (d) a one-shot `bool _sessionRestoreAttempted` field on `FrontComposerShell` guards against re-entry (a later hydrate-race or `StorageReadyAction` would otherwise double-fire). The navigation is a plain `NavigationManager.NavigateTo(LastActiveRoute, forceLoad: false)` — no intermediate `StateHasChanged`, no blocked awaiters. | Tying restoration to `OnAfterRenderAsync(firstRender)` lets the Blazor router finish its initial route match before we override — this matters when an adopter deep-links (`https://app/domain/orders/order-list`) and we must NOT stomp their intent. The home-route guard is the "I opened a bookmark / used an app launcher" heuristic; the deep-link guard is the "I pasted a URL" heuristic. Manifest-registered check is L10 defensive (adopter removes a bounded context between sessions — don't navigate to a 404). Rejected: (a) always navigate to last route, ignoring current URL — breaks deep links; (b) use `NavigationManager.LocationChanged` + first-event gate — async ordering against the initial route match is fragile; (c) use an effect instead of a component-owned flag — effects don't have a natural "first-render" signal. | T4; AC1, AC2 |
| **D4** | **Per-view DataGrid persistence writes one key per view**: `StorageKeys.BuildKey(tenantId, userId, "datagrid", viewKey)` where `viewKey` is the existing Story 2-2 `"{boundedContext}:{projectionTypeFqn}"` string (e.g., `"counter:Hexalith.Samples.Counter.Projections.CounterProjection"`). The resulting full storage key has nested colons: `acme:user42:datagrid:counter:Hexalith.Samples.Counter.Projections.CounterProjection`. The colons are by design — Story 2-2 already uses this viewKey internally and no parser consumes the storage key (the full string is a dictionary lookup). | Per-view keys give granular LRU eviction (the `LocalStorageService`'s internal LRU evicts the least-recently-touched key, which maps cleanly to "the view the user hasn't looked at in a while"); a single aggregate blob would force all-or-nothing eviction. Per-view also isolates failure (one corrupted snapshot doesn't nuke the rest). Nested colons are cosmetically ugly but functionally neutral — Story 3-5 established the "colon-in-segment" precedent (`capability-seen:bc:Counter`). Rejected: (a) one aggregate blob per user — all-or-nothing eviction, harder to diff for schema changes; (b) URL-encode viewKey — breaks human-readability in storage dumps; (c) use a different separator for the viewKey (`__`) — deviates from Story 2-2 convention + needs a migration for existing in-memory keys. | T3; AC4, AC5 |
| **D5** | **`GridViewPersistenceBlob` is a new DTO record** in `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/GridViewPersistenceBlob.cs`, NOT `GridViewSnapshot` itself. The DTO mirrors `GridViewSnapshot`'s fields but uses `Dictionary<string,string>` (not `IImmutableDictionary<string,string>`) for `Filters` — matches `NavigationPersistenceBlob.CollapsedGroups` precedent (Story 3-2 D21). `CapturedAt` serialises as ISO-8601 UTC via `DateTimeOffset`'s default `System.Text.Json` handling. The effect converts snapshot ↔ blob on the boundary; reducers continue to see immutable snapshots. | `System.Text.Json` has no built-in `IImmutableDictionary` converter; adding one (or relying on a third-party) is a dependency burden for one nullable field. Mutable `Dictionary<string,string>` is the canonical JSON shape, and the conversion at the effect boundary is a one-line `ToImmutableDictionary` on hydrate + `new Dictionary<string, string>(snap.Filters, StringComparer.Ordinal)` on persist. Separating blob from snapshot lets the snapshot stay immutable-record-first and the blob stay wire-format-first — a consistent "DTO at storage edges" pattern. Rejected: (a) add a custom `ImmutableDictionaryJsonConverter` — adds type-registration ceremony and a maintained helper for one use; (b) store the snapshot directly — `System.Text.Json` serialises `IImmutableDictionary` as `{}` by default (reflection fails on the enumerator contract), silently corrupting data; (c) use `MessagePack` / `Protobuf` — new dependencies, no matching NFR. | T3; AC4, AC8 |
| **D6** | **`DataGridNavigationEffects` ships three effect handlers** in `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs`: (1) `HandleAppInitialized(AppInitializedAction, IDispatcher)` — enumerates storage keys via `IStorageService.GetKeysAsync(prefix: $"{tenantId}:{userId}:datagrid:")`, reads each blob with a **per-key try/catch (A4)** — a corrupt blob at key K logs `HFC2114 Reason=Corrupt` and continues to key K+1 (a single poisoned key does NOT halt enumeration of the remaining keys), dispatches one `GridViewHydratedAction` per valid blob, logs `HFC2114 Reason=OutOfScope` and SKIPS any blob whose viewKey's BC segment is no longer in `IFrontComposerRegistry.GetManifests()`. **Registry-failure handling (A9):** the `IFrontComposerRegistry.GetManifests()` call is wrapped in its own try/catch; on throw, log `HFC2114 Reason=RegistryFailure` ONCE per hydrate pass (not per key) and abandon out-of-scope pruning for that pass — keys are left intact rather than deleted on the basis of an unreadable registry (fail-safe: preserve data over prune); (2) `HandleCaptureGridState(CaptureGridStateAction, IDispatcher)` — debounces 250 ms per viewKey via a `ConcurrentDictionary<string, CancellationTokenSource>` + `Task.Delay(250, ct)`, then reads `state.Value.ViewStates[action.ViewKey]` to capture the REDUCER-APPLIED snapshot (NOT the action payload — avoids races where the reducer just clamped an invalid value), converts to blob, persists; (3) `HandleClearGridState(ClearGridStateAction, IDispatcher)` — removes the storage key. **All three wrap the same `TryResolveScope` helper** from Story 3-1 / 3-2 precedent (null / empty / whitespace tenant or user → log `HFC2105` + return). | Three handlers is the minimum for the three state-mutating actions — anything fewer forces a shared dispatcher handling ambiguous intent. Reading from `state.Value.ViewStates[...]` post-reducer (via Fluxor's `IState<T>` injection) is Story 3-2 / 3-4 / 3-5 precedent. The 250 ms debounce coalesces rapid filter + scroll + sort combos that would otherwise thrash the LocalStorage drain worker. Rejected: (a) no debounce — causes cascading writes on `<FluentDataGrid>` internal scroll events; (b) debounce in the reducer — reducers MUST stay pure; (c) single combined handler switching on action type — harder to test and Fluxor discourages it. | T3; AC4, AC5, AC8 |
| **D7** | **`RestoreGridStateAction` gains an effect handler** `DataGridNavigationEffects.HandleRestoreGridState(RestoreGridStateAction, IDispatcher)` that reads the specific `{tenantId}:{userId}:datagrid:{viewKey}` storage key and dispatches `GridViewHydratedAction(viewKey, snapshot)` when a valid blob is found. This is an **on-demand hydrate** for views that weren't in `ViewStates` at app init (e.g., a user expanding a bounded context for the first time in the session — the app-init hydrate already loaded any previously-seen view, but a never-before-visited view's state could exist in storage if a different browser tab wrote it earlier). The existing Story 2-2 reducer for `RestoreGridStateAction` stays a no-op (D30); the effect does the read. | Two-tier hydrate (eager at app-init + on-demand per renderer mount) gives both fast boot (state is already there for return-from-form navigation) and cross-tab freshness (a second tab's writes appear when the first tab navigates to that view). The reducer no-op preserves Story 2-2's contract unchanged. Rejected: (a) skip on-demand — second-tab writes lag until app restart; (b) effect-side ALSO read-modify in reducer — couples the state to IO; (c) subscribe renderer directly to IStorageService — renderer-level storage reads break the single-read-path invariant and would bypass fail-closed. | T3; AC5 |
| **D8** | **`GridViewHydratedAction(string ViewKey, GridViewSnapshot Snapshot)` is a new Fluxor action** with a new pure reducer `DataGridNavigationReducers.ReduceGridViewHydrated`. The reducer adds the snapshot into `state.ViewStates` **iff the key is absent** (`state.ViewStates.ContainsKey(action.ViewKey) ? state : state with { ViewStates = state.ViewStates.SetItem(action.ViewKey, action.Snapshot) }`). An existing (more recent) in-memory snapshot WINS over a hydrated one — protects against the race where a storage write from one tab happens just before a navigation in another tab. | The write-wins-over-read protects the within-session state (Story 2-2's authoritative in-memory model). In-memory reflects the user's most recent interaction with the view in THIS circuit; storage reflects the most recent write from ANY circuit, which may be stale. Rejected: (a) hydrate overwrites in-memory — within-session data loss; (b) compare `CapturedAt` and take newer — adds time-skew risk and a per-reducer conditional with no concrete benefit when in-memory is already authoritative; (c) merge field-wise (newer-per-field) — complex, no UX win. | T3; AC5, AC8 |
| **D9** | **Hydrate is read-only** — `NavigationHydratedAction`, `LastActiveRouteHydratedAction`, `GridViewHydratedAction` do NOT trigger re-persistence. Mirrors Story 3-2 ADR-038 precedent ("hydrate is read-only from the storage perspective"). Each hydrate effect handler returns after dispatching the hydrated action; no `SetAsync` call in the hydrate path. Guarded by dedicated tests (`HydrateDoesNotRePersist` test pattern — already shipped in `NavigationEffectsTests`; 3-6 adds parallel tests for DataGrid and LastActiveRoute). | Re-persistence on hydrate would create an infinite loop (persist → hydrate → persist) or at best a no-op churn that wastes LocalStorage bandwidth. Rejected: (a) re-persist to refresh `CapturedAt` as part of hydrate — legitimises a write-on-read pattern that's easy to regress into a loop; (b) unconditional persist on every action — architectural fumble, explicitly called out by Story 3-2 ADR-038. | T3; AC8 |
| **D10** | **Graceful degradation on LocalStorage unavailability** — `IStorageService.GetAsync` / `SetAsync` / `RemoveAsync` / `GetKeysAsync` / `FlushAsync` may throw when the browser lockdown (IT policy, private-browsing, quota-exceeded, JS interop failure) prevents access. Every 3-6 effect wraps storage calls in `try { ... } catch (OperationCanceledException) { LogDebug(...); return; } catch (Exception ex) { LogInformation(ex, "{DiagnosticId} Direction={Direction}", HFC2105, "hydrate|persist"); return; }`. No user-visible message surfaces. In-memory state remains authoritative for the session. The Dev Notes §Degraded-Mode-Behaviour matrix documents the UX for each failure mode. | Fail-closed from the first byte of scope resolution through every IO call matches Story 3-1 / 3-2 / 3-4 / 3-5 precedent — the shell boot MUST degrade silently because "storage refuses writes" is not actionable by the user. `OperationCanceledException` specifically is expected on circuit disposal and gets `LogDebug` (not `LogInformation`) to avoid ops-noise. Rejected: (a) surface a toast / banner — violates the UX commitment to silent-degrade; (b) raise a Fluxor action `StorageDegradedAction` — creates an action with no consumer; (c) Warning severity on the log — false positive in the common case (IT lockdown is expected, not a bug). | T3, T4, T5; AC3 |
| **D11** | **One new diagnostic ID reserved**: `HFC2114_DataGridHydrationEmpty` at Information severity (mirrors `HFC2106` / `HFC2107` / `HFC2111` precedent). Structured payload: `{Reason: "Empty" | "Corrupt" | "OutOfScope" | "RegistryFailure", ViewKey: string?}`. `Empty` = no blob at the enumerated key (race between `GetKeysAsync` and `GetAsync`); `Corrupt` = deserialisation / shape failure (per-key try/catch per D6 A4 — one poisoned key does NOT halt enumeration); `OutOfScope` = viewKey's BC segment isn't in the registry anymore; `RegistryFailure` (A9) = `IFrontComposerRegistry.GetManifests()` throws during hydrate — logged once per hydrate pass, OutOfScope pruning abandoned for that pass (data preserved). No separate ID for `StorageReadyAction` dispatch (one-time-per-circuit event, no operator signal value). | One diagnostic per behavioral category is the established pattern (Story 3-2 D12 / 3-4 D19 / 3-5 D18). Three reason codes on one ID keep the operator catalogue lean while preserving searchability via the `Reason` payload. Rejected: (a) three separate IDs `HFC2114` / `HFC2115` / `HFC2116` — clutter without category benefit; (b) Warning severity for `OutOfScope` — a BC was deleted between sessions, that's a normal adopter change, not an error; (c) include `HFC2116_SessionRestored` for observability — one-time-per-circuit log ≠ operator-actionable. | T3; AC5 |
| **D12** | **Hydrate on boot is always preceded by `FrontComposerTestBase.InitializeStoreAsync` in tests**. In production, the `AppInitializedAction` dispatch originates from the consuming application (Counter.Web) after `Fluxor.IStore.InitializeAsync()` completes. 3-6 does NOT add a new bootstrapping call site; it relies on the existing `AppInitializedAction` contract established by 3-1 / 3-2 / 3-3 / 3-4 / 3-5 for its hydrate-on-init path. | Centralising the hydrate trigger on `AppInitializedAction` avoids a new cross-cutting lifecycle event. The consuming application's bootstrap code (Counter.Web `Program.cs` or its Blazor layout component) already dispatches this action — reusing it keeps adopter ceremony unchanged. Rejected: (a) new `SessionRestorationRequestedAction` — unnecessary specialisation; (b) have the new effect subscribe to `Fluxor.IStore.Initialized` directly — bypasses the standard Fluxor effect pipeline + harder to test. | T3, T5; AC1, AC5, AC7 |
| **D13** | **`StorageReadyAction` semantics**: dispatched exactly once per circuit when `IUserContextAccessor` transitions from any-segment-null-or-whitespace to both-non-null-and-non-whitespace. Detection lives on `NavigationEffects.ObserveScopeFlip` — a handler registered via a `[EffectMethod]` for a lightweight "any action" sentinel isn't viable (Fluxor's effect model doesn't support wildcard subscriptions); instead, 3-6 introduces a `ScopeFlipObserverEffect` that listens for `BoundedContextChangedAction` + `SidebarToggledAction` + `NavGroupToggledAction` + `ThemeChangedAction` + `DensityChangedAction` + `PaletteOpenedAction` + `PaletteClosedAction` + `CapabilityVisitedAction` (the high-frequency user-interaction actions) and checks scope before doing anything else. On transition, dispatches `StorageReadyAction(ulid)`. The reducer flips `FrontComposerNavigationState.StorageReady` to `true` synchronously. Subsequent handler invocations skip dispatch because the flag is `true`. Sign-out (scope → empty) does NOT reset the flag — the circuit either tears down (Blazor Server) or holds per-session lifetime (WASM). | The wildcard-action limitation is real — Fluxor's `[EffectMethod]` is type-specific. Hand-rolling a wildcard via `IStore.Dispatched` observation is possible but breaks the effect DI model; an explicit list of action types covers the realistic detection-window (authenticated user MUST interact with the shell within seconds, all listed actions fire within the first interaction round). The sign-out-does-not-reset rule is a correctness property: once storage IS ready and writes have happened, a later scope-loss should fail-closed for FUTURE writes (existing L03 guard), not retract the flag. Rejected: (a) poll `IUserContextAccessor` every N ms — wrong shape; (b) subscribe to `AuthenticationStateProvider.AuthenticationStateChanged` — hard-couples shell-state to Blazor auth which 3-6 has no reason to do (the abstraction is `IUserContextAccessor`, not auth-state); (c) reset on scope-loss — invites write-burst on rapid sign-in/out toggles. | T5; AC7 |
| **D14** | **Hydrate-time out-of-scope pruning.** When `DataGridNavigationEffects.HandleAppInitialized` reads a blob whose viewKey's BC segment is not in `IFrontComposerRegistry.GetManifests()`, the effect (a) logs `HFC2114 Reason=OutOfScope ViewKey=...` at Information **once** per distinct viewKey (instance-scoped `ConcurrentDictionary<string, byte>` dedup, same pattern as Story 3-5 D7's `HFC2113` dedup), (b) calls `IStorageService.RemoveAsync(key)` to prune the stale key so it doesn't keep triggering the log on every boot, (c) skips dispatching `GridViewHydratedAction` for it. Pruning is defensive — if `RemoveAsync` throws, the catch arm in D10 swallows + logs `HFC2105`. | Leaving orphaned keys indefinitely bloats LocalStorage and recurs noise. Pruning is idempotent and costs one `RemoveAsync` per stale key (rare event — only on BC deletion between sessions). Rejected: (a) leave orphans forever — opens a subtle quota-creep failure over years; (b) surface a toast — violates silent-degrade; (c) prune only on explicit user gesture — no such gesture exists in 3-6. | T3; AC5 |
| **D15** | **NFR17 compliance is enforced by a static audit test** `NFR17ComplianceAuditTests.BlobDoesNotCarryEntityData`. The test walks every persist-call site under `src/Hexalith.FrontComposer.Shell/State/` and asserts that the type-argument to `IStorageService.SetAsync<T>` is in a whitelist: `ThemeValue`, `DensityLevel`, `NavigationPersistenceBlob`, `GridViewPersistenceBlob`, `PaletteRecentRouteBlob`, `CapabilitySeenSetBlob`, `ImmutableHashSet<string>`, `string`, `bool`. Any new type (or generic-erased `object`) fails the test and forces an explicit spec-change update. | The audit test is the cheap enforcement for "framework never proactively writes business data" — it is not a security guarantee (the storage service can be called via reflection or a renamed interface), but it is a tripwire against regressions where a new feature adds an unexpected persist type. Rejected: (a) runtime interceptor on `IStorageService` — adds ceremony to every call site; (b) `[AttributeUsage]` marker on whitelisted types — needs retroactive annotation on types outside our assembly; (c) no test at all — NFR17 becomes a hope. | T7; AC4, AC6 |
| **D16** | **Per-view debounce uses a `ConcurrentDictionary<string, CancellationTokenSource>`** keyed by viewKey inside `DataGridNavigationEffects`. On each `CaptureGridStateAction`, the effect cancels any pending CTS for that viewKey, creates a new CTS linked to the injected `TimeProvider.CreateCancellationTokenSource(TimeSpan.FromMilliseconds(250))`, stores it, then `await Task.Delay(250, ct)`. If cancelled (a newer capture arrived), return silently. If not cancelled, proceed to persist. The CTS is disposed after the delay returns or throws. `IDisposable.Dispose` on the effect cancels + disposes all outstanding CTSes. **Clear-cancels-capture coordination (A5):** `HandleClearGridState` MUST cancel any pending CTS for its viewKey BEFORE calling `IStorageService.RemoveAsync`. Without this, a capture at T=0 + clear at T=100ms produces a `RemoveAsync` at T=100ms followed by a stale `SetAsync` at T=250ms that re-persists the pre-clear snapshot — defeating the clear. Implementation: lookup + cancel + dispose + remove from the `ConcurrentDictionary` in the Clear handler, under the same `TryResolveScope` guard that wraps the RemoveAsync. Asserted by new test `ClearCancelsInFlightCapture` (Task 7 debounce-tests file). | `TimeProvider` injection keeps the debounce deterministically testable under `FakeTimeProvider` (Story 3-5 D4 precedent). Per-viewKey isolation means a scroll event on view A doesn't delay a filter change on view B. Rejected: (a) shared single CTS — A's scroll delays B's filter; (b) `System.Reactive` `Throttle` — adds Rx for one call site; (c) no debounce — write storm on rapid interactions. | T3; AC4 |
| **D17** | **Fast-path restoration in `FrontComposerShell`** does NOT block `OnAfterRenderAsync(firstRender)` on hydrate completion. The hydrate effect runs async on `AppInitializedAction` dispatch (which the consuming app fires before the shell renders). If hydrate is still in-flight when first-render lands, `LastActiveRoute` is null and restoration no-ops. The subsequent `StorageReadyAction` (D13) re-runs hydrate; when `LastActiveRoute` becomes non-null at that point, restoration does NOT re-fire (the one-shot `_sessionRestoreAttempted` flag from D3 blocks it). Users who boot with sub-second latency see restoration; users in extreme prerender-delay scenarios stay on home. Known Gap **G3** documents the "restoration miss on slow prerender" as a v1 acceptable trade-off (silent, no error, user clicks once to navigate). | Blocking the first-render on hydrate would regress NFR29 (sub-300ms first-interactive). Silent miss with a known-gap acknowledgement matches the Journey 4 contract — "session restoration lands her back on the same view" is best-effort, not a contract. Rejected: (a) block first-render on hydrate — NFR29 regression; (b) navigate on `StorageReadyAction` regardless of one-shot flag — re-navigates a user who already clicked into a different view in the restoration-miss window; (c) show a "resuming your session…" toast — violates silent-degrade UX. | T4; AC1 |
| **D18** | **JSON serialisation defaults** across all 3-6 blob types: use `JsonSerializerDefaults.Web` (camelCase property names + case-insensitive read + `JsonNumberHandling.AllowReadingFromString` — matches browser `JSON.stringify` semantics) + `DefaultIgnoreCondition.WhenWritingDefault` (omit null / default values to keep the blob compact — AC4 "compact JSON schema"). Settings are exposed via a private static `JsonSerializerOptions _options` inside each effect class to avoid re-allocating on every serialise. | Web defaults match what an adopter sees in Chrome DevTools Application → LocalStorage, maximising operator debuggability. `WhenWritingDefault` keeps the blob under the typical LRU-eviction cost threshold. Cached options avoid the known `JsonSerializerOptions` allocation anti-pattern. Rejected: (a) `JsonSerializerDefaults.General` — PascalCase keys don't match browser tooling; (b) fresh options per call — allocation churn; (c) source-generation (`JsonSerializable` attribute) — worth doing later at shell-wide scope (Story 9-x) but premature in 3-6. | T3; AC4 |
| **D19** | **Explicit `HydrationState { Idle, Hydrating, Hydrated }` enum per feature** — NOT default-value / triple-field proxies. Every feature that hydrates on boot adds a `HydrationState` field on its state record (`FrontComposerNavigationState.HydrationState`, `FrontComposerThemeState.HydrationState`, `FrontComposerDensityState.HydrationState`, `FrontComposerCommandPaletteState.HydrationState` — already exists from Story 3-4; reused). Initial value `Idle`. Hydrate effect on `AppInitializedAction` flips `Idle → Hydrating → Hydrated` via dedicated reducers. `HandleStorageReady` re-runs hydrate iff `state.Value.HydrationState == Idle` — NEVER via proxy fields like `Theme == ThemeValue.System` or `LastActiveRoute == null && SidebarCollapsed == false && CollapsedGroups.IsEmpty` (those ship silent false-negatives when a user's actual preference happens to match a default). `CapabilityDiscoveryFeature` already has this shape (Story 3-5) — 3-6 extends the pattern to Navigation / Theme / Density. | Proxy gates assume defaults act as sentinels — they do not. A user whose real preference is `ThemeValue.System` has indistinguishable state from an un-hydrated one; `HandleStorageReady` would either re-hydrate over user intent or skip hydrate when it should have run. Explicit three-state enum eliminates the ambiguity at the cost of one field + one reducer per feature. Rejected: (a) keep default-value proxies — silent data loss (Amelia blocker #3); (b) bool `IsHydrated` — can't distinguish in-flight from done-but-failed; (c) per-field timestamps — over-engineered, same info. | T1, T2, T3, T5; AC7b |
| **D20** | **`IScopeReadinessGate` is a dedicated scoped service** injected into `ScopeFlipObserverEffect` — NOT duplicated allowlist logic across 8 `[EffectMethod]` handlers. The gate exposes one method `Task EvaluateAsync(IDispatcher)` which reads `state.Value.StorageReady` + `IUserContextAccessor`, short-circuits if flag true, dispatches `StorageReadyAction(ulid)` on the first observed empty→non-empty transition, and updates an internal cached-scope field to make subsequent evaluations O(1). **Concurrency guard (A3):** Fluxor effect handlers run concurrently by default — two observed actions dispatched nearly simultaneously will both invoke `EvaluateAsync` before the reducer has flipped `state.Value.StorageReady = true`. `ScopeReadinessGate` holds a private `int _dispatched` field (initial value `0`) and wraps the dispatch in `if (Interlocked.CompareExchange(ref _dispatched, 1, 0) == 0) await dispatcher.Dispatch(new StorageReadyAction(...))`. This guarantees exactly-once dispatch under racing handlers, closing the gap the `state.Value.StorageReady` read does not (the state-value check is still performed first as a cheap short-circuit — `Interlocked` is the race tiebreaker, not the primary gate). Asserted by new test `EvaluateAsync_ConcurrentHandlersDispatchStorageReadyExactlyOnce`. The 8 handlers on `ScopeFlipObserverEffect` all reduce to a single `return _gate.EvaluateAsync(dispatcher)` line. **Rationale (Winston):** localising the allowlist + gate logic into one file means future high-frequency actions opt-in via adding one handler line, not distributing gate semantics across the state layer. Also isolates the eventual migration to `IStore.Dispatched` wildcard observation (G2 follow-up) to one implementation swap. | Duplicating gate logic in every `[EffectMethod]` handler is a latent coupling bomb — every future action-type addition has to remember the check, and they will not. Centralising into an interface also makes the 3 `ScopeFlipObserverEffectTests` dispatches compositional with a `[Theory]` over 8 action types (D-test #). Rejected: (a) inline gate logic in each handler — 8× duplication; (b) static helper — can't be DI'd, can't be mocked; (c) observe via `IStore.Dispatched` wildcard directly — orthogonal optimisation deferred (Winston concurred) because it couples to Fluxor internals; the gate extraction unblocks that migration without pre-committing to it. | T5; AC7a |
| **D21** | **Hydrate-side prune of stale `LastActiveRoute` (A1, symmetric to D14).** When `NavigationEffects.HandleAppInitialized` reads the nav blob and `blob.LastActiveRoute` is non-null but its BC segment (via `BoundedContextRouteParser.Parse`) resolves to a bounded-context NOT in `IFrontComposerRegistry.GetManifests()`, the effect dispatches `LastActiveRouteChangedAction(null)` (reducer sets `state.LastActiveRoute = null`) AND calls `PersistAsync()` to write the pruned blob back. This closes the data-loss trap where a user whose last-visited BC was renamed / deleted between sessions gets stuck on home indefinitely: without the prune, every boot re-reads the stale route, D3 precondition (c) fail-soft skips auto-nav, and nothing triggers a new persist (user never issues a `BoundedContextChangedAction` because they never leave home). With the prune, the bad route is cleared on the FIRST boot after the rename, and subsequent boots either auto-nav to a newly captured route (once the user clicks into any valid BC) or cleanly stay on home. The registry-throw guard (D6 A9) applies here too — if `GetManifests()` throws, skip the prune and preserve the route (fail-safe). | Symmetry with D14: DataGrid viewKeys get hydrate-side pruning for unregistered BCs; nav-blob `LastActiveRoute` now gets the same treatment. Without symmetry, stale routes accumulate per user-account forever. One extra persist per stale-route-hydrate is negligible cost (once per rename event). Rejected: (a) leave stale routes forever — persistent "stuck on home" UX for affected users; (b) prune only on user-initiated nav — the user never nav's because they're stuck; (c) prune on first `StorageReadyAction` re-hydrate only — works but duplicates logic across HandleAppInitialized and HandleStorageReady, better to make the hydrate path the single authority. | T3; AC1 |

---

## Architecture decision records

### ADR-048 — `LastActiveRoute` extends `NavigationPersistenceBlob` (single blob), NOT a separate `SessionRestorationBlob`

**Status:** Accepted (Story 3-6).

**Context:** Story 3-6 must persist the user's last active domain route so a returning user lands there instead of the home directory (AC1). The information is conceptually adjacent to the existing sidebar-collapsed + collapsed-groups state (Story 3-2's `NavigationPersistenceBlob`). Two candidate designs: extend the existing blob with an additional nullable field, or introduce a second blob under a separate storage key.

**Decision:** Extend the existing `NavigationPersistenceBlob` with a nullable `string? LastActiveRoute` property. Post-3-6 writes carry the field; pre-3-6 reads deserialise the field as null (System.Text.Json missing-field behaviour). The storage key stays `{tenantId}:{userId}:nav`. One persist effect, one hydrate effect, one fail-closed scope guard.

**Rejected alternatives:**

- **Second blob `SessionRestorationBlob` under key `{tenantId}:{userId}:session`.** Doubles the write path (two `SetAsync` per user interaction that crosses both features), doubles the hydrate path, doubles the fail-closed branches to maintain. The two blobs would write in lock-step with no UX separation, making the split cosmetic.
- **Promote `LastActiveRoute` into a dedicated Fluxor feature `SessionRestorationFeature`.** Creates a feature with one state field + one reducer + one persist/hydrate effect — over-engineered for an additive field. Fluxor features are right-sized around concern boundaries (Theme, Density, Nav, Palette, CapabilityDiscovery), not per-field granularity.
- **Encode `LastActiveRoute` into `CollapsedGroups` with a reserved key.** Cosmetically clever but couples two unrelated concerns into one dictionary; a collapsed-groups reducer would have to filter out the reserved key, and the wire format loses readability.

**Consequences:**

- Backward-compatible: a user upgrading from 3-5 → 3-6 has their existing nav blob read without error; `LastActiveRoute` is null on first 3-6 boot until the user triggers a `BoundedContextChangedAction`.
- Schema lock snapshot test asserts the field is present in the post-3-6 wire format (see Testing standards §Schema-lock tests — relocated from a binding decision to a testing-standard per Bob's pre-trim).
- Forward-compatible: adding further fields to `NavigationPersistenceBlob` in future stories follows the same additive-nullable pattern, already proven out.
- The blob grows from ~200 bytes to ~500 bytes typical (route strings are ~50–200 bytes each) — well under the LocalStorage per-entry cost.

**Verification:** `NavigationPersistenceBlobTests.LastActiveRoutePropertyIsAppendOnly`, `NavigationEffectsLastActiveRouteTests.{Persist,Hydrate}RoundTrip`, `NavigationPersistenceBlobSchemaLockedTests.BlobSchemaMatches`.

---

### ADR-049 — `StorageReadyAction` is dispatched off `NavigationEffects` scope-flip observation, NOT off `AuthenticationStateProvider`

**Status:** Accepted (Story 3-6).

**Context:** Blazor Auto prerender executes server-side before the interactive circuit boots. `IUserContextAccessor` on the prerender pass typically returns `TenantId = null` because the claim pipeline hasn't populated yet. Story 3-4's `CommandPaletteEffects.HandleAppInitialized` fail-closes in this case (per Story 3-4 deferred-work line 43), meaning the palette's recent-routes stay empty until the user refreshes. The same applies to the other hydrate effects. 3-6 needs a re-hydrate trigger that fires when scope becomes available post-prerender.

**Decision:** 3-6 introduces a `StorageReadyAction(CorrelationId)` dispatched exactly once per circuit by `ScopeFlipObserverEffect` (a new effect class living alongside `NavigationEffects`). The observer hooks `[EffectMethod]` handlers for high-frequency user-interaction actions (`BoundedContextChangedAction`, `SidebarToggledAction`, `NavGroupToggledAction`, `ThemeChangedAction`, `DensityChangedAction`, `PaletteOpenedAction`, `PaletteClosedAction`, `CapabilityVisitedAction`). Each handler checks scope: if `StorageReady == false` AND both tenant and user are non-null-non-whitespace, dispatch `StorageReadyAction`. Reducer flips `FrontComposerNavigationState.StorageReady` to true. All hydrate effects subscribe; each re-runs hydrate gated by its own feature's hydration-state.

**Rejected alternatives:**

- **Subscribe to `AuthenticationStateProvider.AuthenticationStateChanged`.** Hard-couples the shell-state layer to Blazor's auth provider. The contract 3-6 consumes is `IUserContextAccessor`, which is the adopter-authored abstraction (could be backed by HTTP claims, `AuthenticationStateProvider`, or a demo stub). Listening to the concrete auth provider bypasses the abstraction and forces every adopter to use `AuthenticationStateProvider` even when their auth model differs.
- **Periodic polling (`PeriodicTimer` with 1-second interval).** Wastes CPU on every circuit, and 1-second granularity is perceptible. Wrong shape for an event-driven transition.
- **Wildcard action observation via `IStore.Dispatched` event.** Bypasses the Fluxor effect DI model, making the observer hard to test (can't inject dependencies via `[EffectMethod]`). Also races with Fluxor's own middleware ordering.
- **Have `FrontComposerShell` call `Dispatch(StorageReadyAction)` directly from `OnAfterRenderAsync(firstRender)` when scope is available.** Shifts the responsibility from the state layer to the component layer — creates an inconsistency where some hydrate triggers are component-driven and others are effect-driven. Also misses the scenario where scope arrives AFTER first-render (sign-in-mid-session).
- **No re-hydrate at all (Story 3-4 floor).** Known gap that 3-4 explicitly parked for 3-6 to resolve. Letting it ride is a regression on the Journey 1 developer-aha ("returning user").

**Consequences:**

- Scope-flip detection has ~O(1) cost per dispatched action (one accessor read + one bool check). The observer's action-type list is a best-effort cover of high-frequency actions; if a scope-flip happens during a quiet window (user authenticates but doesn't interact for 30 seconds), the re-hydrate waits for the next interaction. This is acceptable — the user's first interaction is typically sub-second after auth.
- `StorageReadyAction` subscribers must gate their re-hydrate on their feature's own hydration-state (`FrontComposerCapabilityDiscoveryState.HydrationState`, `FrontComposerNavigationState.LastActiveRouteHydrationState`, etc.). Skipping the gate would double-apply a hydrate and potentially overwrite user changes made between prerender and auth.
- `FrontComposerNavigationState.StorageReady` is the only transient flag on the state; it is explicitly excluded from `NavigationPersistenceBlob` serialisation.
- **⚠️ LOAD-BEARING INVARIANT (Winston):** Sign-out mid-circuit does NOT reset the `StorageReady` flag. Once storage has been written, future writes fail-closed on the L03 scope guard, not on the flag. Do NOT "fix" this in a later refactor — resetting the flag would force a re-hydrate storm against a scope that no longer exists and break the contract documented in the cross-story contract table (`StorageReady` lifecycle row). If you find yourself reaching to reset it, revisit this ADR + the cross-story contract row first.

**Verification:** `ScopeFlipObserverEffectTests.DispatchesStorageReadyOnceWhenScopeResolves`, `ScopeFlipObserverEffectTests.NoOpWhenScopeStillEmpty`, `ScopeFlipObserverEffectTests.NoOpWhenAlreadyReady`, `CommandPaletteEffectsStorageReadyTests.ReHydratesWhenNotAlreadyHydrated`, `CapabilityDiscoveryEffectsStorageReadyTests.ReHydratesSeenSet`, `DataGridNavigationEffectsStorageReadyTests.ReHydratesAllViewStates`.

---

### ADR-050 — DataGrid persistence lives on `DataGridNavigationEffects`, NOT inside Story 4.3's renderer component

**Status:** Accepted (Story 3-6).

**Context:** Story 2-2 shipped `DataGridNavigationState` + `GridViewSnapshot` as reducer-only (D30). The renderer that captures filter / sort / scroll / expanded-row state ships in Story 4.3 (per the architecture § 198 Test Strategy note). 3-6 needs to persist the captured state without blocking on 4.3.

**Decision:** Add a new `DataGridNavigationEffects` class in `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs` that handles `AppInitializedAction` (hydrate all views), `CaptureGridStateAction` (debounced persist), `ClearGridStateAction` (remove key), and `RestoreGridStateAction` (on-demand read → `GridViewHydratedAction`). The effects consume the existing reducer-only actions; the Story 4.3 renderer dispatches them without knowing storage exists. 3-6 tests the state-layer round-trip by dispatching actions directly.

**Rejected alternatives:**

- **Embed storage reads/writes in the Story 4.3 renderer component (`ProjectionRenderer.razor.cs`).** Breaks the single-read-path invariant (state reads from storage in exactly one place — the effect); creates N persistence sites (one per renderer type) that all have to independently get fail-closed right. Also couples Story 4.3's renderer to `IStorageService` + `IUserContextAccessor` which it shouldn't need.
- **Wait until Story 4.3 to ship persistence.** Delays FR19 delivery to a later epic. Story 4.3 has enough scope (renderer + filter UI + sort UI + virtualization + expand-in-row); bundling persistence inflates its decision count.
- **Synchronous persist on every reducer mutation via Fluxor middleware.** Couples reducers to IO (reducers MUST stay pure); bypasses the fail-closed scope guard pattern.
- **Use a `BackgroundService` to periodically snapshot state → storage.** Wrong shape (event-driven on user interaction beats periodic); BlazorServer's scoped lifetime doesn't fit `BackgroundService` semantics.

**Consequences:**

- 3-6 delivers FR19 end-to-end without 4.3's renderer — state-layer tests assert persist → dispose → hydrate → equality. Story 4.3 gets the renderer and its own integration tests for dispatch-on-capture, dispatch-on-restore.
- `RestoreGridStateAction` has a reducer (Story 2-2 no-op) AND an effect (3-6 read-and-dispatch-hydrate). The dual-registration is Fluxor-native (both fire, reducer first, effect second).
- `CaptureGridStateAction` has a reducer (Story 2-2 adds to `ViewStates`) AND an effect (3-6 persists debounced). Same pattern.
- The 250 ms debounce is the effect's internal detail — the reducer sees every capture and updates in-memory state without delay. This gives fast return-from-form (in-memory is authoritative) AND gentle storage access (debounced persist).

**Verification:** `DataGridNavigationEffectsTests.{HandleAppInitialized_HydratesAllViews, HandleCapture_PersistsDebounced, HandleClear_RemovesKey, HandleRestore_DispatchesHydratedWhenFound, HandleRestore_NoOpWhenNotFound}`, `DataGridNavigationEffectsScopeTests.{Hydrate,Persist}ShortCircuitsOnEmptyScope`, `DataGridNavigationEffectsDebounceTests.CoalescesRapidCaptures`, `DataGridNavigationEffectsStorageReadyTests.ReHydratesOnStorageReady`.

---

## Tasks / Subtasks

**Test count target:** ≈ 67 new tests (all Shell-side) — expanded from 55 per advanced-elicitation pass: +1 CorruptBlob-isolation (A4), +1 RegistryFailure (A9), +3 IStorageService throw-paths (A8 — Get/Set/GetKeys), +1 HandlerDispatchedAfterDispose (A2), +1 ClearCancelsInFlightCapture (A5), +1 ConcurrentHandlersDispatchExactlyOnce (A3), +1 UnregisteredBcPrunesLastActiveRoute (D21), +3 Theme/Density/DataGrid HydrationState-flip-on-AppInitialized (A7). Still well below the L11 cheat-sheet trigger threshold (≥ 80 tests). Post-3-6 Shell.Tests expected ≈ 1049 passing (baseline 982 + ≈ 67 additions). L07 ratio: 67 / 21 decisions = 3.2 tests/decision — above the 1.6–2.3 ideal, justified by the review-driven + elicitation-driven correctness expansion (no ceremony tax; each added test maps to a distinct correctness-or-fault-tolerance claim).

### Task 0 — Prereq verification (≤ 20 min — HALT on any miss)

- [ ] **0.1** Confirm `NavigationPersistenceBlob` exists at `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationPersistenceBlob.cs` with `SidebarCollapsed` + `CollapsedGroups` fields. If absent / different → HALT.
- [ ] **0.2** Confirm `DataGridNavigationState` + `GridViewSnapshot` + `{Capture,Restore,Clear,PruneExpired}GridStateAction` exist per Story 2-2 (`src/Hexalith.FrontComposer.Contracts/Rendering/DataGridNavigationActions.cs` + `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationState.cs`). **Confirm the reducers are MERGED to main, not merely planned** — `git log --oneline -- src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/` must show the Story 2-2 commit. If branch-only or unmerged → HALT and block on Story 2-2.
- [ ] **0.3** Confirm `StorageKeys.BuildKey(tenantId, userId, feature, discriminator)` 4-arg overload exists at `src/Hexalith.FrontComposer.Shell/State/StorageKeys.cs`. If absent → HALT.
- [ ] **0.3a** (Amelia blocker #1) Confirm `IStorageService.GetKeysAsync(string prefix)` **method signature** exists on the interface declaration — `grep -n "GetKeysAsync" src/Hexalith.FrontComposer.Contracts/Storage/IStorageService.cs` must return a hit with a `string prefix` parameter. If the method is missing or takes no prefix parameter → HALT (DataGrid hydrate enumeration depends on it; scope-creep into Storage contract would be material).
- [ ] **0.4** Confirm `BoundedContextChangedAction(string? NewBoundedContext)` exists at `src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextChangedAction.cs` (dispatched by `FrontComposerShell.HandleLocationChanged`). If absent → HALT.
- [ ] **0.5** Confirm `AppInitializedAction(string CorrelationId)` exists at `src/Hexalith.FrontComposer.Shell/State/AppInitializedAction.cs`. If absent → HALT.
- [ ] **0.6** Confirm `IUserContextAccessor` + `FrontComposerTestBase` register tenant `"test-tenant"` + user `"test-user"` by default. If different → adjust test expectations.
- [ ] **0.7** Run `dotnet build --warnaserror` on main — MUST be clean before starting.
- [ ] **0.8** Reserve diagnostic ID `HFC2114` (Information) in `src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md` with the reason codes {`Empty`, `Corrupt`, `OutOfScope`}. Also add a corresponding `public const string HFC2114_DataGridHydrationEmpty = "HFC2114";` entry in `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` with XML-doc matching the HFC2107 / HFC2111 pattern.
- [ ] **0.9** (Amelia blocker #2) Confirm `Microsoft.Extensions.TimeProvider.Testing` (provides `FakeTimeProvider`) is referenced in `Directory.Packages.props`. If absent → add it at the matching `Microsoft.Extensions.*` version pin used by the rest of the repo (check `Microsoft.Extensions.Logging` for the baseline version). This is a testing-only dep; add under the existing `<ItemGroup>` with other test deps (`Microsoft.NET.Test.Sdk`, `xunit`, `NSubstitute`). Do NOT HALT if missing — add and commit as part of this story.
- [ ] **0.10** Confirm the Story 3-4 `CommandPaletteHydrationState` enum + the Story 3-5 `CapabilityDiscoveryHydrationState` enum both exist. 3-6's D19 reuses these two patterns for Theme / Density / Navigation. If either enum is absent → HALT (dependency signalling).
- [ ] **0.11** Confirm `IFrontComposerRegistry.GetManifests()` returns a materialisable sequence of `IFrontComposerManifest` with a `BoundedContext` property. 3-6's out-of-scope pruning (D14) + `FrontComposerShell.TryRestoreSessionAsync` manifest lookup depend on both. If the API shape differs → HALT.

### Task 1 — State schema extensions: `LastActiveRoute`, `StorageReady`, per-feature `HydrationState` enums (D19)

Files modified:

- `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationState.cs` — append three fields: `string? LastActiveRoute` (persisted via blob; defaulted null), `bool StorageReady` (transient; defaulted false; never persisted), `NavigationHydrationState HydrationState` (transient; defaulted `Idle`; never persisted — D19).
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationHydrationState.cs` (NEW) — `public enum NavigationHydrationState { Idle, Hydrating, Hydrated }`.
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationPersistenceBlob.cs` — append `string? LastActiveRoute` as a nullable property. XML-doc note: "Added Story 3-6. Null on pre-3-6 stored blobs; feature default (no auto-navigation) applies."
- `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationFeature.cs` — update `GetInitialState()` to seed `LastActiveRoute = null, StorageReady = false, HydrationState = NavigationHydrationState.Idle`.
- `src/Hexalith.FrontComposer.Shell/State/Theme/FrontComposerThemeState.cs` — append `ThemeHydrationState HydrationState` field (defaulted `Idle`).
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeHydrationState.cs` (NEW) — `public enum ThemeHydrationState { Idle, Hydrating, Hydrated }`.
- `src/Hexalith.FrontComposer.Shell/State/Theme/FrontComposerThemeFeature.cs` — update `GetInitialState()` to seed `HydrationState = ThemeHydrationState.Idle`.
- `src/Hexalith.FrontComposer.Shell/State/Density/FrontComposerDensityState.cs` + `DensityHydrationState.cs` (NEW) + `FrontComposerDensityFeature.cs` — mirror the Theme pattern.
- `FrontComposerCapabilityDiscoveryState.HydrationState` (already exists from Story 3-5) — **REUSED unchanged**.
- `FrontComposerCommandPaletteState.HydrationState` (already exists from Story 3-4) — **REUSED unchanged**.

Subtasks:

- [ ] **1.1** Append `LastActiveRoute` + `StorageReady` + `HydrationState` to `FrontComposerNavigationState`. Update XML-doc with ADR-048 + D19 reference. Update `GetInitialState()`.
- [ ] **1.2** Append `LastActiveRoute` to `NavigationPersistenceBlob`. Update XML-doc with ADR-048 reference. Add inline comment on `StorageReady` + `HydrationState` exclusion from wire format.
- [ ] **1.3** Add `NavigationHydrationState.cs`, `ThemeHydrationState.cs`, `DensityHydrationState.cs`, **and `DataGridNavigationHydrationState.cs`** enums (A7 — DataGrid feature also needs enum-gated re-hydrate to stay symmetric with the other three). Extend `FrontComposerThemeState` / `FrontComposerDensityState` / `DataGridNavigationState` + their feature `GetInitialState()` overrides with `HydrationState = Idle`.
- [ ] **1.4** Run `dotnet build --warnaserror` — must be clean (pure additive).

### Task 2 — `NavigationReducers` + new `LastActiveRouteChangedAction` / `LastActiveRouteHydratedAction` / `StorageReadyAction`

Files modified:

- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationActions.cs` — append three new action records:
  - `LastActiveRouteChangedAction(string CorrelationId, string Route)` — dispatched by `NavigationEffects` after `BoundedContextChangedAction` with non-null BC.
  - `LastActiveRouteHydratedAction(string? Route)` — dispatched by `NavigationEffects.HandleAppInitialized` after reading the blob (null if no blob or `LastActiveRoute` was null in the blob).
  - `StorageReadyAction(string CorrelationId)` — dispatched once per circuit by `ScopeFlipObserverEffect` when `IUserContextAccessor` transitions to authenticated scope.
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationReducers.cs` — add three new reducers:
  - `ReduceLastActiveRouteChanged` → `state with { LastActiveRoute = action.Route }`
  - `ReduceLastActiveRouteHydrated` → `state with { LastActiveRoute = action.Route }`
  - `ReduceStorageReady` → `state with { StorageReady = true }` (idempotent — always safe to set true again)

Subtasks:

- [ ] **2.1** Append the three action records to `NavigationActions.cs`. Each carries XML-doc referencing the decision (`D2`, `D3`, `D13`) and the ADR it flows from.
- [ ] **2.2** Append the three reducers to `NavigationReducers.cs` with `[ReducerMethod]` attributes. Each reducer is pure (no DI, no side effects).
- [ ] **2.3** Author `NavigationReducersLastActiveRouteTests.cs` — 3 tests: `ReduceLastActiveRouteChanged_UpdatesStateField`, `ReduceLastActiveRouteHydrated_UpdatesStateField`, `ReduceLastActiveRouteHydrated_NullRoute_SetsNull`.
- [ ] **2.4** Author `NavigationReducersStorageReadyTests.cs` — 2 tests: `ReduceStorageReady_FlipsStorageReadyToTrue`, `ReduceStorageReady_IdempotentWhenAlreadyTrue`.

### Task 3 — `NavigationEffects` extensions + new `DataGridNavigationEffects`

Files modified:

- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs` — extend with three new `[EffectMethod]` handlers:
  - `HandleBoundedContextChanged(BoundedContextChangedAction)` — if `NewBoundedContext` is non-null, read `NavigationManager.Uri`, dispatch `LastActiveRouteChangedAction(ulid, uri)`, then call the existing `PersistAsync()` (which now writes the extended blob).
  - `HandleLastActiveRouteChanged(LastActiveRouteChangedAction)` — call `PersistAsync()` (persist the updated blob).
  - `HandleStorageReady(StorageReadyAction)` — gate via the explicit `HydrationState` enum (D19): `if (state.Value.HydrationState != NavigationHydrationState.Idle) return;` then set `HydrationState = Hydrating` via `NavigationHydratingAction`, re-run the read-blob path from `HandleAppInitialized`, dispatch `NavigationHydratedAction` + `LastActiveRouteHydratedAction`, set `HydrationState = Hydrated` via `NavigationHydratedCompletedAction`. No proxy-field checks — enum eliminates the silent false-negative that defaults-match-user-prefs would cause.
  - Also update `HandleAppInitialized` to: (a) dispatch `NavigationHydratingAction` BEFORE reading the blob (flips `HydrationState: Idle → Hydrating`); (b) dispatch `LastActiveRouteHydratedAction(blob.LastActiveRoute)` after the existing `NavigationHydratedAction` dispatch; (c) if `blob.LastActiveRoute` is non-null AND its BC segment is not in `IFrontComposerRegistry.GetManifests()`, dispatch `LastActiveRouteChangedAction(null)` + call `PersistAsync()` (D21 hydrate-side prune — registry throw is wrapped per D6 A9, skip prune and preserve route on failure); (d) dispatch `NavigationHydratedCompletedAction` LAST (flips `HydrationState: Hydrating → Hydrated`). This state-transition wrapping applies on BOTH the happy path AND fail-closed path (scope-empty fail-closed still flips `HydrationState: Idle → Hydrated` via a single `NavigationHydratedCompletedAction` — so a subsequent `StorageReadyAction` re-triggers hydrate ONLY if the state is still `Idle`, matching Story 3-5's `CapabilityDiscoveryEffects.HandleAppInitialized` precedent exactly). **A7 (advanced-elicitation consistency pass):** this explicit flip on `HandleAppInitialized` is MANDATORY — without it, the `HydrationState == Idle` gate in `HandleStorageReady` would let a spurious re-observation double-apply after a successful initial hydrate. Mirror the same `Idle → Hydrating → Hydrated` progression in `ThemeEffects.HandleAppInitialized` + `DensityEffects.HandleAppInitialized` + `DataGridNavigationEffects.HandleAppInitialized` (the DataGrid one uses `DataGridNavigationHydrationState` — **add this enum in Task 1.3 alongside the other three**).

Files created:

- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs` — four `[EffectMethod]` handlers:
  - `HandleAppInitialized(AppInitializedAction, IDispatcher)` — enumerate keys via `IStorageService.GetKeysAsync($"{tenantId}:{userId}:datagrid:")`, read each, dispatch `GridViewHydratedAction` per key; prune OutOfScope keys via `RemoveAsync` + `HFC2114 Reason=OutOfScope` log.
  - `HandleCaptureGridState(CaptureGridStateAction, IDispatcher)` — debounce 250 ms per-viewKey via `ConcurrentDictionary<string, CancellationTokenSource>`, convert `state.Value.ViewStates[viewKey]` → `GridViewPersistenceBlob`, persist.
  - `HandleClearGridState(ClearGridStateAction, IDispatcher)` — `IStorageService.RemoveAsync` the scoped key.
  - `HandleRestoreGridState(RestoreGridStateAction, IDispatcher)` — read the specific key, dispatch `GridViewHydratedAction` if found.
  - `HandleStorageReady(StorageReadyAction, IDispatcher)` — re-run app-init hydrate logic.
  - Injects: `IStorageService`, `IUserContextAccessor`, `ILogger<DataGridNavigationEffects>`, `IState<DataGridNavigationState>`, `IFrontComposerRegistry` (for OutOfScope pruning), `TimeProvider`.
  - Constructor stores all; implements `IDisposable` to cancel + dispose all outstanding debounce CTSes on circuit teardown.

- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/GridViewPersistenceBlob.cs` — `record` with fields mirroring `GridViewSnapshot` but `Dictionary<string,string> Filters` (mutable). Static `FromSnapshot(GridViewSnapshot) → GridViewPersistenceBlob` + `ToSnapshot(GridViewPersistenceBlob) → GridViewSnapshot` conversion helpers.

- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/GridViewHydratedAction.cs` — new action record `public sealed record GridViewHydratedAction(string ViewKey, GridViewSnapshot Snapshot)`.

- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationReducers.cs` (modify existing) — add `ReduceGridViewHydrated`:

  ```csharp
  [ReducerMethod]
  public static DataGridNavigationState ReduceGridViewHydrated(DataGridNavigationState state, GridViewHydratedAction action) {
      ArgumentNullException.ThrowIfNull(state);
      ArgumentNullException.ThrowIfNull(action);
      return state.ViewStates.ContainsKey(action.ViewKey)
          ? state
          : state with { ViewStates = state.ViewStates.SetItem(action.ViewKey, action.Snapshot) };
  }
  ```

Subtasks:

- [ ] **3.1** Implement `GridViewPersistenceBlob` + conversion helpers. Add schema-lock tests.
- [ ] **3.2** Add `GridViewHydratedAction` record.
- [ ] **3.3** Add `ReduceGridViewHydrated` reducer. Test: adds when absent, no-op when present.
- [ ] **3.4** Implement `DataGridNavigationEffects.HandleAppInitialized` + `HandleCaptureGridState` + `HandleClearGridState` + `HandleRestoreGridState` + `HandleStorageReady`. Each wraps `TryResolveScope` fail-closed guard.
- [ ] **3.5** Implement `DataGridNavigationEffects.IDisposable` — cancels + disposes all outstanding debounce CTSes. **Disposal barrier (A2):** add a private `int _disposed` field (0/1) guarded via `Interlocked.Exchange`. `Dispose()` sets `_disposed = 1`, then cancels + disposes all CTSes, then clears the `ConcurrentDictionary`. **Every handler (`HandleAppInitialized`, `HandleCaptureGridState`, `HandleClearGridState`, `HandleRestoreGridState`, `HandleStorageReady`) starts with `if (Volatile.Read(ref _disposed) == 1) return;`**. This closes the race where a handler invocation arrives CONCURRENT with `Dispose()` and would otherwise stash a new CTS into a dictionary the Dispose just cleared (leaking the CTS and throwing `ObjectDisposedException` on later access). Asserted by new test `HandlerDispatchedAfterDisposeIsDroppedSilently` in `DataGridNavigationEffectsDebounceTests.cs`.
- [ ] **3.6** Extend `NavigationEffects` with `HandleBoundedContextChanged` + `HandleLastActiveRouteChanged` + `HandleStorageReady`. Update `HandleAppInitialized` to also dispatch `LastActiveRouteHydratedAction`.
- [ ] **3.7** Update `NavigationEffects.PersistAsync` to include `LastActiveRoute` in the persisted blob (read from `state.Value.LastActiveRoute`).

### Task 4 — `FrontComposerShell` one-shot session restoration

Files modified:

- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs`:
  - Add `[Inject] IState<FrontComposerNavigationState> NavigationState { get; set; }`.
  - Add `[Inject] IFrontComposerRegistry Registry { get; set; }`.
  - Add private field `private bool _sessionRestoreAttempted;`.
  - In `OnAfterRenderAsync(bool firstRender)`: if `firstRender && !_sessionRestoreAttempted`, set flag true, call new method `TryRestoreSessionAsync()`.
  - `TryRestoreSessionAsync()`:
    1. If `NavigationState.Value.LastActiveRoute` is null → return.
    2. If `NavigationManager.Uri` normalises to something other than home route (`/` or `/home`) → return (respect deep-link).
    3. Parse `LastActiveRoute` via `BoundedContextRouteParser.Parse(route)`; if BC is null OR `Registry.GetManifests().Any(m => m.BoundedContext == bc) == false` → return.
    4. `NavigationManager.NavigateTo(LastActiveRoute, forceLoad: false)`.

Subtasks:

- [ ] **4.1** Inject `IState<FrontComposerNavigationState>` + `IFrontComposerRegistry` into `FrontComposerShell`.
- [ ] **4.2** Add `_sessionRestoreAttempted` flag + `TryRestoreSessionAsync()` method.
- [ ] **4.3** Update `OnAfterRenderAsync` to invoke `TryRestoreSessionAsync()` on `firstRender`.
- [ ] **4.4** Author `FrontComposerShellSessionRestoreTests.cs` — 4 tests:
  - `RestoresLastRouteWhenOnHomeAndRouteResolvesToRegisteredManifest`
  - `DoesNotRestoreWhenDeepLinkedToNonHome`
  - `DoesNotRestoreWhenLastActiveRouteIsNull`
  - `DoesNotRestoreWhenRouteResolvesToUnregisteredBoundedContext` (manifest deleted between sessions)

### Task 5 — `IScopeReadinessGate` + `ScopeFlipObserverEffect` + subscriber re-hydrate (D20)

Files created:

- `src/Hexalith.FrontComposer.Shell/State/Navigation/IScopeReadinessGate.cs` (NEW, D20) — scoped service interface: `Task EvaluateAsync(IDispatcher dispatcher, CancellationToken ct)`. Exposes one method that reads `state.Value.StorageReady` + `IUserContextAccessor`, short-circuits if the flag is true, dispatches `StorageReadyAction(ulid)` on the first observed empty→non-empty transition, and caches the last-seen scope to make subsequent evaluations O(1).
- `src/Hexalith.FrontComposer.Shell/State/Navigation/ScopeReadinessGate.cs` (NEW, D20) — concrete implementation. Injects: `IState<FrontComposerNavigationState>`, `IUserContextAccessor`, `IUlidFactory`, `ILogger<ScopeReadinessGate>`.
- `src/Hexalith.FrontComposer.Shell/State/Navigation/ScopeFlipObserverEffect.cs` — new effect class with eight `[EffectMethod]` handlers (one per covered action type per D13). **Each handler is a single line**: `public Task HandleBoundedContextChanged(BoundedContextChangedAction _, IDispatcher dispatcher) => _gate.EvaluateAsync(dispatcher, CancellationToken.None);` — repeated for the 7 other action types. Injects ONLY `IScopeReadinessGate`.

Files modified:

- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs` — add `HandleStorageReady(StorageReadyAction, IDispatcher)`. Gate: `state.Value.HydrationState == CommandPaletteHydrationState.Idle` (already exists from Story 3-4) — NO `RecentRoutes.IsEmpty` proxy. Re-runs `HandleAppInitialized` hydrate path.
- `src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs` — add `HandleStorageReady`. Gate: `state.Value.HydrationState == CapabilityDiscoveryHydrationState.Idle` (already exists from Story 3-5).
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs` — add `HandleStorageReady`. Gate: `state.Value.HydrationState == ThemeHydrationState.Idle` (D19 NEW enum) — NOT `state.Value.Theme == ThemeValue.System` proxy. Re-runs hydrate.
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs` — add `HandleStorageReady`. Gate: `state.Value.HydrationState == DensityHydrationState.Idle` (D19 NEW enum).

Subtasks:

- [ ] **5.1** Define `IScopeReadinessGate` interface + `ScopeReadinessGate` concrete. Register via `TryAddScoped` in `ServiceCollectionExtensions`.
- [ ] **5.2** Implement `ScopeFlipObserverEffect` with 8 `[EffectMethod]` handlers, each delegating `_gate.EvaluateAsync`. Register via `TryAddScoped`.
- [ ] **5.3** Add `HandleStorageReady` to `CommandPaletteEffects` (gate: `HydrationState == Idle`).
- [ ] **5.4** Add `HandleStorageReady` to `CapabilityDiscoveryEffects` (gate: `HydrationState == Idle`).
- [ ] **5.5** Add `HandleStorageReady` to `ThemeEffects` + `DensityEffects` (gate: `HydrationState == Idle` via new enums from Task 1.3).
- [ ] **5.6** Add `HandleStorageReady` to `NavigationEffects` (gate via `NavigationHydrationState` enum — same as Task 3's `HandleStorageReady`).
- [ ] **5.7** Add `HandleStorageReady` to `DataGridNavigationEffects` (Task 3.4 already includes this; verify enum-gated, not list-emptiness proxy).
- [ ] **5.8** Author `ScopeReadinessGateTests.cs` — 4 tests:
  - `EvaluateAsync_DispatchesStorageReadyOnceWhenScopeResolves`
  - `EvaluateAsync_NoOpWhenScopeStillEmpty`
  - `EvaluateAsync_NoOpWhenAlreadyReady`
  - `EvaluateAsync_CachedScopeShortCircuitsRepeatedCalls`
- [ ] **5.9** Author `ScopeFlipObserverEffectTests.cs` — `[Theory]` over all 8 action types (`DispatchesStorageReadyOnEachActionType`) + 1 negative test (`NonObservedActionTypeDoesNotTriggerGate`) + 1 ordering test (`ScopeCheckPrecedesSubscriberWork` — verifies `IScopeReadinessGate.EvaluateAsync` is invoked before any reducer mutation).

### Task 6 — `ServiceCollectionExtensions` registration

File modified:

- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` — add two scoped registrations immediately after the existing `CapabilityDiscoveryEffects` registration (around line 262):
  - `services.TryAddScoped<DataGridNavigationEffects>();` (concrete for `IDisposable` cleanup; Fluxor's assembly scan auto-discovers via `[EffectMethod]`).
  - `services.TryAddScoped<ScopeFlipObserverEffect>();`

Subtasks:

- [ ] **6.1** Add both `TryAddScoped` lines with inline comments referencing Story 3-6 + the relevant decisions.
- [ ] **6.2** Update `FrontComposerTestBase` at `tests/Hexalith.FrontComposer.Shell.Tests/FrontComposerTestBase.cs` to ensure both effects are discoverable (the existing Fluxor assembly scan covers them; confirm no explicit registration needed).

### Task 7 — Tests (round-trip, scope fail-closed, schema lock, NFR17 audit)

Test files created:

- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsTests.cs` — 11 tests (A4 + A8 + A9 expansion):
  - `HandleAppInitialized_HydratesAllStoredViews`
  - `HandleAppInitialized_PrunesOutOfScopeKeys` (verifies `HFC2114 Reason=OutOfScope` + `RemoveAsync`)
  - `HandleAppInitialized_CorruptBlobDoesNotBlockLaterKeys` (A4 — seed three keys K1=valid, K2=`"not-json"`, K3=valid; assert K1 + K3 hydrate; K2 logs `HFC2114 Reason=Corrupt` exactly once)
  - `HandleAppInitialized_RegistryThrow_PreservesKeysAndLogsRegistryFailure` (A9 — mock `IFrontComposerRegistry.GetManifests()` to throw; assert NO `RemoveAsync`, exactly one `HFC2114 Reason=RegistryFailure`, all keys still readable on a subsequent call)
  - `HandleAppInitialized_GetAsyncThrows_LogsHfc2105AndContinues` (A8 — mock `IStorageService.GetAsync(k2)` to throw; assert other keys still hydrate, `HFC2105 Direction=hydrate` logged for the failing key)
  - `HandleCaptureGridState_PersistsDebouncedSnapshot` (uses `FakeTimeProvider.Advance(250ms)`)
  - `HandleCaptureGridState_SetAsyncThrows_LogsHfc2105AndSwallows` (A8 — mock `SetAsync` to throw `QuotaExceededException`; assert no unobserved exception, `HFC2105 Direction=persist` logged)
  - `HandleClearGridState_RemovesKey`
  - `HandleAppInitialized_GetKeysAsyncThrows_LogsHfc2105AndDegrades` (A8 — mock `GetKeysAsync` to throw; assert no hydrate, `HFC2105 Direction=hydrate` logged, subsequent per-view `RestoreGridStateAction` still attempts read)
  - `HandleRestoreGridState_DispatchesHydratedWhenFound`
  - `HandleRestoreGridState_NoOpWhenNotFound`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsScopeTests.cs` — 4 tests (L03 parity):
  - `Hydrate_ShortCircuitsOnNullTenant`
  - `Hydrate_ShortCircuitsOnNullUser`
  - `Persist_ShortCircuitsOnNullTenant`
  - `Persist_ShortCircuitsOnNullUser`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsDebounceTests.cs` — **6 tests (Murat + A2 + A5)**:
  - `RapidCapturesAreCoalesced` (dispatch 5 captures in 50ms; `FakeTimeProvider.Advance(260ms)`; verify 1 `SetAsync` call)
  - `DifferentViewKeysAreNotCoalesced` (dispatch for viewKey A + viewKey B rapidly; verify 2 separate `SetAsync` calls)
  - `DisposeCancelsInFlightCapture` (dispatch capture → advance 100ms → dispose effect; advance 300ms → assert zero `SetAsync` and no unobserved exception)
  - `ScopeLossCancelsInFlightCapture` (dispatch capture → advance 100ms → flip `IUserContextAccessor.TenantId` to null → advance 300ms → assert zero `SetAsync` AND exactly one `HFC2105 Direction=persist` at Information)
  - `HandlerDispatchedAfterDisposeIsDroppedSilently` (A2 — dispose the effect; then dispatch `CaptureGridStateAction`; assert zero `SetAsync`, zero new CTS allocations in the ConcurrentDictionary, zero unobserved exception)
  - `ClearCancelsInFlightCapture` (A5 — dispatch `CaptureGridStateAction(k, snap1)` at T=0; dispatch `ClearGridStateAction(k)` at T=100ms; advance to T=300ms; assert exactly one `RemoveAsync(k)` call, zero `SetAsync` calls — the pending debounce was cancelled before it could re-persist the stale snapshot)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsStorageReadyTests.cs` — 2 tests:
  - `ReHydratesOnStorageReady`
  - `NoOpWhenAlreadyHydrated`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/GridViewPersistenceBlobSchemaLockedTests.cs` — 2 tests:
  - `BlobSerializesToExpectedJsonShape`
  - `BlobRoundTripsWithImmutableDictionaryFilters`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsLastActiveRouteTests.cs` — 6 tests (D21 expansion):
  - `PersistOnBoundedContextChanged_CapturesFullRouteFromNavigationManager`
  - `PersistOnBoundedContextChanged_NullBcDoesNotPersist`
  - `HydrateOnAppInitialized_DispatchesHydratedWithStoredRoute`
  - `HydrateOnAppInitialized_EmptyBlobDispatchesNullRoute`
  - `HydrateDoesNotRePersist` (ADR-038 mirror)
  - `HydrateOnAppInitialized_UnregisteredBcPrunesLastActiveRouteAndPersists` (D21 — seed a blob with `LastActiveRoute = "/domain/deleted-bc/projection/x"` where `"deleted-bc"` is NOT in the registry; assert `LastActiveRouteChangedAction(null)` dispatched AND `SetAsync` called with the pruned blob AND `HydrateDoesNotRePersist` invariant NOT violated — the persist is a one-shot prune, not a re-persist loop)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationPersistenceBlobSchemaLockedTests.cs` — 1 test: `BlobSchemaMatches` (inline string comparison against expected post-3-6 JSON shape — includes `lastActiveRoute`).
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/ScopeReadinessGateTests.cs` — **5 tests (Task 5.8, D20 + A3)**:
  - `EvaluateAsync_DispatchesStorageReadyOnceWhenScopeResolves`
  - `EvaluateAsync_NoOpWhenScopeStillEmpty`
  - `EvaluateAsync_NoOpWhenAlreadyReady`
  - `EvaluateAsync_CachedScopeShortCircuitsRepeatedCalls` (Murat: gate-caches-last-seen-scope observable via single `IUserContextAccessor` read after first non-null resolve)
  - `EvaluateAsync_ConcurrentHandlersDispatchStorageReadyExactlyOnce` (A3 — launch 8 parallel `EvaluateAsync` calls via `Task.WhenAll` BEFORE the reducer runs; assert `IDispatcher.Dispatch<StorageReadyAction>` invoked exactly once — verifies the `Interlocked.CompareExchange` tiebreaker)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/ScopeFlipObserverEffectTests.cs` — **10 tests (Task 5.9, Murat)**:
  - `[Theory]` `DispatchesStorageReadyOnEachObservedActionType` parameterised over all 8 action types (BoundedContextChanged, SidebarToggled, NavGroupToggled, ThemeChanged, DensityChanged, PaletteOpened, PaletteClosed, CapabilityVisited) — 8 Theory data rows = 8 assertions
  - `NonObservedActionTypeDoesNotTriggerGate` (dispatch a non-listed action e.g. `LastActiveRouteHydratedAction` → gate not invoked)
  - `ScopeCheckPrecedesSubscriberWork` (ordering: gate invoked before any reducer mutation observable via action log)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsStorageReadyTests.cs` — 2 tests:
  - `ReHydratesRecentRoutesWhenPreviouslyEmpty`
  - `NoOpWhenAlreadyHydrated`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CapabilityDiscovery/CapabilityDiscoveryEffectsStorageReadyTests.cs` — 2 tests:
  - `ReHydratesSeenSetOnStorageReady`
  - `NoOpWhenHydrationStateIsSeeded`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellSessionRestoreTests.cs` — **5 tests** (Task 4.4 + Amelia first-render idempotency):
  - `RestoresLastRouteWhenOnHomeAndRouteResolvesToRegisteredManifest`
  - `DoesNotRestoreWhenDeepLinkedToNonHome`
  - `DoesNotRestoreWhenLastActiveRouteIsNull`
  - `DoesNotRestoreWhenRouteResolvesToUnregisteredBoundedContext`
  - `DoesNotNavigateOnSubsequentRenders` (NEW Amelia — `OnAfterRenderAsync(firstRender:false)` must NOT trigger `NavigationManager.NavigateTo`; `_sessionRestoreAttempted` idempotency)
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/NFR17ComplianceTripwireTests.cs` — 1 test: `BlobDoesNotCarryEntityData` (walks `src/Hexalith.FrontComposer.Shell/State/**/*.cs` via `System.IO`, regex-finds `IStorageService.SetAsync<`, asserts type-argument is in the whitelist). **Renamed from `NFR17ComplianceAuditTests` per Murat** — "Tripwire" signals this is a regression-catcher, NOT a security guarantee (regex misses extension-method call-sites + indirect generics — G8).

Subtasks:

- [ ] **7.1–7.12** — Author each test file per the list above (14 test files, ~67 tests total post advanced-elicitation pass). All tests inherit `FrontComposerTestBase` + use `FakeTimeProvider` where time matters. For `HandleAppInitialized_PrunesOutOfScopeKeys`: explicit key enumeration at test start (Murat — do NOT rely on prior test state + shared `InMemoryStorageService`); assert HFC2114 `Reason=OutOfScope` count deterministically. For A3/A8 throw-path tests: use `NSubstitute.ReturnsForAnyArgs` on the specific method, verify handler swallows without unobserved exception and without breaking subsequent handler invocations.
- [ ] **7.13** — Run `dotnet test` — expected total post-3-6: ~1049 passing / 0 failed / 2 skipped (pre-existing E2E).

### Task 8 — Counter.Web / Aspire MCP smoke

**Split per Murat:** 8.1–8.3 remain USER-GATED (visual UX verification per 3-4 / 3-5 precedent). **8.4 is AUTOMATED** (prerender-race + `StorageReadyAction`-once is where cross-circuit dedup bugs hide — per MEMORY `feedback_no_manual_validation`, use Aspire MCP + Claude browser instead of a human subtask).

USER-GATED subtasks (visual verification):

- [ ] **8.1** — With Aspire AppHost running, open Counter.Web in Claude browser, navigate to the Counter projection list, apply a filter / sort. Refresh the page. Verify the DataGrid state is restored.
- [ ] **8.2** — Verify the last-active-route restoration: navigate to the Counter projection view, close the tab, reopen at `/` → shell should auto-navigate to the Counter projection view.
- [ ] **8.3** — Verify graceful degradation: open DevTools → Application → Storage → clear LocalStorage → refresh → app loads to home directory without error.

AUTOMATED subtasks (Aspire MCP + Claude browser; no human-gate):

- [ ] **8.4** — Prerender-race automation. Via `mcp__aspire__list_apphosts` + `mcp__aspire__execute_resource_command` start the apphost; via Claude browser (`mcp__claude-in-chrome__*`) navigate through sign-out → sign-back-in; via `mcp__aspire__list_structured_logs` assert exactly one `StorageReadyAction` dispatch per circuit (filter on the action type in Fluxor diagnostics). Fail the task if count ≠ 1 or if any `HFC2105 Direction=persist` logs appear with a NON-null scope. Automation script committed under `tests/Hexalith.FrontComposer.E2E/PrerenderRace/StorageReadyOnce.mcp.md` (docstring-driven Claude script).

### Task 9 — Zero-warning gate + regression baseline

Subtasks:

- [ ] **9.1** — `dotnet build --warnaserror` clean.
- [ ] **9.2** — All existing Story 3-1 / 3-2 / 3-3 / 3-4 / 3-5 tests still pass (no regression).
- [ ] **9.3** — Update `sprint-status.yaml` to `ready-for-dev` for this story key.

---

## Known gaps (explicit, not bugs)

**G1 — Scroll position is best-effort across browsers / DPRs.** `ScrollTop` (pixel-valued) is captured verbatim but the DOM's rendered height on a new circuit may differ (DPR change, user zoom change, scrollbar styling). A user returning with a 1.25× → 1.0× zoom change will land a few rows off. Acceptable for v1 — the row-anchored restoration would need DOM-measurement at the captured row and a scroll-into-view call; deferred to Story 4.3 where the renderer is the right owner.

**G2 — `ScopeFlipObserverEffect` observes a hand-picked list of 8 action types (D13), now theory-covered via `ScopeFlipObserverEffectTests.DispatchesStorageReadyOnEachObservedActionType` + negative test `NonObservedActionTypeDoesNotTriggerGate` (Task 7, Murat). The residual gap is **inter-interaction latency**: a scope-flip during a pure-idle window (user authenticates via an external SSO popup, walks away, comes back without any UI interaction) waits for the first user click. The `IStore.Dispatched` wildcard alternative (Winston's preferred design) is deferred — `IScopeReadinessGate` extraction (D20) unblocks that migration without pre-committing. Revisit if telemetry shows operators citing "palette recent-routes empty after SSO flow" as a complaint.

**G3 — Restoration miss on slow prerender (D17).** Users with extreme prerender-to-interactive latency may see the home directory briefly before their last-active-route would have resolved. The `_sessionRestoreAttempted` flag prevents a disorienting re-navigation in this case — the user stays on home. Acceptable as a silent v1 degrade.

**G4 — Cross-tab write ordering (concurrency gap, not coverage gap — Murat).** Two tabs open to the same view; tab A captures state at T=0, tab B captures at T=1. On tab C (new circuit) boot, hydrate reads whichever tab's blob the LocalStorage drain worker persisted last. No cross-tab coordination. **Tests deliberately do NOT exercise enumeration-during-concurrent-writes** — that would be a concurrency test, not a hydration test, and requires a real browser harness (SharedWorker + BroadcastChannel). Deferred to v1.x if adopter demand emerges. `HandleAppInitialized_HydratesAllStoredViews` covers the single-tab enumeration path only.

**G5 — `HFC2114 Reason=OutOfScope` dedup is per-effect-instance (circuit-scoped).** Log noise per distinct viewKey per circuit is bounded by the catalog size. Cross-circuit duplicates are expected (a stale key lingers until pruned by D14's `RemoveAsync` on its first-seen circuit). After the first circuit prunes, subsequent circuits see no log for that key.

**G6 — No FcShellOptions flag for restoration opt-out.** Adopters who want to disable restoration entirely register `InMemoryStorageService` at `IStorageService` (see Dev Notes §Adopter opt-out). A dedicated flag is deferred to v1.x. Re-open if compliance / kiosk use cases demand a per-feature toggle.

**G7 — `RestoreGridStateAction` effect reads one key at a time.** A user who rapidly flips between views triggers multiple sequential reads. Acceptable — each read is ~1 ms on a warm LocalStorage; no batching needed for v1. Revisit if profiling shows this as a hotspot (unlikely given the existing 50-view LRU cap in memory).

**G8 — `NFR17ComplianceTripwireTests` is source-file-regex based.** The tripwire catches the common `IStorageService.SetAsync<T>(...)` shape. A call via reflection, extension method, a renamed interface, or indirect generic wrapper bypasses the tripwire. Not a security guarantee — a regression-catcher. Real NFR17 coverage lives in the schema-locked blob tests (3 total: Navigation, GridView, CapabilitySeenSet). Revisit the tripwire only if an adopter pattern legitimately needs reflection-based storage access.

**G9 — Hydrate-side BC pruning uses string-equality on the viewKey's BC segment.** Case-sensitive ordinal match against `IFrontComposerRegistry.GetManifests()[i].BoundedContext`. Adopters changing BC casing between deploys get an OutOfScope log + prune for the old-cased keys. Acceptable — BC names are framework-governed ASCII per epic 7 scope (Turkish-I concern is Epic 7).

**G10 — The restoration-URL-parsing step re-uses `BoundedContextRouteParser.Parse` (Story 3-4 D7).** Known limitations of that parser (2-segment `/domain/{bc}` returns null — noted in Story 3-4 deferred-work) mean users who last visited a bare BC landing page (if such routes ever exist) won't restore. Not a concern for v1 (FrontComposer doesn't emit bare BC landing routes yet); Epic 3 v2 revisits.

**G11 — No "anonymous" / "default" fallback segment in scoped keys (L03-aligned non-bug).** When `IUserContextAccessor.TenantId` or `UserId` is null / empty / whitespace, every persist and hydrate path in 3-6 fail-closes (logs `HFC2105 Direction=hydrate|persist` at Information, returns without IO). 3-6 does NOT synthesize a placeholder segment such as `"anonymous"`, `"default"`, `"public"`, or the empty string to let the write proceed. This is by design per `feedback_tenant_isolation_fail_closed.md` and the L03 lesson — a shared fallback bucket would cross-contaminate multi-tenant state. Verified by `DataGridNavigationEffectsScopeTests` (4) + `NavigationEffectsScopeTests` (3-2 / 3-4 precedent reused). **This is not a gap that will ever be closed**; it is an invariant.

**G12 — Tenant / user key segments are case-sensitive (A11, advanced-elicitation red-team pass).** `StorageKeys.BuildKey(tenantId, userId, …)` performs ordinal string concatenation. If an adopter's `IUserContextAccessor` returns `"Acme"` in one context and `"acme"` in another (claim-mapping inconsistency in the adopter's auth pipeline), `GetKeysAsync($"{tenantId}:…")` enumerates only the matching-case prefix — the other-case keys are invisible, producing per-session data loss (not cross-tenant leak — writes and reads consistently use whichever casing the current scope provides). The `DataGridNavigationEffectsScopeTests` L03 guards prevent writes from happening under null/empty tenant, but do not normalise case. Adopter responsibility: ensure `IUserContextAccessor` implementations return stable-cased tenant / user identifiers (lowercase ASCII recommended, matching BC-name convention in G9). Framework does NOT normalise — normalising would mask real identity mismatches.

**G13 — Sign-in mid-circuit without circuit teardown is out of scope for v1 (A6, advanced-elicitation consistency pass).** Per ADR-049's load-bearing invariant, sign-out does NOT reset `StorageReady`. Per D19, `HydrationState` reaches `Hydrated` once and stays there. In hosting modes where a circuit survives sign-out + sign-in of a DIFFERENT user (rare; most Blazor Server adopters tear down on sign-out, and WASM typically does too via `NavigationManager.NavigateTo(returnUrl, forceLoad: true)`), the hydrated state reflects the PREVIOUS user's identity — a cross-user data-visibility bug within the same circuit. Adopters running in non-tearing hosting modes should add a circuit-disposal / state-reset middleware on `IUserContextAccessor` identity change. Framework does NOT subscribe to identity-change events because the abstraction is `IUserContextAccessor`, not `AuthenticationStateProvider` (same reasoning as ADR-049 rejected alternative). Revisit in Epic 6 if adopter telemetry cites this.

**G14 — Alternative dispatch mechanism for `StorageReadyAction` not fully explored (A13, advanced-elicitation critical-perspective pass).** The chosen design (`ScopeFlipObserverEffect` with 8-action allowlist + `IScopeReadinessGate` service) was selected against the ADR-049 rejected alternatives. A THIRD alternative — a shell-provided `SessionReadyGate.razor` component that adopters drop into their layout within a `<AuthorizeView>.<Authorized>` template, which dispatches `AppInitializedAction` (or a new `SessionReadyAction`) on render — was not formally evaluated. This alternative would remove the observer pattern entirely: adopter ceremony increases (one `<SessionReadyGate />` line in their layout), but state-layer surface decreases (no `ScopeFlipObserverEffect`, no `IScopeReadinessGate`, no 8-action allowlist). Deferred to Epic 6 or a future 3-x story if `IUserContextAccessor` implementations grow in complexity and the observer-based approach becomes a maintenance burden. The D20 extraction makes this migration a localised swap.

---

## Dev Notes

### Executive summary (Feynman-level, ~30 sec)

3-6 is a **persistence story** — no new user-visible UI, no new page, no new dialog. What you are shipping is the glue that makes the existing shell remember where the user was between circuits / tabs / browser restarts.

Three seams:

1. **Nav blob gains a `LastActiveRoute` field.** Extend `NavigationPersistenceBlob`, extend `FrontComposerNavigationState`, extend `NavigationEffects.PersistAsync`, extend `NavigationEffects.HandleAppInitialized`. Auto-navigate in `FrontComposerShell.OnAfterRenderAsync(firstRender)` when on home + route resolves to a registered manifest.
2. **DataGrid persistence.** New `DataGridNavigationEffects` class; four handlers (app-init hydrate, capture persist with 250ms debounce, clear, on-demand restore). One-key-per-view with `{tenantId}:{userId}:datagrid:{viewKey}` schema. New `GridViewHydratedAction` + new reducer.
3. **`StorageReadyAction` event.** New `ScopeFlipObserverEffect` class watches eight user-interaction actions and dispatches once per circuit when scope transitions from empty to authenticated. Every hydrate effect gets a new `HandleStorageReady` handler gated on its own hydration state.

No new page, no new component, no new dialog, no new Fluent UI wiring.

### Persistence lifecycle reference

| Stage | Trigger | State change | Storage IO | Logs |
|---|---|---|---|---|
| Bootstrap | `AppInitializedAction` | None | `GetAsync({...}:nav)`, `GetKeysAsync({...}:datagrid:)`, per-key `GetAsync` | `HFC2107 Empty/Corrupt` on nav miss; `HFC2114 Empty/Corrupt/OutOfScope` per DataGrid miss |
| User opens palette | `PaletteOpenedAction` | `IsOpen = true` | — | — |
| User navigates into BC | `BoundedContextChangedAction(NewBoundedContext: "counter")` | `CurrentBoundedContext = "counter"`, `LastActiveRoute = /domain/counter/counter-list` | After 0ms: `SetAsync({...}:nav, blob)` | `HFC2105` only on fail-closed |
| User filters DataGrid | `CaptureGridStateAction(viewKey, snap)` | `ViewStates[viewKey] = snap` | After 250ms debounce: `SetAsync({...}:datagrid:{viewKey}, blob)` | `HFC2105` only on fail-closed |
| User clears DataGrid | `ClearGridStateAction(viewKey)` | `ViewStates.Remove(viewKey)` | `RemoveAsync({...}:datagrid:{viewKey})` | `HFC2105` only on fail-closed |
| User logs out (Blazor Server: circuit tears down) | — | All state disposed | `FlushAsync` via beforeunload JS interop | — |
| User returns | Bootstrap repeats | Hydrate dispatches restore state | — | — |
| Scope arrives post-prerender | `StorageReadyAction` | `StorageReady = true` | Re-read all keys (per effect, if hydration-state gate matches) | Same as Bootstrap |

### NFR17 Compliance matrix

| Field | Source | Business data? | Whitelist |
|---|---|---|---|
| `NavigationPersistenceBlob.SidebarCollapsed` | User's sidebar toggle click | No | Yes |
| `NavigationPersistenceBlob.CollapsedGroups` (Dict<string,bool>) | User's nav-group toggles; keys are BC names | No (BC names are framework-registered identifiers) | Yes |
| `NavigationPersistenceBlob.LastActiveRoute` | `NavigationManager.Uri` at dispatch time (includes query string + fragment verbatim) | **Adopter-dependent (A10, advanced-elicitation FMA pass):** route PATH is framework-generated, but QUERY-STRING / FRAGMENT passthrough means adopters whose routing convention uses sensitive query parameters (`?token=…`, `?sid=…`, `?filter=acme-corp`) will find those segments persisted. **Framework does NOT strip** — trust model mirrors browser history / bookmark behaviour. Adopter responsibility to scrub query-strings BEFORE they reach `NavigationManager` (via `NavigationInterceptor` or route-rewrite middleware) if they contain secrets. Documented in D2 NFR17-boundary note. | Yes (path trusted as framework-generated; query/fragment trusted under adopter-responsibility model) |
| `GridViewPersistenceBlob.ScrollTop` | DOM scroll position | No | Yes |
| `GridViewPersistenceBlob.SortColumn` | Column key from user sort click | No (column keys are framework-generated from projection property names) | Yes |
| `GridViewPersistenceBlob.SortDescending` | Boolean from sort direction | No | Yes |
| `GridViewPersistenceBlob.ExpandedRowId` | Row identity token (typically the aggregate ID) | **User-input adjacent** — the framework captures the identity as the user's selection, not as entity content. Aggregate IDs are ULIDs / GUIDs, not PII. | Yes (token only, never the full row data) |
| `GridViewPersistenceBlob.SelectedRowId` | Same as `ExpandedRowId` | Same | Yes |
| `GridViewPersistenceBlob.Filters` (Dict<string,string>) | **User-typed filter text** — may contain business data if a user types "ACME" as a customer filter | **Trust-model identical to browser autocomplete** — user initiated, browser-local, user can clear via DevTools | Yes (with trust-model disclosure in Dev Notes + adopter docs) |
| `GridViewPersistenceBlob.CapturedAt` | `TimeProvider.GetUtcNow()` | No | Yes |

Framework NEVER proactively reads server entity bodies for storage. The `NFR17ComplianceTripwireTests` regression-catcher enforces this at the call-site level (tripwire, not security guarantee — see G8).

### Degraded-mode behaviour matrix

| Condition | Hydrate path | Persist path | UX |
|---|---|---|---|
| Anonymous user (no tenant / no user) | All effects fail-closed (HFC2105 Information, `Direction=hydrate`) | All effects fail-closed (HFC2105 Information, `Direction=persist`) | First visit to home directory; no restoration; no error |
| `IStorageService.GetAsync` throws | `HFC2107` / `HFC2111` / `HFC2114` at Information, feature defaults apply | — | Home directory; no restoration; no error |
| `IStorageService.SetAsync` throws | — | `HFC2105` at Information, swallowed, next action retries | State persists in memory; next user action triggers another write attempt |
| `LocalStorage` quota exceeded | — | `SetAsync` throws `QuotaExceededException` → `HFC2105` | Same as above; LRU eviction on `LocalStorageService` internal drain worker eventually frees space |
| Private-browsing / IT lockdown | `GetAsync` returns null / throws | `SetAsync` throws | Home directory, within-session state works, no cross-session restoration, no error |
| Stored blob's BC deleted | `HFC2114 Reason=OutOfScope` + `RemoveAsync` prune + skip hydrate | — | Stale key pruned; user lands on home; no restoration for the deleted BC |
| Prerender scope miss → later auth | Hydrate fail-closes; `ScopeFlipObserverEffect` dispatches `StorageReadyAction` on first post-auth interaction; all hydrate effects re-run gated by their own hydration state | — | User sees home briefly; on first click, state populates; subsequent navigation uses restored state |

### Adopter opt-out

Adopters who need cold-start on every session:

```csharp
services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
services.AddHexalithFrontComposer();
```

`InMemoryStorageService` (`src/Hexalith.FrontComposer.Contracts/Storage/InMemoryStorageService.cs`) is the canonical no-op storage implementation. All persist + hydrate calls succeed but have no cross-session effect. Within-session state still works (`DataGridNavigationState.ViewStates` is the in-memory authority per Story 2-2 AC7).

### Internal surface — adopter DO-NOT-DISPATCH list (A12, advanced-elicitation red-team pass)

Fluxor has no action-ownership enforcement: any adopter code (or a rogue custom effect) CAN dispatch any action type. The following actions are 3-6 **internal surface** — adopters MUST NOT dispatch them directly. Framework behaviour is undefined if they do, and 3-6's invariants (exactly-once `StorageReadyAction`, one-shot `_sessionRestoreAttempted`, hydrate read-only per ADR-038) can be broken.

- `StorageReadyAction` — dispatched ONLY by `IScopeReadinessGate.EvaluateAsync` via `ScopeFlipObserverEffect`. Adopter-side dispatch would force a re-hydrate against potentially-stale scope and is indistinguishable from a genuine scope-flip from the subscribers' perspective. The gate's `Interlocked.CompareExchange` (D20 A3) prevents multi-dispatch within the gate, but the reducer idempotency (T2.4) and per-feature `HydrationState == Idle` guards would still accept a spurious dispatch.
- `NavigationHydratingAction` / `NavigationHydratedAction` / `NavigationHydratedCompletedAction` (and Theme / Density / DataGrid equivalents) — dispatched ONLY by the corresponding `HandleAppInitialized` / `HandleStorageReady` effect pair. Adopter-side dispatch would leave the `HydrationState` machine in an inconsistent state.
- `LastActiveRouteChangedAction` / `LastActiveRouteHydratedAction` — dispatched ONLY by `NavigationEffects`. Adopter-side dispatch would write an attacker-chosen route to the nav blob on next `PersistAsync`.
- `GridViewHydratedAction` — dispatched ONLY by `DataGridNavigationEffects`. Adopter-side dispatch could inject arbitrary snapshot data into `ViewStates`, bypassing Story 2-2's LRU cap enforcement.

Adopter-safe actions to dispatch: `AppInitializedAction` (required — adopters dispatch this after their bootstrap), `BoundedContextChangedAction` / `SidebarToggledAction` / `NavGroupToggledAction` / `ThemeChangedAction` / `DensityChangedAction` / `PaletteOpenedAction` / `PaletteClosedAction` / `CapabilityVisitedAction` (user-interaction actions; adopter renderers may dispatch for test or programmatic-nav scenarios), `CaptureGridStateAction` / `RestoreGridStateAction` / `ClearGridStateAction` (Story 2-2 renderer-level actions; Story 4.3's renderer is the production dispatcher).

### Files touched summary

**New (25):** 10 source + 15 test files.

Source:

- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/GridViewPersistenceBlob.cs`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/GridViewHydratedAction.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/ScopeFlipObserverEffect.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/IScopeReadinessGate.cs` (NEW, D20)
- `src/Hexalith.FrontComposer.Shell/State/Navigation/ScopeReadinessGate.cs` (NEW, D20)
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationHydrationState.cs` (NEW, D19)
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeHydrationState.cs` (NEW, D19)
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityHydrationState.cs` (NEW, D19)
- `tests/Hexalith.FrontComposer.E2E/PrerenderRace/StorageReadyOnce.mcp.md` (NEW, Task 8.4 automation script)

Tests:

- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsScopeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsDebounceTests.cs` (4 tests — expanded from 2)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsStorageReadyTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/GridViewPersistenceBlobSchemaLockedTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsLastActiveRouteTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationPersistenceBlobSchemaLockedTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/ScopeReadinessGateTests.cs` (NEW, Task 5.8)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/ScopeFlipObserverEffectTests.cs` (10 tests, theory-covered)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationReducersLastActiveRouteTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationReducersStorageReadyTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsStorageReadyTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CapabilityDiscovery/CapabilityDiscoveryEffectsStorageReadyTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellSessionRestoreTests.cs` (5 tests — expanded)
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/NFR17ComplianceTripwireTests.cs` (RENAMED from `NFR17ComplianceAuditTests.cs`)

**Modified (14):**

- `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationState.cs` — `+ LastActiveRoute, + StorageReady, + HydrationState` (D19)
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationPersistenceBlob.cs` — `+ LastActiveRoute`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationFeature.cs` — initial-state update (3 new fields)
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationActions.cs` — `+ LastActiveRouteChangedAction, + LastActiveRouteHydratedAction, + StorageReadyAction, + NavigationHydratingAction, + NavigationHydratedCompletedAction`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationReducers.cs` — `+ ReduceLastActiveRouteChanged, + ReduceLastActiveRouteHydrated, + ReduceStorageReady, + ReduceNavigationHydrating, + ReduceNavigationHydratedCompleted`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs` — `+ HandleBoundedContextChanged, + HandleLastActiveRouteChanged, + HandleStorageReady`; update `HandleAppInitialized` to dispatch `LastActiveRouteHydratedAction`; update `PersistAsync` to include `LastActiveRoute`; enum-gated re-hydrate
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationReducers.cs` — `+ ReduceGridViewHydrated`
- `src/Hexalith.FrontComposer.Shell/State/Theme/FrontComposerThemeState.cs` — `+ HydrationState` (D19)
- `src/Hexalith.FrontComposer.Shell/State/Theme/FrontComposerThemeFeature.cs` — initial-state update
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs` — `+ HandleStorageReady` (enum-gated); actions + reducers for HydrationState transitions
- `src/Hexalith.FrontComposer.Shell/State/Density/FrontComposerDensityState.cs` — `+ HydrationState` (D19)
- `src/Hexalith.FrontComposer.Shell/State/Density/FrontComposerDensityFeature.cs` — initial-state update
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs` — `+ HandleStorageReady` (enum-gated); actions + reducers for HydrationState transitions
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs` — `+ HandleStorageReady` (gated on existing 3-4 `HydrationState`)
- `src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs` — `+ HandleStorageReady` (gated on existing 3-5 `HydrationState`)
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` — `+ IState<FrontComposerNavigationState>, + IFrontComposerRegistry, + _sessionRestoreAttempted, + TryRestoreSessionAsync()`, update `OnAfterRenderAsync`
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` — `+ TryAddScoped<DataGridNavigationEffects>, + TryAddScoped<ScopeFlipObserverEffect>, + TryAddScoped<IScopeReadinessGate, ScopeReadinessGate>`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` — `+ HFC2114_DataGridHydrationEmpty`
- `src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md` — `+ HFC2114` row
- `Directory.Packages.props` — `+ Microsoft.Extensions.TimeProvider.Testing` (test-only; if absent per Task 0.9)

### Testing standards

- `xUnit v2` + `NSubstitute` + `bUnit` + `FakeTimeProvider` (Microsoft.Extensions.Time.Testing). No `Task.Delay(250)` in any test — use `FakeTimeProvider.Advance(TimeSpan.FromMilliseconds(260))`.
- All persistence effect tests inherit `FrontComposerTestBase`, which pre-wires `InMemoryStorageService` + stub `IUserContextAccessor` (tenant `"test-tenant"`, user `"test-user"`) + `TimeProvider.System` (substitute with `FakeTimeProvider` per test via `Services.Replace`).
- Schema-lock tests compare serialised JSON via inline string equality — update the expected string intentionally whenever a blob field is added (and update this story's wire-format documentation accordingly).
- L03 fail-closed tests MUST cover BOTH hydrate and persist paths (mirrors Story 3-5 AC11).

### Build & CI

- `dotnet build --warnaserror` clean as a gate.
- Pre-ship: `dotnet test` green (all Contracts + Shell test projects).
- USER-GATED: Counter.Web via Aspire MCP for Task 8 smoke tests (deferred per Story 3-4 / 3-5 DN3 precedent).

### Previous story intelligence

Story 3-5 (reviewed) established:

- `IStorageService` L03 fail-closed symmetry (both hydrate + persist) — **MIRROR EXACTLY for DataGrid + last-active-route**.
- `StorageKeys.BuildKey` 4-arg overload with colon-delimited 4-segment form — **REUSE** for DataGrid keys.
- `IUserContextAccessor` + `NullUserContextAccessor` default — **REUSE**. The `TryResolveScope` helper pattern in `NavigationEffects` lines 178–193 is the canonical template — copy verbatim into `DataGridNavigationEffects`.
- `FcDiagnosticIds.HFC2105_StoragePersistenceSkipped` reused with `Direction` payload — **EXTEND** with `Direction=hydrate|persist` parity.
- Scoped lifetime for effects with concrete registration for `IDisposable` cleanup — **REUSE** (`TryAddScoped<DataGridNavigationEffects>()` + `TryAddScoped<ScopeFlipObserverEffect>()`).
- `TimeProvider` injection for deterministic test timing — **REUSE** for the 250 ms debounce.
- `ImmutableDictionary` in state + `Dictionary` in blob for JSON round-trips — **REUSE** (`GridViewPersistenceBlob.Filters` as `Dictionary<string,string>`).

Story 3-4 (reviewed) established:

- Fail-closed prerender-pass skip → Story 3-6 owns the re-hydrate (deferred-work line 43) — **FULFILLED** via `StorageReadyAction` + `ScopeFlipObserverEffect`.
- `BoundedContextChangedAction` dispatched on `LocationChanged` — **REUSE** as the trigger for `LastActiveRouteChangedAction`.
- `IsInternalRoute` filter for open-redirect defence (Story 3-4 D10) — **NOT APPLIED** to `LastActiveRoute` because the route comes from `NavigationManager.Uri` (framework-authored), not from user input. If adopters ever allow user-authored routes in the shell, revisit.

Story 2-2 (done) established:

- `DataGridNavigationState` + `GridViewSnapshot` + 4 actions + LRU cap on reducer — **CONSUMED AS-IS** by 3-6's new effects.
- Per-view key convention `{boundedContext}:{projectionTypeFqn}` — **REUSE** as the `discriminator` arg to `StorageKeys.BuildKey`.

### Lessons Ledger citations

- **L01** — Cross-story contract table is explicit for every 3-6 ↔ (3-4 / 3-5 / 4-3 / 2-2) seam (see §Cross-story contract table).
- **L02** — Fluxor feature producer AND consumer both ship in 3-6: `StorageReadyAction` (producer: `ScopeFlipObserverEffect`; consumers: 6 hydrate effects). `GridViewHydratedAction` (producer: `DataGridNavigationEffects`; consumer: `DataGridNavigationReducers`). No dead reducers.
- **L03** — Fail-closed tenant/user guards on EVERY new persist AND hydrate path (DataGrid effects, last-active-route effects, scope-flip observer). HFC2105 Information with `Direction` payload.
- **L06** — 21 binding decisions ≤ 25 budget (feature story; D21 added by advanced-elicitation pre-mortem pass).
- **L07** — ~67 tests for 21 decisions = 3.2 tests/decision. Above the 1.6–2.3 ideal range, justified by (a) Murat's expanded debounce-lifecycle + theory-covered scope-flip coverage; (b) advanced-elicitation-added correctness tests (A2 dispose-race, A3 concurrent gate, A4 corrupt-blob isolation, A5 clear-cancels-capture, A8 IStorageService throw-paths, A9 registry-failure, D21 LastActiveRoute prune). Every added test maps to a distinct correctness-or-fault-tolerance claim — no ceremony tax. Post-review + post-elicitation expansion intentional.
- **L09** — ADR-048, ADR-049, ADR-050 each list ≥ 3 rejected alternatives with explicit trade-off rationale. D19 (HydrationState enums) + D20 (IScopeReadinessGate) each list 3 rejected alternatives.
- **L10** — Deferrals name specific stories (4.3, 9-x, v1.x, Epic 7).
- **L11** — Story is ~1590 lines after review-pass + advanced-elicitation amendments (~40 lines added for D21 + G12/G13/G14 + expanded test list + Internal-surface section). Over the 1500-line cheat-sheet threshold. Executive summary + critical decisions table + cross-story contracts table still serve as the fast-path. Post-review amendments (4 reviewer agents + 5 elicitation-method amendments: pre-mortem / red-team / self-consistency / FMA / critical-perspective) concentrated in Critical Decisions D2/D6/D11/D16/D20/D21, Tasks 1.3/3.5/3/7, Known gaps G12/G13/G14, Dev Notes NFR17 matrix + Internal-surface advisory. Elicitation-pass amendments tagged with `(A1)` through `(A13)` inline for traceability.

### References

- FR19 (`_bmad-output/planning-artifacts/prd/functional-requirements.md:30`)
- NFR17 (`_bmad-output/planning-artifacts/prd/non-functional-requirements.md:49`)
- NFR90 (`_bmad-output/planning-artifacts/prd/success-criteria.md:18`)
- UX-DR20 / UX-DR53 (`_bmad-output/planning-artifacts/ux-design-specification/user-journey-flows.md:215-278`)
- Epic 3 acceptance criteria (`_bmad-output/planning-artifacts/epics/epic-3-composition-shell-navigation-experience.md:255-299`)
- Story 3-4 deferred-work line 43 — prerender-only scope resolution retry → `StorageReadyAction` hook
- Story 3-5 AC11 — L03 fail-closed symmetry pattern (template for 3-6)
- Story 2-2 Decision D30 — `DataGridNavigationState` reducer-only; 3-6 lands the persistence half
- Architecture § 198 — Session persistence (`localStorage` via `IStorageService`, tenant+user scoped)
- Architecture § 508–536 — `IStorageService` contract + storage key pattern
- Architecture § 529 — Per-concern Fluxor features; DataGrid persistence listed as Yes / LRU
- `feedback_tenant_isolation_fail_closed.md` — binding L03 memo
- `_bmad-output/process-notes/story-creation-lessons.md` — L01–L11 pattern ledger

### Project Structure Notes

- All new source files live under `src/Hexalith.FrontComposer.Shell/State/` except `FrontComposerShell.razor.cs` (component edit) and `ServiceCollectionExtensions.cs` (DI wiring) and `FcDiagnosticIds.cs` (Contracts).
- Test mirror: every new `src/.../X.cs` gets a `tests/.../XTests.cs` under the same relative path. The test organisation matches architecture § 688 ("Test folder mirrors source").
- No Contracts-side changes beyond the one diagnostic ID — session persistence is a pure Shell concern per architecture § 402.

---

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List

### Change Log
