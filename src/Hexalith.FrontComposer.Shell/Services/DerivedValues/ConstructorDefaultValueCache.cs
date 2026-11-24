using System.Collections.Concurrent;

namespace Hexalith.FrontComposer.Shell.Services.DerivedValues;

/// <summary>Stores a renderer's constructor-derived defaults for one metadata generation.</summary>
internal sealed class ConstructorDefaultValueCache
{
    /// <summary>Gets the command instances shared by the renderer's related default values.</summary>
    internal ConcurrentDictionary<Type, object?> Instances { get; } = new();

    /// <summary>Gets the values already read from those instances.</summary>
    internal ConcurrentDictionary<(Type CommandType, string PropertyName), object?> Values { get; } = new();
}
