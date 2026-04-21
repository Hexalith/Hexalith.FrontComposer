using System.Collections.Concurrent;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Resolves projection type names emitted by the registry into runtime <see cref="Type"/>
/// instances for the optional badge-count contract.
/// </summary>
internal static class ProjectionTypeResolver
{
    private static readonly ConcurrentDictionary<string, Type?> _cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Resolves a projection type name by scanning the already-loaded application assemblies.
    /// </summary>
    /// <param name="typeName">The fully qualified projection type name.</param>
    /// <returns>The resolved runtime type when available; otherwise <see langword="null"/>.</returns>
    public static Type? Resolve(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        // Cache only positive resolutions. Negative results must remain re-tryable so
        // plug-in / lazy-loaded / hot-reloaded adopter assemblies loaded after the first
        // palette open become visible on the next lookup without a process restart.
        if (_cache.TryGetValue(typeName, out Type? cached) && cached is not null)
        {
            return cached;
        }

        Type? resolved = ResolveCore(typeName);
        if (resolved is not null)
        {
            // Pass-5 P9 — atomic add preserves the first writer's result under concurrent resolves.
            _cache.TryAdd(typeName, resolved);
        }

        return resolved;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2057:Unrecognized value passed to the parameter 'typeName' of method 'System.Type.GetType(String, Boolean)'. It's not possible to guarantee the availability of the target type.",
        Justification = "Badge lookup is optional and fail-open. The registry emits source-generated projection type names for app-owned types; unresolved names simply produce null and suppress the badge.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members attributed with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code.",
        Justification = "Scanning loaded assemblies is only used for the optional badge contract. If trimming removes a projection type, the palette intentionally degrades to no badge rather than failing.")]
    private static Type? ResolveCore(string typeName)
    {
        Type? resolved = null;
        try
        {
            resolved = Type.GetType(typeName, throwOnError: false);
        }
        catch (TypeLoadException) { }
        catch (FileLoadException) { }
        catch (BadImageFormatException) { }

        if (resolved is not null)
        {
            return resolved;
        }

        // Pass-5 P8 — snapshot to avoid enumerator-modified exceptions when an assembly loads
        // concurrently during the scan (hot-reload, lazy MEF, dynamic plugin load).
        Assembly[] snapshot = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in snapshot)
        {
            try
            {
                resolved = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            }
            catch (TypeLoadException) { continue; }
            catch (FileLoadException) { continue; }
            catch (BadImageFormatException) { continue; }
            catch (NotSupportedException) { continue; } // Dynamic assemblies don't support GetType.

            if (resolved is not null)
            {
                return resolved;
            }
        }

        return null;
    }
}
