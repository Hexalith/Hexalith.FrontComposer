using System.Collections.Immutable;

using Bunit;
using Bunit.Rendering;

using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 4-1 AC1d — behavioral proof that navigation between ActionQueue-like views on the
/// same circuit gets a fresh per-row <see cref="ProjectionContext"/> instead of leaking the
/// previously observed row. The generated 4-1 action column still renders an empty child
/// fragment, so this test mirrors the emitted fixed <c>CascadingValue</c> pattern with a
/// tiny probe component that records the current AggregateId on click.
/// </summary>
public sealed class ActionQueueProjectionContextIsolationTests : GeneratedComponentTestBase {
    public ActionQueueProjectionContextIsolationTests()
        : base() {
    }

    [Fact]
    public void NavigatingBetweenActionQueueViews_UsesFreshProjectionContextPerRow() {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/view-a");

        string? observedAggregateId = null;
        IRenderedComponent<ActionQueueNavigationHost> cut = Render<ActionQueueNavigationHost>(parameters => parameters
            .Add(p => p.OnObservedAggregateId, id => observedAggregateId = id));

        cut.WaitForAssertion(() => {
            cut.Find("[data-testid='context-a-1']").TextContent.ShouldBe("a-1");
            cut.Find("[data-testid='context-a-2']").TextContent.ShouldBe("a-2");
        });

        cut.Find("[data-testid='probe-a-2']").Click();
        observedAggregateId.ShouldBe("a-2");
        string lastClickedInViewA = observedAggregateId;

        navigation.NavigateTo("/view-b");

        cut.WaitForAssertion(() => {
            cut.Find("[data-testid='context-b-1']").TextContent.ShouldBe("b-1");
            Should.Throw<ElementNotFoundException>(() => cut.Find("[data-testid='context-a-1']"));
        });

        cut.Find("[data-testid='probe-b-1']").Click();
        observedAggregateId.ShouldBe("b-1");
        observedAggregateId.ShouldNotBe(lastClickedInViewA);
    }

    private sealed record ActionQueueRow(string Id, string Label);

    private sealed class ActionQueueNavigationHost : ComponentBase, IDisposable {
        private static readonly IReadOnlyList<ActionQueueRow> _viewAItems =
        [
            new("a-1", "Approve invoice A"),
            new("a-2", "Approve invoice B"),
        ];

        private static readonly IReadOnlyList<ActionQueueRow> _viewBItems =
        [
            new("b-1", "Review shipment A"),
            new("b-2", "Review shipment B"),
        ];

        [Inject] private NavigationManager Navigation { get; set; } = default!;

        [Parameter] public EventCallback<string?> OnObservedAggregateId { get; set; }

        protected override void OnInitialized() {
            Navigation.LocationChanged += HandleLocationChanged;
        }

        public void Dispose() {
            Navigation.LocationChanged -= HandleLocationChanged;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            bool isViewB = string.Equals(
                Navigation.ToBaseRelativePath(Navigation.Uri),
                "view-b",
                StringComparison.OrdinalIgnoreCase);

            builder.OpenComponent<ActionQueueViewHarness>(0);
            builder.AddAttribute(1, nameof(ActionQueueViewHarness.ViewName), isViewB ? "B" : "A");
            builder.AddAttribute(2, nameof(ActionQueueViewHarness.Items), isViewB ? _viewBItems : _viewAItems);
            builder.AddAttribute(3, nameof(ActionQueueViewHarness.OnObservedAggregateId), OnObservedAggregateId);
            builder.CloseComponent();
        }

        private void HandleLocationChanged(object? sender, LocationChangedEventArgs e) {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private sealed class ActionQueueViewHarness : ComponentBase {
        [Parameter] public string ViewName { get; set; } = string.Empty;

        [Parameter] public IReadOnlyList<ActionQueueRow> Items { get; set; } = [];

        [Parameter] public EventCallback<string?> OnObservedAggregateId { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            int seq = 0;
            builder.OpenElement(seq++, "section");
            builder.AddAttribute(seq++, "data-testid", $"action-queue-view-{ViewName.ToLowerInvariant()}");

            foreach (ActionQueueRow item in Items) {
                ProjectionContext rowContext = new(
                    projectionTypeFqn: $"TestDomain.View{ViewName}.ActionQueueProjection",
                    boundedContext: $"View{ViewName}",
                    aggregateId: item.Id,
                    fields: ImmutableDictionary<string, object?>.Empty
                        .Add(nameof(ActionQueueRow.Id), item.Id)
                        .Add(nameof(ActionQueueRow.Label), item.Label));

                builder.OpenComponent<CascadingValue<ProjectionContext>>(seq++);
                builder.AddAttribute(seq++, "Value", rowContext);
                builder.AddAttribute(seq++, "IsFixed", true);
                builder.AddAttribute(seq++, "ChildContent", (RenderFragment)((RenderTreeBuilder rowBuilder) => {
                    rowBuilder.OpenComponent<ProjectionContextProbe>(0);
                    rowBuilder.AddAttribute(1, nameof(ProjectionContextProbe.RowId), item.Id);
                    rowBuilder.AddAttribute(2, nameof(ProjectionContextProbe.OnObservedAggregateId), OnObservedAggregateId);
                    rowBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }

            builder.CloseElement();
        }
    }

    private sealed class ProjectionContextProbe : ComponentBase {
        [CascadingParameter] public ProjectionContext? ProjectionContext { get; set; }

        [Parameter] public string RowId { get; set; } = string.Empty;

        [Parameter] public EventCallback<string?> OnObservedAggregateId { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            int seq = 0;

            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "data-testid", $"context-{RowId}");
            builder.AddContent(seq++, ProjectionContext?.AggregateId ?? "null");
            builder.CloseElement();

            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "type", "button");
            builder.AddAttribute(seq++, "data-testid", $"probe-{RowId}");
            builder.AddAttribute(
                seq++,
                "onclick",
                EventCallback.Factory.Create(this, () => OnObservedAggregateId.InvokeAsync(ProjectionContext?.AggregateId)));
            builder.AddContent(seq++, $"Use {RowId}");
            builder.CloseElement();
        }
    }
}