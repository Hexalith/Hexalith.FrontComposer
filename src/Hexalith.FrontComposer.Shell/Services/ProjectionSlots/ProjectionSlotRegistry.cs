using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services.ProjectionSlots;

/// <summary>
/// Runtime Level 3 field-slot registry. Stores descriptor metadata only and resolves by
/// projection type, role, and field name.
/// </summary>
public sealed class ProjectionSlotRegistry : IProjectionSlotRegistry {
    private readonly ConcurrentDictionary<RegistryKey, RegistryEntry> _entries = new();
    private readonly ILogger<ProjectionSlotRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionSlotRegistry"/> class.
    /// </summary>
    public ProjectionSlotRegistry(
        ILogger<ProjectionSlotRegistry> logger,
        IEnumerable<ProjectionSlotDescriptorSource> sources) {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(sources);
        _logger = logger;

        foreach (ProjectionSlotDescriptorSource source in sources) {
            foreach (ProjectionSlotDescriptor descriptor in source.Descriptors) {
                Register(descriptor);
            }
        }
    }

    /// <inheritdoc />
    public ProjectionSlotDescriptor? Resolve(Type projectionType, ProjectionRole? role, string fieldName) {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        if (_entries.TryGetValue(new RegistryKey(projectionType, role, fieldName), out RegistryEntry exact)) {
            return exact.Ambiguous ? null : exact.Descriptor;
        }

        if (role is not null
            && _entries.TryGetValue(new RegistryKey(projectionType, Role: null, fieldName), out RegistryEntry any)) {
            return any.Ambiguous ? null : any.Descriptor;
        }

        return null;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ProjectionSlotDescriptor> Descriptors {
        get {
            List<ProjectionSlotDescriptor> descriptors = [];
            foreach (KeyValuePair<RegistryKey, RegistryEntry> kvp in _entries) {
                if (!kvp.Value.Ambiguous) {
                    descriptors.Add(kvp.Value.Descriptor);
                }
            }

            return descriptors;
        }
    }

    private void Register(ProjectionSlotDescriptor descriptor) {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(descriptor.ProjectionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.FieldName);
        ArgumentNullException.ThrowIfNull(descriptor.FieldType);
        ArgumentNullException.ThrowIfNull(descriptor.ComponentType);

        if (!IsSupportedContractVersion(descriptor.ContractVersion)) {
            _logger.LogWarning(
                "HFC1041: Level 3 slot descriptor for projection {Projection} field {Field} has incompatible contract version {ContractVersion}. Expected major {ExpectedMajor}. Descriptor ignored.",
                descriptor.ProjectionType.FullName,
                descriptor.FieldName,
                descriptor.ContractVersion,
                ProjectionSlotContractVersion.Major);
            return;
        }

        if (!IsCompatibleComponent(descriptor, out string? reason)) {
            _logger.LogWarning(
                "HFC1039: Invalid Level 3 slot component for projection {Projection} field {Field}. Expected: Razor component with [Parameter] Context of type FieldSlotContext<{Projection},{FieldType}>. Got: {Component}. Fix: add the matching Context parameter or register a compatible component. Docs: https://hexalith.dev/frontcomposer/diagnostics/HFC1039. Reason: {Reason}",
                descriptor.ProjectionType.FullName,
                descriptor.FieldName,
                descriptor.ProjectionType.FullName,
                descriptor.FieldType.FullName,
                descriptor.ComponentType.FullName,
                reason);
            return;
        }

        RegistryKey key = new(descriptor.ProjectionType, descriptor.Role, descriptor.FieldName);
        _ = _entries.AddOrUpdate(
            key,
            _ => new RegistryEntry(descriptor, Ambiguous: false),
            (_, existing) => {
                if (existing.Descriptor == descriptor) {
                    return existing;
                }

                _logger.LogWarning(
                    "HFC1040: Duplicate Level 3 slot overrides registered for projection {Projection} role {Role} field {Field}. Expected: one descriptor. Got: {Existing} and {New}. Fix: remove one registration or make one role-specific. Docs: https://hexalith.dev/frontcomposer/diagnostics/HFC1040.",
                    descriptor.ProjectionType.FullName,
                    descriptor.Role?.ToString() ?? "<any>",
                    descriptor.FieldName,
                    existing.Descriptor.ComponentType.FullName,
                    descriptor.ComponentType.FullName);
                return new RegistryEntry(existing.Descriptor, Ambiguous: true);
            });
    }

    private static bool IsSupportedContractVersion(int contractVersion)
        => contractVersion / 1_000_000 == ProjectionSlotContractVersion.Major;

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075:DynamicallyAccessedMembers",
        Justification = "Slot component types are registered explicitly by adopter startup code or generated registrations; Story 6-6 will add richer trim diagnostics.")]
    private static bool IsCompatibleComponent(ProjectionSlotDescriptor descriptor, out string? reason) {
        Type componentType = descriptor.ComponentType;
        if (componentType.ContainsGenericParameters) {
            reason = "Component type is open generic.";
            return false;
        }

        if (!typeof(IComponent).IsAssignableFrom(componentType)) {
            reason = "Component type does not implement IComponent.";
            return false;
        }

        Type expectedContextType = typeof(FieldSlotContext<,>).MakeGenericType(
            descriptor.ProjectionType,
            descriptor.FieldType);
        PropertyInfo? contextProperty = componentType.GetProperty(
            "Context",
            BindingFlags.Public | BindingFlags.Instance);
        if (contextProperty is null) {
            reason = "Missing public Context property.";
            return false;
        }

        if (contextProperty.PropertyType != expectedContextType) {
            reason = $"Context property type is {contextProperty.PropertyType.FullName}.";
            return false;
        }

        if (contextProperty.GetCustomAttribute<ParameterAttribute>() is null) {
            reason = "Context property is missing [Parameter].";
            return false;
        }

        reason = null;
        return true;
    }

    private readonly record struct RegistryKey(Type ProjectionType, ProjectionRole? Role, string FieldName);

    private readonly record struct RegistryEntry(ProjectionSlotDescriptor Descriptor, bool Ambiguous);
}
