namespace Hexalith.FrontComposer.Shell.Tests.Registration;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Registration;

[BoundedContext("Logging")]
public sealed class LoggingDomain
{
}

public static class PartialLoggingRegistration
{
    public static DomainManifest Manifest { get; } = new(
        "Logging",
        "Logging",
        [],
        []);
}
