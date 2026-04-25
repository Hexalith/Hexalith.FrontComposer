using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-4 T5.2 — verifies the generator emits the server-side <c>LoadPageAsync</c>
/// provider callback per D2 / D3, dispatching <c>LoadPageAction</c> via Fluxor and
/// awaiting the reducer-resolved <c>TaskCompletionSource&lt;object&gt;</c>.
/// </summary>
public sealed class RazorEmitterItemsProviderTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);

    private static RazorModel Model()
        => new("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Id", "Id", TypeCategory.Text, null, false, _emptyBadges),
                new ColumnModel("Name", "Name", TypeCategory.Text, null, false, _emptyBadges))));

    [Fact]
    public void EmitsLoadPageAsyncMethodOnGridStrategies() {
        string src = RazorEmitter.Emit(Model());
        src.ShouldContain("private async ValueTask<global::Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<OrderProjection>> LoadPageAsync");
        src.ShouldContain("\"ItemsProvider\", (global::Microsoft.FluentUI.AspNetCore.Components.GridItemsProvider<OrderProjection>)LoadPageAsync");
        src.ShouldContain("state.Items.Count >= ShellOptions.Value.VirtualizationServerSideThreshold");
    }

    [Fact]
    public void LoadPageAsync_DispatchesLoadPageActionWithViewKeyAndPagingArgs() {
        string src = RazorEmitter.Emit(Model());
        src.ShouldContain("Dispatcher.Dispatch(new global::Hexalith.FrontComposer.Contracts.Rendering.LoadPageAction(");
        src.ShouldContain("viewKey: _viewKey");
        src.ShouldContain("skip: skip");
        src.ShouldContain("take: take");
        src.ShouldContain("filters: filters");
        src.ShouldContain("sortColumn: snapshot?.SortColumn");
        src.ShouldContain("searchQuery: searchQuery");
        src.ShouldContain("completion: completion");
        src.ShouldContain("cancellationToken: ct");
    }

    [Fact]
    public void LoadPageAsync_AwaitsTaskCompletionSourceAndCastsToTypedItems() {
        string src = RazorEmitter.Emit(Model());
        src.ShouldContain("new System.Threading.Tasks.TaskCompletionSource<object>");
        src.ShouldContain("await completion.Task.WaitAsync(ct)");
        src.ShouldContain("if (item is OrderProjection typedItem)");
        src.ShouldContain("new global::Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<OrderProjection>");
    }

    [Fact]
    public void HandleScrollAsync_DispatchesScrollCapturedActionWithInputValidation() {
        string src = RazorEmitter.Emit(Model());
        src.ShouldContain("[global::Microsoft.JSInterop.JSInvokable]");
        src.ShouldContain("public void HandleScrollAsync(string viewKey, double scrollTop)");
        src.ShouldContain("new global::Hexalith.FrontComposer.Contracts.Rendering.ScrollCapturedAction(viewKey, scrollTop)");
        src.ShouldContain("double.IsNaN(scrollTop) || double.IsInfinity(scrollTop) || scrollTop < 0");
    }
}
