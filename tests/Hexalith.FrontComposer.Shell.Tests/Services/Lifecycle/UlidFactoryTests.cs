using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Lifecycle;

/// <summary>
/// Story 3.3 Task 3 — unit tests for the <see cref="UlidFactory"/> command identity contract.
/// </summary>
public class UlidFactoryTests {
    private static readonly Regex UlidPattern = new("^[0-9A-HJKMNP-TV-Z]{26}$", RegexOptions.Compiled);

    [Fact]
    public void NewUlid_ReturnsValidCrockfordBase32_26Chars() {
        UlidFactory factory = new();

        string ulid = factory.NewUlid();

        UlidPattern.IsMatch(ulid).ShouldBeTrue($"'{ulid}' must be a 26-char Crockford Base32 ULID");
    }

    [Fact]
    public void NewUlid_ReturnsMonotonicStrings_WhenCalledRapidly() {
        UlidFactory factory = new();
        List<string> ulids = [];

        for (int i = 0; i < 10; i++) {
            ulids.Add(factory.NewUlid());
            Thread.Sleep(2);
        }

        List<string> sorted = ulids.OrderBy(x => x, StringComparer.Ordinal).ToList();
        ulids.ShouldBe(sorted, "ULIDs generated across 2 ms gaps must sort lexicographically in emission order");
    }

    [Fact]
    public async Task NewUlid_IsThreadSafe() {
        UlidFactory factory = new();
        string[] values = new string[100];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 100),
            (i, _) => {
                values[i] = factory.NewUlid();
                return ValueTask.CompletedTask;
            });

        values.Distinct().Count().ShouldBe(100, "100 parallel NewUlid calls must yield 100 distinct values");
    }

    [Fact]
    public void UlidFactory_ServiceRegistration_ResolvesAsIUlidFactory() {
        ServiceCollection services = new();
        services.AddSingleton<IUlidFactory, UlidFactory>();

        using ServiceProvider sp = services.BuildServiceProvider();
        IUlidFactory resolved = sp.GetRequiredService<IUlidFactory>();

        resolved.ShouldBeOfType<UlidFactory>();
    }

    [Fact]
    public void ProductionCommandIdentityPaths_DoNotUseGuidIdentityApis() {
        string root = ProjectRoot();
        string[] relativePaths = [
            "src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs",
            "src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs",
            "src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs",
            "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs",
        ];
        string[] forbidden = [
            "Guid.NewGuid()",
            "Guid.Parse(",
            "Guid.TryParse(",
            ".ToString(\"N\")",
            ".ToString(\"D\")",
        ];

        foreach (string relativePath in relativePaths) {
            string text = File.ReadAllText(Path.Combine(root, relativePath));
            foreach (string token in forbidden) {
                text.ShouldNotContain(token, Case.Sensitive, $"{relativePath} must not use {token} for command MessageId/CorrelationId identity.");
            }
        }
    }

    [Fact]
    public void NewUlid_EntropyIsCryptographic_NotPredictableFromPriorOutputs() {
        UlidFactory factory = new();
        const int sampleSize = 1000;
        int[] highNibbleCounts = new int[32];

        for (int i = 0; i < sampleSize; i++) {
            string ulid = factory.NewUlid();
            char entropyHead = ulid[10];
            int bucket = DecodeCrockford(entropyHead);
            highNibbleCounts[bucket]++;
        }

        double expected = sampleSize / 32.0;
        double chiSquare = highNibbleCounts.Sum(c => ((c - expected) * (c - expected)) / expected);

        chiSquare.ShouldBeLessThan(
            60.0,
            $"chi-square statistic {chiSquare:F2} suggests non-uniform entropy; NUlid should use RandomNumberGenerator");
    }

    private static int DecodeCrockford(char c) {
        const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        int i = alphabet.IndexOf(char.ToUpperInvariant(c));
        return i >= 0 ? i : throw new ArgumentException($"invalid Crockford char '{c}'");
    }

    private static string ProjectRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx"))) {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate Hexalith.FrontComposer.slnx by walking up from " + AppContext.BaseDirectory);
    }
}
