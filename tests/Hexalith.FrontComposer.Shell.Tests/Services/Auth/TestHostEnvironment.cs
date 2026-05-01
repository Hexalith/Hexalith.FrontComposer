using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

internal sealed class TestHostEnvironment(bool isDevelopment) : IHostEnvironment {
    public string EnvironmentName { get; set; } = isDevelopment ? Environments.Development : Environments.Production;
    public string ApplicationName { get; set; } = "Hexalith.FrontComposer.Tests";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
