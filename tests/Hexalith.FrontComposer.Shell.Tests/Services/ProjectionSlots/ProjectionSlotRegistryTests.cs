using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.ProjectionSlots;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Services.ProjectionSlots;

/// <summary>
/// Story 6-3 T3/T4/T7/T11 — runtime slot descriptor registry semantics.
/// </summary>
public sealed class ProjectionSlotRegistryTests {
    [Fact]
    public void Resolve_ExactRoleMatch_WinsOverAnyRoleSlot() {
        ProjectionSlotRegistry registry = NewRegistry(
            Descriptor("Priority", typeof(int), ProjectionRole.DetailRecord, typeof(DetailPrioritySlot)),
            Descriptor("Priority", typeof(int), null, typeof(AnyPrioritySlot)));

        ProjectionSlotDescriptor? resolved = registry.Resolve(typeof(SlotProjection), ProjectionRole.DetailRecord, "Priority");

        resolved.ShouldNotBeNull();
        resolved!.ComponentType.ShouldBe(typeof(DetailPrioritySlot));
    }

    [Fact]
    public void Resolve_FallsBackToAnyRoleSlot_WhenExactRoleMissing() {
        ProjectionSlotRegistry registry = NewRegistry(
            Descriptor("Priority", typeof(int), null, typeof(AnyPrioritySlot)));

        ProjectionSlotDescriptor? resolved = registry.Resolve(typeof(SlotProjection), ProjectionRole.Timeline, "Priority");

        resolved.ShouldNotBeNull();
        resolved!.ComponentType.ShouldBe(typeof(AnyPrioritySlot));
    }

    [Fact]
    public void Resolve_DifferentField_ReturnsNull() {
        ProjectionSlotRegistry registry = NewRegistry(
            Descriptor("Priority", typeof(int), null, typeof(AnyPrioritySlot)));

        registry.Resolve(typeof(SlotProjection), null, "Name").ShouldBeNull();
    }

    [Fact]
    public void Resolve_DuplicateExactSlot_FailsClosed() {
        ProjectionSlotRegistry registry = NewRegistry(
            Descriptor("Priority", typeof(int), null, typeof(AnyPrioritySlot)),
            Descriptor("Priority", typeof(int), null, typeof(SecondPrioritySlot)));

        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldBeNull();
        registry.Descriptors.ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_IncompatibleComponent_FallsBackToDefault() {
        ProjectionSlotRegistry registry = NewRegistry(
            Descriptor("Priority", typeof(int), null, typeof(WrongContextSlot)));

        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldBeNull();
    }

    [Fact]
    public void Resolve_OpenGenericComponent_FallsBackToDefault() {
        ProjectionSlotRegistry registry = NewRegistry(
            Descriptor("Priority", typeof(int), null, typeof(OpenGenericSlot<>)));

        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldBeNull();
    }

    [Fact]
    public void Descriptors_ExposeOnlyValidNonAmbiguousDescriptors() {
        ProjectionSlotRegistry registry = NewRegistry(
            Descriptor("Priority", typeof(int), null, typeof(AnyPrioritySlot)),
            Descriptor("Name", typeof(int), null, typeof(WrongContextSlot)));

        registry.Descriptors.Single().FieldName.ShouldBe("Priority");
    }

    private static ProjectionSlotRegistry NewRegistry(params ProjectionSlotDescriptor[] descriptors)
        => new(
            NullLogger<ProjectionSlotRegistry>.Instance,
            descriptors.Length == 0 ? [] : [new ProjectionSlotDescriptorSource(descriptors)]);

    private static ProjectionSlotDescriptor Descriptor(
        string fieldName,
        Type fieldType,
        ProjectionRole? role,
        Type componentType)
        => new(
            ProjectionType: typeof(SlotProjection),
            FieldName: fieldName,
            FieldType: fieldType,
            Role: role,
            ComponentType: componentType,
            ContractVersion: ProjectionSlotContractVersion.Current);

    public sealed record SlotProjection(int Priority, string Name);

    public sealed class AnyPrioritySlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, int> Context { get; set; } = default!;
    }

    public sealed class DetailPrioritySlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, int> Context { get; set; } = default!;
    }

    public sealed class SecondPrioritySlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, int> Context { get; set; } = default!;
    }

    public sealed class WrongContextSlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, string> Context { get; set; } = default!;
    }

    public sealed class OpenGenericSlot<T> : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, T> Context { get; set; } = default!;
    }
}
