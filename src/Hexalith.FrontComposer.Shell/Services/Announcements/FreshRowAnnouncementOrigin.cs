namespace Hexalith.FrontComposer.Shell.Services.Announcements;

/// <summary>Retains an entry's originating scope without keeping removed entries alive.</summary>
/// <param name="Scope">The first observed resolved tenant and user, or an unresolved origin.</param>
/// <param name="Generation">The scope generation in which the entry was first observed.</param>
internal sealed record FreshRowAnnouncementOrigin((string Tenant, string User)? Scope, long Generation);
