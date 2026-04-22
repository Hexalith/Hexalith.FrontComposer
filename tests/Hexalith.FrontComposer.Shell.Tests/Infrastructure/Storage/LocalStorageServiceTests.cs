using System.Collections.Concurrent;
using System.Linq;
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
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);

        string? result = await sut.GetAsync<string?>("missing", ct);

        result.ShouldBeNull();
        GetTrackedKeyCount(sut).ShouldBe(0);
    }

    [Fact]
    public async Task GetAsync_ExistingKey_DeserializesValue() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        js.SetStoredJson("theme", "\"Dark\"");

        string? result = await sut.GetAsync<string?>("theme", ct);

        result.ShouldBe("Dark");
    }

    [Fact]
    public async Task GetAsync_UsesWebJsonOptionsForCamelCasePayload() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        js.SetStoredJson("payload", "{\"currentTheme\":\"Dark\"}");

        PersistedPayload? result = await sut.GetAsync<PersistedPayload>("payload", ct);

        result.ShouldBe(new PersistedPayload("Dark"));
    }

    [Fact]
    public async Task RemoveAsync_QueuesDeletionUntilFlush() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        TaskCompletionSource removeGate = js.BlockRemoveCalls();

        Task task = sut.RemoveAsync("theme", ct);
        task.IsCompleted.ShouldBeTrue();

        removeGate.TrySetResult();
        await sut.FlushAsync(ct);

        js.Invocations.ShouldContain(i => i.Identifier == "localStorage.removeItem");
    }

    [Fact]
    public async Task SetAsync_FireAndForget_ReturnsImmediatelyThenDrainsToJs() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        TaskCompletionSource setGate = js.BlockSetItemCalls();

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
    public async Task GetKeysAsync_FiltersByPrefix() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await using LocalStorageService sut = CreateService(out TestJsRuntime js);
        js.SetEvalKeys("acme:u1:theme", "acme:u1:density", "other:u1:theme", "acme:u2:theme");

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

        public TaskCompletionSource? SetItemGate { get; private set; }

        public TaskCompletionSource? RemoveGate { get; private set; }

        public IReadOnlyCollection<JsInvocation> Invocations => _invocations.ToArray();

        public string[] EvalKeys { get; private set; } = [];

        public TaskCompletionSource BlockRemoveCalls()
            => RemoveGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource BlockSetItemCalls()
            => SetItemGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void SetEvalKeys(params string[] keys) => EvalKeys = keys;

        public void SetStoredJson(string key, string? json) => _storedJson[key] = json;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) {
            object?[] actualArgs = args ?? [];
            _invocations.Enqueue(new JsInvocation(identifier, actualArgs));

            switch (identifier) {
                case "localStorage.getItem":
                    string key = (string)actualArgs[0]!;
                    _ = _storedJson.TryGetValue(key, out string? json);
                    return (TValue)(object?)json!;

                case "localStorage.setItem":
                    if (SetItemGate is not null) {
                        await SetItemGate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }

                    if (SetItemException is not null) {
                        throw SetItemException;
                    }

                    return default!;

                case "localStorage.removeItem":
                    if (RemoveGate is not null) {
                        await RemoveGate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }

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
