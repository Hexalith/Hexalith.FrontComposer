using System.Reflection;

using Hexalith.FrontComposer.Mcp.Schema;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Logging;

public sealed class FrontComposerMcpLogTests
{
    private static readonly LogLevel[] EmittableLevels =
    [
        LogLevel.Trace,
        LogLevel.Debug,
        LogLevel.Information,
        LogLevel.Warning,
        LogLevel.Error,
        LogLevel.Critical,
    ];

    private static readonly (string Name, LogLevel Level, int EventId, Action<ILogger> Invoke)[] Wrappers =
    [
        ("ToolsListFailedClosed", LogLevel.Warning, 8310,
            static logger => FrontComposerMcpLog.ToolsListFailedClosed(logger, FrontComposerMcpFailureCategory.SchemaMismatch, "Exception")),
        ("LifecyclePrecheckFailedClosed", LogLevel.Warning, 8311,
            static logger => FrontComposerMcpLog.LifecyclePrecheckFailedClosed(logger, FrontComposerMcpFailureCategory.SchemaMismatch, "Exception")),
        ("ProjectionReaderFailedClosed", LogLevel.Warning, 8312,
            static logger => FrontComposerMcpLog.ProjectionReaderFailedClosed(logger, FrontComposerMcpFailureCategory.SchemaMismatch, "Exception")),
        ("TenantToolGateFailedClosed", LogLevel.Warning, 8313,
            static logger => FrontComposerMcpLog.TenantToolGateFailedClosed(logger, "orders", "Exception")),
        ("PolicyGateFailedClosed", LogLevel.Warning, 8314,
            static logger => FrontComposerMcpLog.PolicyGateFailedClosed(logger, "orders", "Exception")),
        ("CommandInvocationSchemaFailed", LogLevel.Information, 8315,
            static logger => FrontComposerMcpLog.CommandInvocationSchemaFailed(logger, FrontComposerMcpFailureCategory.SchemaMismatch)),
        ("CommandInvocationKnownFailure", LogLevel.Warning, 8316,
            static logger => FrontComposerMcpLog.CommandInvocationKnownFailure(logger, FrontComposerMcpFailureCategory.AuthFailed)),
        ("CommandInvocationUnexpectedFailure", LogLevel.Warning, 8317,
            static logger => FrontComposerMcpLog.CommandInvocationUnexpectedFailure(logger, typeof(InvalidOperationException).FullName!)),
        ("SchemaNegotiationDecision", LogLevel.Information, 8318,
            static logger => FrontComposerMcpLog.SchemaNegotiationDecision(
                logger,
                "schema_warning",
                "schema.compatible-warning",
                "HFC-MCP-SCHEMA-WARNING",
                McpSchemaNegotiationResultKind.CompatibleWarning)),
    ];

    /// <summary>
    /// Verifies that disabled generated logging wrappers return without emitting.
    /// </summary>
    [Fact]
    public void DisabledLogger_AllWrappersReturnWithoutEmitting()
    {
        ILogger logger = Substitute.For<ILogger>();
        FrontComposerMcpFailureCategory invalidCategory = (FrontComposerMcpFailureCategory)int.MaxValue;
        string oversizedContext = new('x', 4097);

        Should.NotThrow(() =>
        {
            FrontComposerMcpLog.ToolsListFailedClosed(logger, invalidCategory, "Exception");
            FrontComposerMcpLog.LifecyclePrecheckFailedClosed(logger, invalidCategory, "Exception");
            FrontComposerMcpLog.ProjectionReaderFailedClosed(logger, invalidCategory, "Exception");
            FrontComposerMcpLog.TenantToolGateFailedClosed(logger, oversizedContext, "Exception");
            FrontComposerMcpLog.PolicyGateFailedClosed(logger, oversizedContext, "Exception");
            FrontComposerMcpLog.CommandInvocationSchemaFailed(logger, invalidCategory);
            FrontComposerMcpLog.CommandInvocationKnownFailure(logger, invalidCategory);
            FrontComposerMcpLog.CommandInvocationUnexpectedFailure(logger, string.Empty);
            FrontComposerMcpLog.SchemaNegotiationDecision(
                logger,
                oversizedContext,
                oversizedContext,
                oversizedContext,
                (McpSchemaNegotiationResultKind)int.MaxValue);
        });
        logger.ReceivedCalls().ShouldAllBe(static call =>
            !string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal));
    }

    [Fact]
    public void CommandInvocationEvents_SecurityInputs_EmitPinnedSanitizedContracts()
    {
        const string ExceptionSentinel = "jwt.payload.secret";
        CapturingLogger<FrontComposerMcpLogTests> logger = new();

        FrontComposerMcpLog.CommandInvocationSchemaFailed(logger, FrontComposerMcpFailureCategory.SchemaMismatch);
        FrontComposerMcpLog.CommandInvocationKnownFailure(logger, FrontComposerMcpFailureCategory.AuthFailed);
        FrontComposerMcpLog.CommandInvocationUnexpectedFailure(logger, typeof(InvalidOperationException).FullName!);
        FrontComposerMcpLog.SchemaNegotiationDecision(
            logger,
            "schema_warning",
            "schema.compatible-warning",
            "HFC-MCP-SCHEMA-WARNING",
            McpSchemaNegotiationResultKind.CompatibleWarning);

        logger.Entries.Select(static entry => entry.EventId.Id).ShouldBe([8315, 8316, 8317, 8318]);
        logger.Entries.Select(static entry => entry.EventId.Name).ShouldBe([
            "McpCommandSchemaFailed",
            "McpCommandKnownFailure",
            "McpCommandDownstreamFailed",
            "McpSchemaDecision",
        ]);
        logger.Entries.Select(static entry => entry.Level).ShouldBe([
            LogLevel.Information,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Information,
        ]);
        logger.Entries.ShouldAllBe(static entry => entry.Exception == null);
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(ExceptionSentinel, StringComparison.Ordinal));
        logger.Entries[2].State["ExceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
    }

    /// <summary>
    /// Gets each public wrapper paired with the level and event its generated call declares.
    /// </summary>
    public static TheoryData<string, LogLevel, int> WrapperContracts {
        get {
            TheoryData<string, LogLevel, int> data = [];
            foreach ((string Name, LogLevel Level, int EventId, Action<ILogger> _) wrapper in Wrappers) {
                data.Add(wrapper.Name, wrapper.Level, wrapper.EventId);
            }

            return data;
        }
    }

    /// <summary>
    /// Verifies that each wrapper emits its pinned event when its own declared level is enabled and
    /// stays silent when only another level is enabled. A guard checking the wrong level fails here.
    /// </summary>
    /// <param name="name">The wrapper name under test.</param>
    /// <param name="level">The level the wrapper's generated call declares.</param>
    /// <param name="eventId">The event identifier the wrapper emits.</param>
    [Theory]
    [MemberData(nameof(WrapperContracts))]
    public void Wrapper_EmitsOnlyWhenItsDeclaredLevelIsEnabled(string name, LogLevel level, int eventId)
    {
        Action<ILogger> invoke = Wrappers.Single(entry => string.Equals(entry.Name, name, StringComparison.Ordinal)).Invoke;

        SingleLevelLogger declaredLevel = new(level);
        invoke(declaredLevel);
        declaredLevel.Entries.Select(static entry => entry.EventId.Id).ShouldBe(
            [eventId],
            $"{name} must emit event {eventId} when {level} is enabled");
        declaredLevel.Entries[0].Level.ShouldBe(level);

        foreach (LogLevel other in EmittableLevels.Where(candidate => candidate != level))
        {
            SingleLevelLogger otherLevel = new(other);
            invoke(otherLevel);
            otherLevel.Entries.ShouldBeEmpty($"{name} must stay silent when only {other} is enabled");
        }
    }

    /// <summary>
    /// Verifies that every public wrapper is covered by the level contract table.
    /// </summary>
    [Fact]
    public void WrapperContracts_CoverEveryPublicWrapper()
    {
        string[] declared = [.. typeof(FrontComposerMcpLog)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(static method => method.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        declared.ShouldBe(
            [.. Wrappers.Select(static wrapper => wrapper.Name).Order(StringComparer.Ordinal)],
            "every public wrapper must declare its level contract");
    }
}
