using Microsoft.Extensions.Localization;
using Simulab.Catalog.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Components.Pages.Catalog;

/// <summary>
/// The reader's words for an organizer's kind (F-33, BR7). The enum name never reaches a screen: it is
/// a key, and the three languages give it their own text (rule: i18n).
/// </summary>
public static class OrganizerText
{
    public static string KindName(IStringLocalizer<SharedResources> l, OrganizerKind kind)
    {
        ArgumentNullException.ThrowIfNull(l);

        return l[$"Organizers.Kind.{kind}"];
    }
}
