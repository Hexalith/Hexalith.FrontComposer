using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

using Fluxor;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>Tracks mounted grid views so shared page and scroll state is cleared by the final owner.</summary>
public sealed class DataGridViewOwnership
{
    private static readonly ConditionalWeakTable<IDispatcher, DataGridViewOwnership> Owners = new();
    private readonly object _gate = new();
    private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<(string Scope, int Slot)>> _panelSlots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _generations = new(StringComparer.Ordinal);
    private long _nextGeneration;

    /// <summary>Gets the ownership registry for a scoped dispatcher.</summary>
    public static DataGridViewOwnership For(IDispatcher dispatcher)
        => Owners.GetValue(dispatcher, static _ => new DataGridViewOwnership());

    /// <summary>Registers one mounted view for the given key.</summary>
    public long Register(string viewKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewKey);
        lock (_gate)
        {
            long generation = CurrentOrNewGeneration(viewKey);
            _counts[viewKey] = _counts.GetValueOrDefault(viewKey) + 1;
            return generation;
        }
    }

    /// <summary>Registers a mounted view and reserves its reusable detail panel slot.</summary>
    public long Register(string viewKey, string panelScope, out int panelSlot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewKey);
        lock (_gate)
        {
            long generation = CurrentOrNewGeneration(viewKey);
            if (!_panelSlots.TryGetValue(viewKey, out HashSet<(string Scope, int Slot)>? slots))
            {
                slots = [];
                _panelSlots.Add(viewKey, slots);
            }

            panelSlot = 0;
            while (!slots.Add((panelScope, panelSlot)))
            {
                panelSlot++;
            }

            return generation;
        }
    }

    /// <summary>Checks that an asynchronous final-owner cleanup still owns the empty view slot.</summary>
    public bool CanDispose(string viewKey, long generation)
    {
        lock (_gate)
        {
            return !HasOwners(viewKey)
                && _generations.TryGetValue(viewKey, out long current)
                && current == generation;
        }
    }

    /// <summary>Releases one mounted view and reports whether it was the final owner.</summary>
    public bool Release(string viewKey)
    {
        lock (_gate)
        {
            if (!_counts.TryGetValue(viewKey, out int count))
            {
                return false;
            }
            if (count > 1)
            {
                _counts[viewKey] = count - 1;
                return false;
            }
            _counts.Remove(viewKey);
            return !HasOwners(viewKey);
        }
    }

    /// <summary>Releases the mounted panel slot and reports whether it was the final owner.</summary>
    public bool Release(string viewKey, string panelScope, int panelSlot)
    {
        lock (_gate)
        {
            if (!_panelSlots.TryGetValue(viewKey, out HashSet<(string Scope, int Slot)>? slots) || !slots.Remove((panelScope, panelSlot)))
            {
                return false;
            }

            if (slots.Count == 0)
            {
                _panelSlots.Remove(viewKey);
            }

            return !HasOwners(viewKey);
        }
    }

    /// <summary>Creates an element ID for a mounted view slot or its active detail.</summary>
    public static string CreatePanelId(string viewKey, string panelScope, int instanceSlot, object? rowKey = null)
    {
        ArgumentNullException.ThrowIfNull(viewKey);
        ArgumentNullException.ThrowIfNull(panelScope);
        string? rowIdentity = rowKey switch
        {
            DateTime timestamp => timestamp.Ticks.ToString(CultureInfo.InvariantCulture),
            DateTimeOffset timestamp => timestamp.UtcTicks.ToString(CultureInfo.InvariantCulture),
            decimal number => number.ToString("G29", CultureInfo.InvariantCulture),
            _ => Convert.ToString(rowKey, CultureInfo.InvariantCulture),
        };
        string identity = FormattableString.Invariant(
            $"{viewKey.Length}:{viewKey}:{panelScope.Length}:{panelScope}:{instanceSlot}:{rowKey is not null}:{rowIdentity}");
        return "fc-expand-panel-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    private bool HasOwners(string viewKey)
        => _counts.ContainsKey(viewKey) || _panelSlots.ContainsKey(viewKey);

    private long CurrentOrNewGeneration(string viewKey)
    {
        if (!HasOwners(viewKey))
        {
            _generations[viewKey] = ++_nextGeneration;
        }

        return _generations[viewKey];
    }
}
