using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Components.Badges;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Badges;

public sealed class FcDesaturatedBadgeTests : LayoutComponentTestBase {
    public FcDesaturatedBadgeTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        EnsureStoreInitialized();
    }

    [Fact]
    public void Confirming_RendersTargetTextAndVisibleStateLabel() {
        IRenderedComponent<FcDesaturatedBadge> cut = Render<FcDesaturatedBadge>(parameters => parameters
            .Add(p => p.PriorSlot, BadgeSlot.Neutral)
            .Add(p => p.PriorLabel, "Draft")
            .Add(p => p.OptimisticSlot, BadgeSlot.Success)
            .Add(p => p.OptimisticLabel, "Approved")
            .Add(p => p.State, OptimisticBadgeState.Confirming)
            .Add(p => p.ColumnHeader, "Status"));

        cut.Markup.ShouldContain("Confirming");
        cut.Markup.ShouldContain("Approved");
        cut.Markup.ShouldContain("fc-desaturated-badge--confirming");
        cut.Markup.ShouldContain("aria-label=\"Status: Confirming Approved\"");
    }

    [Fact]
    public void Rejected_RollsBackToPriorTextWithoutConfirmingAriaCopy() {
        IRenderedComponent<FcDesaturatedBadge> cut = Render<FcDesaturatedBadge>(parameters => parameters
            .Add(p => p.PriorSlot, BadgeSlot.Warning)
            .Add(p => p.PriorLabel, "Draft")
            .Add(p => p.OptimisticSlot, BadgeSlot.Success)
            .Add(p => p.OptimisticLabel, "Approved")
            .Add(p => p.State, OptimisticBadgeState.Rejected)
            .Add(p => p.ColumnHeader, "Status"));

        cut.Markup.ShouldContain("Rejected");
        cut.Markup.ShouldContain("Draft");
        cut.Markup.ShouldNotContain("Confirming Approved");
        cut.Markup.ShouldContain("aria-label=\"Status: Rejected Draft\"");
    }

    [Fact]
    public void IdempotentConfirmed_RendersAlreadyAppliedState() {
        IRenderedComponent<FcDesaturatedBadge> cut = Render<FcDesaturatedBadge>(parameters => parameters
            .Add(p => p.PriorSlot, BadgeSlot.Neutral)
            .Add(p => p.PriorLabel, "Draft")
            .Add(p => p.OptimisticSlot, BadgeSlot.Success)
            .Add(p => p.OptimisticLabel, "Approved")
            .Add(p => p.State, OptimisticBadgeState.IdempotentConfirmed));

        cut.Markup.ShouldContain("Already applied");
        cut.Markup.ShouldContain("Approved");
        cut.Markup.ShouldNotContain("fc-desaturated-badge--confirming");
    }
}
