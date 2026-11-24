using System.Reflection;
using System.Reflection.Metadata;

using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services.DerivedValues;
using Hexalith.FrontComposer.Shell.Tests.Services;

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

    [Fact]
    public async Task MetadataUpdateReplacesTheSharedConstructorDefaultsAsync() {
        var provider = new ConstructorDefaultValueProvider();
        CancellationToken cancellationToken = Xunit.TestContext.Current.CancellationToken;
        Task<DerivedValueResult> ResolveAsync(string property) =>
            provider.ResolveAsync(typeof(ConstructorDefaultsCommand), property, null, cancellationToken);

        object? original = (await ResolveAsync(nameof(ConstructorDefaultsCommand.ExternalReference))).Value;
        (await ResolveAsync(nameof(ConstructorDefaultsCommand.RelatedReference))).Value.ShouldBe(original);

        MetadataUpdateHandlerAttribute handler = typeof(ConstructorDefaultValueProvider).Assembly
            .GetCustomAttributes<MetadataUpdateHandlerAttribute>()
            .Single(attribute => attribute.HandlerType == typeof(DerivedValueMetadataUpdateHandler));
        handler.HandlerType.GetMethod(nameof(DerivedValueMetadataUpdateHandler.ClearCache))!
            .Invoke(null, [new[] { typeof(ConstructorDefaultsCommand) }]);

        object? updated = (await ResolveAsync(nameof(ConstructorDefaultsCommand.ExternalReference))).Value;
        updated.ShouldNotBe(original);
        (await ResolveAsync(nameof(ConstructorDefaultsCommand.RelatedReference))).Value.ShouldBe(updated);
    }

    [Fact]
    public async Task MetadataUpdateReplacesCachedExplicitDefaultsAsync() {
        var provider = new ExplicitDefaultValueProvider();
        CancellationToken cancellationToken = Xunit.TestContext.Current.CancellationToken;
        Task<DerivedValueResult> ResolveAsync() =>
            provider.ResolveAsync(typeof(DerivedValueProviderChainTests.TestCommand), nameof(DerivedValueProviderChainTests.TestCommand.Endpoint), null, cancellationToken);

        Uri original = (await ResolveAsync()).Value.ShouldBeOfType<Uri>();
        (await ResolveAsync()).Value.ShouldBeSameAs(original);

        DerivedValueMetadataUpdateHandler.ClearCache(updatedTypes: null);

        Uri refreshed = (await ResolveAsync()).Value.ShouldBeOfType<Uri>();
        refreshed.ShouldBe(original);
        refreshed.ShouldNotBeSameAs(original);
    }

    private ConstructorDefaultsCommand RenderForm() {
        ConstructorDefaultsCommand model = new() { ExternalReference = "", RelatedReference = "", Constant = "" };
        IRenderedComponent<ConstructorDefaultsCommandRenderer> renderer = Render<ConstructorDefaultsCommandRenderer>(
            parameters => parameters.Add(component => component.InitialValue, model));
        renderer.WaitForAssertion(() => model.Override.ShouldBe("explicit"));
        return model;
    }
}
