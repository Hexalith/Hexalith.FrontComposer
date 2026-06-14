using System.Reflection;

using Hexalith.FrontComposer.Shell.Components.DataGrid;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

public sealed class FcTblPackageBoundaryTests {
    [Fact]
    public void FcTbl_PublicApi_MatchesIntentionalBaseline() {
        string root = FindRepoRoot();
        string baselinePath = Path.Combine(root, "src", "Hexalith.FrontComposer.Shell", "PublicAPI.FcTbl.Shipped.txt");
        string[] expected = File.ReadAllLines(baselinePath)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] actual = EnumeratePublicApi(typeof(FcColumnFilterCell).Assembly)
            .Order(StringComparer.Ordinal)
            .ToArray();

        actual.ShouldBe(expected);
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
            .Where(type => type.Namespace == "Hexalith.FrontComposer.Shell.Components.DataGrid")
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
