using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

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

        List<DuplicateReport> duplicates = [];
        int sourceIndex = -1;
        foreach (ProjectionViewOverrideDescriptorSource source in sources) {
            sourceIndex++;
            // P14 — defend against null elements from misconfigured DI registrations so a
            // single bad source does not take the whole registry boot down without diagnostics.
            if (source is null) {
                _logger.LogWarning(
                    "Null ProjectionViewOverrideDescriptorSource at index {Index} skipped during registry construction. Fix the DI registration that produced a null source.",
                    sourceIndex);
                continue;
            }

            foreach (ProjectionViewOverrideDescriptor descriptor in source.Descriptors) {
                Register(descriptor, duplicates);
            }
        }

        // DN1 / AC7 / D6 — duplicates are deterministic hard failures. Construction throws so
        // adopters discover the misregistration at startup instead of silently falling through
        // to generated rendering.
        if (duplicates.Count > 0) {
            duplicates.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));
            StringBuilder message = new();
            _ = message.Append("HFC1044: Duplicate Level 4 view overrides registered. ");
            for (int i = 0; i < duplicates.Count; i++) {
                if (i > 0) {
                    _ = message.Append("; ");
                }
                _ = message.Append(duplicates[i].Detail);
            }

            throw new InvalidOperationException(message.ToString());
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

        // DN3 — when the role-specific tuple is present but ambiguous, we return null without
        // falling through to the role-agnostic tuple. The DN1 hard-fail in the constructor
        // means Ambiguous=true is unreachable in practice; the defensive null-return remains
        // in case future intake paths accumulate ambiguity post-construction.
        if (_entries.TryGetValue(new RegistryKey(projectionType, role), out RegistryEntry exact)) {
            return exact.Ambiguous ? null : exact.Descriptor;
        }

        if (role is not null
            && _entries.TryGetValue(new RegistryKey(projectionType, Role: null), out RegistryEntry any)) {
            return any.Ambiguous ? null : any.Descriptor;
        }

        return null;
    }

    private void Register(ProjectionViewOverrideDescriptor descriptor, List<DuplicateReport> duplicates) {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(descriptor.ProjectionType);
        ArgumentNullException.ThrowIfNull(descriptor.ComponentType);

        if (descriptor.ContractVersion <= 0) {
            // P10 — include RegistrationSource so adopters can find the originating call site.
            _logger.LogWarning(
                "{DiagnosticId}: Invalid Level 4 view override contract version for projection {Projection} role {Role}. Expected: positive packed version. Got: {ContractVersion}. Source: {Source}. Fix: register with ProjectionViewOverrideContractVersion.Current. Docs: https://hexalith.dev/frontcomposer/diagnostics/HFC1045.",
                FcDiagnosticIds.HFC1045_ProjectionViewOverrideContractVersionMismatch,
                descriptor.ProjectionType.FullName,
                descriptor.Role?.ToString() ?? "<any>",
                descriptor.ContractVersion,
                descriptor.RegistrationSource);
            return;
        }

        if (!IsCompatibleContractVersion(descriptor.ContractVersion)) {
            _logger.LogWarning(
                "{DiagnosticId}: Incompatible Level 4 view override contract version for projection {Projection} role {Role}. Expected: major {ExpectedMajor}. Got: {ContractVersion}. Source: {Source}. Fix: rebuild the replacement against the installed FrontComposer contracts. Docs: https://hexalith.dev/frontcomposer/diagnostics/HFC1045.",
                FcDiagnosticIds.HFC1045_ProjectionViewOverrideContractVersionMismatch,
                descriptor.ProjectionType.FullName,
                descriptor.Role?.ToString() ?? "<any>",
                ProjectionViewOverrideContractVersion.Major,
                descriptor.ContractVersion,
                descriptor.RegistrationSource);
            return;
        }

        // P12 — within an accepted Major, log Information-level when the descriptor's packed
        // version differs from the currently installed build. Drift is non-blocking but
        // observable, satisfying T7's "version drift remains diagnosable" requirement.
        if (descriptor.ContractVersion != ProjectionViewOverrideContractVersion.Current) {
            _logger.LogInformation(
                "{DiagnosticId}: Level 4 view override contract version drift for projection {Projection} role {Role}. Installed: {Current}. Got: {ContractVersion}. Source: {Source}. Selection proceeds.",
                FcDiagnosticIds.HFC1045_ProjectionViewOverrideContractVersionMismatch,
                descriptor.ProjectionType.FullName,
                descriptor.Role?.ToString() ?? "<any>",
                ProjectionViewOverrideContractVersion.Current,
                descriptor.ContractVersion,
                descriptor.RegistrationSource);
        }

        if (!IsCompatibleComponent(descriptor, out string? reason)) {
            _logger.LogWarning(
                "{DiagnosticId}: Invalid Level 4 view override component for projection {Projection} role {Role}. Expected: concrete Razor component with public [Parameter] Context of the projection view type. Got: {Component}. Source: {Source}. Fix: add the matching Context parameter or register a compatible component. Docs: https://hexalith.dev/frontcomposer/diagnostics/HFC1043. Reason: {Reason}",
                FcDiagnosticIds.HFC1043_ProjectionViewOverrideComponentInvalid,
                descriptor.ProjectionType.FullName,
                descriptor.Role?.ToString() ?? "<any>",
                descriptor.ComponentType.FullName,
                descriptor.RegistrationSource,
                reason);
            return;
        }

        RegistryKey key = new(descriptor.ProjectionType, descriptor.Role);
        _ = _entries.AddOrUpdate(
            key,
            _ => new RegistryEntry(descriptor, Ambiguous: false),
            (_, existing) => {
                // P11 — idempotent re-registration: same component+version for the same
                // (projection, role) tuple is a no-op even when RegistrationSource differs
                // (e.g., one helper called from two startup hooks).
                if (IsSameRegistration(existing.Descriptor, descriptor)) {
                    return existing;
                }

                // P13 — sort the duplicate pair before logging so HFC1044 message content
                // is deterministic across DI enumeration orders.
                string firstName = existing.Descriptor.ComponentType.FullName ?? string.Empty;
                string secondName = descriptor.ComponentType.FullName ?? string.Empty;
                string a;
                string b;
                string aSource;
                string bSource;
                if (string.CompareOrdinal(firstName, secondName) <= 0) {
                    a = firstName;
                    b = secondName;
                    aSource = existing.Descriptor.RegistrationSource;
                    bSource = descriptor.RegistrationSource;
                }
                else {
                    a = secondName;
                    b = firstName;
                    aSource = descriptor.RegistrationSource;
                    bSource = existing.Descriptor.RegistrationSource;
                }

                _logger.LogError(
                    "{DiagnosticId}: Duplicate Level 4 view overrides registered for projection {Projection} role {Role}. Got: {ComponentA} (source: {SourceA}) and {ComponentB} (source: {SourceB}). Fix: remove one registration or make one role-specific. Docs: https://hexalith.dev/frontcomposer/diagnostics/HFC1044.",
                    FcDiagnosticIds.HFC1044_ProjectionViewOverrideDuplicate,
                    descriptor.ProjectionType.FullName,
                    descriptor.Role?.ToString() ?? "<any>",
                    a,
                    aSource,
                    b,
                    bSource);

                duplicates.Add(new DuplicateReport(
                    Key: descriptor.ProjectionType.FullName + "|" + (descriptor.Role?.ToString() ?? "<any>") + "|" + a + "|" + b,
                    Detail: $"projection {descriptor.ProjectionType.FullName} role {descriptor.Role?.ToString() ?? "<any>"} matched by {a} (source: {aSource}) and {b} (source: {bSource})"));

                return new RegistryEntry(existing.Descriptor, Ambiguous: true);
            });
    }

    private static bool IsSameRegistration(ProjectionViewOverrideDescriptor a, ProjectionViewOverrideDescriptor b)
        => a.ProjectionType == b.ProjectionType
            && a.Role == b.Role
            && a.ComponentType == b.ComponentType
            && a.ContractVersion == b.ContractVersion;

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

    private readonly record struct DuplicateReport(string Key, string Detail);
}
