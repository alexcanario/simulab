using Microsoft.JSInterop;

namespace Simulab.Web.Components.Layout;

/// <summary>Writes a preference cookie through wwwroot/js/preferences.js.</summary>
public sealed class PreferenceWriter(IJSRuntime js)
{
    public const string SetFunction = "simulabPreferences.set";

    public ValueTask WriteAsync(string name, string value) =>
        js.InvokeVoidAsync(SetFunction, name, value, PreferenceCookies.MaxAgeSeconds);
}
