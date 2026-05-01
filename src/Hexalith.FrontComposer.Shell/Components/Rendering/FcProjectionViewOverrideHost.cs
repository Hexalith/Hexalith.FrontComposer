using System.Security.Cryptography;
using System.Text;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Hosts a Level 4 full projection-view replacement inside a narrow error boundary.
/// </summary>
/// <remarks>
/// <para>
/// The host owns the boundary policy, sanitized HFC2121 logging, recovery strategy, and
/// disposal containment. The replacement component owns only the body markup it returns.
/// </para>
/// <para>
/// Recursion: the only safe way for a replacement to reach generated output is via
/// <c>Context.DefaultBody</c>, which is emitted by the source generator to bypass Level 4
/// resolution for the same projection/role. Re-mounting the typed generated component
/// (e.g. <c>&lt;CounterProjectionView /&gt;</c>) inside a replacement re-enters the registry
/// and selects this same descriptor again — that pattern is unsupported and would loop.
/// </para>
/// </remarks>
/// <typeparam name="TProjection">Projection type being rendered.</typeparam>
public sealed class FcProjectionViewOverrideHost<TProjection> : ComponentBase, IAsyncDisposable {
    private const int RenderFailureCircuitBreaker = 3;

    private ErrorBoundary? _boundary;
    private ProjectionViewOverrideDescriptor? _previousDescriptor;
    private RenderContext? _previousRenderContext;
    private int _consecutiveFailures;
    private bool _circuitOpen;
    private bool _loggedSinceLastRecover;
    private bool _disposed;

    /// <summary>Gets or sets the selected descriptor.</summary>
    [Parameter, EditorRequired]
    public ProjectionViewOverrideDescriptor Descriptor { get; set; } = default!;

    /// <summary>Gets or sets the per-render Level 4 context.</summary>
    [Parameter, EditorRequired]
    public ProjectionViewContext<TProjection> Context { get; set; } = default!;

    /// <summary>Gets or sets the logger.</summary>
    [Inject]
    public ILogger<FcProjectionViewOverrideHost<TProjection>> Logger { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        // P3 — guard against null Context on re-render. BuildRenderTree already short-circuits
        // when Context is null; this keeps the recovery comparison consistent.
        if (Context is null) {
            return;
        }

        // DN2 — recovery fires only on descriptor or RenderContext change. We deliberately do
        // NOT key on Items reference because Fluxor allocates a fresh IReadOnlyList on every
        // state tick (sort flips, polling, ETag refresh, sibling state changes), which would
        // recover the boundary on every render and produce HFC2121 log spam against
        // deterministically-failing replacements. Items are passed through to the replacement
        // unchanged; recovery is bounded to descriptor / RenderContext changes per T5
        // ("framework-owned context generation value").
        if (_boundary is not null
            && (!Equals(_previousDescriptor, Descriptor)
                || !Equals(_previousRenderContext, Context.RenderContext))) {
            _boundary.Recover();
            _consecutiveFailures = 0;
            _circuitOpen = false;
            _loggedSinceLastRecover = false;
        }

        _previousDescriptor = Descriptor;
        _previousRenderContext = Context.RenderContext;
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);

        if (Descriptor is null || Context is null) {
            return;
        }

        builder.OpenComponent<ErrorBoundary>(0);
        builder.AddAttribute(1, "ChildContent", (RenderFragment)RenderReplacement);
        builder.AddAttribute(2, "ErrorContent", (RenderFragment<Exception>)RenderFailure);
        builder.AddComponentReferenceCapture(3, component => _boundary = (ErrorBoundary)component);
        builder.CloseComponent();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() {
        // P4 — host disposal is a no-op: Blazor disposes the inner replacement component
        // through its own renderer disposal chain. The renderer captures any IDisposable /
        // IAsyncDisposable faults from the replacement; this host does not own the inner
        // component instance and cannot pre-empt that disposal. We mark our own state as
        // disposed so a stray re-render after teardown is a noop instead of a NRE.
        _disposed = true;
        _boundary = null;
        _previousDescriptor = null;
        _previousRenderContext = null;
        return ValueTask.CompletedTask;
    }

    private void RenderReplacement(RenderTreeBuilder builder) {
        if (_disposed) {
            return;
        }

        builder.OpenComponent(0, Descriptor.ComponentType);
        builder.AddAttribute(1, "Context", Context);
        builder.CloseComponent();
    }

    private RenderFragment RenderFailure(Exception exception)
        => builder => {
            // DN2 — log once per fault episode. Blazor invokes the ErrorContent fragment on
            // every parent re-render while the boundary is in error state. Without this guard
            // every Items-only state tick produces a fresh HFC2121 line. We reset
            // _loggedSinceLastRecover when the boundary recovers (descriptor / RenderContext
            // change in OnParametersSet).
            if (!_loggedSinceLastRecover) {
                string exceptionCategory = exception.GetType().Name;
                _consecutiveFailures++;
                if (_consecutiveFailures >= RenderFailureCircuitBreaker) {
                    _circuitOpen = true;
                }

                // DN5 — tenant/user redacted via SHA256-truncated hash so operators can
                // correlate incidents within a tenant without leaking identifiers.
                string? tenantHash = HashIdentifier(Context?.RenderContext?.TenantId);
                string? userHash = HashIdentifier(Context?.RenderContext?.UserId);

                Logger?.LogWarning(
                    "{DiagnosticId}: Level 4 view replacement render fault isolated. Projection: {Projection}; Component: {Component}; Role: {Role}; ExceptionCategory: {ExceptionCategory}; TenantHash: {TenantHash}; UserHash: {UserHash}; ConsecutiveFailures: {ConsecutiveFailures}; CircuitOpen: {CircuitOpen}. Item payloads, field values, localized strings, raw exception messages, and render fragments are intentionally omitted.",
                    FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault,
                    typeof(TProjection).FullName,
                    Descriptor.ComponentType.FullName,
                    Context?.Role?.ToString() ?? "<default>",
                    exceptionCategory,
                    tenantHash,
                    userHash,
                    _consecutiveFailures,
                    _circuitOpen);

                _loggedSinceLastRecover = true;
            }

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "fc-view-override-fault");
            builder.AddAttribute(2, "role", "alert");
            builder.AddAttribute(3, "data-fc-diagnostic", FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault);
            builder.AddContent(4, FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault);
            builder.CloseElement();
        };

    private static string? HashIdentifier(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        Span<byte> hashBytes = stackalloc byte[32];
        if (!SHA256.TryHashData(Encoding.UTF8.GetBytes(value), hashBytes, out _)) {
            return null;
        }

        return Convert.ToHexString(hashBytes[..4]).ToLowerInvariant();
    }
}
