namespace Hexalith.FrontComposer.Shell.Tests.Governance;

/// <summary>
/// Serializes analyzer-policy governance against the live test output graph.
/// </summary>
/// <remarks>
/// The governed tests intentionally execute repository-wide no-incremental builds. Running those
/// builds beside ordinary solution-level tests can replace assemblies and generated files beneath
/// active test hosts, so this collection must not overlap another collection.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AnalyzerPolicyGovernanceTestGroup
{
    /// <summary>Gets the non-parallel collection name.</summary>
    public const string Name = nameof(AnalyzerPolicyGovernanceTestGroup);
}
