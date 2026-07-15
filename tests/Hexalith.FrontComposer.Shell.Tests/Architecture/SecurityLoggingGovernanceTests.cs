using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Architecture;

[Trait("Category", "Governance")]
public sealed class SecurityLoggingGovernanceTests
{
    private static readonly HashSet<string> DirectLogMethodNames =
    [
        "LogTrace",
        "LogDebug",
        "LogInformation",
        "LogWarning",
        "LogError",
        "LogCritical",
    ];

    private static readonly HashSet<string> WarningAndAboveMethodNames =
    [
        "LogWarning",
        "LogError",
        "LogCritical",
    ];

    private static readonly HashSet<string> LowSeverityMethodNames =
    [
        "LogTrace",
        "LogDebug",
        "LogInformation",
    ];

    private static readonly string[] SecuritySourcePaths =
    [
        "src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.razor.cs",
        "src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs",
        "src/Hexalith.FrontComposer.Shell/Services/Auth/ClaimsPrincipalUserContextAccessor.cs",
        "src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs",
        "src/Hexalith.FrontComposer.Shell/Services/Auth/ServerCircuitUserContextAccessor.cs",
        "src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs",
        "src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandDispatchAuthorizationGate.cs",
        "src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs",
        "src/Hexalith.FrontComposer.Shell/Services/DerivedValues/LastUsedValueProvider.cs",
        "src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs",
        "src/Hexalith.FrontComposer.Shell/Services/StorageScopeResolver.cs",
    ];

    private static readonly HashSet<string> HotPathCandidateSourcePaths = new(StringComparer.Ordinal)
    {
        "src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/PendingCommands/PendingCommandPollingDriver.cs",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs",
        "src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs",
        "src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStateService.cs",
        "src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs",
        "src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationStateService.cs",
    };

    private static readonly string[] ExpectedHotPathBaselineLocations =
    [
        "src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs:OnTransitionFromService:163:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs:OnTransitionFromService:171:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs:ApplyTransition:210:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs:OnPhaseChangedFromTimer:254:LogDebug",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs:ProtocolFailure:134:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:ExecuteAsync:218:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:ExecuteAsync:227:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:DisposeAsync:188:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:DisposeAsync:227:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:OnProjectionChangedAsync:317:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:OnProjectionChangedDetailAsync:357:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:CompleteReconnectedEpochAsync:440:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:CompleteReconnectedEpochAsync:459:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:RestartClosedConnectionAsync:482:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:RestartClosedConnectionAsync:533:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:RestartClosedConnectionAsync:542:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:RestartClosedConnectionAsync:564:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:RestartClosedConnectionAsync:572:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:RestartClosedConnectionAsync:582:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:RejoinActiveGroupsAsync:607:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:RunBoundedDisposalOperationAsync:831:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:RunBoundedDisposalOperationAsync:840:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:RunBoundedDisposalOperationAsync:846:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:DisposeBoundedAsync:867:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:DisposeBoundedAsync:873:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs:PublishAsync:121:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/PendingCommands/PendingCommandPollingDriver.cs:PollOnceSafelyAsync:151:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/PendingCommands/PendingCommandPollingDriver.cs:WaitForInFlightPollAsync:170:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/PendingCommands/PendingCommandPollingDriver.cs:WaitForInFlightPollAsync:176:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs:DisposeAsync:101:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs:DisposeAsync:107:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs:RunAsync:200:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs:TriggerReconciliationOnceAsync:153:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs:TriggerReconciliationOnceAsync:179:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs:TriggerReconciliationOnceAsync:191:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs:ClassifyRefreshResult:276:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs:Subscribe:126:LogError",
        "src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs:Transition:194:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs:Transition:221:LogError",
        "src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs:Transition:234:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs:InvokeSubscribers:327:LogError",
        "src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs:RecordMessageId:351:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:TryGetAsync:118:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:TryGetAsync:134:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:TryGetAsync:147:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:SetAsync:189:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:RemoveAsync:207:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:RemoveByProjectionTypeAsync:232:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:RemoveByProjectionTypeAsync:252:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:RemoveByProjectionTypeCoreAsync:292:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:TrySeedPersistedLruAsync:345:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:TrySeedPersistedLruAsync:374:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:EvictIfOverCapAsync:406:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs:Clear:166:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs:EnforceScopeBoundary:231:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:Resolve:40:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:Resolve:49:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:Resolve:60:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:Resolve:67:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:PublishNewItemIndicatorIfEligible:119:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs:PollOnceAsync:87:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs:PollOnceAsync:92:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:Register:59:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:Register:64:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:Register:87:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:ResolveTerminal:141:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:ResolveTerminal:156:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:Clear:286:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:DrainEvictionsLocked:349:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:DispatchEvictedLifecycle:384:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:DispatchNeedsReviewLifecycle:412:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:DispatchNeedsReviewLifecycle:418:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs:EnforceScopeBoundary:466:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStateService.cs:_publisher:36:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs:ReconcileAsync:81:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs:ReconcileAsync:103:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs:ReconcileAsync:125:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs:ReconcileAsync:148:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs:Dispose:191:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs:timer:214:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationStateService.cs:_publisher:25:LogWarning",
    ];

    private static readonly string[] ExpectedResidualWarningAndAboveLocations =
    [
        "src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs:InitializeAsync:136:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs:FetchOneAsync:209:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs:UpdateCount:223:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs:OnProjectionChanged:331:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Components/Layout/FcLayoutBreakpointWatcher.razor.cs:OnViewportTierChangedAsync:51:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Components/Layout/FcLayoutBreakpointWatcher.razor.cs:OnAfterRenderAsync:94:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:BuildRenderTree:78:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:BuildRenderTree:109:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:RenderFailure:152:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionSubtitle.razor.cs:OnInitialized:107:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionSubtitle.razor.cs:Dispose:253:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs:RenderFailure:60:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs:RenderFailure:150:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapValidationGate.cs:StartAsync:48:LogError",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs:ReadProblemDetailsAsync:189:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs:ReadProblemDetailsAsync:207:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs:ReadProblemDetailsAsync:223:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs:GetAsync:111:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs:DrainLoopAsync:241:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:FrontComposerRegistry:25:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:TryGetCommandPolicy:144:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:MergeCommandPolicies:291:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/Customization/CustomizationContractValidationGate.cs:StartAsync:101:LogError",
        "src/Hexalith.FrontComposer.Shell/Services/InMemoryDiagnosticSink.cs:Publish:52:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:Register:86:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:Register:98:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:Register:142:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:Register:162:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs:Register:68:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs:Register:122:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:ProjectionViewOverrideRegistry:46:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:Register:117:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:Register:132:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:Register:188:LogWarning",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:Register:232:LogError",
        "src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs:continuation:102:LogError",
        "src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs:DispatchAsync:113:LogError",
        "src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:TryInvokeBindingAsync:225:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs:HandleCapabilityVisited:174:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs:HydrateSeenSetAsync:218:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs:SeedBadgeCountsAsync:240:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteQueryChanged:328:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteQueryChanged:361:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteQueryChanged:420:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteResultActivated:565:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteResultActivated:584:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:BuildDefaultResults:781:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:BuildDefaultResults:811:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:CanSurfaceCommandAsync:871:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs:HandleLoadPageAsync:143:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs:HandleLoadPageAsync:168:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageReducers.cs:ReduceLoadPageSucceeded:102:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs:HydrateAsync:125:LogWarning",
        "src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs:HandleThemeChanged:159:LogWarning",
    ];

    private static readonly string[] ExpectedIntentionalLowSeverityRemainderLocations =
    [
        "src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs:LogUnresolvedOnce:350:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Badges/ReflectionActionQueueProjectionCatalog.cs:SafeGetTypes:101:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Badges/ReflectionActionQueueProjectionCatalog.cs:SafeGetTypes:108:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs:HandleNavigationChangingAsync:116:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs:HandleNavigationChangingAsync:143:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor.cs:ResolveCommandType:144:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs:StartAsync:95:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs:StartAsync:118:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:MergeCommandPolicies:278:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Services/DevMode/ClipboardJSModule.cs:CopyToClipboardAsync:35:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Services/DevMode/ClipboardJSModule.cs:CopyToClipboardAsync:56:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Services/DevMode/ClipboardJSModule.cs:CopyToClipboardAsync:62:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Services/DevMode/ClipboardJSModule.cs:CopyToClipboardAsync:72:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs:EmitStarterTemplate:30:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs:EmitStarterTemplate:39:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs:EmitStarterTemplate:58:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:Register:128:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs:Register:99:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:Register:163:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:Register:177:LogInformation",
        "src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:Register:86:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs:HandleCapabilityVisited:171:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs:HydrateSeenSetAsync:212:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HydrateRecentRoutesAsync:189:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HydrateRecentRoutesAsync:208:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HydrateRecentRoutesAsync:218:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HydrateRecentRoutesAsync:233:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteResultActivated:516:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteResultActivated:544:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteResultActivated:608:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandleRecentRouteVisited:662:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandleRecentRouteVisited:673:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandleRecentRouteVisited:676:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:CanSurfaceCommandAsync:881:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HandleCaptureGridState:225:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HandleCaptureGridState:228:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HandleClearGridState:270:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HandleClearGridState:273:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HandleRestoreGridState:311:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HandleRestoreGridState:315:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HandleRestoreGridState:325:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:362:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:367:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:383:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:393:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:397:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:409:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:420:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:423:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:438:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:442:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:452:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync:465:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:ResolveRegisteredBoundedContexts:485:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs:ResolveRegisteredBoundedContexts:502:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageReducers.cs:ReduceLoadPageSucceeded:141:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs:HydrateAsync:132:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs:HydrateAsync:139:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs:HydrateAsync:144:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs:HydrateAsync:152:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs:PersistAsync:235:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs:PersistAsync:238:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs:HydrateAsync:222:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs:HydrateAsync:244:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs:HydrateAsync:249:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs:HydrateAsync:269:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs:HydrateAsync:280:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs:IsUnregisteredBoundedContext:317:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs:WriteBlobAsync:375:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs:WriteBlobAsync:378:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Navigation/ScopeReadinessGate.cs:EvaluateAsync:83:LogDebug",
        "src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs:HydrateAsync:111:LogInformation",
        "src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs:HydrateAsync:117:LogDebug",
    ];

    private static readonly HashSet<string> ExpectedHotPathMemberKeys = new(
        ExpectedHotPathBaselineLocations.Select(GetMemberKey),
        StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, int> ExpectedDirectCallCounts
        = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs"] = 5,
            ["src/Hexalith.FrontComposer.Shell/Badges/ReflectionActionQueueProjectionCatalog.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Components/Layout/FcLayoutBreakpointWatcher.razor.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs"] = 3,
            ["src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionSubtitle.razor.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapValidationGate.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs"] = 3,
            ["src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs"] = 4,
            ["src/Hexalith.FrontComposer.Shell/Services/Customization/CustomizationContractValidationGate.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Services/DevMode/ClipboardJSModule.cs"] = 4,
            ["src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs"] = 3,
            ["src/Hexalith.FrontComposer.Shell/Services/InMemoryDiagnosticSink.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs"] = 5,
            ["src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs"] = 3,
            ["src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs"] = 7,
            ["src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs"] = 5,
            ["src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs"] = 19,
            ["src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs"] = 21,
            ["src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageReducers.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs"] = 6,
            ["src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs"] = 8,
            ["src/Hexalith.FrontComposer.Shell/State/Navigation/ScopeReadinessGate.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs"] = 4,
        };

    [Fact]
    public void ShellSources_HaveExactScopedInventoryAndCollisionFreeSecurityEvents()
    {
        SourceFile[] sources = LoadSources("src/Hexalith.FrontComposer.Shell");

        sources.ShouldNotBeEmpty("the Shell logging governance scan must cover production sources");
        foreach (string path in SecuritySourcePaths)
        {
            SourceFile source = sources.Where(candidate => candidate.Path == path).ShouldHaveSingleItem();
            source.Content.ShouldNotBeNullOrWhiteSpace($"{path} must be a non-empty source location");
        }

        DirectLogSite[] sites = [.. sources.SelectMany(FindDirectLogSites)];
        sites.Length.ShouldBe(127);
        sites.Where(site => SecuritySourcePaths.Contains(site.Path, StringComparer.Ordinal)).ShouldBeEmpty(
            "11.18a security sources must use FrontComposerSecurityLog wrappers. " + FormatSites(sites));

        string[] actualInventory = [.. sites
            .GroupBy(static site => site.Path, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .Select(static group => $"{group.Key}|{group.Count()}")];
        string[] expectedInventory = [.. ExpectedDirectCallCounts
            .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
            .Select(static entry => $"{entry.Key}|{entry.Value}")];
        actualInventory.ShouldBe(expectedInventory,
            "every post-11.18c direct call must remain path-pinned by the exact remainder ledger");

        DirectLogSite[] security = [.. sites.Where(site => ClassifyOwnership(site) == LogOwnership.Security)];
        DirectLogSite[] story11_18c = [.. sites.Where(site => ClassifyOwnership(site) == LogOwnership.HotPath)];
        DirectLogSite[] story11_18b = [.. sites.Where(site => ClassifyOwnership(site) == LogOwnership.ResidualWarningAndAbove)];
        DirectLogSite[] intentionalLowSeverityRemainder = [.. sites.Where(site =>
            ClassifyOwnership(site) == LogOwnership.IntentionalLowSeverityRemainder)];
        DirectLogSite[] unowned = [.. sites.Where(site => ClassifyOwnership(site) == LogOwnership.Unowned)];
        security.ShouldBeEmpty("11.18a security sources have already been migrated");
        story11_18b.Length.ShouldBe(54, "11.18b owns only residual Warning/Error/Critical sites after semantic hot-path precedence");
        story11_18b.Select(static site => site.Location).ShouldBe(ExpectedResidualWarningAndAboveLocations,
            "every residual Warning+ call must remain pinned by exact path, member, line, and level for 11.18b");
        story11_18c.ShouldBeEmpty("11.18c must leave no direct call in a frozen semantic hot-path member");
        intentionalLowSeverityRemainder.Length.ShouldBe(73, "non-hot Trace/Debug/Information sites remain intentionally outside 11.18c");
        intentionalLowSeverityRemainder.Select(static site => site.Location).ShouldBe(ExpectedIntentionalLowSeverityRemainderLocations,
            "every non-hot low-severity call must remain pinned by exact path, member, line, and level");
        security.Length
            .ShouldBe(0);
        (security.Length + story11_18c.Length + story11_18b.Length + intentionalLowSeverityRemainder.Length + unowned.Length)
            .ShouldBe(sites.Length, "exclusive precedence must partition every direct call exactly once");
        unowned.ShouldBeEmpty("every direct call must have an explicit child-story owner. " + FormatSites(unowned));

        LoggerEvent[] events = [.. sources.SelectMany(FindLoggerEvents)];
        AssertUniqueEventIds(events);
        LoggerEvent[] existingEvents = [.. events.Where(static entry => entry.Path.EndsWith("/FrontComposerLog.cs", StringComparison.Ordinal))];
        existingEvents.Select(static entry => entry.EventId).Order().ShouldBe([
            5601,
            5602,
            5610,
            5611,
            5612,
            5613,
            5614,
            5615,
            5616,
            5620,
            5621,
            5622,
            5623,
            5630,
            5631,
            5640,
            5650,
        ]);

        LoggerEvent[] securityEvents = [.. events.Where(static entry => entry.Path.EndsWith("/FrontComposerSecurityLog.cs", StringComparison.Ordinal))];
        securityEvents.Select(static entry => entry.EventId).Order().ShouldBe(Enumerable.Range(5660, 32));
        foreach (LoggerEvent entry in securityEvents)
        {
            entry.EventName.ShouldNotBeNullOrWhiteSpace($"{entry.Location} must declare an explicit EventName");
            entry.HasExceptionParameter.ShouldBeFalse($"{entry.Location} must not capture an Exception parameter");
        }

        LoggerEvent[] hotPathEvents = [.. events.Where(static entry => entry.Path.EndsWith("/FrontComposerHotPathLog.cs", StringComparison.Ordinal))];
        hotPathEvents.Select(static entry => entry.EventId).Order().ShouldBe(Enumerable.Range(5700, 81));
        foreach (LoggerEvent entry in hotPathEvents)
        {
            entry.EventName.ShouldNotBeNullOrWhiteSpace($"{entry.Location} must declare an explicit EventName");
            entry.HasExceptionParameter.ShouldBeFalse($"{entry.Location} must not capture an Exception parameter");
        }
    }

    [Fact]
    public void GovernanceGuard_SyntheticDirectCallDuplicateIdAndExceptionParameter_AreReported()
    {
        SourceFile[] sources =
        [
            new(
                SecuritySourcePaths[0],
                "using Microsoft.Extensions.Logging; namespace Synthetic; internal sealed class Gate { "
                + "void Run(ILogger logger) => logger.LogWarning(\"unsafe\"); }"),
            new(
                "src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerSecurityLog.cs",
                "using System; using Microsoft.Extensions.Logging; namespace Synthetic; internal static partial class FrontComposerSecurityLog { "
                + "[LoggerMessage(EventId = 5660, EventName = \"First\", Level = LogLevel.Warning, Message = \"first\")] "
                + "static partial void First(ILogger logger, Exception exception); "
                + "[LoggerMessage(EventId = 5660, EventName = \"Second\", Level = LogLevel.Warning, Message = \"second\")] "
                + "static partial void Second(ILogger logger); }"),
        ];

        DirectLogSite directCall = sources.SelectMany(FindDirectLogSites).ShouldHaveSingleItem();
        directCall.Path.ShouldBe(SecuritySourcePaths[0]);

        LoggerEvent[] events = [.. sources.SelectMany(FindLoggerEvents)];
        events.GroupBy(static entry => entry.EventId).ShouldContain(group => group.Count() == 2);
        events.ShouldContain(static entry => entry.HasExceptionParameter);
    }

    [Fact]
    public void OwnershipPrecedence_SyntheticSites_AreExclusiveAndSemantic()
    {
        SourceFile[] sources =
        [
            new(
                SecuritySourcePaths[0],
                "using Microsoft.Extensions.Logging; namespace Synthetic; internal sealed class Gate { "
                + "void Run(ILogger logger) => logger.LogWarning(\"security\"); }"),
            new(
                "src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs",
                "using Microsoft.Extensions.Logging; namespace Synthetic; internal sealed class Lifecycle { "
                + "void OnTransitionFromService(ILogger logger) => logger.LogWarning(\"hot\"); }"),
            new(
                "src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs",
                "using Microsoft.Extensions.Logging; namespace Synthetic; internal sealed class BadgeCountService { "
                + "void Warn(ILogger logger) => logger.LogWarning(\"residual\"); "
                + "void Trace(ILogger logger) => logger.LogDebug(\"intentional remainder\"); }"),
        ];

        LogOwnership[] ownership = [.. sources
            .SelectMany(FindDirectLogSites)
            .Select(ClassifyOwnership)];

        ownership.ShouldBe([
            LogOwnership.Security,
            LogOwnership.HotPath,
            LogOwnership.ResidualWarningAndAbove,
            LogOwnership.IntentionalLowSeverityRemainder,
        ]);
    }

    [Fact]
    public void SemanticHotPathLedger_DirectCalls_AreFullyMigrated()
    {
        DirectLogSite[] sites = [.. LoadSources("src/Hexalith.FrontComposer.Shell")
            .SelectMany(FindDirectLogSites)
            .Where(site => ClassifyOwnership(site) == LogOwnership.HotPath)];

        ExpectedHotPathBaselineLocations.Length.ShouldBe(81);
        ExpectedHotPathBaselineLocations.Count(static location => location.EndsWith("LogWarning", StringComparison.Ordinal)
            || location.EndsWith("LogError", StringComparison.Ordinal)
            || location.EndsWith("LogCritical", StringComparison.Ordinal)).ShouldBe(63);
        ExpectedHotPathBaselineLocations.Count(static location => location.EndsWith("LogTrace", StringComparison.Ordinal)
            || location.EndsWith("LogDebug", StringComparison.Ordinal)
            || location.EndsWith("LogInformation", StringComparison.Ordinal)).ShouldBe(18);
        sites.ShouldBeEmpty("every frozen hot-path member must use FrontComposerHotPathLog. " + FormatSites(sites));
        LoadSources("src/Hexalith.FrontComposer.Shell")
            .SelectMany(FindDirectLogSites)
            .Where(site => HotPathCandidateSourcePaths.Contains(site.Path))
            .ShouldBeEmpty("candidate files must not retain folder- or file-wide direct-call exceptions");
    }

    private static IEnumerable<DirectLogSite> FindDirectLogSites(SourceFile source)
    {
        SyntaxTree tree = Parse(source);
        foreach (InvocationExpressionSyntax invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            string? methodName = invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
                _ => null,
            };
            if (methodName is null || !DirectLogMethodNames.Contains(methodName))
            {
                continue;
            }

            int line = tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;
            yield return new(source.Path, FindContainingMember(invocation), line, methodName);
        }
    }

    private static string FindContainingMember(InvocationExpressionSyntax invocation)
    {
        SyntaxNode? member = invocation.Ancestors().FirstOrDefault(static ancestor => ancestor is
            MethodDeclarationSyntax or
            ConstructorDeclarationSyntax or
            LocalFunctionStatementSyntax or
            AccessorDeclarationSyntax or
            VariableDeclaratorSyntax);
        return member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
            LocalFunctionStatementSyntax localFunction => localFunction.Identifier.ValueText,
            AccessorDeclarationSyntax accessor => accessor.Keyword.ValueText,
            VariableDeclaratorSyntax variable => variable.Identifier.ValueText,
            _ => "<unknown>",
        };
    }

    private static LogOwnership ClassifyOwnership(DirectLogSite site)
    {
        if (SecuritySourcePaths.Contains(site.Path, StringComparer.Ordinal))
        {
            return LogOwnership.Security;
        }

        if (ExpectedHotPathMemberKeys.Contains(site.MemberKey))
        {
            return LogOwnership.HotPath;
        }

        if (WarningAndAboveMethodNames.Contains(site.MethodName))
        {
            return LogOwnership.ResidualWarningAndAbove;
        }

        return LowSeverityMethodNames.Contains(site.MethodName)
            ? LogOwnership.IntentionalLowSeverityRemainder
            : LogOwnership.Unowned;
    }

    private static string GetMemberKey(string location)
    {
        int firstSeparator = location.IndexOf(':', StringComparison.Ordinal);
        int secondSeparator = location.IndexOf(':', firstSeparator + 1);
        return location[..secondSeparator];
    }

    private static IEnumerable<LoggerEvent> FindLoggerEvents(SourceFile source)
    {
        SyntaxTree tree = Parse(source);
        foreach (AttributeSyntax attribute in tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Where(IsLoggerMessageAttribute))
        {
            MethodDeclarationSyntax? method = attribute.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            int? eventId = ReadEventId(attribute);
            if (method is null || eventId is null)
            {
                continue;
            }

            string? eventName = attribute.ArgumentList?.Arguments
                .FirstOrDefault(static argument => argument.NameEquals?.Name.Identifier.ValueText == "EventName")
                ?.Expression is LiteralExpressionSyntax literal
                ? literal.Token.ValueText
                : null;
            bool hasExceptionParameter = method.ParameterList.Parameters.Any(static parameter =>
                parameter.Type?.ToString() is "Exception" or "System.Exception");
            int line = tree.GetLineSpan(attribute.Span).StartLinePosition.Line + 1;
            yield return new(source.Path, line, eventId.Value, eventName, hasExceptionParameter);
        }
    }

    private static void AssertUniqueEventIds(IEnumerable<LoggerEvent> events)
    {
        string[] duplicates = [.. events
            .GroupBy(static entry => entry.EventId)
            .Where(static group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(static entry => entry.Location))}")];
        duplicates.ShouldBeEmpty("LoggerMessage EventIds must be unique. " + string.Join("; ", duplicates));
    }

    private static int? ReadEventId(AttributeSyntax attribute)
    {
        AttributeArgumentSyntax? argument = attribute.ArgumentList?.Arguments
            .FirstOrDefault(static candidate => candidate.NameEquals?.Name.Identifier.ValueText == "EventId")
            ?? attribute.ArgumentList?.Arguments.FirstOrDefault(static candidate => candidate.NameEquals is null);
        return argument is not null && int.TryParse(argument.Expression.ToString(), out int eventId)
            ? eventId
            : null;
    }

    private static bool IsLoggerMessageAttribute(AttributeSyntax attribute)
        => attribute.Name.ToString() is "LoggerMessage" or "LoggerMessageAttribute";

    private static SyntaxTree Parse(SourceFile source)
        => CSharpSyntaxTree.ParseText(
            source.Content,
            new CSharpParseOptions(LanguageVersion.Latest),
            source.Path);

    private static SourceFile[] LoadSources(string relativeRoot)
    {
        string repositoryRoot = LocateRepositoryRoot();
        string sourceRoot = Path.Combine(repositoryRoot, relativeRoot);
        return [.. Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !IsBuildPath(path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(path => new SourceFile(
                Normalize(Path.GetRelativePath(repositoryRoot, path)),
                File.ReadAllText(path)))];
    }

    private static bool IsBuildPath(string path)
        => Normalize(path).Split('/').Any(static segment => segment is "bin" or "obj");

    private static string LocateRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FrontComposer repository root.");
    }

    private static string FormatSites(IEnumerable<DirectLogSite> sites)
        => string.Join(", ", sites.Select(static site => site.Location));

    private static string Normalize(string path) => path.Replace('\\', '/');

    private sealed record SourceFile(string Path, string Content);

    private sealed record DirectLogSite(string Path, string MemberName, int Line, string MethodName)
    {
        public string MemberKey => $"{Path}:{MemberName}";

        public string Location => $"{Path}:{MemberName}:{Line}:{MethodName}";
    }

    private enum LogOwnership
    {
        Security,
        HotPath,
        ResidualWarningAndAbove,
        IntentionalLowSeverityRemainder,
        Unowned,
    }

    private sealed record LoggerEvent(
        string Path,
        int Line,
        int EventId,
        string? EventName,
        bool HasExceptionParameter)
    {
        public string Location => $"{Path}:{Line}";
    }
}
