using System.Collections.Concurrent;
using System.Collections.Generic;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services.ProjectionTemplates;

/// <summary>
/// Story 6-2 T4 — runtime <see cref="IProjectionTemplateRegistry"/> implementation. Stores
/// generated template descriptors in a thread-safe lookup keyed by (projection type, role).
/// </summary>
/// <remarks>
/// <para>
/// <b>Cache-safety boundary (Story 6-2 D15 / AC15).</b> The registry caches descriptor type
/// metadata only. It MUST NOT cache <see cref="ProjectionTemplateContext{TProjection}"/>
/// instances, render fragments, item lists, tenant/user identifiers, or culture-specific
/// resolved strings.
/// </para>
/// <para>
/// <b>No assembly reflection (Story 6-2 D2 / AC11).</b> Discovery flows exclusively through
/// SourceTools-emitted manifest registration; the registry never enumerates loaded assemblies.
/// </para>
/// <para>
/// <b>Duplicate handling.</b> When a duplicate descriptor is registered for a (projection,
/// role) tuple already present, the slot is marked ambiguous and <see cref="Resolve"/>
/// returns <see langword="null"/> for that tuple. SourceTools usually catches duplicates at
/// build time via HFC1037, so an ambiguous runtime slot indicates either two manifests
/// shipped from different assemblies or a misconfigured registration call.
/// </para>
/// </remarks>
public sealed class ProjectionTemplateRegistry : IProjectionTemplateRegistry {
    private readonly ConcurrentDictionary<RegistryKey, RegistryEntry> _entries = new();
    private readonly ILogger<ProjectionTemplateRegistry> _logger;

    public ProjectionTemplateRegistry(
        ILogger<ProjectionTemplateRegistry> logger,
        IEnumerable<ProjectionTemplateAssemblySource> assemblySources) {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(assemblySources);
        _logger = logger;

        foreach (ProjectionTemplateAssemblySource source in assemblySources) {
            foreach (ProjectionTemplateDescriptor descriptor in source.Descriptors) {
                Register(descriptor);
            }
        }
    }

    private void Register(ProjectionTemplateDescriptor descriptor) {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(descriptor.ProjectionType);
        ArgumentNullException.ThrowIfNull(descriptor.TemplateType);

        if (!IsSupportedContractVersion(descriptor.ContractVersion)) {
            _logger.LogWarning(
                "ProjectionTemplateDescriptor for projection {Projection} (role {Role}) uses incompatible contract version {ContractVersion}; current supported major is {CurrentMajor}. Descriptor ignored.",
                descriptor.ProjectionType.FullName,
                descriptor.Role?.ToString() ?? "<any>",
                descriptor.ContractVersion,
                ProjectionTemplateContractVersion.Major);
            return;
        }

        RegistryKey key = new(descriptor.ProjectionType, descriptor.Role);
        _ = _entries.AddOrUpdate(
            key,
            _ => new RegistryEntry(descriptor, Ambiguous: false),
            (_, existing) => {
                if (ReferenceEquals(existing.Descriptor.TemplateType, descriptor.TemplateType)
                    && existing.Descriptor.ContractVersion == descriptor.ContractVersion) {
                    return existing;
                }

                _logger.LogWarning(
                    "Duplicate ProjectionTemplateDescriptor registered for projection {Projection} (role {Role}) — ignoring both. Existing: {Existing}; new: {New}.",
                    descriptor.ProjectionType.FullName,
                    descriptor.Role?.ToString() ?? "<any>",
                    existing.Descriptor.TemplateType.FullName,
                    descriptor.TemplateType.FullName);
                return new RegistryEntry(existing.Descriptor, Ambiguous: true);
            });
    }

    /// <inheritdoc />
    public ProjectionTemplateDescriptor? Resolve(Type projectionType, ProjectionRole? role) {
        ArgumentNullException.ThrowIfNull(projectionType);

        // Story 6-2 T6 — exact-role match wins; otherwise fall back to the any-role slot.
        if (_entries.TryGetValue(new RegistryKey(projectionType, role), out RegistryEntry exact)) {
            return exact.Ambiguous ? null : exact.Descriptor;
        }

        if (role is not null
            && _entries.TryGetValue(new RegistryKey(projectionType, Role: null), out RegistryEntry any)) {
            return any.Ambiguous ? null : any.Descriptor;
        }

        return null;
    }

    /// <summary>Diagnostic accessor — exposed for tests / dev-mode panels (Story 6-5).</summary>
    public IReadOnlyCollection<ProjectionTemplateDescriptor> Descriptors {
        get {
            List<ProjectionTemplateDescriptor> list = [];
            foreach (KeyValuePair<RegistryKey, RegistryEntry> kvp in _entries) {
                if (!kvp.Value.Ambiguous) {
                    list.Add(kvp.Value.Descriptor);
                }
            }

            return list;
        }
    }

    private readonly record struct RegistryKey(Type ProjectionType, ProjectionRole? Role);

    private readonly record struct RegistryEntry(ProjectionTemplateDescriptor Descriptor, bool Ambiguous);

    private static bool IsSupportedContractVersion(int contractVersion)
        => contractVersion / 1_000_000 == ProjectionTemplateContractVersion.Major;
}
