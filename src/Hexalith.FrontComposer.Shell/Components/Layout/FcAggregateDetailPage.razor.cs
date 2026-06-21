using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Reusable aggregate detail page wrapper (FC-DTL contract, cc-2026-06-21). Composes
/// <see cref="FcPageLayout"/>, a vertical <c>FluentStack</c> root, and an optional back link, then routes
/// the <see cref="FcAggregateDetailState"/> surface state so the ready body renders only for the
/// projection-proven states (<see cref="FcAggregateDetailState.Ready"/>, <see cref="FcAggregateDetailState.Stale"/>,
/// <see cref="FcAggregateDetailState.Degraded"/>) — with a caller-supplied banner above the body for stale and
/// degraded — and every non-ready state renders its own caller-supplied content. The consuming domain supplies
/// all headers (<see cref="FcPageHeader"/> with its Actions/Metadata slots), copy, detail sections, and command
/// flows; the wrapper stays generic over <typeparamref name="TItem"/> and owns no domain copy or query behaviour.
/// </summary>
/// <typeparam name="TItem">The detail payload type passed to <see cref="ReadyTemplate"/> via <see cref="Item"/>.</typeparam>
public sealed partial class FcAggregateDetailPage<TItem> : ComponentBase {
    /// <summary>The FC-LYT layout measure the page declares. Defaults to <see cref="FcPageLayoutMode.Constrained"/> for readable detail surfaces.</summary>
    [Parameter] public FcPageLayoutMode LayoutMode { get; set; } = FcPageLayoutMode.Constrained;

    /// <summary>The current FC-DTL surface state. Drives which slot renders; never collapses a non-ready state into the ready body.</summary>
    [Parameter] public FcAggregateDetailState State { get; set; } = FcAggregateDetailState.Loading;

    /// <summary>Stable test selector for the page root stack.</summary>
    [Parameter] public string RootTestId { get; set; } = "fc-aggregate-detail";

    /// <summary>Optional CSS class applied to the page root stack.</summary>
    [Parameter] public string? RootClass { get; set; }

    /// <summary>Vertical gap between root regions (CSS length). Defaults to <c>16px</c>.</summary>
    [Parameter] public string RootGap { get; set; } = "16px";

    /// <summary>Whether the back link is rendered. Defaults to <see langword="true"/>.</summary>
    [Parameter] public bool ShowBackLink { get; set; } = true;

    /// <summary>The (caller-validated, safe) back navigation URL.</summary>
    [Parameter] public string BackHref { get; set; } = string.Empty;

    /// <summary>The visible back-link label.</summary>
    [Parameter] public string? BackLinkLabel { get; set; }

    /// <summary>Stable test selector for the back link.</summary>
    [Parameter] public string BackLinkTestId { get; set; } = "fc-aggregate-detail-back";

    /// <summary>Optional additional CSS classes applied to the back link.</summary>
    [Parameter] public string? BackLinkClass { get; set; }

    /// <summary>Optional detail payload passed to <see cref="ReadyTemplate"/> when the surface is ready.</summary>
    [Parameter] public TItem? Item { get; set; }

    /// <summary>Optional typed ready body template; rendered with <see cref="Item"/> when both are supplied.</summary>
    [Parameter] public RenderFragment<TItem>? ReadyTemplate { get; set; }

    /// <summary>Untemplated ready body (identity header, facts, sections). Used when <see cref="ReadyTemplate"/> is not supplied.</summary>
    [Parameter] public RenderFragment? ReadyContent { get; set; }

    /// <summary>Loading-state content rendered only for <see cref="FcAggregateDetailState.Loading"/>.</summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>Unauthorized-state content rendered only for <see cref="FcAggregateDetailState.Unauthorized"/>.</summary>
    [Parameter] public RenderFragment? UnauthorizedContent { get; set; }

    /// <summary>Not-found-state content rendered only for <see cref="FcAggregateDetailState.NotFound"/>.</summary>
    [Parameter] public RenderFragment? NotFoundContent { get; set; }

    /// <summary>Unavailable-state content rendered for <see cref="FcAggregateDetailState.Unavailable"/> and the fail-closed default.</summary>
    [Parameter] public RenderFragment? UnavailableContent { get; set; }

    /// <summary>Banner rendered above the ready body when the surface is <see cref="FcAggregateDetailState.Stale"/>.</summary>
    [Parameter] public RenderFragment? StaleBanner { get; set; }

    /// <summary>Banner rendered above the ready body when the surface is <see cref="FcAggregateDetailState.Degraded"/>.</summary>
    [Parameter] public RenderFragment? DegradedBanner { get; set; }

    private string BackLinkCssClass
        => string.IsNullOrWhiteSpace(BackLinkClass)
            ? "fc-aggregate-detail__back"
            : $"fc-aggregate-detail__back {BackLinkClass}";

    // Maps each non-ready state to its caller-supplied content, failing closed to UnavailableContent
    // when a specific slot is not provided so no non-ready state can render the ready body.
    private RenderFragment? ResolveStateContent()
        => State switch {
            FcAggregateDetailState.Loading => LoadingContent ?? UnavailableContent,
            FcAggregateDetailState.Unauthorized => UnauthorizedContent ?? UnavailableContent,
            FcAggregateDetailState.NotFound => NotFoundContent ?? UnavailableContent,
            _ => UnavailableContent,
        };
}
