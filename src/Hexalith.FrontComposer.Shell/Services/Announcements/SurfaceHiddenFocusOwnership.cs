using System.Runtime.CompilerServices;

namespace Hexalith.FrontComposer.Shell.Services.Announcements;

/// <summary>Coordinates one first hidden-detail focus across mounted owners of a surface and result.</summary>
internal sealed class SurfaceHiddenFocusOwnership
{
    private static readonly ConditionalWeakTable<ISurfaceAnnouncementCoordinator, SurfaceHiddenFocusOwnership> Owners = new();
    private readonly Dictionary<string, FocusGroup> _groups = new(StringComparer.Ordinal);

    private sealed class FocusGroup
    {
        internal Dictionary<object, Action<string>> Claimants { get; } = new(ReferenceEqualityComparer.Instance);

        internal object? Owner { get; set; }

        internal bool Complete { get; set; }
    }

    /// <summary>Gets the scoped focus coordinator associated with an announcement coordinator.</summary>
    internal static SurfaceHiddenFocusOwnership For(ISurfaceAnnouncementCoordinator announcements) => Owners.GetValue(announcements, static _ => new());

    /// <summary>Retains the focus episode while a banner is mounted.</summary>
    internal bool Claim(string group, object claimant, Action<string> onHandoff)
    {
        lock (_groups)
        {
            if (!_groups.TryGetValue(group, out FocusGroup? state))
            {
                state = new FocusGroup();
                _groups[group] = state;
            }

            state.Claimants[claimant] = onHandoff;
            return !state.Complete;
        }
    }

    /// <summary>Records that an already hidden detail's later result uses speech alone.</summary>
    internal void MarkSpeechOnly(string group)
    {
        lock (_groups)
        {
            if (!_groups.TryGetValue(group, out FocusGroup? state))
            {
                state = new FocusGroup();
                _groups[group] = state;
            }
            state.Complete = true;
        }
    }

    /// <summary>Releases one mounted owner without reopening a completed focus for survivors.</summary>
    internal void Release(string group, object owner)
    {
        Action<string>? onHandoff = null;
        lock (_groups)
        {
            if (!_groups.TryGetValue(group, out FocusGroup? state) || !state.Claimants.Remove(owner))
            {
                return;
            }

            if (state.Claimants.Count == 0)
            {
                _groups.Remove(group);
            }
            else if (ReferenceEquals(state.Owner, owner) && !state.Complete)
            {
                // Elect before notifying: a simultaneous render by a third claimant
                // cannot start another focus attempt while the successor is scheduled.
                KeyValuePair<object, Action<string>> successor = state.Claimants.First();
                state.Owner = successor.Key;
                onHandoff = successor.Value;
            }
        }

        onHandoff?.Invoke(group);
    }

    /// <summary>Elects exactly one banner for the current focus episode, retaining it for retries.</summary>
    internal bool TryOwn(string group, object owner)
    {
        lock (_groups)
        {
            if (!_groups.TryGetValue(group, out FocusGroup? state) || !state.Claimants.ContainsKey(owner)
                || state.Complete || state.Owner is not null && !ReferenceEquals(state.Owner, owner))
            {
                return false;
            }

            state.Owner = owner;
            return true;
        }
    }

    /// <summary>Closes the shared attempt after focus or its polite fallback succeeds.</summary>
    internal void Complete(string group, object owner)
    {
        lock (_groups)
        {
            if (_groups.TryGetValue(group, out FocusGroup? state) && ReferenceEquals(state.Owner, owner))
            {
                state.Complete = true;
            }
        }
    }
}
