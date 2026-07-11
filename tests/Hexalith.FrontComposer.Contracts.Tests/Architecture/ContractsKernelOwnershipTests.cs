using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Architecture;

public sealed class ContractsKernelOwnershipTests {
    private static readonly string[] MovedUiTypeNames = [
        "Hexalith.FrontComposer.Contracts.Rendering.Typography",
        "Hexalith.FrontComposer.Contracts.Rendering.FcTypoToken",
        "Hexalith.FrontComposer.Contracts.Rendering.TypographyStyle",
        "Hexalith.FrontComposer.Contracts.Rendering.FieldSlotContext`2",
        "Hexalith.FrontComposer.Contracts.Rendering.ProjectionTemplateContext`1",
        "Hexalith.FrontComposer.Contracts.Rendering.ProjectionTemplateSectionRenderer",
        "Hexalith.FrontComposer.Contracts.Rendering.ProjectionTemplateRowRenderer`1",
        "Hexalith.FrontComposer.Contracts.Rendering.ProjectionTemplateFieldRenderer`1",
        "Hexalith.FrontComposer.Contracts.Rendering.ProjectionTemplateColumnDescriptor",
        "Hexalith.FrontComposer.Contracts.Rendering.ProjectionTemplateSectionDescriptor",
        "Hexalith.FrontComposer.Contracts.Rendering.ProjectionViewContext`1",
        "Hexalith.FrontComposer.Contracts.Shortcuts.IShortcutService",
        "Hexalith.FrontComposer.Contracts.Shortcuts.ShortcutBinding",
    ];

    private static readonly string[] OldAssemblyQualifiedIdentities = [
        "Hexalith.FrontComposer.Contracts.Storage.InMemoryStorageService, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.FcShellOptions, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.FcShellDevModeOptions, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.CustomizationContractValidationMode, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.InlinePopoverRegistry, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.CaptureGridStateAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.RestoreGridStateAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.ClearGridStateAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.PruneExpiredAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.ColumnFilterChangedAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.StatusFilterToggledAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.GlobalSearchChangedAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.SortChangedAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.FiltersResetAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.LoadPageAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.LoadPageSucceededAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.LoadPageNotModifiedAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.LoadPageFailedAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.LoadPageCancelledAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.ClearPendingPagesAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.ColumnVisibilityChangedAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.ResetColumnVisibilityAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.ScrollCapturedAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.ExpandRowAction, Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.Contracts.Rendering.CollapseRowAction, Hexalith.FrontComposer.Contracts",
    ];

    private static readonly string[] NewAssemblyQualifiedIdentities = [
        "Hexalith.FrontComposer.Testing.InMemoryStorageService, Hexalith.FrontComposer.Testing",
        "Hexalith.FrontComposer.Shell.Options.FcShellOptions, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.Options.FcShellDevModeOptions, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.Options.CustomizationContractValidationMode, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.Services.InlinePopoverRegistry, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.CaptureGridStateAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.RestoreGridStateAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ClearGridStateAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.PruneExpiredAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ColumnFilterChangedAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.StatusFilterToggledAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.GlobalSearchChangedAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.SortChangedAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.FiltersResetAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadPageAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadPageSucceededAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadPageNotModifiedAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadPageFailedAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadPageCancelledAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ClearPendingPagesAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ColumnVisibilityChangedAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ResetColumnVisibilityAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ScrollCapturedAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.ExpandedRow.ExpandRowAction, Hexalith.FrontComposer.Shell",
        "Hexalith.FrontComposer.Shell.State.ExpandedRow.CollapseRowAction, Hexalith.FrontComposer.Shell",
    ];

    [Fact]
    public void ContractsNet10_AfterRelocation_ExportsOnlyRetainedKernelTypes() {
        Assembly assembly = typeof(IStorageService).Assembly;
        string[] exportedNames = assembly.GetExportedTypes().Select(type => type.FullName!).ToArray();

        exportedNames.ShouldNotContain(name => OldAssemblyQualifiedIdentities
            .Select(TypeNameFromAssemblyQualifiedIdentity)
            .Contains(name, StringComparer.Ordinal));
        exportedNames.ShouldContain(typeof(IStorageService).FullName!);
        exportedNames.ShouldContain(typeof(IInlinePopover).FullName!);
        exportedNames.ShouldContain(typeof(GridViewSnapshot).FullName!);
        exportedNames.ShouldNotContain(name => MovedUiTypeNames.Contains(name, StringComparer.Ordinal));
        assembly.GetReferencedAssemblies().Select(reference => reference.Name)
            .Any(name => name is "Hexalith.FrontComposer.Shell" or "Hexalith.FrontComposer.Testing")
            .ShouldBeFalse();
        string?[] kernelReferences = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
        kernelReferences.ShouldNotContain("Hexalith.FrontComposer.Contracts.UI");
        kernelReferences.ShouldNotContain("Microsoft.AspNetCore.Components");
        kernelReferences.ShouldNotContain("Microsoft.AspNetCore.Components.Web");
        kernelReferences.ShouldNotContain("Microsoft.FluentUI.AspNetCore.Components");
        assembly.GetReferencedAssemblies().Select(reference => reference.Name)
            .ShouldNotContain("System.ComponentModel.Annotations");
        assembly.GetExportedTypes()
            .SelectMany(GetPublicSignatureTypes)
            .Any(ContainsTaskCompletionSource)
            .ShouldBeFalse();
    }

    [Fact]
    public void ContractsNetStandard20_AfterRelocation_PinsKernelBoundaryAndIntentionalMoves() {
        string configuration = typeof(ContractsKernelOwnershipTests).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";
        string assemblyPath = Path.Combine(
            FindRepoRoot(),
            "src",
            "Hexalith.FrontComposer.Contracts",
            "bin",
            configuration,
            "netstandard2.0",
            "Hexalith.FrontComposer.Contracts.dll");
        File.Exists(assemblyPath).ShouldBeTrue($"Build the netstandard2.0 Contracts target before running this test: {assemblyPath}");

        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);
        MetadataReader reader = peReader.GetMetadataReader();
        string[] exportedNames = reader.TypeDefinitions
            .Select(reader.GetTypeDefinition)
            .Where(type => (type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
            .Select(type => $"{reader.GetString(type.Namespace)}.{reader.GetString(type.Name)}")
            .ToArray();
        string[] typeReferences = reader.TypeReferences
            .Select(reader.GetTypeReference)
            .Select(type => reader.GetString(type.Name))
            .ToArray();
        string[] assemblyReferences = reader.AssemblyReferences
            .Select(reader.GetAssemblyReference)
            .Select(reference => reader.GetString(reference.Name))
            .ToArray();

        exportedNames.ShouldNotContain(name => OldAssemblyQualifiedIdentities
            .Select(TypeNameFromAssemblyQualifiedIdentity)
            .Contains(name, StringComparer.Ordinal));
        exportedNames.ShouldContain(typeof(IStorageService).FullName!);
        exportedNames.ShouldContain(typeof(IInlinePopover).FullName!);
        exportedNames.ShouldContain(typeof(GridViewSnapshot).FullName!);
        exportedNames.ShouldNotContain(name => MovedUiTypeNames.Contains(name, StringComparer.Ordinal));
        typeReferences.ShouldNotContain("TaskCompletionSource`1");
        assemblyReferences.ShouldNotContain("Hexalith.FrontComposer.Shell");
        assemblyReferences.ShouldNotContain("Hexalith.FrontComposer.Testing");
        assemblyReferences.ShouldNotContain("System.ComponentModel.Annotations");
        assemblyReferences.ShouldNotContain("Hexalith.FrontComposer.Contracts.UI");
        assemblyReferences.ShouldNotContain("Microsoft.AspNetCore.Components");
        assemblyReferences.ShouldNotContain("Microsoft.AspNetCore.Components.Web");
        assemblyReferences.ShouldNotContain("Microsoft.FluentUI.AspNetCore.Components");

        OldAssemblyQualifiedIdentities.Length.ShouldBe(25);
        NewAssemblyQualifiedIdentities.Length.ShouldBe(25);
    }

    private static bool ContainsTaskCompletionSource(Type type) {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TaskCompletionSource<>)) {
            return true;
        }

        return type.HasElementType
            ? ContainsTaskCompletionSource(type.GetElementType()!)
            : type.IsGenericType && type.GetGenericArguments().Any(ContainsTaskCompletionSource);
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

    private static IEnumerable<Type> GetPublicSignatureTypes(Type type) {
        const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        return type.GetConstructors(PublicDeclared).SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))
            .Concat(type.GetMethods(PublicDeclared).Select(method => method.ReturnType))
            .Concat(type.GetMethods(PublicDeclared).SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)))
            .Concat(type.GetProperties(PublicDeclared).Select(property => property.PropertyType))
            .Concat(type.GetFields(PublicDeclared).Select(field => field.FieldType));
    }

    private static string TypeNameFromAssemblyQualifiedIdentity(string identity)
        => identity[..identity.IndexOf(',', StringComparison.Ordinal)];
}
