using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Attributes;

public sealed class RequiresPolicyAttributeTests {
    [Fact]
    public void Constructor_StoresTrimmedPolicyName() {
        var attribute = new RequiresPolicyAttribute(" OrderApprover ");

        attribute.PolicyName.ShouldBe("OrderApprover");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyPolicyName(string value) {
        Should.Throw<ArgumentException>(() => new RequiresPolicyAttribute(value));
    }

    [Theory]
    [InlineData("Order Approver")]
    [InlineData("Order/Approver")]
    [InlineData("Order*Approver")]
    [InlineData("Order!Approver")]
    public void Constructor_RejectsMalformedPolicyName(string value) {
        // Mirrors SourceTools HFC1056 well-formedness check so reflection consumers and
        // generator-driven consumers agree on acceptable policy names.
        Should.Throw<ArgumentException>(() => new RequiresPolicyAttribute(value));
    }

    [Fact]
    public void AttributeUsage_AllowsCommandClassOnly() {
        AttributeUsageAttribute usage = typeof(RequiresPolicyAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>()
            .ShouldNotBeNull();

        usage.ValidOn.ShouldBe(AttributeTargets.Class);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeFalse();
    }
}
