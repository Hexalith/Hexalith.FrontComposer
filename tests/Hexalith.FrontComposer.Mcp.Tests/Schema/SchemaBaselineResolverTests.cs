using System.Reflection;

using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Extensions;
using Hexalith.FrontComposer.Mcp.Schema;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Schema;

/// <summary>
/// AC4 / T1 — trusted baseline resolver. Memory rule "optional security parameters are an
/// anti-pattern" + AC4 require the resolver to consume only typed package-owned identifiers
/// (<see cref="SchemaBaselineProvenance"/> P-17 safe-identifier validated). Client-supplied paths,
/// path-traversal segments, absolute paths, and untrusted generated output must be rejected
/// before any comparison.
/// </summary>
public sealed class SchemaBaselineResolverTests {
    [Fact]
    public void Resolver_TypeExists_AndHasExpectedShape() {
        Type? resolver = TryFindResolverType();
        _ = resolver.ShouldNotBeNull(
            "AC4 / T1 require an ISchemaBaselineProvider (or extension of ISkillCorpusBaselineProvider). "
            + "Implement T1 before unskipping.");

        // Method shape: TryResolve(SchemaContractFamily, string packageOwner, string fixtureId, out SchemaBaselineSnapshot? snapshot)
        MethodInfo? tryResolve = resolver!.GetMethod("TryResolve", BindingFlags.Public | BindingFlags.Instance);
        _ = tryResolve.ShouldNotBeNull();
        ParameterInfo[] parameters = tryResolve!.GetParameters();
        parameters.Length.ShouldBe(4);
        parameters[0].ParameterType.ShouldBe(typeof(SchemaContractFamily));
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[2].ParameterType.ShouldBe(typeof(string));
        parameters[3].IsOut.ShouldBeTrue();
    }

    [Fact]
    public void AddFrontComposerMcp_RegistersBaselineProviderAsScoped() {
        // CK4-P3: the prior `Resolver_TypeExists_AndIsRegisteredAsScopedDi` test name promised
        // scoped-DI verification but the body inspected only type existence. Per AC4 / T1 the
        // baseline provider must be registered as scoped so per-request `RequestServices` scoping
        // works (see SchemaNegotiationRuntimeGate `LogAndReturn` chunk-2 patch). This test now
        // pins the lifetime so a regression to singleton fails loudly.
        ServiceCollection services = [];
        // AddFrontComposerMcp probes the service collection for tenant/visibility gates and fails
        // closed if absent. Register the AllowAll variants up front so the probe succeeds.
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        // Provide a minimal manifest so the descriptor registry can be activated by the probe inside
        // AddFrontComposerMcp.
        _ = services.AddFrontComposerMcp(options => options.Manifests.Add(new Hexalith.FrontComposer.Contracts.Mcp.McpManifest(
            "frontcomposer.mcp.v1",
            [],
            [])));

        ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISchemaBaselineProvider));
        _ = descriptor.ShouldNotBeNull(
            "AC4 / T1: AddFrontComposerMcp must register an ISchemaBaselineProvider.");
        descriptor!.Lifetime.ShouldBe(
            ServiceLifetime.Scoped,
            "AC4: ISchemaBaselineProvider must be scoped so per-request RequestServices flows enrichers / tenant context.");
    }

    [Fact]
    public void Resolver_RejectsClientSuppliedFilesystemHints() {
        // AC4: a caller-supplied package owner that looks like a filesystem path must not resolve
        // to anything. SchemaBaselineProvenance P-17 already rejects "../" via SafeIdentifier; the
        // resolver must surface that as a TryResolve=false (no exception escapes to the agent).
        var resolver = ResolverInvoker.CreateOrSkip();

        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "../etc/passwd", "baseline-known-v1")
            .Resolved.ShouldBeFalse("AC4 forbids path-traversal segments in package owner.");
        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "Hexalith.FrontComposer", "../baseline")
            .Resolved.ShouldBeFalse("AC4 forbids path-traversal segments in fixture id.");
        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "Hexalith.FrontComposer", "/abs/path/v1")
            .Resolved.ShouldBeFalse("AC4 forbids absolute paths in fixture id.");
    }

    [Fact]
    public void Resolver_RejectsCallerSuppliedAbsolutePathsForPackageOwner() {
        var resolver = ResolverInvoker.CreateOrSkip();

        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "C:/Users/agent/inj.json", "baseline-known-v1")
            .Resolved.ShouldBeFalse();
        resolver.TryResolve(SchemaContractFamily.ProjectionResource, @"\\unc\share\inj", "baseline-known-v1")
            .Resolved.ShouldBeFalse();
    }

    [Fact]
    public void Resolver_RejectsExternalPackageOwners() {
        // AC4: only package-owned identifiers are accepted. A different package owner must not
        // resolve, even if the safe-identifier pattern accepts the string.
        var resolver = ResolverInvoker.CreateOrSkip();

        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "Contoso.NotShipped", "baseline-known-v1")
            .Resolved.ShouldBeFalse("Resolver must whitelist package owners shipped with HFC, not honor any safe-identifier string.");
    }

    [Fact]
    public void Resolver_DefaultProviderReturnsFalseUntilD3MaterializesBaselines() {
        // C5 (Group D / chunk-2 re-review): the default `InMemorySchemaBaselineProvider` ships
        // with no snapshots until D3 (build-time baseline materialization) lands. Previously the
        // provider shipped placeholder snapshots that produced `SchemaMismatch` for every real
        // adopter request — fail-closed by default. New default behavior: TryResolve returns
        // false so the gate falls back to descriptor.Fingerprint byte-match. Hosts wanting
        // fixture-driven baselines must register their own ISchemaBaselineProvider.
        var resolver = ResolverInvoker.CreateOrSkip();

        ResolverInvoker.Outcome outcome = resolver.TryResolve(
            SchemaContractFamily.ProjectionResource,
            "Hexalith.FrontComposer",
            "baseline-known-v1");

        outcome.Resolved.ShouldBeFalse(
            "default provider must return false until D3 materializes real baselines (placeholder snapshots caused SchemaMismatch for every real adopter request).");
        outcome.Snapshot.ShouldBeNull();
    }

    [Fact]
    public void Resolver_RejectsNullOrWhitespaceArguments() {
        var resolver = ResolverInvoker.CreateOrSkip();

        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "", "baseline-known-v1")
            .Resolved.ShouldBeFalse();
        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "Hexalith.FrontComposer", "")
            .Resolved.ShouldBeFalse();
        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "   ", "baseline-known-v1")
            .Resolved.ShouldBeFalse();
    }

    private static Type? TryFindResolverType() => typeof(ISchemaBaselineProvider);

    private sealed class ResolverInvoker {
        private readonly object _instance;
        private readonly MethodInfo _tryResolve;

        private ResolverInvoker(object instance, MethodInfo tryResolve) {
            _instance = instance;
            _tryResolve = tryResolve;
        }

        public static ResolverInvoker CreateOrSkip() {
            Type? resolver = TryFindResolverType() ?? throw new InvalidOperationException(
                    "AC4 / T1 require ISchemaBaselineProvider; implement T1 before unskipping.");
            object instance;
            if (resolver.IsInterface) {
                instance = new InMemorySchemaBaselineProvider();
            }
            else {
                instance = Activator.CreateInstance(resolver)!;
            }

            MethodInfo? tryResolve = resolver.GetMethod("TryResolve", BindingFlags.Public | BindingFlags.Instance);
            _ = tryResolve.ShouldNotBeNull();
            return new ResolverInvoker(instance, tryResolve!);
        }

        public Outcome TryResolve(SchemaContractFamily family, string packageOwner, string fixtureId) {
            object?[] args = [family, packageOwner, fixtureId, null];
            bool ok = (bool)_tryResolve.Invoke(_instance, args)!;
            return new Outcome(ok, args[3] as SchemaBaselineSnapshot);
        }

        public sealed record Outcome(bool Resolved, SchemaBaselineSnapshot? Snapshot);
    }
}
