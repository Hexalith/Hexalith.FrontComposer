using System.Runtime.CompilerServices;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>Lets one scope-blocked heading take focus for each scope generation.</summary>
internal sealed class ScopeBlockedFocusOwnership
{
    private static readonly ConditionalWeakTable<ScopeBoundaryService, ScopeBlockedFocusOwnership> Owners = new();
    private readonly object _gate = new();
    private int _generation = int.MinValue;
    private object? _owner;

    /// <summary>Gets the focus owner associated with one circuit scope boundary.</summary>
    internal static ScopeBlockedFocusOwnership For(ScopeBoundaryService scope) => Owners.GetValue(scope, static _ => new());

    /// <summary>Elects the first heading rendered for <paramref name="generation"/>.</summary>
    /// <param name="generation">The current scope generation.</param>
    /// <param name="claimant">The heading asking to take focus.</param>
    /// <returns><see langword="true"/> when this claimant owns focus for the generation.</returns>
    internal bool TryOwn(int generation, object claimant)
    {
        lock (_gate)
        {
            if (_generation != generation)
            {
                _generation = generation;
                _owner = claimant;
                return true;
            }

            return ReferenceEquals(_owner, claimant);
        }
    }
}
