using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

/// <summary>
/// Verifies the canonical correlation pseudonym shared by Shell logging families.
/// </summary>
public sealed class FrontComposerLogPseudonymizerTests
{
    private const string NormalizedIdentifier = "correlation-α";
    private const string ExpectedIdentifierToken = "sha256:d136298b82b4a556";

    [Fact]
    public void LogFamilies_SameIdentifier_EmitJoinablePseudonym()
    {
        const string Input = "  correlation-α  ";
        CapturingLogger<FrontComposerLogPseudonymizerTests> logger = new();
        CapturingLogger<LifecycleStateService> lifecycleLogger = new();

        FrontComposerDiagnosticLog.AbandonmentGuardSuppressedWhileSubmitting(logger, "HFC2100", Input);
        FrontComposerDiagnosticLog.ScopeReadinessStorageReadyDispatched(logger, Input);
        FrontComposerHotPathLog.LifecycleUnexpectedCorrelation(logger, "HFC2100", Input);
        using LifecycleStateService service = new(
            Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()),
            logger: lifecycleLogger);
        service.Transition(Input, CommandLifecycleState.Submitting);

        string[] tokens =
        [
            logger.Entries.Single(static entry => entry.EventId.Id == 6004).State["Cid"].ShouldBeOfType<string>(),
            logger.Entries.Single(static entry => entry.EventId.Id == 6070).State["CorrelationId"].ShouldBeOfType<string>(),
            logger.Entries.Single(static entry => entry.EventId.Id == 5700).State["Cid"].ShouldBeOfType<string>(),
            lifecycleLogger.Entries.Single(static entry => entry.EventId.Id == 5640)
                .State["CorrelationId"].ShouldBeOfType<string>(),
        ];

        tokens.ShouldAllBe(static token => token == ExpectedIdentifierToken);
        tokens.ShouldAllBe(static token => Regex.IsMatch(
            token,
            "^sha256:[0-9a-f]{16}$",
            RegexOptions.CultureInvariant));
        logger.Entries.ShouldAllBe(static entry =>
            !entry.Message.Contains(NormalizedIdentifier, StringComparison.Ordinal));
        lifecycleLogger.Entries.ShouldAllBe(static entry =>
            !entry.Message.Contains(NormalizedIdentifier, StringComparison.Ordinal));
        FrontComposerLogPseudonymizer.Pseudonymize("different-correlation")
            .ShouldNotBe(ExpectedIdentifierToken);
    }

    [Fact]
    public void Pseudonymize_TrimmedUnicodeInput_NormalizesOnlyWhitespace()
    {
        FrontComposerLogPseudonymizer.Pseudonymize("  correlation-α  ")
            .ShouldBe(ExpectedIdentifierToken);
        FrontComposerLogPseudonymizer.Pseudonymize(NormalizedIdentifier)
            .ShouldBe(ExpectedIdentifierToken);
        FrontComposerLogPseudonymizer.Pseudonymize("correlation-é")
            .ShouldNotBe(FrontComposerLogPseudonymizer.Pseudonymize("correlation-é"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void Pseudonymize_MissingInput_ReturnsAbsent(string? value)
    {
        FrontComposerLogPseudonymizer.Pseudonymize(value).ShouldBe("absent");
    }

    [Fact]
    public void Pseudonymize_OversizedInputs_HashesCompleteUtf8WithBoundedTemporaryMemory()
    {
        string sharedPrefix = new('x', 1_000_000);
        string first = sharedPrefix + 'a';
        string second = sharedPrefix + 'b';
        string surrogateBoundary = new string('x', 1023) + "🚀" + new string('y', 1024);
        _ = FrontComposerLogPseudonymizer.Pseudonymize("warmup");

        long before = GC.GetAllocatedBytesForCurrentThread();
        string firstToken = FrontComposerLogPseudonymizer.Pseudonymize(first);
        string secondToken = FrontComposerLogPseudonymizer.Pseudonymize(second);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        firstToken.ShouldNotBe(secondToken);
        firstToken.ShouldBe(ExpectedToken(first));
        secondToken.ShouldBe(ExpectedToken(second));
        FrontComposerLogPseudonymizer.Pseudonymize(surrogateBoundary)
            .ShouldBe(ExpectedToken(surrogateBoundary));
        allocated.ShouldBeLessThan(32_768L);
    }

    private static string ExpectedToken(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value.Trim());
        byte[] digest = SHA256.HashData(bytes);
        try
        {
            return "sha256:" + Convert.ToHexStringLower(digest.AsSpan(0, 8));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(digest);
            CryptographicOperations.ZeroMemory(bytes);
        }
    }
}
