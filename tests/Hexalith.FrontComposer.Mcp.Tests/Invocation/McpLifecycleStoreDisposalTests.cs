using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

/// <summary>
/// Story 11.21 CA1513 regression cover. <c>FrontComposerMcpLifecycleStore.ThrowIfDisposed</c> was
/// rewritten from an explicit <c>throw new ObjectDisposedException(...)</c> to
/// <see cref="ObjectDisposedException.ThrowIf(bool, object)"/>. The store's fail-closed contract is
/// that every post-dispose read still throws <see cref="ObjectDisposedException"/> and that disposal
/// stays idempotent, so those two properties are pinned here rather than left to the analyzer fix.
/// </summary>
public sealed class McpLifecycleStoreDisposalTests {
    [Fact]
    public void TryReadSnapshot_AfterDispose_ThrowsObjectDisposedException() {
        FrontComposerMcpLifecycleStore store = CreateStore();
        store.Dispose();

        _ = Should.Throw<ObjectDisposedException>(
            () => store.TryReadSnapshot(
                "01JBX0000000000000000000AB",
                new FrontComposerMcpOptions(),
                out _,
                out _));
    }

    [Fact]
    public void TryReadSnapshot_BeforeDispose_DoesNotThrowAndReportsUnknownHandle() {
        using FrontComposerMcpLifecycleStore store = CreateStore();

        bool known = store.TryReadSnapshot(
            "01JBX0000000000000000000AB",
            new FrontComposerMcpOptions(),
            out _,
            out _);

        known.ShouldBeFalse("an unknown handle is a miss, not a disposal failure.");
    }

    [Fact]
    public void Dispose_CalledTwice_IsIdempotent() {
        FrontComposerMcpLifecycleStore store = CreateStore();

        store.Dispose();
        Should.NotThrow(store.Dispose);

        // Idempotent Dispose must not reopen the store: a post-dispose read still fails closed.
        _ = Should.Throw<ObjectDisposedException>(
            () => store.TryReadSnapshot(
                "01JBX0000000000000000000AB",
                new FrontComposerMcpOptions(),
                out _,
                out _));
    }

    private static FrontComposerMcpLifecycleStore CreateStore()
        => new(Options.Create(new FrontComposerMcpOptions()));
}
