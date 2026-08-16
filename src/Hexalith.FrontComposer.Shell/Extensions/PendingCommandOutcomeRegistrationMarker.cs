using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>Identifies the pending-command outcome services registered for one scoped producer boundary.</summary>
/// <param name="resolverDescriptor">The resolver service descriptor.</param>
/// <param name="coordinatorDescriptor">The coordinator service descriptor.</param>
/// <param name="concreteDescriptor">The optional concrete resolver service descriptor.</param>
internal sealed class PendingCommandOutcomeRegistrationMarker(
    ServiceDescriptor resolverDescriptor,
    ServiceDescriptor coordinatorDescriptor,
    ServiceDescriptor? concreteDescriptor) {
    /// <summary>Gets the resolver service descriptor.</summary>
    public ServiceDescriptor ResolverDescriptor { get; } = resolverDescriptor;

    /// <summary>Gets the coordinator service descriptor.</summary>
    public ServiceDescriptor CoordinatorDescriptor { get; } = coordinatorDescriptor;

    /// <summary>Gets the optional concrete resolver service descriptor.</summary>
    public ServiceDescriptor? ConcreteDescriptor { get; } = concreteDescriptor;
}
