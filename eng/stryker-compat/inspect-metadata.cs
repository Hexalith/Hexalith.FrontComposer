using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

var result = new List<object>();
foreach (string path in args)
{
    using FileStream stream = File.OpenRead(path);
    using PEReader pe = new(stream);
    MetadataReader reader = pe.GetMetadataReader();
    AssemblyDefinition definition = reader.GetAssemblyDefinition();
    var values = new Dictionary<string, string?>();
    foreach (CustomAttributeHandle handle in definition.GetCustomAttributes())
    {
        CustomAttribute attribute = reader.GetCustomAttribute(handle);
        if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;
        MemberReference constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
        if (constructor.Parent.Kind != HandleKind.TypeReference) continue;
        TypeReference type = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
        string name = reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
        if (name is not ("System.Reflection.AssemblyInformationalVersionAttribute" or "System.Reflection.AssemblyFileVersionAttribute" or "System.Runtime.Versioning.TargetFrameworkAttribute")) continue;
        BlobReader blob = reader.GetBlobReader(attribute.Value);
        if (blob.ReadUInt16() != 1) throw new InvalidDataException("Unexpected attribute prolog.");
        values[name] = blob.ReadSerializedString();
    }
    var references = definition.GetCustomAttributes().Count;
    result.Add(new { file = Path.GetFileName(path), assemblyName = reader.GetString(definition.Name), assemblyVersion = definition.Version.ToString(), values });
}
Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
