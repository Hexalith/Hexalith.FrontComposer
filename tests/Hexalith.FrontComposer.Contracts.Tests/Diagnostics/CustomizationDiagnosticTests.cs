using Hexalith.FrontComposer.Contracts.DevMode;
using Hexalith.FrontComposer.Contracts.Diagnostics;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Diagnostics;

/// <summary>
/// Story 6-6 T1: shared customization diagnostic contract invariants.
/// </summary>
public sealed class CustomizationDiagnosticTests {
    [Fact]
    public void Diagnostic_IsMetadataOnlyAndImmutable() {
        CustomizationDiagnostic diagnostic = new(
            Id: "HFC2115",
            Severity: CustomizationDiagnosticSeverity.Warning,
            Phase: CustomizationDiagnosticPhase.Runtime,
            Level: CustomizationLevel.Level3,
            ProjectionTypeName: "Counter.Domain.CounterProjection",
            ComponentTypeName: "Counter.Web.ThrowingSlot",
            Role: "Default",
            FieldName: "Count",
            What: "The Level 3 slot override threw while rendering.",
            Expected: "Generated sibling fields remain interactive.",
            Got: "InvalidOperationException from the override component.",
            Fix: "Fix the override markup and retry the affected slot.",
            Fallback: "Generated field rendering replaced only the failed slot.",
            DocsLink: "https://hexalith.github.io/FrontComposer/diagnostics/HFC2115",
            Properties: new Dictionary<string, string> {
                ["exceptionType"] = "InvalidOperationException",
                ["category"] = "RenderFault",
            });

        diagnostic.Id.ShouldBe("HFC2115");
        diagnostic.Level.ShouldBe(CustomizationLevel.Level3);
        diagnostic.Properties.ShouldContainKey("exceptionType");
        diagnostic.Properties.ShouldNotContainKey("tenantId");
        diagnostic.Properties.ShouldNotContainKey("userId");
        diagnostic.Properties.ShouldNotContainKey("itemPayload");
    }

    [Fact]
    public void Formatter_EmitsCanonicalTeachingSections() {
        CustomizationDiagnostic diagnostic = CustomizationDiagnostic.Create(
            id: "HFC1036",
            severity: CustomizationDiagnosticSeverity.Warning,
            phase: CustomizationDiagnosticPhase.Build,
            level: CustomizationLevel.Level2,
            projectionTypeName: "Counter.Domain.CounterProjection",
            componentTypeName: "Counter.Web.CounterTemplate",
            role: "ActionQueue",
            fieldName: null,
            what: "The template was built against an older compatible contract.",
            expected: "Expected contract version 1.0.0.",
            got: "Got contract version 1.1.0.",
            fix: "Regenerate the starter template at the next safe touch.",
            fallback: null,
            docsLink: "https://hexalith.github.io/FrontComposer/diagnostics/HFC1036");

        string message = CustomizationDiagnosticFormatter.Format(diagnostic);

        message.ShouldContain("What: The template was built against an older compatible contract.");
        message.ShouldContain("Expected: Expected contract version 1.0.0.");
        message.ShouldContain("Got: Got contract version 1.1.0.");
        message.ShouldContain("Fix: Regenerate the starter template at the next safe touch.");
        message.ShouldContain("DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1036");
        message.ShouldNotContain("Fallback:");
    }

    [Fact]
    public void Create_RejectsMissingTeachingSections() {
        Should.Throw<ArgumentException>(() =>
            CustomizationDiagnostic.Create(
                id: "HFC2115",
                severity: CustomizationDiagnosticSeverity.Warning,
                phase: CustomizationDiagnosticPhase.Runtime,
                level: CustomizationLevel.Level4,
                projectionTypeName: "Counter.Domain.CounterProjection",
                componentTypeName: "Counter.Web.ThrowingView",
                role: null,
                fieldName: null,
                what: "",
                expected: "Expected text.",
                got: "Got text.",
                fix: "Fix text.",
                fallback: "Fallback text.",
                docsLink: "https://hexalith.github.io/FrontComposer/diagnostics/HFC2115"));
    }
}
