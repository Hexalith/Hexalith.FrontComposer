using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
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

            int symbolPackageCount = Directory.EnumerateFiles(packageDirectory, "*.snupkg").Count();
            if (symbolPackageCount != ExpectedPackageCount)
            {
                throw new InvalidOperationException(
                    $"expected exactly {ExpectedPackageCount} root-level symbol packages; found {symbolPackageCount}");
            }

            int assemblyCount = 0;
            foreach (string packageId in packageIds)
            {
                string packagePath = Path.Combine(packageDirectory, $"{packageId}.{candidateVersion}.nupkg");
                if (!File.Exists(packagePath))
                {
                    throw new InvalidOperationException($"{packageId}: expected candidate package is missing");
                }

                string symbolPath = Path.ChangeExtension(packagePath, ".snupkg");
                if (!File.Exists(symbolPath))
                {
                    throw new InvalidOperationException($"{packageId}: expected symbol package is missing");
                }

                assemblyCount += VerifyPackage(packagePath, symbolPath, packageId, candidateVersion, binaryVersion);
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
        string symbolPath,
        string packageId,
        string candidateVersion,
        Version binaryVersion)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        using ZipArchive symbols = ZipFile.OpenRead(symbolPath);
        VerifyNuspec(archive, packageId, candidateVersion, isSymbolPackage: false);
        VerifyNuspec(symbols, packageId, candidateVersion, isSymbolPackage: true);

        string assemblyName = $"{packageId}.dll";
        string[] expectedAssemblyPaths = packageId switch
        {
            "Hexalith.FrontComposer.Cli" => [$"tools/net11.0/any/{assemblyName}"],
            "Hexalith.FrontComposer.SourceTools" => [$"analyzers/dotnet/cs/{assemblyName}"],
            "Hexalith.FrontComposer.Contracts" or "Hexalith.FrontComposer.Schema" =>
                [$"lib/net11.0/{assemblyName}", $"lib/netstandard2.0/{assemblyName}"],
            "Hexalith.FrontComposer.Contracts.UI" or "Hexalith.FrontComposer.Mcp" or
                "Hexalith.FrontComposer.Shell" or "Hexalith.FrontComposer.Testing" => [$"lib/net11.0/{assemblyName}"],
            _ => throw new InvalidOperationException($"{packageId}: no primary assembly paths are defined"),
        };
        ZipArchiveEntry[] primaryAssemblies = archive.Entries
            .Where(entry => string.Equals(entry.Name, assemblyName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (!primaryAssemblies.Select(entry => entry.FullName).Order(StringComparer.Ordinal)
            .SequenceEqual(expectedAssemblyPaths.Order(StringComparer.Ordinal), StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"{packageId}: primary assembly paths must be [{string.Join(", ", expectedAssemblyPaths)}]; " +
                $"found [{string.Join(", ", primaryAssemblies.Select(entry => entry.FullName))}]");
        }

        string scratch = Path.Combine(Path.GetTempPath(), $"frontcomposer-version-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(scratch);
        try
        {
            for (int index = 0; index < primaryAssemblies.Length; index++)
            {
                ZipArchiveEntry entry = primaryAssemblies[index];
                string extracted = Path.Combine(scratch, $"{index}-{assemblyName}");
                entry.ExtractToFile(extracted);
                VerifyAssembly(extracted, packageId, entry.FullName, candidateVersion, binaryVersion);
            }

            VerifySymbols(archive, symbols, packageId, expectedAssemblyPaths);
        }
        finally
        {
            Directory.Delete(scratch, recursive: true);
        }

        return expectedAssemblyPaths.Length;
    }

    private static void VerifyNuspec(
        ZipArchive archive,
        string packageId,
        string candidateVersion,
        bool isSymbolPackage)
    {
        string packageLabel = isSymbolPackage ? $"{packageId} symbol package" : packageId;
        ZipArchiveEntry[] nuspecs = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (nuspecs.Length != 1 || nuspecs[0].FullName != $"{packageId}.nuspec")
        {
            throw new InvalidOperationException($"{packageLabel}: expected exactly one root {packageId}.nuspec");
        }

        using Stream stream = nuspecs[0].Open();
        XElement root = XDocument.Load(stream).Root
            ?? throw new InvalidOperationException($"{packageLabel}: missing nuspec root");
        XNamespace ns = root.Name.Namespace;
        XElement? metadata = root.Name.LocalName == "package" ? root.Elements(ns + "metadata").SingleOrDefault() : null;
        string? nuspecId = metadata?.Elements(ns + "id").SingleOrDefault()?.Value;
        string? nuspecVersion = metadata?.Elements(ns + "version").SingleOrDefault()?.Value;
        if (!string.Equals(nuspecId, packageId, StringComparison.Ordinal)
            || !string.Equals(nuspecVersion, candidateVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{packageLabel}: nuspec id/version '{nuspecId}/{nuspecVersion}' does not match candidate");
        }

        if (isSymbolPackage)
        {
            XElement[] containers = metadata!.Elements(ns + "packageTypes").ToArray();
            XElement[] types = containers.SelectMany(element => element.Elements(ns + "packageType")).ToArray();
            if (containers.Length != 1 || types.Length != 1 || types[0].Attribute("name")?.Value != "SymbolsPackage")
            {
                throw new InvalidOperationException($"{packageLabel}: nuspec must declare exactly one SymbolsPackage type");
            }
        }
    }

    private static void VerifySymbols(
        ZipArchive package,
        ZipArchive symbols,
        string packageId,
        string[] expectedAssemblyPaths)
    {
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ZipArchiveEntry entry in symbols.Entries)
        {
            if (!seenPaths.Add(entry.FullName))
            {
                throw new InvalidOperationException($"{packageId}: duplicate symbol package entry {entry.FullName}");
            }
        }

        if (symbols.GetEntry("[Content_Types].xml") is null || symbols.GetEntry("_rels/.rels") is null)
        {
            throw new InvalidOperationException($"{packageId}: symbol package is missing required ZIP control parts");
        }

        ZipArchiveEntry[] pdbs = symbols.Entries
            .Where(entry => entry.FullName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        string[] expectedPdbPaths = expectedAssemblyPaths.Select(path => Path.ChangeExtension(path, ".pdb")).ToArray();
        if (packageId == "Hexalith.FrontComposer.SourceTools")
        {
            expectedPdbPaths = [.. expectedPdbPaths, "analyzers/dotnet/cs/Hexalith.FrontComposer.Contracts.pdb"];
        }

        if (!pdbs.Select(entry => entry.FullName).Order(StringComparer.Ordinal)
            .SequenceEqual(expectedPdbPaths.Order(StringComparer.Ordinal), StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"{packageId}: symbol package PDB paths must be [{string.Join(", ", expectedPdbPaths)}]; " +
                $"found [{string.Join(", ", pdbs.Select(entry => entry.FullName))}]");
        }

        ZipArchiveEntry? unexpectedEntry = symbols.Entries.FirstOrDefault(entry =>
            !entry.FullName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)
            && entry.FullName != $"{packageId}.nuspec"
            && entry.FullName is not "[Content_Types].xml" and not "_rels/.rels"
            && !(entry.Name.EndsWith(".psmdcp", StringComparison.Ordinal)
                && entry.FullName == $"package/services/metadata/core-properties/{entry.Name}"));
        if (unexpectedEntry is not null)
        {
            throw new InvalidOperationException($"{packageId}: unexpected symbol package entry {unexpectedEntry.FullName}");
        }

        foreach (ZipArchiveEntry pdb in pdbs)
        {
            string assemblyPath = Path.ChangeExtension(pdb.FullName, ".dll");
            ZipArchiveEntry? assembly = package.Entries.SingleOrDefault(entry => entry.FullName == assemblyPath);
            if (assembly is null)
            {
                throw new InvalidOperationException($"{packageId}: symbol {pdb.FullName} has no matching assembly");
            }

            try
            {
                if (!MatchesPortablePdb(assembly, pdb))
                {
                    throw new InvalidOperationException($"{packageId}: symbol {pdb.FullName} does not match its assembly");
                }
            }
            catch (BadImageFormatException error)
            {
                throw new InvalidOperationException($"{packageId}: invalid portable PDB or assembly for {pdb.FullName}", error);
            }
        }
    }

    private static bool MatchesPortablePdb(ZipArchiveEntry assembly, ZipArchiveEntry pdb)
    {
        using MemoryStream pdbStream = ReadEntry(pdb);
        using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(
            pdbStream, MetadataStreamOptions.LeaveOpen);
        DebugMetadataHeader header = provider.GetMetadataReader().DebugMetadataHeader
            ?? throw new BadImageFormatException("Portable PDB debug metadata is missing.");
        var pdbId = new BlobContentId(header.Id);

        using MemoryStream assemblyStream = ReadEntry(assembly);
        using var pe = new PEReader(assemblyStream);
        var debugEntries = pe.ReadDebugDirectory();
        if (!debugEntries.Any(entry => entry.IsPortableCodeView
            && entry.Stamp == pdbId.Stamp
            && pe.ReadCodeViewDebugDirectoryData(entry).Guid == pdbId.Guid))
        {
            return false;
        }

        DebugDirectoryEntry checksumEntry = debugEntries.Single(entry => entry.Type == DebugDirectoryEntryType.PdbChecksum);
        PdbChecksumDebugDirectoryData checksum = pe.ReadPdbChecksumDebugDirectoryData(checksumEntry);
        using var hash = IncrementalHash.CreateHash(new HashAlgorithmName(checksum.AlgorithmName));
        ReadOnlySpan<byte> bytes = pdbStream.GetBuffer().AsSpan(0, checked((int)pdbStream.Length));
        // The compiler hashes the PDB before writing the content ID into its reserved field.
        hash.AppendData(bytes[..header.IdStartOffset]);
        hash.AppendData(new byte[header.Id.Length]);
        hash.AppendData(bytes[(header.IdStartOffset + header.Id.Length)..]);
        return hash.GetHashAndReset().AsSpan().SequenceEqual(checksum.Checksum.AsSpan());
    }

    private static MemoryStream ReadEntry(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        buffer.Position = 0;
        return buffer;
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
