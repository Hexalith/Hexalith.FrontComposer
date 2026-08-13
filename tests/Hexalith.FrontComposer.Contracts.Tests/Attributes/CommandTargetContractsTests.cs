using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Attributes;

public sealed class CommandTargetContractsTests {
    [Fact]
    public void CommandTargetAttribute_ExposesApprovedDeclarationShape() {
        var attribute = new CommandTargetAttribute(
            typeof(TargetProjection),
            CommandTargetResolutionMode.Provider,
            CommandTargetChangeKind.StatusMove) {
            ViewKey = "approved-orders",
            ExpectedStatus = "Approved",
        };

        attribute.ProjectionType.ShouldBe(typeof(TargetProjection));
        attribute.ResolutionMode.ShouldBe(CommandTargetResolutionMode.Provider);
        attribute.ChangeKind.ShouldBe(CommandTargetChangeKind.StatusMove);
        attribute.ViewKey.ShouldBe("approved-orders");
        attribute.ExpectedStatus.ShouldBe("Approved");

        AttributeUsageAttribute usage = typeof(CommandTargetAttribute).GetCustomAttribute<AttributeUsageAttribute>().ShouldNotBeNull();
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeFalse();
    }

    [Fact]
    public async Task TypedProvider_ReturnsOnlyTargetIntent() {
        ICommandTargetIdentityProvider<TestCommand> provider = new TestProvider();

        CommandTargetIdentity? identity = await provider.ResolveAsync(
            new TestCommand(),
            TestContext.Current.CancellationToken);

        identity.ShouldBe(new CommandTargetIdentity("orders", "order-1", "Draft", "Approved"));
    }

    [Fact]
    public void CommandMateriality_DefaultIsFailClosedAndNumericValuesAreStable() {
        default(CommandMateriality).ShouldBe(CommandMateriality.Unknown);
        ((int)CommandMateriality.Unknown).ShouldBe(0);
        ((int)CommandMateriality.Material).ShouldBe(1);
        ((int)CommandMateriality.NoOp).ShouldBe(2);
    }

    [Projection]
    private sealed class TargetProjection;

    private sealed class TestCommand;

    private sealed class TestProvider : ICommandTargetIdentityProvider<TestCommand> {
        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            TestCommand command,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<CommandTargetIdentity?>(new("orders", "order-1", "Draft", "Approved"));
    }
}
