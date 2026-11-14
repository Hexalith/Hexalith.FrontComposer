#pragma warning disable CA2007
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Services.DevMode;
using Bunit;

using Microsoft.JSInterop;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class DataGridFocusScopeTests {
    [Fact]
    public async Task SoleSearchForwardsToCurrentRouteInterop()
    {
        using Bunit.BunitContext context = new();
        Bunit.BunitJSModuleInterop module = context.JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js");
        module.Setup<bool>("focusSolePageSearch").SetResult(true);
        await using DataGridFocusScope scope = new(context.JSInterop.JSRuntime);
        (await scope.FocusSolePageSearchAsync(Xunit.TestContext.Current.CancellationToken)).ShouldBeTrue();
        module.Invocations.Count(call => call.Identifier == "focusSolePageSearch").ShouldBe(1);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blazor", "BL0016", Justification = "Test-only mock configuration or controlled fixture completion, not a browser call; interop failures must remain observable by the service assertions.")]
    public async Task MethodsReuseImportedKeyboardModule_AndDisposeItOnce() {
        try {
            CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
            IJSRuntime js = Substitute.For<IJSRuntime>();
            IJSObjectReference module = Substitute.For<IJSObjectReference>();
    #pragma warning disable CA2012 // NSubstitute captures this reusable ValueTask fixture; production code awaits each invocation once.
            js.InvokeAsync<IJSObjectReference>("import", Arg.Any<object?[]>())
                .Returns(new ValueTask<IJSObjectReference>(module));
    #pragma warning restore CA2012
    #pragma warning disable CA2012 // NSubstitute captures this reusable ValueTask fixture; production code awaits each invocation once.
            module.InvokeAsync<bool>("isFocusWithinDataGrid", Arg.Any<CancellationToken>(), Arg.Any<object?[]>())
                .Returns(new ValueTask<bool>(true));
    #pragma warning restore CA2012
    #pragma warning disable CA2012 // NSubstitute captures this reusable ValueTask fixture; production code awaits each invocation once.
            module.InvokeAsync<string?>("activeDataGridViewKey", Arg.Any<CancellationToken>(), Arg.Any<object?[]>())
                .Returns(new ValueTask<string?>("acme:OrdersProjection"));
    #pragma warning restore CA2012
    #pragma warning disable CA2012 // NSubstitute captures this reusable ValueTask fixture; production code awaits each invocation once.
            module.InvokeAsync<bool>("focusFirstColumnFilter", Arg.Any<CancellationToken>(), Arg.Any<object?[]>())
                .Returns(new ValueTask<bool>(true));
    #pragma warning restore CA2012

            DataGridFocusScope sut = new(js);

            (await sut.IsFocusWithinDataGridAsync(ct)).ShouldBeTrue();
            (await sut.GetActiveViewKeyAsync(ct)).ShouldBe("acme:OrdersProjection");
            (await sut.FocusFirstColumnFilterAsync("acme:OrdersProjection", ct)).ShouldBeTrue();

            await sut.DisposeAsync();

    #pragma warning disable CA2012 // NSubstitute's verification ValueTask is intentionally inspected only for the recorded invocation.
            _ = js.Received(1).InvokeAsync<IJSObjectReference>("import", Arg.Any<object?[]>());
    #pragma warning restore CA2012
    #pragma warning disable CA2012 // NSubstitute's verification ValueTask is intentionally inspected only for the recorded invocation.
            _ = module.Received(1).DisposeAsync();
    #pragma warning restore CA2012
        }
        catch (JSException ex) {
            throw new InvalidOperationException("The synthetic JS runtime failed unexpectedly.", ex);
        }
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blazor", "BL0016", Justification = "Test-only mock configuration or controlled fixture completion, not a browser call; interop failures must remain observable by the service assertions.")]
    public async Task FaultedImport_IsClearedSoLaterCallCanRetry() {
        try {
            CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
            IJSRuntime js = Substitute.For<IJSRuntime>();
            IJSObjectReference module = Substitute.For<IJSObjectReference>();
    #pragma warning disable CA2012 // NSubstitute captures only these ordered ValueTask fixtures to verify retry behavior.
            js.InvokeAsync<IJSObjectReference>("import", Arg.Any<object?[]>())
                .Returns(
                    ValueTask.FromException<IJSObjectReference>(new JSException("boom")),
                    new ValueTask<IJSObjectReference>(module));
    #pragma warning restore CA2012
    #pragma warning disable CA2012 // NSubstitute captures this reusable ValueTask fixture; production code awaits each invocation once.
            module.InvokeAsync<bool>("isFocusWithinDataGrid", Arg.Any<CancellationToken>(), Arg.Any<object?[]>())
                .Returns(new ValueTask<bool>(true));
    #pragma warning restore CA2012

            DataGridFocusScope sut = new(js);

            (await sut.IsFocusWithinDataGridAsync(ct)).ShouldBeFalse();
            (await sut.IsFocusWithinDataGridAsync(ct)).ShouldBeTrue();

    #pragma warning disable CA2012 // NSubstitute's verification ValueTask is intentionally inspected only for the recorded invocation.
            _ = js.Received(2).InvokeAsync<IJSObjectReference>("import", Arg.Any<object?[]>());
    #pragma warning restore CA2012
        }
        catch (JSException ex) {
            throw new InvalidOperationException("The synthetic JS runtime failed unexpectedly.", ex);
        }
    }

    /// <summary>Each module wrapper retries a synchronous import failure without invoking a missing module.</summary>
    /// <param name="wrapper">The production wrapper to exercise.</param>
    [Theory]
    [InlineData("focus")]
    [InlineData("scroll")]
    [InlineData("expand")]
    [InlineData("clipboard")]
    public async Task SynchronousImportFailureCanBeRetried(string wrapper) {
        SynchronousImportFailureRuntime js = new();
        string operation;
        switch (wrapper) {
            case "focus":
                await using (DataGridFocusScope sut = new(js)) {
                    (await sut.IsFocusWithinDataGridAsync(Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBeFalse();
                    (await sut.IsFocusWithinDataGridAsync(Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBeTrue();
                }
                operation = "isFocusWithinDataGrid";
                break;
            case "scroll":
                await using (DataGridScrollInterop sut = new(js)) {
                    using DotNetObjectReference<object> reference = DotNetObjectReference.Create(new object());
                    await sut.CaptureScrollAsync("view", 0, reference, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
                    await sut.CaptureScrollAsync("view", 0, reference, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
                }
                operation = "captureScrollThrottled";
                break;
            case "expand":
                await using (ExpandInRowJSModule sut = new(js)) {
                    await sut.InitializeAsync(default).ConfigureAwait(true);
                    await sut.InitializeAsync(default).ConfigureAwait(true);
                }
                operation = "initializeExpandInRow";
                break;
            default:
                await using (ClipboardJSModule sut = new(js)) {
                    (await sut.CopyToClipboardAsync("text", Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe(ClipboardCopyResult.Failed);
                    (await sut.CopyToClipboardAsync("text", Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe(ClipboardCopyResult.Success);
                }
                operation = "copyToClipboard";
                break;
        }
        js.ImportCalls.ShouldBe(2);
        js.Invocations.ShouldBe([operation]);
    }
}
