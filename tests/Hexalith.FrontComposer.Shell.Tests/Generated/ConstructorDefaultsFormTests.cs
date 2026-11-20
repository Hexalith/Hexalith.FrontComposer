using Bunit;

using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services.DerivedValues;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class ConstructorDefaultsFormTests : CommandRendererTestBase {
    [Fact]
    public async Task RenderersInSameCircuitGetIndependentAndInternallyConsistentDefaults() {
        // Use the production registrations and their actual lifetimes with the generated-component test host.
        IServiceCollection shell = new ServiceCollection().AddHexalithFrontComposer();
        foreach (ServiceDescriptor provider in shell.Where(descriptor =>
            descriptor.ImplementationType == typeof(ExplicitDefaultValueProvider)
            || descriptor.ImplementationType == typeof(ConstructorDefaultValueProvider))) {
            Services.Add(provider);
        }

        await InitializeStoreAsync();
        ConstructorDefaultsCommand first = RenderForm();
        ConstructorDefaultsCommand second = RenderForm();

        first.ExternalReference.ShouldNotBe(second.ExternalReference);
        foreach (ConstructorDefaultsCommand form in new[] { first, second }) {
            Guid.TryParse(form.ExternalReference, out _).ShouldBeTrue();
            form.RelatedReference.ShouldBe(form.ExternalReference);
            form.Constant.ShouldBe("constant");
        }
    }

    private ConstructorDefaultsCommand RenderForm() {
        ConstructorDefaultsCommand model = new() { ExternalReference = "", RelatedReference = "", Constant = "" };
        IRenderedComponent<ConstructorDefaultsCommandRenderer> renderer = Render<ConstructorDefaultsCommandRenderer>(
            parameters => parameters.Add(component => component.InitialValue, model));
        renderer.WaitForAssertion(() => model.Override.ShouldBe("explicit"));
        return model;
    }
}
