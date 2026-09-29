using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Services.Validation;

using Microsoft.AspNetCore.Components.Forms;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Validation;

/// <summary>
/// Story 5-2 D5 / D14 / T5 — verify the form-side validation applicator maps allowlisted
/// field paths to <see cref="ValidationMessageStore"/> entries while routing unknown,
/// nested, hostile, or global errors to a form-level MessageBar list.
/// </summary>
public class ServerValidationApplicatorTests {
    private static readonly string[] GlobalPolicyErrors = ["tenant-wide policy block"];
    private static readonly string[] MappedQuantityErrors = ["Quantity is no longer available."];
    private static readonly string[] NestedValueErrors = ["Nested value is invalid."];
    private static readonly string[] RejectionGlobalErrors = ["Review this command."];
    private static readonly string[] ExpectedMixedUnmappedErrors = ["Review this command.", "Nested value is invalid."];
    private static readonly string[] ExpectedUnmappedErrors = ["Authoritative form-level failure"];

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
            GlobalErrors: GlobalPolicyErrors);
        CommandValidationException exception = new(problem);

        IReadOnlyList<string> formLevel = ServerValidationApplicator.Apply(store, exception, allowlist, model);

        formLevel.ShouldContain("tenant-wide policy block");
    }

    [Fact]
    public void ApplyRejection_ExplicitlyReportsMappedFieldsAndPreservesUnmappedMessages() {
        SampleCommand model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        ICommandValidationFieldAllowlist allowlist = new ReflectionCommandValidationFieldAllowlist<SampleCommand>();
        ProblemDetailsPayload problem = new(
            Title: "Rejected",
            Detail: "Correct the quantity.",
            Status: 409,
            EntityLabel: null,
            ValidationErrors: new Dictionary<string, IReadOnlyList<string>>(System.StringComparer.Ordinal) {
                ["quantity"] = MappedQuantityErrors,
                ["Nested.Value"] = NestedValueErrors,
            },
            GlobalErrors: RejectionGlobalErrors);

        ServerValidationApplicationResult result = ServerValidationApplicator.ApplyRejection(
            store,
            CommandRejectedException.FromProblem("Rejected", "Correct the quantity.", problem),
            allowlist,
            model);

        result.HasMappedFieldErrors.ShouldBeTrue();
        result.MappedFieldCount.ShouldBe(1);
        context.GetValidationMessages(context.Field(nameof(SampleCommand.Quantity))).Single()
            .ShouldBe("Quantity is no longer available.");
        result.UnmappedMessages.ShouldBe(ExpectedMixedUnmappedErrors);
    }

    [Fact]
    public void ApplyRejection_UnmappedRecoveryDoesNotInventFieldErrors() {
        SampleCommand model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        ICommandValidationFieldAllowlist allowlist = new ReflectionCommandValidationFieldAllowlist<SampleCommand>();
        ProblemDetailsPayload problem = NewProblem("Unknown", "Authoritative form-level failure") with { Status = 409 };

        ServerValidationApplicationResult result = ServerValidationApplicator.ApplyRejection(
            store,
            CommandRejectedException.FromProblem("Rejected", "Retry.", problem),
            allowlist,
            model);

        result.HasMappedFieldErrors.ShouldBeFalse();
        context.GetValidationMessages(context.Field(nameof(SampleCommand.Quantity))).ShouldBeEmpty();
        result.UnmappedMessages.ShouldBe(ExpectedUnmappedErrors);
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
