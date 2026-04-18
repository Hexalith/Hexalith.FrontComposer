using Fluxor;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Density;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Density;

/// <summary>
/// Story 3-1 Task 10.6 (D8 / ADR-029) — fail-closed tenant/user context tests for
/// <see cref="DensityEffects"/>. Mirrors <c>ThemeEffectsScopeTests</c> (symmetric refactor).
/// </summary>
public sealed class DensityEffectsScopeTests {
    [Fact]
    public async Task HandleDensityChanged_NullTenant_SkipsPersistenceAndLogsHFC2105() {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId: null, userId: "alice");
        var sut = new DensityEffects(storage, accessor, logger);

        await sut.HandleDensityChanged(new DensityChangedAction("c1", DensityLevel.Compact), Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(DensityLevel), Arg.Any<CancellationToken>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Fact]
    public async Task HandleDensityChanged_NullUser_SkipsPersistenceAndLogsHFC2105() {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId: "acme", userId: null);
        var sut = new DensityEffects(storage, accessor, logger);

        await sut.HandleDensityChanged(new DensityChangedAction("c1", DensityLevel.Compact), Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(DensityLevel), Arg.Any<CancellationToken>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Theory]
    [InlineData("   ", "alice")]
    [InlineData("acme", " ")]
    public async Task HandleDensityChanged_WhitespaceSegment_SkipsPersistence(string tenantId, string userId) {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId, userId);
        var sut = new DensityEffects(storage, accessor, logger);

        await sut.HandleDensityChanged(new DensityChangedAction("c1", DensityLevel.Compact), Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(DensityLevel), Arg.Any<CancellationToken>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Fact]
    public async Task HandleDensityChanged_ValidTenantAndUser_PersistsWithScopedKey() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        var storage = new InMemoryStorageService();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        var sut = new DensityEffects(storage, accessor, logger);

        await sut.HandleDensityChanged(new DensityChangedAction("c1", DensityLevel.Compact), Substitute.For<IDispatcher>());

        string expectedKey = StorageKeys.BuildKey("acme", "alice", "density");
        (await storage.GetAsync<DensityLevel>(expectedKey, ct)).ShouldBe(DensityLevel.Compact);
    }

    [Fact]
    public async Task HandleAppInitialized_NullContext_NoDispatchNoPersistenceRead() {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId: null, userId: null);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new DensityEffects(storage, accessor, logger);

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
        _ = storage.DidNotReceiveWithAnyArgs().GetAsync<DensityLevel?>(default!, Arg.Any<CancellationToken>());
    }

    private static IUserContextAccessor MakeAccessor(string? tenantId, string? userId) {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(tenantId);
        accessor.UserId.Returns(userId);
        return accessor;
    }

    private static void AssertLoggedInformation(ILogger<DensityEffects> logger, string diagnosticId) {
        bool found = false;
        foreach (NSubstitute.Core.ICall call in logger.ReceivedCalls()) {
            if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal)) {
                continue;
            }

            object?[] args = call.GetArguments();
            bool isInformation = args.Any(a => a is LogLevel lvl && lvl == LogLevel.Information);
            bool mentionsId = args.Any(a => a is not null && a.ToString()?.Contains(diagnosticId, StringComparison.Ordinal) == true);
            if (isInformation && mentionsId) {
                found = true;
                break;
            }
        }

        found.ShouldBeTrue($"Expected ILogger.Log call with LogLevel.Information referencing '{diagnosticId}'.");
    }
}
