using Hexalith.FrontComposer.Contracts.Downloads;
using Hexalith.FrontComposer.Shell.Services.Downloads;
using Hexalith.FrontComposer.Shell.Services.Feedback;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>Registers additive, transport-neutral UI services for an interactive circuit.</summary>
public static class FrontComposerFeedbackServiceExtensions
{
    /// <summary>Registers browser downloads and callback-based optimistic reconciliation.</summary>
    /// <param name="services">The host service collection.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFrontComposerFeedback(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<IFcBrowserDownloadService, FcBrowserDownloadService>();
        services.TryAddScoped<FcOptimisticReconciler>();
        return services;
    }
}
