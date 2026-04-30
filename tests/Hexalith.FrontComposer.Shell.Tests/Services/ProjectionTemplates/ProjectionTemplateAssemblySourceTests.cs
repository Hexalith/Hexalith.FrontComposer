using System.Collections.Generic;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.ProjectionTemplates;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Services.ProjectionTemplates;

/// <summary>
/// Story 6-2 T10 — assembly-source reflection contract: returns descriptors when the
/// generated manifest type exists, and an empty list otherwise. No broad assembly scan.
/// </summary>
public sealed class ProjectionTemplateAssemblySourceTests {
    [Fact]
    public void ResolveDescriptors_AssemblyWithoutGeneratedType_ReturnsEmpty() {
        // Microsoft.Extensions.Logging.Abstractions has no generated manifest — exercises the
        // not-found path without needing a synthetic test assembly.
        Assembly assembly = typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger).Assembly;
        IReadOnlyList<ProjectionTemplateDescriptor> descriptors =
            ProjectionTemplateAssemblySource.ResolveDescriptors(assembly);
        descriptors.ShouldBeEmpty();
    }

    [Fact]
    public void ResolveDescriptors_NullAssembly_Throws() {
        Should.Throw<ArgumentNullException>(() =>
            ProjectionTemplateAssemblySource.ResolveDescriptors(null!));
    }

    [Fact]
    public void Constructor_NullDescriptors_Throws() {
        Should.Throw<ArgumentNullException>(() =>
            new ProjectionTemplateAssemblySource(null!));
    }

    [Fact]
    public void Constructor_StoresDescriptorList() {
        ProjectionTemplateDescriptor descriptor = new(
            typeof(string),
            (ProjectionRole?)null,
            typeof(int),
            ProjectionTemplateContractVersion.Current);
        ProjectionTemplateAssemblySource source = new([descriptor]);
        source.Descriptors.Count.ShouldBe(1);
        source.Descriptors[0].ShouldBe(descriptor);
    }
}
