using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Routing;

/// <summary>Identifies a routed page instance independently of query and tab selection.</summary>
public static class RouteComponentIdentity
{
    /// <summary>Builds a stable identity from the page type and route parameters other than the conventional Tab and RouteTab selectors.</summary>
    public static string For(RouteData routeData)
    {
        ArgumentNullException.ThrowIfNull(routeData);
        return routeData.PageType.FullName + string.Concat(routeData.RouteValues
            .Where(pair => !string.Equals(pair.Key, "Tab", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(pair.Key, "RouteTab", StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"|{pair.Key.Length}:{pair.Key}:{Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture)?.Length}:{pair.Value}"));
    }
}
