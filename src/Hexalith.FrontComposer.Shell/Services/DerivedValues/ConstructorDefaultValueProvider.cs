using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.DerivedValues;

/// <summary>
/// Derived-value provider #5 (final fallback) in the chain (Story 2-2 Decision D24).
/// Reads the command's property initialization value via <c>new TCommand()</c>, which
/// captures <c>= defaultValue</c> initializers on auto-properties. Cached per
/// <c>(Type, string)</c> pair so the reflection/instantiation cost is paid once per property.
/// </summary>
public sealed class ConstructorDefaultValueProvider : IDerivedValueProvider {
    private static readonly ConcurrentDictionary<Type, object?> InstanceCache = new();
    private static readonly ConcurrentDictionary<(Type, string), object?> ValueCache = new();

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2067:UnrecognizedReflectionPattern", Justification = "commandType flows from generated code; trim preservation is delegated to the renderer.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern", Justification = "commandType flows from generated code; trim preservation is delegated to the renderer.")]
    public Task<DerivedValueResult> ResolveAsync(
        Type commandType,
        string propertyName,
        ProjectionContext? projectionContext,
        CancellationToken ct) {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        (Type commandType, string propertyName) key = (commandType, propertyName);
        if (ValueCache.TryGetValue(key, out object? cached)) {
            return Task.FromResult(new DerivedValueResult(true, cached));
        }

#pragma warning disable IL2067 // commandType flows from the generated renderer; trim analysis is delegated to the renderer's call site.
        object? instance = InstanceCache.GetOrAdd(commandType, static t => {
            try {
                return Activator.CreateInstance(t);
            }
            catch {
                return null;
            }
        });
#pragma warning restore IL2067

        if (instance is null) {
            return Task.FromResult(DerivedValueResult.None);
        }

        PropertyInfo? prop = commandType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop is null || !prop.CanRead) {
            return Task.FromResult(DerivedValueResult.None);
        }

        object? value = prop.GetValue(instance);
        _ = ValueCache.TryAdd(key, value);
        return Task.FromResult(new DerivedValueResult(true, value));
    }
}
