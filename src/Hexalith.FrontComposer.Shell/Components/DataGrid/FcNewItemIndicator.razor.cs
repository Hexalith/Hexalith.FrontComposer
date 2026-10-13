using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>Transient DataGrid lane marker for newly confirmed rows outside active filter criteria.</summary>
public partial class FcNewItemIndicator : ComponentBase, IDisposable {
    private NewItemIndicatorEntry? _observedEntry;
    private bool _scopeResolved;
    private bool _pendingAnnouncement;
    private bool _announcementScheduled;
    private string? _activeSurface;
    private string? _activeGroup;
    private ITimer? _expiryTimer;
    private IDisposable? _stateSubscription;
    private bool _stateOwned;
    private bool _delivered;
    private bool _disposed;

    [CascadingParameter]
    private PagePanelVisibility? PanelVisibility { get; set; }

    private TimeProvider Time => Services.GetRequiredService<TimeProvider>();

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private FreshRowAnnouncementLedger Ledger => Services.GetRequiredService<FreshRowAnnouncementLedger>();

    private ISurfaceAnnouncementCoordinator Announcements => Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public string? AriaLabelOverride { get; set; }

    /// <summary>Fresh-row provenance. When supplied, names the entity and announces through the shared channel.</summary>
    [Parameter]
    public NewItemIndicatorEntry? Entry { get; set; }

    /// <summary>Uses the parent's persistent status node when true; entry-aware direct callers own one by default.</summary>
    [Parameter]
    public bool SharedStatusOwner { get; set; }

    /// <summary>Resolves the visible copy. Falls back to the localized whole-string default.</summary>
    protected string ResolvedText => string.IsNullOrWhiteSpace(Text)
        ? Entry is null ? Localizer["NewItemIndicatorText"].Value : UpdatedRowText
        : Text!;

    private string UpdatedRowText => Localizer["Am21UpdatedRow", Entry!.EntityKey].Value;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (_disposed)
        {
            return;
        }
        if (Entry != _observedEntry)
        {
            CancelAnnouncement();
            _expiryTimer?.Dispose();
            _expiryTimer = null;
            _stateSubscription?.Dispose();
            _stateSubscription = null;
            _observedEntry = Entry;
            _delivered = false;
            _stateOwned = Entry is not null && !string.IsNullOrWhiteSpace(Entry.ViewKey)
                && Services.GetService<INewItemIndicatorStateService>()?.Snapshot(Entry.ViewKey)
                    .Any(entry => entry == Entry) == true;
            if (_stateOwned && Entry is not null)
            {
                _stateSubscription = Services.GetRequiredService<INewItemIndicatorStateService>()
                    .Subscribe(Entry.ViewKey, () =>
                    {
                        if (!_disposed)
                        {
                            _ = InvokeAsync(() =>
                            {
                                if (!_disposed)
                                {
                                    OnParametersSet();
                                    StateHasChanged();
                                }
                            });
                        }
                    });
            }
            if (Entry is not null && !_stateOwned)
            {
                TimeSpan remaining = Entry.CreatedAt.AddSeconds(10) - Time.GetUtcNow();
                if (remaining > TimeSpan.Zero)
                {
                    NewItemIndicatorEntry captured = Entry;
                    _expiryTimer = Time.CreateTimer(_ =>
                    {
                        if (!_disposed)
                        {
                            _ = InvokeAsync(() =>
                            {
                                if (!_disposed && Entry == captured)
                                {
                                    _scopeResolved = false;
                                    _pendingAnnouncement = false;
                                    CancelAnnouncement();
                                    StateHasChanged();
                                }
                            });
                        }
                    }, null, remaining, Timeout.InfiniteTimeSpan);
                }
            }
        }
        _scopeResolved = IsEligibleEntry(Entry);
        _pendingAnnouncement = _scopeResolved && !_delivered && !_announcementScheduled && PanelVisibility?.IsVisible != false;
        if (!_scopeResolved)
        {
            CancelAnnouncement();
        }
    }

    private bool IsEligibleEntry(NewItemIndicatorEntry? entry) => entry is not null && Ledger.IsOriginatingScope(entry)
        && (_stateOwned
            ? Services.GetRequiredService<INewItemIndicatorStateService>().Snapshot(entry.ViewKey)
                .Any(candidate => candidate == entry)
            : Time.GetUtcNow() < entry.CreatedAt.AddSeconds(10));

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_pendingAnnouncement || Entry is null || _disposed)
        {
            return;
        }
        _pendingAnnouncement = false;
        _announcementScheduled = true;
        NewItemIndicatorEntry captured = Entry;
        // Let the containing render batch finish before publishing. Each continuation then
        // lets the shared status node render its mutation before the next cue publishes.
        await Task.Yield();
        _announcementScheduled = false;
        if (!_disposed && Entry != captured)
        {
            _pendingAnnouncement = _scopeResolved && !_delivered && PanelVisibility?.IsVisible != false;
            StateHasChanged();
            return;
        }
        if (_disposed)
        {
            return;
        }
        if (!_scopeResolved || !IsEligibleEntry(captured))
        {
            _scopeResolved = false;
            CancelAnnouncement();
            StateHasChanged();
            return;
        }
        if (PanelVisibility?.IsVisible == false)
        {
            return;
        }
        _delivered = true;
        if (Ledger.TryClaim(Entry))
        {
            _activeSurface = "projection:" + Entry.ViewKey;
            _activeGroup = "fresh-row:" + Guid.NewGuid().ToString("N");
            Announcements.Announce(_activeSurface, _activeGroup, Entry.MessageId, UpdatedRowText, immediate: true);
        }
        else if (!IsEligibleEntry(captured))
        {
            _scopeResolved = false;
            CancelAnnouncement();
            StateHasChanged();
        }
    }

    private void CancelAnnouncement()
    {
        if (_activeSurface is not null && _activeGroup is not null)
        {
            Announcements.Cancel(_activeSurface, _activeGroup);
            _activeSurface = null;
            _activeGroup = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _expiryTimer?.Dispose();
        _stateSubscription?.Dispose();
        if (Entry is not null)
        {
            _ = Ledger.HasResolvedScope();
        }
        CancelAnnouncement();
        GC.SuppressFinalize(this);
    }

    /// <summary>P12 — distinct accessible label so the role=status announcement carries context without duplication.</summary>
    protected string AriaLabel => string.IsNullOrWhiteSpace(AriaLabelOverride)
        ? Localizer["NewItemIndicatorAriaLabel"].Value
        : AriaLabelOverride!;
}
