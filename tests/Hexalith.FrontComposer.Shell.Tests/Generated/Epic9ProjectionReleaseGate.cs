namespace Hexalith.FrontComposer.Shell.Tests.Generated;

internal sealed class Epic9ProjectionReleaseGate
{
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitAsync() => _release.Task;

    public void Release() => _release.TrySetResult();
}
