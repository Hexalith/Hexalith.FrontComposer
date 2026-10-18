using System.Globalization;
using System.Resources;

using Hexalith.FrontComposer.Shell.Resources;

namespace Hexalith.FrontComposer.Testing;

/// <summary>Canonical announcement identities resolved from Shell localization resources.</summary>
public static class UxAnnouncementCatalog
{
    private static readonly ResourceManager Resources = new(typeof(FcShellResources));

    /// <summary>Creates an expectation from canonical copy. AM-26 requires a supported heading resource key; AM-30 accepts "offline".</summary>
    public static UxAnnouncement Expected(string rowId, string operationId, string stateIdentity,
        object[]? arguments = null, CultureInfo? culture = null, string? variant = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateIdentity);
        string resource = rowId switch
        {
            "AM-01" => "Am01Loading", "AM-02" => "Am02Empty", "AM-03" => "Am03Loaded",
            "AM-04" => "Am04Stale", "AM-05" => "Am05Reconnecting", "AM-06" => "Am06Fallback",
            "AM-07" => "Am07Recovery", "AM-08" => "Am08SlowQuery", "AM-09" => "Am09MaxItems",
            "AM-10" => "Am10Submitting", "AM-11" => "Am11Acknowledged", "AM-12" => "Am12Syncing",
            "AM-13" => "Am13Confirmed", "AM-14" => "Am14Rejected", "AM-15" => "Am15NeedsReview",
            "AM-16" => "Am16Warning", "AM-17" => "Am17Degraded",
            "AM-18" => variant == "single" ? "ValidationSummaryMessageSingle" : "ValidationSummaryMessage",
            "AM-19" => variant == "single" ? "MappedRejectionSummaryMessageSingle" : "MappedRejectionSummaryMessage",
            "AM-20" => "CommandBlockedMessage", "AM-21" => "Am21UpdatedRow", "AM-22" => "Am22DetailHidden",
            "AM-23" => "RouteNavigationFailedNamedText", "AM-24" => "Am24Exhausted", "AM-25" => "ShellNoModulesText",
            "AM-26" => variant is "ScopeBlockedHeading" or "RouteForbiddenHeading" or "UnauthorizedCommandWarningTitle"
                or "UnauthenticatedCommandWarningTitle" or "AuthorizationActionUnavailableTitle" ? variant
                : throw new ArgumentException("AM-26 requires a supported safe heading resource key.", nameof(variant)),
            "AM-27" => "Am27NoMatches", "AM-28" => "PaletteNoCommandsOrPagesText", "AM-29" => "StartupFailureText",
            "AM-30" => variant == "offline" ? "Am30Offline" : "Am30QueryFailed", "AM-31" => "ModuleTabUnavailableTemplate",
            _ => throw new ArgumentOutOfRangeException(nameof(rowId), "Expected AM-01 through AM-31."),
        };
        CultureInfo resolved = culture ?? CultureInfo.CurrentUICulture;
        string template = Resources.GetString(resource, resolved)
            ?? throw new InvalidOperationException("Canonical announcement resource is missing.");
        string copy = string.Format(resolved, template, arguments ?? []);
        UxAnnouncementChannel channel = rowId switch
        {
            "AM-18" or "AM-19" => UxAnnouncementChannel.FocusedSummary,
            "AM-26" or "AM-29" => UxAnnouncementChannel.FocusedHeading,
            "AM-28" => UxAnnouncementChannel.PoliteComboboxStatus,
            _ => UxAnnouncementChannel.PoliteStatus,
        };
        return new(rowId, copy, channel, operationId, stateIdentity);
    }
}
