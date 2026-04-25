using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-1 T5.4 / D22 — scope-guardrail test asserting the generated view body does
/// NOT include tokens owned by future stories. Each forbidden token has an explicit
/// owner-story in the failure message so scope creep fails fast with a clear redirect.
/// </summary>
public sealed class RazorEmitterScopeGuardrailTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);

    private static readonly (string Token, string OwningStory)[] ForbiddenTokens = [
        // Story 4-4 has now landed Virtualize / ItemsProvider / FcColumnPrioritizer (T2.1 / T2.2 / T2.3) — these
        // tokens are no longer scope creep and are intentionally part of the generated view body.
        ("FluentSearch", "Story 4.3 — DataGrid Filtering, Sorting & Search"),
        ("FcEmptyState", "Story 4.6 — Empty States (full CTA variant; only FcProjectionEmptyPlaceholder is allowed in 4-1)"),
        ("CaptureGridStateAction", "Story 3-6 / 4.3 — Filter/Sort state persistence is 4.3's scope"),
        ("HubConnection", "Epic 5 — SignalR"),
        ("FcStatusBadge", "Story 4.2 — Status Badge System"),
        ("FcFilterChip", "Story 4.3"),
    ];

    [Theory]
    [InlineData(ProjectionRenderStrategy.Default)]
    [InlineData(ProjectionRenderStrategy.ActionQueue)]
    [InlineData(ProjectionRenderStrategy.StatusOverview)]
    [InlineData(ProjectionRenderStrategy.DetailRecord)]
    [InlineData(ProjectionRenderStrategy.Timeline)]
    [InlineData(ProjectionRenderStrategy.Dashboard)]
    public void ProjectionViewBodyStaysMinimal(ProjectionRenderStrategy strategy) {
        RazorModel model = new RazorModel(
            "OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Id", "Id", TypeCategory.Text, null, false, _emptyBadges),
                new ColumnModel("Status", "Status", TypeCategory.Enum, "Humanize:30", false, _emptyBadges),
                new ColumnModel("Count", "Count", TypeCategory.Numeric, "N0", false, _emptyBadges))),
            strategy,
            new EquatableArray<string>(ImmutableArray<string>.Empty));

        string output = RazorEmitter.Emit(model);

        foreach ((string token, string owningStory) in ForbiddenTokens) {
            output.ShouldNotContain(
                token,
                customMessage: $"Scope creep detected: generated view body contains '{token}' which is owned by {owningStory}. "
                    + "4-1's scope guardrails forbid emitting this token. If the scope genuinely expanded, update "
                    + "RazorEmitterScopeGuardrailTests.ForbiddenTokens and document the decision.");
        }
    }
}
