namespace Hexalith.FrontComposer.Testing;

/// <summary>The five WCAG 2.5.8 exceptions; null means the measured target meets 24 by 24 CSS pixels.</summary>
public enum UxTargetSizeExceptionKind
{
    /// <summary>Inline evidence code.</summary>
    Inline,
    /// <summary>Spacing evidence code.</summary>
    Spacing,
    /// <summary>Equivalent evidence code.</summary>
    Equivalent,
    /// <summary>UserAgentControl evidence code.</summary>
    UserAgentControl,
    /// <summary>Essential evidence code.</summary>
    Essential,
}
