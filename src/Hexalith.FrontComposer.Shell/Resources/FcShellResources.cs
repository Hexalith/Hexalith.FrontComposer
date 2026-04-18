namespace Hexalith.FrontComposer.Shell.Resources;

/// <summary>
/// Marker type for the shell's framework-generated UI strings (Story 3-1 D12 / AC7).
/// Consumed by <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/> to resolve
/// the embedded <c>FcShellResources.resx</c> (EN default) and <c>FcShellResources.fr.resx</c>
/// (French) satellites.
/// </summary>
/// <remarks>
/// The ASP.NET Core resource resolution convention uses <c>typeof(T).FullName</c> as the resx
/// BaseName. With this class in <c>Hexalith.FrontComposer.Shell.Resources</c>, the resx files
/// embedded under <c>Resources/FcShellResources.resx</c> match by namespace + class name.
/// </remarks>
public sealed class FcShellResources {
}
