using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 3-6 Task 7 — L03 fail-closed coverage for <c>DataGridNavigationEffects</c>. Null or
/// whitespace tenant / user → zero storage IO, logs <c>HFC2105</c>.
/// </summary>
public sealed class DataGridNavigationEffectsScopeTests {
    private const string ViewKey = "counter:X";

    private static GridViewSnapshot Snap() => new(
        scrollTop: 0,
        filters: ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.Ordinal),
        sortColumn: null,
        sortDescending: false,
        expandedRowId: null,
        selectedRowId: null,
        capturedAt: new DateTimeOffset(2026, 4, 22, 10, 0, 0, TimeSpan.Zero));

    private static IUserContextAccessor MakeAccessor(string? tenant, string? user) {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(tenant);
        accessor.UserId.Returns(user);
        return accessor;
    }

    private static IState<DataGridNavigationState> FakeState(GridViewSnapshot? snap = null) {
        ImmutableDictionary<string, GridViewSnapshot> views = snap is null
            ? ImmutableDictionary<string, GridViewSnapshot>.Empty
            : ImmutableDictionary<string, GridViewSnapshot>.Empty.Add(ViewKey, snap);
        IState<DataGridNavigationState> state = Substitute.For<IState<DataGridNavigationState>>();
        state.Value.Returns(new DataGridNavigationState(views, Cap: 50));
        return state;
    }

    private static void AssertLoggedInformation(ILogger<DataGridNavigationEffects> logger, string diagnosticId) {
        bool found = false;
        foreach (NSubstitute.Core.ICall call in logger.ReceivedCalls()) {
            if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal)) {
                continue;
            }

            object?[] args = call.GetArguments();
            if (args.Any(a => a is LogLevel lvl && lvl == LogLevel.Information)
                && args.Any(a => a is object[] state && state.Any(v => v is KeyValuePair<string, object?> kvp && kvp.Value is string s && s.Contains(diagnosticId, StringComparison.Ordinal)))) {
                found = true;
                break;
            }
        }

        // Fallback: scan formatted message if structured args didn't surface the ID.
        if (!found) {
            foreach (NSubstitute.Core.ICall call in logger.ReceivedCalls()) {
                if (call.GetArguments().OfType<object>().Any(o => o?.ToString()?.Contains(diagnosticId, StringComparison.Ordinal) == true)) {
                    found = true;
                    break;
                }
            }
        }

        found.ShouldBeTrue($"Expected a log at Information level mentioning {diagnosticId}");
    }

    [Fact]
    public async Task Hydrate_NullTenant_ShortCircuits() {
        var storage = new InMemoryStorageService();
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var sut = new DataGridNavigationEffects(storage, MakeAccessor(null, "alice"), logger, FakeState());

        await sut.HandleAppInitialized(
            new Hexalith.FrontComposer.Shell.State.AppInitializedAction("c1"),
            Substitute.For<IDispatcher>());

        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Fact]
    public async Task Hydrate_NullUser_ShortCircuits() {
        var storage = new InMemoryStorageService();
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var sut = new DataGridNavigationEffects(storage, MakeAccessor("acme", null), logger, FakeState());

        await sut.HandleAppInitialized(
            new Hexalith.FrontComposer.Shell.State.AppInitializedAction("c1"),
            Substitute.For<IDispatcher>());

        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Fact]
    public async Task Persist_WhitespaceTenant_DoesNotTouchStorage() {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var sut = new DataGridNavigationEffects(storage, MakeAccessor("  ", "alice"), logger, FakeState(Snap()));

        await sut.HandleCaptureGridState(
            new CaptureGridStateAction(ViewKey, Snap()),
            Substitute.For<IDispatcher>());

        await storage.DidNotReceiveWithAnyArgs().SetAsync<GridViewPersistenceBlob>(default!, default!, default!);
    }

    [Fact]
    public async Task Clear_NullUser_DoesNotTouchStorage() {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var sut = new DataGridNavigationEffects(storage, MakeAccessor("acme", null), logger, FakeState());

        await sut.HandleClearGridState(new ClearGridStateAction(ViewKey), Substitute.For<IDispatcher>());

        await storage.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default!);
    }
}
