namespace Simulab.Catalog.Contracts;

/// <summary>
/// The catalog's length limits: the width of their columns, read by the Api (the authority), the EF
/// mapping and the Web (comfort checks). One number per limit (F-33, BR6; rule from B-7 BR4).
/// </summary>
public static class CatalogLimits
{
    /// <summary>The organizer's full name. A longer one is refused, not cut.</summary>
    public const int OrganizerNameMaxLength = 150;

    /// <summary>The organizer's short name (CEBRASPE, FGV), stored uppercase.</summary>
    public const int OrganizerAcronymMaxLength = 20;

    /// <summary>Free text about the organizer.</summary>
    public const int OrganizerDescriptionMaxLength = 500;

    /// <summary>The organizer's official site, an absolute http or https address.</summary>
    public const int OrganizerWebsiteMaxLength = 300;

    /// <summary>The shortest a name or an acronym may be (BR8).</summary>
    public const int NameMinLength = 2;
}
