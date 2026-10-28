using System.Collections.Immutable;

using CommunityToolkit.Aspire.Hosting.Dapr;
using Hexalith.EventStore.Aspire;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.AppHost;

/// <summary>Composes an opt-in disposable gateway using the selected platform's existing isolation seams.</summary>
internal static class LocalReadinessTopology
{
    /// <summary>Creates only owned infrastructure and the gateway required by the readiness rehearsal.</summary>
    /// <returns>The gateway whose application health gates the verification Counter resource.</returns>
    internal static IResourceBuilder<ProjectResource> Add(
        IDistributedApplicationBuilder builder,
        LocalReadinessConfiguration configuration,
        HexalithEventStoreSecurityResources security,
        LocalWorkloadRealm realm,
        bool maintenance)
    {
        AddInfrastructure(builder, configuration);
        IResourceBuilder<ProjectResource> gateway = builder.AddProject<HexalithEventStore>("eventstore");
        WaitForInfrastructure(builder, gateway);
        _ = builder.AddHexalithEventStore(gateway, adminServer: null, adminUI: null,
            eventStoreDaprConfigPath: configuration.FilePath("accesscontrol.yaml"),
            adminServerDaprConfigPath: null, resiliencyConfigPath: null,
            stateStoreComponentPath: configuration.FilePath("components/statestore.yaml"),
            eventStoreDaprHttpPort: configuration.Port("daprHttp"),
            daprPlacementHostAddress: "127.0.0.1:" + configuration.Port("placement"),
            daprSchedulerHostAddress: "127.0.0.1:" + configuration.Port("scheduler"),
            pubSubComponentPath: configuration.FilePath("components/pubsub.yaml"));
        AddConfigurationStore(builder, configuration);
        _ = gateway.WithJwtBearerSecurity(security);
        IResourceBuilder<ParameterResource> secret = builder.AddParameter(LocalWorkloadRealm.SecretParameterName,
            () => realm.ClientSecret, secret: true, publishValueAsDefault: false);
        _ = gateway.WithEventStoreWorkloadClientCredentials(LocalWorkloadRealm.ClientId, secret)
            .WithEnvironment("EventStore__SignalR__Enabled", "true");
        if (maintenance)
        {
            // Only the maintenance lifecycle suppresses background projection effects.
            // Post-activation runs omit the override and use the selected gateway's ordinary workers.
            _ = gateway.WithEnvironment("EventStore__Projections__WorkersEnabled", "false");
        }
        IsolateSidecars(builder, configuration);
        return WithGatewayHealth(builder, gateway, maintenance);
    }

    /// <summary>Adds only the gateway's private configuration-store reference.</summary>
    internal static void AddConfigurationStore(IDistributedApplicationBuilder builder, LocalReadinessConfiguration configuration)
    {
        IDaprSidecarResource sidecar = builder.Resources.OfType<IDaprSidecarResource>()
            .Single(resource => resource.Name == "eventstore-dapr");
        _ = builder.CreateResourceBuilder(sidecar).WithReference(builder.AddDaprComponent("configstore", "configuration.redis",
            new DaprComponentOptions { LocalPath = configuration.FilePath("components/configstore.yaml") }));
    }

    /// <summary>Creates the same owned infrastructure for gateway-only and ordinary full composition.</summary>
    internal static void AddInfrastructure(IDistributedApplicationBuilder builder, LocalReadinessConfiguration configuration)
    {
        _ = Container(builder, configuration, "redis", "redis", "7.4-alpine", 6379)
            .WithBindMount(configuration.FilePath("redis.conf"), "/usr/local/etc/redis/verification.conf", isReadOnly: true)
            .WithEntrypoint("redis-server")
            .WithArgs("/usr/local/etc/redis/verification.conf");
        _ = Container(builder, configuration, "placement", "daprio/dapr", configuration.DaprVersion, 50005)
            .WithEntrypoint("./placement");
        _ = Container(builder, configuration, "scheduler", "daprio/dapr", configuration.DaprVersion, 50006)
            .WithEntrypoint("./scheduler")
            .WithArgs("--etcd-data-dir=/var/lock/dapr/scheduler", "--etcd-client-listen-address=0.0.0.0",
                "--override-broadcast-host-port=127.0.0.1:" + configuration.Port("scheduler"));
    }

    /// <summary>Orders a project behind the private infrastructure resources.</summary>
    internal static void WaitForInfrastructure(IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> project)
    {
        foreach (ContainerResource container in builder.Resources.OfType<ContainerResource>().Where(resource => resource.Name.StartsWith("verification-", StringComparison.Ordinal)))
            _ = project.WaitFor(builder.CreateResourceBuilder(container));
    }

    /// <summary>Overrides the platform's sidecar paths without reimplementing platform orchestration.</summary>
    internal static void IsolateSidecars(IDistributedApplicationBuilder builder, LocalReadinessConfiguration configuration)
    {
        foreach (IDaprSidecarResource sidecar in builder.Resources.OfType<IDaprSidecarResource>())
        {
            string name = sidecar.Name.EndsWith("-dapr", StringComparison.Ordinal) ? sidecar.Name[..^5] : throw new InvalidOperationException("Unknown disposable sidecar.");
            if (name is not "eventstore" and not "eventstore-admin" and not "eventstore-admin-ui" and not "tenants" and not "parties" and not "sample" and not "counter-scope-fixture")
                throw new InvalidOperationException("Unknown disposable application identity.");
            DaprSidecarOptionsAnnotation annotation = sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>().Single();
            DaprSidecarOptions options = annotation.Options;
            if (options.AppId != name) throw new InvalidOperationException("Disposable application identity changed.");
            // Logical workload identities remain unchanged. The private registry and
            // placement/scheduler scope those IDs to this rehearsal only.
            _ = sidecar.Annotations.Remove(annotation);
            _ = builder.CreateResourceBuilder(sidecar).WithOptions(options with
            {
                Config = configuration.FilePath(name == "eventstore-admin" ? "accesscontrol.eventstore-admin.yaml"
                    : name is "sample" or "counter-scope-fixture" ? "accesscontrol.sample.yaml" : "accesscontrol.yaml"),
                ResourcesPaths = ImmutableHashSet.Create(configuration.FilePath("empty")),
                PlacementHostAddress = "127.0.0.1:" + configuration.Port("placement"),
                SchedulerHostAddress = "127.0.0.1:" + configuration.Port("scheduler"),
                DaprListenAddresses = "127.0.0.1",
            });
        }
    }

    /// <summary>Refuses occupied fixed application ports before an owned full graph starts.</summary>
    internal static void RequireAvailableApplicationPorts(IDistributedApplicationBuilder builder)
    {
        foreach (ProjectResource project in builder.Resources.OfType<ProjectResource>())
        {
            foreach (EndpointAnnotation endpoint in project.Annotations.OfType<EndpointAnnotation>())
            {
                if (endpoint.Port is not int port) continue;
                using System.Net.Sockets.TcpListener listener = new(System.Net.IPAddress.Loopback, port);
                try { listener.Start(); }
                catch (System.Net.Sockets.SocketException)
                {
                    throw new InvalidOperationException("A fixed full-topology application port is occupied; no existing listener will be reused or stopped.");
                }
            }
        }
    }

    /// <summary>Attaches the gateway's phase-aware direct application readiness check.</summary>
    internal static IResourceBuilder<ProjectResource> WithGatewayHealth(IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> gateway, bool maintenance)
    {
        const string healthName = "disposable-application-readiness";
        _ = builder.Services.AddHealthChecks().AddCheck(healthName,
            new LocalReadinessHealthCheck(() => gateway.GetEndpoint("http").Url, maintenance));
        return gateway.WithHealthCheck(healthName);
    }

    private static IResourceBuilder<ContainerResource> Container(IDistributedApplicationBuilder builder,
        LocalReadinessConfiguration configuration, string name, string image, string version, int targetPort)
        => builder.AddContainer("verification-" + name, image, version)
            .WithContainerName("fc-verify-" + configuration.Owner + "-" + name)
            .WithLifetime(ContainerLifetime.Persistent)
            .WithContainerRuntimeArgs("--label", "hexalith.frontcomposer.verification=" + configuration.Owner,
                "--publish", "127.0.0.1:" + configuration.Port(name) + ":" + targetPort);
}
