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
        IResourceBuilder<ContainerResource> redis = Container(builder, configuration, "redis", "redis", "7.4-alpine", 6379)
            .WithBindMount(configuration.FilePath("redis.conf"), "/usr/local/etc/redis/verification.conf", isReadOnly: true)
            .WithEntrypoint("redis-server")
            .WithArgs("/usr/local/etc/redis/verification.conf");
        IResourceBuilder<ContainerResource> placement = Container(builder, configuration, "placement", "daprio/dapr", configuration.DaprVersion, 50005)
            .WithEntrypoint("./placement");
        IResourceBuilder<ContainerResource> scheduler = Container(builder, configuration, "scheduler", "daprio/dapr", configuration.DaprVersion, 50006)
            .WithEntrypoint("./scheduler")
            .WithArgs("--etcd-data-dir=/var/lock/dapr/scheduler", "--etcd-client-listen-address=0.0.0.0",
                "--override-broadcast-host-port=127.0.0.1:" + configuration.Port("scheduler"));
        IResourceBuilder<ProjectResource> gateway = builder.AddProject<HexalithEventStore>("eventstore")
            .WaitFor(redis).WaitFor(placement).WaitFor(scheduler);
        _ = builder.AddHexalithEventStore(gateway, adminServer: null, adminUI: null,
            eventStoreDaprConfigPath: configuration.FilePath("accesscontrol.yaml"),
            adminServerDaprConfigPath: null, resiliencyConfigPath: null,
            stateStoreComponentPath: configuration.FilePath("components/statestore.yaml"),
            eventStoreDaprHttpPort: configuration.Port("daprHttp"),
            daprPlacementHostAddress: "127.0.0.1:" + configuration.Port("placement"),
            daprSchedulerHostAddress: "127.0.0.1:" + configuration.Port("scheduler"),
            pubSubComponentPath: configuration.FilePath("components/pubsub.yaml"));
        IDaprSidecarResource sidecar = builder.Resources.OfType<IDaprSidecarResource>()
            .Single(resource => resource.Name == "eventstore-dapr");
        _ = builder.CreateResourceBuilder(sidecar).WithReference(builder.AddDaprComponent("configstore", "configuration.redis",
            new DaprComponentOptions { LocalPath = configuration.FilePath("components/configstore.yaml") }));
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
