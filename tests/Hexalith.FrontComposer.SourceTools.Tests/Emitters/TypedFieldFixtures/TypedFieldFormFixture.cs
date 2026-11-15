using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;
// Low-level render frames are intentional regression inputs for generated component parameters.
#pragma warning disable BL0006
/// <summary>Compiles and hosts a generated form to exercise its typed field callbacks.</summary>
internal sealed class TypedFieldFormFixture : IDisposable
{
    private readonly ComponentBase _component;
    private readonly Type _type;
    private readonly ServiceProvider _services;
    private TypedFieldFormFixture(ComponentBase component)
    {
        _component = component;
        _type = component.GetType();
        Model = GetField("_model").ShouldNotBeNull();
        EditContext = new EditContext(Model);
        SetField("_editContext", EditContext);
        SetField("_serverValidationMessages", new ValidationMessageStore(EditContext));
        SetField("_rejectionValidationMessages", new ValidationMessageStore(EditContext));
        PropertyInfo stateProperty = _type.GetProperty("LifecycleState", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull();
        Type stateType = stateProperty.PropertyType.GenericTypeArguments.Single();
        ConstructorInfo constructor = stateType.GetConstructors().Single();
        object state = constructor.Invoke(constructor.GetParameters().Select(parameter => parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null).ToArray());
        stateProperty.SetValue(component, Activator.CreateInstance(typeof(TypedFieldFixedState<>).MakeGenericType(stateType), state));
        foreach (PropertyInfo property in _type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
        {
            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(IStringLocalizer<>))
            {
                property.SetValue(component, Activator.CreateInstance(typeof(TypedFieldFixedLocalizer<>).MakeGenericType(property.PropertyType.GenericTypeArguments.Single())));
            }
        }

        _type.GetProperty("JS", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull().SetValue(component, new TypedFieldPrerenderJsRuntime());
        _services = new ServiceCollection().BuildServiceProvider();
        Renderer = new TypedFieldFixtureRenderer(_services);
        Renderer.Attach(component);
    }

    /// <summary>Gets the attached framework renderer.</summary>
    internal TypedFieldFixtureRenderer Renderer { get; }
    /// <summary>Gets the generated command model.</summary>
    internal object Model { get; }
    /// <summary>Gets the command model validation context.</summary>
    internal EditContext EditContext { get; }

    /// <summary>Compiles a form with nullable or non-nullable identifier and time fields.</summary>
    /// <param name="nullable">Whether both generated input properties allow null.</param>
    internal static TypedFieldFormFixture Create(bool nullable)
    {
        string suffix = nullable ? "?" : string.Empty;
        string source = "using System; using Hexalith.FrontComposer.Contracts.Attributes; namespace TypedInputs; [Command] public sealed class EditCommand { public Guid" + suffix + " Identifier { get; set; } public TimeOnly" + suffix + " At { get; set; } }";
        CSharpCompilation input = CompilationHelper.CreateCompilation(source, assemblyName: "TypedInput_" + Guid.NewGuid().ToString("N"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator()).WithUpdatedParseOptions(CSharpParseOptions.Default.WithPreprocessorSymbols("NET11_0_OR_GREATER"));
        GeneratorDriverRunResult result = driver.RunGenerators(input, TestContext.Current.CancellationToken).GetRunResult();
        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        CSharpCompilation output = input.AddSyntaxTrees(result.GeneratedTrees);
        using MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = output.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        emitted.Success.ShouldBeTrue(string.Join(Environment.NewLine, emitted.Diagnostics));
        stream.Position = 0;
        Type type = AssemblyLoadContext.Default.LoadFromStream(stream).GetType("TypedInputs.EditCommandForm").ShouldNotBeNull();
        return new TypedFieldFormFixture((ComponentBase)Activator.CreateInstance(type).ShouldNotBeNull());
    }

    /// <summary>Reads the actual generated submission parse-error gate.</summary>
    internal bool HasParseErrors() => (bool)_type.GetMethod("HasClientParseErrors", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull().Invoke(_component, null)!;
    /// <summary>Renders the generated field and validates its parameters against the actual Fluent component.</summary>
    /// <param name="name">The generated command property name.</param>
    internal TypedFieldInputParameters GetInput(string name)
    {
        using RenderTreeBuilder root = new();
        _type.GetMethod("BuildRenderTree", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull().Invoke(_component, [root]);
        RenderFragment wrap = Attributes(root).Select(pair => pair.Value).OfType<RenderFragment>().Single();
        using RenderTreeBuilder form = new();
        wrap(form);
        RenderFragment<EditContext> body = Attributes(form).Select(pair => pair.Value).OfType<RenderFragment<EditContext>>().Single();
        using RenderTreeBuilder fields = new();
        body(EditContext)(fields);
        ArrayRange<RenderTreeFrame> frames = fields.GetFrames();
        for (int i = 0; i < frames.Count; i++)
        {
            if (frames.Array[i].FrameType != RenderTreeFrameType.Component)
            {
                continue;
            }

            Type type = frames.Array[i].ComponentType;
            Dictionary<string, object?> values = new(StringComparer.Ordinal);
            for (int j = i + 1; j < frames.Count && frames.Array[j].FrameType == RenderTreeFrameType.Attribute; j++)
            {
                values[frames.Array[j].AttributeName] = frames.Array[j].AttributeValue;
            }

            if (values.TryGetValue("Name", out object? value) && Equals(value, name))
            {
                // ParameterView also checks the real V5 component's public parameter names/types.
                object control = Activator.CreateInstance(type, new LibraryConfiguration()).ShouldNotBeNull();
                ParameterView.FromDictionary(values).SetParameterProperties(control);
                return new TypedFieldInputParameters(type, values);
            }
        }

        throw new InvalidOperationException("Generated field was not rendered: " + name);
    }

    private static Dictionary<string, object?> Attributes(RenderTreeBuilder builder)
    {
        ArrayRange<RenderTreeFrame> frames = builder.GetFrames();
        return frames.Array.Take(frames.Count).Where(frame => frame.FrameType == RenderTreeFrameType.Attribute).ToDictionary(frame => frame.AttributeName, frame => (object? )frame.AttributeValue);
    }

    private object? GetField(string name) => _type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull().GetValue(_component);
    private void SetField(string name, object value) => _type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull().SetValue(_component, value);
    /// <inheritdoc/>
    public void Dispose()
    {
        Renderer.Dispose();
        _services.Dispose();
    }
}
#pragma warning restore BL0006
