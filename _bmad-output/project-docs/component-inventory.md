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
| `FcHamburgerToggle` | `FluentLayoutHamburger` wrapper; mobile/tablet only. |
| `FcCollapsedNavRail` | 48px icon rail at compact-desktop / manual collapse. |
| `FcLayoutBreakpointWatcher` | Headless JS interop (`fc-layout-breakpoints.js`) → `ViewportChangedAction`. |
| `FcSystemThemeWatcher` | Headless (`fc-prefers-color-scheme.js`) → `SystemThemeChangedAction`. |
| `FcDensityApplier` / `FcDensityAnnouncer` | Headless density → `<body data-fc-density>` + `aria-live` announcement. |
| `FcDensityPreviewPanel` | Density specimen (grid/form/nav) with local override. |

### Navigation
| Component | Role |
|---|---|
| `FrontComposerNavigation` | `FluentNav`/`FluentNavCategory`/`FluentNavItem` tree driven by `IFrontComposerRegistry`; per-projection count + "New" badges; renders rail at compact viewport. |

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

> **Authoring conventions:** `FluxorComponent` base for state-bound components; `[EditorRequired]` on mandatory params; FluentUI **v5** API (`FluentLayoutHamburger`, `FluentNavCategory`/`FluentNavItem`, `FluentDialogBody`, `FluentProviders`, `FluentBadge`, `FluentTextInput`, `FluentDataGrid`); accessibility attributes (`aria-label`, `role`, `aria-live`, `data-testid`) on every interactive element; JS loaded lazily as ES modules from `_content/Hexalith.FrontComposer.Shell/js/`.

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

**Communication:** `ICommandService`, `ICommandServiceWithLifecycle`, `IQueryService`, `IProjectionChangeNotifier`, `IProjectionSubscription`, `IProjectionSearchProvider`; records `CommandResult`, `QueryResult`, `QueryRequest`, `ProblemDetailsPayload`, `CommandRejectionDetails`; warning kind `CommandWarningKind` including `RetryableDispatchFailed`; exceptions `CommandRejectedException`, `CommandWarningException`, `CommandValidationException`, `AuthRedirectRequiredException`.

**Lifecycle:** `CommandLifecycleState` (Idle→Submitting→Acknowledged→Syncing→Confirmed/Rejected), `CommandLifecycleTransition`, `ICommandLifecycleTracker`, `ILifecycleStateService`, `IUlidFactory`, `LifecycleOptions`, `McpLifecycleStateNames`.

**Rendering:** `Typography` (static, 9 `FcTypoToken` role constants; net10.0 only), `FcTypoToken` (readonly record struct), `TypographyStyle`, `ProjectionContext` (cascading parameter), `FrontComposerRenderContract`, `RenderSurfaceKind`/`RenderCapability`/`RenderBounds`/`DensityLevel`/`DensitySurface`, `FieldDescriptor`, `FieldSlotContext<,>`, projection slot/template/view descriptor + context + registry types, `FcRenderMode`/`CommandRenderMode`, `IRenderer`, `IDerivedValueProvider`/`DerivedValueResult`, `IUserContextAccessor`.

**Registration:** `IFrontComposerRegistry`, `IFrontComposerFullPageRouteRegistry`, `IFrontComposerCommandWriteAccessRegistry`, `IOverrideRegistry`, `DomainManifest`.

**Other contracts:** `IStorageService`/`InMemoryStorageService`, `IShortcutService`/`ShortcutBinding`/`ShortcutRegistration`, `IBadgeCountService`/`IActionQueueCountReader`/`BadgeCountChangedArgs`, `FrontComposerActivitySource`, `FcShellOptions`, `GeneratedOutputPathContract`, `ContractsMetadata` (`TypographyMappingVersion = "3.1.0"`), `FcDiagnosticIds`, dev-mode `ComponentTreeNode`/`ConventionDescriptor`/`CustomizationDiagnostic*`.

---

## D. MCP server surface (`Hexalith.FrontComposer.Mcp`)

(Tools/resources detailed in [api-contracts.md](./api-contracts.md).) Key public/registration types:

- `FrontComposerMcpDescriptorRegistry` — loads `McpManifest` from `[GeneratedManifest]` types.
- `FrontComposerMcpOptions` — endpoint pattern, API-key map, arg caps, rendering/lifecycle bounds, tenant/user claim types.
- `FrontComposerSkillResourceProvider`, `SkillCorpusLoader`, `SkillCorpusParser`, `SkillCorpusReferenceValidator`, `SkillCorpusSnippetValidator`.
- `FrontComposerMcpToolAdmissionService`, `FrontComposerMcpCommandInvoker`, `FrontComposerMcpProjectionReader`, `FrontComposerMcpLifecycleTracker`.
- `McpSchemaNegotiator`.
- **Host must implement:** `IFrontComposerMcpTenantToolGate`, `IFrontComposerMcpResourceVisibilityGate` (fail-closed); optional `IFrontComposerMcpCommandPolicyGate`.
- **DI:** `AddFrontComposerMcp(...)`, `MapFrontComposerMcp()`.

---

## E. CLI surface (`Hexalith.FrontComposer.Cli`)

(Commands detailed in [api-contracts.md](./api-contracts.md).) Public types: `CliApplication.RunAsync(args, out, err, ct)` (sole entry), `ExitCodes` (Success=0, ActionableFindings=1, InvalidArguments=2, GeneratedOutputUnavailable=3, ApplyWriteFailure=4), `OutputSanitizer`. All other types are `internal`.

---

## F. Testing library surface (`Hexalith.FrontComposer.Testing`)

For adopters writing bUnit tests against generated components:

| Type | Role |
|---|---|
| `FrontComposerTestBase` | Abstract `BunitContext` base; auto-registers host + fakes. |
| `AddFrontComposerTestHost()` (+ `FrontComposerTestHostBuilder`) | DI wiring without inheriting the base; `AddDomainAssembly<TMarker>()`, `ValidateVersionAlignment()`. |
| `FrontComposerTestOptions` | Tenant/user IDs, culture, JS-runtime mode, store-init mode, evidence caps. |
| `TestCommandService` / `TestQueryService` / `TestProjectionPageLoader` | Deterministic fakes (replay lifecycle, prime results, paging). |
| `TestFaultInjectionProvider` | Deterministic fault recorder (Drop/Delay/PartialDelivery/Reorder/ReconnectNudge). |
| `CommandDispatchEvidence` / `ProjectionPageEvidence` / `FaultInjectionEvidence` + `RedactedEvidenceFormatter` | Immutable interaction evidence + redaction. |
| `ProjectionTestDataBuilder<T>` / `CommandTestDataBuilder<T>` | Fluent test-data builders. |
| `GeneratedProjectionAssertions` / `CommandEvidenceAssertions` | DOM/lifecycle assertion helpers. |
| `FrontComposerTestUserContextAccessor` | Mutable user/tenant fake. |

> The Testing library has a committed `PublicAPI.Shipped.txt`; `PackageBoundaryTests` fails if the exported surface drifts without an intentional baseline update.
