using System.Globalization;
using System.Reflection;

using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Configures the adopter-facing FrontComposer component test host.
/// </summary>
public sealed class FrontComposerTestOptions {
    /// <summary>Default tenant id used by fake user context and captured evidence.</summary>
    public string TestTenantId { get; set; } = "test-tenant";

    /// <summary>Default user id used by fake user context and captured evidence.</summary>
    public string TestUserId { get; set; } = "test-user";

    /// <summary>Default bounded context used by command-page context and evidence.</summary>
    public string BoundedContext { get; set; } = "Test";

    /// <summary>Default command name used when a generated command component has no page context override.</summary>
    public string CommandName { get; set; } = "Test Command";

    /// <summary>Optional return path exposed through <see cref="ICommandPageContext"/>.</summary>
    public string? ReturnPath { get; set; }

    /// <summary>Culture applied to the current test context during setup.</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>Clock used by lifecycle wrappers, fake evidence, and generated formatting tests.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>How the Fluxor store should be initialized by helper APIs.</summary>
    public StoreInitializationMode StoreInitialization { get; set; } = StoreInitializationMode.OnDemand;

    /// <summary>bUnit JSInterop mode applied during setup.</summary>
    public JSRuntimeMode JSInteropMode { get; set; } = JSRuntimeMode.Loose;

    /// <summary>Generated domain assemblies to scan with Fluxor and the FrontComposer domain registrar.</summary>
    public IList<Assembly> DomainAssemblies { get; } = [];

    /// <summary>Maximum number of evidence records retained by fake providers.</summary>
    public int MaxEvidenceRecords { get; set; } = 100;

    /// <summary>Maximum payload length included in redacted diagnostics.</summary>
    public int MaxDiagnosticPayloadCharacters { get; set; } = 256;
}

/// <summary>
/// Controls when the test host initializes the Fluxor store.
/// </summary>
public enum StoreInitializationMode {
    /// <summary>Call <see cref="FrontComposerTestBase.InitializeStoreAsync"/> explicitly before rendering.</summary>
    OnDemand,

    /// <summary>Initialize the store after the default host registrations are complete.</summary>
    DuringHostSetup,
}
