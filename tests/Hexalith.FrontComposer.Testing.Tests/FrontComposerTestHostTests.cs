using System.Globalization;

using Bunit;

using Counter.Domain;
using Counter.Web.Components.Replacements;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Testing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Testing.Tests;

public sealed class FrontComposerTestHostTests
{
    [Fact]
    public async Task FrontComposerTestBase_DefaultSetup_RegistersDeterministicServices()
    {
        using TestHost host = new();

        await host.InitializeAsync();

        host.Services.GetRequiredService<IStorageService>().ShouldBeOfType<InMemoryStorageService>();
        FrontComposerTestUserContextAccessor user = host.Services.GetRequiredService<FrontComposerTestUserContextAccessor>();
        user.TenantId.ShouldBe("tenant-a");
        user.UserId.ShouldBe("user-a");
        host.Services.GetRequiredService<ICommandService>().ShouldBeSameAs(host.ExposedCommandService);
    }

    [Fact]
    public async Task AddFrontComposerTestHost_CustomReplacementBeforeStoreInitialization_IsHonored()
    {
        await using BunitContext context = new();
        FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(
            context,
            options =>
            {
                options.TestTenantId = "tenant-b";
                options.TestUserId = "user-b";
            });

        FrontComposerTestUserContextAccessor replacement = new() { TenantId = "tenant-c", UserId = "user-c" };
        context.Services.Replace(ServiceDescriptor.Scoped(_ => replacement));
        context.Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ => replacement));

        IStore store = context.Services.GetRequiredService<IStore>();
        await store.InitializeAsync();

        context.Services.GetRequiredService<IUserContextAccessor>().TenantId.ShouldBe("tenant-c");
        host.CommandService.Evidence.ShouldBeEmpty();
    }

    [Fact]
    public async Task TestCommandService_Dispatch_CapturesRedactedEvidenceAndLifecycle()
    {
        using TestHost host = new();
        ICommandServiceWithLifecycle commandService = host.Services.GetRequiredService<ICommandServiceWithLifecycle>();
        SensitiveCommand command = new()
        {
            Tenant = "tenant-a",
            User = "user-a",
            Token = "secret-token",
            Amount = 42,
        };

        CommandResult result = await commandService
            .DispatchAsync(command, (_, _) => { }, Xunit.TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Status.ShouldBe("Accepted");
        CommandDispatchEvidence evidence = host.ExposedCommandService.Evidence.Single();
        evidence.TenantId.ShouldBe("tenant-a");
        evidence.UserId.ShouldBe("user-a");
        CommandEvidenceAssertions.AssertLifecycleContains(evidence, Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Confirmed);
        CommandEvidenceAssertions.AssertRedacted(evidence, "tenant-a", "user-a");
        evidence.RedactedPayload.ShouldNotContain("secret-token");
    }

    [Fact]
    public async Task TestProjectionPageLoader_ConfiguredPage_ReturnsEvidenceWithoutNetwork()
    {
        using TestHost host = new();
        TestProjectionPageLoader loader = host.Services.GetRequiredService<TestProjectionPageLoader>();
        loader.SucceedWith(typeof(CounterProjection).FullName!, [new CounterProjection { Id = "counter-1", Count = 7 }]);

        Hexalith.FrontComposer.Shell.State.DataGridNavigation.ProjectionPageResult result =
            await loader.LoadPageAsync(
                typeof(CounterProjection).FullName!,
                0,
                20,
                System.Collections.Immutable.ImmutableDictionary<string, string>.Empty,
                null,
                false,
                null,
                Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.TotalCount.ShouldBe(1);
        loader.Evidence.Single().ProjectionTypeFqn.ShouldBe(typeof(CounterProjection).FullName);
    }

    [Fact]
    public async Task QueryAndPageLoaderEvidence_MaxEvidenceRecords_IsBounded()
    {
        await using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(
            context,
            options => options.MaxEvidenceRecords = 2);

        for (int i = 0; i < 5; i++)
        {
            _ = await host.QueryService.QueryAsync<string>(
                new QueryRequest("String", null),
                Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
            _ = await host.PageLoader.LoadPageAsync(
                typeof(CounterProjection).FullName!,
                i,
                20,
                System.Collections.Immutable.ImmutableDictionary<string, string>.Empty,
                null,
                false,
                null,
                Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        host.QueryService.Evidence.Count.ShouldBe(2);
        host.PageLoader.Evidence.Count.ShouldBe(2);
        host.PageLoader.Evidence.Select(e => e.Skip).ShouldBe([3, 4]);
    }

    [Fact]
    public void FrontComposerTestHost_Dispose_RestoresAppliedCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUICulture = CultureInfo.CurrentUICulture;
        try
        {
            using BunitContext context = new();
            using (context.Services.AddFrontComposerTestHost(
                context,
                options => options.Culture = CultureInfo.GetCultureInfo("ja-JP")))
            {
                CultureInfo.CurrentCulture.Name.ShouldBe("ja-JP");
                CultureInfo.CurrentUICulture.Name.ShouldBe("ja-JP");
            }

            CultureInfo.CurrentCulture.ShouldBe(originalCulture);
            CultureInfo.CurrentUICulture.ShouldBe(originalUICulture);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [Fact]
    public async Task ParallelContexts_DifferentTenants_DoNotShareFakeEvidence()
    {
        static async Task<CommandDispatchEvidence> DispatchAsync(string tenant, string user, CancellationToken cancellationToken)
        {
            using TestHost host = new(tenant, user);
            ICommandService service = host.Services.GetRequiredService<ICommandService>();
            _ = await service.DispatchAsync(new SensitiveCommand { Tenant = tenant, User = user }, cancellationToken)
                .ConfigureAwait(true);
            return host.ExposedCommandService.Evidence.Single();
        }

        CommandDispatchEvidence[] evidence = await Task.WhenAll(
            DispatchAsync("tenant-1", "user-1", Xunit.TestContext.Current.CancellationToken),
            DispatchAsync("tenant-2", "user-2", Xunit.TestContext.Current.CancellationToken)).ConfigureAwait(true);

        evidence[0].TenantId.ShouldBe("tenant-1");
        evidence[1].TenantId.ShouldBe("tenant-2");
        evidence[0].MessageId.ShouldBe(evidence[1].MessageId);
    }

    [Fact]
    public async Task CounterProjectionView_WithCompositionHost_RendersDataGridAndViewOverride()
    {
        await using BunitContext context = new();
        FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        _ = host.AddDomainAssembly<CounterDomain>();
        context.Services.AddViewOverride<CounterProjection, CounterFullViewReplacement>();

        IStore store = context.Services.GetRequiredService<IStore>();
        await store.InitializeAsync();
        IDispatcher dispatcher = context.Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [new CounterProjection { Id = "counter-1", Count = 12, LastUpdated = DateTimeOffset.Parse("2026-04-14T00:00:00Z") }]));

        IRenderedComponent<CounterProjectionView> cut = context.Render<CounterProjectionView>();

        cut.WaitForAssertion(() =>
        {
            GeneratedProjectionAssertions.AssertDataGridEnvelope(cut, "Counter");
            GeneratedProjectionAssertions.AssertCellValues(cut, "counter-full-view-heading", "12");
        });
    }

    private sealed class TestHost : FrontComposerTestBase
    {
        public TestHost()
            : this("tenant-a", "user-a")
        {
        }

        public TestHost(string tenant, string user)
            : base(options =>
            {
                options.TestTenantId = tenant;
                options.TestUserId = user;
            })
        {
        }

        public TestCommandService ExposedCommandService => CommandService;

        public Task InitializeAsync() => InitializeStoreAsync();
    }

    private sealed class SensitiveCommand
    {
        public string? Tenant { get; init; }

        public string? User { get; init; }

        public string? Token { get; init; }

        public int Amount { get; init; }
    }
}
