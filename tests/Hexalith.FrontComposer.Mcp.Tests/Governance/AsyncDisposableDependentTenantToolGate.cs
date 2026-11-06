using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

/// <summary>A restrictive scoped tenant gate whose dependency is disposable only asynchronously.</summary>
/// <param name="dependency">The asynchronously disposable dependency.</param>
internal sealed class AsyncDisposableDependentTenantToolGate(AsyncOnlyDisposableGateDependency dependency) : IFrontComposerMcpTenantToolGate {
    /// <inheritdoc />
    public ValueTask<bool> IsVisibleAsync(
        McpCommandDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(dependency);
        return ValueTask.FromResult(false);
    }
}
