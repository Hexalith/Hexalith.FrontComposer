using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Forms;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Forms;

/// <summary>
/// Story 13.3 VR-01 / VR-02 / AM-18 / AM-19 — component coverage for the focus-only, linked
/// validation summary.
/// </summary>
public sealed class FcValidationSummaryTests : BunitContext {
    private static readonly string[] ExpectedTargets = ["first-input", "second-input"];
    private static readonly string[] DuplicateFormLevelErrors = ["First is invalid.", "The record is locked."];

    public FcValidationSummaryTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddFluentUIComponents();
        _ = Services.AddLocalization();
        _ = Services.AddLogging();
    }

    [Fact]
    public async Task ClientValidationRendersOneNonLiveLinkedSummaryInDeclaredFieldOrder() {
        ValidationModel model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        store.Add(context.Field(nameof(ValidationModel.Second)), "Second is invalid.");
        store.Add(context.Field(nameof(ValidationModel.First)), "First is invalid.");

        IRenderedComponent<FcValidationSummary> cut = Render<FcValidationSummary>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.SummaryId, "validation-summary")
            .Add(component => component.Fields, new[] {
                new FcValidationFieldDescriptor(nameof(ValidationModel.First), "First", "first-input"),
                new FcValidationFieldDescriptor(nameof(ValidationModel.Second), "Second", "second-input"),
            }));

        await cut.InvokeAsync(() => cut.Instance.ShowAndFocusAsync(FcValidationSummaryKind.ClientValidation));

        IElement summary = cut.Find("[data-testid='fc-validation-summary']");
        // AA5-05 — a named group (VR-01), not a region landmark; focus-only, never live.
        summary.GetAttribute("role").ShouldBe("group");
        summary.HasAttribute("aria-live").ShouldBeFalse();
        summary.GetAttribute("aria-labelledby").ShouldBe("validation-summary-title");
        summary.GetAttribute("aria-describedby").ShouldBe("validation-summary-description");
        // VG5-02 — the client-validation kind speaks its own title and the canonical AM-18 sentence.
        cut.Find("#validation-summary-title").TextContent.Trim().ShouldBe("Validation failed");
        cut.Find("#validation-summary-description").TextContent.Trim().ShouldBe("Correct the errors before submitting. 2 errors.");
        string[] targets = [.. cut.FindAll("[data-fc-validation-target]").Select(element => element.GetAttribute("data-fc-validation-target")!)];
        targets.ShouldBe(ExpectedTargets);
        _ = JSInterop.VerifyInvoke("focusValidationOutcome", 1);
    }

    [Fact]
    public async Task LinkedErrorKeepsSamePageFragmentFallbackAndBindsProgrammaticFocus() {
        Services.GetRequiredService<NavigationManager>().NavigateTo("commands/Orders/PlaceOrderCommand?tab=details#stale");
        ValidationModel model = new();
        EditContext context = new(model);
        new ValidationMessageStore(context).Add(context.Field(nameof(ValidationModel.First)), "First is invalid.");

        IRenderedComponent<FcValidationSummary> cut = Render<FcValidationSummary>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.SummaryId, "linked-summary")
            .Add(component => component.Fields, new[] {
                new FcValidationFieldDescriptor(nameof(ValidationModel.First), "First", "first-input"),
            }));

        await cut.InvokeAsync(() => cut.Instance.ShowAndFocusAsync(FcValidationSummaryKind.ClientValidation));

        // BH2-16 — the href stays on this page (the base href would otherwise send #id to the app
        // root), and the rendered Blazor click handler cancels navigation only after scripted focus works.
        cut.Find("[data-fc-validation-target='first-input']").GetAttribute("href")
            .ShouldBe("commands/Orders/PlaceOrderCommand?tab=details#first-input");
        cut.WaitForAssertion(() => {
            IElement link = cut.Find("[data-fc-validation-target='first-input']");
            link.HasAttribute("blazor:onclick").ShouldBeTrue();
            link.HasAttribute("blazor:onclick:preventdefault").ShouldBeTrue();
        });

        await cut.InvokeAsync(() => cut.Find("[data-fc-validation-target='first-input']").Click());

        JSInterop.VerifyInvoke("focusValidationTarget", 1).Single().Arguments.ShouldBe(["linked-summary", "first-input"]);
    }

    [Fact]
    public async Task LinkKeepsNativeFragmentNavigationWhenScriptedFocusIsUnavailable() {
        _ = JSInterop.SetupVoid("focusValidationOutcome", _ => true).SetException(new JSException("focus unavailable"));
        ValidationModel model = new();
        EditContext context = new(model);
        new ValidationMessageStore(context).Add(context.Field(nameof(ValidationModel.First)), "First is invalid.");

        IRenderedComponent<FcValidationSummary> cut = Render<FcValidationSummary>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.SummaryId, "fallback-summary")
            .Add(component => component.Fields, new[] {
                new FcValidationFieldDescriptor(nameof(ValidationModel.First), "First", "first-input"),
            }));

        await cut.InvokeAsync(() => cut.Instance.ShowAndFocusAsync(FcValidationSummaryKind.ClientValidation));
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke("focusValidationOutcome", 1));

        // ECH-10 / BH-04 — without proven scripted focus the native fragment action is not cancelled.
        IElement link = cut.Find("[data-fc-validation-target='first-input']");
        link.HasAttribute("blazor:onclick").ShouldBeTrue();
        link.HasAttribute("blazor:onclick:preventdefault").ShouldBeFalse();
        link.GetAttribute("href").ShouldNotBeNull().ShouldEndWith("#first-input");
    }

    [Fact]
    public async Task LinkRestoresNativeFragmentNavigationAfterAFailedScriptedFocus() {
        _ = JSInterop.SetupVoid("focusValidationTarget", _ => true).SetException(new JSException("target focus unavailable"));
        ValidationModel model = new();
        EditContext context = new(model);
        new ValidationMessageStore(context).Add(context.Field(nameof(ValidationModel.First)), "First is invalid.");

        IRenderedComponent<FcValidationSummary> cut = Render<FcValidationSummary>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.SummaryId, "restored-summary")
            .Add(component => component.Fields, new[] {
                new FcValidationFieldDescriptor(nameof(ValidationModel.First), "First", "first-input"),
            }));

        await cut.InvokeAsync(() => cut.Instance.ShowAndFocusAsync(FcValidationSummaryKind.ClientValidation));
        cut.WaitForAssertion(() => cut.Find("[data-fc-validation-target='first-input']")
            .HasAttribute("blazor:onclick:preventdefault").ShouldBeTrue());

        await cut.InvokeAsync(() => cut.Find("[data-fc-validation-target='first-input']").Click());

        // ECH-10 / BH3-16 — once scripted focus fails, the next activation uses the native fragment link.
        cut.WaitForAssertion(() => {
            IElement link = cut.Find("[data-fc-validation-target='first-input']");
            link.HasAttribute("blazor:onclick").ShouldBeTrue();
            link.HasAttribute("blazor:onclick:preventdefault").ShouldBeFalse();
            link.GetAttribute("href").ShouldNotBeNull().ShouldEndWith("#first-input");
        });
        _ = JSInterop.VerifyInvoke("focusValidationTarget", 1);
    }

    [Fact]
    public async Task VisibleSummaryRebuildsWhenValidationStateChanges() {
        ValidationModel model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        store.Add(context.Field(nameof(ValidationModel.First)), "First is invalid.");
        store.Add(context.Field(nameof(ValidationModel.Second)), "Second is invalid.");

        IRenderedComponent<FcValidationSummary> cut = Render<FcValidationSummary>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.SummaryId, "rebuilt-summary")
            .Add(component => component.Fields, new[] {
                new FcValidationFieldDescriptor(nameof(ValidationModel.First), "First", "first-input"),
                new FcValidationFieldDescriptor(nameof(ValidationModel.Second), "Second", "second-input"),
            }));

        await cut.InvokeAsync(() => cut.Instance.ShowAndFocusAsync(FcValidationSummaryKind.ClientValidation));
        cut.FindAll("[data-fc-validation-target]").Count.ShouldBe(2);

        await cut.InvokeAsync(() => {
            store.Clear(context.Field(nameof(ValidationModel.First)));
            context.NotifyValidationStateChanged();
        });

        // VG2-06 — a corrected field leaves the visible summary without a second focus move.
        cut.WaitForAssertion(() => {
            IElement[] links = [.. cut.FindAll("[data-fc-validation-target]")];
            links.Length.ShouldBe(1);
            links[0].GetAttribute("data-fc-validation-target").ShouldBe("second-input");
            // AA5-06 — one error keeps the canonical AM-18 sentence; only the count is singular.
            cut.Find("#rebuilt-summary-description").TextContent.Trim().ShouldBe("Correct the errors before submitting. One error.");
        });
        _ = JSInterop.VerifyInvoke("focusValidationOutcome", 1);

        await cut.Instance.HideAsync();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='fc-validation-summary']").ShouldBeEmpty());
    }

    [Fact]
    public async Task MappedRejectionUsesExplicitMappedKindWithoutLiveRegion() {
        ValidationModel model = new();
        EditContext context = new(model);
        new ValidationMessageStore(context).Add(context.Field(nameof(ValidationModel.First)), "Rejected value.");

        IRenderedComponent<FcValidationSummary> cut = Render<FcValidationSummary>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.SummaryId, "mapped-summary")
            .Add(component => component.Fields, new[] {
                new FcValidationFieldDescriptor(nameof(ValidationModel.First), "First", "first-input"),
            }));

        await cut.InvokeAsync(() => cut.Instance.ShowAndFocusAsync(FcValidationSummaryKind.MappedServerRejection));

        IElement summary = cut.Find("[data-fc-validation-kind='mapped-server-rejection']");
        summary.GetAttribute("role").ShouldBe("group");
        summary.HasAttribute("aria-live").ShouldBeFalse();
        // VG5-02 — a mapped rejection speaks the AM-19 title and sentence, never the client-validation copy.
        cut.Find("#mapped-summary-title").TextContent.Trim().ShouldBe("Command rejected");
        string description = cut.Find("#mapped-summary-description").TextContent.Trim();
        description.ShouldStartWith("The command was rejected.");
        description.ShouldEndWith("One error.");
        summary.TextContent.ShouldNotContain("Validation failed");
        summary.TextContent.ShouldNotContain("Correct the errors before submitting.");
        summary.TextContent.ShouldNotContain("1 errors");
    }

    [Fact]
    public async Task LinkNamesTheFieldOnceWhenTheMessageAlreadyContainsTheLabel() {
        ValidationModel model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        store.Add(context.Field(nameof(ValidationModel.First)), "The Record Id field is required.");
        store.Add(context.Field(nameof(ValidationModel.Second)), "Must be positive.");

        IRenderedComponent<FcValidationSummary> cut = Render<FcValidationSummary>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.SummaryId, "label-summary")
            .Add(component => component.Fields, new[] {
                new FcValidationFieldDescriptor(nameof(ValidationModel.First), "Record Id", "first-input"),
                new FcValidationFieldDescriptor(nameof(ValidationModel.Second), "Amount", "second-input"),
            }));

        await cut.InvokeAsync(() => cut.Instance.ShowAndFocusAsync(FcValidationSummaryKind.ClientValidation));

        // BH3-17 — a default DataAnnotations message already names its field, so the link is not
        // prefixed again; a message without the label keeps the "Label: message" form.
        string[] links = [.. cut.FindAll("[data-fc-validation-target]").Select(link => link.TextContent.Trim())];
        links.ShouldBe(["The Record Id field is required.", "Amount: Must be positive."]);
    }

    [Fact]
    public async Task LinkKeepsTheLabelWhenItOnlyAppearsInsideAnUnrelatedWord() {
        ValidationModel model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        store.Add(context.Field(nameof(ValidationModel.First)), "Invalid number format.");
        store.Add(context.Field(nameof(ValidationModel.Second)), "The id is required.");

        IRenderedComponent<FcValidationSummary> cut = Render<FcValidationSummary>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.SummaryId, "word-summary")
            .Add(component => component.Fields, new[] {
                new FcValidationFieldDescriptor(nameof(ValidationModel.First), "Id", "first-input"),
                new FcValidationFieldDescriptor(nameof(ValidationModel.Second), "Id", "second-input"),
            }));

        await cut.InvokeAsync(() => cut.Instance.ShowAndFocusAsync(FcValidationSummaryKind.ClientValidation));

        // BH4-03 — "Id" inside "Invalid" does not name the field, so that link keeps its label; the
        // whole word "id" matches case-insensitively and is not prefixed again.
        string[] links = [.. cut.FindAll("[data-fc-validation-target]").Select(link => link.TextContent.Trim())];
        links.ShouldBe(["Id: Invalid number format.", "The id is required."]);
    }

    [Fact]
    public async Task ModelLevelMessageRendersAsAnUnlinkedEntryInTheCount() {
        ValidationModel model = new();
        EditContext context = new(model);
        ValidationMessageStore store = new(context);
        store.Add(context.Field(nameof(ValidationModel.First)), "First is invalid.");
        store.Add(new FieldIdentifier(model, string.Empty), "The record cannot be saved.");

        IRenderedComponent<FcValidationSummary> cut = Render<FcValidationSummary>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.SummaryId, "model-summary")
            .Add(component => component.Fields, new[] {
                new FcValidationFieldDescriptor(nameof(ValidationModel.First), "First", "first-input"),
            }));

        await cut.InvokeAsync(() => cut.Instance.ShowAndFocusAsync(FcValidationSummaryKind.ClientValidation));

        // VG4-04 — a model-level message has no field descriptor, so it is listed without a link and
        // still counts toward the summary total.
        IElement summary = cut.Find("[data-testid='fc-validation-summary']");
        IElement[] entries = [.. summary.QuerySelectorAll("li")];
        entries.Length.ShouldBe(2);
        entries[0].QuerySelector("a[data-fc-validation-target='first-input']").ShouldNotBeNull();
        entries[1].QuerySelector("a").ShouldBeNull();
        entries[1].TextContent.Trim().ShouldBe("The record cannot be saved.");
        summary.TextContent.ShouldContain("2 errors.");
    }

    [Fact]
    public async Task FormLevelErrorRepeatingALinkedFieldMessageIsListedOnce() {
        ValidationModel model = new();
        EditContext context = new(model);
        new ValidationMessageStore(context).Add(context.Field(nameof(ValidationModel.First)), "First is invalid.");

        IRenderedComponent<FcValidationSummary> cut = Render<FcValidationSummary>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.SummaryId, "duplicate-summary")
            .Add(component => component.FormLevelErrors, DuplicateFormLevelErrors)
            .Add(component => component.Fields, new[] {
                new FcValidationFieldDescriptor(nameof(ValidationModel.First), "First", "first-input"),
            }));

        await cut.InvokeAsync(() => cut.Instance.ShowAndFocusAsync(FcValidationSummaryKind.ClientValidation));

        // A form-level error identical to a linked field message stays the single linked entry and is
        // not counted twice; a distinct form-level error is still listed unlinked.
        IElement summary = cut.Find("[data-testid='fc-validation-summary']");
        IElement[] entries = [.. summary.QuerySelectorAll("li")];
        entries.Length.ShouldBe(2);
        entries[0].QuerySelector("a[data-fc-validation-target='first-input']").ShouldNotBeNull();
        entries[1].QuerySelector("a").ShouldBeNull();
        entries[1].TextContent.Trim().ShouldBe("The record is locked.");
        cut.Find("#duplicate-summary-description").TextContent.Trim().ShouldBe("Correct the errors before submitting. 2 errors.");
    }

    private sealed class ValidationModel {
        public string First { get; set; } = string.Empty;

        public string Second { get; set; } = string.Empty;
    }
}
