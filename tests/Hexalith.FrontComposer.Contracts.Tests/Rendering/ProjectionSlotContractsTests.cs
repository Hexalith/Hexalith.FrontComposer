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
    private const int CapturedField = 7;

    [Fact]
    public void ContractVersion_Current_PacksMajorMinorBuild() {
        ProjectionSlotContractVersion.Current.ShouldBe(1_000_000);
        ProjectionSlotContractVersion.Major.ShouldBe(1);
        ProjectionSlotContractVersion.Minor.ShouldBe(0);
        ProjectionSlotContractVersion.Build.ShouldBe(0);
        ProjectionSlotContractVersion.Current
            .ShouldBe((ProjectionSlotContractVersion.Major * 1_000_000)
                + (ProjectionSlotContractVersion.Minor * 1_000)
                + ProjectionSlotContractVersion.Build);
    }

    [Fact]
    public void FieldSlotContext_ExposesSuppliedInputs() {
        RenderHints hints = new(BadgeSlot: BadgeSlot.Accent, IsSortable: false);
        FieldDescriptor field = new(
            Name: "Priority",
            TypeName: typeof(int).FullName!,
            IsNullable: false,
            DisplayName: "Priority",
            Format: "N0",
            Order: 1,
            IsReadOnly: true,
            Hints: hints,
            Description: "Order priority in the queue.");
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
        context.Field.Hints.ShouldBeSameAs(hints);
        context.Field.Description.ShouldBe("Order priority in the queue.");
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
    public void ProjectionSlotDescriptor_NullReferenceArguments_Throw() {
        Should.Throw<ArgumentNullException>(() =>
            new ProjectionSlotDescriptor(null!, "Priority", typeof(int), null, typeof(ValidPrioritySlot), 1));
        Should.Throw<ArgumentException>(() =>
            new ProjectionSlotDescriptor(typeof(ProjectionSlotProjection), "  ", typeof(int), null, typeof(ValidPrioritySlot), 1));
        Should.Throw<ArgumentNullException>(() =>
            new ProjectionSlotDescriptor(typeof(ProjectionSlotProjection), "Priority", null!, null, typeof(ValidPrioritySlot), 1));
        Should.Throw<ArgumentNullException>(() =>
            new ProjectionSlotDescriptor(typeof(ProjectionSlotProjection), "Priority", typeof(int), null, null!, 1));
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

    [Fact]
    public void SlotSelector_AcceptsObjectBoxingOfValueProperty() {
        ProjectionSlotSelector.Parse<ProjectionSlotProjection>(x => x.Priority)
            .ShouldBe(new ProjectionSlotFieldIdentity("Priority", typeof(int)));
    }

    [Fact]
    public void SlotSelector_AcceptsObjectBoxingOfNullableProperty() {
        ProjectionSlotSelector.Parse<ProjectionSlotWithNullable>(x => x.Priority)
            .ShouldBe(new ProjectionSlotFieldIdentity("Priority", typeof(int?)));
    }

    [Fact]
    public void SlotSelector_AcceptsLiftedNullableConversion() {
        ProjectionSlotSelector.Parse<ProjectionSlotProjection, int?>(x => x.Priority)
            .ShouldBe(new ProjectionSlotFieldIdentity("Priority", typeof(int)));
    }

    [Fact]
    public void SlotSelector_AcceptsInheritedProperty() {
        ProjectionSlotSelector.Parse<DerivedProjection, int>(x => x.Priority)
            .ShouldBe(new ProjectionSlotFieldIdentity("Priority", typeof(int)));
    }

    [Fact]
    public void SlotSelector_AcceptsExplicitInterfaceProperty() {
        ProjectionSlotSelector.Parse<ProjectionWithInterface, string>(x => x.Code)
            .ShouldBe(new ProjectionSlotFieldIdentity("Code", typeof(string)));
    }

    [Fact]
    public void SlotSelector_AcceptsShadowedPropertyAtMostDerived() {
        ProjectionSlotSelector.Parse<ShadowingProjection, string>(x => x.Code)
            .ShouldBe(new ProjectionSlotFieldIdentity("Code", typeof(string)));
    }

    [Theory]
    [MemberData(nameof(InvalidSelectorCases))]
    public void SlotSelector_RejectsNonDirectPropertyExpressions(
        string caseName,
        Expression<Func<ProjectionSlotProjection, object?>> selector) {
        ProjectionSlotSelectorException ex = Should.Throw<ProjectionSlotSelectorException>(() =>
            ProjectionSlotSelector.Parse(selector));

        ex.Message.ShouldContain("HFC1038", customMessage: $"case '{caseName}' must surface HFC1038");
        ex.Message.ShouldContain("Expected", customMessage: $"case '{caseName}' must teach the expected shape");
        ex.Message.ShouldContain("Fix", customMessage: $"case '{caseName}' must include a fix line");
        ex.Message.ShouldContain("Docs", customMessage: $"case '{caseName}' must include a docs link");
        ex.ParamName.ShouldBe("field", customMessage: $"case '{caseName}' must surface the canonical paramName");
    }

    public static TheoryData<string, Expression<Func<ProjectionSlotProjection, object?>>> InvalidSelectorCases() {
        TheoryData<string, Expression<Func<ProjectionSlotProjection, object?>>> data = new();
        data.Add("nested-member-access", x => x.Name.Length);
        data.Add("method-call-on-field", x => x.Name.ToString());
        data.Add("computed-expression", x => x.Priority + 1);
        data.Add("captured-static-field", _ => CapturedField);
        data.Add("indexer-on-collection", x => x.Tags[0]);
        return data;
    }

    public sealed record ProjectionSlotProjection(int Priority, string Name) {
        public string[] Tags { get; init; } = [];
    }

    public sealed record ProjectionSlotWithNullable(int? Priority);

    public abstract class BaseProjection {
        public int Priority { get; init; }
    }

    public sealed class DerivedProjection : BaseProjection {
        public string Name { get; init; } = string.Empty;
    }

    public interface IHasCode {
        string Code { get; }
    }

    public sealed class ProjectionWithInterface : IHasCode {
        public string Code { get; init; } = string.Empty;
    }

    public class ShadowingBaseProjection {
        public string Code { get; init; } = "base";
    }

    public sealed class ShadowingProjection : ShadowingBaseProjection {
        public new string Code { get; init; } = "derived";
    }

    public sealed class ValidPrioritySlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<ProjectionSlotProjection, int> Context { get; set; } = default!;
    }
}
