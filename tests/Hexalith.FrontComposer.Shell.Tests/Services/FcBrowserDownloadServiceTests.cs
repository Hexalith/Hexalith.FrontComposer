using Bunit;
using Hexalith.FrontComposer.Shell.Services.Downloads;
using Microsoft.JSInterop;
using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class FcBrowserDownloadServiceTests : BunitContext
{
    [Fact]
    public async Task JsonPreservesBytesAndFilenameAcrossRepeatedDownloads()
    {
        var module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-download.js");
        module.Mode = JSRuntimeMode.Loose;
        var service = new FcBrowserDownloadService(JSInterop.JSRuntime);
        byte[] bytes = [0xEF, 0xBB, 0xBF, 123, 10, 125];
        var receivedBytes = new List<byte[]>();
        module.SetupVoid("downloadStream", invocation =>
        {
            var reference = (DotNetStreamReference)invocation.Arguments[3]!;
            using var copy = new MemoryStream();
            reference.Stream.CopyTo(copy);
            receivedBytes.Add(copy.ToArray());
            return true;
        }).SetVoidResult();
        for (int index = 0; index < 2; index++)
        {
            await service.DownloadJsonAsync(bytes, "safe-export.json", Xunit.TestContext.Current.CancellationToken);
        }
        module.Invocations["downloadStream"].Count.ShouldBe(2);
        foreach (var invocation in module.Invocations["downloadStream"])
        {
            invocation.Arguments[1].ShouldBe("safe-export.json");
            invocation.Arguments[2].ShouldBe("application/json");
        }
        module.Invocations["cancelDownload"].Count.ShouldBe(2);
        receivedBytes.Count.ShouldBe(2);
        receivedBytes.ShouldAllBe(actual => actual.SequenceEqual(bytes));
    }

    [Theory]
    [InlineData("../secret.json")]
    [InlineData("a\\b.json")]
    [InlineData("a:b.json")]
    [InlineData("a\n.json")]
    [InlineData("..")]
    public async Task UnsafeNamesAreRejectedBeforeInterop(string filename)
    {
        var service = new FcBrowserDownloadService(JSInterop.JSRuntime);
        await Should.ThrowAsync<ArgumentException>(() => service.DownloadJsonAsync(new byte[] { 123, 125 }, filename, Xunit.TestContext.Current.CancellationToken));
        JSInterop.Invocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task PreCancelledDownloadDoesNotInvokeBrowserAndLeavesCallerStreamOpen()
    {
        var service = new FcBrowserDownloadService(JSInterop.JSRuntime);
        using var stream = new MemoryStream(new byte[] { 1, 2 });
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => service.DownloadStreamAsync(stream, "safe.bin", "application/octet-stream", cancellation.Token));
        stream.CanRead.ShouldBeTrue();
        JSInterop.Invocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task BrowserFailureStillCleansOperationAndKeepsCallerStreamOpen()
    {
        var module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-download.js");
        module.Mode = JSRuntimeMode.Loose;
        module.SetupVoid("downloadStream", _ => true).SetException(new JSException("synthetic"));
        using var stream = new MemoryStream(new byte[] { 1, 2 });
        await Should.ThrowAsync<JSException>(() => new FcBrowserDownloadService(JSInterop.JSRuntime)
            .DownloadStreamAsync(stream, "safe.bin", "application/octet-stream", Xunit.TestContext.Current.CancellationToken));
        module.Invocations["cancelDownload"].Count.ShouldBe(1);
        stream.CanRead.ShouldBeTrue();
    }
    [Fact]
    public async Task CancellationAtBrowserAdmissionCancelsTheAdmittedOperationBeforeTransfer()
    {
        using var cancellation = new CancellationTokenSource();
        var module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-download.js");
        module.Mode = JSRuntimeMode.Loose;
        module.SetupVoid("beginDownload", _ => { cancellation.Cancel(); return true; }).SetVoidResult();
        using var stream = new MemoryStream(new byte[] { 1, 2 });
        await Should.ThrowAsync<OperationCanceledException>(() => new FcBrowserDownloadService(JSInterop.JSRuntime)
            .DownloadStreamAsync(stream, "safe.bin", "application/octet-stream", cancellation.Token));
        module.Invocations["beginDownload"].Count.ShouldBe(1);
        module.Invocations["cancelDownload"].Count.ShouldBe(1);
        module.Invocations.ShouldNotContain(invocation => invocation.Identifier == "downloadStream");
        stream.CanRead.ShouldBeTrue();
    }

}
