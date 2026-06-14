using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Testing.Tests;

public sealed class PackageBoundaryTests {
    [Fact]
    public void PublicApi_ExportedTypes_MatchIntentionalBaseline() {
        string root = FindRepoRoot();
        string baselinePath = Path.Combine(root, "src", "Hexalith.FrontComposer.Testing", "PublicAPI.Shipped.txt");
        string[] expected = File.ReadAllLines(baselinePath)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] actual = EnumeratePublicApi(typeof(FrontComposerTestBase).Assembly)
            .Order(StringComparer.Ordinal)
            .ToArray();

        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task DotnetPack_TestingPackage_DoesNotLeakRepoLocalArtifactsOrInternalTestAssemblies() {
        string root = FindRepoRoot();
        string output = Path.Combine(Path.GetTempPath(), "fc-testing-pack-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(output);

        await RunDotnetAsync(root, TestContext.Current.CancellationToken, "pack", "src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj", "-c", "Release", "-o", output, "--no-restore", "-m:1", "/nr:false").ConfigureAwait(true);

        string package = Directory.GetFiles(output, "Hexalith.FrontComposer.Testing.*.nupkg").Single();
        using ZipArchive archive = ZipFile.OpenRead(package);
        string[] entries = archive.Entries.Select(e => e.FullName).ToArray();

        entries.ShouldContain(e => e.EndsWith("lib/net10.0/Hexalith.FrontComposer.Testing.dll", StringComparison.Ordinal));
        entries.ShouldContain(e => string.Equals(e, "README.md", StringComparison.Ordinal));
        entries.ShouldContain(e => e.EndsWith("build/Hexalith.FrontComposer.Testing.PublicAPI.Shipped.txt", StringComparison.Ordinal));
        entries.ShouldNotContain(e => e.Contains("tests/", StringComparison.OrdinalIgnoreCase));
        entries.ShouldNotContain(e => e.Contains("bin/", StringComparison.OrdinalIgnoreCase));
        entries.ShouldNotContain(e => e.Contains("obj/", StringComparison.OrdinalIgnoreCase));
        entries.ShouldNotContain(e => e.Contains("screenshots", StringComparison.OrdinalIgnoreCase));
        entries.ShouldNotContain(e => e.Contains(".git", StringComparison.OrdinalIgnoreCase));

        string nuspec = ReadNuspec(archive);
        nuspec.ShouldContain("Hexalith.FrontComposer.Contracts");
        nuspec.ShouldContain("Hexalith.FrontComposer.Shell");
        nuspec.ShouldContain("bunit");
        nuspec.ShouldNotContain("Hexalith.FrontComposer.Shell.Tests");
        nuspec.ShouldNotContain("NSubstitute");
        nuspec.ShouldNotContain("Shouldly");
        nuspec.ShouldNotContain("xunit.v3");
    }

    [Fact]
    public async Task CleanTemporaryConsumer_RestoresFromPackedNupkgs_WithoutRepoRelativeProjectReferences() {
        string root = FindRepoRoot();
        string packageOutput = Path.Combine(Path.GetTempPath(), "fc-testing-clean-pack-" + Guid.NewGuid().ToString("N"));
        string consumer = Path.Combine(Path.GetTempPath(), "fc-testing-consumer-" + Guid.NewGuid().ToString("N"));
        string packageVersion = "0.2.0-review." + Guid.NewGuid().ToString("N")[..8];
        string fallbackPackages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        _ = Directory.CreateDirectory(packageOutput);
        _ = Directory.CreateDirectory(consumer);

        await RunDotnetAsync(root, TestContext.Current.CancellationToken, "pack", "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj", "-c", "Release", "-o", packageOutput, "--no-restore", "-m:1", "/nr:false", $"-p:Version={packageVersion}").ConfigureAwait(true);
        await RunDotnetAsync(root, TestContext.Current.CancellationToken, "pack", "src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj", "-c", "Release", "-o", packageOutput, "--no-restore", "-m:1", "/nr:false", $"-p:Version={packageVersion}").ConfigureAwait(true);
        await RunDotnetAsync(root, TestContext.Current.CancellationToken, "pack", "src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj", "-c", "Release", "-o", packageOutput, "--no-restore", "-m:1", "/nr:false", $"-p:Version={packageVersion}").ConfigureAwait(true);

        await File.WriteAllTextAsync(Path.Combine(consumer, "Consumer.csproj"), $$"""
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NuGetAudit>false</NuGetAudit>
    <RestorePackagesPath>$(MSBuildProjectDirectory)/packages</RestorePackagesPath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Hexalith.FrontComposer.Testing" Version="{{packageVersion}}" />
    <PackageReference Include="xunit.v3" Version="3.2.2" />
    <PackageReference Include="xunit.v3.assert" Version="3.2.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
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
        await File.WriteAllTextAsync(Path.Combine(consumer, "ConsumerSmokeTests.cs"), """
using Bunit;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ConsumerSmokeTests
{
    [Fact]
    public async Task TestHost_DispatchesWithoutInternalTestAssemblies()
    {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        ICommandService service = context.Services.GetRequiredService<ICommandService>();
        await service.DispatchAsync(new SmokeCommand { Name = "demo" }, Xunit.TestContext.Current.CancellationToken);
        Assert.Single(host.CommandService.Evidence);
    }

    private sealed class SmokeCommand
    {
        public string? Name { get; init; }
    }
}
""", TestContext.Current.CancellationToken).ConfigureAwait(true);

        await RunDotnetAsync(consumer, TestContext.Current.CancellationToken, "build", "-m:1", "/nr:false").ConfigureAwait(true);
    }

    private static string ReadNuspec(ZipArchive archive) {
        ZipArchiveEntry entry = archive.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using Stream stream = entry.Open();
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private static string FormatTypeName(Type type) {
        if (type.IsGenericParameter) {
            return type.Name;
        }

        if (type.IsArray) {
            return FormatTypeName(type.GetElementType()!) + "[]";
        }

        if (type.IsByRef) {
            return FormatTypeName(type.GetElementType()!);
        }

        if (type == typeof(void)) {
            return "void";
        }

        if (!type.IsGenericType) {
            return type.FullName!;
        }

        string name = type.FullName ?? type.Name;
        int tick = name.IndexOf('`', StringComparison.Ordinal);
        string genericName = tick < 0 ? name : name[..tick];
        string args = string.Join(",", type.GetGenericArguments().Select(FormatTypeName));
        return $"{genericName}<{args}>";
    }

    private static IEnumerable<string> EnumeratePublicApi(Assembly assembly) {
        foreach (Type type in assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "Hexalith.FrontComposer.Testing")
            .OrderBy(FormatTypeName, StringComparer.Ordinal)) {
            string typeName = FormatTypeName(type);
            yield return typeName;

            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
                yield return $"{typeName}.#ctor({FormatParameters(constructor)})";
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
                MethodInfo? getter = property.GetMethod;
                MethodInfo? setter = property.SetMethod;
                string access = $"{(getter is null ? "-" : "get")}/{(setter is null ? "-" : "set")}";
                yield return $"{typeName}.{property.Name}:{FormatTypeName(property.PropertyType)}:{access}";
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
                yield return $"{typeName}.{field.Name}:{FormatTypeName(field.FieldType)}";
            }

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)) {
                string genericArgs = method.IsGenericMethodDefinition
                    ? $"<{string.Join(",", method.GetGenericArguments().Select(arg => arg.Name))}>"
                    : string.Empty;
                yield return $"{typeName}.{method.Name}{genericArgs}({FormatParameters(method)}):{FormatTypeName(method.ReturnType)}";
            }
        }
    }

    private static string FormatParameters(MethodBase method)
        => string.Join(",", method.GetParameters().Select(parameter => FormatTypeName(parameter.ParameterType)));

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
        string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        string stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0) {
            throw new InvalidOperationException(
                $"dotnet {string.Join(' ', args)} failed with exit code {process.ExitCode}.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
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
