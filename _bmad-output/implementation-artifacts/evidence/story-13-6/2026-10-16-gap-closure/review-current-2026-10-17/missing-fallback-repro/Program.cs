using System.Reflection;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
var speech = new SurfaceAnnouncementCoordinator(TimeProvider.System);
var messages = new List<string>();
using var subscription=speech.Subscribe("projection:orders:Orders", messages.Add);
var failed=new Runtime(true);var good=new Runtime(false);
foreach(var runtime in new[]{failed,good}) {
 var component=new FcExpandedRowHiddenBanner { ViewKey="orders:Orders", IsMissing=true, ResultIdentity="same" };
 foreach(var (name,value) in new (string,object)[]{("Announcements",speech),("JS",runtime),("Localizer",new Localizer())})
  typeof(FcExpandedRowHiddenBanner).GetProperty(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(component,value);
 typeof(FcExpandedRowHiddenBanner).GetMethod("OnParametersSet",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(component,null);
 await (Task)typeof(FcExpandedRowHiddenBanner).GetMethod("OnAfterRenderAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(component,new object[]{true})!;
}
Console.WriteLine($"spoken={messages.Count(m=>m.Length>0)}; second owner focus calls={good.FocusCalls}");
public class Runtime(bool fail):IJSRuntime,IJSObjectReference {
 public int FocusCalls;
 public ValueTask<T> InvokeAsync<T>(string identifier,object?[]? args)=>InvokeAsync<T>(identifier,default,args);
 public ValueTask<T> InvokeAsync<T>(string identifier,CancellationToken token,object?[]? args) {
  if(fail)return ValueTask.FromException<T>(new JSException("import failed"));
  if(identifier=="import")return ValueTask.FromResult((T)(object)this);
  if(identifier=="focusReplacementHeading"){FocusCalls++;return ValueTask.FromResult((T)(object)true);}
  return ValueTask.FromResult(default(T)!);
 }
 public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
}
public class Localizer:IStringLocalizer<FcShellResources> {
 public LocalizedString this[string name]=>new(name,"The expanded item is not in the current results.");
 public LocalizedString this[string name,params object[] args]=>this[name];
 public IEnumerable<LocalizedString> GetAllStrings(bool ancestors)=>Array.Empty<LocalizedString>();
}
