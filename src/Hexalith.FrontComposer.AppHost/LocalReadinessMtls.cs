using System.Text.Json;

using Hexalith.Builds.Tooling.Runtime;

namespace Hexalith.FrontComposer.AppHost;

/// <summary>Enrolls canonical peer identity orchestration and records owned disposable cleanup paths.</summary>
internal static class LocalReadinessMtls
{
    /// <summary>Adds the canonical private Sentry and actor control plane to the completed model.</summary>
    /// <param name="builder">The owning AppHost builder.</param>
    /// <param name="configuration">Optional disposable verification ownership.</param>
    /// <returns>The canonical helper's private run and issuer directories.</returns>
    internal static (string RunDirectory, string CertificateDirectory) Add(
        IDistributedApplicationBuilder builder,
        LocalReadinessConfiguration? configuration)
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../.."));
        if (configuration is not null)
        {
            configuration.RequireActivation();
            if (CompositionToolchainPins.DaprRuntimeVersion != configuration.DaprVersion)
                throw new InvalidOperationException("Installed verification Dapr differs from the preserved catalog runtime.");
            builder.Configuration["Dapr:Mtls:CertificateDirectory"] = configuration.FilePath("mtls");
        }
        (string RunDirectory, string CertificateDirectory) owned = DaprSelfHostedMtls.AddLocalApplicationInfrastructure(
            builder, repositoryRoot, Path.Combine(builder.AppHostDirectory, "DaprComponents", "sentry.yaml"));
        if (configuration is not null)
        {
            configuration.ValidateMtlsDirectories(owned.RunDirectory, owned.CertificateDirectory);
            foreach (ContainerResource resource in builder.Resources.OfType<ContainerResource>()
                .Where(resource => resource.Name is "dapr-sentry" or "dapr-placement-mtls" or "dapr-scheduler-mtls"))
            {
                // Retain the canonical logical/DNS resource names. Ownership is verified
                // through this exact AppHost's container ID, image, listeners and labels.
                _ = builder.CreateResourceBuilder(resource).WithContainerRuntimeArgs(
                    "--label", "hexalith.frontcomposer.verification=" + configuration.Owner,
                    "--label", "hexalith.frontcomposer.verification.instance=" + configuration.Instance);
            }
            string record = configuration.FilePath("mtls-plane.json");
            if (File.Exists(record)) throw new InvalidOperationException("Prior private mTLS recovery metadata remains; cleanup must be verified before restart.");
            File.WriteAllText(record, JsonSerializer.Serialize(new
            {
                owner = configuration.Owner, instance = configuration.Instance,
                runDirectory = owned.RunDirectory, certificateDirectory = owned.CertificateDirectory,
            }));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(record, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        return owned;
    }

    /// <summary>Removes ordinary private files after disposal; the verification runner proves container absence first.</summary>
    /// <param name="owned">The canonical helper's generated paths.</param>
    /// <param name="configuration">Optional verification ownership delegated to the runner.</param>
    /// <returns>A task completing after bounded ordinary-directory cleanup.</returns>
    internal static async Task CleanupAsync((string RunDirectory, string CertificateDirectory) owned,
        LocalReadinessConfiguration? configuration)
    {
        if (configuration is not null) return;
        bool issuerRemoved = await CompositionWorkspace.TryDeleteAsync(owned.CertificateDirectory, CancellationToken.None).ConfigureAwait(false);
        bool runRemoved = issuerRemoved && await CompositionWorkspace.TryDeleteAsync(owned.RunDirectory, CancellationToken.None).ConfigureAwait(false);
        if (!runRemoved) throw new InvalidOperationException("Private canonical Dapr recovery files were retained because cleanup could not be confirmed.");
    }
}
