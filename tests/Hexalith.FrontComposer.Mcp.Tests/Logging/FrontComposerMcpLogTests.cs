using Hexalith.FrontComposer.Mcp.Schema;

using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Logging;

public sealed class FrontComposerMcpLogTests
{
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
