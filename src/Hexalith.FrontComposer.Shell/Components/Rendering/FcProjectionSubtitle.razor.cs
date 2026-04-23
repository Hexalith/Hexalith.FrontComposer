using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Story 4-1 T6 / D8 / D9 / D21 / AC6 / AC11 — contextual subtitle rendered above every
/// generated projection view. Copy varies per <see cref="Role"/> (e.g., ActionQueue renders
/// <c>"N {entities} awaiting your action"</c>; Default renders <c>"N {entities}"</c>).
/// Count comes from <see cref="IBadgeCountService.Counts"/> when the service is registered;
/// otherwise falls back to the generator-passed <see cref="FallbackCount"/>.
/// </summary>
/// <remarks>
/// <para>Subscription scope is per-view (D21): one subscription per <see cref="IBadgeCountService.CountChanged"/>
/// stream, filtered inline to the current <see cref="ProjectionType"/>. Row-level components do
/// NOT subscribe.</para>
/// <para>Disposal: <see cref="IAsyncDisposable"/> owns a single <see cref="IDisposable"/>
/// subscription. The <c>_disposed</c> gate prevents late <c>CountChanged</c> notifications
/// from triggering <c>StateHasChanged</c> after disposal starts (FMA round 3 teardown-race).</para>
/// <para>Loading-state rendering (SCAMPER-M round 3): during load the component renders
/// an empty fragment — no <c>"0 entities"</c> flash-of-false-info before data arrives.</para>
/// </remarks>
public partial class FcProjectionSubtitle : IAsyncDisposable, IDisposable {
    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [Inject]
    private ILogger<FcProjectionSubtitle> Logger { get; set; } = default!;

    private IBadgeCountService? BadgeCountService => Services.GetService<IBadgeCountService>();

    private IDisposable? _countSubscription;
    private bool _disposed;
    private int? _countOverride;
    private bool _subscribeFailed;

    /// <summary>Gets or sets the projection runtime type this subtitle orients.</summary>
    [Parameter, EditorRequired]
    public Type ProjectionType { get; set; } = default!;

    /// <summary>
    /// Gets or sets the projection role driving copy selection. <see langword="null"/> means
    /// the Default role copy (no <c>[ProjectionRole]</c> attribute on the projection).
    /// </summary>
    [Parameter]
    public ProjectionRole? Role { get; set; }

    /// <summary>
    /// Gets or sets the raw CSV WhenState carried through from
    /// <see cref="ProjectionRoleAttribute.WhenState"/> for tool support / telemetry.
    /// Not rendered directly in v1.
    /// </summary>
    [Parameter]
    public string? WhenState { get; set; }

    /// <summary>
    /// Gets or sets the fallback count passed by the generated view (typically
    /// <c>state.Items?.Count ?? 0</c>). Used when <see cref="IBadgeCountService"/> is not
    /// registered OR throws during subscription.
    /// </summary>
    [Parameter]
    public int FallbackCount { get; set; }

    /// <summary>Gets or sets the resolved singular entity label emitted by the generator.</summary>
    [Parameter]
    public string? EntityLabel { get; set; }

    /// <summary>Gets or sets the resolved plural entity label emitted by the generator.</summary>
    [Parameter]
    public string? EntityPluralLabel { get; set; }

    /// <summary>
    /// Gets or sets the distinct status-bucket count for StatusOverview subtitles.
    /// Null or non-positive values fall back to the Default subtitle copy.
    /// </summary>
    [Parameter]
    public int? DistinctStatusCount { get; set; }

    /// <summary>Gets or sets the optional loading flag; true suppresses the subtitle render.</summary>
    [Parameter]
    public bool IsLoading { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized() {
        if (BadgeCountService is null) {
            return;
        }

        try {
            _countSubscription = BadgeCountService.CountChanged.Subscribe(OnCountChanged);
        }
        catch (Exception ex) {
            _subscribeFailed = true;
            Logger.LogWarning(
                ex,
                "FcProjectionSubtitle failed to subscribe to IBadgeCountService.CountChanged for {ProjectionType}; falling back to cascading count.",
                ProjectionType?.FullName ?? "(null)");
        }
    }

    private void OnCountChanged(BadgeCountChangedArgs args) {
        if (_disposed || args is null) {
            return;
        }

        if (!Equals(args.ProjectionType, ProjectionType)) {
            return;
        }

        _countOverride = args.NewCount;
        _ = InvokeAsync(StateHasChanged).ConfigureAwait(false);
    }

    private int ResolvedCount {
        get {
            if (_countOverride.HasValue) {
                return _countOverride.Value;
            }

            if (_subscribeFailed || BadgeCountService is null) {
                return FallbackCount;
            }

            if (BadgeCountService.Counts.TryGetValue(ProjectionType, out int count)) {
                return count;
            }

            return FallbackCount;
        }
    }

    private string ResolvedEntityPluralLabel {
        get {
            if (!string.IsNullOrWhiteSpace(EntityPluralLabel)) {
                return EntityPluralLabel!;
            }

            DisplayAttribute? attribute = ProjectionType.GetCustomAttribute<DisplayAttribute>();
            string? groupName = attribute?.GetGroupName();
            if (!string.IsNullOrWhiteSpace(groupName)) {
                return groupName!;
            }

            string? displayName = attribute?.GetName();
            if (!string.IsNullOrWhiteSpace(displayName)) {
                return displayName!;
            }

            string humanized = HumanizeProjectionTypeName().ToLowerInvariant();
            return humanized.EndsWith("s", StringComparison.Ordinal) ? humanized : humanized + "s";
        }
    }

    private bool _suppressRender => IsLoading;

    private string Subtitle {
        get {
            int count = ResolvedCount;
            string plural = ResolvedEntityPluralLabel;

            string key = Role switch {
                ProjectionRole.ActionQueue => "HomeActionQueueSubtitleTemplate",
                ProjectionRole.StatusOverview => "HomeStatusOverviewSubtitleTemplate",
                ProjectionRole.DetailRecord => "HomeDetailSubtitleTemplate",
                ProjectionRole.Timeline => "HomeTimelineSubtitleTemplate",
                _ => "HomeDefaultSubtitleTemplate",
            };

            string template = Localizer[key].Value;

            if (Role == ProjectionRole.DetailRecord) {
                return string.Format(CultureInfo.CurrentCulture, template, ResolvedEntityLabelForDetail());
            }

            if (Role == ProjectionRole.StatusOverview) {
                int? distinctStatusCount = DistinctStatusCount;
                if (distinctStatusCount.HasValue && distinctStatusCount.Value > 0) {
                    return string.Format(CultureInfo.CurrentCulture, template, count, distinctStatusCount.Value);
                }

                string fallbackTemplate = Localizer["HomeDefaultSubtitleTemplate"].Value;
                return string.Format(CultureInfo.CurrentCulture, fallbackTemplate, count, plural);
            }

            return string.Format(CultureInfo.CurrentCulture, template, count, plural);
        }
    }

    private string ResolvedEntityLabelForDetail() {
        if (!string.IsNullOrWhiteSpace(EntityLabel)) {
            return EntityLabel!;
        }

        DisplayAttribute? attribute = ProjectionType.GetCustomAttribute<DisplayAttribute>();
        string? displayName = attribute?.GetName();
        if (!string.IsNullOrWhiteSpace(displayName)) {
            return displayName!;
        }

        return HumanizeProjectionTypeName();
    }

    private string HumanizeProjectionTypeName() {
        string typeName = ProjectionType?.Name ?? "items";
        const string projectionSuffix = "Projection";
        string baseName = typeName.EndsWith(projectionSuffix, StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - projectionSuffix.Length)
            : typeName;

        System.Text.StringBuilder sb = new(baseName.Length + 4);
        for (int i = 0; i < baseName.Length; i++) {
            char c = baseName[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(baseName[i - 1])) {
                _ = sb.Append(' ');
            }

            _ = sb.Append(c);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        try {
            _countSubscription?.Dispose();
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "FcProjectionSubtitle disposal threw while unsubscribing CountChanged.");
        }

        _countSubscription = null;
    }
}
