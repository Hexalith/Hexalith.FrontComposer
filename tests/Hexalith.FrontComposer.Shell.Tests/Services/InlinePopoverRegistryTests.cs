using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class InlinePopoverRegistryTests {
    [Fact]
    public void Quickstart_RegistersRegistryAsScopedPerCircuit() {
        ServiceCollection services = [];
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposerQuickstart();

        ServiceDescriptor descriptor = services.Single(item => item.ServiceType == typeof(InlinePopoverRegistry));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();
        InlinePopoverRegistry first = firstScope.ServiceProvider.GetRequiredService<InlinePopoverRegistry>();
        InlinePopoverRegistry second = secondScope.ServiceProvider.GetRequiredService<InlinePopoverRegistry>();
        first.ShouldNotBeSameAs(second);
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Transient)]
    public void Quickstart_PreRegisteredNonScopedRegistry_Throws(ServiceLifetime lifetime) {
        IServiceCollection services = new ServiceCollection();
        services.Add(new ServiceDescriptor(typeof(InlinePopoverRegistry), typeof(InlinePopoverRegistry), lifetime));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => services.AddHexalithFrontComposerQuickstart());

        exception.Message.ShouldContain("must be registered as Scoped");
    }

    [Fact]
    public async Task OpenAsync_TwoPopovers_ClosesPreviousAndIgnoresStaleRelease() {
        InlinePopoverRegistry registry = new();
        IInlinePopover first = Substitute.For<IInlinePopover>();
        IInlinePopover second = Substitute.For<IInlinePopover>();
        IInlinePopover third = Substitute.For<IInlinePopover>();

        await registry.OpenAsync(first).ConfigureAwait(true);
        await registry.OpenAsync(second).ConfigureAwait(true);
        registry.Released(first);
        await registry.OpenAsync(third).ConfigureAwait(true);

        await first.Received(1).ClosePopoverAsync().ConfigureAwait(true);
        await second.Received(1).ClosePopoverAsync().ConfigureAwait(true);
    }

    [Fact]
    public async Task OpenAsync_PreviousCloseFails_NewPopoverStillOpens() {
        InlinePopoverRegistry registry = new();
        IInlinePopover first = Substitute.For<IInlinePopover>();
        IInlinePopover second = Substitute.For<IInlinePopover>();
        IInlinePopover third = Substitute.For<IInlinePopover>();
        first.ClosePopoverAsync().Returns(Task.FromException(new InvalidOperationException("stale")));

        await registry.OpenAsync(first).ConfigureAwait(true);
        await registry.OpenAsync(second).ConfigureAwait(true);
        await registry.OpenAsync(third).ConfigureAwait(true);

        await second.Received(1).ClosePopoverAsync().ConfigureAwait(true);
    }

    [Fact]
    public async Task OpenAsync_PreviousCloseIsCanceled_PropagatesCancellation() {
        InlinePopoverRegistry registry = new();
        IInlinePopover first = Substitute.For<IInlinePopover>();
        IInlinePopover second = Substitute.For<IInlinePopover>();
        first.ClosePopoverAsync().Returns(Task.FromCanceled(new CancellationToken(canceled: true)));

        await registry.OpenAsync(first).ConfigureAwait(true);

        _ = await Should.ThrowAsync<OperationCanceledException>(() => registry.OpenAsync(second)).ConfigureAwait(true);
    }
}
