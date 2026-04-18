using Bunit;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// Story 2-5 Task 8.3 — idempotent Info MessageBar coverage (AC2 / D3 / D6 / D7 / D14).
/// </summary>
public sealed class FcLifecycleWrapperIdempotentTests : LifecycleWrapperTestBase {
    [Fact]
    public void Idempotent_confirmed_renders_info_bar_with_default_copy() {
        var (cut, push) = RenderWrapperWithLiveService();

        push(IdempotentConfirmed());

        cut.Markup.ShouldContain("fc-idempotent", Case.Sensitive);
        cut.Markup.ShouldContain("Already confirmed");
        cut.Markup.ShouldContain("This was already confirmed — no action needed.");
        cut.Markup.ShouldNotContain("fc-confirmed", Case.Sensitive, "Success bar must NOT render concurrently (AC2).");
    }

    [Fact]
    public void Idempotent_confirmed_honours_adopter_override() {
        var (cut, push) = RenderWrapperWithLiveService(
            idempotentInfoMessage: "Another user already approved this order.");

        push(IdempotentConfirmed());

        cut.Markup.ShouldContain("Another user already approved this order.");
    }

    [Fact]
    public void Idempotent_copy_is_plain_text_not_markup() {
        var (cut, push) = RenderWrapperWithLiveService(
            idempotentInfoMessage: "<script>alert('xss')</script>");

        push(IdempotentConfirmed());

        cut.Markup.ShouldContain("&lt;script&gt;");
        cut.Markup.ShouldNotContain("<script>alert", Case.Sensitive);
    }

    [Fact]
    public async Task Idempotent_bar_auto_dismisses_after_IdempotentInfoToastDurationMs() {
        int durationMs = new FcShellOptions().IdempotentInfoToastDurationMs; // default 5000

        var (cut, push) = RenderWrapperWithLiveService();

        push(IdempotentConfirmed());
        cut.Markup.ShouldContain("fc-idempotent");

        FakeTime.Advance(TimeSpan.FromMilliseconds(durationMs + 10));
        await cut.InvokeAsync(() => { });

        cut.Markup.ShouldNotContain("fc-idempotent");
    }

    [Fact]
    public void Non_idempotent_confirmed_still_renders_success_bar() {
        var (cut, push) = RenderWrapperWithLiveService();

        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, FakeTime.GetUtcNow(), idempotencyResolved: false));

        cut.Markup.ShouldContain("fc-confirmed");
        cut.Markup.ShouldNotContain("fc-idempotent");
    }

    private CommandLifecycleTransition IdempotentConfirmed()
        => TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, FakeTime.GetUtcNow(), idempotencyResolved: true);
}
