using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Simulab.Web.Tests;

public sealed class FakeHostEnvironment(string name) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = name;
    public string ApplicationName { get; set; } = "Simulab.Web";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
