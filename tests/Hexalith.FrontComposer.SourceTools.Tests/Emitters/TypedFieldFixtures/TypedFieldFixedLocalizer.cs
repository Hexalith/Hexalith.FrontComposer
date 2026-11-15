using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;
/// <summary>Preserves the generated localizer fallback for missing resources.</summary>
/// <typeparam name="T">The lifecycle state or localization resource type.</typeparam>
internal sealed class TypedFieldFixedLocalizer<T> : IStringLocalizer<T>
{
    /// <inheritdoc/>
    public LocalizedString this[string name] => new(name, name, resourceNotFound: true);
    /// <inheritdoc/>
    public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(CultureInfo.InvariantCulture, name, arguments), resourceNotFound: true);
    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
