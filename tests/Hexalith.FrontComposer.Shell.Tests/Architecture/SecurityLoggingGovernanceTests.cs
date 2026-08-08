using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Architecture;

[Trait("Category", "Governance")]
public sealed class SecurityLoggingGovernanceTests
{
    private static readonly Lazy<MetadataReference[]> CompilationReferences = new(CreateCompilationReferences);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<SourceFile, SemanticContext> SemanticContexts = new();

    private static readonly HashSet<string> DirectLogMethodNames =
    [
        "Log",
        "LogTrace",
        "LogDebug",
        "LogInformation",
        "LogWarning",
        "LogError",
        "LogCritical",
    ];

    private static readonly HashSet<string> AllowedUnwrappedParameterNames = new(StringComparer.Ordinal)
    {
        "exceptionType",
        "diagnosticId",
    };

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

    private static readonly HashSet<string> ExpectedHotPathMemberKeys = new(
        ExpectedHotPathBaselineLocations.Select(GetMemberKey),
        StringComparer.Ordinal);

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
        sites.ShouldBeEmpty(
            "Story 11.21 migrated the last 73 low-severity direct calls, so no Shell production source "
            + "may call ILogger.Log* directly. " + FormatSites(sites));
        sites.Where(site => SecuritySourcePaths.Contains(site.Path, StringComparer.Ordinal)).ShouldBeEmpty(
            "11.18a security sources must use FrontComposerSecurityLog wrappers. " + FormatSites(sites));

        DirectLogSite[] security = [.. sites.Where(site => ClassifyOwnership(site) == LogOwnership.Security)];
        DirectLogSite[] story11_18c = [.. sites.Where(site => ClassifyOwnership(site) == LogOwnership.HotPath)];
        DirectLogSite[] story11_18b = [.. sites.Where(site => ClassifyOwnership(site) == LogOwnership.ResidualWarningAndAbove)];
        DirectLogSite[] intentionalLowSeverityRemainder = [.. sites.Where(site =>
            ClassifyOwnership(site) == LogOwnership.IntentionalLowSeverityRemainder)];
        DirectLogSite[] unowned = [.. sites.Where(site => ClassifyOwnership(site) == LogOwnership.Unowned)];
        security.ShouldBeEmpty("11.18a security sources have already been migrated");
        story11_18b.ShouldBeEmpty("11.18b must leave no residual direct Warning/Error/Critical calls");
        story11_18c.ShouldBeEmpty("11.18c must leave no direct call in a frozen semantic hot-path member");
        intentionalLowSeverityRemainder.ShouldBeEmpty(
            "Story 11.21 burned the 73-call low-severity remainder down to zero; every non-hot "
            + "Trace/Debug/Information site now uses FrontComposerDiagnosticLog. "
            + FormatSites(intentionalLowSeverityRemainder));
        security.Length
            .ShouldBe(0);
        (security.Length + story11_18c.Length + story11_18b.Length + intentionalLowSeverityRemainder.Length + unowned.Length)
            .ShouldBe(sites.Length, "exclusive precedence must partition every direct call exactly once");
        unowned.ShouldBeEmpty("every direct call must have an explicit child-story owner. " + FormatSites(unowned));

        string[] unwrappedArguments = [.. sources.SelectMany(FindUnwrappedIdentifierArguments)];
        unwrappedArguments.ShouldBeEmpty(
            "generated security log calls must not pass raw string parameters directly; wrap with a sanitizing helper. "
            + string.Join(", ", unwrappedArguments));

        LoggerEvent[] events = [.. sources.SelectMany(FindLoggerEvents)];
        AssertUniqueEventIds(events);
        events.Where(static entry => entry.HasPlaceholderSignatureDrift).ShouldBeEmpty(
            "LoggerMessage placeholders must exactly match the generated method signature");
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

        LoggerEvent[] warningEvents = [.. events.Where(static entry => entry.Path.EndsWith("/FrontComposerWarningLog.cs", StringComparison.Ordinal))];
        warningEvents.Select(static entry => entry.EventId).Order().ShouldBe(Enumerable.Range(5800, 54));
        warningEvents.Count(static entry => entry.Level == "Warning").ShouldBe(49);
        warningEvents.Count(static entry => entry.Level == "Error").ShouldBe(5);
        foreach (LoggerEvent entry in warningEvents)
        {
            entry.EventName.ShouldNotBeNullOrWhiteSpace($"{entry.Location} must declare an explicit EventName");
            entry.HasExceptionParameter.ShouldBeFalse($"{entry.Location} must not capture an Exception parameter");
        }

        // Story 11.21 — the migrated low-severity family occupies its own collision-free band above
        // every Story 11.18 family and above the 5900+ band owned by SourceTools-generated output.
        LoggerEvent[] diagnosticEvents = [.. events.Where(static entry =>
            entry.Path.EndsWith("/FrontComposerDiagnosticLog.cs", StringComparison.Ordinal))];
        diagnosticEvents.Select(static entry => entry.EventId).Order().ShouldBe(Enumerable.Range(6000, 73));
        diagnosticEvents.Count(static entry => entry.Level == "Information").ShouldBe(56);
        diagnosticEvents.Count(static entry => entry.Level == "Debug").ShouldBe(17);
        diagnosticEvents.Select(static entry => entry.EventName).Distinct(StringComparer.Ordinal).Count().ShouldBe(73);

        // Unlike the Story 11.18 families, this family preserves the exception attachment of the
        // direct calls it replaced: exactly the 20 migrated sites that passed an Exception keep one.
        diagnosticEvents.Count(static entry => entry.HasExceptionParameter).ShouldBe(20);
        foreach (LoggerEvent entry in diagnosticEvents)
        {
            entry.EventName.ShouldNotBeNullOrWhiteSpace($"{entry.Location} must declare an explicit EventName");
            entry.Level.ShouldBeOneOf(
                "Trace",
                "Debug",
                "Information");
        }

        int[] story11_18EventIds = [.. existingEvents
            .Concat(securityEvents)
            .Concat(hotPathEvents)
            .Concat(warningEvents)
            .Select(static entry => entry.EventId)];
        diagnosticEvents
            .Select(static entry => entry.EventId)
            .Intersect(story11_18EventIds)
            .ShouldBeEmpty("Story 11.21 must not reuse a Story 11.18 EventId");
    }

    [Fact]
    public void GovernanceGuard_SyntheticDirectCallDuplicateIdExceptionAndPlaceholderDrift_AreReported()
    {
        SourceFile[] sources =
        [
            new(
                SecuritySourcePaths[0],
                "using static Microsoft.Extensions.Logging.LoggerExtensions; using Microsoft.Extensions.Logging; "
                + "namespace Synthetic; internal sealed class Gate { "
                + "void Run(ILogger logger, Audit audit) { LogWarning(logger, \"unsafe\"); audit.Log(\"not logging\"); } "
                + "internal sealed class Audit { public void Log(string message) { } } }"),
            new(
                "src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerSecurityLog.cs",
                "using System; using Boom = System.InvalidOperationException; using Microsoft.Extensions.Logging; "
                + "namespace Synthetic; internal static partial class FrontComposerSecurityLog { "
                + "private const int SharedId = 5660; private const string SharedName = \"First\"; "
                + "[global::Microsoft.Extensions.Logging.LoggerMessage(EventId = SharedId, EventName = SharedName, Level = LogLevel.Warning, Message = \"first\")] "
                + "static partial void First(ILogger logger, Boom exception); "
                + "[LoggerMessage(EventId = SharedId, EventName = \"Second\", Level = LogLevel.Warning, Message = \"{Exception}\")] "
                + "static partial void Second(ILogger logger, FakeException exception); "
                + "[LoggerMessage(EventId = 5661, EventName = \"Drift\", Level = LogLevel.Warning, Message = \"{{Literal}} {First} {Missing}\")] "
                + "static partial void Drift(ILogger logger, string first); "
                + "[LoggerMessage(EventId = 5662, EventName = \"Escaped\", Level = LogLevel.Warning, Message = \"{{Literal}} {First}\")] "
                + "static partial void Escaped(ILogger logger, string first); private sealed class FakeException { } }"),
        ];

        DirectLogSite directCall = sources.SelectMany(FindDirectLogSites).ShouldHaveSingleItem();
        directCall.Path.ShouldBe(SecuritySourcePaths[0]);

        LoggerEvent[] events = [.. sources.SelectMany(FindLoggerEvents)];
        events.GroupBy(static entry => entry.EventId).ShouldContain(group => group.Count() == 2);
        events.ShouldContain(static entry => entry.HasExceptionParameter);
        events.ShouldContain(static entry => entry.HasPlaceholderSignatureDrift);
        events.Single(static entry => entry.EventName == "Second").HasExceptionParameter.ShouldBeFalse();
        events.Single(static entry => entry.EventName == "Escaped").HasPlaceholderSignatureDrift.ShouldBeFalse();
    }

    [Fact]
    public void GovernanceGuard_SyntheticUnwrappedIdentifierArgument_IsReported()
    {
        SourceFile[] sources =
        [
            new(
                "src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerSecurityLog.cs",
                "using Microsoft.Extensions.Logging; namespace Synthetic; internal static partial class FrontComposerSecurityLog { "
                + "public static void Unsafe(ILogger logger, string tenantId) { LogUnsafe(logger, tenantId); } "
                + "[LoggerMessage(EventId = 9999, EventName = \"Unsafe\", Level = LogLevel.Warning, Message = \"{TenantId}\")] "
                + "private static partial void LogUnsafe(ILogger logger, string tenantId); }"),
        ];

        string[] unwrappedArguments = [.. sources.SelectMany(FindUnwrappedIdentifierArguments)];
        unwrappedArguments.ShouldContain(
            "src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerSecurityLog.cs:Unsafe:tenantId");
    }

    [Fact]
    public void GovernanceGuard_EmptySourceCensusAndOverBroadSecurityAllowlist_AreReported()
    {
        Should.Throw<ShouldAssertException>(() =>
            Array.Empty<SourceFile>().ShouldNotBeEmpty("the Shell logging governance scan must cover production sources"));

        SourceFile[] overBroadSources =
        [
            new(
                SecuritySourcePaths[0],
                "using Microsoft.Extensions.Logging; namespace Synthetic; internal sealed class Gate { "
                + "void Run(ILogger logger) => logger.LogWarning(\"still raw\"); }"),
        ];
        DirectLogSite[] overBroadSites = [.. overBroadSources.SelectMany(FindDirectLogSites)];
        Should.Throw<ShouldAssertException>(() =>
            overBroadSites.Where(site => SecuritySourcePaths.Contains(site.Path, StringComparer.Ordinal)).ShouldBeEmpty(
                "11.18a security sources must use FrontComposerSecurityLog wrappers."));
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

    [Fact]
    public void ResidualWarningAndAboveLedger_DirectCalls_AreFullyMigrated()
    {
        SourceFile[] sources = LoadSources("src/Hexalith.FrontComposer.Shell");
        DirectLogSite[] sites = [.. sources
            .SelectMany(FindDirectLogSites)
            .Where(site => ClassifyOwnership(site) == LogOwnership.ResidualWarningAndAbove)];
        GeneratedLogCallSite[] generatedSites = [.. sources.SelectMany(FindWarningWrapperCallSites)];
        LoggerEvent[] warningEvents = [.. sources
            .SelectMany(FindLoggerEvents)
            .Where(static entry => entry.Path.EndsWith("/FrontComposerWarningLog.cs", StringComparison.Ordinal))];

        ExpectedResidualWarningAndAboveLocations.Length.ShouldBe(54);
        ExpectedResidualWarningAndAboveLocations.Count(static location =>
            location.EndsWith("LogWarning", StringComparison.Ordinal)).ShouldBe(49);
        ExpectedResidualWarningAndAboveLocations.Count(static location =>
            location.EndsWith("LogError", StringComparison.Ordinal)).ShouldBe(5);
        ExpectedResidualWarningAndAboveLocations.Count(static location =>
            location.EndsWith("LogCritical", StringComparison.Ordinal)).ShouldBe(0);
        sites.ShouldBeEmpty(
            "every frozen residual Warning/Error/Critical site must use generated logging. " + FormatSites(sites));
        generatedSites.Select(static site => site.MemberKey).Order(StringComparer.Ordinal).ShouldBe(
            ExpectedResidualWarningAndAboveLocations.Select(GetMemberKey).Order(StringComparer.Ordinal),
            "each frozen production branch must retain exactly one generated warning wrapper call");
        generatedSites.Select(static site => site.MethodName).Distinct(StringComparer.Ordinal).Count().ShouldBe(54);
        generatedSites.Select(static site => site.MethodName).Order(StringComparer.Ordinal).ShouldBe(
            warningEvents.Select(static entry => entry.EventName).OfType<string>().Order(StringComparer.Ordinal),
            "all 54 generated warning events must each have one production call site");
    }

    private static IEnumerable<DirectLogSite> FindDirectLogSites(SourceFile source)
    {
        SemanticContext context = CreateSemanticContext(source);
        SyntaxTree tree = context.Tree;
        foreach (InvocationExpressionSyntax invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            IMethodSymbol? method = ResolveMethod(context.Model, invocation);
            string? methodName = method?.Name;
            bool isLoggingMethod = method is not null && IsDirectLoggingMethod(method);
            if (!isLoggingMethod
                && !IsUnqualifiedStaticLoggingCall(context.Model, invocation, out methodName))
            {
                continue;
            }

            int line = tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;
            yield return new(source.Path, FindContainingMember(invocation), line, methodName!);
        }
    }

    private static IEnumerable<GeneratedLogCallSite> FindWarningWrapperCallSites(SourceFile source)
    {
        SyntaxTree tree = Parse(source);
        foreach (InvocationExpressionSyntax invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: "FrontComposerWarningLog" },
                    Name: SimpleNameSyntax methodName,
                })
            {
                continue;
            }

            yield return new(source.Path, FindContainingMember(invocation), methodName.Identifier.ValueText);
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
        SemanticContext context = CreateSemanticContext(source);
        SyntaxTree tree = context.Tree;
        foreach (AttributeSyntax attribute in tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>()
            .Where(attribute => IsLoggerMessageAttribute(attribute, context.Model)))
        {
            MethodDeclarationSyntax? method = attribute.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            int? eventId = ReadEventId(attribute, context.Model);
            if (method is null || eventId is null)
            {
                continue;
            }

            string? eventName = ReadNamedConstant<string>(attribute, context.Model, "EventName");
            int? levelValue = ReadNamedConstant<int>(attribute, context.Model, "Level");
            string? level = levelValue is null
                ? null
                : Enum.GetName(typeof(Microsoft.Extensions.Logging.LogLevel), levelValue.Value);
            bool hasExceptionParameter = method.ParameterList.Parameters.Any(parameter =>
                IsExceptionParameterType(parameter.Type, context.Model));
            int line = tree.GetLineSpan(attribute.Span).StartLinePosition.Line + 1;
            yield return new(
                source.Path,
                line,
                eventId.Value,
                eventName,
                level,
                hasExceptionParameter,
                HasPlaceholderSignatureDrift(attribute, method, context.Model));
        }
    }

    private static bool IsExceptionParameterType(TypeSyntax? type, SemanticModel model)
    {
        if (type is null)
        {
            return false;
        }

        INamedTypeSymbol? symbol = model.GetTypeInfo(type).Type as INamedTypeSymbol;
        if (symbol is null or IErrorTypeSymbol)
        {
            // The Shell compiles with ImplicitUsings, so a scanned source may name `Exception`
            // without `using System;`. The single-file governance compilation cannot bind that
            // type, and without this fallback an Exception parameter would be silently misread as
            // a message placeholder. Only the exact spelling counts, so a look-alike such as
            // `FakeException` still fails to qualify. `global::System.Exception` is accepted for
            // fully-qualified source that never relies on a using directive.
            string spelling = type.ToString().TrimEnd('?');
            return spelling is "Exception" or "System.Exception" or "global::System.Exception";
        }

        while (symbol is not null)
        {
            if (symbol.ToDisplayString() == "System.Exception")
            {
                return true;
            }

            symbol = symbol.BaseType;
        }

        return false;
    }

    private static IEnumerable<string> FindUnwrappedIdentifierArguments(SourceFile source)
    {
        SyntaxTree tree = Parse(source);
        SyntaxNode root = tree.GetRoot();
        HashSet<string> generatedMethodNames = [.. root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Where(IsLoggerMessageAttribute)
            .Select(static attribute => attribute.FirstAncestorOrSelf<MethodDeclarationSyntax>())
            .OfType<MethodDeclarationSyntax>()
            .Select(static method => method.Identifier.ValueText)];

        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not IdentifierNameSyntax invokedName || invocation.ArgumentList is null)
            {
                continue;
            }

            MethodDeclarationSyntax? enclosingMethod = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (enclosingMethod is null)
            {
                continue;
            }

            bool isGeneratedCall = generatedMethodNames.Contains(invokedName.Identifier.ValueText);
            bool isDelegateCall = !isGeneratedCall && enclosingMethod.ParameterList.Parameters.Any(parameter =>
                string.Equals(parameter.Identifier.ValueText, invokedName.Identifier.ValueText, StringComparison.Ordinal)
                && parameter.Type?.ToString().Contains("Action", StringComparison.Ordinal) == true);
            if (!isGeneratedCall && !isDelegateCall)
            {
                continue;
            }

            foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
            {
                if (argument.Expression is not IdentifierNameSyntax identifier
                    || AllowedUnwrappedParameterNames.Contains(identifier.Identifier.ValueText))
                {
                    continue;
                }

                string name = identifier.Identifier.ValueText;
                ParameterSyntax? parameter = enclosingMethod.ParameterList.Parameters
                    .FirstOrDefault(candidate => string.Equals(candidate.Identifier.ValueText, name, StringComparison.Ordinal));
                if (parameter?.Type?.ToString().TrimEnd('?') == "string")
                {
                    yield return $"{source.Path}:{enclosingMethod.Identifier.ValueText}:{name}";
                }
            }
        }
    }

    private static bool HasPlaceholderSignatureDrift(
        AttributeSyntax attribute,
        MethodDeclarationSyntax method,
        SemanticModel model)
    {
        string? message = ReadNamedConstant<string>(attribute, model, "Message");
        HashSet<string> placeholders = ReadPlaceholders(message);
        HashSet<string> parameters = method.ParameterList.Parameters
            .Where(parameter => !IsLoggerParameterType(parameter.Type, model)
                && !IsExceptionParameterType(parameter.Type, model))
            .Select(static parameter => parameter.Identifier.ValueText)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return !placeholders.SetEquals(parameters);
    }

    private static HashSet<string> ReadPlaceholders(string? message)
    {
        HashSet<string> placeholders = new(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(message))
        {
            return placeholders;
        }

        for (int index = 0; index < message.Length; index++)
        {
            if (message[index] != '{')
            {
                continue;
            }

            if (index + 1 < message.Length && message[index + 1] == '{')
            {
                index++;
                continue;
            }

            int end = message.IndexOf('}', index + 1);
            if (end < 0)
            {
                break;
            }

            int separator = message.IndexOfAny([',', ':'], index + 1, end - index - 1);
            int nameEnd = separator >= 0 ? separator : end;
            string name = message[(index + 1)..nameEnd].Trim();
            if (name.Length > 0)
            {
                _ = placeholders.Add(name);
            }

            index = end;
        }

        return placeholders;
    }

    private static void AssertUniqueEventIds(IEnumerable<LoggerEvent> events)
    {
        string[] duplicates = [.. events
            .GroupBy(static entry => entry.EventId)
            .Where(static group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(static entry => entry.Location))}")];
        duplicates.ShouldBeEmpty("LoggerMessage EventIds must be unique. " + string.Join("; ", duplicates));
    }

    private static int? ReadEventId(AttributeSyntax attribute, SemanticModel model)
    {
        AttributeArgumentSyntax? argument = attribute.ArgumentList?.Arguments
            .FirstOrDefault(static candidate => candidate.NameEquals?.Name.Identifier.ValueText == "EventId")
            ?? attribute.ArgumentList?.Arguments.FirstOrDefault(static candidate => candidate.NameEquals is null);
        Optional<object?> value = argument is null
            ? default
            : model.GetConstantValue(argument.Expression);
        return value.HasValue && value.Value is not null
            ? Convert.ToInt32(value.Value, System.Globalization.CultureInfo.InvariantCulture)
            : null;
    }

    private static bool IsLoggerMessageAttribute(AttributeSyntax attribute)
        => attribute.Name.ToString() is "LoggerMessage" or "LoggerMessageAttribute";

    private static bool IsLoggerMessageAttribute(AttributeSyntax attribute, SemanticModel model)
    {
        ISymbol? symbol = model.GetSymbolInfo(attribute).Symbol;
        return symbol is IMethodSymbol method
            && method.ContainingType.ToDisplayString() == "Microsoft.Extensions.Logging.LoggerMessageAttribute";
    }

    private static T? ReadNamedConstant<T>(AttributeSyntax attribute, SemanticModel model, string name)
    {
        AttributeArgumentSyntax? argument = attribute.ArgumentList?.Arguments
            .FirstOrDefault(candidate => candidate.NameEquals?.Name.Identifier.ValueText == name);
        Optional<object?> value = argument is null
            ? default
            : model.GetConstantValue(argument.Expression);
        if (!value.HasValue || value.Value is null)
        {
            return default;
        }

        return value.Value is T typed
            ? typed
            : (T)Convert.ChangeType(value.Value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static SemanticContext CreateSemanticContext(SourceFile source)
        => SemanticContexts.GetOrAdd(source, static item =>
        {
            SyntaxTree tree = Parse(item);
            CSharpCompilation compilation = CSharpCompilation.Create(
                "SecurityLoggingGovernance",
                [tree],
                CompilationReferences.Value,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithNullableContextOptions(NullableContextOptions.Enable));
            return new(tree, compilation.GetSemanticModel(tree, ignoreAccessibility: true));
        });

    private static MetadataReference[] CreateCompilationReferences()
    {
        string[] paths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            ?? [];
        return [.. paths
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(static path => MetadataReference.CreateFromFile(path))];
    }

    private static IMethodSymbol? ResolveMethod(SemanticModel model, InvocationExpressionSyntax invocation)
    {
        SymbolInfo symbolInfo = model.GetSymbolInfo(invocation);
        IMethodSymbol? method = symbolInfo.Symbol as IMethodSymbol
            ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault(IsDirectLoggingMethod);
        if (method is not null)
        {
            return method;
        }

        symbolInfo = model.GetSymbolInfo(invocation.Expression);
        return symbolInfo.Symbol as IMethodSymbol
            ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault(IsDirectLoggingMethod);
    }

    private static bool IsDirectLoggingMethod(IMethodSymbol method)
    {
        if (!DirectLogMethodNames.Contains(method.Name))
        {
            return false;
        }

        IMethodSymbol canonical = method.ReducedFrom ?? method;
        string containingType = canonical.ContainingType.ToDisplayString();
        if (containingType == "Microsoft.Extensions.Logging.LoggerExtensions"
            || containingType == "Microsoft.Extensions.Logging.ILogger")
        {
            return true;
        }

        return method.Name == "Log"
            && canonical.ContainingType.AllInterfaces.Any(static interfaceType =>
                interfaceType.ToDisplayString() == "Microsoft.Extensions.Logging.ILogger");
    }

    private static bool IsLoggerParameterType(TypeSyntax? type, SemanticModel model)
    {
        INamedTypeSymbol? symbol = type is null ? null : model.GetTypeInfo(type).Type as INamedTypeSymbol;
        return IsLoggerType(symbol);
    }

    private static bool IsUnqualifiedStaticLoggingCall(
        SemanticModel model,
        InvocationExpressionSyntax invocation,
        out string? methodName)
    {
        methodName = (invocation.Expression as IdentifierNameSyntax)?.Identifier.ValueText;
        if (methodName is null
            || !DirectLogMethodNames.Contains(methodName)
            || invocation.ArgumentList.Arguments.Count == 0)
        {
            return false;
        }

        INamedTypeSymbol? firstArgumentType = model.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression).Type
            as INamedTypeSymbol;
        return IsLoggerType(firstArgumentType);
    }

    private static bool IsLoggerType(INamedTypeSymbol? symbol)
        => symbol is not null
            && (symbol.OriginalDefinition.ToDisplayString() is "Microsoft.Extensions.Logging.ILogger"
                or "Microsoft.Extensions.Logging.ILogger<TCategoryName>"
                || symbol.AllInterfaces.Any(static interfaceType =>
                    interfaceType.ToDisplayString() == "Microsoft.Extensions.Logging.ILogger"));

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

    private sealed record SemanticContext(SyntaxTree Tree, SemanticModel Model);

    private sealed record DirectLogSite(string Path, string MemberName, int Line, string MethodName)
    {
        public string MemberKey => $"{Path}:{MemberName}";

        public string Location => $"{Path}:{MemberName}:{Line}:{MethodName}";
    }

    private sealed record GeneratedLogCallSite(string Path, string MemberName, string MethodName)
    {
        public string MemberKey => $"{Path}:{MemberName}";
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
        string? Level,
        bool HasExceptionParameter,
        bool HasPlaceholderSignatureDrift)
    {
        public string Location => $"{Path}:{Line}";
    }
}
