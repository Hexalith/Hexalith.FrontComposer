using System.Text;

using FsCheck;
using FsCheck.Xunit;

using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.State;

using Shouldly;

using Xunit;

using FArb = FsCheck.Fluent.Arb;
using FGen = FsCheck.Fluent.Gen;

namespace Hexalith.FrontComposer.Shell.Tests.State;

public sealed class StorageKeysTests {
    [Fact]
    public void BuildKey_IdentitySegments_TrimsNormalizesEncodesAndLowercasesEmailUser() {
        string key = StorageKeys.BuildKey(
            " tenant:Cafe\u0301 ",
            " ALICE:OPS@Example.COM ",
            "theme");

        key.ShouldBe("tenant%3ACaf%C3%A9:alice%3Aops%40example.com:theme");
    }

    [Fact]
    public void BuildKey_DiscriminatorWithColons_PreservesDocumentedShape() {
        string key = StorageKeys.BuildKey(
            "tenant-a",
            "alice@example.com",
            "datagrid",
            "counter:Hexalith.Samples.Counter.Projections.CounterProjection");

        key.ShouldBe("tenant-a:alice%40example.com:datagrid:counter:Hexalith.Samples.Counter.Projections.CounterProjection");
    }

    [Theory]
    [InlineData(null, "alice")]
    [InlineData("", "alice")]
    [InlineData(" ", "alice")]
    [InlineData("tenant-a", null)]
    [InlineData("tenant-a", "")]
    [InlineData("tenant-a", " ")]
    public void BuildKey_BlankTenantOrUser_Throws(string? tenantId, string? userId) {
        _ = Should.Throw<ArgumentException>(() => StorageKeys.BuildKey(tenantId!, userId!, "theme"));
        _ = Should.Throw<ArgumentException>(() => StorageKeys.BuildKey(tenantId!, userId!, "datagrid", "projection-page:Foo:s0-t25"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("datagrid:projection-page")]
    public void BuildKey_InvalidFeature_Throws(string? feature) {
        _ = Should.Throw<ArgumentException>(() => StorageKeys.BuildKey("tenant-a", "alice", feature!));
        _ = Should.Throw<ArgumentException>(() => StorageKeys.BuildKey("tenant-a", "alice", feature!, "projection-page:Foo:s0-t25"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void BuildKey_BlankDiscriminator_Throws(string? discriminator)
        => Should.Throw<ArgumentException>(() => StorageKeys.BuildKey("tenant-a", "alice", "datagrid", discriminator!));

    [Property(MaxTest = 200, Arbitrary = [typeof(StorageIdentityCaseArbitraries)])]
    public void BuildKey_IdentitySegments_ConvergeWithFrontComposerStorageKey(StorageIdentityCase identity) {
        ArgumentNullException.ThrowIfNull(identity);

        string canonicalTenant = FrontComposerStorageKey.CanonicalizeTenant(identity.TenantId);
        string canonicalUser = FrontComposerStorageKey.CanonicalizeUser(identity.UserId);

        string threeSegment = StorageKeys.BuildKey(identity.TenantId, identity.UserId, "theme");
        threeSegment.ShouldBe($"{canonicalTenant}:{canonicalUser}:theme");

        string fourSegment = StorageKeys.BuildKey(identity.TenantId, identity.UserId, "etag", identity.Discriminator);
        fourSegment.ShouldBe(string.Join(
            FrontComposerStorageKey.Separator,
            canonicalTenant,
            canonicalUser,
            "etag",
            identity.Discriminator));
    }

    /// <summary>
    /// A generated tenant/user/discriminator triple. <see cref="FromToken"/> decomposes one
    /// FsCheck-generated token (mixed radix) into core value, whitespace padding, Unicode
    /// normalization form, and letter-case decorations so the convergence property explores
    /// whitespace, colon, NFD/NFC, and mixed-case-email identity inputs generatively.
    /// </summary>
    public sealed record StorageIdentityCase(string TenantId, string UserId, string Discriminator) {
        private static readonly string[] TenantCores = [
            "tenant-a",
            "division:west",
            "Cafe\u0301",
            "Caf\u00E9",
            "ACME Corp",
            "east::zone",
        ];

        private static readonly string[] UserCores = [
            "ALICE@Example.COM",
            "service:principal",
            "Cafe\u0301.User@Example.COM",
            "Mixed.Case+Tag@Example.COM",
            "plain.user",
            "O'Brien+ops@Example.COM",
        ];

        private static readonly string[] Paddings = ["", " ", "\t", " \r\n ", "\u00A0"];

        private static readonly string[] Discriminators = [
            "projection-page:Foo:s0-t25",
            "counter:Hexalith.Samples.Counter.Projections.CounterProjection",
        ];

        public static StorageIdentityCase FromToken(int token) {
            int value = Math.Abs(token);
            int Next(int radix) {
                int result = value % radix;
                value /= radix;
                return result;
            }

            string tenantCore = ApplyForm(TenantCores[Next(TenantCores.Length)], Next(3));
            string tenant = Paddings[Next(Paddings.Length)] + tenantCore + Paddings[Next(Paddings.Length)];

            string userCore = ApplyCase(ApplyForm(UserCores[Next(UserCores.Length)], Next(3)), Next(3));
            string user = Paddings[Next(Paddings.Length)] + userCore + Paddings[Next(Paddings.Length)];

            return new StorageIdentityCase(tenant, user, Discriminators[Next(Discriminators.Length)]);
        }

        private static string ApplyForm(string value, int form) => form switch {
            1 => value.Normalize(NormalizationForm.FormC),
            2 => value.Normalize(NormalizationForm.FormD),
            _ => value,
        };

        private static string ApplyCase(string value, int letterCase) => letterCase switch {
            1 => value.ToUpperInvariant(),
            2 => value.ToLowerInvariant(),
            _ => value,
        };
    }

    public static class StorageIdentityCaseArbitraries {
        public static Arbitrary<StorageIdentityCase> StorageIdentity()
            => FArb.From(FGen.Select(FGen.Choose(0, int.MaxValue - 1), StorageIdentityCase.FromToken));
    }
}
