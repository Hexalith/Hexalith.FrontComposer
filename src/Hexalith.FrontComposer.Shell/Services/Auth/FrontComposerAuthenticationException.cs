namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>
/// Sanitized authentication bridge failure. The message intentionally carries only diagnostic
/// metadata and fix guidance, never tokens, claims, subjects, emails, or provider payloads.
/// </summary>
public sealed class FrontComposerAuthenticationException(string diagnosticId, string message) : InvalidOperationException(message) {
    public string DiagnosticId { get; } = diagnosticId;
}
