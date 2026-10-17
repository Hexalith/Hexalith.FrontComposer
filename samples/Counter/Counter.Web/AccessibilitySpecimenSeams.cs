using Counter.Specimens;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Counter.Web;

/// <summary>Registers the fixed accessibility failure seams, only in the opt-in Test host.</summary>
internal static class AccessibilitySpecimenSeams
{
    public static IServiceCollection AddAccessibilitySpecimenSeams(this IServiceCollection services, IHostEnvironment environment)
    {
        if (!environment.IsEnvironment("Test"))
        {
            return services;
        }

        services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider, CounterSpecimenAuthenticationStateProvider>());
        services.AddScoped<AccessibilitySpecimenState>();
        services.AddScoped<AccessibilitySpecimenUserContextAccessor>();
        services.Replace(ServiceDescriptor.Scoped<Hexalith.FrontComposer.Contracts.Rendering.IUserContextAccessor>(sp => sp.GetRequiredService<AccessibilitySpecimenUserContextAccessor>()));
        services.AddScoped<Action<bool>>(sp => tenantChange => {
            AccessibilitySpecimenUserContextAccessor context = sp.GetRequiredService<AccessibilitySpecimenUserContextAccessor>();
            if (tenantChange) context.TenantId = context.TenantId == "counter-demo" ? "counter-ae-replacement" : "counter-demo";
            else context.UserId = context.UserId == "demo-user" ? "ae-replacement-user" : "demo-user";
            sp.GetRequiredService<Hexalith.FrontComposer.Shell.Services.ScopeBoundaryService>().Synchronize();
        });
        services.Replace(ServiceDescriptor.Scoped<Hexalith.FrontComposer.Shell.State.DataGridNavigation.IProjectionPageLoader, AccessibilitySpecimenPageLoader>());
        services.AddScoped<AccessibilitySpecimenPendingScope>();
        services.Replace(ServiceDescriptor.Scoped<Hexalith.FrontComposer.Shell.State.PendingCommands.ICommandExecutionAdmissionGate>(sp =>
            new Hexalith.FrontComposer.Shell.State.PendingCommands.CommandExecutionAdmissionGate(
                sp.GetRequiredService<Hexalith.FrontComposer.Shell.State.PendingCommands.IPendingCommandStateService>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<AccessibilitySpecimenPendingScope>())));
        services.AddScoped<Hexalith.FrontComposer.Shell.Services.Authorization.CommandAuthorizationEvaluator>();
        services.Replace(ServiceDescriptor.Scoped<Hexalith.FrontComposer.Shell.Services.Authorization.ICommandAuthorizationEvaluator, AccessibilitySpecimenAuthorizationEvaluator>());
        return services;
    }
}
