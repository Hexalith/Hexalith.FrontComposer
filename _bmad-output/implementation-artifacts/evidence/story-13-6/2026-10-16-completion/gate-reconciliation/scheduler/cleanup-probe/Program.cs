using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
string assemblyDirectory = "/home/administrator/projects/hexalith/frontcomposer/tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0";
AssemblyLoadContext.Default.Resolving += (_, name) => {
    string path = Path.Combine(assemblyDirectory, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
Assembly shell = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(assemblyDirectory, "Hexalith.FrontComposer.Shell.dll"));
MethodInfo observe = shell.GetType("Hexalith.FrontComposer.Shell.Infrastructure.EventStore.EventStoreCommandClient")!
    .GetMethod("ObserveAcceptedCleanup", BindingFlags.Static | BindingFlags.NonPublic)!;
using CancellationTokenSource parent = new();
CancellationTokenSource child = CancellationTokenSource.CreateLinkedTokenSource(parent.Token);
using ManualResetEventSlim release = new(false);
TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
using CancellationTokenRegistration registration = child.Token.Register(() => { entered.TrySetResult(); release.Wait(); });
Task cancellation = parent.CancelAsync();
await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
Task invocation = Task.Run(() => observe.Invoke(null, [child, Task.CompletedTask, TimeProvider.System]));
bool returnedWhileCallbackHeld = false;
try {
    await invocation.WaitAsync(TimeSpan.FromMilliseconds(500));
    returnedWhileCallbackHeld = true;
}
catch (TimeoutException) { }
finally {
    release.Set();
    await cancellation.WaitAsync(TimeSpan.FromSeconds(5));
    await invocation.WaitAsync(TimeSpan.FromSeconds(5));
}
Console.WriteLine(JsonSerializer.Serialize(new { returnedWhileCallbackHeld, parentCallbackCompleted = cancellation.IsCompleted }));
return returnedWhileCallbackHeld ? 0 : 1;
