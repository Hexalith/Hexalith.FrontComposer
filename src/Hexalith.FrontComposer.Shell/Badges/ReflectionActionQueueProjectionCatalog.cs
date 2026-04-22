using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Badges;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Badges;

/// <summary>
/// Default <see cref="IActionQueueProjectionCatalog"/> implementation (Story 3-5 D2 / ADR-045).
/// Enumerates a caller-supplied list of assemblies once, filters to concrete classes decorated with
/// <c>[ProjectionRole(ProjectionRole.ActionQueue)]</c>, and caches the result for the service lifetime.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cache semantics (first-call-wins):</b> <see cref="ActionQueueTypes"/> is backed by a
/// <see cref="Lazy{T}"/>. Adopters that lazy-load a bounded-context assembly AFTER the first read
/// will NOT see new types until the service container rebuilds. This is the acknowledged constraint
/// of reflection-based discovery; hosts with post-boot assembly loading must supply a custom
/// <see cref="IActionQueueProjectionCatalog"/> (source-generated catalog).
/// </para>
/// <para>
/// <b>Assembly injection (constructor):</b> the assembly list is explicit so the catalog is unit-
/// testable in isolation — tests pass a synthetic list without needing <c>AppDomain</c> manipulation
/// (which would leak across parallel xUnit collections). The default registration in
/// <c>AddHexalithFrontComposer</c> supplies <c>AppDomain.CurrentDomain.GetAssemblies()</c>.
/// </para>
/// <para>
/// <b>Trimming / AOT:</b> reflection-based discovery is incompatible with <c>PublishTrimmed=true</c>;
/// Story 9-1 will emit a build-time diagnostic when a trim-enabled project depends on this default.
/// </para>
/// </remarks>
public sealed class ReflectionActionQueueProjectionCatalog : IActionQueueProjectionCatalog {
    private readonly ILogger<ReflectionActionQueueProjectionCatalog> _logger;
    private readonly Lazy<IReadOnlyList<Type>> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionActionQueueProjectionCatalog"/> class.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan. Typically <c>AppDomain.CurrentDomain.GetAssemblies()</c>.</param>
    /// <param name="logger">Logger for reflection-load diagnostics.</param>
    [RequiresUnreferencedCode(
        "Reflection-based projection catalog enumerates assembly types at runtime. AOT / trim-enabled "
        + "hosts must supply a source-generated IActionQueueProjectionCatalog via TryAddSingleton "
        + "before AddHexalithFrontComposer. Story 9-1 will flag this at build time.")]
    public ReflectionActionQueueProjectionCatalog(
        IEnumerable<Assembly> assemblies,
        ILogger<ReflectionActionQueueProjectionCatalog> logger) {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        Assembly[] snapshot = [.. assemblies];
        _cache = new Lazy<IReadOnlyList<Type>>(() => Discover(snapshot), isThreadSafe: true);
    }

    /// <inheritdoc />
    public IReadOnlyList<Type> ActionQueueTypes => _cache.Value;

    private IReadOnlyList<Type> Discover(Assembly[] assemblies) {
        List<Type> result = [];
        HashSet<Type> seen = [];
        foreach (Assembly assembly in assemblies) {
            foreach (Type type in SafeGetTypes(assembly)) {
                if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters) {
                    continue;
                }

                ProjectionRoleAttribute? attribute;
                try {
                    attribute = type.GetCustomAttribute<ProjectionRoleAttribute>(inherit: false);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException) {
                    // Metadata read can fail on exotic generated types; skip rather than abort.
                    continue;
                }

                if (attribute?.Role == ProjectionRole.ActionQueue && seen.Add(type)) {
                    result.Add(type);
                }
            }
        }

        return result;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Using member 'System.Reflection.Assembly.GetTypes()' which has 'RequiresUnreferencedCodeAttribute'",
        Justification =
            "Documented AOT-incompatibility (G22 / ADR-045). Trim-enabled hosts MUST override via "
            + "services.TryAddSingleton<IActionQueueProjectionCatalog, MyCatalog>() before "
            + "AddHexalithFrontComposer. Story 9-1 will emit a build-time diagnostic.")]
    private IEnumerable<Type> SafeGetTypes(Assembly assembly) {
        try {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex) {
            _logger.LogInformation(
                ex,
                "ReflectionActionQueueProjectionCatalog: partial type-load for assembly '{AssemblyName}' — continuing with resolved types.",
                assembly.FullName);
            return ex.Types.Where(static t => t is not null)!;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException) {
            _logger.LogInformation(
                ex,
                "ReflectionActionQueueProjectionCatalog: skipped assembly '{AssemblyName}' — GetTypes threw.",
                assembly.FullName);
            return [];
        }
    }
}
