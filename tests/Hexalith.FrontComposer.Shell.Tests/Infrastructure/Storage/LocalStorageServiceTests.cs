using System.Collections.Concurrent;
using System.Reflection;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Infrastructure.Storage;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Storage;

/// <summary>
/// Story 3-1 Task 10.4 (D9 / D15 / D16 / AC6) — core behaviour tests for
/// <see cref="LocalStorageService"/> using bUnit's JSInterop mock and <c>FakeTimeProvider</c>
/// for deterministic LRU ordering.
/// </summary>
public sealed class LocalStorageServiceTests {
    [Fact]
    public async Task GetAsync_MissingKey_ReturnsDefault() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out _);

        string? result = await sut.GetAsync<string?>("missing", ct);

        result.ShouldBeNull();
        GetTrackedKeyCount(sut).ShouldBe(0);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blazor", "BL0016", Justification = "Test-only mock configuration or controlled fixture completion, not a browser call; interop failures must remain observable by the service assertions.")]
    public async Task GetAsync_ExistingKey_DeserializesValue() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        js.StoredJson["theme"] = "\"Dark\"";

        string? result = await sut.GetAsync<string?>("theme", ct);

        result.ShouldBe("Dark");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blazor", "BL0016", Justification = "Test-only mock configuration or controlled fixture completion, not a browser call; interop failures must remain observable by the service assertions.")]
    public async Task GetAsync_UsesWebJsonOptionsForCamelCasePayload() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        js.StoredJson["payload"] = "{\"currentTheme\":\"Dark\"}";

        PersistedPayload? result = await sut.GetAsync<PersistedPayload>("payload", ct);

        result.ShouldBe(new PersistedPayload("Dark"));
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blazor", "BL0016", Justification = "Test-only mock configuration or controlled fixture completion, not a browser call; interop failures must remain observable by the service assertions.")]
    public async Task RemoveAsync_QueuesDeletionUntilFlush() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        TaskCompletionSource removeGate = js.RemoveGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task task = sut.RemoveAsync("theme", ct);
        task.IsCompleted.ShouldBeTrue();

        removeGate.TrySetResult();
        await sut.FlushAsync(ct);

        js.Invocations.ShouldContain(i => i.Identifier == "localStorage.removeItem");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blazor", "BL0016", Justification = "Test-only mock configuration or controlled fixture completion, not a browser call; interop failures must remain observable by the service assertions.")]
    public async Task SetAsync_FireAndForget_ReturnsImmediatelyThenDrainsToJs() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        TaskCompletionSource setGate = js.SetItemGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task task = sut.SetAsync("theme", "Dark", ct);
        task.IsCompleted.ShouldBeTrue();

        setGate.TrySetResult();
        await sut.FlushAsync(ct);

        js.Invocations.ShouldContain(i => i.Identifier == "localStorage.setItem");
    }

    [Fact]
    public async Task SetAsync_UsesCamelCaseAndOmitsDefaultValues() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);

        await sut.SetAsync("payload", new PersistedPayload("Dark"), ct);
        await sut.FlushAsync(ct);

        JsInvocation invocation = js.Invocations.Last(i => i.Identifier == "localStorage.setItem");
        invocation.Arguments[1].ShouldBe("{\"currentTheme\":\"Dark\"}");
    }

    [Fact]
    public async Task FlushAsync_DrainsPendingWrites() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);

        await sut.SetAsync("a", 1, ct);
        await sut.SetAsync("b", 2, ct);
        await sut.SetAsync("c", 3, ct);
        await sut.FlushAsync(ct);

        js.Invocations.Count(i => i.Identifier == "localStorage.setItem").ShouldBe(3);
    }

    [Fact]
    public async Task EvictIfOverCap_EvictsOldestEntryByTimestamp() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 18, 0, 0, 0, TimeSpan.Zero));
        await using LocalStorageService sut = CreateService(out TestJsRuntime js, maxEntries: 3, time: time);

        await sut.SetAsync("oldest", 1, ct);
        time.Advance(TimeSpan.FromSeconds(1));
        await sut.SetAsync("middle-a", 2, ct);
        time.Advance(TimeSpan.FromSeconds(1));
        await sut.SetAsync("middle-b", 3, ct);
        time.Advance(TimeSpan.FromSeconds(1));
        await sut.SetAsync("newest", 4, ct);
        await sut.FlushAsync(ct);

        js.Invocations.ShouldContain(i =>
            i.Identifier == "localStorage.removeItem"
            && i.Arguments.Length > 0
            && (string?)i.Arguments[0] == "oldest");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blazor", "BL0016", Justification = "Test-only mock configuration or controlled fixture completion, not a browser call; interop failures must remain observable by the service assertions.")]
    public async Task GetKeysAsync_FiltersByPrefix() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        js.EvalKeys = ["acme:u1:theme", "acme:u1:density", "other:u1:theme", "acme:u2:theme"];

        IReadOnlyList<string> keys = await sut.GetKeysAsync("acme:u1:", ct);

        keys.ShouldBe(["acme:u1:theme", "acme:u1:density"], ignoreOrder: true);
    }

    [Fact]
    public async Task SetAsync_EmptyKey_Throws() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out _);
        _ = await Should.ThrowAsync<ArgumentException>(async () => await sut.SetAsync("", 1, ct).ConfigureAwait(false));
    }

    [Fact]
    public async Task DisposeAsync_IsSafeToCallMultipleTimes() {
        LocalStorageService sut = CreateService(out _);
        await sut.DisposeAsync();
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task FlushAsync_ThrowsWhenEarlierDrainWriteFailed() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        js.SetItemException = new InvalidOperationException("boom");

        await sut.SetAsync("theme", "Dark", ct);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.FlushAsync(ct));
        ex.Message.ShouldContain("boom");
    }

    [Fact]
    public async Task FlushAsyncReportsInteropCancellationAndContinuesPersisting() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        js.SetItemException = new TaskCanceledException("Interop timed out");

        await sut.SetAsync("failed", 1, ct);
        await Should.ThrowAsync<TaskCanceledException>(
            () => sut.FlushAsync(ct).WaitAsync(TimeSpan.FromSeconds(5), ct));
        ct.IsCancellationRequested.ShouldBeFalse();

        js.SetItemException = null;
        await sut.SetAsync("next", 2, ct);
        await sut.FlushAsync(ct);
        js.StoredJson.ContainsKey("failed").ShouldBeFalse();
        js.StoredJson["next"].ShouldBe("2");
    }

    [Fact]
    public async Task FlushAsyncCancelledWaitLeavesWriteFailureForNextFlush() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(ct);
        TaskCompletionSource gate = js.SetItemGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        js.SetItemException = new JSException("Write failed");
        await sut.SetAsync("failed", 1, ct);

        Task abandoned = sut.FlushAsync(caller.Token);
        await caller.CancelAsync();
        gate.SetResult();
        await Should.ThrowAsync<TaskCanceledException>(() => abandoned);
        await Should.ThrowAsync<JSException>(() => sut.FlushAsync(ct));
        await sut.FlushAsync(ct);
    }

    [Fact]
    public async Task DisposeAsyncDrainsAcceptedWritesAndRacingFlushes() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        TaskCompletionSource gate = js.SetItemGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await sut.SetAsync("theme", "Dark", ct);

        Task firstFlush = sut.FlushAsync(ct);
        Task dispose = sut.DisposeAsync().AsTask();
        Task lateFlush = sut.FlushAsync(ct);
        lateFlush.IsCompleted.ShouldBeFalse();
        gate.SetResult();
        await Task.WhenAll(firstFlush, lateFlush, dispose).WaitAsync(TimeSpan.FromSeconds(5), ct);
        js.StoredJson["theme"].ShouldBe("\"Dark\"");
        await Should.ThrowAsync<ObjectDisposedException>(() => sut.SetAsync("late", 1, ct));
        await Should.ThrowAsync<ObjectDisposedException>(() => sut.RemoveAsync("theme", ct));
    }

    [Fact]
    public async Task DisposeAsyncCancelsStalledInteropAndSettlesAllFlushWaiters() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        FakeTimeProvider time = new();
        await using LocalStorageService sut = CreateService(out TestJsRuntime js, time: time);
        js.SetItemGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await sut.SetAsync("stalled", true, ct);
        await js.SetItemStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);

        Task acceptedFlush = sut.FlushAsync(ct);
        Task dispose = sut.DisposeAsync().AsTask();
        Task concurrentDispose = sut.DisposeAsync().AsTask();
        Task lateFlush = sut.FlushAsync(ct);
        time.Advance(TimeSpan.FromSeconds(6));
        await Task.WhenAll(dispose, concurrentDispose).WaitAsync(TimeSpan.FromSeconds(5), ct);
        await Should.ThrowAsync<TaskCanceledException>(() => acceptedFlush.WaitAsync(TimeSpan.FromSeconds(5), ct));
        await Should.ThrowAsync<TaskCanceledException>(() => lateFlush.WaitAsync(TimeSpan.FromSeconds(5), ct));
        js.StoredJson.ContainsKey("stalled").ShouldBeFalse();
    }

    private static LocalStorageService CreateService(
        out TestJsRuntime js,
        int maxEntries = 500,
        FakeTimeProvider? time = null) {
        js = new TestJsRuntime();
        FcShellOptions options = new() { LocalStorageMaxEntries = maxEntries };
        IOptions<FcShellOptions> optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);
        return new LocalStorageService(
            js,
            optionsWrapper,
            time ?? new FakeTimeProvider(),
            NullLogger<LocalStorageService>.Instance);
    }

    private static int GetTrackedKeyCount(LocalStorageService sut) {
        PropertyInfo? property = typeof(LocalStorageService).GetProperty(
            "TrackedKeyCount",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        property.ShouldNotBeNull();
        return (int)property.GetValue(sut)!;
    }

    private sealed class TestJsRuntime : IJSRuntime {
        private readonly ConcurrentDictionary<string, string?> _storedJson = new(StringComparer.Ordinal);
        private readonly ConcurrentQueue<JsInvocation> _invocations = new();

        public Exception? SetItemException { get; set; }

        public TaskCompletionSource SetItemStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource? SetItemGate { get; set; }

        public TaskCompletionSource? RemoveGate { get; set; }

        public IReadOnlyCollection<JsInvocation> Invocations => _invocations.ToArray();

        public string[] EvalKeys { get; set; } = [];

        public ConcurrentDictionary<string, string?> StoredJson => _storedJson;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Blazor", "BL0016", Justification = "Test-only JS fake forwarding; faults and cancellation must propagate unchanged so the component or service caller handles them under test.")]
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            try
            {
                return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
            }
            catch (Exception ex) when (ex is JSException or InvalidOperationException or OperationCanceledException)
            {
                return ValueTask.FromException<TValue>(ex);
            }
        }

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) {
            object?[] actualArgs = args ?? [];
            _invocations.Enqueue(new JsInvocation(identifier, actualArgs));

            switch (identifier) {
                case "localStorage.getItem":
                    string key = (string)actualArgs[0]!;
                    _ = _storedJson.TryGetValue(key, out string? json);
                    return (TValue)(object?)json!;

                case "localStorage.setItem":
                    _ = SetItemStarted.TrySetResult();
                    if (SetItemGate is not null) {
                        await SetItemGate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }

                    if (SetItemException is not null) {
                        throw SetItemException;
                    }

                    _storedJson[(string)actualArgs[0]!] = (string?)actualArgs[1];
                    return default!;

                case "localStorage.removeItem":
                    if (RemoveGate is not null) {
                        await RemoveGate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }

                    _ = _storedJson.TryRemove((string)actualArgs[0]!, out _);
                    return default!;

                case "eval":
                    return (TValue)(object)EvalKeys;

                default:
                    return default!;
            }
        }
    }

    private sealed record JsInvocation(string Identifier, object?[] Arguments);

    private sealed record PersistedPayload(string CurrentTheme, bool SidebarCollapsed = false, int Count = 0);
}
