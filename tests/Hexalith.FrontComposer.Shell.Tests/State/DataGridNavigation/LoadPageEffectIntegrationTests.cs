#pragma warning disable CA2007
#pragma warning disable xUnit1051
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Hexalith.FrontComposer.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 4-4 T4.5 / D3 — integration-flavoured tests for <see cref="LoadPageEffects"/> using a
/// substitute <see cref="IProjectionPageLoader"/>. Success, exception, and cancellation paths
/// each produce the expected terminal Fluxor action.
/// </summary>
public sealed class LoadPageEffectIntegrationTests {
    private const string ViewKey = "acme:OrdersProjection";

    private static LoadPageEffects MakeSut(
        IProjectionPageLoader loader,
        LoadedPageState? state = null,
        FcShellOptions? shellOptions = null) {
        IState<LoadedPageState> iState = Substitute.For<IState<LoadedPageState>>();
        iState.Value.Returns(state ?? new LoadedPageState());
        IOptionsMonitor<FcShellOptions> options = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        options.CurrentValue.Returns(shellOptions ?? new FcShellOptions());
        return new LoadPageEffects(iState, loader, NullLogger<LoadPageEffects>.Instance, options, new FakeTimeProvider());
    }

    private static LoadPageAction MakeAction(TaskCompletionSource<object> tcs, CancellationToken ct = default) =>
        new(ViewKey, 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null, tcs, ct);

    [Fact]
    public async Task HappyPath_DispatchesLoadPageSucceededWithLoaderItems() {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        IReadOnlyList<object> items = new object[] { "a", "b" };
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(new ProjectionPageResult(items, TotalCount: 42, ETag: null)));

        LoadPageEffects sut = MakeSut(loader);
        RecordingDispatcher dispatcher = new();
        TaskCompletionSource<object> tcs = new();

        await sut.HandleLoadPageAsync(MakeAction(tcs), dispatcher);

        LoadPageSucceededAction succeeded = dispatcher.Single<LoadPageSucceededAction>();
        succeeded.ViewKey.ShouldBe(ViewKey);
        succeeded.Skip.ShouldBe(0);
        succeeded.Items.ShouldBe(items);
        succeeded.TotalCount.ShouldBe(42);
    }

    [Fact]
    public async Task UnfilteredRequest_ClampsTakeAndReportedTotalToMaxUnfilteredItems() {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        IReadOnlyList<object> items = new object[] { "tail" };
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(new ProjectionPageResult(items, TotalCount: 1_000, ETag: null)));

        LoadPageEffects sut = MakeSut(loader, shellOptions: new FcShellOptions { MaxUnfilteredItems = 100 });
        RecordingDispatcher dispatcher = new();
        TaskCompletionSource<object> tcs = new();
        LoadPageAction action = new(ViewKey, 90, 50, ImmutableDictionary<string, string>.Empty, null, false, null, tcs, CancellationToken.None);

        await sut.HandleLoadPageAsync(action, dispatcher);

        _ = loader.Received(1).LoadPageAsync(
            Arg.Any<string>(),
            Arg.Is<int>(x => x == 90),
            Arg.Is<int>(x => x == 10),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
        LoadPageSucceededAction succeeded = dispatcher.Single<LoadPageSucceededAction>();
        succeeded.TotalCount.ShouldBe(100);
    }

    [Fact]
    public async Task UnfilteredRequestPastCap_CompletesWithEmptyPageWithoutCallingLoader() {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        LoadPageEffects sut = MakeSut(loader, shellOptions: new FcShellOptions { MaxUnfilteredItems = 100 });
        RecordingDispatcher dispatcher = new();
        TaskCompletionSource<object> tcs = new();
        LoadPageAction action = new(ViewKey, 100, 50, ImmutableDictionary<string, string>.Empty, null, false, null, tcs, CancellationToken.None);

        await sut.HandleLoadPageAsync(action, dispatcher);

        _ = loader.DidNotReceiveWithAnyArgs().LoadPageAsync(default!, default, default, default!, default, default, default, default);
        LoadPageSucceededAction succeeded = dispatcher.Single<LoadPageSucceededAction>();
        succeeded.Items.ShouldBeEmpty();
        succeeded.TotalCount.ShouldBe(100);
    }

    [Fact]
    public async Task ExceptionPath_DispatchesLoadPageFailedWithExceptionMessage() {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
        .Returns<Task<ProjectionPageResult>>(_ => throw new InvalidOperationException("loader boom"));

        // Store state shows the TCS is registered, so the defensive finally does NOT fire
        // (since the primary catch block already dispatched the terminal action).
        LoadedPageState stateAfter = new LoadedPageState { PendingCompletionsByKey = ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>>.Empty };
        LoadPageEffects sut = MakeSut(loader, stateAfter);
        RecordingDispatcher dispatcher = new();
        TaskCompletionSource<object> tcs = new();

        await sut.HandleLoadPageAsync(MakeAction(tcs), dispatcher);

        LoadPageFailedAction failed = dispatcher.Single<LoadPageFailedAction>();
        failed.ErrorMessage.ShouldBe("loader boom");
    }

    [Fact]
    public async Task CancellationPath_DispatchesLoadPageCancelled() {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        TaskCompletionSource<ProjectionPageResult> loaderCompletion = new();
        loader.LoadPageAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
        .Returns(loaderCompletion.Task);

        LoadPageEffects sut = MakeSut(loader);
        RecordingDispatcher dispatcher = new();
        using CancellationTokenSource cts = new();
        TaskCompletionSource<object> tcs = new();
        LoadPageAction action = MakeAction(tcs, cts.Token);

        Task effectTask = sut.HandleLoadPageAsync(action, dispatcher);
        cts.Cancel();
        loaderCompletion.TrySetCanceled(cts.Token);
        await effectTask;

        dispatcher.All<LoadPageCancelledAction>().ShouldNotBeEmpty();
    }
}
