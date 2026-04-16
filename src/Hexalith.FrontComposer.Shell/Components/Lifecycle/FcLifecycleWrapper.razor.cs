using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Lifecycle;

/// <summary>
/// TDD RED-phase stub for Story 2-4. Task 2.1–2.6 fleshes out subscription, timer wiring,
/// live-region rendering, and dispose contract. Shape exists so the test suite compiles;
/// behavior lands with implementation.
/// </summary>
public partial class FcLifecycleWrapper : ComponentBase, IAsyncDisposable {
    [Parameter]
    [EditorRequired]
    public string CorrelationId { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? RejectionMessage { get; set; }

    public ValueTask DisposeAsync() => throw new NotImplementedException("TDD RED — Story 2-4 Task 2.6");
}
