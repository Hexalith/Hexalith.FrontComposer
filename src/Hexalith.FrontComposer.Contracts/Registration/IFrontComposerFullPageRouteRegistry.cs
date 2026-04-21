namespace Hexalith.FrontComposer.Contracts.Registration;

/// <summary>
/// Optional companion contract for <see cref="IFrontComposerRegistry"/> implementations that can
/// answer whether a generated FullPage route exists for a given command type (Story 3-4 D21).
/// </summary>
/// <remarks>
/// Existing <see cref="IFrontComposerRegistry"/> implementers do NOT need to adopt this interface.
/// Callers should use <see cref="FrontComposerRegistryExtensions.HasFullPageRoute(IFrontComposerRegistry, string)"/>
/// which falls back to <see langword="true"/> when the registry does not implement this companion
/// interface — preserving the pre-3-4 behaviour.
/// </remarks>
public interface IFrontComposerFullPageRouteRegistry
{
    /// <summary>
    /// Returns whether a generated FullPage route exists for the given command type.
    /// </summary>
    /// <param name="commandTypeName">The fully qualified command type name (e.g., <c>Counter.Domain.IncrementCommand</c>).</param>
    /// <returns><see langword="true"/> when a FullPage route exists; <see langword="false"/> when the command is unreachable.</returns>
    bool HasFullPageRoute(string commandTypeName);
}