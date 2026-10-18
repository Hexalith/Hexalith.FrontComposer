using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Testing;

/// <summary>Caller-observed semantic surface truth; the assertion resolves the expected state from an SS row.</summary>
/// <param name="State">Canonical state name documented by <see cref="UxStateAssertions.AssertState"/>.</param>
/// <param name="Terminal">Whether the attempt/result/connection/overlay subject has concluded.</param>
/// <param name="Semantics">Observed role and accessible name.</param>
/// <param name="Lifecycle">Actual lifecycle identity when this is a command outcome.</param>
/// <param name="Dispatched">Whether this attempted command dispatched.</param>
/// <param name="Facts">Measured facts required by the selected SS row.</param>
/// <param name="Queued">Whether this attempted command queued.</param>
public sealed record UxSurfaceSnapshot(string State, bool Terminal, UxAccessibilitySnapshot Semantics,
    CommandLifecycleState? Lifecycle = null, bool Dispatched = false, bool Queued = false, UxSurfaceFacts? Facts = null);
