using Hexalith.FrontComposer.Contracts.Routing;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Routing;

public sealed class GeneratedCommandRouteTests {
    [Theory]
    [InlineData("Commerce", "SubmitOrderCommand", "/commands/Commerce/SubmitOrderCommand")]
    [InlineData("Commerce", "Commerce.Commands.SubmitOrderCommand", "/commands/Commerce/SubmitOrderCommand")]
    [InlineData(null, "SubmitOrderCommand", "/commands/Default/SubmitOrderCommand")]
    [InlineData("", "SubmitOrderCommand", "/commands/Default/SubmitOrderCommand")]
    [InlineData("Sales / West", "Commerce.Commands.Submit/Order Command", "/commands/Sales---West/Submit-Order-Command")]
    [InlineData("Mixed.Case_Context", "SubmitOrderCommand", "/commands/Mixed.Case_Context/SubmitOrderCommand")]
    public void Build_ValidModel_ProducesCanonicalRoute(
        string? boundedContext,
        string commandTypeName,
        string expected) {
        GeneratedCommandRoute.Build(boundedContext, commandTypeName).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Commerce.Commands.")]
    public void Build_InvalidCommandTypeName_Throws(string? commandTypeName) {
        Should.Throw<ArgumentException>(() => GeneratedCommandRoute.Build("Commerce", commandTypeName!));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..")]
    public void Build_UnsafeBoundedContext_Throws(string boundedContext) {
        Should.Throw<ArgumentException>(() => GeneratedCommandRoute.Build(boundedContext, "SubmitOrderCommand"));
    }
}
