namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>
/// Sanitized authentication bridge failure. The message intentionally carries only diagnostic
/// metadata and fix guidance, never tokens, claims, subjects, emails, or provider payloads.
/// </summary>
public sealed class FrontComposerAuthenticationException : InvalidOperationException {
    public FrontComposerAuthenticationException(string diagnosticId, string message)
        : base(message) {
        DiagnosticId = diagnosticId;
    }

    /// <summary>P26 — preserve the inner cause so operators can diagnose the root failure (network error vs token endpoint vs config) while the user-visible message stays sanitized.</summary>
    public FrontComposerAuthenticationException(string diagnosticId, string message, Exception? innerException)
        : base(message, innerException) {
        DiagnosticId = diagnosticId;
    }

    public string DiagnosticId { get; }
}
