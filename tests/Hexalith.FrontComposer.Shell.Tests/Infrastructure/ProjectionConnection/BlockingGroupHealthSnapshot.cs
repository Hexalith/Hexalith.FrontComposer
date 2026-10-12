using System.Collections;

using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.ProjectionConnection;

/// <summary>Pauses group-health enumeration after the first entry to test atomic publication.</summary>
internal sealed class BlockingGroupHealthSnapshot : IReadOnlyDictionary<ProjectionFallbackGroupKey, bool>, IDisposable
{
    private readonly KeyValuePair<ProjectionFallbackGroupKey, bool>[] _entries;
    private readonly ManualResetEventSlim _release = new();

    /// <summary>Creates a snapshot that blocks after its first entry.</summary>
    internal BlockingGroupHealthSnapshot(params KeyValuePair<ProjectionFallbackGroupKey, bool>[] entries) => _entries = entries;

    /// <summary>Signals that enumeration has reached its blocked boundary.</summary>
    internal TaskCompletionSource<bool> FirstEntryRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Allows enumeration to finish.</summary>
    internal void Release() => _release.Set();

    /// <inheritdoc />
    public int Count => _entries.Length;

    /// <inheritdoc />
    public IEnumerable<ProjectionFallbackGroupKey> Keys => _entries.Select(static entry => entry.Key);

    /// <inheritdoc />
    public IEnumerable<bool> Values => _entries.Select(static entry => entry.Value);

    /// <inheritdoc />
    public bool this[ProjectionFallbackGroupKey key] => _entries.Single(entry => entry.Key.Equals(key)).Value;

    /// <inheritdoc />
    public bool ContainsKey(ProjectionFallbackGroupKey key) => _entries.Any(entry => entry.Key.Equals(key));

    /// <inheritdoc />
    public bool TryGetValue(ProjectionFallbackGroupKey key, out bool value)
    {
        foreach (KeyValuePair<ProjectionFallbackGroupKey, bool> entry in _entries)
        {
            if (entry.Key.Equals(key))
            {
                value = entry.Value;
                return true;
            }
        }
        value = false;
        return false;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<ProjectionFallbackGroupKey, bool>> GetEnumerator()
    {
        if (_entries.Length == 0)
        {
            yield break;
        }
        yield return _entries[0];
        FirstEntryRead.TrySetResult(true);
        _release.Wait();
        for (int index = 1; index < _entries.Length; index++)
        {
            yield return _entries[index];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public void Dispose() => _release.Dispose();
}
