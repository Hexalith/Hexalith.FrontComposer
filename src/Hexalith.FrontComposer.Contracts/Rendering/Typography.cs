#if NET10_0_OR_GREATER
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Framework-owned typography tokens mapping the 9 UX roles (UX-DR26) onto Fluent UI Blazor
/// v5 <see cref="TextSize"/> + <see cref="TextWeight"/> + <see cref="TextTag"/> + <see cref="TextFont"/>
/// primitives (Story 3-1 D2 / AC5). Consumed as:
/// <code>
/// &lt;FluentText Size="@Typography.ViewTitle.Size"
///             Weight="@Typography.ViewTitle.Weight"
///             As="@Typography.ViewTitle.Tag"&gt;Order List&lt;/FluentText&gt;
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// <b>Living-table version-pinning policy (D11):</b>
/// <list type="bullet">
///   <item><description>Patch version bump (x.y.Z → x.y.Z+1): mapping MUST NOT change.</description></item>
///   <item><description>Minor version bump (x.Y.z → x.Y+1.0): mapping change allowed, requires
///     changelog entry + before/after screenshot committed to <c>docs/typography-baseline/</c>.</description></item>
///   <item><description>Major version bump (X.y.z → X+1.0.0): restructurable with migration notes.</description></item>
/// </list>
/// The current mapping version is <see cref="ContractsMetadata.TypographyMappingVersion"/> and is
/// snapshot-locked in <c>TypographyConstantsTests.cs</c>.
/// </para>
/// <para>
/// <b>Spec adaptation:</b> Story 3-1 D2 originally named 9 Fluent UI v5 <c>Typography</c> enum
/// values (<c>Title1</c>, <c>Subtitle1</c>, <c>Title3</c>, <c>Subtitle2</c>, <c>Body1Strong</c>,
/// <c>Body1</c>, <c>Body2</c>, <c>Caption1</c>). That enum exists in Fluent UI's React library but
/// NOT in the Blazor SDK (<c>Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.2-26098.1</c>), where
/// the primitives are <c>TextSize</c>/<c>TextWeight</c>/<c>TextTag</c>. The token pair model keeps
/// the 9 adopter-visible constants + living-table discipline intact; consumers bind primitives
/// directly on <c>FluentText</c>.
/// </para>
/// </remarks>
public static class Typography
{
    /// <summary>Application title (UX-DR26 role #1). Mapped to Size700 (28 px), Bold, H1.</summary>
    public static readonly FcTypoToken AppTitle = new(TextSize.Size700, TextWeight.Bold, TextTag.H1);

    /// <summary>Bounded-context heading (UX-DR26 role #2). Mapped to Size500 (20 px), Semibold, H2.</summary>
    public static readonly FcTypoToken BoundedContextHeading = new(TextSize.Size500, TextWeight.Semibold, TextTag.H2);

    /// <summary>View title (UX-DR26 role #3). Mapped to Size600 (24 px), Semibold, H2.</summary>
    public static readonly FcTypoToken ViewTitle = new(TextSize.Size600, TextWeight.Semibold, TextTag.H2);

    /// <summary>Section heading (UX-DR26 role #4). Mapped to Size400 (16 px), Semibold, H3.</summary>
    public static readonly FcTypoToken SectionHeading = new(TextSize.Size400, TextWeight.Semibold, TextTag.H3);

    /// <summary>Form field label (UX-DR26 role #5). Mapped to Size300 (14 px), Semibold, Span.</summary>
    public static readonly FcTypoToken FieldLabel = new(TextSize.Size300, TextWeight.Semibold, TextTag.Span);

    /// <summary>Body copy (UX-DR26 role #6). Mapped to Size300 (14 px), Regular, Span.</summary>
    public static readonly FcTypoToken Body = new(TextSize.Size300, TextWeight.Regular, TextTag.Span);

    /// <summary>Secondary body copy (UX-DR26 role #7). Mapped to Size200 (12 px), Regular, Span.</summary>
    public static readonly FcTypoToken Secondary = new(TextSize.Size200, TextWeight.Regular, TextTag.Span);

    /// <summary>Caption (UX-DR26 role #8). Mapped to Size200 (12 px), Regular, Span.</summary>
    public static readonly FcTypoToken Caption = new(TextSize.Size200, TextWeight.Regular, TextTag.Span);

    /// <summary>
    /// Monospace code fragment (UX-DR26 role #9). Mapped to Size300 (14 px), Regular, Span,
    /// Monospace. The companion <see cref="TypographyStyle.CodeFontFamily"/> string is available
    /// for non-FluentText consumers that need the fallback family stack inline.
    /// </summary>
    public static readonly FcTypoToken Code = new(TextSize.Size300, TextWeight.Regular, TextTag.Span, TextFont.Monospace);
}

/// <summary>
/// Immutable typography token binding a Fluent UI v5 <see cref="TextSize"/> +
/// <see cref="TextWeight"/> + <see cref="TextTag"/> (+ optional <see cref="TextFont"/>) set
/// into a single addressable constant. See <see cref="Typography"/> for the 9 framework-owned roles.
/// </summary>
/// <param name="Size">Fluent UI text size (Size100–Size1000).</param>
/// <param name="Weight">Fluent UI text weight (Regular / Medium / Semibold / Bold).</param>
/// <param name="Tag">HTML element tag emitted by <c>FluentText</c>.</param>
/// <param name="Font">Optional font family override (Base / Numeric / Monospace).</param>
public readonly record struct FcTypoToken(
    TextSize Size,
    TextWeight Weight,
    TextTag Tag,
    TextFont? Font = null);
#endif
