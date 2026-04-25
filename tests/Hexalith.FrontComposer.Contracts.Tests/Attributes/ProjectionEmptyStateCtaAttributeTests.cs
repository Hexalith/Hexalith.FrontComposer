using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Attributes;

public sealed class ProjectionEmptyStateCtaAttributeTests {
    [Fact]
    public void Constructor_StoresCommandTypeName() {
        var attribute = new ProjectionEmptyStateCtaAttribute("IncrementCounterCommand");

        attribute.CommandTypeName.ShouldBe("IncrementCounterCommand");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyCommandTypeName(string value) {
        Should.Throw<ArgumentException>(() => new ProjectionEmptyStateCtaAttribute(value));
    }

    [Fact]
    public void AttributeUsage_AllowsClassAndStructOnly() {
        AttributeUsageAttribute usage = typeof(ProjectionEmptyStateCtaAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>()
            .ShouldNotBeNull();

        usage.ValidOn.ShouldBe(AttributeTargets.Class | AttributeTargets.Struct);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeFalse();
    }
}
