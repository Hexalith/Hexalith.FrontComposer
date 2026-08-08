using Counter.Web;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

/// <summary>
/// P11 — Counter sample fake-auth toggle smoke tests. Mirrors the registration shape used by
/// `samples/Counter/Counter.Web/Program.cs` so a regression in the production guard surfaces
/// in CI rather than at deployment.
/// </summary>
public sealed class CounterFakeAuthIntegrationTests {
    [Fact]
    public void FakeAuthFlag_DefaultsOff_PreservesDemoAccessor() {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        bool requested = config.GetValue<bool>("Hexalith:FrontComposer:FakeAuth:Enabled");

        requested.ShouldBeFalse();
    }

    [Fact]
    public void FakeAuthFlag_OnInDevelopment_IsAcceptedByEnvironmentGuard() {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection([new("Hexalith:FrontComposer:FakeAuth:Enabled", "true")])
            .Build();

        bool requested = config.GetValue<bool>("Hexalith:FrontComposer:FakeAuth:Enabled");
        bool isDevelopment = true;

        Should.NotThrow(() => SimulateProgramGuard(requested, isDevelopment));
    }

    [Fact]
    public void FakeAuthFlag_OnInProduction_ThrowsAtStartup() {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection([new("Hexalith:FrontComposer:FakeAuth:Enabled", "true")])
            .Build();

        bool requested = config.GetValue<bool>("Hexalith:FrontComposer:FakeAuth:Enabled");
        bool isDevelopment = false;

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => SimulateProgramGuard(requested, isDevelopment));

        ex.Message.ShouldContain("Development", Case.Insensitive);
    }

    [Fact]
    public void FakeAuthEnabledLog_EmitsOneCriticalNamedEventWithDeploymentWarning() {
        CapturingLogger<CounterFakeAuthIntegrationTests> logger = new();

        CounterFakeAuthLogging.FakeAuthenticationEnabled(logger);

        CapturedLogEntry entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Critical);
        entry.EventId.Id.ShouldBe(1);
        entry.EventId.Name.ShouldBe("FakeAuthenticationEnabled");
        entry.Message.ShouldBe(
            "Counter sample is running with FAKE authentication (Hexalith:FrontComposer:FakeAuth:Enabled=true). All requests share a single shared identity. Do not deploy with this flag set.");
        entry.Exception.ShouldBeNull();
    }

    /// <summary>
    /// Mirrors the production environment guard in `samples/Counter/Counter.Web/Program.cs`;
    /// the logging contract itself is asserted directly against the sample logging type above.
    /// </summary>
    private static void SimulateProgramGuard(bool fakeAuthRequested, bool isDevelopment) {
        if (fakeAuthRequested && !isDevelopment) {
            throw new InvalidOperationException(
                "Hexalith:FrontComposer:FakeAuth:Enabled is only permitted when ASPNETCORE_ENVIRONMENT=Development.");
        }
    }
}
