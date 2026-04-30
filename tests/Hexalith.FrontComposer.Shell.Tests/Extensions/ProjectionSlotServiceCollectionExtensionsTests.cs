using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services.ProjectionSlots;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Extensions;

/// <summary>
/// Story 6-3 GB-P17 — public <see cref="ProjectionSlotServiceCollectionExtensions"/> behavior:
/// AC1 refactor-safe registration, descriptor-source DI shape, registry self-registration
/// (GB-P15), idempotent re-registration via record equality, and typed-component overload
/// constraints (GB-P10).
/// </summary>
public sealed class ProjectionSlotServiceCollectionExtensionsTests {
    [Fact]
    public void AddSlotOverride_RegistersOneDescriptorSource_AndSelfRegistersRegistry() {
        ServiceCollection services = NewServices();

        _ = services.AddSlotOverride<SlotProjection, int, AnyPrioritySlot>(p => p.Priority);

        services.ShouldContain(d => d.ServiceType == typeof(IProjectionSlotRegistry));
        services.Count(d => d.ServiceType == typeof(ProjectionSlotDescriptorSource)).ShouldBe(1);
    }

    [Fact]
    public void AddSlotOverride_RegistersDescriptorWithExpectedShape() {
        ServiceCollection services = NewServices();
        _ = services.AddSlotOverride<SlotProjection, int, AnyPrioritySlot>(
            p => p.Priority,
            ProjectionRole.DetailRecord);

        ServiceProvider provider = services.BuildServiceProvider();
        ProjectionSlotDescriptorSource source = provider.GetRequiredService<ProjectionSlotDescriptorSource>();
        ProjectionSlotDescriptor descriptor = source.Descriptors.ShouldHaveSingleItem();

        descriptor.ProjectionType.ShouldBe(typeof(SlotProjection));
        descriptor.FieldName.ShouldBe("Priority");
        descriptor.FieldType.ShouldBe(typeof(int));
        descriptor.Role.ShouldBe(ProjectionRole.DetailRecord);
        descriptor.ComponentType.ShouldBe(typeof(AnyPrioritySlot));
        descriptor.ContractVersion.ShouldBe(ProjectionSlotContractVersion.Current);
    }

    [Fact]
    public void AddSlotOverride_ResolvesViaRegistry() {
        ServiceCollection services = NewServices();
        _ = services.AddSlotOverride<SlotProjection, int, AnyPrioritySlot>(p => p.Priority);

        ServiceProvider provider = services.BuildServiceProvider();
        IProjectionSlotRegistry registry = provider.GetRequiredService<IProjectionSlotRegistry>();
        ProjectionSlotDescriptor? resolved = registry.Resolve(typeof(SlotProjection), null, "Priority");
        resolved.ShouldNotBeNull();
        resolved!.ComponentType.ShouldBe(typeof(AnyPrioritySlot));
    }

    [Fact]
    public void AddSlotOverride_InvalidSelector_ThrowsAtCallSite() {
        ServiceCollection services = NewServices();
        Should.Throw<ProjectionSlotSelectorException>(() =>
            services.AddSlotOverride<SlotProjection, int, AnyPrioritySlot>(p => p.Priority + 1));
    }

    [Fact]
    public void AddSlotOverride_DuplicateIdenticalCall_DedupsViaRecordEquality_NoHfc1040() {
        ServiceCollection services = NewServices();
        _ = services.AddSlotOverride<SlotProjection, int, AnyPrioritySlot>(p => p.Priority);
        _ = services.AddSlotOverride<SlotProjection, int, AnyPrioritySlot>(p => p.Priority);

        ServiceProvider provider = services.BuildServiceProvider();
        IProjectionSlotRegistry registry = provider.GetRequiredService<IProjectionSlotRegistry>();
        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldNotBeNull();
    }

    [Fact]
    public void AddSlotOverride_DuplicateDifferentComponent_FailsClosed() {
        ServiceCollection services = NewServices();
        _ = services.AddSlotOverride<SlotProjection, int, AnyPrioritySlot>(p => p.Priority);
        _ = services.AddSlotOverride<SlotProjection, int, SecondPrioritySlot>(p => p.Priority);

        ServiceProvider provider = services.BuildServiceProvider();
        IProjectionSlotRegistry registry = provider.GetRequiredService<IProjectionSlotRegistry>();
        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldBeNull();
    }

    [Fact]
    public void AddSlotOverride_TypedOverload_PassesComponentTypeToDescriptor() {
        ServiceCollection services = NewServices();
        _ = services.AddSlotOverride<SlotProjection, int, AnyPrioritySlot>(p => p.Priority);

        ServiceProvider provider = services.BuildServiceProvider();
        ProjectionSlotDescriptorSource source = provider.GetRequiredService<ProjectionSlotDescriptorSource>();
        source.Descriptors.Single().ComponentType.ShouldBe(typeof(AnyPrioritySlot));
    }

    [Fact]
    public void AddSlotOverride_NonTypedOverload_AcceptsRuntimeComponentType() {
        ServiceCollection services = NewServices();
        _ = services.AddSlotOverride<SlotProjection, int>(p => p.Priority, typeof(AnyPrioritySlot));

        ServiceProvider provider = services.BuildServiceProvider();
        ProjectionSlotDescriptorSource source = provider.GetRequiredService<ProjectionSlotDescriptorSource>();
        source.Descriptors.Single().ComponentType.ShouldBe(typeof(AnyPrioritySlot));
    }

    private static ServiceCollection NewServices() {
        ServiceCollection services = [];
        _ = services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        _ = services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        return services;
    }

    public sealed record SlotProjection(int Priority);

    public sealed class AnyPrioritySlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, int> Context { get; set; } = default!;
    }

    public sealed class SecondPrioritySlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, int> Context { get; set; } = default!;
    }
}
