using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Components.Badges;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Badges;

/// <summary>
/// Story 4-2 T4.2 (AC1, AC2, AC5, D1, D4, D12) — bUnit rendering tests for
/// <see cref="FcStatusBadge"/>. Covers per-slot visual resolution, aria-label construction
/// in English + French, and null / empty label edge cases.
/// </summary>
public sealed class FcStatusBadgeTests : LayoutComponentTestBase {
    public FcStatusBadgeTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        EnsureStoreInitialized();
    }

    [Theory]
    [InlineData(BadgeSlot.Neutral, "subtle", "tint")]
    [InlineData(BadgeSlot.Info, "informative", "tint")]
    [InlineData(BadgeSlot.Success, "success", "tint")]
    [InlineData(BadgeSlot.Warning, "warning", "tint")]
    [InlineData(BadgeSlot.Danger, "danger", "tint")]
    [InlineData(BadgeSlot.Accent, "brand", "filled")]
    public void ResolvesSlotToFluentColorAndAppearance(BadgeSlot slot, string expectedColor, string expectedAppearance) {
        IRenderedComponent<FcStatusBadge> cut = Render<FcStatusBadge>(parameters => parameters
            .Add(p => p.Slot, slot)
            .Add(p => p.Label, "Pending"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain($"color=\"{expectedColor}\"", Case.Insensitive);
            cut.Markup.ShouldContain($"appearance=\"{expectedAppearance}\"", Case.Insensitive);
            cut.Markup.ShouldContain($"data-fc-badge-slot=\"{slot}\"", Case.Insensitive);
            cut.Markup.ShouldContain("Pending");
        });
    }

    [Fact]
    public void AriaLabelCombinesColumnHeaderAndLabelInEnglish() {
        IRenderedComponent<FcStatusBadge> cut = Render<FcStatusBadge>(parameters => parameters
            .Add(p => p.Slot, BadgeSlot.Warning)
            .Add(p => p.Label, "Pending")
            .Add(p => p.ColumnHeader, "Status"));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("aria-label=\"Status: Pending\"", Case.Sensitive));
    }

    [Fact]
    public void AriaLabelUsesFrenchNonBreakingSpaceBeforeColon() {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("fr");
        CultureInfo.CurrentCulture = new CultureInfo("fr");
        try {
            IRenderedComponent<FcStatusBadge> cut = Render<FcStatusBadge>(parameters => parameters
                .Add(p => p.Slot, BadgeSlot.Warning)
                .Add(p => p.Label, "En attente")
                .Add(p => p.ColumnHeader, "Statut"));

            cut.WaitForAssertion(() => cut.Markup.ShouldContain("aria-label=\"Statut : En attente\"", Case.Sensitive));
        }
        finally {
            CultureInfo.CurrentUICulture = previous;
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void AriaLabelFallsBackToLabelWhenColumnHeaderIsNull() {
        IRenderedComponent<FcStatusBadge> cut = Render<FcStatusBadge>(parameters => parameters
            .Add(p => p.Slot, BadgeSlot.Success)
            .Add(p => p.Label, "Approved"));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("aria-label=\"Approved\"", Case.Sensitive));
    }

    [Fact]
    public void AriaLabelFallsBackToLabelWhenColumnHeaderIsEmpty() {
        IRenderedComponent<FcStatusBadge> cut = Render<FcStatusBadge>(parameters => parameters
            .Add(p => p.Slot, BadgeSlot.Success)
            .Add(p => p.Label, "Approved")
            .Add(p => p.ColumnHeader, string.Empty));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("aria-label=\"Approved\"", Case.Sensitive));
    }

    [Fact]
    public void RendersLabelAsVisibleTextContent() {
        IRenderedComponent<FcStatusBadge> cut = Render<FcStatusBadge>(parameters => parameters
            .Add(p => p.Slot, BadgeSlot.Danger)
            .Add(p => p.Label, "Rejected"));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Rejected"));
    }

    [Fact]
    public void EmptyLabelRendersEmptyChipWithoutCrashing() {
        IRenderedComponent<FcStatusBadge> cut = Render<FcStatusBadge>(parameters => parameters
            .Add(p => p.Slot, BadgeSlot.Neutral)
            .Add(p => p.Label, string.Empty));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("data-testid=\"fc-status-badge\"", Case.Insensitive));
    }

    [Fact]
    public void DoesNotExposeColorOrAppearanceParametersOnPublicApi() {
        // Story 4-2 D1 — semantic slot is the single knob; adopters MUST NOT set Color /
        // Appearance directly or the semantic-color contract (UX-DR24) breaks. If a future
        // refactor adds those parameters, this test surfaces it immediately.
        System.Reflection.PropertyInfo[] parameters = [
            ..typeof(FcStatusBadge).GetProperties(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
        ];

        parameters.ShouldContain(p => p.Name == nameof(FcStatusBadge.Slot));
        parameters.ShouldNotContain(p => p.Name == "Color");
        parameters.ShouldNotContain(p => p.Name == "Appearance");
    }
}
