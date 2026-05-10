using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hexalith.FrontComposer.Shell.Components.Specimens;

/// <summary>
/// Route contract for browser-level accessibility and visual specimens.
/// </summary>
public static class FrontComposerSpecimenRoutes {
    /// <summary>Configuration key that explicitly enables specimen route discovery.</summary>
    public const string EnabledConfigurationKey = "Hexalith:FrontComposer:Specimens:Enabled";

    /// <summary>Story key owning the current specimen route contract.</summary>
    public const string OwnerStoryKey = "10-2-accessibility-ci-gates-and-visual-specimen-verification";

    /// <summary>Route for the type, theme, density, navigation, lifecycle, and badge specimen.</summary>
    public const string TypeSpecimen = "/__frontcomposer/specimens/type";

    /// <summary>Route for deterministic data-formatting specimen coverage.</summary>
    public const string DataFormattingSpecimen = "/__frontcomposer/specimens/data-formatting";

    /// <summary>
    /// Returns whether the host may expose specimen routes.
    /// </summary>
    public static bool IsEnabled(IConfiguration configuration, IHostEnvironment environment) {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        return string.Equals(configuration[EnabledConfigurationKey], "true", StringComparison.OrdinalIgnoreCase)
            && (environment.IsDevelopment() || environment.IsEnvironment("Test"));
    }
}
