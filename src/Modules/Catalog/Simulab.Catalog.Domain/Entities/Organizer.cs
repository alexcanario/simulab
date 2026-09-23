using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// Who runs an exam (F-33): an exam board, a certifying body or a university. Global data —
/// <see cref="TenantEntity.TenantId"/> is null in v1 (BR5) — and the parent every exam hangs on.
/// The rules here answer with a <see cref="Result"/> and never throw (BR13); uniqueness is not one of
/// them, because it needs the table (BR9, checked by the handler and by the unique index).
/// </summary>
public sealed class Organizer : TenantEntity
{
    private Organizer()
    {
    }

    /// <summary>The organizer's full name, as it signs a notice.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The short name, stored uppercase (BR8).</summary>
    public string Acronym { get; private set; } = string.Empty;

    /// <summary>What the organizer is (BR7).</summary>
    public OrganizerKind Kind { get; private set; }

    /// <summary>Free text, optional.</summary>
    public string? Description { get; private set; }

    /// <summary>The official site, an absolute http or https address, optional (BR10).</summary>
    public string? Website { get; private set; }

    /// <summary>The comparable form of <see cref="Name"/>: what the unique index and the search read (BR9, BR12).</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="Acronym"/>, for the same reason.</summary>
    public string NormalizedAcronym { get; private set; } = string.Empty;

    public static Result<Organizer> Create(string? name, string? acronym, OrganizerKind kind, string? description, string? website)
    {
        var organizer = new Organizer();
        var applied = organizer.Apply(name, acronym, kind, description, website);

        return applied.IsFailure
            ? Result.Failure<Organizer>(applied.Error!)
            : Result.Success(organizer);
    }

    public Result Update(string? name, string? acronym, OrganizerKind kind, string? description, string? website) =>
        Apply(name, acronym, kind, description, website);

    private Result Apply(string? name, string? acronym, OrganizerKind kind, string? description, string? website)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length < CatalogLimits.NameMinLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerNameRequired, ErrorKind.Validation));
        }

        if (trimmedName.Length > CatalogLimits.OrganizerNameMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerNameTooLong, ErrorKind.Validation));
        }

        var trimmedAcronym = acronym?.Trim().ToUpperInvariant() ?? string.Empty;
        if (trimmedAcronym.Length < CatalogLimits.NameMinLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerAcronymRequired, ErrorKind.Validation));
        }

        if (trimmedAcronym.Length > CatalogLimits.OrganizerAcronymMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerAcronymTooLong, ErrorKind.Validation));
        }

        if (!Enum.IsDefined(kind))
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerKindInvalid, ErrorKind.Validation));
        }

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (trimmedDescription?.Length > CatalogLimits.OrganizerDescriptionMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerDescriptionTooLong, ErrorKind.Validation));
        }

        var trimmedWebsite = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
        if (trimmedWebsite is not null && !IsAbsoluteWebAddress(trimmedWebsite))
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerWebsiteInvalid, ErrorKind.Validation));
        }

        Name = trimmedName;
        Acronym = trimmedAcronym;
        Kind = kind;
        Description = trimmedDescription;
        Website = trimmedWebsite;
        NormalizedName = CatalogText.Normalize(trimmedName);
        NormalizedAcronym = CatalogText.Normalize(trimmedAcronym);

        return Result.Success();
    }

    // BR10: an address the browser can open. A relative path, a mailto: or a 400-character URL is not one.
    private static bool IsAbsoluteWebAddress(string value) =>
        value.Length <= CatalogLimits.OrganizerWebsiteMaxLength
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
