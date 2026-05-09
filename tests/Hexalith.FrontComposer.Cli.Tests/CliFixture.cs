namespace Hexalith.FrontComposer.Cli.Tests;

internal sealed class CliFixture : IDisposable
{
    private CliFixture(string root) => Root = root;

    public string Root { get; }

    public static CliFixture Create()
    {
        string root = Path.Combine(Path.GetTempPath(), "hfc-cli-tests", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(root);
        return new CliFixture(root);
    }

    public string WriteProject(string name, string targetFrameworks)
    {
        string projectDirectory = Path.Combine(Root, name);
        _ = Directory.CreateDirectory(projectDirectory);
        string projectPath = Path.Combine(projectDirectory, name + ".csproj");
        string frameworkProperty = targetFrameworks.Contains(';', StringComparison.Ordinal)
            ? $"<TargetFrameworks>{targetFrameworks}</TargetFrameworks>"
            : $"<TargetFramework>{targetFrameworks}</TargetFramework>";
        File.WriteAllText(
            projectPath,
            $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                {{frameworkProperty}}
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="**\*.cs" Exclude="bin\**;obj\**" />
              </ItemGroup>
            </Project>
            """);
        return projectPath;
    }

    public string WriteSource(string projectName, string relativePath, string content)
    {
        string path = Path.Combine(Root, projectName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public string WriteGenerated(string projectName, string configuration, string framework, string fileName, string content)
    {
        string path = Path.Combine(
            Root,
            projectName,
            "obj",
            configuration,
            framework,
            "generated",
            "HexalithFrontComposer",
            fileName);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public string WriteGeneratedDiagnosticSidecar(string projectName, string configuration, string framework, string fileName, string content)
        => WriteGenerated(projectName, configuration, framework, fileName, content);

    public void Dispose()
    {
        try {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException) {
        }
        catch (UnauthorizedAccessException) {
        }
    }
}
