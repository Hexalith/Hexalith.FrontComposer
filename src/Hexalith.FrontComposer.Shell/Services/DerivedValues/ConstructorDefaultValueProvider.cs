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
/// <remarks>
/// Commands declared as positional-ctor records (no parameterless ctor) resolve to
/// <see cref="DerivedValueResult.None"/> — the provider declines rather than fabricating a
/// default. Adopters that need pre-fill values on such records should register a custom
/// <see cref="IDerivedValueProvider"/> via
/// <c>AddDerivedValueProvider&lt;T&gt;</c> or add parameterless ctors.
/// </remarks>
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
        ct.ThrowIfCancellationRequested();

        (Type commandType, string propertyName) key = (commandType, propertyName);
        if (ValueCache.TryGetValue(key, out object? cached)) {
            return Task.FromResult(new DerivedValueResult(true, cached));
        }

#pragma warning disable IL2067 // commandType flows from the generated renderer; trim analysis is delegated to the renderer's call site.
        object? instance = InstanceCache.GetOrAdd(commandType, static t => {
            try {
                return Activator.CreateInstance(t);
            }
            catch (MissingMethodException) {
                // Positional-ctor records without a parameterless ctor — provider declines.
                return null;
            }
            catch (MemberAccessException) {
                // Non-public / protected parameterless ctor — provider declines.
                return null;
            }
            catch (TargetInvocationException) {
                // Parameterless ctor threw — provider declines (the exception itself is the
                // adopter's bug, not ours; we don't mask it with a fabricated default).
                return null;
            }
        });
#pragma warning restore IL2067

        if (instance is null) {
            return Task.FromResult(DerivedValueResult.None);
        }

        PropertyInfo? prop;
        try {
            prop = commandType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        }
        catch (AmbiguousMatchException) {
            // Shadowed/new property in a derived type — provider declines rather than guessing.
            return Task.FromResult(DerivedValueResult.None);
        }

        if (prop is null || !prop.CanRead) {
            return Task.FromResult(DerivedValueResult.None);
        }

        object? value;
        try {
            value = prop.GetValue(instance);
        }
        catch (TargetInvocationException) {
            // The property getter threw — provider declines.
            return Task.FromResult(DerivedValueResult.None);
        }
        catch (TargetParameterCountException) {
            // Indexer-style property — provider declines.
            return Task.FromResult(DerivedValueResult.None);
        }

        _ = ValueCache.TryAdd(key, value);
        return Task.FromResult(new DerivedValueResult(true, value));
    }
}
