using Counter.Specimens;
using Counter.Specimens.Domain;
using Counter.Web;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Specimens;

using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Samples;

/// <summary>Fixed failure seams (command service and Test-host service replacements) cannot activate outside the Test host.</summary>
public sealed class AccessibilitySpecimenGateTests
{
    [Theory]
    [InlineData("Production", "true", false)]
    [InlineData("Development", "true", false)]
    [InlineData("Test", "false", false)]
    [InlineData("Test", "true", true)]
    public async Task CommandFailureRequiresBothTestEnvironmentAndSpecimenOptIn(string environmentName, string enabled, bool active)
    {
        AccessibilitySpecimenState scenarios = new();
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            [FrontComposerSpecimenRoutes.EnabledConfigurationKey] = enabled,
        }).Build();
        ICommandServiceWithLifecycleObservations inner = Substitute.For<ICommandServiceWithLifecycleObservations>();
        inner.DispatchAsync(Arg.Any<AccessibilityZeroFieldCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult("01ARZ3NDEKTSV4RRFFQ69G5FAV", CommandResultStatus.Accepted));
        CounterSampleCommandService service = new(inner, new CounterCommandProjectionCatchUpChannel(), new DemoUserContextAccessor(),
            NullLogger<CounterSampleCommandService>.Instance, configuration, environment, scenarios);
        if (active)
        {
            await Should.ThrowAsync<CommandValidationException>(() => service.DispatchAsync(new AccessibilityZeroFieldCommand(), TestContext.Current.CancellationToken));
            scenarios.DispatchCount.ShouldBe(1);
            await inner.DidNotReceive().DispatchAsync(Arg.Any<AccessibilityZeroFieldCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
        }
        else
        {
            (await service.DispatchAsync(new AccessibilityZeroFieldCommand(), TestContext.Current.CancellationToken)).Status.ShouldBe(CommandResultStatus.Accepted);
            scenarios.DispatchCount.ShouldBe(0);
            await inner.Received(1).DispatchAsync(Arg.Any<AccessibilityZeroFieldCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
        }
    }

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Development", false)]
    [InlineData("Test", true)]
    public void ServiceReplacementsAreRegisteredOnlyInTheTestEnvironment(string environmentName, bool registered)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        ServiceCollection services = [];

        _ = services.AddAccessibilitySpecimenSeams(environment);

        services.Any(d => d.ServiceType == typeof(AccessibilitySpecimenState)).ShouldBe(registered);
        services.Any(d => d.ServiceType == typeof(AccessibilitySpecimenUserContextAccessor)).ShouldBe(registered);
        services.Any(d => d.ServiceType == typeof(Action<bool>)).ShouldBe(registered);
        services.Any(d => d.ServiceType == typeof(AccessibilitySpecimenPendingScope)).ShouldBe(registered);
        services.Any(d => d.ServiceType == typeof(ICommandAuthorizationEvaluator)).ShouldBe(registered);
        services.Any(d => d.ServiceType == typeof(IProjectionPageLoader)).ShouldBe(registered);
        services.Any(d => d.ServiceType == typeof(ICommandExecutionAdmissionGate)).ShouldBe(registered);
        services.Any(d => d.ServiceType == typeof(AuthenticationStateProvider)).ShouldBe(registered);
    }
}
