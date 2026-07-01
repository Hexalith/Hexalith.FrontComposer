using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Bundles the browser document title and the visible route-level page header for FrontComposer
/// adopters. Consuming domains provide localized strings and optional page-specific fragments.
/// </summary>
/// <remarks>
/// <para>
/// <b>Landmark contract (handoff <c>frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening</c>):</b>
/// the header root is a plain <c>&lt;header&gt;</c> element scoped <b>inside</b> the shell's single
/// <c>#fc-main-content</c> <c>main</c> landmark. Because an HTML <c>&lt;header&gt;</c> that is a
/// descendant of <c>main</c> (or any sectioning element) is <b>not</b> a <c>banner</c> landmark, the
/// page header no longer creates a competing global <c>banner</c> per route — the shell header owns
/// the single page-level <c>banner</c>. The header therefore advertises <c>role="presentation"</c>
/// only when it would otherwise be exposed as a top-level banner; see <see cref="LandmarkRole"/>.
/// </para>
/// <para>
/// <b>Naming the shell main landmark:</b> set <see cref="HeadingId"/> and pass the same id to
/// <c>FrontComposerShell.ContentLabelledBy</c>. The shell main landmark is then named by the route
/// heading without any page-level <c>aria-labelledby</c> on a non-landmark wrapper (which would be
/// orphaned / ignored by assistive technology).
/// </para>
/// <para>
/// <b>Blank-heading fail-safe (Requested outcome 3):</b> a blank or whitespace <see cref="Heading"/>
/// is tolerated. When <see cref="Heading"/> is blank the visible <c>&lt;h1&gt;</c> is suppressed so
/// the route never renders a dangling empty heading element (an empty heading is itself a WCAG
/// failure); this is pinned by <c>FcPageHeader_WithBlankHeading_RendersNoDanglingHeadingElement</c>.
/// Consumers that require a heading should keep passing a non-blank value: a blank
/// <see cref="Heading"/> then surfaces diagnostically at the focus-restore call rather than silently,
/// because <see cref="FocusHeadingAsync"/> throws when the heading is suppressed (pinned by
/// <c>FcPageHeader_FocusHeadingAsync_WithBlankHeading_FailsDiagnostically</c>).
/// </para>
/// </remarks>
public sealed partial class FcPageHeader : ComponentBase {
    private ElementReference _headingElement;

    /// <summary>The browser document title rendered through Blazor <c>PageTitle</c>.</summary>
    [Parameter] public string? PageTitle { get; set; }

    /// <summary>The visible route-level heading text.</summary>
    [Parameter] public string Heading { get; set; } = string.Empty;

    /// <summary>Optional short context text rendered above the heading.</summary>
    [Parameter] public string? Eyebrow { get; set; }

    /// <summary>Optional descriptive text rendered below the heading.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Optional id assigned to the route-level heading.</summary>
    [Parameter] public string? HeadingId { get; set; }

    /// <summary>Optional tabindex assigned to the route-level heading when it is a focus target.</summary>
    [Parameter] public int? HeadingTabIndex { get; set; }

    /// <summary>Optional stable test selector for the header root.</summary>
    [Parameter] public string TestId { get; set; } = "fc-page-header";

    /// <summary>Optional additional CSS classes for the header root.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Optional page actions rendered in the header title row.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Optional page metadata or return-context content rendered below the description.</summary>
    [Parameter] public RenderFragment? Metadata { get; set; }

    /// <summary>Additional attributes applied to the header root.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Whether the visible route heading is rendered. <see langword="false"/> when
    /// <see cref="Heading"/> is blank or whitespace, in which case the <c>&lt;h1&gt;</c> is suppressed
    /// so the route never carries a dangling empty heading element (Requested outcome 3).
    /// </summary>
    private bool HasHeading => !string.IsNullOrWhiteSpace(Heading);

    /// <summary>
    /// The role advertised on the header root so it never becomes a competing top-level
    /// <c>banner</c> landmark (Requested outcome 2). A bare <c>&lt;header&gt;</c> nested inside the
    /// shell's <c>main</c> landmark is already non-banner per the HTML-AAM rules, but adopters that
    /// render <see cref="FcPageHeader"/> outside a sectioning ancestor (e.g. isolated component tests
    /// or a bespoke layout) would still expose a banner. Emitting <c>role="presentation"</c> keeps the
    /// header purely visual while preserving its heading semantics, so exactly one banner exists per
    /// page (owned by the shell), independent of how the adopter nests the header.
    /// </summary>
    private const string LandmarkRole = "presentation";

    /// <summary>
    /// Moves focus to the route-level heading. Pages use this when restoring context after navigation.
    /// </summary>
    /// <remarks>
    /// A heading is not focusable by default, so focusing one requires a <c>tabindex</c>. When
    /// <see cref="HeadingTabIndex"/> is omitted the heading carries no <c>tabindex</c> and a browser
    /// focus call would silently no-op, breaking the post-navigation focus-restore contract. To fail
    /// diagnostically rather than silently (Requested outcome 5) this method throws
    /// <see cref="InvalidOperationException"/> when no <see cref="HeadingTabIndex"/> is set instead of
    /// attempting a focus that cannot succeed.
    /// <para>
    /// <b>Behavior change for external adopters:</b> before the landmark/contract hardening this method
    /// silently no-oped when the heading was not focusable; it now throws (above). Adopters upgrading
    /// must set <see cref="HeadingTabIndex"/> (typically <c>-1</c>) on any header used as a focus-restore
    /// target. Note the <c>FcAggregateListPage</c> wrapper's <c>… ?? ValueTask.CompletedTask</c> guards
    /// only the pre-first-render null <c>@ref</c> window — it does <b>not</b> swallow this throw.
    /// </para>
    /// </remarks>
    /// <returns>A task that completes once focus has moved to the heading.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="HeadingTabIndex"/> is <see langword="null"/>, because the heading is not
    /// a focusable target without an explicit <c>tabindex</c>.
    /// </exception>
    public ValueTask FocusHeadingAsync() {
        if (!HasHeading) {
            throw new InvalidOperationException(
                $"{nameof(FcPageHeader)}.{nameof(FocusHeadingAsync)} has no heading element to focus because "
                + $"{nameof(Heading)} is blank — the heading is suppressed by the blank-heading fail-safe. Provide a "
                + "non-blank Heading before using the header as a focus target.");
        }

        if (HeadingTabIndex is null) {
            throw new InvalidOperationException(
                $"{nameof(FcPageHeader)}.{nameof(FocusHeadingAsync)} requires {nameof(HeadingTabIndex)} to be set "
                + "(typically -1) so the route heading is a focusable target. Without it the browser focus call is a "
                + "silent no-op and post-navigation focus restoration fails. Set HeadingTabIndex=\"-1\" on the header.");
        }

        return _headingElement.FocusAsync();
    }

    private string ResolvedPageTitle
        => string.IsNullOrWhiteSpace(PageTitle) ? Heading : PageTitle;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "fc-page-header" : $"fc-page-header {Class}";
}
