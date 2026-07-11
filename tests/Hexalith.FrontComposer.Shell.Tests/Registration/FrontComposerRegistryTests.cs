using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Registration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Registration;

public class FrontComposerRegistryTests {

    [Fact]
    public void AddHexalithDomain_CounterAssembly_UsesGeneratedBoundedContextFallback() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithDomain<CounterDomain>();

        using ServiceProvider sp = services.BuildServiceProvider();
        IFrontComposerRegistry registry = sp.GetRequiredService<IFrontComposerRegistry>();

        IReadOnlyList<DomainManifest> manifests = registry.GetManifests();
        manifests.Single(m => m.BoundedContext == "Counter")
            .Projections.ShouldContain(typeof(CounterProjection).FullName!);
        manifests.Single(m => m.BoundedContext == "Default")
            .Commands.ShouldContain(typeof(IncrementCommand).FullName!);
    }

    [Fact]
    public void NavEntries_AreReturnedSortedByOrderThenTitleOrdinal() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        using ServiceProvider sp = services.BuildServiceProvider();
        IFrontComposerRegistry registry = sp.GetRequiredService<IFrontComposerRegistry>();

        registry.AddNavEntry(new FrontComposerNavEntry("tenants", "Zebra", "/z", Order: 2));
        registry.AddNavEntry(new FrontComposerNavEntry("tenants", "Apple", "/a", Order: 2));
        registry.AddNavEntry(new FrontComposerNavEntry("tenants", "Middle", "/m", Order: 1));

        registry.GetNavEntries().Select(static e => e.Title).ShouldBe(["Middle", "Apple", "Zebra"]);
    }

    [Fact]
    public void GetNavEntries_OnRegistryWithoutEntries_ReturnsEmpty() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        using ServiceProvider sp = services.BuildServiceProvider();
        IFrontComposerRegistry registry = sp.GetRequiredService<IFrontComposerRegistry>();

        registry.GetNavEntries().ShouldBeEmpty();
    }

    [Fact]
    public void AddHexalithDomain_PartialRegistration_LogsWarning() {
        ServiceCollection services = new();
        CollectingLoggerProvider loggerProvider = new();
        _ = services.AddLogging(builder => builder.AddProvider(loggerProvider));
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithDomain<LoggingDomain>();

        using ServiceProvider sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<IFrontComposerRegistry>();

        loggerProvider.Messages.ShouldContain(message => message.Contains("PartialLoggingRegistration", StringComparison.Ordinal));
    }

    [Fact]
    public void AddHexalithFrontComposer_RegistersRegistry() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        using ServiceProvider sp = services.BuildServiceProvider();

        IFrontComposerRegistry registry = sp.GetRequiredService<IFrontComposerRegistry>();
        _ = registry.ShouldNotBeNull();
    }

    [Fact]
    public void AddHexalithFrontComposer_DoesNotForceLocalization_AdopterOwnsIt() {
        // Since 2026-04-15 Shell no longer FrameworkReferences Microsoft.AspNetCore.App
        // and no longer calls AddLocalization inside AddHexalithFrontComposer. The adopter is
        // expected to call services.AddLocalization() themselves when they need IStringLocalizer.
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddLocalization();
        using ServiceProvider sp = services.BuildServiceProvider();

        IStringLocalizer<FrontComposerRegistryTests> localizer = sp.GetRequiredService<IStringLocalizer<FrontComposerRegistryTests>>();
        _ = localizer.ShouldNotBeNull();
    }

    [Fact]
    public void AddHexalithFrontComposer_WithFluxorOptions_BackwardCompatible() {
        // Parameterless call (backward compatible)
        ServiceCollection services1 = new();
        _ = services1.AddHexalithFrontComposer();
        using ServiceProvider sp1 = services1.BuildServiceProvider();
        _ = sp1.GetRequiredService<IFrontComposerRegistry>().ShouldNotBeNull();

        // Call with fluxor options
        ServiceCollection services2 = new();
        bool optionsInvoked = false;
        _ = services2.AddHexalithFrontComposer(o => optionsInvoked = true);
        using ServiceProvider sp2 = services2.BuildServiceProvider();
        _ = sp2.GetRequiredService<IFrontComposerRegistry>().ShouldNotBeNull();
        optionsInvoked.ShouldBeTrue();
    }

    [Fact]
    public void Registry_RegisterDomain_ManifestRetrievable() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        using ServiceProvider sp = services.BuildServiceProvider();

        IFrontComposerRegistry registry = sp.GetRequiredService<IFrontComposerRegistry>();
        DomainManifest manifest = new("Counter", "Counter", ["Counter.Domain.CounterProjection"], []);
        registry.RegisterDomain(manifest);

        IReadOnlyList<DomainManifest> manifests = registry.GetManifests();
        manifests.Count.ShouldBe(1);
        manifests[0].BoundedContext.ShouldBe("Counter");
        manifests[0].Projections.ShouldContain("Counter.Domain.CounterProjection");
    }

    [Fact]
    public void GetManifests_ReturnsSnapshot_NotLiveBackingList() {
        FrontComposerRegistry registry = new([], [], NullLogger<FrontComposerRegistry>.Instance);
        registry.RegisterDomain(new DomainManifest("Counter", "Counter", ["Counter.Projection"], ["Counter.CommandA"]));

        IReadOnlyList<DomainManifest> snapshot = registry.GetManifests();
        registry.RegisterDomain(new DomainManifest("Counter", "Counter", ["Counter.OtherProjection"], ["Counter.CommandB"]));

        snapshot[0].Commands.ShouldBe(["Counter.CommandA"]);
        registry.GetManifests()[0].Commands.ShouldContain("Counter.CommandB");
    }

    [Fact]
    public void RegisterDomain_MergeWithNullIncomingCollections_DoesNotThrow() {
        // H-F3 — the merge branch (second registration of a bounded context) must tolerate null
        // incoming Projections / Commands the same way Clone and ValidateManifests do; the previous
        // code threw ArgumentNullException from Concat under the registry lock at host startup.
        FrontComposerRegistry registry = new([], [], NullLogger<FrontComposerRegistry>.Instance);
        registry.RegisterDomain(new DomainManifest("Counter", "Counter", ["Counter.Projection"], ["Counter.CommandA"]));

        Should.NotThrow(() => registry.RegisterDomain(new DomainManifest("Counter", "Counter", null!, null!)));

        DomainManifest merged = registry.GetManifests().Single(m => m.BoundedContext == "Counter");
        merged.Projections.ShouldBe(["Counter.Projection"]);
        merged.Commands.ShouldBe(["Counter.CommandA"]);
    }

    [Fact]
    public void Registry_ReadsUseSnapshotsDuringConcurrentRegistration() {
        FrontComposerRegistry registry = new([], [], NullLogger<FrontComposerRegistry>.Instance);

        Parallel.For(0, 200, i => {
            registry.RegisterDomain(new DomainManifest(
                "Counter",
                "Counter",
                [$"Counter.Projection{i}"],
                [$"Counter.Command{i}"]));
            _ = registry.GetManifests();
            _ = registry.GetNavEntries();
            _ = registry.HasFullPageRoute($"Counter.Command{i}");
        });

        registry.GetManifests()[0].Commands.Count.ShouldBe(200);
    }

    [Fact]
    public void HasFullPageRoute_GeneratedMembership_DistinguishesCommandDensity() {
        FrontComposerRegistry registry = new([], [], NullLogger<FrontComposerRegistry>.Instance);
        registry.RegisterDomain(new DomainManifest(
            "Counter",
            "Counter",
            [],
            ["Counter.IncrementCommand", "Counter.ConfigureCounterCommand"]) {
            FullPageCommands = ["Counter.ConfigureCounterCommand"],
        });

        registry.HasFullPageRoute("Counter.IncrementCommand").ShouldBeFalse();
        registry.HasFullPageRoute("Counter.ConfigureCounterCommand").ShouldBeTrue();
        registry.HasFullPageRoute("Counter.UnknownCommand").ShouldBeFalse();
    }

    [Fact]
    public void HasFullPageRoute_LegacyManifest_AssumesRegisteredCommandIsReachable() {
        FrontComposerRegistry registry = new([], [], NullLogger<FrontComposerRegistry>.Instance);
        registry.RegisterDomain(new DomainManifest("Counter", "Counter", [], ["Counter.LegacyCommand"]));

        registry.HasFullPageRoute("Counter.LegacyCommand").ShouldBeTrue();
        registry.GetManifests().Single().FullPageCommands.ShouldBeNull();
    }

    [Fact]
    public void RegisterDomain_MergeLegacyAndGeneratedMetadata_PreservesPerCommandReachability() {
        FrontComposerRegistry registry = new([], [], NullLogger<FrontComposerRegistry>.Instance);
        registry.RegisterDomain(new DomainManifest("Counter", "Counter", [], ["Counter.LegacyCommand"]));
        registry.RegisterDomain(new DomainManifest(
            "Counter",
            "Counter",
            [],
            ["Counter.InlineCommand", "Counter.FullPageCommand"]) {
            FullPageCommands = ["Counter.FullPageCommand"],
        });

        DomainManifest merged = registry.GetManifests().Single();
        merged.FullPageCommands.ShouldBe(["Counter.LegacyCommand", "Counter.FullPageCommand"]);
        registry.HasFullPageRoute("Counter.LegacyCommand").ShouldBeTrue();
        registry.HasFullPageRoute("Counter.InlineCommand").ShouldBeFalse();
        registry.HasFullPageRoute("Counter.FullPageCommand").ShouldBeTrue();
    }

    [Fact]
    public void RegisterDomain_ClonesKnownEmptyMembership() {
        List<string> fullPageCommands = [];
        FrontComposerRegistry registry = new([], [], NullLogger<FrontComposerRegistry>.Instance);
        registry.RegisterDomain(new DomainManifest(
            "Counter",
            "Counter",
            [],
            ["Counter.InlineCommand"]) {
            FullPageCommands = fullPageCommands,
        });

        fullPageCommands.Add("Counter.InlineCommand");

        DomainManifest snapshot = registry.GetManifests().Single();
        snapshot.FullPageCommands.ShouldNotBeNull();
        snapshot.FullPageCommands.ShouldBeEmpty();
        registry.HasFullPageRoute("Counter.InlineCommand").ShouldBeFalse();
    }

    [Fact]
    public void RegisterDomain_ExplicitMetadataOverridesLegacyAssumptionInEitherOrder() {
        DomainManifest legacy = new("Counter", "Counter", [], ["Counter.InlineCommand"]);
        DomainManifest generated = new("Counter", "Counter", [], ["Counter.InlineCommand"]) {
            FullPageCommands = [],
        };

        FrontComposerRegistry legacyFirst = new([], [], NullLogger<FrontComposerRegistry>.Instance);
        legacyFirst.RegisterDomain(legacy);
        legacyFirst.RegisterDomain(generated);

        FrontComposerRegistry generatedFirst = new([], [], NullLogger<FrontComposerRegistry>.Instance);
        generatedFirst.RegisterDomain(generated);
        generatedFirst.RegisterDomain(legacy);

        legacyFirst.HasFullPageRoute("Counter.InlineCommand").ShouldBeFalse();
        generatedFirst.HasFullPageRoute("Counter.InlineCommand").ShouldBeFalse();
    }

    [Fact]
    public void Constructor_InvalidFullPageMembership_ThrowsHfc1601() {
        DomainManifest invalid = new("Counter", "Counter", [], ["Counter.KnownCommand"]) {
            FullPageCommands = ["Counter.UnknownCommand"],
        };
        DomainRegistrationAction action = new(registry => registry.RegisterDomain(invalid));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            _ = new FrontComposerRegistry([action], [], NullLogger<FrontComposerRegistry>.Instance));

        exception.Message.ShouldContain("HFC1601");
    }

    [Fact]
    public void RegisterDomain_LateInvalidFullPageMembership_ThrowsHfc1601() {
        FrontComposerRegistry registry = new([], [], NullLogger<FrontComposerRegistry>.Instance);
        DomainManifest invalid = new("Counter", "Counter", [], ["Counter.KnownCommand"]) {
            FullPageCommands = ["Counter.UnknownCommand"],
        };

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => registry.RegisterDomain(invalid));

        exception.Message.ShouldContain("HFC1601");
        registry.GetManifests().ShouldBeEmpty();
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider {
        private readonly List<string> _messages = [];

        public IReadOnlyList<string> Messages => _messages;

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(_messages);

        public void Dispose() {
        }

        private sealed class CollectingLogger(List<string> messages) : ILogger {

            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull
                => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) => messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
