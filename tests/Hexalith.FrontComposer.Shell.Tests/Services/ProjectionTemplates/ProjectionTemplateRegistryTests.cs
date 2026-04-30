using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.ProjectionTemplates;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Services.ProjectionTemplates;

/// <summary>
/// Story 6-2 T10 — runtime registry resolution semantics: exact-role precedence, any-role
/// fallback, duplicate fail-closed handling, and assembly-source bootstrap.
/// </summary>
public sealed class ProjectionTemplateRegistryTests {
    private sealed class FakeProjection { }

    private sealed class FakeProjectionTwo { }

    private sealed class FakeTemplate { }

    private sealed class FakeTemplateTwo { }

    [Fact]
    public void Resolve_ExactRoleMatch_Wins() {
        ProjectionTemplateRegistry registry = NewRegistry(
            Descriptor(typeof(FakeProjection), ProjectionRole.DetailRecord, typeof(FakeTemplate)),
            Descriptor(typeof(FakeProjection), null, typeof(FakeTemplateTwo)));

        ProjectionTemplateDescriptor? resolved = registry.Resolve(typeof(FakeProjection), ProjectionRole.DetailRecord);

        resolved.ShouldNotBeNull();
        resolved!.TemplateType.ShouldBe(typeof(FakeTemplate));
    }

    [Fact]
    public void Resolve_FallsBackToAnyRoleSlot_WhenExactMatchMissing() {
        ProjectionTemplateRegistry registry = NewRegistry(
            Descriptor(typeof(FakeProjection), null, typeof(FakeTemplate)));

        ProjectionTemplateDescriptor? resolved = registry.Resolve(typeof(FakeProjection), ProjectionRole.Timeline);

        resolved.ShouldNotBeNull();
        resolved!.TemplateType.ShouldBe(typeof(FakeTemplate));
    }

    [Fact]
    public void Resolve_NoDescriptor_ReturnsNull() {
        ProjectionTemplateRegistry registry = NewRegistry();
        registry.Resolve(typeof(FakeProjection), null).ShouldBeNull();
    }

    [Fact]
    public void Resolve_AmbiguousSlot_FailsClosed() {
        ProjectionTemplateRegistry registry = NewRegistry(
            Descriptor(typeof(FakeProjection), null, typeof(FakeTemplate)),
            Descriptor(typeof(FakeProjection), null, typeof(FakeTemplateTwo)));

        registry.Resolve(typeof(FakeProjection), null).ShouldBeNull();
    }

    [Fact]
    public void Register_IdempotentSameDescriptor_DoesNotMarkAmbiguous() {
        ProjectionTemplateDescriptor descriptor = Descriptor(typeof(FakeProjection), null, typeof(FakeTemplate));
        ProjectionTemplateRegistry registry = NewRegistry(descriptor, descriptor);

        registry.Resolve(typeof(FakeProjection), null).ShouldNotBeNull();
    }

    [Fact]
    public void Register_DistinctProjections_KeepsBoth() {
        ProjectionTemplateRegistry registry = NewRegistry(
            Descriptor(typeof(FakeProjection), null, typeof(FakeTemplate)),
            Descriptor(typeof(FakeProjectionTwo), null, typeof(FakeTemplateTwo)));

        registry.Resolve(typeof(FakeProjection), null)!.TemplateType.ShouldBe(typeof(FakeTemplate));
        registry.Resolve(typeof(FakeProjectionTwo), null)!.TemplateType.ShouldBe(typeof(FakeTemplateTwo));
    }

    [Fact]
    public void Constructor_NullDescriptor_Throws() {
        ProjectionTemplateAssemblySource source = new([null!]);
        Should.Throw<ArgumentNullException>(() =>
            new ProjectionTemplateRegistry(NullLogger<ProjectionTemplateRegistry>.Instance, [source]));
    }

    [Fact]
    public void Constructor_BootstrapsFromAssemblySources() {
        ProjectionTemplateAssemblySource source = new(
        [
            Descriptor(typeof(FakeProjection), null, typeof(FakeTemplate)),
            Descriptor(typeof(FakeProjectionTwo), ProjectionRole.Timeline, typeof(FakeTemplateTwo)),
        ]);

        ProjectionTemplateRegistry registry = new(NullLogger<ProjectionTemplateRegistry>.Instance, [source]);

        registry.Resolve(typeof(FakeProjection), null).ShouldNotBeNull();
        registry.Resolve(typeof(FakeProjectionTwo), ProjectionRole.Timeline).ShouldNotBeNull();
    }

    [Fact]
    public void Resolve_DescriptorsExposesNonAmbiguousEntriesOnly() {
        ProjectionTemplateRegistry registry = NewRegistry(
            Descriptor(typeof(FakeProjection), null, typeof(FakeTemplate)),
            Descriptor(typeof(FakeProjection), null, typeof(FakeTemplateTwo)),
            Descriptor(typeof(FakeProjectionTwo), null, typeof(FakeTemplate)));

        registry.Descriptors.Count.ShouldBe(1);
        registry.Descriptors.Single().ProjectionType.ShouldBe(typeof(FakeProjectionTwo));
    }

    [Fact]
    public void Resolve_IncompatibleContractMajor_FailsClosed() {
        ProjectionTemplateRegistry registry = NewRegistry(
            new ProjectionTemplateDescriptor(typeof(FakeProjection), null, typeof(FakeTemplate), 2_000_000));

        registry.Resolve(typeof(FakeProjection), null).ShouldBeNull();
        registry.Descriptors.ShouldBeEmpty();
    }

    private static ProjectionTemplateRegistry NewRegistry(params ProjectionTemplateDescriptor[] descriptors)
        => new(
            NullLogger<ProjectionTemplateRegistry>.Instance,
            descriptors.Length == 0
                ? []
                : [new ProjectionTemplateAssemblySource(descriptors)]);

    private static ProjectionTemplateDescriptor Descriptor(Type projection, ProjectionRole? role, Type template)
        => new(projection, role, template, ProjectionTemplateContractVersion.Current);
}
