using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Home;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

using Fluxor;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Components;

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

public sealed class ScopeTransitionShellTests : LayoutComponentTestBase {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidTenantSwitch_RearmsSessionRestoreWithoutOverridingNavigation(bool navigateAfterSwitch) {
        string tenant = "tenant-a";
        string user = "user-a";
        IUserContextAccessor identity = Substitute.For<IUserContextAccessor>();
        identity.TenantId.Returns(_ => tenant);
        identity.UserId.Returns(_ => user);
        Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ => identity));
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));
        EnsureStoreInitialized();

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p.AddChildContent("<p>Body</p>"));
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new LastActiveRouteHydratedAction("domain/counter/counter-view"));
        dispatcher.Dispatch(new NavigationHydratedCompletedAction());
        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/domain/counter/counter-view"));

        navigation.NavigateTo("/");
        tenant = "tenant-b";
        user = "user-b";
        Authorization.SetAuthorized("operator-b");
        cut.WaitForAssertion(() => cut.Find("#fc-main-content").TextContent.ShouldContain("Body"));
        if (navigateAfterSwitch) {
            navigation.NavigateTo("/settings");
        }

        dispatcher.Dispatch(new LastActiveRouteHydratedAction("domain/counter/counter-view"));
        dispatcher.Dispatch(new NavigationHydratedCompletedAction());
        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith(
            navigateAfterSwitch ? "/settings" : "/domain/counter/counter-view"));
    }

    [Fact]
    public void ValidTenantSwitch_RemountsStatefulChild_AndScopeLossBlocks() {
        string? tenant = "tenant-a";
        string? user = "user-a";
        IUserContextAccessor identity = Substitute.For<IUserContextAccessor>();
        identity.TenantId.Returns(_ => tenant);
        identity.UserId.Returns(_ => user);
        Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ => identity));
        EnsureStoreInitialized();

        int mounts = 0;
        RenderFragment child = builder => {
            builder.OpenComponent<MountProbe>(0);
            builder.AddAttribute(1, nameof(MountProbe.OnMount), (Action)(() => mounts++));
            builder.CloseComponent();
        };
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p.AddChildContent(child));
        cut.WaitForAssertion(() => mounts.ShouldBe(1));

        tenant = "tenant-b";
        user = "user-b";
        Authorization.SetAuthorized("operator-b");
        cut.WaitForAssertion(() => mounts.ShouldBe(2));

        tenant = null;
        Authorization.SetNotAuthorized();
        cut.WaitForAssertion(() => cut.Find("#fc-main-content [data-testid='fc-scope-blocked']"));
    }

    private sealed class MountProbe : ComponentBase {
        [Parameter]
        public Action? OnMount { get; set; }

        protected override void OnInitialized() => OnMount?.Invoke();
    }
}

public sealed class ThrowingScopeSurfaceTests : LayoutComponentTestBase {
    [Fact]
    public void Home_ThrowingTenantAccessor_RendersBlockedMeaning() {
        IFrontComposerTenantContextAccessor accessor = Substitute.For<IFrontComposerTenantContextAccessor>();
        accessor.TryGetContext(Arg.Any<string?>(), Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("unavailable"));
        Services.Replace(ServiceDescriptor.Scoped<IFrontComposerTenantContextAccessor>(_ => accessor));
        EnsureStoreInitialized();

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();
        cut.Find("[data-testid='fc-scope-blocked'] h1").TextContent.ShouldBe("Workspace unavailable");
        cut.Markup.ShouldNotContain("fc-home-directory");
    }
}
