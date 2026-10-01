using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp;

namespace Counter.Web;

/// <summary>Admits only the explicit Counter sample command inventory for its local identity.</summary>
internal sealed class CounterMcpSampleTenantToolGate : IFrontComposerMcpTenantToolGate
{
    /// <inheritdoc />
    public ValueTask<bool> IsVisibleAsync(
        McpCommandDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        cancellationToken.ThrowIfCancellationRequested();
        bool allowedCommand = (descriptor.BoundedContext, descriptor.ProtocolName, descriptor.CommandTypeName) is
            ("Counter", "Counter.CreateCounterCommand.Execute", "Counter.Domain.CreateCounterCommand")
            or ("Counter", "Counter.UpdateCounterCommand.Execute", "Counter.Domain.UpdateCounterCommand")
            or ("Default", "Default.IncrementCommand.Execute", "Counter.Domain.IncrementCommand")
            or ("Counter", "Counter.BatchIncrementCommand.Execute", "Counter.Domain.BatchIncrementCommand")
            or ("Counter", "Counter.ConfigureCounterCommand.Execute", "Counter.Domain.ConfigureCounterCommand");
        return ValueTask.FromResult(CounterMcpSampleAccessPolicy.IsAdmitted(context)
            && allowedCommand);
    }
}
