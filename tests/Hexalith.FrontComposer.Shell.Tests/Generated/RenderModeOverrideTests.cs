using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class RenderModeOverrideTests : CommandRendererTestBase {
    [Fact]
    public async Task Renderer_DefaultMode_MatchesDensityForZeroFields() {
        await InitializeStoreAsync();

        IRenderedComponent<ZeroFieldInlineCommandRenderer> cut = Render<ZeroFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("fluent-button", Case.Insensitive);
            cut.Markup.ShouldNotContain("fc-expand-in-row", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_DefaultMode_MatchesDensityForCompactCommand() {
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
        await InitializeStoreAsync();

        IRenderedComponent<TwoFieldCompactCommandRenderer> cut = Render<TwoFieldCompactCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("fc-expand-in-row", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_DefaultMode_MatchesDensityForFullPageCommand() {
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("breadcrumb", Case.Insensitive);
            cut.Markup.ShouldNotContain("fc-expand-in-row", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_RenderModeOverride_LogsHFC1015OnMismatch() {
        TestLogger<FiveFieldFullPageCommandRenderer> logger = new();
        _ = Services.AddSingleton<ILogger<FiveFieldFullPageCommandRenderer>>(logger);
        await InitializeStoreAsync();

        _ = Render<FiveFieldFullPageCommandRenderer>(parameters =>
            parameters.Add(p => p.RenderMode, (CommandRenderMode?)CommandRenderMode.Inline));

        logger.WarningMessages.ShouldContain(message => message.Contains("HFC1015", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Renderer_RenderModeOverride_DoesNotLogHFC1015ForCompatibleFullPageOverride() {
        TestLogger<TwoFieldCompactCommandRenderer> logger = new();
        _ = Services.AddSingleton<ILogger<TwoFieldCompactCommandRenderer>>(logger);
        await InitializeStoreAsync();

        _ = Render<TwoFieldCompactCommandRenderer>(parameters =>
            parameters.Add(p => p.RenderMode, (CommandRenderMode?)CommandRenderMode.FullPage));

        logger.WarningMessages.ShouldNotContain(message => message.Contains("HFC1015", StringComparison.Ordinal));
    }

    private sealed class TestLogger<T> : ILogger<T> {
        public List<string> WarningMessages { get; } = [];

        IDisposable? ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            if (logLevel >= LogLevel.Warning) {
                WarningMessages.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
