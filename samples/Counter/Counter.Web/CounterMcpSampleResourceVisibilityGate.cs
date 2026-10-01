using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp;

namespace Counter.Web;

/// <summary>Admits only the Counter projection resource for the explicit local sample identity.</summary>
internal sealed class CounterMcpSampleResourceVisibilityGate : IFrontComposerMcpResourceVisibilityGate
{
    /// <inheritdoc />
    public ValueTask<bool> IsVisibleAsync(
        McpResourceDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(CounterMcpSampleAccessPolicy.IsAdmitted(context)
            && string.Equals(descriptor.BoundedContext, "Counter", StringComparison.Ordinal)
            && string.Equals(descriptor.ProjectionTypeName, "Counter.Domain.CounterProjection", StringComparison.Ordinal)
            && string.Equals(descriptor.ProtocolUri, "frontcomposer://Counter/projections/CounterProjection", StringComparison.Ordinal));
    }
}
