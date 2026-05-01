using Hexalith.FrontComposer.Contracts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

/// <summary>
/// Story 7-2 DN1 — production-environment guardrail for synthetic tenant contexts. Fails
/// startup when <see cref="FcShellOptions.AllowDemoTenantContext"/> is enabled in the
/// Production environment so the demo/synthetic tenant accessor cannot ship to production
/// regardless of host wiring mistakes.
/// </summary>
/// <remarks>
/// Resolves <see cref="IHostEnvironment"/> lazily via <see cref="IServiceProvider"/> so test
/// hosts that omit hosting can still register this validator (validation no-ops when the
/// environment cannot be determined).
/// </remarks>
internal sealed class FcShellTenantOptionsValidator : IValidateOptions<FcShellOptions> {
    private readonly IServiceProvider _services;

    public FcShellTenantOptionsValidator(IServiceProvider services) {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
    }

    public ValidateOptionsResult Validate(string? name, FcShellOptions options) {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.AllowDemoTenantContext) {
            return ValidateOptionsResult.Success;
        }

        IHostEnvironment? environment = _services.GetService<IHostEnvironment>();
        if (environment is not null && environment.IsProduction()) {
            return ValidateOptionsResult.Fail(
                "FcShellOptions.AllowDemoTenantContext must not be enabled in the Production environment. "
                + "Demo/synthetic tenant identifiers are reserved for Development and Test hosts.");
        }

        return ValidateOptionsResult.Success;
    }
}
