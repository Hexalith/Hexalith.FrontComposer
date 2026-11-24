using System.Text.RegularExpressions;

using Bunit;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2-2 Task 11.4 — guards the 2-1 ↔ 2-2 form contract: rendering the generated
/// <c>{Cmd}Form</c> with explicit defaults (DerivableFieldsHidden=false, ShowFieldsOnly=null)
/// must produce the same markup as rendering it with the omitted defaults. Catches a renderer
/// or emitter change that silently breaks the backward-compatible parameter contract on which
/// Story 2-1 callers depend.
/// </summary>
public sealed class Story21Story22ContractTests : CommandRendererTestBase {
    [Fact]
    public async Task CommandFormFieldFilterRefreshesWhenTheSameArrayIsResupplied() {
        await InitializeStoreAsync();
        string[] fields = [nameof(TwoFieldCompactCommand.Amount), "UnknownField"];
        IRenderedComponent<TwoFieldCompactCommandForm> form = Render<TwoFieldCompactCommandForm>(
            parameters => parameters.Add(component => component.ShowFieldsOnly, fields));

        form.FindAll(".fc-command-field").Count.ShouldBe(1);
        fields[0] = "amount";
        form.Render(parameters => parameters.Add(component => component.ShowFieldsOnly, fields));
        form.FindAll(".fc-command-field").ShouldBeEmpty();
        string[] singleField = [nameof(TwoFieldCompactCommand.Amount)];
        form.Render(parameters => parameters.Add(component => component.ShowFieldsOnly, singleField));
        form.FindAll(".fc-command-field").Count.ShouldBe(1);
        form.Render(parameters => parameters.Add(component => component.ShowFieldsOnly, Array.Empty<string>()));
        form.FindAll(".fc-command-field").ShouldBeEmpty();
        form.Render(parameters => parameters.Add(component => component.ShowFieldsOnly, null));
        form.FindAll(".fc-command-field").Count.ShouldBe(2);
    }

    [Fact]
    public async Task CommandForm_RendererDelegation_FormBodyStructurallyIdentical() {
        await InitializeStoreAsync();

        IRenderedComponent<TwoFieldCompactCommandForm> defaults = Render<TwoFieldCompactCommandForm>();
        IRenderedComponent<TwoFieldCompactCommandForm> explicitDefaults = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.DerivableFieldsHidden, false)
            .Add(p => p.ShowFieldsOnly, null));

        string normalizedDefaults = NormalizeMarkup(defaults.Markup);
        string normalizedExplicit = NormalizeMarkup(explicitDefaults.Markup);

        normalizedExplicit.ShouldBe(normalizedDefaults,
            "Form rendered with explicit-default 2-2 parameters must match form rendered without them — the contract MUST stay backward-compatible.");
    }

    private static string NormalizeMarkup(string markup) {
        // Collapse whitespace and strip per-render volatility (GUIDs, event handler indices,
        // generated form-input ids) so the comparison reflects structure only.
        string normalized = Regex.Replace(markup, @"\s+", " ").Trim();
        normalized = Regex.Replace(normalized, @"id=""[^""]+""", "id=\"GEN\"");
        normalized = Regex.Replace(normalized, @"for=""[^""]+""", "for=\"GEN\"");
        // Story 13.3 — per-instance form DOM ids embed a type-local counter that also appears inside
        // other attribute values (for example aria-describedby and summary targets).
        normalized = Regex.Replace(normalized, @"fc-command-form-[A-Za-z0-9_-]*?-\d+(?=-[A-Z])", "fc-command-form-GEN");
        normalized = Regex.Replace(normalized, @"blazor:on\w+=""\d+""", "blazor:onevt=\"N\"");
        normalized = Regex.Replace(normalized, @"blazor:elementReference=""[^""]+""", "blazor:elementReference=\"GEN\"");
        return normalized;
    }
}
