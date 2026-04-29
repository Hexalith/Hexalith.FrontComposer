using Hexalith.FrontComposer.Contracts.Attributes;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Attributes;

public sealed class Level1FormatAttributeTests {
    [Fact]
    public void RelativeTimeAttribute_DefaultsToSevenDayWindow() {
        var attribute = new RelativeTimeAttribute();

        attribute.RelativeWindowDays.ShouldBe(7);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    public void RelativeTimeAttribute_AcceptsBoundedWindow(int days) {
        new RelativeTimeAttribute(days).RelativeWindowDays.ShouldBe(days);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void RelativeTimeAttribute_RejectsOutOfRangeWindow(int days) {
        Should.Throw<ArgumentOutOfRangeException>(() => new RelativeTimeAttribute(days));
    }

    [Fact]
    public void RelativeTimeAttribute_IsPropertyOnlyAndNotRepeatable() {
        AttributeUsageAttribute usage = typeof(RelativeTimeAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.ShouldBe(AttributeTargets.Property);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeFalse();
    }

    [Fact]
    public void CurrencyAttribute_IsPropertyOnlyAndNotRepeatable() {
        AttributeUsageAttribute usage = typeof(CurrencyAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.ShouldBe(AttributeTargets.Property);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeFalse();
    }
}
