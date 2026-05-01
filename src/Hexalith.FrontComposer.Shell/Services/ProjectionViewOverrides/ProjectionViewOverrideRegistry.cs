using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services.ProjectionViewOverrides;

/// <summary>
/// Runtime Level 4 view override registry. Stores descriptor metadata only and resolves by
/// projection type and optional role.
/// </summary>
public sealed class ProjectionViewOverrideRegistry : IProjectionViewOverrideRegistry {
    private readonly ConcurrentDictionary<RegistryKey, RegistryEntry> _entries = new();
    private readonly IReadOnlyCollection<ProjectionViewOverrideDescriptor> _descriptorsSnapshot;
    private readonly ILogger<ProjectionViewOverrideRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionViewOverrideRegistry"/> class.
    /// </summary>
    public ProjectionViewOverrideRegistry(
        ILogger<ProjectionViewOverrideRegistry> logger,
        IEnumerable<ProjectionViewOverrideDescriptorSource> sources) {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(sources);
        _logger = logger;

        foreach (ProjectionViewOverrideDescriptorSource source in sources) {
            foreach (ProjectionViewOverrideDescriptor descriptor in source.Descriptors) {
                Register(descriptor);
            }
        }

        List<ProjectionViewOverrideDescriptor> visible = [];
        foreach (KeyValuePair<RegistryKey, RegistryEntry> kvp in _entries) {
            if (!kvp.Value.Ambiguous) {
                visible.Add(kvp.Value.Descriptor);
            }
        }

        visible.Sort(static (a, b) => string.CompareOrdinal(
            a.ProjectionType.FullName + "|" + a.Role?.ToString() + "|" + a.ComponentType.FullName,
            b.ProjectionType.FullName + "|" + b.Role?.ToString() + "|" + b.ComponentType.FullName));
        _descriptorsSnapshot = new ReadOnlyCollection<ProjectionViewOverrideDescriptor>(visible);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ProjectionViewOverrideDescriptor> Descriptors => _descriptorsSnapshot;

    /// <inheritdoc />
    public ProjectionViewOverrideDescriptor? Resolve(Type projectionType, ProjectionRole? role) {
        ArgumentNullException.ThrowIfNull(projectionType);

        if (_entries.TryGetValue(new RegistryKey(projectionType, role), out RegistryEntry exact)) {
            return exact.Ambiguous ? null : exact.Descriptor;
        }

        if (role is not null
            && _entries.TryGetValue(new RegistryKey(projectionType, Role: null), out RegistryEntry any)) {
            return any.Ambiguous ? null : any.Descriptor;
        }

        return null;
    }

    private void Register(ProjectionViewOverrideDescriptor descriptor) {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(descriptor.ProjectionType);
        ArgumentNullException.ThrowIfNull(descriptor.ComponentType);

        if (descriptor.ContractVersion <= 0) {
            _logger.LogWarning(
                "{DiagnosticId}: Invalid Level 4 view override contract version for projection {Projection} role {Role}. Expected: positive packed version. Got: {ContractVersion}. Fix: register with ProjectionViewOverrideContractVersion.Current. Docs: https://hexalith.dev/frontcomposer/diagnostics/HFC1045.",
                FcDiagnosticIds.HFC1045_ProjectionViewOverrideContractVersionMismatch,
                descriptor.ProjectionType.FullName,
                descriptor.Role?.ToString() ?? "<any>",
                descriptor.ContractVersion);
            return;
        }

        if (!IsCompatibleContractVersion(descriptor.ContractVersion)) {
            _logger.LogWarning(
                "{DiagnosticId}: Incompatible Level 4 view override contract version for projection {Projection} role {Role}. Expected: major {ExpectedMajor}. Got: {ContractVersion}. Fix: rebuild the replacement against the installed FrontComposer contracts. Docs: https://hexalith.dev/frontcomposer/diagnostics/HFC1045.",
                FcDiagnosticIds.HFC1045_ProjectionViewOverrideContractVersionMismatch,
                descriptor.ProjectionType.FullName,
                descriptor.Role?.ToString() ?? "<any>",
                ProjectionViewOverrideContractVersion.Major,
                descriptor.ContractVersion);
            return;
        }

        if (!IsCompatibleComponent(descriptor, out string? reason)) {
            _logger.LogWarning(
                "{DiagnosticId}: Invalid Level 4 view override component for projection {Projection} role {Role}. Expected: concrete Razor component with public [Parameter] Context of the projection view type. Got: {Component}. Fix: add the matching Context parameter or register a compatible component. Docs: https://hexalith.dev/frontcomposer/diagnostics/HFC1043. Reason: {Reason}",
                FcDiagnosticIds.HFC1043_ProjectionViewOverrideComponentInvalid,
                descriptor.ProjectionType.FullName,
                descriptor.Role?.ToString() ?? "<any>",
                descriptor.ComponentType.FullName,
                reason);
            return;
        }

        RegistryKey key = new(descriptor.ProjectionType, descriptor.Role);
        _ = _entries.AddOrUpdate(
            key,
            _ => new RegistryEntry(descriptor, Ambiguous: false),
            (_, existing) => {
                if (existing.Descriptor == descriptor) {
                    return existing;
                }

                _logger.LogWarning(
                    "{DiagnosticId}: Duplicate Level 4 view overrides registered for projection {Projection} role {Role}. Expected: one descriptor. Got: {Existing} and {New}. Fix: remove one registration or make one role-specific. Docs: https://hexalith.dev/frontcomposer/diagnostics/HFC1044.",
                    FcDiagnosticIds.HFC1044_ProjectionViewOverrideDuplicate,
                    descriptor.ProjectionType.FullName,
                    descriptor.Role?.ToString() ?? "<any>",
                    existing.Descriptor.ComponentType.FullName,
                    descriptor.ComponentType.FullName);
                return new RegistryEntry(existing.Descriptor, Ambiguous: true);
            });
    }

    private static bool IsCompatibleContractVersion(int contractVersion)
        => contractVersion / 1_000_000 == ProjectionViewOverrideContractVersion.Major;

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075:DynamicallyAccessedMembers",
        Justification = "View override component types are explicitly registered at startup; ProjectionViewOverrideDescriptor.ComponentType carries all member metadata through validation.")]
    private static bool IsCompatibleComponent(ProjectionViewOverrideDescriptor descriptor, out string? reason) {
        Type componentType = descriptor.ComponentType;

        if (componentType.ContainsGenericParameters) {
            reason = "Component type is open generic.";
            return false;
        }

        if (componentType.IsInterface) {
            reason = "Component type is an interface; expected a concrete Razor component class.";
            return false;
        }

        if (componentType.IsAbstract) {
            reason = "Component type is abstract; expected a concrete Razor component class.";
            return false;
        }

        if (!typeof(IComponent).IsAssignableFrom(componentType)) {
            reason = "Component type does not implement IComponent.";
            return false;
        }

        Type expectedContextType = typeof(ProjectionViewContext<>).MakeGenericType(descriptor.ProjectionType);
        PropertyInfo? contextProperty;
        try {
            contextProperty = componentType.GetProperty(
                "Context",
                BindingFlags.Public | BindingFlags.Instance);
        }
        catch (AmbiguousMatchException) {
            reason = "Component declares multiple public Context properties (shadowed via 'new').";
            return false;
        }

        if (contextProperty is null) {
            reason = "Missing public Context property.";
            return false;
        }

        if (contextProperty.PropertyType != expectedContextType) {
            reason = $"Context property type is {contextProperty.PropertyType.FullName}.";
            return false;
        }

        if (contextProperty.SetMethod is null || !contextProperty.SetMethod.IsPublic) {
            reason = "Context property has no public setter.";
            return false;
        }

        if (contextProperty.GetCustomAttribute<ParameterAttribute>() is null) {
            reason = "Context property is missing [Parameter].";
            return false;
        }

        reason = null;
        return true;
    }

    private readonly record struct RegistryKey(Type ProjectionType, ProjectionRole? Role);

    private readonly record struct RegistryEntry(ProjectionViewOverrideDescriptor Descriptor, bool Ambiguous);
}
