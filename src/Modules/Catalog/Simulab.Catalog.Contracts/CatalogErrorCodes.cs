namespace Simulab.Catalog.Contracts;

/// <summary>
/// Every failure code this module can return (rule: api-contracts). A code is stable: it is the
/// resource key the UI translates, so it is never renamed or reused.
/// </summary>
public static class CatalogErrorCodes
{
    public const string OrganizerNotFound = "organizer.not_found";
    public const string OrganizerNameRequired = "organizer.name_required";
    public const string OrganizerNameTooLong = "organizer.name_too_long";
    public const string OrganizerNameTaken = "organizer.name_taken";
    public const string OrganizerAcronymRequired = "organizer.acronym_required";
    public const string OrganizerAcronymTooLong = "organizer.acronym_too_long";
    public const string OrganizerAcronymTaken = "organizer.acronym_taken";
    public const string OrganizerKindInvalid = "organizer.kind_invalid";
    public const string OrganizerDescriptionTooLong = "organizer.description_too_long";
    public const string OrganizerWebsiteInvalid = "organizer.website_invalid";
}
