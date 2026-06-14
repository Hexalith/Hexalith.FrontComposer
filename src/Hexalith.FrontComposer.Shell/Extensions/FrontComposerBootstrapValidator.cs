namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Story 1.1 AC2 — validates the FrontComposer bootstrap ordering markers and throws a clear,
/// <b>named</b> <see cref="InvalidOperationException"/> when the three-call bootstrap is missing the
/// foundational call or is mis-ordered. The thrown message names the offending <c>AddHexalith*</c>
/// call and the fix, so a misconfigured host fails fast at startup instead of dying later with an
/// opaque "Unable to resolve service for type 'IFrontComposerRegistry'" DI error at first render.
/// </summary>
/// <remarks>
/// <para>
/// Exposed as a static method (separate from the <see cref="FrontComposerBootstrapValidationGate"/>
/// hosted service that calls it) so it is directly unit-testable without standing up a
/// <c>HostBuilder</c> — test hosts that build a bare <c>ServiceProvider</c> never run hosted
/// services.
/// </para>
/// <para>
/// Required order: <see cref="FrontComposerBootstrapStage.Quickstart"/> →
/// <see cref="FrontComposerBootstrapStage.Domain"/> (optional) →
/// <see cref="FrontComposerBootstrapStage.EventStore"/> (optional). Only the foundational call is
/// required; an empty shell with neither a domain nor an EventStore is a valid bootstrap (AC3).
/// </para>
/// </remarks>
internal static class FrontComposerBootstrapValidator {
    /// <summary>
    /// Validates the supplied bootstrap markers (enumerated in DI registration order).
    /// </summary>
    /// <param name="markers">The registered bootstrap markers, in insertion order.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the foundational <c>AddHexalithFrontComposerQuickstart()</c> /
    /// <c>AddHexalithFrontComposer()</c> call is missing, or when the calls are mis-ordered.
    /// </exception>
    public static void Validate(IEnumerable<IFrontComposerBootstrapMarker> markers) {
        ArgumentNullException.ThrowIfNull(markers);

        // Materialise once, preserving DI registration order.
        List<IFrontComposerBootstrapMarker> ordered = [.. markers];

        int quickstartIndex = ordered.FindIndex(static m => m.Stage == FrontComposerBootstrapStage.Quickstart);
        int domainIndex = ordered.FindIndex(static m => m.Stage == FrontComposerBootstrapStage.Domain);
        int eventStoreIndex = ordered.FindIndex(static m => m.Stage == FrontComposerBootstrapStage.EventStore);

        if (quickstartIndex < 0) {
            // Name what the adopter DID call so the message points at the concrete mistake. The
            // empty-marker case (no FrontComposer entry point ran) is defensive only — the gate is
            // registered by an entry point, so a marker is always present in production — but it must
            // NOT falsely claim a downstream call was made.
            string problem = ordered.Count == 0
                ? "no FrontComposer bootstrap call was made"
                : $"{(eventStoreIndex >= 0 ? "AddHexalithEventStore(...)" : "AddHexalithDomain<TMarker>()")} "
                    + "was called but AddHexalithFrontComposerQuickstart() (or the granular "
                    + "AddHexalithFrontComposer()) was not";
            throw new InvalidOperationException(
                $"FrontComposer bootstrap is incomplete: {problem}. "
                + "Call AddHexalithFrontComposerQuickstart() first so it can establish the authoritative "
                + "Fluxor store, IStorageService, and IFrontComposerRegistry before any domain or EventStore "
                + "registration.");
        }

        if (eventStoreIndex >= 0 && eventStoreIndex < quickstartIndex) {
            throw new InvalidOperationException(
                "FrontComposer bootstrap is mis-ordered: AddHexalithEventStore(...) was called before "
                + "AddHexalithFrontComposerQuickstart(). Call AddHexalithFrontComposerQuickstart() first so it "
                + "can establish the authoritative Fluxor store, IStorageService, and IFrontComposerRegistry; "
                + "AddHexalithEventStore(...) must run last so it only swaps the stub command/query clients for "
                + "the real ones.");
        }

        if (domainIndex >= 0 && domainIndex < quickstartIndex) {
            throw new InvalidOperationException(
                "FrontComposer bootstrap is mis-ordered: AddHexalithDomain<TMarker>() was called before "
                + "AddHexalithFrontComposerQuickstart(). Call AddHexalithFrontComposerQuickstart() first so the "
                + "authoritative IFrontComposerRegistry exists before AddHexalithDomain<TMarker>() feeds the "
                + "generated domain manifests into it.");
        }

        if (domainIndex >= 0 && eventStoreIndex >= 0 && eventStoreIndex < domainIndex) {
            throw new InvalidOperationException(
                "FrontComposer bootstrap is mis-ordered: AddHexalithEventStore(...) was called before "
                + "AddHexalithDomain<TMarker>(). Register domains via AddHexalithDomain<TMarker>() before "
                + "AddHexalithEventStore(...) so the registry holds the domain manifests when EventStore swaps "
                + "in the real command/query clients.");
        }
    }
}
