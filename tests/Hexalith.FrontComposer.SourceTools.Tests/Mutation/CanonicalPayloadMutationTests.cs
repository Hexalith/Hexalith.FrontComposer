using System.Security.Cryptography;
using System.Text;

using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Checks the protocol and canonical bytes retained by the schema fingerprint transforms.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class CanonicalPayloadMutationTests
{
    /// <summary>Checks the canonical protocol row and the full lifecycle fingerprint.</summary>
    [Fact]
    public void LifecycleResultRetainsItsProtocolUri()
    {
        GeneratedSchemaPayload payload = SchemaFingerprintTransform.CreateLifecycleResultPayload();

        payload.Json.Split('\n').ShouldContain("protocol=frontcomposer\\://lifecycle/result");
        payload.Fingerprint.Value.ShouldBe("4cfad4b6ac6bcd5f9cd5ab0254da79125e7f03776e3397a34e711166d8d3d9de");
    }

    /// <summary>Checks legacy and framed singleton behavior on each side of the resource.</summary>
    [Theory]
    [InlineData("Alpha", "v1", "Alpha")]
    [InlineData("", "v2", "0\\:")]
    [InlineData(" \t", "v2", "0\\:")]
    [InlineData("|Ref", "v2", "4\\:\\|Ref")]
    public void ACollectionOnEitherSideSelectsItsCanonicalRepresentation(string input, string version, string value)
    {
        AssertSkillPayload(CreateSkill([input], []), version, value, string.Empty);
        AssertSkillPayload(CreateSkill([], [input]), version, string.Empty, value);
    }

    /// <summary>Checks that empty lists and a recoverable legacy join retain compatible bytes.</summary>
    [Fact]
    public void RecoverableCollectionBoundariesRetainTheirLegacyBytes()
    {
        AssertSkillPayload(CreateSkill([], []), "v1", string.Empty, string.Empty);

        GeneratedSchemaPayload recoverable = CreateSkill(["", "Ref"], []);
        AssertSkillPayload(recoverable, "v1", "\\|Ref", string.Empty);
        recoverable.Fingerprint.Value.ShouldNotBe(CreateSkill(["|Ref"], []).Fingerprint.Value);
    }

    /// <summary>Checks framed element lengths, ordinal order, and input permutation invariance.</summary>
    [Fact]
    public void FramingEitherCollectionPreservesCanonicalOrderAndLengthsOnBothSides()
    {
        const string mixed = "5\\:Alpha4\\:\\|Ref";
        const string ordinary = "4\\:B.cs4\\:a.cs";

        GeneratedSchemaPayload framedReferences = CreateSkill(["|Ref", "Alpha"], ["a.cs", "B.cs"]);
        AssertSkillPayload(framedReferences, "v2", mixed, ordinary);
        CreateSkill(["Alpha", "|Ref"], ["B.cs", "a.cs"]).Json.ShouldBe(framedReferences.Json);

        GeneratedSchemaPayload framedSamples = CreateSkill(["a.cs", "B.cs"], ["|Ref", "Alpha"]);
        AssertSkillPayload(framedSamples, "v2", ordinary, mixed);
        CreateSkill(["B.cs", "a.cs"], ["Alpha", "|Ref"]).Fingerprint.Value.ShouldBe(framedSamples.Fingerprint.Value);

        AssertSkillPayload(CreateSkill(["|Ref", "Alpha"], ["|Ref", "Alpha"]), "v2", mixed, mixed);
    }

    /// <summary>Checks the existing defensive normalization of a null entry in a framed collection.</summary>
    [Fact]
    public void FramingNormalizesNullToAnEmptyElement()
    {
        GeneratedSchemaPayload payload = CreateSkill([null!, "|Ref"], []);

        AssertSkillPayload(payload, "v2", "0\\:4\\:\\|Ref", string.Empty);
        payload.Fingerprint.Value.ShouldBe(CreateSkill(["", "|Ref"], []).Fingerprint.Value);
    }

    private static GeneratedSchemaPayload CreateSkill(IReadOnlyList<string> references, IReadOnlyList<string> samples)
        => SchemaFingerprintTransform.CreateSkillCorpusResourcePayload(
            "skill", "1.0", "frontcomposer://skill", "Skill", "text/markdown", 1, references, samples);

    private static void AssertSkillPayload(GeneratedSchemaPayload payload, string version, string references, string samples)
    {
        string expected = $$"""
            root=frontcomposer.schema.contract.v1
            family=SkillCorpusResource
            contractId=skill
            schemaVersion=frontcomposer.skill-corpus.{{version}}
            boundedContext=
            fqn=
            protocol=frontcomposer\://skill
            collections=fields:non-structural-sorted:name
            metadata.contentType=text/markdown
            metadata.order=1
            metadata.publicApiReferences={{references}}
            metadata.samplePaths={{samples}}
            metadata.title=Skill
            metadata.version=1.0
            """.ReplaceLineEndings("\n") + "\n";

        payload.Json.ShouldBe(expected);
        payload.Fingerprint.Value.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(expected))));
    }
}
