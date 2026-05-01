using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.DevMode;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.DevMode;

public sealed class ComponentTreeNodeTests {
    [Fact]
    public void CustomizationLevel_PreservesPrecedenceOrdering() {
        ((int)CustomizationLevel.Default).ShouldBeLessThan((int)CustomizationLevel.Level1);
        ((int)CustomizationLevel.Level1).ShouldBeLessThan((int)CustomizationLevel.Level2);
        ((int)CustomizationLevel.Level2).ShouldBeLessThan((int)CustomizationLevel.Level3);
        ((int)CustomizationLevel.Level3).ShouldBeLessThan((int)CustomizationLevel.Level4);
    }

    [Fact]
    public void ComponentTreeNode_CarriesImmutableDevModeSnapshotMetadata() {
        ComponentTreeNode child = CreateNode("counter-name", CustomizationLevel.Level1);
        ComponentTreeNode node = CreateNode(
            "counter-grid",
            CustomizationLevel.Level4,
            children: [child],
            role: ProjectionRole.ActionQueue,
            fieldAccessor: "Name");

        node.AnnotationKey.ShouldBe("counter-grid");
        node.Convention.Name.ShouldBe("Generated DataGrid");
        node.ContractTypeName.ShouldBe("Counter.Contracts.CounterProjection");
        node.CurrentLevel.ShouldBe(CustomizationLevel.Level4);
        node.OriginatingProjectionTypeName.ShouldBe("Counter.Contracts.CounterProjection");
        node.Role.ShouldBe(ProjectionRole.ActionQueue);
        node.FieldAccessor.ShouldBe("Name");
        node.Children.ShouldHaveSingleItem().AnnotationKey.ShouldBe("counter-name");
        node.RenderEpoch.ShouldBe(42);
        node.ComponentTreeContractVersion.ShouldBe(ComponentTreeContractVersion.Current);
        node.DescriptorHash.ShouldBe("sha256:1234");
        node.SourceComponentIdentity.ShouldBe("CounterProjection.Default");
        node.IsStale.ShouldBeFalse();
        node.CanEmitStarterTemplate.ShouldBeTrue();
    }

    [Fact]
    public void ComponentTreeNode_SuppressesStarterTemplateCopyForStaleMetadata() {
        ComponentTreeNode node = CreateNode(
            "counter-grid",
            CustomizationLevel.Level3,
            staleReasons: [ComponentTreeStaleReason.DescriptorHashMismatch, ComponentTreeStaleReason.ContractVersionMismatch]);

        node.IsStale.ShouldBeTrue();
        node.CanEmitStarterTemplate.ShouldBeFalse();
        node.StaleReasons.ShouldContain(ComponentTreeStaleReason.DescriptorHashMismatch);
        node.StaleReasons.ShouldContain(ComponentTreeStaleReason.ContractVersionMismatch);
    }

    [Fact]
    public void ComponentTreeNode_CreatesFreshnessReasonsFromCurrentMetadata() {
        ComponentTreeNode node = CreateNode(
            "counter-grid",
            CustomizationLevel.Level2,
            componentTreeContractVersion: ComponentTreeContractVersion.Current - 1,
            descriptorHash: "sha256:old",
            sourceComponentIdentity: "CounterProjection.Old");

        ImmutableArray<ComponentTreeStaleReason> reasons = node.DetectStaleReasons(
            ComponentTreeContractVersion.Current,
            "sha256:new",
            "CounterProjection.New",
            generatedContractVersion: 1,
            runningContractVersion: 2);

        reasons.ShouldBe([
            ComponentTreeStaleReason.ContractVersionMismatch,
            ComponentTreeStaleReason.DescriptorHashMismatch,
            ComponentTreeStaleReason.SourceComponentIdentityMismatch,
            ComponentTreeStaleReason.GeneratedRunningContractDrift,
        ]);
    }

    [Fact]
    public void ConventionDescriptor_IsValueObjectAndRejectsBlankName() {
        ConventionDescriptor descriptor = new(
            "RelativeTime",
            "Relative time formatting",
            "Level 1 annotation",
            CustomizationLevel.Level1);

        descriptor.Name.ShouldBe("RelativeTime");
        descriptor.Description.ShouldBe("Relative time formatting");
        descriptor.RecommendedOverrideLevel.ShouldBe(CustomizationLevel.Level1);

        Should.Throw<ArgumentException>(() => new ConventionDescriptor(" ", "Description", "Recommendation", CustomizationLevel.Level2));
    }

    [Fact]
    public void ComponentTreeContractVersion_IsPackedVersion() {
        ComponentTreeContractVersion.Current.ShouldBe(
            (ComponentTreeContractVersion.Major * 1_000_000)
            + (ComponentTreeContractVersion.Minor * 1_000)
            + ComponentTreeContractVersion.Build);
    }

    private static ComponentTreeNode CreateNode(
        string annotationKey,
        CustomizationLevel level,
        ImmutableArray<ComponentTreeNode> children = default,
        ProjectionRole? role = null,
        string? fieldAccessor = null,
        ImmutableArray<ComponentTreeStaleReason> staleReasons = default,
        int componentTreeContractVersion = ComponentTreeContractVersion.Current,
        string descriptorHash = "sha256:1234",
        string sourceComponentIdentity = "CounterProjection.Default")
        => new(
            AnnotationKey: annotationKey,
            Convention: new ConventionDescriptor("Generated DataGrid", "Generated projection grid.", "Use the lowest viable override.", level),
            ContractTypeName: "Counter.Contracts.CounterProjection",
            CurrentLevel: level,
            OriginatingProjectionTypeName: "Counter.Contracts.CounterProjection",
            Role: role,
            FieldAccessor: fieldAccessor,
            Children: children,
            RenderEpoch: 42,
            ComponentTreeContractVersion: componentTreeContractVersion,
            DescriptorHash: descriptorHash,
            SourceComponentIdentity: sourceComponentIdentity,
            StaleReasons: staleReasons);
}
