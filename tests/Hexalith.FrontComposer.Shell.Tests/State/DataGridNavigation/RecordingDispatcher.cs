using System.Collections.Generic;
using System.Linq;

using Fluxor;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Test helper — records every dispatched action so tests can assert on the sequence.
/// </summary>
internal sealed class RecordingDispatcher : IDispatcher {
    private readonly List<object> _dispatched = [];

    public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched;

    public IReadOnlyList<object> AllDispatched => _dispatched;

    public void Dispatch(object action) {
        _dispatched.Add(action);
        ActionDispatched?.Invoke(this, new ActionDispatchedEventArgs(action));
    }

    public T Single<T>() where T : class {
        List<T> matches = [.. _dispatched.OfType<T>()];
        matches.Count.ShouldBe(1, $"Expected exactly one {typeof(T).Name} dispatched; got {matches.Count}.");
        return matches[0];
    }

    public IReadOnlyList<T> All<T>() where T : class => [.. _dispatched.OfType<T>()];
}
