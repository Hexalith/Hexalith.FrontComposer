using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;

using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.UI.Tests;

public sealed class PackageBoundaryTests {
    private const string FluentV5Version = "5.0.0-rc.4-26180.1";

    [Fact]
    public void PublicApi_ExportedMembers_MatchIntentionalBaseline() {
        string root = FindRepoRoot();
        string baselinePath = Path.Combine(root, "src", "Hexalith.FrontComposer.Contracts.UI", "PublicAPI.Shipped.txt");
        string[] expected = File.ReadAllLines(baselinePath)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] actual = EnumeratePublicApi(typeof(Typography).Assembly)
            .Order(StringComparer.Ordinal)
            .ToArray();

        actual.ShouldBe(expected);
    }

    [Fact]
    public void AssemblyOwnership_MovedSurface_IsUiOwnedAndPointsOnlyDownward() {
        Assembly assembly = typeof(Typography).Assembly;
        string[] exported = assembly.GetExportedTypes().Select(type => type.FullName!).ToArray();
        string[] references = assembly.GetReferencedAssemblies().Select(reference => reference.Name!).ToArray();

        exported.ShouldContain("Hexalith.FrontComposer.Contracts.Rendering.Typography");
        exported.ShouldContain("Hexalith.FrontComposer.Contracts.Rendering.FcTypoToken");
        exported.ShouldContain("Hexalith.FrontComposer.Contracts.Rendering.FieldSlotContext`2");
        exported.ShouldContain("Hexalith.FrontComposer.Contracts.Rendering.ProjectionTemplateContext`1");
        exported.ShouldContain("Hexalith.FrontComposer.Contracts.Rendering.ProjectionViewContext`1");
        exported.ShouldContain("Hexalith.FrontComposer.Contracts.Shortcuts.IShortcutService");
        exported.ShouldContain("Hexalith.FrontComposer.Contracts.Shortcuts.ShortcutBinding");
        references.ShouldContain("Hexalith.FrontComposer.Contracts");
        references.ShouldContain("Microsoft.AspNetCore.Components");
        references.ShouldContain("Microsoft.AspNetCore.Components.Web");
        references.ShouldContain("Microsoft.FluentUI.AspNetCore.Components");
        references.ShouldNotContain("Hexalith.FrontComposer.Shell");
    }

    [Fact]
    public void PackageValidation_Published20UsesItsReleasedBaseline() {
        string root = FindRepoRoot();
        string project = File.ReadAllText(Path.Combine(root, "src", "Hexalith.FrontComposer.Contracts.UI", "Hexalith.FrontComposer.Contracts.UI.csproj"));
        string targets = File.ReadAllText(Path.Combine(root, "Directory.Build.targets"));

        project.ShouldContain("<FrontComposerPackageValidationBaselineVersion>2.0.0</FrontComposerPackageValidationBaselineVersion>");
        project.ShouldNotContain("<FrontComposerPackageValidationSkipBaseline>true</FrontComposerPackageValidationSkipBaseline>");
        project.ShouldNotContain("<EnablePackageValidation>false</EnablePackageValidation>");
        targets.ShouldContain("<FrontComposerPackageValidationBaselineVersion Condition=\"'$(FrontComposerPackageValidationBaselineVersion)' == ''\">1.12.0</FrontComposerPackageValidationBaselineVersion>");
        targets.ShouldContain("Condition=\"'$(FrontComposerPackageValidationSkipBaseline)' != 'true'\"");
        targets.ShouldNotContain(">0.1.0</FrontComposerPackageValidationBaselineVersion>");
    }

    [Fact]
    public async Task PackedUiPackage_CleanConsumer_UsesExistingNamespacesAndExpectedDependencies() {
        string root = FindRepoRoot();
        string packageOutput = Path.Combine(Path.GetTempPath(), "fc-contracts-ui-pack-" + Guid.NewGuid().ToString("N"));
        string consumer = Path.Combine(Path.GetTempPath(), "fc-contracts-ui-consumer-" + Guid.NewGuid().ToString("N"));
        string version = "2.0.0-review.g" + Guid.NewGuid().ToString("N")[..8];
        _ = Directory.CreateDirectory(packageOutput);
        _ = Directory.CreateDirectory(consumer);

        await RunDotnetAsync(root, TestContext.Current.CancellationToken, "pack", "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj", "-c", "Release", "-o", packageOutput, "--no-build", "-m:1", "/nr:false", $"-p:Version={version}").ConfigureAwait(true);
        await RunDotnetAsync(root, TestContext.Current.CancellationToken, "pack", "src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj", "-c", "Release", "-o", packageOutput, "--no-build", "-m:1", "/nr:false", $"-p:Version={version}").ConfigureAwait(true);

        string package = Directory.GetFiles(packageOutput, "Hexalith.FrontComposer.Contracts.UI.*.nupkg").Single();
        using (ZipArchive archive = ZipFile.OpenRead(package)) {
            XDocument nuspec = ReadNuspec(archive);
            XElement dependencyGroup = nuspec
                .Descendants()
                .Single(element => string.Equals(element.Name.LocalName, "group", StringComparison.Ordinal)
                    && element.Parent?.Name.LocalName == "dependencies");
            dependencyGroup.Attribute("targetFramework")?.Value.ShouldBe("net10.0");
            string[] dependencies = dependencyGroup
                .Elements()
                .Where(element => string.Equals(element.Name.LocalName, "dependency", StringComparison.Ordinal))
                .Select(element => $"{element.Attribute("id")?.Value}|{element.Attribute("version")?.Value}")
                .Order(StringComparer.Ordinal)
                .ToArray();
            dependencies.ShouldBe([
                $"Hexalith.FrontComposer.Contracts|{version}",
                $"Microsoft.FluentUI.AspNetCore.Components|{FluentV5Version}",
            ], ignoreOrder: false);
            archive.Entries.Select(entry => entry.FullName)
                .ShouldContain(entry => entry.EndsWith("build/Hexalith.FrontComposer.Contracts.UI.PublicAPI.Shipped.txt", StringComparison.Ordinal));
        }

        await File.WriteAllTextAsync(Path.Combine(consumer, "Consumer.csproj"), $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><TreatWarningsAsErrors>true</TreatWarningsAsErrors><NuGetAudit>false</NuGetAudit></PropertyGroup>
  <ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /><PackageReference Include="Hexalith.FrontComposer.Contracts.UI" Version="{{version}}" /></ItemGroup>
</Project>
""", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(Path.Combine(consumer, "nuget.config"), $$"""
<?xml version="1.0" encoding="utf-8"?><configuration><packageSources><clear /><add key="local" value="{{packageOutput}}" /><add key="nuget" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>
""", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(Path.Combine(consumer, "Consumer.cs"), """
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Microsoft.AspNetCore.Components.Web;
public static class Consumer { public static FcTypoToken Token => Typography.Body; public static bool Map(KeyboardEventArgs e) => ShortcutBinding.TryFromKeyboardEvent(e, out _); }
""", TestContext.Current.CancellationToken).ConfigureAwait(true);

        await RunDotnetAsync(consumer, TestContext.Current.CancellationToken, "build", "-m:1", "/nr:false").ConfigureAwait(true);
    }

    private static XDocument ReadNuspec(ZipArchive archive) {
        ZipArchiveEntry entry = archive.Entries.Single(item => item.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using Stream stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static IEnumerable<string> EnumeratePublicApi(Assembly assembly) {
        foreach (Type type in assembly.GetExportedTypes().OrderBy(FormatTypeName, StringComparer.Ordinal)) {
            string typeName = FormatTypeName(type);
            yield return typeName;
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
                yield return $"{typeName}.#ctor({FormatParameters(constructor)})";
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
                yield return $"{typeName}.{property.Name}:{FormatTypeName(property.PropertyType)}:{(property.GetMethod is null ? "-" : "get")}/{(property.SetMethod is null ? "-" : "set")}";
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
                yield return $"{typeName}.{field.Name}:{FormatTypeName(field.FieldType)}";
            }

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(method => !method.IsSpecialName)) {
                string genericArgs = method.IsGenericMethodDefinition ? $"<{string.Join(",", method.GetGenericArguments().Select(argument => argument.Name))}>" : string.Empty;
                yield return $"{typeName}.{method.Name}{genericArgs}({FormatParameters(method)}):{FormatTypeName(method.ReturnType)}";
            }
        }
    }

    private static string FormatParameters(MethodBase method) => string.Join(",", method.GetParameters().Select(parameter => FormatTypeName(parameter.ParameterType)));

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
        return $"{genericName}<{string.Join(",", type.GetGenericArguments().Select(FormatTypeName))}>";
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
