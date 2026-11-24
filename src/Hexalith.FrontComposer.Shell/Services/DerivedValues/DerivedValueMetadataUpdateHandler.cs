using System.Reflection.Metadata;

using Hexalith.FrontComposer.Shell.Services.DerivedValues;

[assembly: MetadataUpdateHandler(typeof(DerivedValueMetadataUpdateHandler))]

namespace Hexalith.FrontComposer.Shell.Services.DerivedValues;

/// <summary>Invalidates derived-value caches when the runtime applies a metadata update.</summary>
internal static class DerivedValueMetadataUpdateHandler
{
    private static object _generation = new();

    /// <summary>Gets the current cache key, shared by both default providers.</summary>
    internal static object Generation => Volatile.Read(ref _generation);

    /// <summary>Starts a fresh cache generation after the runtime updates command metadata.</summary>
    /// <param name="updatedTypes">The updated types, or null when the runtime cannot identify them.</param>
    public static void ClearCache(Type[]? updatedTypes) => Volatile.Write(ref _generation, new object());
}
