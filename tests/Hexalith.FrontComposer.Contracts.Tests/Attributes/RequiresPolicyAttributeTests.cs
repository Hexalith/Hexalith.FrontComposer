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
    public void Constructor_RejectsEmptyPolicyName(string value) => Should.Throw<ArgumentException>(() => new RequiresPolicyAttribute(value));

    [Theory]
    [InlineData("Order Approver")]
    [InlineData("Order/Approver")]
    [InlineData("Order*Approver")]
    [InlineData("Order!Approver")]
    public void Constructor_RejectsMalformedPolicyName(string value) =>
        // Mirrors SourceTools HFC1056 well-formedness check so reflection consumers and
        // generator-driven consumers agree on acceptable policy names.
        Should.Throw<ArgumentException>(() => new RequiresPolicyAttribute(value));

    /// <summary>DW-877 — letter-only names outside Normalization Form C would never match their composed registration.</summary>
    /// <param name="value">The decomposed policy name.</param>
    [Theory]
    [InlineData("\u212Aelvin.Approve")]
    [InlineData(" \u1112\u1161\u11AB.Approve ")]
    public void ConstructorRejectsAPolicyNameOutsideNormalizationFormC(string value) =>
        Should.Throw<ArgumentException>(() => new RequiresPolicyAttribute(value)).Message.ShouldContain("Normalization Form C");

    /// <summary>DW-877 — composed non-ASCII letters remain valid policy name characters.</summary>
    /// <param name="value">The composed policy name.</param>
    [Theory]
    [InlineData("Caf\u00E9.Approve")]
    [InlineData("\uD55C.Approve")]
    public void ConstructorAcceptsAComposedNonAsciiPolicyName(string value) =>
        new RequiresPolicyAttribute(value).PolicyName.ShouldBe(value);

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
