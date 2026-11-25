using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Services.DerivedValues;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal static class EventStoreIdentity {
    private static readonly ConditionalWeakTable<object, ConditionalWeakTable<Type, Lazy<PropertyInfo?>[]>> AggregateIdCaches = new();

    public static (string Tenant, string UserId) RequireUserContext(TenantContextSnapshot context) {
        ArgumentNullException.ThrowIfNull(context);
        return (context.TenantId, context.UserId);
    }

    public static string GetDomain(Type type) {
        string? value = type.GetCustomAttribute<BoundedContextAttribute>()?.Name;
        return EventStoreValidation.RequireNonColonSegment(NormalizeRouteSegment(value), "Domain");
    }

    public static string GetAggregateId(object value) {
        var cache = AggregateIdCaches.GetOrCreateValue(DerivedValueMetadataUpdateHandler.Generation);
        Lazy<PropertyInfo?>[] properties = cache.GetValue(
            value.GetType(),
            static type => [
                new(() => FindAggregateIdProperty(type, "AggregateId")),
                new(() => FindAggregateIdProperty(type, "Id")),
                new(() => FindAggregateIdProperty(type, "Name")),
            ]);
        foreach (Lazy<PropertyInfo?> lookup in properties) {
            PropertyInfo? property = lookup.Value;
            object? candidate = property?.GetValue(value);
            if (candidate is not null
                && Convert.ToString(candidate, CultureInfo.InvariantCulture) is { } id
                && !string.IsNullOrWhiteSpace(id)) {
                return EventStoreValidation.RequireNonColonSegment(id, property!.Name);
            }
        }

        throw new ArgumentException("EventStore commands require an AggregateId, Id, or Name value.", nameof(value));
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2070:DynamicallyAccessedMembers",
        Justification = "FrontComposer command DTOs are runtime adopter types; EventStore adapter uses the established reflection-based metadata pattern.")]
    private static PropertyInfo? FindAggregateIdProperty(Type type, string name)
        => type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

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
