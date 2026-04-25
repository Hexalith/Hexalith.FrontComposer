using System;

using Hexalith.FrontComposer.Contracts.Attributes;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Attributes;

/// <summary>
/// Story 4-4 T7.4 / D14 — contract tests for <see cref="ColumnPriorityAttribute"/>:
/// any 32-bit priority is accepted, property is readable, usage constraint targets properties only.
/// </summary>
public sealed class ColumnPriorityAttributeTests {
    [Fact]
    public void Constructor_AcceptsAnyIntAndExposesPriority() {
        new ColumnPriorityAttribute(0).Priority.ShouldBe(0);
        new ColumnPriorityAttribute(42).Priority.ShouldBe(42);
        new ColumnPriorityAttribute(int.MinValue).Priority.ShouldBe(int.MinValue);
        new ColumnPriorityAttribute(int.MaxValue).Priority.ShouldBe(int.MaxValue);
        new ColumnPriorityAttribute(-1).Priority.ShouldBe(-1);
    }

    [Fact]
    public void AttributeUsage_TargetsPropertiesOnly_NotInherited_SingleUse() {
        AttributeUsageAttribute? usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(ColumnPriorityAttribute), typeof(AttributeUsageAttribute));

        usage.ShouldNotBeNull();
        usage!.ValidOn.ShouldBe(AttributeTargets.Property);
        usage.Inherited.ShouldBeFalse();
        usage.AllowMultiple.ShouldBeFalse();
    }
}
