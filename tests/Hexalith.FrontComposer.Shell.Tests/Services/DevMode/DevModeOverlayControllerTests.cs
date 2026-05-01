using Hexalith.FrontComposer.Contracts.DevMode;
using Hexalith.FrontComposer.Shell.Services.DevMode;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.DevMode;

public sealed class DevModeOverlayControllerTests {
    [Fact]
    public void ToggleOpenClose_IsIdempotentAndNotifiesSubscribers() {
        DevModeOverlayController controller = new();
        int changes = 0;
        controller.Changed += (_, _) => changes++;

        controller.IsActive.ShouldBeFalse();
        controller.Toggle();
        controller.IsActive.ShouldBeTrue();
        controller.Toggle();
        controller.IsActive.ShouldBeFalse();

        controller.Open("missing");
        controller.SelectedAnnotationKey.ShouldBeNull();
        controller.Close();
        controller.IsActive.ShouldBeFalse();
        changes.ShouldBe(2);
    }

    [Fact]
    public void RegisterOpenAndDispose_TrackCurrentEpochAnnotations() {
        DevModeOverlayController controller = new();
        ComponentTreeNode node = CreateNode("counter-name", 7);

        controller.Toggle();
        IDisposable registration = controller.Register(node);
        controller.Open("counter-name", renderEpoch: 7).ShouldBeTrue();

        controller.SelectedNode.ShouldBe(node);
        controller.SelectedAnnotationKey.ShouldBe("counter-name");

        registration.Dispose();
        controller.SelectedNode.ShouldBeNull();
        controller.SelectedAnnotationKey.ShouldBeNull();
    }

    [Fact]
    public void Open_IgnoresStaleEpoch() {
        DevModeOverlayController controller = new();
        controller.Toggle();
        _ = controller.Register(CreateNode("counter-name", 8));

        controller.Open("counter-name", renderEpoch: 7).ShouldBeFalse();

        controller.SelectedNode.ShouldBeNull();
    }

    private static ComponentTreeNode CreateNode(string key, long epoch)
        => new(
            AnnotationKey: key,
            Convention: new ConventionDescriptor("Generated field", "Field output.", "Use a slot override.", CustomizationLevel.Level3),
            ContractTypeName: "Counter.Contracts.CounterProjection",
            CurrentLevel: CustomizationLevel.Level3,
            OriginatingProjectionTypeName: "Counter.Contracts.CounterProjection",
            Role: null,
            FieldAccessor: "Name",
            Children: [],
            RenderEpoch: epoch,
            ComponentTreeContractVersion: ComponentTreeContractVersion.Current,
            DescriptorHash: "sha256:abc",
            SourceComponentIdentity: "CounterProjection.Name");
}
