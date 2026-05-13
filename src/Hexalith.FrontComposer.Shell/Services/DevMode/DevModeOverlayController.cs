using System.Collections.Concurrent;

using Hexalith.FrontComposer.Contracts.DevMode;

namespace Hexalith.FrontComposer.Shell.Services.DevMode;

/// <summary>
/// Default scoped implementation of <see cref="IDevModeOverlayController"/>.
/// </summary>
public sealed class DevModeOverlayController : IDevModeOverlayController {
    private readonly ConcurrentDictionary<string, ComponentTreeNode> _nodes = new(StringComparer.Ordinal);
    private readonly object _selectionLock = new();
    private int _isActive;
    private string? _selectedAnnotationKey;
    private ComponentTreeNode? _selectedNode;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public bool IsActive => Volatile.Read(ref _isActive) == 1;

    /// <inheritdoc />
    public string? SelectedAnnotationKey {
        get {
            lock (_selectionLock) {
                return _selectedAnnotationKey;
            }
        }
    }

    /// <inheritdoc />
    public ComponentTreeNode? SelectedNode {
        get {
            lock (_selectionLock) {
                return _selectedNode;
            }
        }
    }

    /// <inheritdoc />
    public void Toggle() {
        int previous;
        do {
            previous = Volatile.Read(ref _isActive);
        }
        while (Interlocked.CompareExchange(ref _isActive, previous ^ 1, previous) != previous);

        bool nowActive = previous == 0;
        if (!nowActive) {
            lock (_selectionLock) {
                _selectedAnnotationKey = null;
                _selectedNode = null;
            }
        }

        NotifyChanged();
    }

    /// <inheritdoc />
    public bool Open(string annotationKey) {
        if (string.IsNullOrWhiteSpace(annotationKey) || !IsActive) {
            return false;
        }

        if (!_nodes.TryGetValue(annotationKey, out ComponentTreeNode? node)) {
            lock (_selectionLock) {
                _selectedAnnotationKey = null;
                _selectedNode = null;
            }

            NotifyChanged();
            return false;
        }

        lock (_selectionLock) {
            _selectedAnnotationKey = annotationKey;
            _selectedNode = node;
        }

        NotifyChanged();
        return true;
    }

    /// <inheritdoc />
    public bool Open(string annotationKey, long renderEpoch) {
        if (string.IsNullOrWhiteSpace(annotationKey) || !IsActive) {
            return false;
        }

        if (!_nodes.TryGetValue(annotationKey, out ComponentTreeNode? node) || node.RenderEpoch != renderEpoch) {
            lock (_selectionLock) {
                _selectedAnnotationKey = null;
                _selectedNode = null;
            }

            NotifyChanged();
            return false;
        }

        lock (_selectionLock) {
            _selectedAnnotationKey = annotationKey;
            _selectedNode = node;
        }

        NotifyChanged();
        return true;
    }

    /// <inheritdoc />
    public void Close() {
        lock (_selectionLock) {
            if (_selectedAnnotationKey is null && _selectedNode is null) {
                return;
            }

            _selectedAnnotationKey = null;
            _selectedNode = null;
        }

        NotifyChanged();
    }

    /// <inheritdoc />
    public IDisposable Register(ComponentTreeNode node) {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentException.ThrowIfNullOrWhiteSpace(node.AnnotationKey);

        _nodes[node.AnnotationKey] = node;
        lock (_selectionLock) {
            if (string.Equals(_selectedAnnotationKey, node.AnnotationKey, StringComparison.Ordinal)) {
                _selectedNode = node;
            }
        }

        NotifyChanged();
        return new Registration(this, node.AnnotationKey);
    }

    private void Unregister(string annotationKey) {
        _ = _nodes.TryRemove(annotationKey, out _);
        lock (_selectionLock) {
            if (string.Equals(_selectedAnnotationKey, annotationKey, StringComparison.Ordinal)) {
                _selectedAnnotationKey = null;
                _selectedNode = null;
            }
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
