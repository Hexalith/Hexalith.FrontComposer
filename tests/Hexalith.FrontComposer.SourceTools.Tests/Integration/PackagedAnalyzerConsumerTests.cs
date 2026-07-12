using System.Diagnostics;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Integration;

public sealed class PackagedAnalyzerConsumerTests {
    private const string FluentV5Version = "5.0.0-rc.4-26180.1";

    [Fact]
    public async Task PackagedAnalyzer_ContractsOnlyPayload_GeneratedShellConsumerCompiles() {
        string root = FindRepoRoot();
        AssertCentralPackageVersion(root, "Microsoft.FluentUI.AspNetCore.Components", FluentV5Version);
        AssertCentralPackageVersion(root, "Microsoft.FluentUI.AspNetCore.Components.Icons", FluentV5Version);
        string packageOutput = Path.Combine(Path.GetTempPath(), "fc-source-tools-pack-" + Guid.NewGuid().ToString("N"));
        string consumer = Path.Combine(Path.GetTempPath(), "fc-source-tools-consumer-" + Guid.NewGuid().ToString("N"));
        string packageVersion = "2.0.0-review.g" + Guid.NewGuid().ToString("N")[..8];
        string fallbackPackages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        _ = Directory.CreateDirectory(packageOutput);
        _ = Directory.CreateDirectory(consumer);

        await RunDotnetAsync(root, TestContext.Current.CancellationToken, "pack", "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj", "-c", "Release", "-o", packageOutput, "--no-build", "-m:1", "/nr:false", $"-p:Version={packageVersion}").ConfigureAwait(true);
        await RunDotnetAsync(root, TestContext.Current.CancellationToken, "pack", "src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj", "-c", "Release", "-o", packageOutput, "--no-build", "-m:1", "/nr:false", $"-p:Version={packageVersion}").ConfigureAwait(true);
        await RunDotnetAsync(root, TestContext.Current.CancellationToken, "pack", "src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj", "-c", "Release", "-o", packageOutput, "--no-build", "-m:1", "/nr:false", $"-p:Version={packageVersion}").ConfigureAwait(true);
        await RunDotnetAsync(root, TestContext.Current.CancellationToken, "pack", "src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj", "-c", "Release", "-o", packageOutput, "--no-build", "-m:1", "/nr:false", $"-p:Version={packageVersion}").ConfigureAwait(true);

        AssertPackageDependencyVersion(packageOutput, "Hexalith.FrontComposer.Contracts.UI", "Microsoft.FluentUI.AspNetCore.Components", FluentV5Version);
        AssertPackageDependencyVersion(packageOutput, "Hexalith.FrontComposer.Shell", "Microsoft.FluentUI.AspNetCore.Components", FluentV5Version);
        AssertPackageDependencyVersion(packageOutput, "Hexalith.FrontComposer.Shell", "Microsoft.FluentUI.AspNetCore.Components.Icons", FluentV5Version);

        string sourceToolsPackage = Directory.GetFiles(packageOutput, $"Hexalith.FrontComposer.SourceTools.{packageVersion}.nupkg").Single();
        using (ZipArchive archive = ZipFile.OpenRead(sourceToolsPackage)) {
            string[] analyzerEntries = archive.Entries
                .Where(entry => entry.FullName.StartsWith("analyzers/dotnet/cs/", StringComparison.Ordinal))
                .Select(entry => entry.FullName)
                .Order(StringComparer.Ordinal)
                .ToArray();
            analyzerEntries.ShouldContain("analyzers/dotnet/cs/Hexalith.FrontComposer.SourceTools.dll");
            analyzerEntries.ShouldContain("analyzers/dotnet/cs/Hexalith.FrontComposer.Contracts.dll");
            analyzerEntries.ShouldNotContain(entry => entry.Contains("Hexalith.FrontComposer.Shell", StringComparison.Ordinal));
            analyzerEntries.ShouldNotContain(entry => entry.Contains("Hexalith.FrontComposer.Contracts.UI", StringComparison.Ordinal));

            ZipArchiveEntry analyzer = archive.GetEntry("analyzers/dotnet/cs/Hexalith.FrontComposer.SourceTools.dll")!;
            using Stream analyzerStream = analyzer.Open();
            using MemoryStream analyzerBytes = new();
            await analyzerStream.CopyToAsync(analyzerBytes, TestContext.Current.CancellationToken).ConfigureAwait(true);
            analyzerBytes.Position = 0;
            using PEReader peReader = new(analyzerBytes);
            MetadataReader reader = peReader.GetMetadataReader();
            string[] references = reader.AssemblyReferences
                .Select(reader.GetAssemblyReference)
                .Select(reference => reader.GetString(reference.Name))
                .ToArray();
            references.ShouldContain("Hexalith.FrontComposer.Contracts");
            references.ShouldNotContain("Hexalith.FrontComposer.Contracts.UI");
            references.ShouldNotContain("Hexalith.FrontComposer.Shell");
            references.ShouldNotContain("Hexalith.FrontComposer.Testing");
        }

        await File.WriteAllTextAsync(Path.Combine(consumer, "Consumer.csproj"), $$"""
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>$(NoWarn);ASP0006</NoWarn>
    <NuGetAudit>false</NuGetAudit>
    <RestorePackagesPath>$(MSBuildProjectDirectory)/packages</RestorePackagesPath>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Hexalith.FrontComposer.Contracts" Version="{{packageVersion}}" />
    <PackageReference Include="Hexalith.FrontComposer.Contracts.UI" Version="{{packageVersion}}" />
    <PackageReference Include="Hexalith.FrontComposer.Shell" Version="{{packageVersion}}" />
    <PackageReference Include="Hexalith.FrontComposer.SourceTools" Version="{{packageVersion}}" PrivateAssets="all" />
    <PackageReference Include="Microsoft.FluentUI.AspNetCore.Components" Version="{{FluentV5Version}}" />
    <PackageReference Include="Microsoft.FluentUI.AspNetCore.Components.Icons" Version="{{FluentV5Version}}" />
  </ItemGroup>
</Project>
""", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(Path.Combine(consumer, "nuget.config"), $$"""
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="{{packageOutput}}" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <fallbackPackageFolders>
    <add key="global" value="{{fallbackPackages}}" />
  </fallbackPackageFolders>
</configuration>
""", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(Path.Combine(consumer, "GeneratedSurface.cs"), """
using Hexalith.FrontComposer.Contracts.Attributes;

namespace PackageConsumer;

[Projection]
public sealed partial record OrdersProjection(
    string Id,
    string Customer,
    string Status,
    decimal Total,
    DateTimeOffset UpdatedAt);

[Command]
public sealed partial class CreateOrderCommand
{
    public string MessageId { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public string Product { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public string Notes { get; set; } = string.Empty;
}
""", TestContext.Current.CancellationToken).ConfigureAwait(true);

        await RunDotnetAsync(consumer, TestContext.Current.CancellationToken, "build", "-c", "Release", "-m:1", "/nr:false").ConfigureAwait(true);

        string assets = await File.ReadAllTextAsync(Path.Combine(consumer, "obj", "project.assets.json"), TestContext.Current.CancellationToken).ConfigureAwait(true);
        assets.ShouldContain("\"Microsoft.FluentUI.AspNetCore.Components/" + FluentV5Version + "\"");
        assets.ShouldContain("\"Microsoft.FluentUI.AspNetCore.Components.Icons/" + FluentV5Version + "\"");
        assets.ShouldNotContain("\"Microsoft.FluentUI.AspNetCore.Components/4.");
        assets.ShouldNotContain("\"Microsoft.FluentUI.AspNetCore.Components.Icons/4.");

        string[] generatedFiles = Directory.GetFiles(
            Path.Combine(consumer, "obj", "generated"),
            "*.cs",
            SearchOption.AllDirectories);
        generatedFiles.ShouldNotBeEmpty("the packaged analyzer must run and emit source in the clean consumer");
        string projectionSourcePath = generatedFiles.Single(path => path.EndsWith("OrdersProjection.g.razor.cs", StringComparison.Ordinal));
        string projectionSource = await File.ReadAllTextAsync(projectionSourcePath, TestContext.Current.CancellationToken).ConfigureAwait(true);
        projectionSource.ShouldContain("global::Hexalith.FrontComposer.Shell.Options.FcShellOptions");
        projectionSource.ShouldContain("global::Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadPageAction");
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

    private static void AssertCentralPackageVersion(string root, string packageId, string expectedVersion) {
        string path = Path.Combine(root, "references", "Hexalith.Builds", "Props", "Directory.Packages.props");
        XDocument document = XDocument.Load(path);
        string? actualVersion = document
            .Descendants("PackageVersion")
            .Single(element => string.Equals((string?)element.Attribute("Include"), packageId, StringComparison.Ordinal))
            .Attribute("Version")
            ?.Value;

        actualVersion.ShouldBe(expectedVersion);
    }

    private static void AssertPackageDependencyVersion(string packageOutput, string packageId, string dependencyId, string expectedVersion) {
        string package = Directory.GetFiles(packageOutput, $"{packageId}.*.nupkg").Single();
        using ZipArchive archive = ZipFile.OpenRead(package);
        ZipArchiveEntry nuspec = archive.Entries.Single(entry => entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using Stream stream = nuspec.Open();
        XDocument document = XDocument.Load(stream);
        string actualVersion = document
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "dependency", StringComparison.Ordinal))
            .Where(element => string.Equals((string?)element.Attribute("id"), dependencyId, StringComparison.Ordinal))
            .Select(element => (string?)element.Attribute("version"))
            .Distinct(StringComparer.Ordinal)
            .Single()!;

        actualVersion.ShouldBe(expectedVersion);
    }

    private static async Task RunDotnetAsync(string workingDirectory, CancellationToken cancellationToken, params string[] args) {
        ProcessStartInfo startInfo = new("dotnet") {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        foreach (string arg in args) {
            startInfo.ArgumentList.Add(arg);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet.");
        string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(true);
        string stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(true);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(true);

        if (process.ExitCode != 0) {
            throw new InvalidOperationException(
                $"dotnet {string.Join(' ', args)} failed with exit code {process.ExitCode}.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
        }
    }
}
