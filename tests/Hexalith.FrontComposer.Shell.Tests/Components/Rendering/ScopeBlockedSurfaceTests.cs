using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Home;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class ScopeBlockedSurfaceTests : LayoutComponentTestBase
{
    public ScopeBlockedSurfaceTests()
    {
        IUserContextAccessor identity = Substitute.For<IUserContextAccessor>();
        identity.TenantId.Returns((string?)null);
        identity.UserId.Returns((string?)null);
        Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ => identity));
        EnsureStoreInitialized();
    }

    [Fact]
    public void MissingScope_HomeShowsBlockInsteadOfEmptyDirectory()
    {
        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.Find("[data-testid='fc-scope-blocked'] h1").TextContent.ShouldBe("Workspace unavailable");
        cut.Markup.ShouldNotContain("fc-home-directory");
        cut.Markup.ShouldNotContain("fc-home-empty-no-microservices");
    }

    [Fact]
    public void MissingScope_ShellHidesChildContentAndShowsBlock()
    {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(parameters => parameters
            .AddChildContent("<p data-testid='prior-scope-content'>prior scope</p>"));

        cut.Find("#fc-main-content [data-testid='fc-scope-blocked'] h1").TextContent.ShouldBe("Workspace unavailable");
        cut.Markup.ShouldNotContain("prior-scope-content");
    }
}
