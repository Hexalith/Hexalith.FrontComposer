using System.Collections.Concurrent;
using System.Collections.Generic;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.DevMode;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Customization;

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
    private readonly ICustomizationContractRejectionLog? _rejectionLog;

    public ProjectionTemplateRegistry(
        ILogger<ProjectionTemplateRegistry> logger,
        IEnumerable<ProjectionTemplateAssemblySource> assemblySources,
        ICustomizationContractRejectionLog? rejectionLog = null) {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(assemblySources);
        _logger = logger;
        _rejectionLog = rejectionLog;

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

        CustomizationContractVersionComparison comparison =
            CustomizationContractVersion.Compare(descriptor.ContractVersion, ProjectionTemplateContractVersion.Current);
        if (!comparison.CanSelect) {
            // P13 — branch the message on Decision so MajorMismatch is named distinctly from
            // other reject paths. Currently MajorMismatch is the only !CanSelect outcome, but
            // future enum members may add more — Decision in the log lets operators triage.
            _logger.LogWarning(
                "{DiagnosticId}: ProjectionTemplateDescriptor for projection {Projection} (role {Role}) uses incompatible contract version {Decision} expected {ExpectedMajor}.{ExpectedMinor}.{ExpectedBuild} got {ActualMajor}.{ActualMinor}.{ActualBuild}; descriptor ignored. Fix: rebuild the template against the installed framework. Docs: https://hexalith.github.io/FrontComposer/diagnostics/HFC1035",
                FcDiagnosticIds.HFC1035_ProjectionTemplateContractVersionMismatch,
                descriptor.ProjectionType.FullName,
                descriptor.Role?.ToString() ?? "<any>",
                comparison.Decision,
                comparison.Expected.Major,
                comparison.Expected.Minor,
                comparison.Expected.Build,
                comparison.Actual.Major,
                comparison.Actual.Minor,
                comparison.Actual.Build);
            // P17 / AC2 — record the rejection so the strict-mode validation gate can fail
            // closed at startup when the adopter opted into FailClosedOnMajorMismatch.
            _rejectionLog?.Record(new CustomizationContractRejection(
                Level: CustomizationLevel.Level2,
                ProjectionTypeName: descriptor.ProjectionType.FullName ?? string.Empty,
                ComponentTypeName: descriptor.TemplateType.FullName ?? string.Empty,
                Role: descriptor.Role?.ToString() ?? "<any>",
                FieldName: null,
                Comparison: comparison,
                DiagnosticId: FcDiagnosticIds.HFC1035_ProjectionTemplateContractVersionMismatch));
            return;
        }

        // P13 — surface MinorDrift as an Information-level message at registry hydration so
        // adopters running newer framework Minor than the manifest can see why an override
        // selected without diagnostic at build time. AC3 (amended): runtime registries emit
        // equivalent log messages naming expected/actual + rebuild guidance.
        if (comparison.ShouldReportDiagnostic
            && comparison.Decision == CustomizationContractVersionDecision.MinorDrift) {
            _logger.LogInformation(
                "{DiagnosticId}: ProjectionTemplateDescriptor for projection {Projection} (role {Role}) targets contract minor {ExpectedMajor}.{ExpectedMinor}.{ExpectedBuild} but installed framework reports {ActualMajor}.{ActualMinor}.{ActualBuild}. Override accepted (source-compatible). Fix: rebuild the template to silence this message. Docs: https://hexalith.github.io/FrontComposer/diagnostics/HFC1036",
                FcDiagnosticIds.HFC1036_ProjectionTemplateContractVersionDrift,
                descriptor.ProjectionType.FullName,
                descriptor.Role?.ToString() ?? "<any>",
                comparison.Expected.Major,
                comparison.Expected.Minor,
                comparison.Expected.Build,
                comparison.Actual.Major,
                comparison.Actual.Minor,
                comparison.Actual.Build);
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

}
