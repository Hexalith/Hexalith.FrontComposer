# Hexalith.FrontComposer — Component Inventory

> **Generated:** 2026-06-02 · deep scan. The public/consumable surface of each project: Blazor components, service interfaces, attributes, and key types. Source-generator contract and CLI/MCP surfaces are in [api-contracts.md](./api-contracts.md); the data model is in [data-models.md](./data-models.md).

---

## A. Blazor UI components (`Hexalith.FrontComposer.Shell`)

The shell ships a `Fc*`-prefixed component library (most inherit `Fluxor.Blazor.Web.Components.FluxorComponent`; each `.razor` declares an explicit `@namespace`; non-trivial components have a `.razor.cs` code-behind using `[Inject]`).

### Layout & frame
| Component | Role |
|---|---|
| `FrontComposerShell` | **Root shell.** `FluentLayout` with Header/Navigation/Content/Footer; mounts Fluxor `StoreInitializer`, `FluentProviders`, skip links, global keyboard shortcuts (`Ctrl+,`, `Ctrl+K`). Adopter `MainLayout` reduces to `<FrontComposerShell>@Body</FrontComposerShell>`. Slots: `HeaderStart/Center/End`, `Navigation`, `Footer`. |
| `FcPageLayout` | Opt-in page-measure wrapper (FC-LYT contract, Story 1.2). `Mode` parameter (`FcPageLayoutMode.FullWidth` default / `Constrained`) cascades through `FcPageLayoutCoordinator` to toggle `#fc-main-content[data-fc-page-layout]` + the constrained `--fc-page-max-inline-size` rule. Public enum `FcPageLayoutMode` lives in `Contracts.Rendering`. |
| `FcHamburgerToggle` | Always-visible `FluentLayoutHamburger` wrapper; at Desktop toggles the unified rail between 72px labelled and 48px icon-only modes, and at Tablet/Phone opens the drawer. |
| `FcLayoutBreakpointWatcher` | Headless JS interop (`fc-layout-breakpoints.js`) → `ViewportChangedAction`. |
| `FcSystemThemeWatcher` | Headless (`fc-prefers-color-scheme.js`) → `SystemThemeChangedAction`. |
| `FcDensityApplier` / `FcDensityAnnouncer` | Headless density → `<body data-fc-density>` + `aria-live` announcement. |
| `FcDensityPreviewPanel` | Density specimen (grid/form/nav) with local override. |

### Navigation
| Component | Role |
|---|---|
| `FrontComposerNavigation` | Unified registry-driven navigation rail; renders bounded-context `FluentButton` tiles at 72px labelled or 48px icon-only width, opens `FluentMenu` projection flyouts, and preserves count + "New" badge semantics. |

### Forms, dialogs & lifecycle
| Component | Role |
|---|---|
| `FcSettingsDialog` | `FluentDialogBody`: density radio group, embedded `FcThemeToggle`, `FcDensityPreviewPanel`, Reset + Done buttons. Opened via `IDialogService`. |
| `FcCommandPalette` | `FluentDialogBody`: search input + `FcPaletteResultList`; ARIA combobox; keyboard nav via `fc-keyboard.js`. |
| `FcDestructiveConfirmationDialog` | Cancel/confirm body for destructive commands. |
| `FcFormAbandonmentGuard` | `NavigationLock` + `FluentMessageBar` on unsaved navigation. |
| `FcLifecycleWrapper` | Wraps a command form; surfaces Submitting→Acknowledged→Syncing→Confirmed/Rejected plus idempotent and NeedsReview paths via `FluentBadge`/`FluentMessageBar`. |

### Display & status
| Component | Role |
|---|---|
| `FcHomeDirectory` / `FcHomeCard` / `FcHomeRouteView` | Home landing page (`@page "/"`, `/home`): four progressive states; urgency-sorted bounded-context cards. |
| `FcStatusIcon` / `FcStatusBadge` / `FcDesaturatedBadge` | Generated `[ProjectionBadge]` statuses render colored `FluentIcon` indicators with tooltip/focus labels; text badge variants remain for optimistic/count-like status summaries. |
| `FcProjectionConnectionStatus` | `FluentMessageBar` for SignalR reconnect/reconciliation. |
| `FcPendingCommandSummary` | Bounded `aria-live` summary of active pending commands plus rejected, confirmed, idempotent-confirmed, and NeedsReview entries. |
| `FcProjectionLoadingSkeleton` | `FluentSkeleton` in Card/Timeline/Grid layout. |

### DataGrid surface
| Component | Role |
|---|---|
| `FcColumnFilterCell` | Debounced filter input → `ColumnFilterChangedAction`. |
| `FcColumnPrioritizer` | Column visibility/priority (>15-col projections). |
| `FcExpandInRowDetail` / `FcExpandedRowHiddenBanner` | Always-rendered `role="region"` row-detail panel + live-region for filter-hidden expansions (WCAG 4.1.2). |
| `FcFilterEmptyState`, `FcFilterResetButton`, `FcFilterSummary`, `FcStatusFilterChips` | Filter UI. |
| `FcMaxItemsCapNotice`, `FcSlowQueryNotice`, `FcNewItemIndicator`, `FcProjectionGlobalSearch` | Grid status/notices. |

### Rendering scaffolds
| Component | Role |
|---|---|
| `FcAuthorizedCommandRegion` | Pending/Authorized/NotAuthorized fragments by `CommandAuthorizationDecisionKind`. |
| `FcProjectionEmptyPlaceholder`, `FcProjectionSubtitle`, `FcFieldPlaceholder` | Display scaffolds (`FcFieldPlaceholder` renders unsupported field types). |

### Dev-mode & diagnostics (DEBUG + `IsDevelopment()` only)
| Component | Role |
|---|---|
| `FcDevModeOverlay`, `FcDevModeAnnotation`, `FcDevModeToggleButton` | Debug overlay. |
| `FcCustomizationDiagnosticPanel` | Displays customization-contract mismatches. |

### Helper (C#-only) components
`FcSettingsButton`, `FcPaletteTriggerButton`, `FcSettingsDialogLauncher`, `LayoutHamburgerCoordinator`, `FcPageLayoutCoordinator` (internal child→shell cascade for `FcPageLayout`, Story 1.2), `FcFluentIcons` (inline-SVG icon factory — avoids the unavailable FluentUI v5 icons NuGet), `FcThemeToggle`.

> **Authoring conventions:** `FluxorComponent` base for state-bound components; `[EditorRequired]` on mandatory params; FluentUI **v5** API (`FluentLayoutHamburger`, `FluentButton`, `FluentMenu`, `FluentDialogBody`, `FluentProviders`, `FluentBadge`, `FluentTextInput`, `FluentDataGrid`); accessibility attributes (`aria-label`, `role`, `aria-live`, `data-testid`) on every interactive element; JS loaded lazily as ES modules from `_content/Hexalith.FrontComposer.Shell/js/`.

---

## B. Shell services, state & options

**DI extension methods** (`Extensions/ServiceCollectionExtensions.cs`):
`AddHexalithFrontComposer(...)`, `AddHexalithFrontComposerQuickstart(...)`, `AddHexalithShellLocalization(...)`, `AddHexalithDomain<TMarker>()`, `AddHexalithEventStore(Action<EventStoreOptions>?)`, `AddHexalithProjectionTemplates<TMarker>` / `(IReadOnlyList<...>)`, `AddDerivedValueProvider<T>(...)`, `AddFrontComposerDevMode(...)`.

**Fluxor state slices** (`State/`): Theme, Density, Navigation, CommandPalette, CapabilityDiscovery, DataGridNavigation, ETagCache, ExpandedRow, PendingCommands, ProjectionConnection, ReconnectionReconciliation. *(Single-writer discipline per slice; effects own persistence + JS interop.)*

**Services** (`Services/`, `Badges/`, `Infrastructure/`, `State/PendingCommands/`): `BadgeCountService` (Rx `Subject<T>`), auth + `AuthorizingCommandServiceDecorator`, authorization evaluator/gate, FC-CNC `CommandExecutionAdmissionGate`, projection slot/template/view-override registries, derived-value provider chain, lifecycle state service, pending-command state/resolver/polling coordinator/driver, `LocalStorageService`, tenant context accessor, `EventStoreCommandClient`/`EventStoreQueryClient`/`EventStorePendingCommandStatusQuery`/`ProjectionSubscriptionService`, `SignalRProjectionHubConnectionFactory`.

**Options** (`Options/`): `FcShellOptions`, `FrontComposerAuthenticationOptions`, `FrontComposerAuthorizationOptions` + validators (DataAnnotations and cross-property checks for lifecycle thresholds, command-status polling interval/duration, command dispatch retry delay, pending-command expiry, cache caps, and known policy catalog strictness).

---

## C. Contracts kernel surface (`Hexalith.FrontComposer.Contracts`)

The vocabulary shared by generator + runtime + MCP. (Attributes are catalogued in [api-contracts.md](./api-contracts.md); data records in [data-models.md](./data-models.md).)

**Communication:** `ICommandService`, `ICommandServiceWithLifecycle`, `IQueryService`, `IProjectionChangeNotifier`, `IProjectionSubscription`, `IProjectionSearchProvider`; records `CommandResult`, `QueryResult`, canonical `ProjectionQuery`, composed `QueryRequest`, `ProblemDetailsPayload`, `CommandRejectionDetails`. HFC0001/CS0618 retains the v1.12 flattened `QueryRequest` source/deconstruction surface and flat JSON shape throughout 2.x, with removal targeted for `3.0.0`.

**Lifecycle:** `CommandLifecycleState` (Idle→Submitting→Acknowledged→Syncing→Confirmed/Rejected), `CommandLifecycleTransition`, `ICommandLifecycleTracker`, `ILifecycleStateService`, `IUlidFactory`, `LifecycleOptions`, `McpLifecycleStateNames`.

**Rendering:** UI-neutral `ProjectionContext`, `FrontComposerRenderContract`, `RenderSurfaceKind`/`RenderCapability`/`RenderBounds`/`DensityLevel`/`DensitySurface`, `FieldDescriptor`, descriptor/selector/registry seams, `FcRenderMode`/`CommandRenderMode`, `IRenderer`, `IDerivedValueProvider`/`DerivedValueResult`, and `IUserContextAccessor`. Blazor/Fluent render-fragment contexts and typography live in Contracts.UI.

**Registration:** `IFrontComposerRegistry`, `IFrontComposerFullPageRouteRegistry`, `IFrontComposerCommandWriteAccessRegistry`, `IOverrideRegistry`, `DomainManifest`.

**Other contracts:** `IStorageService`, `IInlinePopover`, `GridViewSnapshot`, UI-neutral `ShortcutRegistration`, `IBadgeCountService`/`IActionQueueCountReader`/`BadgeCountChangedArgs`, `FrontComposerActivitySource`, `GeneratedOutputPathContract`, `ContractsMetadata` (`TypographyMappingVersion = "3.1.0"`), `FcDiagnosticIds`, and dev-mode contract records. Runtime implementations/options/actions are Shell-owned; `InMemoryStorageService` is Testing-owned.

---

## D. UI contracts surface (`Hexalith.FrontComposer.Contracts.UI`)

Packable net10-only assembly referencing Contracts, ASP.NET Core components, and Fluent UI v5 while preserving existing public namespaces:

- `Hexalith.FrontComposer.Contracts.Rendering`: `Typography`, `FcTypoToken`, `TypographyStyle`, `FieldSlotContext<,>`, projection template contexts/descriptors/delegates, and projection view context.
- `Hexalith.FrontComposer.Contracts.Shortcuts`: `IShortcutService` and `ShortcutBinding`.
- `PublicAPI.Shipped.txt` pins the intentional assembly-owned surface. Source compatibility requires adding the package reference; the assembly identity move has no upward type forwarder and is binary-breaking.

---

## E. MCP server surface (`Hexalith.FrontComposer.Mcp`)

(Tools/resources detailed in [api-contracts.md](./api-contracts.md).) Key public/registration types:

- `FrontComposerMcpDescriptorRegistry` — loads `McpManifest` from `[GeneratedManifest]` types.
- `FrontComposerMcpOptions` — endpoint pattern, API-key map, arg caps, rendering/lifecycle bounds, tenant/user claim types.
- `FrontComposerSkillResourceProvider`, `SkillCorpusLoader`, `SkillCorpusParser`, `SkillCorpusReferenceValidator`, `SkillCorpusSnippetValidator`.
- `FrontComposerMcpToolAdmissionService`, `FrontComposerMcpCommandInvoker`, `FrontComposerMcpProjectionReader`, `FrontComposerMcpLifecycleTracker`.
- `McpSchemaNegotiator`.
- **Host must implement:** `IFrontComposerMcpTenantToolGate`, `IFrontComposerMcpResourceVisibilityGate` (fail-closed); optional `IFrontComposerMcpCommandPolicyGate`.
- **DI:** `AddFrontComposerMcp(...)`, `MapFrontComposerMcp()`.

---

## F. CLI surface (`Hexalith.FrontComposer.Cli`)

(Commands detailed in [api-contracts.md](./api-contracts.md).) Public types: `CliApplication.RunAsync(args, out, err, ct)` (sole entry), `ExitCodes` (Success=0, ActionableFindings=1, InvalidArguments=2, GeneratedOutputUnavailable=3, ApplyWriteFailure=4), `OutputSanitizer`. All other types are `internal`.

---

## G. Testing library surface (`Hexalith.FrontComposer.Testing`)

For adopters writing bUnit tests against generated components:

| Type | Role |
|---|---|
| `FrontComposerTestBase` | Abstract `BunitContext` base; auto-registers host + fakes. |
| `AddFrontComposerTestHost()` (+ `FrontComposerTestHostBuilder`) | DI wiring without inheriting the base; `AddDomainAssembly<TMarker>()`, `ValidateVersionAlignment()`. |
| `FrontComposerTestOptions` | Tenant/user IDs, culture, JS-runtime mode, store-init mode, evidence caps. |
| `InMemoryStorageService` | Testing-owned implementation of the kernel `IStorageService` seam. |
| `TestCommandService` / `TestQueryService` / `TestProjectionPageLoader` | Deterministic fakes (replay lifecycle, prime results, paging). |
| `TestFaultInjectionProvider` | Deterministic fault recorder (Drop/Delay/PartialDelivery/Reorder/ReconnectNudge). |
| `CommandDispatchEvidence` / `ProjectionPageEvidence` / `FaultInjectionEvidence` + `RedactedEvidenceFormatter` | Immutable interaction evidence + redaction. |
| `ProjectionTestDataBuilder<T>` / `CommandTestDataBuilder<T>` | Fluent test-data builders. |
| `GeneratedProjectionAssertions` / `CommandEvidenceAssertions` | DOM/lifecycle assertion helpers. |
| `FrontComposerTestUserContextAccessor` | Mutable user/tenant fake. |

> The Testing library has a committed `PublicAPI.Shipped.txt`; `PackageBoundaryTests` fails if the exported surface drifts without an intentional baseline update.

## H. Current component documentation classifications (Story 15.2, 2026-10-21)

This additive classification preserves the accepted scan above. Adopter rows resolve to grouped FC-DOC pages; infrastructure rows identify framework composition rather than extra public customization seams. Parameter and state tables are checked against source. Classification describes supported adoption, independently of CLR visibility. Infrastructure includes generated routing, headless shell wiring and developer tooling; it is not a promise of new public extension seams.

| Component | Classification | Documentation | Owner |
|---|---|---|---|
| `FcAccountMenu` | infrastructure | `docs/reference/components/front-composer-shell.md` | FrontComposer |
| `FcAggregateDetailPage` | adopter | `docs/reference/components/page-patterns.md` | FrontComposer |
| `FcAggregateListPage` | adopter | `docs/reference/components/page-patterns.md` | FrontComposer |
| `FcAnyAuthorized` | infrastructure | `docs/reference/components/command-lifecycle.md` | FrontComposer |
| `FcAuthorizedCommandRegion` | adopter | `docs/reference/components/command-lifecycle.md` | FrontComposer |
| `FcColumnFilterCell` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcColumnPrioritizer` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcCommandBlockedOutcome` | adopter | `docs/reference/components/command-lifecycle.md` | FrontComposer |
| `FcCommandPalette` | infrastructure | `docs/reference/components/front-composer-shell.md` | FrontComposer |
| `FcContentLabel` | infrastructure | `docs/reference/components/page-patterns.md` | FrontComposer |
| `FcCustomizationFallbackBoundary` | infrastructure | `docs/reference/components/customization.md` | FrontComposer |
| `FcCustomizationDiagnosticPanel` | infrastructure | `docs/reference/components/customization.md` | FrontComposer |
| `FcDensityAnnouncer` | infrastructure | `docs/reference/components/settings.md` | FrontComposer |
| `FcDensityApplier` | infrastructure | `docs/reference/components/settings.md` | FrontComposer |
| `FcDensityPreviewPanel` | adopter | `docs/reference/components/settings.md` | FrontComposer |
| `FcDesaturatedBadge` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcDestructiveConfirmationDialog` | adopter | `docs/reference/components/command-lifecycle.md` | FrontComposer |
| `FcDevModeAnnotation` | infrastructure | `docs/reference/components/customization.md` | FrontComposer |
| `FcDevModeOverlay` | infrastructure | `docs/reference/components/customization.md` | FrontComposer |
| `FcDevModeToggleButton` | infrastructure | `docs/reference/components/customization.md` | FrontComposer |
| `FcExpandInRowDetail` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcExpandedRowHiddenBanner` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcFieldPlaceholder` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcFieldSlotHost` | infrastructure | `docs/reference/components/customization.md` | FrontComposer |
| `FcFilterEmptyState` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcFilterResetButton` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcFilterSummary` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcFormAbandonmentGuard` | adopter | `docs/reference/components/command-lifecycle.md` | FrontComposer |
| `FcFreshnessIndicator` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcHamburgerToggle` | infrastructure | `docs/reference/components/front-composer-shell.md` | FrontComposer |
| `FcHomeCard` | adopter | `docs/reference/components/page-patterns.md` | FrontComposer |
| `FcHomeDirectory` | adopter | `docs/reference/components/page-patterns.md` | FrontComposer |
| `FcHomeRouteView` | adopter | `docs/reference/components/page-patterns.md` | FrontComposer |
| `FcLayoutBreakpointWatcher` | infrastructure | `docs/reference/components/front-composer-shell.md` | FrontComposer |
| `FcLifecycleWrapper` | adopter | `docs/reference/components/command-lifecycle.md` | FrontComposer |
| `FcLiveRegion` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcMaxItemsCapNotice` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcModuleLandingPage` | adopter | `docs/reference/components/page-patterns.md` | FrontComposer |
| `FcNewItemIndicator` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcPageHeader` | adopter | `docs/reference/components/page-patterns.md` | FrontComposer |
| `FcPageLayout` | adopter | `docs/reference/components/page-patterns.md` | FrontComposer |
| `FcPageTab` | adopter | `docs/reference/components/page-tabs.md` | FrontComposer |
| `FcPageTabs` | adopter | `docs/reference/components/page-tabs.md` | FrontComposer |
| `FcPageToolbar` | adopter | `docs/reference/components/page-toolbar.md` | FrontComposer |
| `FcPaletteResultList` | infrastructure | `docs/reference/components/front-composer-shell.md` | FrontComposer |
| `FcPaletteTriggerButton` | infrastructure | `docs/reference/components/front-composer-shell.md` | FrontComposer |
| `FcPendingCommandSummary` | adopter | `docs/reference/components/command-lifecycle.md` | FrontComposer |
| `FcProjectionConnectionStatus` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcProjectionEmptyPlaceholder` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcProjectionFailure` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcProjectionGlobalSearch` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcProjectionLoadingSkeleton` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcProjectionStaleNotice` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcProjectionSubtitle` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcProjectionTemplateHost` | infrastructure | `docs/reference/components/customization.md` | FrontComposer |
| `FcProjectionViewOverrideHost` | infrastructure | `docs/reference/components/customization.md` | FrontComposer |
| `FcRouteBodyLayout` | adopter | `docs/reference/components/front-composer-shell.md` | FrontComposer |
| `FcRouteFailureHeading` | infrastructure | `docs/reference/components/front-composer-shell.md` | FrontComposer |
| `FcRouteFocus` | adopter | `docs/reference/components/front-composer-shell.md` | FrontComposer |
| `FcRouteUnavailable` | adopter | `docs/reference/components/front-composer-shell.md` | FrontComposer |
| `FcScopeBlocked` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcSettingsButton` | infrastructure | `docs/reference/components/settings.md` | FrontComposer |
| `FcSettingsDialog` | infrastructure | `docs/reference/components/settings.md` | FrontComposer |
| `FcSlowQueryNotice` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcStatusBadge` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcStatusFilterChips` | adopter | `docs/reference/components/datagrid.md` | FrontComposer |
| `FcStatusIcon` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcSurfaceStatus` | adopter | `docs/reference/components/projection-status.md` | FrontComposer |
| `FcSystemThemeWatcher` | infrastructure | `docs/reference/components/settings.md` | FrontComposer |
| `FcThemeToggle` | adopter | `docs/reference/components/settings.md` | FrontComposer |
| `FcValidationSummary` | adopter | `docs/reference/components/command-lifecycle.md` | FrontComposer |
| `FrontComposerNavigation` | adopter | `docs/reference/components/navigation.md` | FrontComposer |
| `FrontComposerShell` | adopter | `docs/reference/components/front-composer-shell.md` | FrontComposer |
