using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp.Invocation;

public sealed partial class FrontComposerMcpLifecycleTracker(
    FrontComposerMcpLifecycleStore store,
    FrontComposerMcpToolAdmissionService admissionService,
    IServiceProvider services,
    IOptions<FrontComposerMcpOptions> options,
    ILogger<FrontComposerMcpLifecycleTracker>? logger = null) {
    internal McpCommandAcknowledgement TrackAcknowledged(
        McpCommandDescriptor descriptor,
        CommandResult result,
        IReadOnlyList<(CommandLifecycleState State, string? MessageId)> pendingTransitions,
        CancellationToken cancellationToken) {
        ILifecycleStateService lifecycle = services.GetService<ILifecycleStateService>()
            ?? throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        return store.TrackAcknowledged(
            descriptor,
            result,
            pendingTransitions,
            lifecycle,
            cancellationToken);
    }

    public Task<FrontComposerMcpResult> ReadAsync(
        IReadOnlyDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken = default) => ReadCoreAsync(arguments, cancellationToken);

    private async Task<FrontComposerMcpResult> ReadCoreAsync(
        IReadOnlyDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken) {
        try {
            if (!TryReadHandle(arguments, out string? handle)) {
                return HiddenUnknown();
            }

            if (!store.TryReadSnapshot(
                handle!,
                options.Value,
                out McpCommandDescriptor descriptor,
                out McpLifecycleSnapshot snapshot)) {
                return HiddenUnknown();
            }

            McpToolResolutionResult current = await admissionService
                .ResolveAsync(descriptor.ProtocolName, cancellationToken)
                .ConfigureAwait(false);
            if (!current.Accepted
                || current.Tool is null
                || !string.Equals(current.Tool.Descriptor.ProtocolName, descriptor.ProtocolName, StringComparison.Ordinal)) {
                return HiddenUnknown();
            }

            return FrontComposerMcpResult.Success("Lifecycle snapshot.", snapshot.ToJson());
        }
        catch (OperationCanceledException) {
            return FrontComposerMcpResult.Failure(
                cancellationToken.IsCancellationRequested
                    ? FrontComposerMcpFailureCategory.Canceled
                    : FrontComposerMcpFailureCategory.DownstreamFailed);
        }
        catch (FrontComposerMcpException ex) {
            return FrontComposerMcpResult.Failure(ex.Category);
        }
        catch (Exception ex) {
            LogReadFailure(ex);
            return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.DownstreamFailed);
        }
    }

    private void LogReadFailure(Exception ex) {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning)) {
            return;
        }

        LogReadFailureMessage(logger, ex.GetType().FullName ?? "Exception");
    }

    private static bool TryReadHandle(IReadOnlyDictionary<string, JsonElement>? arguments, out string? handle) {
        handle = null;
        if (arguments is null || arguments.Count != 1) {
            return false;
        }

        KeyValuePair<string, JsonElement> pair = arguments.Single();
        if (pair.Key is not "correlationId" and not "messageId"
            || pair.Value.ValueKind != JsonValueKind.String) {
            return false;
        }

        handle = FrontComposerMcpLifecycleStore.NormalizeIdentifier(pair.Value.GetString());
        return handle is not null;
    }

    private static FrontComposerMcpResult HiddenUnknown()
        => FrontComposerMcpResult.Failure(
            FrontComposerMcpFailureCategory.UnknownTool,
            FrontComposerMcpToolAdmissionService.BuildHiddenUnknownStructuredContent());

    [LoggerMessage(EventId = 8300, Level = LogLevel.Warning,
        Message = "MCP lifecycle read failed with sanitized category. ExceptionType={ExceptionType}.")]
    private static partial void LogReadFailureMessage(ILogger logger, string exceptionType);
}
