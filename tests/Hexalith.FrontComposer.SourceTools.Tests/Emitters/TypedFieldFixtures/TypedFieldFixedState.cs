using Fluxor;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;
/// <summary>Supplies immutable generated lifecycle state without a store or subscriptions.</summary>
/// <typeparam name="T">The lifecycle state or localization resource type.</typeparam>
/// <param name="value">The fixed lifecycle value.</param>
internal sealed class TypedFieldFixedState<T>(T value) : IState<T>
{
    /// <inheritdoc/>
    public T Value => value;

    /// <inheritdoc/>
    public event EventHandler? StateChanged
    {
        add
        {
        }

        remove
        {
        }
    }
}
