using Microsoft.JSInterop;

namespace Simulab.Web.Services;

/// <summary>
/// The time zone of the user's browser, read once per circuit through wwwroot/js/shell.js (rule: i18n, instants
/// are shown in the user's time zone). Until the browser answers, or when it cannot, times stay in UTC.
/// Call it from <c>OnAfterRenderAsync</c>: JavaScript is not reachable while a page prerenders.
/// </summary>
public sealed class UserTimeZone(IJSRuntime js)
{
    public const string TimeZoneFunction = "simulabShell.timeZone";

    private TimeZoneInfo? _zone;

    public TimeZoneInfo Current => _zone ?? TimeZoneInfo.Utc;

    public async Task<TimeZoneInfo> ResolveAsync()
    {
        if (_zone is not null)
        {
            return _zone;
        }

        try
        {
            var id = await js.InvokeAsync<string?>(TimeZoneFunction);
            _zone = id is not null && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var found) ? found : TimeZoneInfo.Utc;
        }
        catch (JSException)
        {
            _zone = TimeZoneInfo.Utc;
        }
        catch (JSDisconnectedException)
        {
            _zone = TimeZoneInfo.Utc;
        }

        return _zone;
    }

    public DateTimeOffset ToLocal(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Current);
}
