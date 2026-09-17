using Microsoft.Extensions.Localization;
using Simulab.Web.Resources;

namespace Simulab.Web.Components.Ui;

/// <summary>
/// Turns an API error code into text (rule: ui). The code is the resource key.
/// Unknown codes show a generic message; a raw code is never shown.
/// </summary>
public sealed class ErrorText(IStringLocalizer<SharedResources> l)
{
    public const string UnexpectedCode = "common.unexpected_error";

    public string For(string? code)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            var text = l[code];
            if (!text.ResourceNotFound)
                return text;
        }

        return l[UnexpectedCode];
    }
}
