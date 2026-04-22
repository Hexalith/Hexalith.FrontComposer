using Fluxor;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Hexalith.FrontComposer.Shell.Tests.State.Theme;

/// <summary>
/// Story 3-1 Task 10.5 (D8 / AC3 / ADR-029) — fail-closed tenant/user context tests for
/// <see cref="ThemeEffects"/>. Null / empty / whitespace tenant or user MUST short-circuit
/// persistence and log <see cref="FcDiagnosticIds.HFC2105_StoragePersistenceSkipped"/>. Both
/// non-null AND non-whitespace → persist and use the tenant-scoped key.
/// </summary>
public sealed class ThemeEffectsScopeTests
{
    [Fact]
    public async Task HandleThemeChanged_NullTenant_SkipsPersistenceAndLogsHFC2105()
    {
        IStorageService storage = Substitute.For<IStorageService>();
        IThemeService themeService = Substitute.For<IThemeService>();
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId: null, userId: "alice");
        var sut = new ThemeEffects(storage, MsOptions.Create(new Hexalith.FrontComposer.Contracts.FcShellOptions()), accessor, logger, themeService);

        await sut.HandleThemeChanged(new ThemeChangedAction("c1", ThemeValue.Dark), Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(ThemeValue), Arg.Any<CancellationToken>());
        await themeService.Received(1)
            .SetThemeAsync(Arg.Is<ThemeSettings>(t => t.Mode == ThemeMode.Dark && t.Color == "#0097A7"));
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Fact]
    public async Task HandleThemeChanged_NullUser_SkipsPersistenceAndLogsHFC2105()
    {
        IStorageService storage = Substitute.For<IStorageService>();
        IThemeService themeService = Substitute.For<IThemeService>();
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId: "acme", userId: null);
        var sut = new ThemeEffects(storage, MsOptions.Create(new Hexalith.FrontComposer.Contracts.FcShellOptions()), accessor, logger, themeService);

        await sut.HandleThemeChanged(new ThemeChangedAction("c1", ThemeValue.Dark), Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(ThemeValue), Arg.Any<CancellationToken>());
        await themeService.Received(1)
            .SetThemeAsync(Arg.Is<ThemeSettings>(t => t.Mode == ThemeMode.Dark && t.Color == "#0097A7"));
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Theory]
    [InlineData("   ", "alice")]
    [InlineData("acme", " ")]
    public async Task HandleThemeChanged_WhitespaceSegment_SkipsPersistence(string tenantId, string userId)
    {
        IStorageService storage = Substitute.For<IStorageService>();
        IThemeService themeService = Substitute.For<IThemeService>();
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId, userId);
        var sut = new ThemeEffects(storage, MsOptions.Create(new Hexalith.FrontComposer.Contracts.FcShellOptions()), accessor, logger, themeService);

        await sut.HandleThemeChanged(new ThemeChangedAction("c1", ThemeValue.Dark), Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(ThemeValue), Arg.Any<CancellationToken>());
        await themeService.Received(1)
            .SetThemeAsync(Arg.Is<ThemeSettings>(t => t.Mode == ThemeMode.Dark && t.Color == "#0097A7"));
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Fact]
    public async Task HandleThemeChanged_ValidTenantAndUser_PersistsWithScopedKey()
    {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        var storage = new InMemoryStorageService();
        IThemeService themeService = Substitute.For<IThemeService>();
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        var sut = new ThemeEffects(storage, MsOptions.Create(new Hexalith.FrontComposer.Contracts.FcShellOptions()), accessor, logger, themeService);

        await sut.HandleThemeChanged(new ThemeChangedAction("c1", ThemeValue.Dark), Substitute.For<IDispatcher>());

        string expectedKey = StorageKeys.BuildKey("acme", "alice", "theme");
        (await storage.GetAsync<ThemeValue>(expectedKey, ct)).ShouldBe(ThemeValue.Dark);
        await themeService.Received(1)
            .SetThemeAsync(Arg.Is<ThemeSettings>(t => t.Mode == ThemeMode.Dark && t.Color == "#0097A7"));
    }

    [Fact]
    public async Task HandleAppInitialized_ValidContextEmptyStorage_LogsHFC2106AndDoesNotDispatchThemeChanged()
    {
        var storage = new InMemoryStorageService();
        IThemeService themeService = Substitute.For<IThemeService>();
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new ThemeEffects(storage, MsOptions.Create(new Hexalith.FrontComposer.Contracts.FcShellOptions()), accessor, logger, themeService);

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        // Story 3-6 D19 dispatches ThemeHydratingAction + ThemeHydratedCompletedAction on every
        // hydrate path (including empty-storage); only ThemeChangedAction should be suppressed
        // in this case.
        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(Arg.Any<ThemeChangedAction>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2106_ThemeHydrationEmpty);
    }

    private static IUserContextAccessor MakeAccessor(string? tenantId, string? userId)
    {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(tenantId);
        accessor.UserId.Returns(userId);
        return accessor;
    }

    private static void AssertLoggedInformation(ILogger<ThemeEffects> logger, string diagnosticId)
    {
        bool found = false;
        foreach (NSubstitute.Core.ICall call in logger.ReceivedCalls())
        {
            if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal))
            {
                continue;
            }

            object?[] args = call.GetArguments();
            bool isInformation = args.Any(a => a is LogLevel lvl && lvl == LogLevel.Information);
            bool mentionsId = args.Any(a => a is not null && a.ToString()?.Contains(diagnosticId, StringComparison.Ordinal) == true);
            if (isInformation && mentionsId)
            {
                found = true;
                break;
            }
        }

        found.ShouldBeTrue($"Expected ILogger.Log call with LogLevel.Information referencing '{diagnosticId}'.");
    }
}
