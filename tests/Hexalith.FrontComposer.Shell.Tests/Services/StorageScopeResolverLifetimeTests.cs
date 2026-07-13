using System.Reflection;

using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

/// <summary>
/// Story 11.15 (ADR-030) — lock the Scoped lifetime of the consolidated
/// <see cref="IStorageScopeResolver"/> and the two <c>SnapshotPublisher&lt;T&gt;</c> owners
/// (<see cref="IProjectionConnectionState"/>, <see cref="IReconnectionReconciliationState"/>) so a
/// future Singleton back-slide fails CI. Distinct scopes must yield distinct per-circuit instances;
/// the same scope must yield one. Built with <c>ValidateScopes = true</c>.
/// </summary>
public sealed class StorageScopeResolverLifetimeTests {
    [Fact]
    public void IStorageScopeResolver_IsScoped_DistinctScopesYieldDistinctInstances() {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope1 = provider.CreateScope();
        using IServiceScope scope2 = provider.CreateScope();

        scope1.ServiceProvider.GetRequiredService<IStorageScopeResolver>()
            .ShouldNotBeSameAs(scope2.ServiceProvider.GetRequiredService<IStorageScopeResolver>());
    }

    [Fact]
    public void IStorageScopeResolver_IsScoped_SameScopeYieldsSameInstance() {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IStorageScopeResolver>()
            .ShouldBeSameAs(scope.ServiceProvider.GetRequiredService<IStorageScopeResolver>());
    }

    [Fact]
    public void PersistedFeatureEffects_ProductionResolution_UsesRegisteredScopedResolver() {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();
        IServiceProvider services = scope.ServiceProvider;
        IStorageScopeResolver expected = services.GetRequiredService<IStorageScopeResolver>();
        object[] effects = [
            services.GetRequiredService<ThemeEffects>(),
            services.GetRequiredService<DensityEffects>(),
            services.GetRequiredService<NavigationEffects>(),
            services.GetRequiredService<DataGridNavigationEffects>(),
            services.GetRequiredService<CapabilityDiscoveryEffects>(),
            services.GetRequiredService<CommandPaletteEffects>(),
        ];

        foreach (object effect in effects) {
            ReadScopeResolver(effect).ShouldBeSameAs(
                expected,
                $"{effect.GetType().Name} must consume the registered scoped resolver.");
        }
    }

    [Fact]
    public void PersistedFeatureEffects_PublicConstructors_RetainPublishedSixParameterSignatures() {
        Type[] effectTypes = [
            typeof(ThemeEffects),
            typeof(DensityEffects),
            typeof(DataGridNavigationEffects),
            typeof(CapabilityDiscoveryEffects),
        ];

        foreach (Type effectType in effectTypes) {
            ConstructorInfo constructor = effectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Single();
            constructor.GetParameters().Length.ShouldBe(
                6,
                $"{effectType.Name} must retain its pre-11.15 CLR constructor signature.");
            constructor.GetParameters().ShouldAllBe(parameter => parameter.ParameterType != typeof(IServiceProvider));
        }
    }

    [Fact]
    public void SnapshotPublisherOwners_AreScoped_DistinctScopesYieldDistinctInstances() {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope1 = provider.CreateScope();
        using IServiceScope scope2 = provider.CreateScope();

        scope1.ServiceProvider.GetRequiredService<IProjectionConnectionState>()
            .ShouldNotBeSameAs(scope2.ServiceProvider.GetRequiredService<IProjectionConnectionState>());
        scope1.ServiceProvider.GetRequiredService<IReconnectionReconciliationState>()
            .ShouldNotBeSameAs(scope2.ServiceProvider.GetRequiredService<IReconnectionReconciliationState>());
    }

    [Fact]
    public void SnapshotPublisherOwners_AreScoped_SameScopeYieldsSameInstance() {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IProjectionConnectionState>()
            .ShouldBeSameAs(scope.ServiceProvider.GetRequiredService<IProjectionConnectionState>());
        scope.ServiceProvider.GetRequiredService<IReconnectionReconciliationState>()
            .ShouldBeSameAs(scope.ServiceProvider.GetRequiredService<IReconnectionReconciliationState>());
    }

    private static ServiceProvider BuildProvider() {
        ServiceCollection services = [];
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer();
        services.Replace(ServiceDescriptor.Scoped<IStorageService>(_ => Substitute.For<IStorageService>()));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static IStorageScopeResolver ReadScopeResolver(object effect) {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo? field = effect.GetType().GetField("_scopeResolver", Flags);
        if (field?.GetValue(effect) is IStorageScopeResolver fieldValue) {
            return fieldValue;
        }

        PropertyInfo? property = effect.GetType().GetProperty("ScopeResolver", Flags);
        return property?.GetValue(effect) as IStorageScopeResolver
            ?? throw new InvalidOperationException($"{effect.GetType().Name} exposes no scope-resolver seam.");
    }
}
