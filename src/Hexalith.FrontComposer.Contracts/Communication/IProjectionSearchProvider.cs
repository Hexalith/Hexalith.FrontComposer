namespace Hexalith.FrontComposer.Contracts.Communication;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Story 4-3 D6 / AC4 — adopter-implemented global search hook for a projection type.
/// Resolved via nullable DI from the generated view; when unregistered the
/// <c>FcProjectionGlobalSearch</c> component is omitted from the render tree
/// entirely (UX-DR40 "framework hook, not framework feature").
/// </summary>
/// <typeparam name="T">The projection type whose rows are searched.</typeparam>
public interface IProjectionSearchProvider<T> {
    /// <summary>
    /// Executes the adopter's search for <paramref name="query"/>.
    /// Called once per debounce-complete keystroke from the generated view.
    /// </summary>
    /// <param name="query">The user-supplied search query. Non-null; may be whitespace.</param>
    /// <param name="cancellationToken">Cancelled on component dispose.</param>
    /// <returns>The matching projection rows in ranked order.</returns>
    Task<IReadOnlyList<T>> SearchAsync(string query, CancellationToken cancellationToken);
}
