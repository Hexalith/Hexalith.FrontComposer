using Hexalith.FrontComposer.Contracts.Attributes;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Attributes;

/// <summary>
/// Story 4-5 T4.6 / D9 — construction-time guards on ProjectionFieldGroupAttribute.
/// </summary>
public sealed class ProjectionFieldGroupAttributeTests {
    [Fact]
    public void Constructor_AcceptsNonEmptyGroupName() {
        ProjectionFieldGroupAttribute attribute = new("Shipping");
        attribute.GroupName.ShouldBe("Shipping");
    }

    [Fact]
    public void Constructor_RejectsNullOrWhitespace() {
        Should.Throw<ArgumentException>(() => new ProjectionFieldGroupAttribute(null!));
        Should.Throw<ArgumentException>(() => new ProjectionFieldGroupAttribute(""));
        Should.Throw<ArgumentException>(() => new ProjectionFieldGroupAttribute("   "));
    }

    [Fact]
    public void AttributeUsage_TargetsPropertiesOnly_NotInherited_NotMultiple() {
        AttributeUsageAttribute? usage = typeof(ProjectionFieldGroupAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .SingleOrDefault();

        usage.ShouldNotBeNull();
        usage.ValidOn.ShouldBe(AttributeTargets.Property);
        usage.Inherited.ShouldBeFalse();
        usage.AllowMultiple.ShouldBeFalse();
    }
}
