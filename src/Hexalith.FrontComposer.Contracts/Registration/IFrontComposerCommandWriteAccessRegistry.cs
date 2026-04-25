namespace Hexalith.FrontComposer.Contracts.Registration;

/// <summary>
/// Optional companion contract for <see cref="IFrontComposerRegistry"/> implementations that can
/// answer whether a registered command performs a write/state-change (Story 4-6 D5).
/// </summary>
/// <remarks>
/// Existing <see cref="IFrontComposerRegistry"/> implementers do NOT need to adopt this interface.
/// Callers should use <see cref="FrontComposerRegistryExtensions.IsCommandWritable(IFrontComposerRegistry, string)"/>
/// which falls back to <see langword="true"/> when the registry does not implement this companion
/// interface — preserving the historical "every registered command is treated as writable" behaviour.
/// </remarks>
public interface IFrontComposerCommandWriteAccessRegistry
{
    /// <summary>
    /// Returns whether the given command performs a write/state-change action (vs. a read-only
    /// query) and is therefore eligible to surface as an empty-state CTA.
    /// </summary>
    /// <param name="commandTypeName">The fully qualified command type name.</param>
    /// <returns><see langword="true"/> when the command writes/changes state; <see langword="false"/> for query/read-only commands.</returns>
    bool IsCommandWritable(string commandTypeName);
}
