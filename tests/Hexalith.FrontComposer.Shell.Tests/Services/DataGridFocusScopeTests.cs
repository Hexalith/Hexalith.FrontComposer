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
        js.InvokeAsync<IJSObjectReference>("import", Arg.Any<object?[]>())
            .Returns(new ValueTask<IJSObjectReference>(module));
        module.InvokeAsync<bool>("isFocusWithinDataGrid", Arg.Any<CancellationToken>(), Arg.Any<object?[]>())
            .Returns(new ValueTask<bool>(true));
        module.InvokeAsync<string?>("activeDataGridViewKey", Arg.Any<CancellationToken>(), Arg.Any<object?[]>())
            .Returns(new ValueTask<string?>("acme:OrdersProjection"));
        module.InvokeAsync<bool>("focusFirstColumnFilter", Arg.Any<CancellationToken>(), Arg.Any<object?[]>())
            .Returns(new ValueTask<bool>(true));

        DataGridFocusScope sut = new(js);

        (await sut.IsFocusWithinDataGridAsync(ct)).ShouldBeTrue();
        (await sut.GetActiveViewKeyAsync(ct)).ShouldBe("acme:OrdersProjection");
        (await sut.FocusFirstColumnFilterAsync("acme:OrdersProjection", ct)).ShouldBeTrue();

        await sut.DisposeAsync();

        _ = js.Received(1).InvokeAsync<IJSObjectReference>("import", Arg.Any<object?[]>());
        _ = module.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task FaultedImport_IsClearedSoLaterCallCanRetry() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        IJSRuntime js = Substitute.For<IJSRuntime>();
        IJSObjectReference module = Substitute.For<IJSObjectReference>();
        js.InvokeAsync<IJSObjectReference>("import", Arg.Any<object?[]>())
            .Returns(
                ValueTask.FromException<IJSObjectReference>(new JSException("boom")),
                new ValueTask<IJSObjectReference>(module));
        module.InvokeAsync<bool>("isFocusWithinDataGrid", Arg.Any<CancellationToken>(), Arg.Any<object?[]>())
            .Returns(new ValueTask<bool>(true));

        DataGridFocusScope sut = new(js);

        (await sut.IsFocusWithinDataGridAsync(ct)).ShouldBeFalse();
        (await sut.IsFocusWithinDataGridAsync(ct)).ShouldBeTrue();

        _ = js.Received(2).InvokeAsync<IJSObjectReference>("import", Arg.Any<object?[]>());
    }
}
