using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Immutable, framework-scoped command target captured before dispatch and associated after acceptance.
/// </summary>
public sealed class CommandTargetSnapshot : IEquatable<CommandTargetSnapshot>
{
    /// <summary>Initializes a validated command target snapshot.</summary>
    public CommandTargetSnapshot(
        string projectionTypeName,
        string viewKey,
        string entityKey,
        CommandTargetChangeKind changeKind,
        string? priorStatus,
        string? expectedStatus,
        string tenantId,
        string userId,
        DateTimeOffset capturedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (!Enum.IsDefined(changeKind))
        {
            throw new ArgumentOutOfRangeException(nameof(changeKind), changeKind, "The target change kind must be defined.");
        }

        string? normalizedPriorStatus = NormalizeOptional(priorStatus);
        string? normalizedExpectedStatus = NormalizeOptional(expectedStatus);
        if (changeKind == CommandTargetChangeKind.StatusMove
            && (normalizedPriorStatus is null || normalizedExpectedStatus is null))
        {
            throw new ArgumentException("StatusMove targets require both prior and expected statuses.", nameof(changeKind));
        }

        if (changeKind == CommandTargetChangeKind.StatusMove
            && string.Equals(normalizedPriorStatus, normalizedExpectedStatus, StringComparison.Ordinal))
        {
            throw new ArgumentException("StatusMove targets require different prior and expected statuses.", nameof(changeKind));
        }

        ProjectionTypeName = projectionTypeName.Trim();
        ViewKey = viewKey.Trim();
        EntityKey = entityKey.Trim();
        ChangeKind = changeKind;
        PriorStatus = normalizedPriorStatus;
        ExpectedStatus = normalizedExpectedStatus;
        TenantId = tenantId.Trim();
        UserId = userId.Trim();
        CapturedAt = capturedAt;
    }

    /// <summary>Gets the declared target projection type name.</summary>
    public string ProjectionTypeName { get; }

    /// <summary>Gets the canonical target view or lane key.</summary>
    public string ViewKey { get; }

    /// <summary>Gets the exact target entity key.</summary>
    public string EntityKey { get; }

    /// <summary>Gets the declared target change kind.</summary>
    public CommandTargetChangeKind ChangeKind { get; }

    /// <summary>Gets the optional prior status.</summary>
    public string? PriorStatus { get; }

    /// <summary>Gets the optional expected destination status.</summary>
    public string? ExpectedStatus { get; }

    /// <summary>Gets the framework-owned tenant scope captured before dispatch.</summary>
    public string TenantId { get; }

    /// <summary>Gets the framework-owned user scope captured before dispatch.</summary>
    public string UserId { get; }

    /// <summary>Gets the framework-owned capture time.</summary>
    public DateTimeOffset CapturedAt { get; }

    /// <summary>
    /// Compares target identity fields while deliberately excluding <see cref="CapturedAt"/>.
    /// </summary>
    public bool HasSameTarget(CommandTargetSnapshot? other) => Equals(other);

    /// <inheritdoc />
    public bool Equals(CommandTargetSnapshot? other) =>
        other is not null
        && string.Equals(ProjectionTypeName, other.ProjectionTypeName, StringComparison.Ordinal)
        && string.Equals(ViewKey, other.ViewKey, StringComparison.Ordinal)
        && string.Equals(EntityKey, other.EntityKey, StringComparison.Ordinal)
        && ChangeKind == other.ChangeKind
        && string.Equals(PriorStatus, other.PriorStatus, StringComparison.Ordinal)
        && string.Equals(ExpectedStatus, other.ExpectedStatus, StringComparison.Ordinal)
        && string.Equals(TenantId, other.TenantId, StringComparison.Ordinal)
        && string.Equals(UserId, other.UserId, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as CommandTargetSnapshot);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(
        StringComparer.Ordinal.GetHashCode(ProjectionTypeName),
        StringComparer.Ordinal.GetHashCode(ViewKey),
        StringComparer.Ordinal.GetHashCode(EntityKey),
        ChangeKind,
        PriorStatus is null ? 0 : StringComparer.Ordinal.GetHashCode(PriorStatus),
        ExpectedStatus is null ? 0 : StringComparer.Ordinal.GetHashCode(ExpectedStatus),
        StringComparer.Ordinal.GetHashCode(TenantId),
        StringComparer.Ordinal.GetHashCode(UserId));

    /// <summary>Determines whether two snapshots identify the same target.</summary>
    public static bool operator ==(CommandTargetSnapshot? left, CommandTargetSnapshot? right) =>
        ReferenceEquals(left, right) || left is not null && left.Equals(right);

    /// <summary>Determines whether two snapshots identify different targets.</summary>
    public static bool operator !=(CommandTargetSnapshot? left, CommandTargetSnapshot? right) => !(left == right);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
