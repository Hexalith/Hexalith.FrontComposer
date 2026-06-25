using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Components.Badges;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Badges;

#pragma warning disable CS0618 // Story 8.7 pins Color.Neutral and Color.Accent despite Fluent's newer aliases.

/// <summary>
/// Story 8.7 — generated projection status indicators render as colored icons with
/// contextual accessible names and tooltip labels, not visible FluentBadge pills.
/// </summary>
public sealed class FcStatusIconTests : LayoutComponentTestBase {
    public FcStatusIconTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        EnsureStoreInitialized();
    }

    [Theory]
    [InlineData(BadgeSlot.Neutral, Color.Neutral, "QuestionCircle")]
    [InlineData(BadgeSlot.Info, Color.Info, "InfoCircle")]
    [InlineData(BadgeSlot.Success, Color.Success, "CheckmarkCircle")]
    [InlineData(BadgeSlot.Warning, Color.Warning, "Warning")]
    [InlineData(BadgeSlot.Danger, Color.Error, "DismissCircle")]
    [InlineData(BadgeSlot.Accent, Color.Accent, "Star")]
    public void StatusIconTable_MapsEverySlotToExpectedFluentIconColorAndGlyph(BadgeSlot slot, Color expectedColor, string expectedIconName) {
        (Icon icon, Color color) = StatusIconTable.Resolve(slot);

        color.ShouldBe(expectedColor);
        icon.Name.ShouldBe(expectedIconName);
    }

    [Fact]
    public void StatusIconTable_IsExhaustive() {
        BadgeSlot[] declared = Enum.GetValues<BadgeSlot>();
        HashSet<BadgeSlot> tableKeys = [.. StatusIconTable.Mapping.Keys];
        HashSet<BadgeSlot> declaredSet = [.. declared];

        tableKeys.SetEquals(declaredSet).ShouldBeTrue(
            $"StatusIconTable must have an entry for every BadgeSlot. Missing: [{string.Join(", ", declaredSet.Except(tableKeys))}]; Extra: [{string.Join(", ", tableKeys.Except(declaredSet))}]");
    }

    [Theory]
    [InlineData(BadgeSlot.Neutral)]
    [InlineData(BadgeSlot.Info)]
    [InlineData(BadgeSlot.Success)]
    [InlineData(BadgeSlot.Warning)]
    [InlineData(BadgeSlot.Danger)]
    [InlineData(BadgeSlot.Accent)]
    public void RendersFocusableIconWithSlotColorAndNoBadgePill(BadgeSlot slot) {
        IRenderedComponent<FcStatusIcon> cut = Render<FcStatusIcon>(parameters => parameters
            .Add(p => p.Slot, slot)
            .Add(p => p.Label, "Pending")
            .Add(p => p.ColumnHeader, "Status"));

        cut.WaitForAssertion(() => {
            (Icon expectedIcon, Color expectedColor) = StatusIconTable.Resolve(slot);
            IRenderedComponent<FluentIcon<Icon>> fluentIcon = cut.FindComponent<FluentIcon<Icon>>();

            fluentIcon.Instance.Color.ShouldBe(expectedColor);
            fluentIcon.Instance.Value.Name.ShouldBe(expectedIcon.Name);
            cut.Markup.ShouldContain("data-testid=\"fc-status-icon\"", Case.Insensitive);
            cut.Markup.ShouldContain("tabindex=\"0\"", Case.Insensitive);
            cut.Markup.ShouldContain("role=\"img\"", Case.Insensitive);
            cut.Markup.ShouldContain($"data-fc-badge-slot=\"{slot}\"", Case.Insensitive);
            cut.Markup.ShouldNotContain("fluent-badge", Case.Insensitive);
            cut.Markup.ShouldNotContain("data-testid=\"fc-status-badge\"", Case.Insensitive);
        });
    }

    [Fact]
    public void AriaLabelCombinesColumnHeaderAndLabelInEnglish() {
        IRenderedComponent<FcStatusIcon> cut = Render<FcStatusIcon>(parameters => parameters
            .Add(p => p.Slot, BadgeSlot.Warning)
            .Add(p => p.Label, "Pending")
            .Add(p => p.ColumnHeader, "Status"));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("aria-label=\"Status: Pending\"", Case.Sensitive));
    }

    [Fact]
    public void AriaLabelUsesFrenchNonBreakingSpaceBeforeColon() {
        CultureInfo previousUi = CultureInfo.CurrentUICulture;
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentUICulture = new CultureInfo("fr");
        CultureInfo.CurrentCulture = new CultureInfo("fr");
        try {
            IRenderedComponent<FcStatusIcon> cut = Render<FcStatusIcon>(parameters => parameters
                .Add(p => p.Slot, BadgeSlot.Warning)
                .Add(p => p.Label, "En attente")
                .Add(p => p.ColumnHeader, "Statut"));

            cut.WaitForAssertion(() => cut.Markup.ShouldContain("aria-label=\"Statut : En attente\"", Case.Sensitive));
        }
        finally {
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void TooltipIsAnchoredToFocusableIconAndContainsLabel() {
        IRenderedComponent<FcStatusIcon> cut = Render<FcStatusIcon>(parameters => parameters
            .Add(p => p.Slot, BadgeSlot.Success)
            .Add(p => p.Label, "Approved")
            .Add(p => p.ColumnHeader, "Status"));

        cut.WaitForAssertion(() => {
            IRenderedComponent<FluentTooltip> tooltip = cut.FindComponent<FluentTooltip>();
            string anchorId = cut.Find("[data-testid='fc-status-icon']").Id!;

            tooltip.Instance.Anchor.ShouldBe(anchorId);
            tooltip.Instance.UseTooltipService.ShouldBeFalse();
            cut.Markup.ShouldContain("id=\"fc-status-icon-", Case.Insensitive);
            cut.Markup.ShouldContain("anchor=\"fc-status-icon-", Case.Insensitive);
            cut.Markup.ShouldContain("Approved");
        });
    }

    [Fact]
    public void AriaLabelFallsBackToLabelWhenColumnHeaderIsEmpty() {
        IRenderedComponent<FcStatusIcon> cut = Render<FcStatusIcon>(parameters => parameters
            .Add(p => p.Slot, BadgeSlot.Success)
            .Add(p => p.Label, "Approved")
            .Add(p => p.ColumnHeader, string.Empty));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("aria-label=\"Approved\"", Case.Sensitive));
    }
}

#pragma warning restore CS0618
