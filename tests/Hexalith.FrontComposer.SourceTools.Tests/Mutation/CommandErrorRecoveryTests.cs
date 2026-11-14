using System.Text.Json;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Checks failed-command isolation and recovery of the complete generated command contract.</summary>
public sealed class CommandErrorRecoveryTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid policy")]
    [InlineData("Orders/*")]
    [InlineData("::")]
    [InlineData("\t")]
    [Trait("Category", "MutationErrorRegression")]
    public void InvalidPolicyRepairRestoresMetadataAndDoesNotReuseTheRejectedCacheEntry(string? policy) {
        string healthy = Source("HealthyCommand", "Recovery.Manage");
        string bad = Source("AffectedCommand", policy);
        string repaired = Source("AffectedCommand", "Recovery.Manage");
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([healthy, bad]);
        CommandParseResult refused = CompilationHelper.ParseCommand(compilation, "Recovery.AffectedCommand");
        refused.Model.ShouldBeNull();
        DiagnosticInfo error = refused.Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1056");
        error.Severity.ShouldBe("Error");
        error.Message.ShouldContain("AffectedCommand");
        AttributeData attribute = compilation.GetTypeByMetadataName("Recovery.AffectedCommand")!.GetAttributes()
            .Single(item => item.AttributeClass!.Name == "RequiresPolicyAttribute");
        FileLinePositionSpan expected = attribute.ApplicationSyntaxReference!.GetSyntax(TestContext.Current.CancellationToken).GetLocation().GetLineSpan();
        error.FilePath.ShouldBe(expected.Path);
        error.Line.ShouldBe(expected.StartLinePosition.Line);
        error.Column.ShouldBe(expected.StartLinePosition.Character);
        AssertSuppressedCommandCanRecover(compilation, repaired, "HFC1056");
    }

    [Theory]
    [InlineData("init;")]
    [InlineData("private set;")]
    [InlineData("")]
    [Trait("Category", "MutationErrorRegression")]
    public void InvalidDerivableSetterCannotPoisonAnotherCommandOrSurviveRepair(string setter) {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([
            Source("HealthyCommand", "Recovery.Manage"),
            Source("AffectedCommand", "Recovery.Manage", messageIdSetter: setter)]);
        CommandParseResult refused = CompilationHelper.ParseCommand(compilation, "Recovery.AffectedCommand");
        refused.Model.ShouldBeNull();
        DiagnosticInfo error = refused.Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1016");
        error.Severity.ShouldBe("Error");
        error.Message.ShouldContain("MessageId");
        IPropertySymbol property = compilation.GetTypeByMetadataName("Recovery.AffectedCommand")!
            .GetMembers("MessageId").OfType<IPropertySymbol>().Single();
        FileLinePositionSpan expected = property.Locations.Single().GetLineSpan();
        error.FilePath.ShouldBe(expected.Path);
        error.Line.ShouldBe(expected.StartLinePosition.Line);
        error.Column.ShouldBe(expected.StartLinePosition.Character);
        AssertSuppressedCommandCanRecover(compilation, Source("AffectedCommand", "Recovery.Manage"), "HFC1016");
    }

    [Theory]
    [InlineData("object")]
    [InlineData("byte[]")]
    [InlineData("Dictionary<string, int>")]
    [InlineData("(int X, int Y)")]
    [Trait("Category", "MutationErrorRegression")]
    public void UnsupportedPayloadRepairRestoresTheFormAndMcpFieldWithoutDroppingOtherPayload(string unsupported) {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([
            Source("HealthyCommand", "Recovery.Manage"),
            Source("AffectedCommand", "Recovery.Manage", rawType: unsupported)]);
        CommandParseResult refused = CompilationHelper.ParseCommand(compilation, "Recovery.AffectedCommand");
        CommandModel model = refused.Model.ShouldNotBeNull();
        refused.Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1002").Message.ShouldContain("Raw");
        CommandFormModel fallback = CommandFormTransform.Transform(model);
        FormFieldModel field = fallback.Fields.Single(item => item.PropertyName == "Raw");
        field.TypeCategory.ShouldBe(FormFieldTypeCategory.Placeholder);
        field.IsRequired.ShouldBeFalse();
        fallback.Fields.Select(item => item.PropertyName).ShouldBe(PayloadNames);
        McpManifestTransform.TransformCommand(model).Parameters.Single(item => item.Name == "Raw").IsUnsupported.ShouldBeTrue();

        GeneratorDriver driver = Driver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        string[] unaffected = Output(driver.GetRunResult(), "HealthyCommand");
        string.Join('\n', Output(driver.GetRunResult(), "AffectedCommand")).ShouldContain("FcFieldPlaceholder");
        SyntaxTree badTree = compilation.SyntaxTrees.Last();
        SyntaxTree fixedTree = CSharpSyntaxTree.ParseText(Source("AffectedCommand", "Recovery.Manage"),
            (CSharpParseOptions)badTree.Options, badTree.FilePath, cancellationToken: TestContext.Current.CancellationToken);
        CSharpCompilation recovered = compilation.ReplaceSyntaxTree(badTree, fixedTree);
        driver = driver.RunGenerators(recovered, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "HFC1002");
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(unaffected);
        CommandModel fresh = CompilationHelper.ParseCommand(recovered, "Recovery.AffectedCommand").Model.ShouldNotBeNull();
        fresh.ShouldNotBe(model);
        AssertCompleteRecoveredContract(fresh);
        CommandFormTransform.Transform(fresh).Fields.Single(item => item.PropertyName == "Raw").TypeCategory.ShouldBe(FormFieldTypeCategory.TextInput);
        McpManifestTransform.TransformCommand(fresh).Parameters.Single(item => item.Name == "Raw").IsUnsupported.ShouldBeFalse();
        AssertCompiles(recovered, driver.GetRunResult());

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.Count(diagnostic => diagnostic.Id == "HFC1002").ShouldBe(1);
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(unaffected);
        string.Join('\n', Output(driver.GetRunResult(), "AffectedCommand")).ShouldContain("FcFieldPlaceholder");
    }

    private static readonly string[] PayloadNames = ["Name", "Quantity", "Price", "Enabled", "UpdatedAt", "Notes", "Raw"];

    private static void AssertSuppressedCommandCanRecover(CSharpCompilation compilation, string repaired, string diagnosticId) {
        GeneratorDriver driver = Driver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.ShouldContain(diagnostic => diagnostic.Id == diagnosticId);
        Output(driver.GetRunResult(), "AffectedCommand").ShouldBeEmpty();
        string[] unaffected = Output(driver.GetRunResult(), "HealthyCommand");
        unaffected.ShouldNotBeEmpty();
        SyntaxTree bad = compilation.SyntaxTrees.Last();
        SyntaxTree replacement = CSharpSyntaxTree.ParseText(repaired, (CSharpParseOptions)bad.Options, bad.FilePath, cancellationToken: TestContext.Current.CancellationToken);
        CSharpCompilation recovered = compilation.ReplaceSyntaxTree(bad, replacement);
        driver = driver.RunGenerators(recovered, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult recoveredResult = driver.GetRunResult();
        recoveredResult.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == diagnosticId);
        Output(recoveredResult, "AffectedCommand").ShouldNotBeEmpty();
        Output(recoveredResult, "HealthyCommand").ShouldBe(unaffected);
        CommandModel model = CompilationHelper.ParseCommand(recovered, "Recovery.AffectedCommand").Model.ShouldNotBeNull();
        CommandModel equivalent = CompilationHelper.ParseCommand(recovered, "Recovery.AffectedCommand").Model.ShouldNotBeNull();
        model.ShouldBe(equivalent);
        model.GetHashCode().ShouldBe(equivalent.GetHashCode());
        CommandFormTransform.Transform(model).ShouldBe(CommandFormTransform.Transform(equivalent));
        CommandFluxorTransform.Transform(model).ShouldBe(CommandFluxorTransform.Transform(equivalent));
        CommandRendererTransform.Transform(model, CommandFluxorTransform.Transform(model))
            .ShouldBe(CommandRendererTransform.Transform(equivalent, CommandFluxorTransform.Transform(equivalent)));
        RegistrationModelTransform.TransformCommand(model).ShouldBe(RegistrationModelTransform.TransformCommand(equivalent));
        JsonSerializer.Serialize(McpManifestTransform.TransformCommand(model))
            .ShouldBe(JsonSerializer.Serialize(McpManifestTransform.TransformCommand(equivalent)));
        AssertCompleteRecoveredContract(model);
        AssertCompiles(recovered, recoveredResult);

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.ShouldContain(diagnostic => diagnostic.Id == diagnosticId);
        Output(driver.GetRunResult(), "AffectedCommand").ShouldBeEmpty();
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(unaffected);
    }

    private static void AssertCompleteRecoveredContract(CommandModel model) {
        model.TypeName.ShouldBe("AffectedCommand");
        model.Namespace.ShouldBe("Recovery");
        model.BoundedContext.ShouldBe("Recovery");
        model.AuthorizationPolicyName.ShouldBe("Recovery.Manage");
        model.Density.ShouldBe(CommandDensity.FullPage);
        model.NonDerivableProperties.Select(property => property.Name).ShouldBe(PayloadNames);
        model.DerivableProperties.Select(property => property.Name)
            .ShouldBe(["MessageId", "CommandId", "CorrelationId", "TenantId", "UserId", "Timestamp", "CreatedAt", "ModifiedAt"]);
        CommandFormModel form = CommandFormTransform.Transform(model);
        form.Fields.Select(field => field.PropertyName).ShouldBe(PayloadNames);
        form.Fields.Select(field => field.TypeCategory).ShouldBe([
            FormFieldTypeCategory.TextInput, FormFieldTypeCategory.NumberInput, FormFieldTypeCategory.DecimalInput,
            FormFieldTypeCategory.Switch, FormFieldTypeCategory.DatePicker, FormFieldTypeCategory.TextInput,
            FormFieldTypeCategory.TextInput]);
        form.Fields.Single(field => field.PropertyName == "Notes").IsNullable.ShouldBeTrue();
        form.Fields.Single(field => field.PropertyName == "Notes").IsRequired.ShouldBeFalse();
        form.Fields.Single(field => field.PropertyName == "Quantity").IsRequired.ShouldBeTrue();
        form.AuthorizationPolicyName.ShouldBe("Recovery.Manage");
        RegistrationModelTransform.TransformCommand(model).AuthorizationPolicyName.ShouldBe("Recovery.Manage");
        CommandRendererModel renderer = CommandRendererTransform.Transform(model, CommandFluxorTransform.Transform(model));
        renderer.NonDerivablePropertyNames.ShouldBe(new EquatableArray<string>([.. PayloadNames]));
        renderer.CommandFullyQualifiedName.ShouldBe("Recovery.AffectedCommand");
        renderer.AuthorizationPolicyName.ShouldBe("Recovery.Manage");
        renderer.StateName.ShouldBe("AffectedCommandLifecycleState");
        renderer.ActionsWrapperName.ShouldBe("AffectedCommandActions");
        renderer.FormComponentName.ShouldBe("AffectedCommandForm");
        renderer.SubscriberTypeName.ShouldBe("AffectedCommandLastUsedSubscriber");
        CommandFluxorModel state = CommandFluxorTransform.Transform(model);
        state.FeatureName.ShouldBe("AffectedCommandLifecycleFeature");
        state.ReducersClassName.ShouldBe("AffectedCommandReducers");
        RegistrationModel registration = RegistrationModelTransform.TransformCommand(model);
        registration.IsCommand.ShouldBeTrue();
        registration.HasFullPageRoute.ShouldBeTrue();
        registration.BoundedContext.ShouldBe("Recovery");
        McpCommandDescriptorModel descriptor = McpManifestTransform.TransformCommand(model);
        descriptor.Parameters.Select(parameter => parameter.Name).ShouldBe(PayloadNames);
        descriptor.DerivablePropertyNames.ShouldBe(["MessageId", "CommandId", "CorrelationId", "TenantId", "UserId", "Timestamp", "CreatedAt", "ModifiedAt"]);
        descriptor.CommandTypeName.ShouldBe("Recovery.AffectedCommand");
        descriptor.ProtocolName.ShouldBe("Recovery.AffectedCommand.Execute");
        descriptor.AuthorizationPolicyName.ShouldBe("Recovery.Manage");
    }

    private static CSharpGeneratorDriver Driver() => CSharpGeneratorDriver.Create(new FrontComposerGenerator());

    private static string[] Output(GeneratorDriverRunResult result, string name) => result.Results.Single().GeneratedSources
        .Where(source => source.HintName.Contains(name, StringComparison.Ordinal))
        .OrderBy(source => source.HintName, StringComparer.Ordinal)
        .Select(source => source.HintName + "\n" + source.SourceText).ToArray();

    private static void AssertCompiles(CSharpCompilation input, GeneratorDriverRunResult result) => input.AddSyntaxTrees(result.GeneratedTrees)
        .GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

    private static string Source(string name, string? policy, string rawType = "string", string messageIdSetter = "set;") => $$"""
        using System;
        using System.Collections.Generic;
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Recovery;
        [Command]
        [BoundedContext("Recovery")]
        [RequiresPolicy({{JsonSerializer.Serialize(policy)}})]
        public sealed class {{name}} {
            public string MessageId { get; {{messageIdSetter}} } = "";
            public string CommandId { get; set; } = "";
            public string CorrelationId { get; set; } = "";
            public string TenantId { get; set; } = "";
            public string UserId { get; set; } = "";
            public DateTime Timestamp { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ModifiedAt { get; set; }
            public string Name { get; set; } = "";
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public bool Enabled { get; set; }
            public DateTimeOffset UpdatedAt { get; set; }
            public string? Notes { get; set; }
            public {{rawType}} Raw { get; set; } = default!;
        }
        """;
}
