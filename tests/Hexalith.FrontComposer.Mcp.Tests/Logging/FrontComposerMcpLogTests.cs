using Hexalith.FrontComposer.Mcp.Schema;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Logging;

public sealed class FrontComposerMcpLogTests
{
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
}
