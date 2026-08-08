using System.Text;

using Hexalith.FrontComposer.SourceTools.Emitters;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 11.21 (CA1848) — unit coverage for the shared emitter that writes cached
/// <c>LoggerMessage.Define</c> delegates plus their private static wrappers into generated component
/// partials. The emitter had no direct tests; these pin both the emitted shape and the two
/// generation-time guards that keep it from producing source an adopter cannot compile.
/// </summary>
public sealed class GeneratedLogMethodEmitterTests {
    [Fact]
    public void Emit_WritesACachedDelegateAndAPrivateStaticWrapper() {
        StringBuilder sb = new();

        GeneratedLogMethodEmitter.Emit(
            sb,
            "LogCommandSubmitted",
            5904,
            "CommandFormSubmitted",
            "Information",
            "Command submitted. CorrelationId={CorrelationId}",
            hasException: false,
            ("string", "correlationId"));

        string emitted = sb.ToString();

        emitted.ShouldContain("private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, string, global::System.Exception?> _logCommandSubmitted =");
        emitted.ShouldContain("global::Microsoft.Extensions.Logging.LoggerMessage.Define<string>(");
        emitted.ShouldContain("global::Microsoft.Extensions.Logging.LogLevel.Information,");
        emitted.ShouldContain("new global::Microsoft.Extensions.Logging.EventId(5904, \"CommandFormSubmitted\"),");
        emitted.ShouldContain("private static void LogCommandSubmitted(global::Microsoft.Extensions.Logging.ILogger logger, string correlationId)");
        emitted.ShouldContain("=> _logCommandSubmitted(logger, correlationId, null);");
        ShouldBeAValidClassBody(emitted);
    }

    [Fact]
    public void Emit_WithAnException_PlacesTheLoggerFirstAndTheExceptionSecond() {
        StringBuilder sb = new();

        GeneratedLogMethodEmitter.Emit(
            sb,
            "LogAuthStateRefreshFailed",
            5900,
            "CommandFormAuthStateRefreshFailed",
            "Warning",
            "Refresh after auth-state-changed failed.",
            hasException: true);

        string emitted = sb.ToString();

        // Repository signature rule: ILogger first, Exception second, template arguments after.
        emitted.ShouldContain("private static void LogAuthStateRefreshFailed(global::Microsoft.Extensions.Logging.ILogger logger, global::System.Exception exception)");
        emitted.ShouldContain("=> _logAuthStateRefreshFailed(logger, exception);");

        // No template arguments means no generic argument list on Define.
        emitted.ShouldContain("global::Microsoft.Extensions.Logging.LoggerMessage.Define(");
        emitted.ShouldNotContain("Define<");
        ShouldBeAValidClassBody(emitted);
    }

    [Fact]
    public void Emit_WithAnExceptionAndTemplateParameters_KeepsExceptionSecondInTheWrapperSignature() {
        StringBuilder sb = new();

        GeneratedLogMethodEmitter.Emit(
            sb,
            "LogCommandValidationFailed",
            5908,
            "CommandFormValidationFailed",
            "Warning",
            "Validation failed. CorrelationId={CorrelationId}",
            hasException: true,
            ("string", "correlationId"));

        string emitted = sb.ToString();

        emitted.ShouldContain(
            "private static void LogCommandValidationFailed(global::Microsoft.Extensions.Logging.ILogger logger, global::System.Exception exception, string correlationId)");
        emitted.ShouldContain("=> _logCommandValidationFailed(logger, correlationId, exception);");
        emitted.ShouldContain("global::Microsoft.Extensions.Logging.LoggerMessage.Define<string>(");
        ShouldBeAValidClassBody(emitted);
    }

    [Fact]
    public void Emit_AtTheSixArgumentCeiling_StillEmits() {
        StringBuilder sb = new();

        GeneratedLogMethodEmitter.Emit(
            sb,
            "LogSixArguments",
            5999,
            "SixArguments",
            "Debug",
            "A={A} B={B} C={C} D={D} E={E} F={F}",
            hasException: false,
            ("string", "a"),
            ("string", "b"),
            ("string", "c"),
            ("string", "d"),
            ("string", "e"),
            ("string", "f"));

        sb.ToString().ShouldContain("Define<string, string, string, string, string, string>(");
        ShouldBeAValidClassBody(sb.ToString());
    }

    [Fact]
    public void Emit_AboveTheSixArgumentCeiling_FailsTheGenerationInsteadOfEmittingUncompilableSource() {
        // LoggerMessage.Define has no seven-argument overload; emitting one would fail the adopter
        // build (CS0308/CS1501) inside generated code the adopter cannot edit.
        StringBuilder sb = new();

        ArgumentOutOfRangeException thrown = Should.Throw<ArgumentOutOfRangeException>(() => GeneratedLogMethodEmitter.Emit(
            sb,
            "LogSevenArguments",
            5999,
            "SevenArguments",
            "Debug",
            "A={A} B={B} C={C} D={D} E={E} F={F} G={G}",
            hasException: false,
            ("string", "a"),
            ("string", "b"),
            ("string", "c"),
            ("string", "d"),
            ("string", "e"),
            ("string", "f"),
            ("string", "g")));

        thrown.Message.ShouldContain("LogSevenArguments");
        thrown.Message.ShouldContain("at most 6");
        sb.ToString().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Only one hole: {A}", 2)]
    [InlineData("Two holes: {A} {B}", 1)]
    [InlineData("No holes at all", 1)]
    [InlineData("Three holes: {A} {B} {C}", 2)]
    public void Emit_WhenPlaceholderCountDoesNotMatchParameterCount_FailsTheGeneration(
        string messageTemplate,
        int parameterCount) {
        // The XML contract on Emit has always required these to be equal; nothing enforced it, so a
        // mismatch shipped a message whose holes never bind to the values that were passed.
        StringBuilder sb = new();
        (string Type, string Name)[] parameters = new (string Type, string Name)[parameterCount];
        for (int index = 0; index < parameterCount; index++) {
            parameters[index] = ("string", "value" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        ArgumentException thrown = Should.Throw<ArgumentException>(() => GeneratedLogMethodEmitter.Emit(
            sb,
            "LogMismatch",
            5999,
            "Mismatch",
            "Debug",
            messageTemplate,
            hasException: false,
            parameters));

        thrown.Message.ShouldContain("LogMismatch");
        thrown.Message.ShouldContain("placeholder");
        sb.ToString().ShouldBeEmpty();
    }

    [Fact]
    public void Emit_WithAnUnterminatedPlaceholder_FailsTheGeneration() {
        StringBuilder sb = new();

        _ = Should.Throw<ArgumentException>(() => GeneratedLogMethodEmitter.Emit(
            sb,
            "LogUnterminated",
            5999,
            "Unterminated",
            "Debug",
            "Broken {Placeholder",
            hasException: false,
            ("string", "placeholder")));

        sb.ToString().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("no holes", 0)]
    [InlineData("{One}", 1)]
    [InlineData("{One} and {Two}", 2)]
    [InlineData("{{escaped}} is literal", 0)]
    [InlineData("{{escaped}} plus {Real}", 1)]
    [InlineData("}} trailing escape only", 0)]
    public void CountTemplatePlaceholders_TreatsDoubledBracesAsLiterals(string messageTemplate, int expected)
        => GeneratedLogMethodEmitter.CountTemplatePlaceholders(messageTemplate).ShouldBe(expected);

    [Fact]
    public void Emit_WithEscapedBraces_CountsOnlyTheRealHole() {
        StringBuilder sb = new();

        Should.NotThrow(() => GeneratedLogMethodEmitter.Emit(
            sb,
            "LogEscaped",
            5999,
            "Escaped",
            "Debug",
            "Literal {{braces}} around {Real}",
            hasException: false,
            ("string", "real")));

        ShouldBeAValidClassBody(sb.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Emit_WithMissingMethodName_FailsTheGenerationInsteadOfIndexingEmpty(string? methodName) {
        StringBuilder sb = new();

        ArgumentException thrown = Should.Throw<ArgumentException>(() => GeneratedLogMethodEmitter.Emit(
            sb,
            methodName!,
            5999,
            "EventName",
            "Debug",
            "no holes",
            hasException: false));

        thrown.ParamName.ShouldBe("methodName");
        sb.ToString().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Emit_WithMissingEventName_FailsTheGeneration(string? eventName) {
        StringBuilder sb = new();

        ArgumentException thrown = Should.Throw<ArgumentException>(() => GeneratedLogMethodEmitter.Emit(
            sb,
            "LogSomething",
            5999,
            eventName!,
            "Debug",
            "no holes",
            hasException: false));

        thrown.ParamName.ShouldBe("eventName");
        sb.ToString().ShouldBeEmpty();
    }

    [Fact]
    public void Emit_EscapesQuotesInEventNameTheSameWayAsTheMessageTemplate() {
        StringBuilder sb = new();

        GeneratedLogMethodEmitter.Emit(
            sb,
            "LogQuoted",
            5999,
            "NameWith\"Quote",
            "Debug",
            "Message with \"quotes\"",
            hasException: false);

        string emitted = sb.ToString();
        emitted.ShouldContain("new global::Microsoft.Extensions.Logging.EventId(5999, \"NameWith\\\"Quote\"),");
        emitted.ShouldContain("\"Message with \\\"quotes\\\"\"");
        ShouldBeAValidClassBody(emitted);
    }

    /// <summary>
    /// Parses the emitted fragment inside a container class so a malformed member surfaces here
    /// rather than in a consumer's generated output.
    /// </summary>
    /// <param name="emitted">The emitted class-body fragment.</param>
    private static void ShouldBeAValidClassBody(string emitted) {
        string document = "internal static class Host\r\n{\r\n" + emitted + "}\r\n";
        Diagnostic[] errors = [.. CSharpSyntaxTree.ParseText(document)
            .GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];

        errors.ShouldBeEmpty(
            "The emitted logging member does not parse: "
            + string.Join(" | ", errors.Select(diagnostic => diagnostic.ToString())));
    }
}
