using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public class LastUsedSubscriberEmitterTests {
    private static CommandFluxorModel BuildFluxor(
        string typeName = "IncrementCommand",
        string @namespace = "Counter.Domain") {
        return new CommandFluxorModel(
            typeName,
            @namespace,
            typeName + "LifecycleState",
            typeName + "LifecycleFeature",
            typeName + "Actions",
            typeName + "Reducers",
            @namespace + "." + typeName,
            @namespace + "." + typeName + "LifecycleState");
    }

    [Fact]
    public void Emit_GeneratesSubscriberPerCommand() {
        string source = LastUsedSubscriberEmitter.Emit(BuildFluxor());

        source.ShouldContain("public sealed class IncrementCommandLastUsedSubscriber : IDisposable");
        source.ShouldContain("private readonly TimeProvider _timeProvider;");
        source.ShouldContain("TimeProvider? timeProvider = null");
        source.ShouldContain("_timeProvider = timeProvider ?? TimeProvider.System;");
        source.ShouldContain("private async Task RecordConfirmedAsync(Counter.Domain.IncrementCommand command)");
        source.ShouldContain("await _recorder.RecordAsync<Counter.Domain.IncrementCommand>(command, _cts.Token).ConfigureAwait(false);");
        source.ShouldContain("services.AddScoped<IncrementCommandLastUsedSubscriber>()");
    }

    [Fact]
    public Task Emit_MatchesVerifiedSnapshot() {
        string source = LastUsedSubscriberEmitter.Emit(BuildFluxor());
        return Verify(source);
    }
}
