using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.ProjectionSlots;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

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

    // GB-P8 — argument-guard coverage.

    [Fact]
    public void Constructor_NullSources_Throws()
        => Should.Throw<ArgumentNullException>(
            () => new ProjectionSlotRegistry(NullLogger<ProjectionSlotRegistry>.Instance, sources: null!));

    [Fact]
    public void Resolve_NullProjectionType_Throws() {
        ProjectionSlotRegistry registry = NewRegistry();
        Should.Throw<ArgumentNullException>(() => registry.Resolve(null!, null, "Priority"));
    }

    [Fact]
    public void Resolve_WhitespaceFieldName_Throws() {
        ProjectionSlotRegistry registry = NewRegistry();
        Should.Throw<ArgumentException>(() => registry.Resolve(typeof(SlotProjection), null, "   "));
    }

    // GB-P9 — diagnostic emission verification via capturing logger.

    [Fact]
    public void Register_IncompatibleContractVersionMajor_LogsHfc1041_And_DescriptorIsIgnored() {
        ListLogger<ProjectionSlotRegistry> logger = new();
        ProjectionSlotDescriptor mismatched = new(
            ProjectionType: typeof(SlotProjection),
            FieldName: "Priority",
            FieldType: typeof(int),
            Role: null,
            ComponentType: typeof(AnyPrioritySlot),
            ContractVersion: (ProjectionSlotContractVersion.Major + 1) * 1_000_000);

        ProjectionSlotRegistry registry = new(logger, [new ProjectionSlotDescriptorSource([mismatched])]);

        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldBeNull();
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("HFC1041") && e.Message.Contains("incompatible contract version"));
    }

    [Fact]
    public void Register_InvalidContractVersionZero_LogsHfc1041Invalid_And_DescriptorIsIgnored() {
        ListLogger<ProjectionSlotRegistry> logger = new();
        ProjectionSlotDescriptor invalid = new(
            ProjectionType: typeof(SlotProjection),
            FieldName: "Priority",
            FieldType: typeof(int),
            Role: null,
            ComponentType: typeof(AnyPrioritySlot),
            ContractVersion: 0);

        ProjectionSlotRegistry registry = new(logger, [new ProjectionSlotDescriptorSource([invalid])]);

        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldBeNull();
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("HFC1041") && e.Message.Contains("invalid contract version"));
    }

    [Fact]
    public void Register_MinorContractVersionDrift_LogsHfc1041Information_And_DescriptorIsAccepted() {
        ListLogger<ProjectionSlotRegistry> logger = new();
        ProjectionSlotDescriptor minorDrift = new(
            ProjectionType: typeof(SlotProjection),
            FieldName: "Priority",
            FieldType: typeof(int),
            Role: null,
            ComponentType: typeof(AnyPrioritySlot),
            ContractVersion: ProjectionSlotContractVersion.Current + 1_000);

        ProjectionSlotRegistry registry = new(logger, [new ProjectionSlotDescriptorSource([minorDrift])]);

        ProjectionSlotDescriptor? resolved = registry.Resolve(typeof(SlotProjection), null, "Priority");
        resolved.ShouldNotBeNull();
        resolved!.ComponentType.ShouldBe(typeof(AnyPrioritySlot));
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Information
            && e.Message.Contains("HFC1041")
            && e.Message.Contains("Override accepted"));
    }

    [Fact]
    public void Register_IncompatibleComponent_LogsHfc1039_WithSupportSafeShape() {
        ListLogger<ProjectionSlotRegistry> logger = new();
        ProjectionSlotRegistry registry = new(
            logger,
            [new ProjectionSlotDescriptorSource([Descriptor("Priority", typeof(int), null, typeof(WrongContextSlot))])]);

        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldBeNull();
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning
            && e.EventId.Id == 5826
            && e.EventId.Name == "ProjectionSlotInvalidComponent"
            && e.Message.Contains("HFC1039")
            && e.Message.Contains("ExpectedProjectionTypeDigest=sha256:")
            && e.Message.Contains("ComponentTypeDigest=sha256:")
            && !e.Message.Contains(typeof(WrongContextSlot).FullName!, StringComparison.Ordinal));
    }

    [Fact]
    public void Register_DuplicateExactSlot_LogsHfc1040_WithBothComponentTypes() {
        ListLogger<ProjectionSlotRegistry> logger = new();
        ProjectionSlotRegistry registry = new(
            logger,
            [
                new ProjectionSlotDescriptorSource([
                    Descriptor("Priority", typeof(int), null, typeof(AnyPrioritySlot)),
                    Descriptor("Priority", typeof(int), null, typeof(SecondPrioritySlot)),
                ])
            ]);

        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldBeNull();
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning
            && e.EventId.Id == 5827
            && e.EventId.Name == "ProjectionSlotDuplicate"
            && e.Message.Contains("HFC1040")
            && e.Message.Contains("ExistingComponentDigest=sha256:")
            && e.Message.Contains("NewComponentDigest=sha256:")
            && !e.Message.Contains(typeof(AnyPrioritySlot).FullName!, StringComparison.Ordinal)
            && !e.Message.Contains(typeof(SecondPrioritySlot).FullName!, StringComparison.Ordinal));
    }

    // GB-P12 — abstract / interface component rejection.

    [Fact]
    public void Register_AbstractComponent_FallsBackToDefault() {
        ProjectionSlotRegistry registry = NewRegistry(
            Descriptor("Priority", typeof(int), null, typeof(AbstractPrioritySlot)));
        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldBeNull();
    }

    [Fact]
    public void Register_InterfaceComponent_FallsBackToDefault() {
        ProjectionSlotRegistry registry = NewRegistry(
            Descriptor("Priority", typeof(int), null, typeof(IComponent)));
        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldBeNull();
    }

    // GB-P13 — public-setter requirement on Context property.

    [Fact]
    public void Register_GetOnlyContextProperty_FallsBackToDefault() {
        ProjectionSlotRegistry registry = NewRegistry(
            Descriptor("Priority", typeof(int), null, typeof(GetOnlyContextSlot)));
        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldBeNull();
    }

    [Fact]
    public void DescriptorSource_DefensiveCopiesInputList() {
        ProjectionSlotDescriptor original = Descriptor("Priority", typeof(int), null, typeof(AnyPrioritySlot));
        ProjectionSlotDescriptor replacement = Descriptor("Name", typeof(string), null, typeof(NameSlot));
        List<ProjectionSlotDescriptor> descriptors = [original];

        ProjectionSlotDescriptorSource source = new(descriptors);
        descriptors[0] = replacement;

        source.Descriptors.ShouldHaveSingleItem().ShouldBe(original);
        ProjectionSlotRegistry registry = new(NullLogger<ProjectionSlotRegistry>.Instance, [source]);
        registry.Resolve(typeof(SlotProjection), null, "Priority").ShouldNotBeNull();
        registry.Resolve(typeof(SlotProjection), null, "Name").ShouldBeNull();
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

    public sealed class NameSlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, string> Context { get; set; } = default!;
    }

    public sealed class WrongContextSlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, string> Context { get; set; } = default!;
    }

    public sealed class OpenGenericSlot<T> : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, T> Context { get; set; } = default!;
    }

    public abstract class AbstractPrioritySlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, int> Context { get; set; } = default!;
    }

#pragma warning disable BL0001 // BL0001 enforces public setter on [Parameter]; suppressed in test fixture so we can verify the registry's GB-P13 runtime check.
    public sealed class GetOnlyContextSlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, int> Context { get; } = default!;
    }
#pragma warning restore BL0001

    private sealed class ListLogger<T> : ILogger<T> {
        public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId, formatter(state, exception)));

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
