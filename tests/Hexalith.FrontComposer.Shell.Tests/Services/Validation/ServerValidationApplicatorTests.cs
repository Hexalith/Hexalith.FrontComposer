using System.Collections.Generic;
using System.Linq;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Services.Validation;

using Microsoft.AspNetCore.Components.Forms;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Validation;

/// <summary>
/// Story 5-2 D5 / D14 / T5 — verify the form-side validation applicator maps allowlisted
/// field paths to <see cref="ValidationMessageStore"/> entries while routing unknown,
/// nested, hostile, or global errors to a form-level MessageBar list.
/// </summary>
public class ServerValidationApplicatorTests {
    [Fact]
    public void Apply_KnownField_AddsValidationMessageToStore() {
        SampleCommand model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        ICommandValidationFieldAllowlist allowlist = new ReflectionCommandValidationFieldAllowlist<SampleCommand>();
        CommandValidationException exception = new(NewProblem(
            field: "Quantity",
            message: "must be > 0"));

        IReadOnlyList<string> formLevel = ServerValidationApplicator.Apply(store, exception, allowlist, model);

        formLevel.ShouldBeEmpty();
        FieldIdentifier identifier = new(model, nameof(SampleCommand.Quantity));
        context.GetValidationMessages(identifier).Single().ShouldBe("must be > 0");
    }

    [Fact]
    public void Apply_KnownFieldWithCaseInsensitiveMatch_StillRoutesToStore() {
        SampleCommand model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        ICommandValidationFieldAllowlist allowlist = new ReflectionCommandValidationFieldAllowlist<SampleCommand>();
        CommandValidationException exception = new(NewProblem(field: "quantity", message: "case insensitive"));

        IReadOnlyList<string> formLevel = ServerValidationApplicator.Apply(store, exception, allowlist, model);

        formLevel.ShouldBeEmpty();
        FieldIdentifier identifier = new(model, nameof(SampleCommand.Quantity));
        context.GetValidationMessages(identifier).Single().ShouldBe("case insensitive");
    }

    [Fact]
    public void Apply_UnknownField_RoutesMessageToFormLevelList() {
        SampleCommand model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        ICommandValidationFieldAllowlist allowlist = new ReflectionCommandValidationFieldAllowlist<SampleCommand>();
        CommandValidationException exception = new(NewProblem(field: "DoesNotExist", message: "ghost field"));

        IReadOnlyList<string> formLevel = ServerValidationApplicator.Apply(store, exception, allowlist, model);

        formLevel.ShouldContain("ghost field");
    }

    [Fact]
    public void Apply_NestedFieldPath_RoutesToFormLevelList() {
        SampleCommand model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        ICommandValidationFieldAllowlist allowlist = new ReflectionCommandValidationFieldAllowlist<SampleCommand>();
        CommandValidationException exception = new(NewProblem(field: "Address.City", message: "nested rejected"));

        IReadOnlyList<string> formLevel = ServerValidationApplicator.Apply(store, exception, allowlist, model);

        formLevel.ShouldContain("nested rejected");
    }

    [Fact]
    public void Apply_GlobalErrors_RoutesToFormLevelList() {
        SampleCommand model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        ICommandValidationFieldAllowlist allowlist = new ReflectionCommandValidationFieldAllowlist<SampleCommand>();
        ProblemDetailsPayload problem = new(
            Title: "Validation failed",
            Detail: null,
            Status: 400,
            EntityLabel: null,
            ValidationErrors: new Dictionary<string, IReadOnlyList<string>>(System.StringComparer.Ordinal),
            GlobalErrors: new[] { "tenant-wide policy block" });
        CommandValidationException exception = new(problem);

        IReadOnlyList<string> formLevel = ServerValidationApplicator.Apply(store, exception, allowlist, model);

        formLevel.ShouldContain("tenant-wide policy block");
    }

    private static ProblemDetailsPayload NewProblem(string field, string message) => new(
        Title: "Validation failed",
        Detail: null,
        Status: 400,
        EntityLabel: null,
        ValidationErrors: new Dictionary<string, IReadOnlyList<string>>(System.StringComparer.Ordinal) {
            [field] = new[] { message },
        },
        GlobalErrors: System.Array.Empty<string>());

    public sealed class SampleCommand {
        public int Quantity { get; set; }
        public string AggregateId { get; set; } = string.Empty;
    }
}
