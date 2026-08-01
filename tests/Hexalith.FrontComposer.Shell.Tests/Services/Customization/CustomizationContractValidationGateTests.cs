using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Customization;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Tests.Services.Diagnostics;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Customization;

public sealed class CustomizationContractValidationGateTests
{
    [Fact]
    public async Task StartAsync_WhenStrictValidationRejectsRegistration_LogsBeforeThrowing()
    {
        CustomizationContractRejectionLog rejectionLog = new();
        rejectionLog.Record(CustomizationContractMismatchDiagnosticProviderTests.NewRejection());
        CapturingLogger<CustomizationContractValidationGate> logger = new();
        CustomizationContractValidationGate sut = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions
            {
                CustomizationContractValidation = CustomizationContractValidationMode.FailClosedOnMajorMismatch,
            }),
            rejectionLog,
            logger,
            Substitute.For<IProjectionTemplateRegistry>(),
            Substitute.For<IProjectionSlotRegistry>(),
            Substitute.For<IProjectionViewOverrideRegistry>());

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.StartAsync(TestContext.Current.CancellationToken));

        CapturedLogEntry entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.EventId.Id.ShouldBe(5822);
        entry.EventId.Name.ShouldBe("CustomizationValidationFailed");
        entry.Exception.ShouldBeNull();
        entry.State["RejectionCount"].ShouldBe(1);
        ((string)entry.State["MessageDigest"]!).ShouldStartWith("sha256:");
        entry.Message.ShouldNotContain("Demo.CounterProjection");
        entry.Message.ShouldNotContain("Demo.CounterSlot");
        exception.Message.ShouldContain("Demo.CounterProjection");
    }
}
