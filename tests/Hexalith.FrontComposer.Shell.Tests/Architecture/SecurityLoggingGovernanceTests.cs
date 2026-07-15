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

    private static readonly HashSet<string> Story11_18bMethods =
    [
        "LogWarning",
        "LogError",
        "LogCritical",
    ];

    private static readonly HashSet<string> Story11_18cMethods =
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

    private static readonly IReadOnlyDictionary<string, int> ExpectedDirectCallCounts
        = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs"] = 5,
            ["src/Hexalith.FrontComposer.Shell/Badges/ReflectionActionQueueProjectionCatalog.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Components/Layout/FcLayoutBreakpointWatcher.razor.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs"] = 4,
            ["src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs"] = 3,
            ["src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionSubtitle.razor.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapValidationGate.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs"] = 3,
            ["src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs"] = 18,
            ["src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Infrastructure/PendingCommands/PendingCommandPollingDriver.cs"] = 3,
            ["src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs"] = 3,
            ["src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs"] = 4,
            ["src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs"] = 4,
            ["src/Hexalith.FrontComposer.Shell/Services/Customization/CustomizationContractValidationGate.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Services/DevMode/ClipboardJSModule.cs"] = 4,
            ["src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs"] = 3,
            ["src/Hexalith.FrontComposer.Shell/Services/InMemoryDiagnosticSink.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs"] = 6,
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
            ["src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs"] = 11,
            ["src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs"] = 8,
            ["src/Hexalith.FrontComposer.Shell/State/Navigation/ScopeReadinessGate.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs"] = 5,
            ["src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs"] = 2,
            ["src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs"] = 11,
            ["src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStateService.cs"] = 1,
            ["src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs"] = 6,
            ["src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationStateService.cs"] = 1,
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
        sites.Length.ShouldBe(208);
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
            "every remaining direct call is explicitly path-pinned; Warning+ is owned by 11.18b and Trace/Debug/Information by 11.18c");

        DirectLogSite[] story11_18b = [.. sites.Where(site => Story11_18bMethods.Contains(site.MethodName))];
        DirectLogSite[] story11_18c = [.. sites.Where(site => Story11_18cMethods.Contains(site.MethodName))];
        DirectLogSite[] unowned = [.. sites.Where(site => !Story11_18bMethods.Contains(site.MethodName)
            && !Story11_18cMethods.Contains(site.MethodName))];
        story11_18b.Length.ShouldBe(117, "11.18b owns the remaining Warning/Error/Critical sites");
        story11_18c.Length.ShouldBe(91, "11.18c owns the remaining Trace/Debug/Information sites");
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
            yield return new(source.Path, line, methodName);
        }
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

    private sealed record DirectLogSite(string Path, int Line, string MethodName)
    {
        public string Location => $"{Path}:{Line}:{MethodName}";
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
