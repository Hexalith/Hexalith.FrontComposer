using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

[Trait("Category", "Governance")]
public sealed class MutationReportValidationTests {
    [Theory]
    [InlineData("qualified", true, "")]
    [InlineData("ignored", true, "")]
    [InlineData("version", false, "Unqualified CompileError")]
    [InlineData("reason", false, "Unqualified CompileError")]
    [InlineData("missing-covered", false, "Unqualified CompileError")]
    [InlineData("missing-killed", false, "Unqualified CompileError")]
    [InlineData("null-covered", false, "Unqualified CompileError")]
    [InlineData("string-killed", false, "Unqualified CompileError")]
    [InlineData("covered", false, "Unqualified CompileError")]
    [InlineData("killed", false, "Unqualified CompileError")]
    [InlineData("missing-id", false, "Unqualified CompileError")]
    [InlineData("outside-killed", false, "out-of-scope file")]
    [InlineData("outside-survived", false, "out-of-scope file")]
    [InlineData("outside-no-coverage", false, "out-of-scope file")]
    [InlineData("outside-timeout", false, "out-of-scope file")]
    [InlineData("pending", false, "unknown or unhandled status")]
    [InlineData("runtime-error", false, "unknown or unhandled status")]
    [InlineData("unknown", false, "unknown or unhandled status")]
    [InlineData("missing-status", false, "unknown or unhandled status")]
    [InlineData("compile-only", false, "contains zero mutants")]
    [InlineData("survived", false, "untriaged Survived")]
    [InlineData("no-coverage", false, "untriaged NoCoverage")]
    [InlineData("timeout", false, "untriaged Timeout")]
    public void PinnedCompilerMetadataPreservesEvidenceAndCannotHideExecutedMutationFailures(string scenario, bool accepted, string diagnostic) {
        string fixture = Path.Combine(Path.GetTempPath(), $"fc-mutation-report-{Guid.NewGuid():N}");
        Directory.CreateDirectory(fixture);
        try {
            string repository = FindRepositoryRoot();
            Directory.CreateDirectory(Path.Combine(fixture, "eng"));
            File.Copy(Path.Combine(repository, "eng/validate-stryker-reports.ps1"), Path.Combine(fixture, "eng/validate-stryker-reports.ps1"));
            string[] roots = ["src/Hexalith.FrontComposer.SourceTools/Parsing", "src/Hexalith.FrontComposer.SourceTools/Transforms"];
            foreach (string root in roots) {
                Directory.CreateDirectory(Path.Combine(fixture, root));
                File.WriteAllText(Path.Combine(fixture, root, "Fixture.cs"), "namespace Fixture; class Example {}\n");
            }
            Directory.CreateDirectory(Path.Combine(fixture, ".config"));
            JsonObject tools = new() { ["tools"] = new JsonObject { ["dotnet-stryker"] = new JsonObject { ["version"] = scenario == "version" ? "5.0.0" : "5.1.0" } } };
            WriteJson(fixture, ".config/dotnet-tools.json", tools);
            WriteJson(fixture, "config.json", new JsonObject {
                ["stryker-config"] = new JsonObject {
                    ["project"] = "src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj",
                    ["configuration"] = "Release", ["reporters"] = new JsonArray("progress", "html", "json"),
                    ["coverage-analysis"] = "perTest", ["thresholds"] = new JsonObject { ["break"] = 80 },
                    ["mutate"] = new JsonArray("Parsing/**/*.cs", "Transforms/**/*.cs"),
                },
            });
            WriteJson(fixture, "manifest.json", new JsonObject {
                ["approvedTargetRoots"] = new JsonArray(roots.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray()),
                ["segments"] = new JsonArray(new JsonObject { ["name"] = "fixture", ["config"] = "config.json", ["threshold"] = 80, ["artifactPrefix"] = "fixture-report", ["minimumMutantCount"] = 1 }),
                ["explicitExclusions"] = new JsonArray(), ["triageActions"] = new JsonArray("blocking"), ["problemMutantTriage"] = new JsonArray(),
            });
            JsonObject outside = CompilerMutant("3");
            JsonObject inside = Mutant("1", "Killed");
            switch (scenario) {
                case "ignored": outside["status"] = "Ignored"; break;
                case "reason": outside["statusReason"] = "Another compiler failure"; break;
                case "missing-covered": outside.Remove("coveredBy"); break;
                case "missing-killed": outside.Remove("killedBy"); break;
                case "null-covered": outside["coveredBy"] = null; break;
                case "string-killed": outside["killedBy"] = ""; break;
                case "covered": outside["coveredBy"] = new JsonArray("executed-test"); break;
                case "killed": outside["killedBy"] = new JsonArray("executed-test"); break;
                case "missing-id": outside.Remove("id"); break;
                case "outside-killed": outside["status"] = "Killed"; break;
                case "outside-survived": outside["status"] = "Survived"; break;
                case "outside-no-coverage": outside["status"] = "NoCoverage"; break;
                case "outside-timeout": outside["status"] = "Timeout"; break;
                case "pending": inside["status"] = "Pending"; break;
                case "runtime-error": inside["status"] = "RuntimeError"; break;
                case "unknown": inside["status"] = "FutureStatus"; break;
                case "missing-status": inside.Remove("status"); break;
                case "compile-only": inside = CompilerMutant("1"); break;
                case "survived": inside["status"] = "Survived"; break;
                case "no-coverage": inside["status"] = "NoCoverage"; break;
                case "timeout": inside["status"] = "Timeout"; break;
            }
            JsonObject report = new() {
                ["schemaVersion"] = "1.0", ["thresholds"] = new JsonObject { ["high"] = 80, ["low"] = 80 },
                ["files"] = new JsonObject {
                    [roots[0] + "/Fixture.cs"] = SourceFile(inside, CompilerMutant("4")),
                    [roots[1] + "/Fixture.cs"] = SourceFile(scenario == "compile-only" ? CompilerMutant("2") : Mutant("2", "Killed")),
                    ["src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs"] = SourceFile(outside),
                },
            };
            WriteJson(fixture, "reports/fixture-report.json", report);
            string reportPath = Path.Combine(fixture, "reports/fixture-report.json");
            string inputHash = Hash(File.ReadAllBytes(reportPath));
            (int exit, string output) = RunValidator(fixture);
            (exit == 0).ShouldBe(accepted, $"{scenario}: {output}");
            if (!accepted) {
                string refusal = File.ReadAllText(Path.Combine(fixture, "reports/mutation-validation-errors.txt"));
                refusal.Contains(diagnostic, StringComparison.Ordinal).ShouldBeTrue(output);
            }
            JsonObject after = JsonNode.Parse(File.ReadAllText(reportPath))!.AsObject();
            foreach ((string path, JsonNode? file) in report["files"]!.AsObject()) {
                JsonNode.DeepEquals(file!["mutants"], after["files"]![path]!["mutants"]).ShouldBeTrue("Validation must never rewrite raw mutant statuses, IDs or execution metadata.");
            }
            JsonObject audit = JsonNode.Parse(File.ReadAllText(Path.Combine(fixture, "reports/mutation-report-audit.json")))!.AsObject();
            JsonNode entry = audit["Reports"]!.AsArray().Single()!;
            entry["InputSha256"]!.GetValue<string>().ShouldBe(inputHash);
            entry["OutputSha256"]!.GetValue<string>().ShouldBe(Hash(File.ReadAllBytes(reportPath)));
            entry["RawMutantStatuses"]!.AsArray().Count.ShouldBe(4);
            foreach (JsonNode? source in entry["RawSourceHashes"]!.AsArray()) {
                source!["SourceSha256"]!.GetValue<string>().ShouldBe(Hash(Encoding.UTF8.GetBytes("namespace Fixture; class Example {}\n")));
            }
            if (accepted) {
                entry["LiveMutantCount"]!.GetValue<int>().ShouldBe(2);
                entry["InvalidCompilerCandidates"]!.AsArray().Count.ShouldBe(scenario == "ignored" ? 1 : 2);
            }
        }
        finally {
            Directory.Delete(fixture, recursive: true);
        }
    }

    private static JsonObject Mutant(string id, string status) => new() { ["id"] = id, ["status"] = status, ["location"] = new JsonObject() };

    private static JsonObject CompilerMutant(string id) {
        JsonObject mutant = Mutant(id, "CompileError");
        mutant["statusReason"] = "Mutant caused compile errors";
        mutant["coveredBy"] = new JsonArray();
        mutant["killedBy"] = new JsonArray();
        return mutant;
    }

    private static JsonObject SourceFile(params JsonObject[] mutants) => new() {
        ["language"] = "cs", ["source"] = "namespace Fixture; class Example {}\n",
        ["mutants"] = new JsonArray(mutants.Select(value => (JsonNode?)value).ToArray()),
    };

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void WriteJson(string root, string path, JsonNode value) {
        string fullPath = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, value.ToJsonString());
    }

    private static string FindRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "eng/validate-stryker-reports.ps1"))) { directory = directory.Parent; }
        return directory?.FullName ?? throw new InvalidOperationException("Cannot locate the report validator.");
    }

    private static (int Exit, string Output) RunValidator(string root) {
        using Process process = new() {
            StartInfo = new ProcessStartInfo("pwsh") {
                WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true,
            },
        };
        foreach (string argument in new[] { "-NoProfile", "-NonInteractive", "-File", "eng/validate-stryker-reports.ps1", "-ManifestPath", "manifest.json", "-ReportRoot", "reports", "-OutputPath", "reports/job-summary.md" }) {
            process.StartInfo.ArgumentList.Add(argument);
        }
        process.Start().ShouldBeTrue();
        process.StandardInput.Close();
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new TimeoutException("Report validator exceeded its native fixture deadline.");
        }
        return (process.ExitCode, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
    }
}
