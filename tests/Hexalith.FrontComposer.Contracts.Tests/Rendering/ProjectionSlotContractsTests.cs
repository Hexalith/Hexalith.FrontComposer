using System.Linq.Expressions;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

/// <summary>
/// Story 6-3 T1/T2/T11 — Level 3 field-slot contract invariants.
/// </summary>
public sealed class ProjectionSlotContractsTests {
    [Fact]
    public void ContractVersion_Current_PacksMajorMinorBuild() {
        ProjectionSlotContractVersion.Current.ShouldBe(1_000_000);
        ProjectionSlotContractVersion.Major.ShouldBe(1);
        ProjectionSlotContractVersion.Minor.ShouldBe(0);
        ProjectionSlotContractVersion.Build.ShouldBe(0);
    }

    [Fact]
    public void FieldSlotContext_ExposesSuppliedInputs() {
        FieldDescriptor field = new(
            Name: "Priority",
            TypeName: typeof(int).FullName!,
            IsNullable: false,
            DisplayName: "Priority",
            Format: "N0",
            Order: 1,
            IsReadOnly: true,
            Hints: null);
        RenderContext renderContext = new("tenant-a", "user-b", FcRenderMode.Server, DensityLevel.Compact, IsReadOnly: true) {
            IsDevMode = true,
        };
        ProjectionSlotProjection parent = new(42, "Alpha");
        RenderFragment<FieldSlotContext<ProjectionSlotProjection, int>> fallback = static _ => static _ => { };

        FieldSlotContext<ProjectionSlotProjection, int> context = new(
            value: 42,
            parent: parent,
            field: field,
            renderContext: renderContext,
            projectionRole: ProjectionRole.ActionQueue,
            densityLevel: DensityLevel.Compact,
            isReadOnly: true,
            isDevMode: true,
            renderDefault: fallback);

        context.Value.ShouldBe(42);
        context.Parent.ShouldBe(parent);
        context.Field.ShouldBeSameAs(field);
        context.RenderContext.ShouldBeSameAs(renderContext);
        context.ProjectionRole.ShouldBe(ProjectionRole.ActionQueue);
        context.DensityLevel.ShouldBe(DensityLevel.Compact);
        context.IsReadOnly.ShouldBeTrue();
        context.IsDevMode.ShouldBeTrue();
        context.RenderDefault.ShouldBeSameAs(fallback);
    }

    [Fact]
    public void FieldSlotContext_NullRequiredArguments_Throw() {
        FieldDescriptor field = new("Priority", typeof(int).FullName!, IsNullable: false);
        RenderContext renderContext = new("tenant-a", "user-b", FcRenderMode.Server, DensityLevel.Comfortable, IsReadOnly: false);
        ProjectionSlotProjection parent = new(42, "Alpha");

        Should.Throw<ArgumentNullException>(() =>
            new FieldSlotContext<ProjectionSlotProjection, int>(
                value: 42,
                parent: null!,
                field: field,
                renderContext: renderContext,
                projectionRole: null,
                densityLevel: DensityLevel.Comfortable,
                isReadOnly: false,
                isDevMode: false,
                renderDefault: null));

        Should.Throw<ArgumentNullException>(() =>
            new FieldSlotContext<ProjectionSlotProjection, int>(
                value: 42,
                parent: parent,
                field: null!,
                renderContext: renderContext,
                projectionRole: null,
                densityLevel: DensityLevel.Comfortable,
                isReadOnly: false,
                isDevMode: false,
                renderDefault: null));

        Should.Throw<ArgumentNullException>(() =>
            new FieldSlotContext<ProjectionSlotProjection, int>(
                value: 42,
                parent: parent,
                field: field,
                renderContext: null!,
                projectionRole: null,
                densityLevel: DensityLevel.Comfortable,
                isReadOnly: false,
                isDevMode: false,
                renderDefault: null));
    }

    [Fact]
    public void ProjectionSlotDescriptor_StructuralEquality() {
        ProjectionSlotDescriptor a = new(
            ProjectionType: typeof(ProjectionSlotProjection),
            FieldName: "Priority",
            FieldType: typeof(int),
            Role: null,
            ComponentType: typeof(ValidPrioritySlot),
            ContractVersion: ProjectionSlotContractVersion.Current);
        ProjectionSlotDescriptor b = new(
            ProjectionType: typeof(ProjectionSlotProjection),
            FieldName: "Priority",
            FieldType: typeof(int),
            Role: null,
            ComponentType: typeof(ValidPrioritySlot),
            ContractVersion: ProjectionSlotContractVersion.Current);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void SlotSelector_AcceptsDirectPropertyAccess() {
        ProjectionSlotSelector.Parse<ProjectionSlotProjection, int>(x => x.Priority)
            .ShouldBe(new ProjectionSlotFieldIdentity("Priority", typeof(int)));
    }

    [Fact]
    public void SlotSelector_AcceptsNullableDirectPropertyAccess() {
        ProjectionSlotSelector.Parse<ProjectionSlotWithNullable, int?>(x => x.Priority)
            .ShouldBe(new ProjectionSlotFieldIdentity("Priority", typeof(int?)));
    }

    [Theory]
    [MemberData(nameof(InvalidSelectors))]
    public void SlotSelector_RejectsNonDirectPropertyExpressions(
        Expression<Func<ProjectionSlotProjection, object?>> selector) {
        ProjectionSlotSelectorException ex = Should.Throw<ProjectionSlotSelectorException>(() =>
            ProjectionSlotSelector.Parse(selector));

        ex.Message.ShouldContain("HFC1038");
        ex.Message.ShouldContain("Expected");
        ex.Message.ShouldContain("Fix");
        ex.Message.ShouldContain("Docs");
    }

    public static TheoryData<Expression<Func<ProjectionSlotProjection, object?>>> InvalidSelectors()
        => new() {
            x => x.Name.Length,
            x => x.Name.ToString(),
            x => x.Priority + 1,
            x => CapturedField,
            x => x.Tags[0],
        };

    private static readonly int CapturedField = 7;

    public sealed record ProjectionSlotProjection(int Priority, string Name) {
        public string[] Tags { get; init; } = [];
    }

    public sealed record ProjectionSlotWithNullable(int? Priority);

    public sealed class ValidPrioritySlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<ProjectionSlotProjection, int> Context { get; set; } = default!;
    }
}
