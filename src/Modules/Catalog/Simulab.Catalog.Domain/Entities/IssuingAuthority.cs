using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// The body that publishes a notice and defines the positions, the syllabus, the schedule and the rules of
/// an exam: a city hall, a state or federal government body, a ministry, a university, a company (F-34 BR18,
/// v2). It is the parent every exam hangs on, and it never applies a paper — that is the
/// <see cref="Organizer"/>, on the edition (F-35).
/// <para>
/// Same shape as <see cref="Organizer"/> without a kind, and for the same reasons: global data
/// (<see cref="TenantEntity.TenantId"/> null), rules that answer with a <see cref="Result"/> and never
/// throw, and uniqueness left to the table because it needs one.
/// </para>
/// </summary>
public sealed class IssuingAuthority : TenantEntity
{
    private IssuingAuthority()
    {
    }

    /// <summary>The body's full name, as it signs a notice.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The short name, stored uppercase.</summary>
    public string Acronym { get; private set; } = string.Empty;

    /// <summary>Free text, optional.</summary>
    public string? Description { get; private set; }

    /// <summary>The official site, an absolute http or https address, optional.</summary>
    public string? Website { get; private set; }

    /// <summary>The comparable form of <see cref="Name"/>: what the unique index and the search read.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="Acronym"/>, for the same reason.</summary>
    public string NormalizedAcronym { get; private set; } = string.Empty;

    public static Result<IssuingAuthority> Create(string? name, string? acronym, string? description, string? website)
    {
        var authority = new IssuingAuthority();
        var applied = authority.Apply(name, acronym, description, website);

        return applied.IsFailure
            ? Result.Failure<IssuingAuthority>(applied.Error!)
            : Result.Success(authority);
    }

    public Result Update(string? name, string? acronym, string? description, string? website) =>
        Apply(name, acronym, description, website);

    private Result Apply(string? name, string? acronym, string? description, string? website)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length < CatalogLimits.NameMinLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.IssuingAuthorityNameRequired, ErrorKind.Validation));
        }

        if (trimmedName.Length > CatalogLimits.IssuingAuthorityNameMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.IssuingAuthorityNameTooLong, ErrorKind.Validation));
        }

        var trimmedAcronym = acronym?.Trim().ToUpperInvariant() ?? string.Empty;
        if (trimmedAcronym.Length < CatalogLimits.NameMinLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.IssuingAuthorityAcronymRequired, ErrorKind.Validation));
        }

        if (trimmedAcronym.Length > CatalogLimits.IssuingAuthorityAcronymMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.IssuingAuthorityAcronymTooLong, ErrorKind.Validation));
        }

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (trimmedDescription?.Length > CatalogLimits.IssuingAuthorityDescriptionMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.IssuingAuthorityDescriptionTooLong, ErrorKind.Validation));
        }

        var trimmedWebsite = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
        if (trimmedWebsite is not null && !IsAbsoluteWebAddress(trimmedWebsite))
        {
            return Result.Failure(new Error(CatalogErrorCodes.IssuingAuthorityWebsiteInvalid, ErrorKind.Validation));
        }

        Name = trimmedName;
        Acronym = trimmedAcronym;
        Description = trimmedDescription;
        Website = trimmedWebsite;
        NormalizedName = CatalogText.Normalize(trimmedName);
        NormalizedAcronym = CatalogText.Normalize(trimmedAcronym);

        return Result.Success();
    }

    // An address the browser can open. A relative path, a mailto: or a 400-character URL is not one.
    private static bool IsAbsoluteWebAddress(string value) =>
        value.Length <= CatalogLimits.IssuingAuthorityWebsiteMaxLength
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
