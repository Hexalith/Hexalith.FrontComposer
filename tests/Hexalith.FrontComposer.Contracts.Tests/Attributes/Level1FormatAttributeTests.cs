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

    // ──────────────────────────────────────────────────────────────────────────
    //   Story 6-1 review F19 / T1 / T10 — story-owned regression for the
    //   pre-existing ColumnPriorityAttribute now reclassified as Level 1.
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void ColumnPriorityAttribute_AcceptsAnySignedIntPriority(int priority) {
        new ColumnPriorityAttribute(priority).Priority.ShouldBe(priority);
    }

    [Fact]
    public void ColumnPriorityAttribute_IsPropertyOnlyAndNotRepeatable() {
        AttributeUsageAttribute usage = typeof(ColumnPriorityAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.ShouldBe(AttributeTargets.Property);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeFalse();
    }
}
