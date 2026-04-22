namespace Hexalith.FrontComposer.Contracts.Badges;

/// <summary>
/// Produces the list of projection runtime types hinted as ActionQueue via
/// <see cref="Hexalith.FrontComposer.Contracts.Attributes.ProjectionRoleAttribute"/>
/// (<see cref="Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole.ActionQueue"/>).
/// Consumed by Story 3-5's <c>BadgeCountService</c> initial-fetch fan-out.
/// </summary>
/// <remarks>
/// <para>
/// The default Shell registration is <c>ReflectionActionQueueProjectionCatalog</c>, which scans the
/// assemblies handed to its constructor (defaults to <c>AppDomain.CurrentDomain.GetAssemblies()</c>)
/// once and caches the result. Adopters override via
/// <c>services.TryAddSingleton&lt;IActionQueueProjectionCatalog, MyCatalog&gt;()</c> before
/// <c>AddHexalithFrontComposer</c> — <c>TryAdd</c> means adopter-first registration wins.
/// </para>
/// <para>
/// <b>Trimming / AOT note (Known Gap G22):</b> the reflection default is incompatible with
/// <c>PublishTrimmed=true</c> or native AOT — the runtime cannot enumerate trimmed assemblies and
/// the attribute may be stripped. Trim-enabled hosts MUST supply a source-generated catalog via
/// the adopter-override path. Story 9-1 will emit a build-time diagnostic when a trim-enabled
/// project depends on the reflection default.
/// </para>
/// </remarks>
public interface IActionQueueProjectionCatalog {
    /// <summary>
    /// Gets the projection runtime types decorated with
    /// <see cref="Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole.ActionQueue"/>.
    /// The result is cached by the default implementation; adopter implementations choose their own
    /// caching strategy.
    /// </summary>
    IReadOnlyList<Type> ActionQueueTypes { get; }
}
