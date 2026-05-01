using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal static class EventStoreIdentity {
    public static (string Tenant, string UserId) RequireUserContext(TenantContextSnapshot context) {
        ArgumentNullException.ThrowIfNull(context);
        // P8 — snapshot fields were validated when the snapshot was constructed (TenantContextSnapshot
        // ctor enforces non-empty + IsAuthenticated). Re-running RequireValidSegment here threw
        // ArgumentException, diverging from the rest of the tenant pipeline (TenantContextException).
        return (context.TenantId, context.UserId);
    }

    public static (string Tenant, string UserId) RequireUserContext(
        IUserContextAccessor userContextAccessor,
        string? requestedTenant = null) {
        string? authenticatedTenant = userContextAccessor.TenantId;
        if (string.IsNullOrWhiteSpace(authenticatedTenant)) {
            throw new InvalidOperationException(
                "EventStore requires an authenticated tenant context.");
        }

        if (!string.IsNullOrWhiteSpace(requestedTenant)
            && !string.Equals(requestedTenant, authenticatedTenant, StringComparison.Ordinal)) {
            throw new InvalidOperationException(
                "Requested tenant does not match the authenticated tenant context.");
        }

        string userId = userContextAccessor.UserId ?? string.Empty;

        return (
            EventStoreValidation.RequireNonColonSegment(authenticatedTenant, nameof(IUserContextAccessor.TenantId)),
            EventStoreValidation.RequireNonColonSegment(userId, nameof(IUserContextAccessor.UserId)));
    }

    public static string GetDomain(Type type) {
        string? value = type.GetCustomAttribute<BoundedContextAttribute>()?.Name;
        return EventStoreValidation.RequireNonColonSegment(NormalizeRouteSegment(value), "Domain");
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075:DynamicallyAccessedMembers",
        Justification = "FrontComposer command DTOs are runtime adopter types; EventStore adapter uses the established reflection-based metadata pattern.")]
    public static string GetAggregateId(object value) {
        foreach (string propertyName in new[] { "AggregateId", "Id", "Name" }) {
            PropertyInfo? property = value.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            object? candidate = property?.GetValue(value);
            if (candidate is not null && !string.IsNullOrWhiteSpace(Convert.ToString(candidate, System.Globalization.CultureInfo.InvariantCulture))) {
                return EventStoreValidation.RequireNonColonSegment(
                    Convert.ToString(candidate, System.Globalization.CultureInfo.InvariantCulture),
                    propertyName);
            }
        }

        throw new ArgumentException("EventStore commands require an AggregateId, Id, or Name value.", nameof(value));
    }

    public static string NormalizeRouteSegment(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return string.Empty;
        }

        string trimmed = value.Trim();
        StringBuilder builder = new(trimmed.Length + 8);
        for (int i = 0; i < trimmed.Length; i++) {
            char ch = trimmed[i];
            if (char.IsUpper(ch)) {
                if (i > 0 && builder.Length > 0 && builder[^1] != '-' && !char.IsUpper(trimmed[i - 1])) {
                    _ = builder.Append('-');
                }

                _ = builder.Append(char.ToLowerInvariant(ch));
            }
            else if (char.IsWhiteSpace(ch) || ch == '_') {
                if (builder.Length > 0 && builder[^1] != '-') {
                    _ = builder.Append('-');
                }
            }
            else {
                _ = builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder.ToString();
    }
}
