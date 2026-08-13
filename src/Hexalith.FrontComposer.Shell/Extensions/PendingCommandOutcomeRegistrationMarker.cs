using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Shell.Extensions;

internal sealed class PendingCommandOutcomeRegistrationMarker(
    ServiceDescriptor resolverDescriptor,
    ServiceDescriptor coordinatorDescriptor,
    ServiceDescriptor? concreteDescriptor) {
    public ServiceDescriptor ResolverDescriptor { get; } = resolverDescriptor;

    public ServiceDescriptor CoordinatorDescriptor { get; } = coordinatorDescriptor;

    public ServiceDescriptor? ConcreteDescriptor { get; } = concreteDescriptor;
}
