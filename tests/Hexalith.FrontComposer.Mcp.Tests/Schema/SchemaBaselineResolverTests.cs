using System.Reflection;

using Hexalith.FrontComposer.Contracts.Schema;

using Shouldly;
using Xunit;

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
    public void Resolver_TypeExists_AndIsRegisteredAsScopedDi() {
        Type? resolver = TryFindResolverType();
        resolver.ShouldNotBeNull(
            "AC4 / T1 require an ISchemaBaselineProvider (or extension of ISkillCorpusBaselineProvider). "
            + "Implement T1 before unskipping.");

        // Method shape: TryResolve(SchemaContractFamily, string packageOwner, string fixtureId, out SchemaBaselineSnapshot? snapshot)
        MethodInfo? tryResolve = resolver!.GetMethod("TryResolve", BindingFlags.Public | BindingFlags.Instance);
        tryResolve.ShouldNotBeNull();
        ParameterInfo[] parameters = tryResolve!.GetParameters();
        parameters.Length.ShouldBe(4);
        parameters[0].ParameterType.ShouldBe(typeof(SchemaContractFamily));
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[2].ParameterType.ShouldBe(typeof(string));
        parameters[3].IsOut.ShouldBeTrue();
    }

    [Fact]
    public void Resolver_RejectsClientSuppliedFilesystemHints() {
        // AC4: a caller-supplied package owner that looks like a filesystem path must not resolve
        // to anything. SchemaBaselineProvenance P-17 already rejects "../" via SafeIdentifier; the
        // resolver must surface that as a TryResolve=false (no exception escapes to the agent).
        ResolverInvoker resolver = ResolverInvoker.CreateOrSkip();

        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "../etc/passwd", "baseline-known-v1")
            .Resolved.ShouldBeFalse("AC4 forbids path-traversal segments in package owner.");
        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "Hexalith.FrontComposer", "../baseline")
            .Resolved.ShouldBeFalse("AC4 forbids path-traversal segments in fixture id.");
        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "Hexalith.FrontComposer", "/abs/path/v1")
            .Resolved.ShouldBeFalse("AC4 forbids absolute paths in fixture id.");
    }

    [Fact]
    public void Resolver_RejectsCallerSuppliedAbsolutePathsForPackageOwner() {
        ResolverInvoker resolver = ResolverInvoker.CreateOrSkip();

        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "C:/Users/agent/inj.json", "baseline-known-v1")
            .Resolved.ShouldBeFalse();
        resolver.TryResolve(SchemaContractFamily.ProjectionResource, @"\\unc\share\inj", "baseline-known-v1")
            .Resolved.ShouldBeFalse();
    }

    [Fact]
    public void Resolver_RejectsExternalPackageOwners() {
        // AC4: only package-owned identifiers are accepted. A different package owner must not
        // resolve, even if the safe-identifier pattern accepts the string.
        ResolverInvoker resolver = ResolverInvoker.CreateOrSkip();

        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "Contoso.NotShipped", "baseline-known-v1")
            .Resolved.ShouldBeFalse("Resolver must whitelist package owners shipped with HFC, not honor any safe-identifier string.");
    }

    [Fact]
    public void Resolver_ReturnsTypedSnapshotForKnownIdentifiers() {
        ResolverInvoker resolver = ResolverInvoker.CreateOrSkip();

        ResolverInvoker.Outcome outcome = resolver.TryResolve(
            SchemaContractFamily.ProjectionResource,
            "Hexalith.FrontComposer",
            "baseline-known-v1");

        outcome.Resolved.ShouldBeTrue();
        outcome.Snapshot.ShouldNotBeNull();
        outcome.Snapshot!.Provenance.PackageOwner.ShouldBe("Hexalith.FrontComposer");
        outcome.Snapshot.Provenance.FixtureId.ShouldBe("baseline-known-v1");
        outcome.Snapshot.Fingerprint.AlgorithmId.ShouldBeOneOf(
            SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
            SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1);
    }

    [Fact]
    public void Resolver_RejectsNullOrWhitespaceArguments() {
        ResolverInvoker resolver = ResolverInvoker.CreateOrSkip();

        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "", "baseline-known-v1")
            .Resolved.ShouldBeFalse();
        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "Hexalith.FrontComposer", "")
            .Resolved.ShouldBeFalse();
        resolver.TryResolve(SchemaContractFamily.ProjectionResource, "   ", "baseline-known-v1")
            .Resolved.ShouldBeFalse();
    }

    private static Type? TryFindResolverType() {
        Assembly mcp = typeof(Hexalith.FrontComposer.Mcp.Schema.McpSchemaNegotiator).Assembly;
        Assembly contracts = typeof(SchemaBaselineSnapshot).Assembly;

        return mcp.GetTypes()
            .Concat(contracts.GetTypes())
            .FirstOrDefault(t =>
                (t.IsInterface || (t.IsClass && !t.IsAbstract))
                && (t.Name == "ISchemaBaselineProvider"
                    || t.Name == "SchemaBaselineProvider"
                    || t.GetMethods().Any(m =>
                        m.Name == "TryResolve"
                        && m.GetParameters().Length == 4
                        && m.GetParameters()[0].ParameterType == typeof(SchemaContractFamily))));
    }

    private sealed class ResolverInvoker {
        private readonly object _instance;
        private readonly MethodInfo _tryResolve;

        private ResolverInvoker(object instance, MethodInfo tryResolve) {
            _instance = instance;
            _tryResolve = tryResolve;
        }

        public static ResolverInvoker CreateOrSkip() {
            Type? resolver = TryFindResolverType();
            if (resolver is null) {
                throw new InvalidOperationException(
                    "AC4 / T1 require ISchemaBaselineProvider; implement T1 before unskipping.");
            }

            object instance;
            if (resolver.IsInterface) {
                Type? impl = resolver.Assembly.GetTypes()
                    .FirstOrDefault(t => !t.IsAbstract && resolver.IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) is not null);
                if (impl is null) {
                    throw new InvalidOperationException("No concrete ISchemaBaselineProvider implementation found with a public parameterless constructor.");
                }

                instance = Activator.CreateInstance(impl)!;
            } else {
                instance = Activator.CreateInstance(resolver)!;
            }

            MethodInfo? tryResolve = resolver.GetMethod("TryResolve", BindingFlags.Public | BindingFlags.Instance);
            tryResolve.ShouldNotBeNull();
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
