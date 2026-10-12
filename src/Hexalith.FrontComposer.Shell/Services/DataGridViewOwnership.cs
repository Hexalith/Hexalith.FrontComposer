using System.Runtime.CompilerServices;

using Fluxor;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>Tracks mounted grid views so shared page and scroll state is cleared by the final owner.</summary>
public sealed class DataGridViewOwnership
{
    private static readonly ConditionalWeakTable<IDispatcher, DataGridViewOwnership> Owners = new();
    private readonly object _gate = new();
    private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);
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
            if (!_counts.ContainsKey(viewKey))
            {
                _generations[viewKey] = ++_nextGeneration;
            }
            _counts[viewKey] = _counts.GetValueOrDefault(viewKey) + 1;
            return _generations[viewKey];
        }
    }

    /// <summary>Checks that an asynchronous final-owner cleanup still owns the empty view slot.</summary>
    public bool CanDispose(string viewKey, long generation)
    {
        lock (_gate)
        {
            return !_counts.ContainsKey(viewKey)
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
            return true;
        }
    }
}
