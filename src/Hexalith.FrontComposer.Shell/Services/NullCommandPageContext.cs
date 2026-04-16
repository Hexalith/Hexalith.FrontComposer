using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Default <see cref="ICommandPageContext"/> used when the adopter has not registered a page-level
/// context. The FullPage renderer tolerates an empty context (D32 navigation falls through to <c>/</c>).
/// </summary>
public sealed class NullCommandPageContext : ICommandPageContext {
    public string CommandName => string.Empty;
    public string BoundedContext => string.Empty;
    public string? ReturnPath => null;
}
