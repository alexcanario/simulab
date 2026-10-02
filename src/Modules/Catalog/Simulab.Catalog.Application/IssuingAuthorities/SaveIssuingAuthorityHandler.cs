using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.IssuingAuthorities;

/// <summary>
/// Creates an issuing authority or replaces the fields of an existing one (F-34 BR18, v2). The shape rules
/// belong to the entity; the one that needs the table — name already taken — belongs here. The
/// unique indexes are the real guarantee; these checks turn the common case into a clean 409.
/// </summary>
public sealed class SaveIssuingAuthorityHandler(IIssuingAuthorityStore store)
{
    public async Task<Result<IssuingAuthorityResponse>> HandleAsync(
        Guid? id,
        SaveIssuingAuthorityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = id is { } existingId ? await store.FindAsync(existingId, cancellationToken) : null;
        if (id is not null && existing is null)
        {
            return Result.Failure<IssuingAuthorityResponse>(
                new Error(CatalogErrorCodes.IssuingAuthorityNotFound, ErrorKind.NotFound));
        }

        // Shape first, then the name against the table.
        var candidate = IssuingAuthority.Create(request.Name, request.Description, request.Website);
        if (candidate.IsFailure)
        {
            return Result.Failure<IssuingAuthorityResponse>(candidate.Error!);
        }

        if (await store.NameIsTakenAsync(candidate.Value.NormalizedName, id, cancellationToken))
        {
            return Result.Failure<IssuingAuthorityResponse>(
                new Error(CatalogErrorCodes.IssuingAuthorityNameTaken, ErrorKind.Conflict));
        }

        var authority = existing ?? candidate.Value;
        if (existing is null)
        {
            store.Add(authority);
        }
        else
        {
            // The same validation ran on the candidate a moment ago, so this one cannot fail.
            authority.Update(request.Name, request.Description, request.Website);
        }

        // Another writer may have committed the same name since the check above (B-14).
        var refused = await store.TrySaveChangesAsync(cancellationToken);
        if (refused is not null)
        {
            return Result.Failure<IssuingAuthorityResponse>(refused);
        }

        return Result.Success(new IssuingAuthorityResponse(
            authority.Id,
            authority.Name,
            authority.Description,
            authority.Website));
    }
}
