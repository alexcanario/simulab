using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Organizers;

/// <summary>
/// Creates an organizer (UC2) or replaces the fields of an existing one (UC3). The shape rules belong
/// to the entity (BR13); the two that need the table — name and acronym already taken (BR9) — belong
/// here. The unique indexes are the real guarantee; these checks are what turns the common case into a
/// clean 409 instead of a database error.
/// </summary>
public sealed class SaveOrganizerHandler(IOrganizerStore store)
{
    public async Task<Result<OrganizerResponse>> HandleAsync(
        Guid? id,
        SaveOrganizerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = id is { } existingId ? await store.FindAsync(existingId, cancellationToken) : null;
        if (id is not null && existing is null)
        {
            return Result.Failure<OrganizerResponse>(new Error(CatalogErrorCodes.OrganizerNotFound, ErrorKind.NotFound));
        }

        // Shape first, so a blank name is answered as a blank name even when the acronym is also taken.
        var candidate = Organizer.Create(request.Name, request.Acronym, request.Kind, request.Description, request.Website);
        if (candidate.IsFailure)
        {
            return Result.Failure<OrganizerResponse>(candidate.Error!);
        }

        var taken = await TakenAsync(candidate.Value, id, cancellationToken);
        if (taken is not null)
        {
            return Result.Failure<OrganizerResponse>(taken);
        }

        var organizer = existing ?? candidate.Value;
        if (existing is null)
        {
            store.Add(organizer);
        }
        else
        {
            // The same validation ran on the candidate a moment ago, so this one cannot fail.
            organizer.Update(request.Name, request.Acronym, request.Kind, request.Description, request.Website);
        }

        await store.SaveChangesAsync(cancellationToken);

        return Result.Success(new OrganizerResponse(
            organizer.Id,
            organizer.Name,
            organizer.Acronym,
            organizer.Kind,
            organizer.Description,
            organizer.Website));
    }

    // The comparison runs over the same normalized form the unique index uses, so this answer and
    // PostgreSQL's never disagree (BR9).
    private async Task<Error?> TakenAsync(Organizer candidate, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await store.NameIsTakenAsync(candidate.NormalizedName, exceptId, cancellationToken))
        {
            return new Error(CatalogErrorCodes.OrganizerNameTaken, ErrorKind.Conflict);
        }

        return await store.AcronymIsTakenAsync(candidate.NormalizedAcronym, exceptId, cancellationToken)
            ? new Error(CatalogErrorCodes.OrganizerAcronymTaken, ErrorKind.Conflict)
            : null;
    }
}
