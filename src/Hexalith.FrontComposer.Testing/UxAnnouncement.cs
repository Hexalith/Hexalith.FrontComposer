namespace Hexalith.FrontComposer.Testing;

/// <summary>A caller-observed speech event; identifiers remain machine identities even when copy is shared.</summary>
/// <param name="RowId">Canonical AM-01 through AM-31 row.</param>
/// <param name="Copy">Observed localized message.</param>
/// <param name="Channel">Observed speech path.</param>
/// <param name="OperationId">Opaque operation, connection epoch, load, or palette session identity.</param>
/// <param name="StateIdentity">Opaque state/result identity used to detect duplicate events.</param>
public sealed record UxAnnouncement(string RowId, string Copy, UxAnnouncementChannel Channel, string OperationId, string StateIdentity);
