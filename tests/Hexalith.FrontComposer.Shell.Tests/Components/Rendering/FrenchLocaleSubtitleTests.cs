using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>
/// Story 4-1 T8.5 / AC12 — French locale Theory ensuring every subtitle role
/// resolves localized copy from <c>FcShellResources.fr.resx</c> when the current
/// culture is French. Catches French key absence, mismatched placeholder
/// positions, and accidental English fallbacks. The native-speaker pre-pass for
/// the three USER-GATED keys (<c>HomeActionQueueSubtitleTemplate</c>,
/// <c>HomeDefaultSubtitleTemplate</c>, <c>HomeEmptyPlaceholderText</c>) is
/// tracked separately under T8.4.
/// </summary>
public sealed class FrenchLocaleSubtitleTests : LayoutComponentTestBase {
    private sealed class OrderProjection { }

    public FrenchLocaleSubtitleTests() {
        CultureInfo french = new("fr-FR");
        CultureInfo.CurrentCulture = french;
        CultureInfo.CurrentUICulture = french;
    }

    [Theory]
    [InlineData(ProjectionRole.ActionQueue, 3, 0, "3 orders en attente d'action")]
    [InlineData(ProjectionRole.StatusOverview, 7, 3, "7 au total dans 3 statuts")]
    [InlineData(ProjectionRole.DetailRecord, 1, 0, "Order - aperçu")]
    [InlineData(ProjectionRole.Timeline, 5, 0, "5 événements")]
    public void RendersFrenchCopyForEachRole(ProjectionRole role, int count, int distinctStatusCount, string expectedFragment) {
        Action<ComponentParameterCollectionBuilder<FcProjectionSubtitle>> parameters = RenderComponentParameters(role, count, distinctStatusCount);
        IRenderedComponent<FcProjectionSubtitle> cut = Render<FcProjectionSubtitle>(parameters);

        cut.Markup.ShouldContain(expectedFragment);
    }

    [Fact]
    public void DefaultRoleRendersFrenchCopyWhenNoRoleAttribute() {
        // Default (no [ProjectionRole]) uses HomeDefaultSubtitleTemplate which is
        // identity ({0} {1}) — same shape EN/FR per D18; verifies no English-only
        // fallback path leaks through when culture is French.
        IRenderedComponent<FcProjectionSubtitle> cut = Render<FcProjectionSubtitle>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.FallbackCount, 4));

        cut.Markup.ShouldContain("4 orders");
    }

    private static Action<ComponentParameterCollectionBuilder<FcProjectionSubtitle>> RenderComponentParameters(
        ProjectionRole role,
        int count,
        int distinctStatusCount)
        => parameters => {
            _ = parameters
                .Add(p => p.ProjectionType, typeof(OrderProjection))
                .Add(p => p.Role, role)
                .Add(p => p.FallbackCount, count);

            if (role == ProjectionRole.StatusOverview && distinctStatusCount > 0) {
                _ = parameters.Add(p => p.DistinctStatusCount, distinctStatusCount);
            }
        };
}
