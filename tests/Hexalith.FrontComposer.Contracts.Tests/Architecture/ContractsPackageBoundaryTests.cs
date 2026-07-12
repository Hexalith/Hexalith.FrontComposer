using System.Diagnostics;
using System.IO.Compression;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Architecture;

public sealed class ContractsPackageBoundaryTests {
    [Fact]
    public async Task PackedKernel_CleanMultiTargetConsumer_HasNoUiOrFluentDependency() {
        string root = FindRepoRoot();
        string packageOutput = Path.Combine(Path.GetTempPath(), "fc-contracts-kernel-pack-" + Guid.NewGuid().ToString("N"));
        string consumer = Path.Combine(Path.GetTempPath(), "fc-contracts-kernel-consumer-" + Guid.NewGuid().ToString("N"));
        string version = "0.2.0-review.g" + Guid.NewGuid().ToString("N")[..8];
        _ = Directory.CreateDirectory(packageOutput);
        _ = Directory.CreateDirectory(consumer);

        await RunDotnetAsync(root, TestContext.Current.CancellationToken, "pack", "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj", "-c", "Release", "-o", packageOutput, "--no-build", "-m:1", "/nr:false", $"-p:Version={version}").ConfigureAwait(true);

        string package = Directory.GetFiles(packageOutput, "Hexalith.FrontComposer.Contracts.*.nupkg")
            .Single(path => !Path.GetFileName(path).StartsWith("Hexalith.FrontComposer.Contracts.UI.", StringComparison.Ordinal));
        using (ZipArchive archive = ZipFile.OpenRead(package)) {
            string nuspec = ReadNuspec(archive);
            nuspec.ShouldNotContain("Hexalith.FrontComposer.Contracts.UI");
            nuspec.ShouldNotContain("Microsoft.FluentUI");
            nuspec.ShouldNotContain("Microsoft.AspNetCore.Components");
        }

        await File.WriteAllTextAsync(Path.Combine(consumer, "Consumer.csproj"), $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks><LangVersion>latest</LangVersion><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><TreatWarningsAsErrors>true</TreatWarningsAsErrors><NuGetAudit>false</NuGetAudit></PropertyGroup>
  <ItemGroup><PackageReference Include="Hexalith.FrontComposer.Contracts" Version="{{version}}" /></ItemGroup>
</Project>
""", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(Path.Combine(consumer, "nuget.config"), $$"""
<?xml version="1.0" encoding="utf-8"?><configuration><packageSources><clear /><add key="local" value="{{packageOutput}}" /><add key="nuget" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>
""", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(Path.Combine(consumer, "Consumer.cs"), """
using Hexalith.FrontComposer.Contracts.Attributes;
public sealed class Consumer { public ProjectionAttribute Attribute { get; } = new(); }
""", TestContext.Current.CancellationToken).ConfigureAwait(true);

        await RunDotnetAsync(consumer, TestContext.Current.CancellationToken, "build", "-m:1", "/nr:false").ConfigureAwait(true);
        string assets = await File.ReadAllTextAsync(Path.Combine(consumer, "obj", "project.assets.json"), TestContext.Current.CancellationToken).ConfigureAwait(true);
        assets.ShouldNotContain("Hexalith.FrontComposer.Contracts.UI");
        assets.ShouldNotContain("Microsoft.FluentUI");
        assets.ShouldNotContain("Microsoft.AspNetCore.Components");
    }

    private static string ReadNuspec(ZipArchive archive) {
        ZipArchiveEntry entry = archive.Entries.Single(item => item.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using StreamReader reader = new(entry.Open());
        return reader.ReadToEnd();
    }

    private static async Task RunDotnetAsync(string workingDirectory, CancellationToken cancellationToken, params string[] args) {
        ProcessStartInfo startInfo = new("dotnet") { WorkingDirectory = workingDirectory, RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (string arg in args) {
            startInfo.ArgumentList.Add(arg);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
        string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        string stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0) {
            throw new InvalidOperationException($"dotnet {string.Join(' ', args)} failed with exit code {process.ExitCode}.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
        }
    }

    private static string FindRepoRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
