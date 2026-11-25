using System.Security.Claims;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Theme;
using Hexalith.FrontComposer.Shell.Tests.Generated;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State;

/// <summary>
/// Tests verifying DI registration via <see cref="ServiceCollectionExtensions.AddHexalithFrontComposer"/>.
/// </summary>
public class FluxorRegistrationTests {

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesAllStateTypes() {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        _ = provider.GetService<IState<FrontComposerThemeState>>().ShouldNotBeNull();
        _ = provider.GetService<IState<FrontComposerDensityState>>().ShouldNotBeNull();
    }

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesIDispatcher() {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        _ = provider.GetService<IDispatcher>().ShouldNotBeNull();
    }

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesIStorageService() {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        _ = provider.GetService<IStorageService>().ShouldNotBeNull();
        _ = provider.GetService<IStorageService>().ShouldBeOfType<InMemoryStorageService>();
    }

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesIStore() {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        _ = provider.GetService<IStore>().ShouldNotBeNull();
    }

    [Fact]
    public async Task PaletteFluxorSearchUsesTheAuthorizationEvaluatorFromItsOwnScopeAsync()
    {
        const string policy = "Orders.Approver";
        const string query = "Protected";
        string command = typeof(ProtectedTwoFieldCompactCommand).FullName!;
        ServiceCollection services = [];
        services.AddLogging();
        services.AddHexalithFrontComposer();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        services.AddScoped(_ => Substitute.For<IAuthorizationService>());
        services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider, MutableTestAuthenticationStateProvider>());
        services.Replace(ServiceDescriptor.Scoped(_ => Substitute.For<IFrontComposerTenantContextAccessor>()));

        await using ServiceProvider root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using AsyncServiceScope allowed = root.CreateAsyncScope();
        await using AsyncServiceScope denied = root.CreateAsyncScope();
        IServiceProvider allowedServices = allowed.ServiceProvider;
        IServiceProvider deniedServices = denied.ServiceProvider;
        allowedServices.GetRequiredService<IFrontComposerRegistry>().RegisterDomain(new DomainManifest(
            "Orders", "Orders", [], [command], new Dictionary<string, string>(StringComparer.Ordinal) { [command] = policy }));

        allowedServices.GetRequiredService<ICommandAuthorizationEvaluator>()
            .ShouldBeSameAs(allowedServices.GetRequiredService<ICommandAuthorizationEvaluator>());
        allowedServices.GetRequiredService<ICommandAuthorizationEvaluator>()
            .ShouldNotBeSameAs(deniedServices.GetRequiredService<ICommandAuthorizationEvaluator>());
        allowedServices.GetRequiredService<CommandPaletteEffects>()
            .ShouldNotBeSameAs(deniedServices.GetRequiredService<CommandPaletteEffects>());

        (await SearchAsync(allowedServices, "tenant-a", "user-a", AuthorizationResult.Success()))
            .ShouldContain(result => result.CommandTypeName == command);
        (await SearchAsync(deniedServices, "tenant-b", "user-b", AuthorizationResult.Failed()))
            .ShouldNotContain(result => result.CommandTypeName == command);

        static async Task<IReadOnlyList<PaletteResult>> SearchAsync(
            IServiceProvider scope, string tenantId, string userId, AuthorizationResult decision)
        {
            var provider = (MutableTestAuthenticationStateProvider)scope.GetRequiredService<AuthenticationStateProvider>();
            provider.Publish(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "Test")))));
            IFrontComposerTenantContextAccessor tenant = scope.GetRequiredService<IFrontComposerTenantContextAccessor>();
            tenant.TryGetContext(Arg.Any<string?>(), Arg.Any<string>()).Returns(
                TenantContextResult.Success(new TenantContextSnapshot(tenantId, userId, true, "correlation")));
            IAuthorizationService authorization = scope.GetRequiredService<IAuthorizationService>();
            authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), policy).Returns(decision);
            await scope.GetRequiredService<IStore>().InitializeAsync().ConfigureAwait(false);
            IDispatcher dispatcher = scope.GetRequiredService<IDispatcher>();
            IState<FrontComposerCommandPaletteState> state = scope.GetRequiredService<IState<FrontComposerCommandPaletteState>>();
            var completed = new TaskCompletionSource<IReadOnlyList<PaletteResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
            state.StateChanged += (_, _) => {
                if (state.Value is { Query: query, LoadState: PaletteLoadState.Ready } ready)
                {
                    completed.TrySetResult(ready.Results);
                }
            };
            dispatcher.Dispatch(new PaletteOpenedAction("opened"));
            dispatcher.Dispatch(new PaletteQueryChangedAction("searched", query));
            IReadOnlyList<PaletteResult> results = await completed.Task
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken).ConfigureAwait(false);
            await authorization.Received(1).AuthorizeAsync(
                Arg.Is<ClaimsPrincipal>(principal => principal.FindFirst(ClaimTypes.NameIdentifier)!.Value == userId),
                Arg.Is<object>(resource => ((CommandAuthorizationResource)resource).TenantContext!.TenantId == tenantId
                    && ((CommandAuthorizationResource)resource).TenantContext!.UserId == userId),
                policy).ConfigureAwait(false);
            return results;
        }
    }

    [Fact]
    public void Fluxor_AssemblyScan_NoDuplicateRegistration() {
        ServiceCollection services = [];

        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer();

        services.Count(d => d.ServiceType == typeof(IStore)).ShouldBe(1);
    }

    private static ServiceProvider BuildProvider() {
        ServiceCollection services = [];
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer();
        // LocalStorageService requires IJSRuntime; test hosts swap in InMemoryStorageService.
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        return services.BuildServiceProvider();
    }
}
