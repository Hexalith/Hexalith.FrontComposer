using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.DerivedValues;

/// <summary>
/// Derived-value provider #3 in the chain (Story 2-2 Decision D24): returns the value of
/// <see cref="System.ComponentModel.DefaultValueAttribute"/> when present on the command's property.
/// Placed BEFORE <c>LastUsedValueProvider</c> so <c>[DefaultValue]</c> acts as a hard floor for
/// reset-semantics fields.
/// </summary>
public sealed class ExplicitDefaultValueProvider : IDerivedValueProvider {
    private static readonly ConcurrentDictionary<(Type, string), object?> Cache = new();
    private static readonly ConcurrentDictionary<(Type, string), bool> HasAttribute = new();

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern", Justification = "commandType flows from generated code; trim preservation is the renderer's responsibility.")]
    public Task<DerivedValueResult> ResolveAsync(
        Type commandType,
        string propertyName,
        ProjectionContext? projectionContext,
        CancellationToken ct) {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        (Type commandType, string propertyName) key = (commandType, propertyName);

        bool hasAttr = HasAttribute.GetOrAdd(key, static k => LookupDefaultValueAttribute(k.Item1, k.Item2, out _));
        if (!hasAttr) {
            return Task.FromResult(DerivedValueResult.None);
        }

        object? value = Cache.GetOrAdd(key, static k => {
            _ = LookupDefaultValueAttribute(k.Item1, k.Item2, out object? value);
            return value;
        });

        return Task.FromResult(new DerivedValueResult(true, value));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern", Justification = "Delegated to renderer emission point.")]
    private static bool LookupDefaultValueAttribute(Type commandType, string propertyName, out object? value) {
        PropertyInfo? prop = commandType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop is null) {
            value = null;
            return false;
        }

        DefaultValueAttribute? attr = prop.GetCustomAttribute<DefaultValueAttribute>(inherit: true);
        if (attr is null) {
            value = null;
            return false;
        }

        value = attr.Value;
        return true;
    }
}
