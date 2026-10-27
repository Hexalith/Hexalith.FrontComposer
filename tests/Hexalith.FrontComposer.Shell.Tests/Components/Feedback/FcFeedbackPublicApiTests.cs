using System.Reflection;
using Hexalith.FrontComposer.Shell.Components.Feedback;
using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Feedback;

public sealed class FcFeedbackPublicApiTests
{
    [Fact]
    public void FeedbackAndDialogApiMatchTheIntentionalAdditiveManifest()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx")))
        {
            directory = directory.Parent;
        }
        directory.ShouldNotBeNull();
        string manifest = Path.Combine(directory.FullName, "src/Hexalith.FrontComposer.Shell/PublicAPI.Feedback.Shipped.txt");
        string[] prefixes = ["Hexalith.FrontComposer.Shell.Components.Feedback.", "Hexalith.FrontComposer.Shell.Services.Feedback.",
            "Hexalith.FrontComposer.Shell.Services.Downloads.", "Hexalith.FrontComposer.Shell.Extensions.FrontComposerFeedbackServiceExtensions",
            "Hexalith.FrontComposer.Shell.Components.Forms.FcDestructiveConfirmationDialog"];
        EnumeratePublicApi(typeof(FcLiveRegion).Assembly)
            .Where(line => prefixes.Any(prefix => line.StartsWith(prefix, StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal).ToArray()
            .ShouldBe(File.ReadAllLines(manifest).Where(line => !string.IsNullOrWhiteSpace(line)).Order(StringComparer.Ordinal).ToArray());
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

}
