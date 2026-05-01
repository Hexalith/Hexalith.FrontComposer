using System.Collections.Concurrent;

using Hexalith.FrontComposer.Contracts.DevMode;

namespace Hexalith.FrontComposer.Shell.Services.DevMode;

/// <summary>
/// Default scoped implementation of <see cref="IDevModeOverlayController"/>.
/// </summary>
public sealed class DevModeOverlayController : IDevModeOverlayController {
    private readonly ConcurrentDictionary<string, ComponentTreeNode> _nodes = new(StringComparer.Ordinal);
    private int _isActive;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public bool IsActive => Volatile.Read(ref _isActive) == 1;

    /// <inheritdoc />
    public string? SelectedAnnotationKey { get; private set; }

    /// <inheritdoc />
    public ComponentTreeNode? SelectedNode { get; private set; }

    /// <inheritdoc />
    public void Toggle() {
        int previous;
        do {
            previous = Volatile.Read(ref _isActive);
        }
        while (Interlocked.CompareExchange(ref _isActive, previous ^ 1, previous) != previous);

        bool nowActive = previous == 0;
        if (!nowActive) {
            SelectedAnnotationKey = null;
            SelectedNode = null;
        }

        NotifyChanged();
    }

    /// <inheritdoc />
    public bool Open(string annotationKey) {
        if (string.IsNullOrWhiteSpace(annotationKey) || !IsActive) {
            return false;
        }

        if (!_nodes.TryGetValue(annotationKey, out ComponentTreeNode? node)) {
            SelectedAnnotationKey = null;
            SelectedNode = null;
            NotifyChanged();
            return false;
        }

        SelectedAnnotationKey = annotationKey;
        SelectedNode = node;
        NotifyChanged();
        return true;
    }

    /// <inheritdoc />
    public bool Open(string annotationKey, long renderEpoch) {
        if (string.IsNullOrWhiteSpace(annotationKey) || !IsActive) {
            return false;
        }

        if (!_nodes.TryGetValue(annotationKey, out ComponentTreeNode? node) || node.RenderEpoch != renderEpoch) {
            SelectedAnnotationKey = null;
            SelectedNode = null;
            NotifyChanged();
            return false;
        }

        SelectedAnnotationKey = annotationKey;
        SelectedNode = node;
        NotifyChanged();
        return true;
    }

    /// <inheritdoc />
    public void Close() {
        if (SelectedAnnotationKey is null && SelectedNode is null) {
            return;
        }

        SelectedAnnotationKey = null;
        SelectedNode = null;
        NotifyChanged();
    }

    /// <inheritdoc />
    public IDisposable Register(ComponentTreeNode node) {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentException.ThrowIfNullOrWhiteSpace(node.AnnotationKey);

        _nodes[node.AnnotationKey] = node;
        NotifyChanged();
        return new Registration(this, node.AnnotationKey);
    }

    private void Unregister(string annotationKey) {
        _ = _nodes.TryRemove(annotationKey, out _);
        if (string.Equals(SelectedAnnotationKey, annotationKey, StringComparison.Ordinal)) {
            SelectedAnnotationKey = null;
            SelectedNode = null;
        }

        NotifyChanged();
    }

    private void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);

    private sealed class Registration(DevModeOverlayController owner, string annotationKey) : IDisposable {
        private int _disposed;

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) {
                owner.Unregister(annotationKey);
            }
        }
    }
}
