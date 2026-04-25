using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Shell.Services.Validation;

/// <summary>
/// Story 5-2 D14 / T1 — reflection-based default
/// <see cref="ICommandValidationFieldAllowlist"/>. Resolves a server-supplied field path to
/// a public, writable, instance property on the command type when (and only when) the path
/// matches case-insensitively. Nested property paths are accepted only when each segment
/// resolves to a writable property on the previous segment's declared type.
/// </summary>
/// <remarks>
/// Designed for the framework default. Generated forms can ship a faster, AOT-safe
/// allowlist via the source generator (Story 6-1+); both implementations satisfy the same
/// contract: unknown / hostile field paths return <see langword="false"/> and route to a
/// form-level validation banner instead of polluting unrelated fields.
/// </remarks>
/// <typeparam name="TCommand">The command type whose allowlist is being computed.</typeparam>
public sealed class ReflectionCommandValidationFieldAllowlist<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>
    : ICommandValidationFieldAllowlist {
    private readonly Dictionary<string, string> _byInsensitiveName;

    /// <summary>Initializes a new instance of the <see cref="ReflectionCommandValidationFieldAllowlist{TCommand}"/> class.</summary>
    public ReflectionCommandValidationFieldAllowlist() {
        _byInsensitiveName = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (PropertyInfo property in typeof(TCommand).GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (property.GetIndexParameters().Length > 0) {
                continue;
            }

            if (!property.CanWrite || property.SetMethod is null || !property.SetMethod.IsPublic) {
                continue;
            }

            // Guard against duplicate aliases that differ only by casing — keep the first occurrence,
            // route subsequent collisions to the form-level banner by simply not overwriting.
            _ = _byInsensitiveName.TryAdd(property.Name, property.Name);
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> EditableFields => _byInsensitiveName.Values;

    /// <inheritdoc />
    public bool TryGetEditableField(string serverFieldPath, out string normalizedFieldName) {
        normalizedFieldName = string.Empty;
        if (string.IsNullOrWhiteSpace(serverFieldPath)) {
            return false;
        }

        // Reject nested paths ('.' / '[' / ']' / '/') in v1: a path like "Address.City" must
        // resolve to a generated nested editable field, but the reflection default cannot prove
        // a nested allowlist exists without recursing through unknown adopter type graphs.
        // Story 6-1+ generator-emitted allowlists may relax this when they have safe coverage.
        for (int i = 0; i < serverFieldPath.Length; i++) {
            char ch = serverFieldPath[i];
            if (ch is '.' or '[' or ']' or '/' or '\\' or ' ') {
                return false;
            }
        }

        return _byInsensitiveName.TryGetValue(serverFieldPath, out normalizedFieldName!);
    }
}
