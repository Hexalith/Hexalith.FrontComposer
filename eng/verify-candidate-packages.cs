using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

return CandidatePackageVerifier.Run(args);

internal static partial class CandidatePackageVerifier
{
    private const int ExpectedPackageCount = 8;

    [GeneratedRegex(
        "^(?<major>0|[1-9][0-9]*)\\.(?<minor>0|[1-9][0-9]*)\\.(?<patch>0|[1-9][0-9]*)" +
        "(?:-(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(?:\\.(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*)?" +
        "(?:\\+[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?$")]
    private static partial Regex CandidateVersionRegex();

    public static int Run(string[] arguments)
    {
        try
        {
            if (arguments.Length != 3)
            {
                throw new InvalidOperationException(
                    "usage: verify_candidate_packages.cs <package-directory> <inventory> <candidate-version>");
            }

            string packageDirectory = Path.GetFullPath(arguments[0]);
            string inventoryPath = Path.GetFullPath(arguments[1]);
            string candidateVersion = arguments[2];
            Match versionMatch = CandidateVersionRegex().Match(candidateVersion);
            if (!versionMatch.Success)
            {
                throw new InvalidOperationException(
                    $"candidate version '{candidateVersion}' must be strict SemVer major.minor.patch");
            }

            var binaryVersion = new Version(
                int.Parse(versionMatch.Groups["major"].Value, System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(versionMatch.Groups["minor"].Value, System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(versionMatch.Groups["patch"].Value, System.Globalization.CultureInfo.InvariantCulture),
                0);
            string[] packageIds = ReadPackageIds(inventoryPath);
            string[] packageFiles = Directory
                .EnumerateFiles(packageDirectory, "*.nupkg", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (packageFiles.Length != ExpectedPackageCount)
            {
                throw new InvalidOperationException(
                    $"expected exactly {ExpectedPackageCount} root-level candidate packages; found {packageFiles.Length}");
            }

            int assemblyCount = 0;
            foreach (string packageId in packageIds)
            {
                string packagePath = Path.Combine(packageDirectory, $"{packageId}.{candidateVersion}.nupkg");
                if (!File.Exists(packagePath))
                {
                    throw new InvalidOperationException($"{packageId}: expected candidate package is missing");
                }

                assemblyCount += VerifyPackage(packagePath, packageId, candidateVersion, binaryVersion);
            }

            Console.WriteLine(
                $"Verified {packageIds.Length} candidate packages and {assemblyCount} primary assembly copies at {candidateVersion}.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Candidate package version verification failed: {error.Message}");
            return 1;
        }
    }

    private static string[] ReadPackageIds(string inventoryPath)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(inventoryPath));
        if (!document.RootElement.TryGetProperty("packages", out JsonElement packages)
            || packages.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("release package inventory packages must be an array");
        }

        string[] ids = packages
            .EnumerateArray()
            .Where(row => row.ValueKind == JsonValueKind.Object
                && row.TryGetProperty("packable", out JsonElement packable)
                && packable.ValueKind == JsonValueKind.True)
            .Select(row => row.TryGetProperty("package_id", out JsonElement packageId)
                && packageId.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(packageId.GetString())
                    ? packageId.GetString()!
                    : throw new InvalidOperationException("packable inventory row has no package_id"))
            .ToArray();
        if (ids.Length != ExpectedPackageCount
            || ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ExpectedPackageCount)
        {
            throw new InvalidOperationException(
                $"release inventory must contain exactly {ExpectedPackageCount} unique packable package IDs");
        }

        return ids;
    }

    private static int VerifyPackage(
        string packagePath,
        string packageId,
        string candidateVersion,
        Version binaryVersion)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        ZipArchiveEntry[] nuspecs = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (nuspecs.Length != 1)
        {
            throw new InvalidOperationException($"{packageId}: expected exactly one nuspec");
        }

        using (Stream stream = nuspecs[0].Open())
        {
            XDocument nuspec = XDocument.Load(stream);
            string nuspecId = nuspec.Descendants().Single(element => element.Name.LocalName == "id").Value;
            string nuspecVersion = nuspec.Descendants().Single(element => element.Name.LocalName == "version").Value;
            if (!string.Equals(nuspecId, packageId, StringComparison.Ordinal)
                || !string.Equals(nuspecVersion, candidateVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{packageId}: nuspec id/version '{nuspecId}/{nuspecVersion}' does not match candidate");
            }
        }

        string expectedAssemblyName = $"{packageId}.dll";
        ZipArchiveEntry[] assemblies = archive.Entries
            .Where(entry => string.Equals(entry.Name, expectedAssemblyName, StringComparison.Ordinal))
            .ToArray();
        if (assemblies.Length == 0)
        {
            throw new InvalidOperationException($"{packageId}: primary FrontComposer assembly is missing");
        }
        if (assemblies.Select(entry => entry.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != assemblies.Length)
        {
            throw new InvalidOperationException($"{packageId}: primary assembly path is duplicated");
        }

        string scratch = Path.Combine(Path.GetTempPath(), $"frontcomposer-version-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(scratch);
        try
        {
            for (int index = 0; index < assemblies.Length; index++)
            {
                string extracted = Path.Combine(scratch, $"{index}-{expectedAssemblyName}");
                assemblies[index].ExtractToFile(extracted);
                VerifyAssembly(extracted, packageId, assemblies[index].FullName, candidateVersion, binaryVersion);
            }
        }
        finally
        {
            Directory.Delete(scratch, recursive: true);
        }

        return assemblies.Length;
    }

    private static void VerifyAssembly(
        string assemblyPath,
        string packageId,
        string packageEntry,
        string candidateVersion,
        Version binaryVersion)
    {
        Version? actualAssemblyVersion = AssemblyName.GetAssemblyName(assemblyPath).Version;
        if (actualAssemblyVersion != binaryVersion)
        {
            throw new InvalidOperationException(
                $"{packageId}:{packageEntry}: assembly version {actualAssemblyVersion} must be {binaryVersion}");
        }

        string? fileVersion = FileVersionInfo.GetVersionInfo(assemblyPath).FileVersion;
        if (!string.Equals(fileVersion, binaryVersion.ToString(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{packageId}:{packageEntry}: file version {fileVersion ?? "<missing>"} must be {binaryVersion}");
        }

        string? informationalVersion = ReadInformationalVersion(assemblyPath);
        bool aligned = string.Equals(informationalVersion, candidateVersion, StringComparison.Ordinal)
            || (!candidateVersion.Contains('+')
                && informationalVersion?.StartsWith(candidateVersion + "+", StringComparison.Ordinal) is true)
            || (candidateVersion.Contains('+')
                && informationalVersion?.StartsWith(candidateVersion + ".", StringComparison.Ordinal) is true);
        if (!aligned)
        {
            throw new InvalidOperationException(
                $"{packageId}:{packageEntry}: informational version " +
                $"{informationalVersion ?? "<missing>"} does not align with {candidateVersion}");
        }
    }

    private static string? ReadInformationalVersion(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        MetadataReader metadata = peReader.GetMetadataReader();
        AssemblyDefinition assembly = metadata.GetAssemblyDefinition();
        foreach (CustomAttributeHandle handle in assembly.GetCustomAttributes())
        {
            CustomAttribute attribute = metadata.GetCustomAttribute(handle);
            if (!string.Equals(
                GetAttributeTypeName(metadata, attribute.Constructor),
                "System.Reflection.AssemblyInformationalVersionAttribute",
                StringComparison.Ordinal))
            {
                continue;
            }

            BlobReader value = metadata.GetBlobReader(attribute.Value);
            if (value.ReadUInt16() != 1)
            {
                throw new InvalidOperationException("invalid informational-version attribute prolog");
            }

            return value.ReadSerializedString();
        }

        return null;
    }

    private static string GetAttributeTypeName(MetadataReader metadata, EntityHandle constructor)
    {
        EntityHandle type = constructor.Kind switch
        {
            HandleKind.MemberReference => metadata.GetMemberReference((MemberReferenceHandle)constructor).Parent,
            HandleKind.MethodDefinition => metadata.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType(),
            _ => throw new InvalidOperationException("unsupported custom-attribute constructor"),
        };
        return type.Kind switch
        {
            HandleKind.TypeReference => GetTypeName(metadata, metadata.GetTypeReference((TypeReferenceHandle)type)),
            HandleKind.TypeDefinition => GetTypeName(metadata, metadata.GetTypeDefinition((TypeDefinitionHandle)type)),
            _ => throw new InvalidOperationException("unsupported custom-attribute type"),
        };
    }

    private static string GetTypeName(MetadataReader metadata, TypeReference type)
    {
        string name = metadata.GetString(type.Name);
        string typeNamespace = metadata.GetString(type.Namespace);
        return string.IsNullOrEmpty(typeNamespace) ? name : $"{typeNamespace}.{name}";
    }

    private static string GetTypeName(MetadataReader metadata, TypeDefinition type)
    {
        string name = metadata.GetString(type.Name);
        string typeNamespace = metadata.GetString(type.Namespace);
        return string.IsNullOrEmpty(typeNamespace) ? name : $"{typeNamespace}.{name}";
    }
}
