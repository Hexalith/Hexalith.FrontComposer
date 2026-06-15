using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Resolves domain-contributed navigation labels (category names, nav-entry titles) to the request
/// culture at render time. Domains register entries once as culture-invariant data at startup; the
/// shell carries an optional resource marker <see cref="Type"/> plus a key on each entry/manifest and
/// resolves it here via <see cref="IStringLocalizerFactory.Create(Type)"/> so the menu honours the
/// same culture as the page body. The invariant <c>Title</c>/<c>Name</c> stays the fallback, keeping
/// derived test ids and sort order culture-stable.
/// </summary>
internal static class FcNavLocalization {
    /// <summary>
    /// Resolves <paramref name="key"/> against <paramref name="resource"/> for the current request
    /// culture, returning <paramref name="fallback"/> when localization is not configured for the
    /// entry (no resource type, no key) or the key is absent from the resource set.
    /// </summary>
    /// <param name="factory">The injected localizer factory.</param>
    /// <param name="resource">The domain resource marker type, or <see langword="null"/> when the label is not localized.</param>
    /// <param name="key">The resource key, or <see langword="null"/>/empty when the label is not localized.</param>
    /// <param name="fallback">The culture-invariant label rendered when no localized value resolves.</param>
    /// <returns>The localized label, or <paramref name="fallback"/>.</returns>
    public static string Resolve(IStringLocalizerFactory factory, Type? resource, string? key, string fallback) {
        if (factory is null || resource is null || string.IsNullOrEmpty(key)) {
            return fallback;
        }

        LocalizedString localized = factory.Create(resource)[key];
        return localized.ResourceNotFound ? fallback : localized.Value;
    }
}
