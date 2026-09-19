using Microsoft.AspNetCore.Localization;

namespace Simulab.Web.Localization;

/// <summary>
/// The culture cookie the request localization reads first. Written by the language switch and, for a
/// signed-in user, from the profile at sign-in and after a save (F-8 BR5).
/// </summary>
public static class CultureCookie
{
    /// <summary>Writes <paramref name="culture"/> when the app ships in it; anything else is ignored.</summary>
    public static void Write(HttpContext context, string? culture)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!SupportedCultures.IsSupported(culture))
        {
            return;
        }

        var canonical = SupportedCultures.All.First(supported => string.Equals(supported.Name, culture, StringComparison.OrdinalIgnoreCase)).Name;
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(canonical)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax, Secure = true });
    }
}
