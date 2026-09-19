using Microsoft.AspNetCore.Http;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Api;

/// <summary>
/// The locale a request is answered in. The API has no culture middleware: the Web sends
/// <c>Accept-Language</c> with the culture the visitor is browsing in, and anything else falls back
/// to English. It decides which legal document is shown and which language an email is written in.
/// </summary>
public static class RequestLocale
{
    public const string Default = SupportedLanguages.Default;

    /// <summary>The locales the app ships in (F-8: one list, in the contracts, shared with the Web).</summary>
    public static IReadOnlyList<string> Supported => SupportedLanguages.All;

    public static string From(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        foreach (var language in request.GetTypedHeaders().AcceptLanguage.OrderByDescending(header => header.Quality ?? 1))
        {
            var match = Match(language.Value.Value);
            if (match is not null)
            {
                return match;
            }
        }

        return Default;
    }

    private static string? Match(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var exact = Supported.FirstOrDefault(locale => string.Equals(locale, value, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact;
        }

        // "pt" alone, or a variant the app does not ship: answer in the first locale of that language.
        var prefix = value.Split('-')[0];
        return Supported.FirstOrDefault(locale => locale.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase)
            || string.Equals(locale, prefix, StringComparison.OrdinalIgnoreCase));
    }
}
