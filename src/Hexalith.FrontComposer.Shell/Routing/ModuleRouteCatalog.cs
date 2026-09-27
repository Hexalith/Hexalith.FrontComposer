using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.FrontComposer.Shell.Routing;

/// <summary>Builds the single Module destination for a bounded context.</summary>
internal static class ModuleRouteCatalog
{
    /// <summary>Returns the Module alias for a registered manifest.</summary>
    internal static string BuildRoute(DomainManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return BuildRoute(manifest.BoundedContext);
    }

    /// <summary>Returns the Module alias for a bounded context.</summary>
    internal static string BuildRoute(string boundedContext)
    {
        if (!IsValidSegment(boundedContext))
        {
            throw new ArgumentException("A Module bounded context must be a route-safe segment.", nameof(boundedContext));
        }

        return $"/{boundedContext.ToLowerInvariant()}";
    }

    /// <summary>Whether a bounded context can be represented by one unescaped route segment.</summary>
    internal static bool IsValidSegment(string? boundedContext)
        => !string.IsNullOrEmpty(boundedContext)
            && char.IsAsciiLetterOrDigit(boundedContext[0])
            && boundedContext.All(static value => char.IsAsciiLetterOrDigit(value) || value is '-' or '_');
}
