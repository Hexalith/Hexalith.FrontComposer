using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services.ProjectionViewOverrides;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Extensions;

public sealed class ProjectionViewOverrideServiceCollectionExtensionsTests {
    [Fact]
    public void AddViewOverride_RegistersDescriptorSource_AndSelfRegistersRegistry() {
        ServiceCollection services = NewServices();

        _ = services.AddViewOverride<ViewProjection, ValidReplacement>();

        services.ShouldContain(d => d.ServiceType == typeof(IProjectionViewOverrideRegistry));
        services.Count(d => d.ServiceType == typeof(ProjectionViewOverrideDescriptorSource)).ShouldBe(1);
    }

    [Fact]
    public void AddViewOverride_RegistersDescriptorWithExpectedShape() {
        ServiceCollection services = NewServices();
        _ = services.AddViewOverride<ViewProjection, ValidReplacement>(ProjectionRole.DetailRecord);

        ServiceProvider provider = services.BuildServiceProvider();
        ProjectionViewOverrideDescriptor descriptor = provider
            .GetRequiredService<ProjectionViewOverrideDescriptorSource>()
            .Descriptors
            .ShouldHaveSingleItem();

        descriptor.ProjectionType.ShouldBe(typeof(ViewProjection));
        descriptor.Role.ShouldBe(ProjectionRole.DetailRecord);
        descriptor.ComponentType.ShouldBe(typeof(ValidReplacement));
        descriptor.ContractVersion.ShouldBe(ProjectionViewOverrideContractVersion.Current);
        descriptor.RegistrationSource.ShouldContain(nameof(AddViewOverride_RegistersDescriptorWithExpectedShape));
    }

    [Fact]
    public void Registry_RoleSpecificOverride_WinsBeforeRoleAgnosticOverride() {
        ServiceCollection services = NewServices();
        _ = services.AddViewOverride<ViewProjection, AnyRoleReplacement>();
        _ = services.AddViewOverride<ViewProjection, DetailReplacement>(ProjectionRole.DetailRecord);

        IProjectionViewOverrideRegistry registry = services.BuildServiceProvider()
            .GetRequiredService<IProjectionViewOverrideRegistry>();

        registry.Resolve(typeof(ViewProjection), ProjectionRole.DetailRecord)!.ComponentType.ShouldBe(typeof(DetailReplacement));
        registry.Resolve(typeof(ViewProjection), ProjectionRole.Timeline)!.ComponentType.ShouldBe(typeof(AnyRoleReplacement));
    }

    [Fact]
    public void Registry_DuplicateDifferentComponent_FailsHardOnConstruction() {
        // DN1 / AC7 / D6 — duplicates are deterministic hard failures so adopters discover
        // the misregistration at startup instead of silently falling through to generated.
        ServiceCollection services = NewServices();
        _ = services.AddViewOverride<ViewProjection, AnyRoleReplacement>();
        _ = services.AddViewOverride<ViewProjection, SecondAnyRoleReplacement>();

        ServiceProvider provider = services.BuildServiceProvider();
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            provider.GetRequiredService<IProjectionViewOverrideRegistry>);

        ex.Message.ShouldContain("HFC1044");
        ex.Message.ShouldContain(typeof(AnyRoleReplacement).FullName!);
        ex.Message.ShouldContain(typeof(SecondAnyRoleReplacement).FullName!);
    }

    [Fact]
    public void Registry_IdempotentReRegistration_KeepsSingleDescriptor() {
        // P11 — same (projection, role, component, version) registered twice from different
        // call sites is a no-op; only RegistrationSource differs and that field is excluded
        // from the idempotent check.
        ServiceCollection services = NewServices();
        _ = AddFromHelperA(services);
        _ = AddFromHelperB(services);

        IProjectionViewOverrideRegistry registry = services.BuildServiceProvider()
            .GetRequiredService<IProjectionViewOverrideRegistry>();

        registry.Descriptors.ShouldHaveSingleItem().ComponentType.ShouldBe(typeof(ValidReplacement));
        registry.Resolve(typeof(ViewProjection), null)!.ComponentType.ShouldBe(typeof(ValidReplacement));
    }

    private static IServiceCollection AddFromHelperA(IServiceCollection services)
        => services.AddViewOverride<ViewProjection, ValidReplacement>();

    private static IServiceCollection AddFromHelperB(IServiceCollection services)
        => services.AddViewOverride<ViewProjection, ValidReplacement>();

    [Fact]
    public void Registry_InvalidComponent_IsIgnored_AndGeneratedFallbackCanRun() {
        ServiceCollection services = NewServices();
        ProjectionViewOverrideDescriptor descriptor = new(
            typeof(ViewProjection),
            null,
            typeof(MissingContextReplacement),
            ProjectionViewOverrideContractVersion.Current,
            "test");
        _ = services.AddSingleton(new ProjectionViewOverrideDescriptorSource([descriptor]));
        _ = services.AddSingleton<IProjectionViewOverrideRegistry, ProjectionViewOverrideRegistry>();

        IProjectionViewOverrideRegistry registry = services.BuildServiceProvider()
            .GetRequiredService<IProjectionViewOverrideRegistry>();

        registry.Resolve(typeof(ViewProjection), null).ShouldBeNull();
    }

    [Fact]
    public void DescriptorSource_DefensiveCopiesInputList() {
        ProjectionViewOverrideDescriptor original = Descriptor(typeof(ValidReplacement));
        ProjectionViewOverrideDescriptor replacement = Descriptor(typeof(AnyRoleReplacement), ProjectionRole.DetailRecord);
        List<ProjectionViewOverrideDescriptor> descriptors = [original];

        ProjectionViewOverrideDescriptorSource source = new(descriptors);
        descriptors[0] = replacement;

        source.Descriptors.ShouldHaveSingleItem().ShouldBe(original);
        ProjectionViewOverrideRegistry registry = new(
            NullLogger<ProjectionViewOverrideRegistry>.Instance,
            [source]);

        registry.Descriptors.ShouldHaveSingleItem().ComponentType.ShouldBe(typeof(ValidReplacement));
        registry.Resolve(typeof(ViewProjection), null)!.ComponentType.ShouldBe(typeof(ValidReplacement));
        registry.Resolve(typeof(ViewProjection), ProjectionRole.DetailRecord)!.ComponentType.ShouldBe(typeof(ValidReplacement));
    }

    [Fact]
    public void Registry_MinorContractVersionDrift_LogsHfc1045Information_AndDescriptorIsAccepted() {
        ListLogger<ProjectionViewOverrideRegistry> logger = new();
        ProjectionViewOverrideDescriptor descriptor = Descriptor(
            typeof(ValidReplacement),
            contractVersion: ProjectionViewOverrideContractVersion.Current + 1_000);

        ProjectionViewOverrideRegistry registry = new(
            logger,
            [new ProjectionViewOverrideDescriptorSource([descriptor])]);

        registry.Resolve(typeof(ViewProjection), null)!.ComponentType.ShouldBe(typeof(ValidReplacement));
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Information
            && e.Message.Contains("HFC1045", StringComparison.Ordinal)
            && e.Message.Contains("Override accepted", StringComparison.Ordinal));
    }

    private static ServiceCollection NewServices() {
        ServiceCollection services = [];
        _ = services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        _ = services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        return services;
    }

    private static ProjectionViewOverrideDescriptor Descriptor(
        Type componentType,
        ProjectionRole? role = null,
        int contractVersion = ProjectionViewOverrideContractVersion.Current)
        => new(
            ProjectionType: typeof(ViewProjection),
            Role: role,
            ComponentType: componentType,
            ContractVersion: contractVersion,
            RegistrationSource: "test");

    public sealed record ViewProjection(int Id);

    public sealed class AnyRoleReplacement : ComponentBase {
        [Parameter]
        public ProjectionViewContext<ViewProjection> Context { get; set; } = default!;
    }

    public sealed class SecondAnyRoleReplacement : ComponentBase {
        [Parameter]
        public ProjectionViewContext<ViewProjection> Context { get; set; } = default!;
    }

    public sealed class DetailReplacement : ComponentBase {
        [Parameter]
        public ProjectionViewContext<ViewProjection> Context { get; set; } = default!;
    }

    public sealed class ValidReplacement : ComponentBase {
        [Parameter]
        public ProjectionViewContext<ViewProjection> Context { get; set; } = default!;
    }

    public sealed class MissingContextReplacement : ComponentBase {
    }

    private sealed class ListLogger<T> : ILogger<T> {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
