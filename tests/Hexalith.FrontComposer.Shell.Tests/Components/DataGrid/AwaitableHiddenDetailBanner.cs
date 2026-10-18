using Hexalith.FrontComposer.Shell.Components.DataGrid;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>Exposes completion of the real banner lifecycle for delayed interop tests.</summary>
internal sealed class AwaitableHiddenDetailBanner : FcExpandedRowHiddenBanner
{
    /// <summary>Gets the latest after-render operation.</summary>
    public Task AfterRender { get; private set; } = Task.CompletedTask;

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        AfterRender = base.OnAfterRenderAsync(firstRender);
        return AfterRender;
    }
}
