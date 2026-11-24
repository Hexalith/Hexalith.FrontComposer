using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.DerivedValues;

/// <summary>
/// Derived-value provider #3 in the chain (Story 2-2 Decision D24): returns the value of
/// <see cref="System.ComponentModel.DefaultValueAttribute"/> when present on the command's property.
/// Placed BEFORE <c>LastUsedValueProvider</c> so <c>[DefaultValue]</c> acts as a hard floor for
/// reset-semantics fields.
/// </summary>
public sealed class ExplicitDefaultValueProvider : IDerivedValueProvider {
    private static readonly ConditionalWeakTable<object, ConcurrentDictionary<(Type CommandType, string PropertyName), (bool HasAttribute, object? Value)>> _caches = new();

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern", Justification = "commandType flows from generated code; trim preservation is the renderer's responsibility.")]
    public Task<DerivedValueResult> ResolveAsync(
        Type commandType,
        string propertyName,
        ProjectionContext? projectionContext,
        CancellationToken ct) {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        ct.ThrowIfCancellationRequested();

        var cache = _caches.GetOrCreateValue(DerivedValueMetadataUpdateHandler.Generation);
        (bool HasAttribute, object? Value) = cache.GetOrAdd(
            (commandType, propertyName),
            static key => LookupDefaultValueAttribute(key.CommandType, key.PropertyName));

        return Task.FromResult(HasAttribute
            ? new DerivedValueResult(true, Value)
            : DerivedValueResult.None);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern", Justification = "Delegated to renderer emission point.")]
    private static (bool HasAttribute, object? Value) LookupDefaultValueAttribute(Type commandType, string propertyName) {
        PropertyInfo? prop = commandType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop is null) {
            return (false, null);
        }

        DefaultValueAttribute? attr = prop.GetCustomAttribute<DefaultValueAttribute>(inherit: true);
        return attr is null ? (false, null) : (true, attr.Value);
    }
}
