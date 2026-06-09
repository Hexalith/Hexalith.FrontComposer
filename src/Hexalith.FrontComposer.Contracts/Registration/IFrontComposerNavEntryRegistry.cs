namespace Hexalith.FrontComposer.Contracts.Registration;

/// <summary>
/// Optional companion contract for <see cref="IFrontComposerRegistry"/> implementations that collect
/// declarative <see cref="FrontComposerNavEntry"/> items so the shell can render domain menus in the
/// global left navigation automatically.
/// </summary>
/// <remarks>
/// Existing <see cref="IFrontComposerRegistry"/> implementers do NOT need to adopt this interface.
/// Callers should use
/// <see cref="FrontComposerRegistryExtensions.AddNavEntry(IFrontComposerRegistry, FrontComposerNavEntry)"/>
/// and <see cref="FrontComposerRegistryExtensions.GetNavEntries(IFrontComposerRegistry)"/>, which fall
/// back to a no-op / empty result when the registry does not implement this companion interface —
/// preserving backward compatibility.
/// </remarks>
public interface IFrontComposerNavEntryRegistry
{
    /// <summary>
    /// Adds a declarative navigation entry to the registry.
    /// </summary>
    /// <param name="entry">The navigation entry to register.</param>
    void AddNavEntry(FrontComposerNavEntry entry);

    /// <summary>
    /// Returns all registered navigation entries.
    /// </summary>
    /// <returns>A read-only list of registered navigation entries.</returns>
    IReadOnlyList<FrontComposerNavEntry> GetNavEntries();
}
