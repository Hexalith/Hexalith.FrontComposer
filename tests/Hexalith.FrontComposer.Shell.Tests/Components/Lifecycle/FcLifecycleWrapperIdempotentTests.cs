using Bunit;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;

using Microsoft.Extensions.DependencyInjection;


using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// Story 2-5 Task 8.3 — idempotent Info MessageBar coverage (AC2 / D3 / D6 / D7 / D14).
/// </summary>
public sealed class FcLifecycleWrapperIdempotentTests : LifecycleWrapperTestBase {
    [Fact]
    public void LateMountedWrapperDoesNotRepeatDeliveredIdempotentTerminalSpeech()
    {
        using LifecycleStateService service = new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime);
        RegisterLifecycleService(service);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable speech = announcements.Subscribe("lifecycle:" + DefaultCorrelationId, message =>
        {
            if (message.Length > 0) { spoken.Add(message); }
        });
        IRenderedComponent<FcLifecycleWrapper> continuous = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId));
        service.Transition(DefaultCorrelationId, CommandLifecycleState.Submitting, "accepted-id");
        service.Transition(DefaultCorrelationId, CommandLifecycleState.Acknowledged, "accepted-id");
        service.Transition(DefaultCorrelationId, CommandLifecycleState.Confirmed, "accepted-id", idempotencyResolved: true);
        continuous.WaitForAssertion(() => continuous.Find("[data-testid='fc-idempotent']").ShouldNotBeNull());
        spoken.Count(message => message == "Command confirmed.").ShouldBe(1);

        IRenderedComponent<FcLifecycleWrapper> late = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId));
        late.WaitForAssertion(() => late.Find("[data-testid='fc-idempotent']").ShouldNotBeNull());
        spoken.Count(message => message == "Command confirmed.").ShouldBe(1);
    }

    [Fact]
    public void LateMountedWrapperReplaysLegacyConfirmedIdempotentOutcome()
    {
        using LifecycleStateService service = new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime);
        RegisterLifecycleService(service);
        service.Transition(DefaultCorrelationId, CommandLifecycleState.Submitting, "accepted-id");
        service.Transition(DefaultCorrelationId, CommandLifecycleState.Acknowledged, "accepted-id");
        service.Transition(DefaultCorrelationId, CommandLifecycleState.Confirmed, "accepted-id", idempotencyResolved: true);

        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-idempotent']").TextContent
            .ShouldContain("Already confirmed"));
        cut.Markup.ShouldNotContain("data-testid=\"fc-confirmed\"");
        cut.Find("[data-testid='fc-surface-status']").TextContent
            .ShouldBe("Command confirmed.");
    }

    [Fact]
    public void Idempotent_confirmed_renders_info_bar_with_default_copy() {
        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService();

        push(IdempotentConfirmed());

        cut.Markup.ShouldContain("fc-idempotent", Case.Sensitive);
        cut.Markup.ShouldContain("Already confirmed");
        cut.Markup.ShouldContain("This was already confirmed — no action needed.");
        cut.Markup.ShouldNotContain("fc-confirmed", Case.Sensitive, "Success bar must NOT render concurrently (AC2).");
    }

    [Fact]
    public void Idempotent_confirmed_honours_adopter_override() {
        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService(
            idempotentInfoMessage: "Another user already approved this order.");

        push(IdempotentConfirmed());

        cut.Markup.ShouldContain("Another user already approved this order.");
        cut.Find("[data-testid='fc-surface-status']").TextContent
            .ShouldBe("Another user already approved this order.");
    }

    [Fact]
    public void Idempotent_copy_is_plain_text_not_markup() {
        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService(
            idempotentInfoMessage: "<script>alert('xss')</script>");

        push(IdempotentConfirmed());

        cut.Markup.ShouldContain("&lt;script&gt;");
        cut.Markup.ShouldNotContain("<script>alert", Case.Sensitive);
    }

    [Fact]
    public async Task Idempotent_bar_auto_dismisses_after_IdempotentInfoToastDurationMs() {
        int durationMs = new FcShellOptions().IdempotentInfoToastDurationMs; // default 5000

        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService();

        push(IdempotentConfirmed());
        cut.Markup.ShouldContain("fc-idempotent");

        FakeTime.Advance(TimeSpan.FromMilliseconds(durationMs + 10));
        await cut.InvokeAsync(() => { });

        cut.Markup.ShouldNotContain("fc-idempotent");
        cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBeEmpty();
    }

    [Fact]
    public void Non_idempotent_confirmed_still_renders_success_bar() {
        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService();

        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, FakeTime.GetUtcNow(), idempotencyResolved: false));

        cut.Markup.ShouldContain("fc-confirmed");
        cut.Markup.ShouldNotContain("fc-idempotent");
    }

    private CommandLifecycleTransition IdempotentConfirmed()
        => TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, FakeTime.GetUtcNow(), idempotencyResolved: true);
    [Theory]
    [InlineData("en", "Command confirmed.")]
    [InlineData("fr", "Commande confirmée.")]
    public void WhitespaceIdempotentCopyUsesLocalizedConfirmation(string culture, string expected)
    {
        System.Globalization.CultureInfo.CurrentUICulture = new(culture);
        var (cut, push) = RenderWrapperWithLiveService(idempotentInfoMessage: "  ");
        push(IdempotentConfirmed());
        cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBe(expected);
        cut.Find("[data-testid='fc-idempotent']").TextContent.ShouldNotBeEmpty();
    }

}
