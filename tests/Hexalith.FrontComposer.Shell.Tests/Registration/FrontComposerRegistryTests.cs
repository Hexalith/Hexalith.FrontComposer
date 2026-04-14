using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Registration;

public class FrontComposerRegistryTests {
    [Fact]
    public void AddHexalithFrontComposer_RegistersRegistry() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        using ServiceProvider sp = services.BuildServiceProvider();

        IFrontComposerRegistry registry = sp.GetRequiredService<IFrontComposerRegistry>();
        _ = registry.ShouldNotBeNull();
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
    public void AddHexalithDomain_CounterAssembly_MergesProjectionAndCommands() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithDomain<CounterDomain>();

        using ServiceProvider sp = services.BuildServiceProvider();
        IFrontComposerRegistry registry = sp.GetRequiredService<IFrontComposerRegistry>();

        DomainManifest manifest = registry.GetManifests().Single(m => m.BoundedContext == "Counter");
        manifest.Projections.ShouldContain(typeof(CounterProjection).FullName!);
        manifest.Commands.ShouldContain(typeof(IncrementCommand).FullName!);
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
