using System.Reflection;

using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

[Trait("Category", "Governance")]
public sealed class PendingStatusReopenGovernanceTests {
    [Fact]
    public void Story35Disposition_DefaultFrontComposerRegistrationKeepsNullProvider() {
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposer();

        ServiceDescriptor descriptor = PendingStatusDescriptors(services).Single();

        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        descriptor.ImplementationType.ShouldBe(typeof(NullPendingCommandStatusQuery));
    }

    [Fact]
    public void Story35Disposition_EventStoreRegistrationReplacesNullProvider() {
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithEventStore(options => {
            options.BaseAddress = new Uri("https://eventstore.test");
            options.RequireAccessToken = false;
        });

        ServiceDescriptor descriptor = PendingStatusDescriptors(services).Single();

        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        descriptor.ImplementationType.ShouldBe(typeof(EventStorePendingCommandStatusQuery));
    }

    [Fact]
    public void Story35Disposition_EventStoreOptionsDoNotExposeAConfigurablePendingStatusEndpoint() {
        string[] suspicious = [.. typeof(EventStoreOptions)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(static property => property.Name)
            .Where(IsSuspiciousStatusContractProperty)
            .Order(StringComparer.Ordinal)];

        suspicious.ShouldBeEmpty(
            "Story 3.5 intentionally uses the confirmed fixed /api/v1/commands/status/{id} path; "
            + "new status endpoint options need an explicit future story.");
    }

    private static ServiceDescriptor[] PendingStatusDescriptors(IServiceCollection services)
        => [.. services.Where(static descriptor =>
            descriptor.ServiceType == typeof(IPendingCommandStatusQuery)
            && !descriptor.IsKeyedService)];

    private static bool IsSuspiciousStatusContractProperty(string propertyName) {
        string[] exactSuspiciousFragments = [
            "PendingStatus",
            "PendingCommandStatus",
            "CommandStatusQuery",
            "StatusResource",
            "StatusQueryProvider",
        ];
        if (exactSuspiciousFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase))) {
            return true;
        }

        bool containsStatus = propertyName.Contains("Status", StringComparison.OrdinalIgnoreCase);
        if (!containsStatus) {
            return false;
        }

        string[] resourceFragments = ["Endpoint", "Uri", "Url", "Path", "Resource", "Metadata"];
        return resourceFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            || propertyName.Contains("PendingCommand", StringComparison.OrdinalIgnoreCase);
    }
}
