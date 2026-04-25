namespace Hexalith.FrontComposer.Contracts.Registration;

/// <summary>
/// Backward-compatible helpers for <see cref="IFrontComposerRegistry"/>.
/// </summary>
public static class FrontComposerRegistryExtensions
{
    /// <summary>
    /// Returns whether a generated FullPage route exists for the given command type
    /// (Story 3-4 D21 cross-story contract). Used by <c>FcCommandPalette</c> to filter unreachable
    /// command results out of the search index BEFORE they appear to the user — preventing a 404
    /// when the user activates a command whose generator did not emit a FullPage page.
    /// </summary>
    /// <remarks>
    /// The extension preserves backward compatibility with pre-3-4 <see cref="IFrontComposerRegistry"/>
    /// implementers: when the registry also implements <see cref="IFrontComposerFullPageRouteRegistry"/>
    /// the route-aware answer is used; otherwise the method returns <see langword="true"/> and the
    /// palette preserves the historical “surface every command” behaviour. Story 9-4 layers a
    /// build-time analyzer that catches missing FullPage routes independent of this runtime check.
    /// </remarks>
    /// <param name="registry">The registry to query.</param>
    /// <param name="commandTypeName">The fully qualified command type name (e.g., <c>Counter.Domain.IncrementCommand</c>).</param>
    /// <returns><see langword="true"/> when a FullPage route exists; <see langword="false"/> when the command is unreachable.</returns>
    public static bool HasFullPageRoute(this IFrontComposerRegistry registry, string commandTypeName)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        return registry is IFrontComposerFullPageRouteRegistry routeAware
            ? routeAware.HasFullPageRoute(commandTypeName)
            : true;
    }

    /// <summary>
    /// Returns whether the given command performs a write/state-change action and is therefore
    /// eligible to surface as an empty-state CTA (Story 4-6 D5 cross-story contract).
    /// </summary>
    /// <remarks>
    /// The extension preserves backward compatibility with pre-4-6 <see cref="IFrontComposerRegistry"/>
    /// implementers: when the registry also implements <see cref="IFrontComposerCommandWriteAccessRegistry"/>
    /// the write-aware answer is used; otherwise the method returns <see langword="true"/> and every
    /// registered command is treated as writable.
    /// </remarks>
    /// <param name="registry">The registry to query.</param>
    /// <param name="commandTypeName">The fully qualified command type name.</param>
    /// <returns><see langword="true"/> when the command is writable; <see langword="false"/> for query/read-only commands.</returns>
    public static bool IsCommandWritable(this IFrontComposerRegistry registry, string commandTypeName)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        return registry is IFrontComposerCommandWriteAccessRegistry writeAware
            ? writeAware.IsCommandWritable(commandTypeName)
            : true;
    }
}