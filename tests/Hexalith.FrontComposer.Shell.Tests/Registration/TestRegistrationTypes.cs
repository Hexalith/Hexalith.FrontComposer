
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.FrontComposer.Shell.Tests.Registration;

[BoundedContext("Logging")]
public sealed class LoggingDomain {
}

public static class PartialLoggingRegistration {
    public static DomainManifest Manifest { get; } = new(
        "Logging",
        "Logging",
        [],
        []);
}
