// ATDD RED PHASE — Story 3-3 Task 10.1 (D1, D5, D6, D7; AC1, AC4, AC5; ADR-039, ADR-040)
// Fails at compile until Task 1.2 (DensitySurface) + Task 1.3 (DensityPrecedence) land.

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Density;

/// <summary>
/// Story 3-3 Task 10.1 — pure-function precedence tests covering the four-tier rule
/// (ADR-039) and the tier-force override (ADR-040). Resolver order:
/// (1) tier ≤ Tablet → Comfortable; (2) user preference; (3) deployment default;
/// (4) factory hybrid by surface.
/// </summary>
public sealed class DensityPrecedenceTests
{
    [Theory]
    // (userPreference, deploymentDefault, surface, tier, expected)
    // Row 1 — no preferences, Default surface, Desktop → factory hybrid (Comfortable)
    [InlineData(null, null, DensitySurface.Default, ViewportTier.Desktop, DensityLevel.Comfortable)]
    // Row 2 — user pref wins over factory hybrid at Desktop
    [InlineData(DensityLevel.Compact, null, DensitySurface.Default, ViewportTier.Desktop, DensityLevel.Compact)]
    // Row 3 — deployment default wins over factory hybrid at Desktop (no user pref)
    [InlineData(null, DensityLevel.Roomy, DensitySurface.Default, ViewportTier.Desktop, DensityLevel.Roomy)]
    // Row 4 — DataGrid surface factory hybrid is Compact (UX spec §208-214)
    [InlineData(null, null, DensitySurface.DataGrid, ViewportTier.Desktop, DensityLevel.Compact)]
    // Row 5 — deployment default beats surface factory hybrid for NavigationSidebar
    [InlineData(null, DensityLevel.Compact, DensitySurface.NavigationSidebar, ViewportTier.Desktop, DensityLevel.Compact)]
    // Row 6 — tier-force override at Tablet supersedes user pref AND deployment default (ADR-040)
    [InlineData(DensityLevel.Compact, DensityLevel.Roomy, DensitySurface.Default, ViewportTier.Tablet, DensityLevel.Comfortable)]
    public void Resolve_AllCombinations(
        DensityLevel? userPreference,
        DensityLevel? deploymentDefault,
        DensitySurface surface,
        ViewportTier tier,
        DensityLevel expected)
    {
        DensityLevel actual = DensityPrecedence.Resolve(userPreference, deploymentDefault, surface, tier);

        actual.ShouldBe(expected);
    }
}
