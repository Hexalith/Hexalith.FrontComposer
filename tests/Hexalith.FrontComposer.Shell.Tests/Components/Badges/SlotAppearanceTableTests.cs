using System;
using System.Collections.Frozen;
using System.Collections.Generic;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Components.Badges;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Badges;

/// <summary>
/// Story 4-2 T4.1 (AC2, D2, D3) — exhaustiveness + fidelity tests for the frozen
/// <see cref="SlotAppearanceTable"/>. Guards against accidentally dropping a slot / mis-mapping
/// a color / changing the Accent appearance.
/// </summary>
public sealed class SlotAppearanceTableTests {
    [Fact]
    public void TableIsExhaustive() {
        BadgeSlot[] declared = Enum.GetValues<BadgeSlot>();
        FrozenDictionary<BadgeSlot, (BadgeColor Color, BadgeAppearance Appearance)> mapping = SlotAppearanceTable.Mapping;

        HashSet<BadgeSlot> tableKeys = [.. mapping.Keys];
        HashSet<BadgeSlot> declaredSet = [.. declared];
        tableKeys.SetEquals(declaredSet).ShouldBeTrue(
            $"SlotAppearanceTable must have an entry for every BadgeSlot. Missing: [{string.Join(", ", declaredSet.Except(tableKeys))}]; Extra: [{string.Join(", ", tableKeys.Except(declaredSet))}]");
    }

    [Fact]
    public void AccentMapsToBrandFilled() {
        (BadgeColor color, BadgeAppearance appearance) = SlotAppearanceTable.Resolve(BadgeSlot.Accent);

        color.ShouldBe(BadgeColor.Brand);
        appearance.ShouldBe(BadgeAppearance.Filled);
    }

    [Theory]
    [InlineData(BadgeSlot.Neutral, BadgeColor.Subtle)]
    [InlineData(BadgeSlot.Info, BadgeColor.Informative)]
    [InlineData(BadgeSlot.Success, BadgeColor.Success)]
    [InlineData(BadgeSlot.Warning, BadgeColor.Warning)]
    [InlineData(BadgeSlot.Danger, BadgeColor.Danger)]
    public void NonAccentSlotsUseTintAppearance(BadgeSlot slot, BadgeColor expectedColor) {
        (BadgeColor color, BadgeAppearance appearance) = SlotAppearanceTable.Resolve(slot);

        color.ShouldBe(expectedColor);
        appearance.ShouldBe(BadgeAppearance.Tint);
    }

    [Fact]
    public void TableDoesNotClaimReservedFluentColors() {
        foreach ((BadgeColor color, BadgeAppearance _) in SlotAppearanceTable.Mapping.Values) {
            color.ShouldNotBe(BadgeColor.Severe);
            color.ShouldNotBe(BadgeColor.Important);
        }
    }
}
