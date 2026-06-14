#pragma warning disable CA2007
using Bunit;

using Fluxor;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2-3 AC3 — end-to-end pin: a generated grid column for a <c>[ProjectionBadge]</c>-annotated
/// status enum renders through <c>FcStatusBadge</c> carrying the mandatory aria-label
/// <c>"{ColumnHeader}: {Label}"</c>. The emitted source is pinned by
/// <c>RoleSpecificProjectionApprovalTests</c> and the component aria-label by <c>FcStatusBadgeTests</c>;
/// this closes the runtime-render wiring between the two halves.
/// </summary>
public sealed class BadgeProjectionRenderTests : GeneratedComponentTestBase {
    public BadgeProjectionRenderTests()
        : base(typeof(BadgeProjection).Assembly) {
    }

    [Fact]
    public async Task BadgeColumn_RendersFcStatusBadge_WithAccessibleColumnHeaderAriaLabel() {
        using CultureScope _ = new("en");
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        dispatcher.Dispatch(new BadgeProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                new BadgeProjection { Name = "Alpha", Status = ReviewState.Pending },
                new BadgeProjection { Name = "Beta", Status = ReviewState.Approved },
            ]));

        IRenderedComponent<BadgeProjectionView> cut = Render<BadgeProjectionView>();

        await cut.WaitForAssertionAsync(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-status-badge\"");
            cut.Markup.ShouldContain("aria-label=\"Status: Pending\"");
            cut.Markup.ShouldContain("aria-label=\"Status: Approved\"");
            cut.Markup.ShouldContain("data-fc-badge-slot=\"Warning\"");
            cut.Markup.ShouldContain("data-fc-badge-slot=\"Success\"");
        });
    }
}
