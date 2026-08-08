#pragma warning disable CA2007
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.JSInterop;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class DataGridFocusScopeTests {
    [Fact]
    public async Task MethodsReuseImportedKeyboardModule_AndDisposeItOnce() {
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

    [Fact]
    public async Task FaultedImport_IsClearedSoLaterCallCanRetry() {
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
}
