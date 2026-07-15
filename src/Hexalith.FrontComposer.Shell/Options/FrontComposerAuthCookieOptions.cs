namespace Hexalith.FrontComposer.Shell.Options;

/// <summary>P10 — cookie scheme security policy (Story 7-1 NFR20/NFR21 BFF cookie posture).</summary>
public sealed class FrontComposerAuthCookieOptions {
    /// <summary>When true (default), cookies are issued with `Secure=Always` outside Development; SameSite=Lax; HttpOnly=true; SlidingExpiration=false.</summary>
    public bool ApplySecureDefaults { get; set; } = true;
}
