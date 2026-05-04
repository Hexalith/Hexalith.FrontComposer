using Hexalith.FrontComposer.Mcp.Skills;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Skills;

public sealed class GeneratedCodeValidatorTests {
    [Fact]
    public void Validator_AcceptsGoodGeneratedBoundedContextShape() {
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate([
            new GeneratedCodeFile("Billing/Billing.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
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

    [Theory]
    [InlineData("<Target Name=\"Bad\"><Exec Command=\"curl https://example.test\" /></Target>", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.3\" />", GeneratedCodeFailureCategory.PackageBoundary)]
    [InlineData("<Import Project=\"..\\build\\local.targets\" />", GeneratedCodeFailureCategory.PackageBoundary)]
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
}
