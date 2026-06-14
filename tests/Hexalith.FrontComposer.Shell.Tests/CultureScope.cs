using System.Globalization;

namespace Hexalith.FrontComposer.Shell.Tests;

internal sealed class CultureScope : IDisposable {
    private readonly CultureInfo _originalCulture;
    private readonly CultureInfo _originalUICulture;

    public CultureScope(string cultureName)
        : this(new CultureInfo(cultureName)) {
    }

    public CultureScope(CultureInfo culture) {
        _originalCulture = CultureInfo.CurrentCulture;
        _originalUICulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose() {
        CultureInfo.CurrentCulture = _originalCulture;
        CultureInfo.CurrentUICulture = _originalUICulture;
    }
}
