using Hexalith.FrontComposer.Contracts.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hexalith.FrontComposer.Shell.Services.Diagnostics;

internal static class CustomizationDiagnosticPublisher {
    /// <summary>Determines whether customization diagnostics may render or publish.</summary>
    /// <param name="services">The render service provider, or null when unavailable.</param>
    /// <returns>True only for a DEBUG build with an explicitly Development host environment.</returns>
    /// <remarks>Missing hosting metadata fails closed, including direct panel rendering.</remarks>
    internal static bool IsEnabled(IServiceProvider? services)
    {
#if DEBUG
        return services?.GetService<IHostEnvironment>()?.IsDevelopment() == true;
#else
        return false;
#endif
    }

    public static void Publish(IDiagnosticSink? sink, CustomizationDiagnostic diagnostic) {
        if (sink is null) {
            return;
        }

        sink.Publish(new DevDiagnosticEvent(
            diagnostic.Id,
            "Customization",
            CustomizationDiagnosticFormatter.Format(diagnostic),
            DateTimeOffset.UtcNow));
    }
}
