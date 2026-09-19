using System.Globalization;

namespace Simulab.Identity.Infrastructure.Email;

/// <summary>
/// Switches <see cref="CultureInfo.CurrentUICulture"/> to the recipient's language while an email is
/// rendered: <c>IStringLocalizer</c> reads the current culture, and an email leaves the request, so the
/// visitor's culture is the wrong one (F-4 BR13, F-7 BR4).
/// </summary>
internal sealed class EmailCulture : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentUICulture;

    private EmailCulture(CultureInfo culture)
    {
        Culture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public CultureInfo Culture { get; }

    public static EmailCulture Use(string locale) => new(ToCulture(locale));

    public void Dispose() => CultureInfo.CurrentUICulture = _previous;

    private static CultureInfo ToCulture(string locale)
    {
        try
        {
            return CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("en");
        }
    }
}
