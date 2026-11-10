using System.Diagnostics.CodeAnalysis;
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
    /// <summary>
    /// Records a command acknowledgement in the shared lifecycle store, bound to the principal that dispatched it.
    /// </summary>
    /// <param name="descriptor">Descriptor of the command tool that produced the acknowledgement.</param>
    /// <param name="creator">Agent context whose tenant and user were injected into the dispatched command.</param>
    /// <param name="result">Dispatcher acknowledgement containing the agent-visible handles.</param>
    /// <param name="pendingTransitions">Transitions captured by the command dispatcher callback.</param>
    /// <param name="cancellationToken">Cancellation token for pending transition recording.</param>
    /// <returns>The MCP acknowledgement payload.</returns>
    internal McpCommandAcknowledgement TrackAcknowledged(
        McpCommandDescriptor descriptor,
        FrontComposerMcpAgentContext creator,
        CommandResult result,
        IReadOnlyList<(CommandLifecycleState State, string? MessageId)> pendingTransitions,
        CancellationToken cancellationToken) {
        ILifecycleStateService lifecycle = services.GetService<ILifecycleStateService>()
            ?? throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        return store.TrackAcknowledged(
            descriptor,
            creator,
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
            // E14R-AI-1 D2: resolve the reader before any handle lookup, so a missing or unusable context
            // produces the same hidden-unknown result whether or not the handle exists.
            if (!TryResolveReader(out FrontComposerMcpAgentContext? reader)
                || !TryReadHandle(arguments, out string? handle)) {
                return HiddenUnknown();
            }

            if (!store.TryReadSnapshot(
                handle!,
                reader,
                options.Value,
                out McpCommandDescriptor descriptor,
                out McpLifecycleSnapshot snapshot)) {
                return HiddenUnknown();
            }

            McpToolResolutionResult current = await admissionService
                .ResolveAsync(descriptor.ProtocolName, reader, cancellationToken)
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

        // CA1873: the projection below is already deferred behind the IsEnabled guard above;
        // binding it to a local keeps that laziness explicit at the call site rather than relying
        // on the analyzer to model the early-return guard. No behavioural change.
        string exceptionType = ex.GetType().FullName ?? "Exception";
        LogReadFailureMessage(logger, exceptionType);
    }

    private bool TryResolveReader([NotNullWhen(true)] out FrontComposerMcpAgentContext? reader) {
        reader = null;
        IFrontComposerMcpAgentContextAccessor? accessor = services.GetService<IFrontComposerMcpAgentContextAccessor>();
        if (accessor is null) {
            return false;
        }

        FrontComposerMcpAgentContext? context;
        try {
            context = accessor.GetContext();
            if (context is null) {
                return false;
            }

            // Uses the admission context check itself, so a reader that admission would reject gets the same
            // answer for an issued handle as for a never-issued one, and the two rules cannot drift.
            FrontComposerMcpToolAdmissionService.ValidateContext(context);
        }
        catch (FrontComposerMcpException) {
            return false;
        }

        reader = context;
        return true;
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

        handle = pair.Value.GetString();
        return FrontComposerMcpUlid.IsCanonical(handle);
    }

    private static FrontComposerMcpResult HiddenUnknown()
        => FrontComposerMcpResult.Failure(
            FrontComposerMcpFailureCategory.UnknownTool,
            FrontComposerMcpToolAdmissionService.BuildHiddenUnknownStructuredContent());

    [LoggerMessage(EventId = 8300, Level = LogLevel.Warning,
        Message = "MCP lifecycle read failed with sanitized category. ExceptionType={ExceptionType}.")]
    private static partial void LogReadFailureMessage(ILogger logger, string exceptionType);
}
