using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Provides mutable, explicit scope values for the Epic 9 transition matrix.</summary>
internal sealed class Epic9UserContextAccessor : IUserContextAccessor
{
    /// <inheritdoc />
    public string? TenantId { get; set; } = "test-tenant";

    /// <inheritdoc />
    public string? UserId { get; set; } = "test-user";
}
