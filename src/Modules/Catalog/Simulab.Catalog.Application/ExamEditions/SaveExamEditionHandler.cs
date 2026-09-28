using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.ExamEditions;

/// <summary>
/// Creates an edition (UC2) or replaces the fields of an existing one (UC3). The shape rules belong to the
/// entity (BR15); the ones that need the table belong here: the exam and the board must exist (BR3) and the
/// year, position and board must be free inside the exam (BR10). The unique index is the real guarantee.
/// </summary>
public sealed class SaveExamEditionHandler(IExamEditionStore store, IExamEditionQueries queries, TimeProvider clock)
{
    public async Task<Result<ExamEditionResponse>> HandleAsync(
        Guid examId,
        Guid? id,
        SaveExamEditionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await store.ExamExistsAsync(examId, cancellationToken))
        {
            return Failure(CatalogErrorCodes.ExamNotFound, ErrorKind.NotFound);
        }

        var existing = id is { } editionId ? await store.FindAsync(examId, editionId, cancellationToken) : null;
        if (id is not null && existing is null)
        {
            return Failure(CatalogErrorCodes.ExamEditionNotFound, ErrorKind.NotFound);
        }

        // BR9: a blank status is Draft only for a new edition. On an update it is refused, so a caller that
        // leaves the field out cannot silently unpublish a live edition.
        var status = request.ParseStatus();
        if (status is null && (id is not null || !string.IsNullOrWhiteSpace(request.Status)))
        {
            return Failure(CatalogErrorCodes.ExamEditionStatusInvalid, ErrorKind.Validation);
        }

        // BR4: an edital published in December is for next year's paper.
        var maxNoticeYear = clock.GetUtcNow().Year + 1;
        var chosenStatus = status ?? ExamEditionStatus.Draft;

        var candidate = ExamEdition.Create(
            examId,
            request.OrganizerId,
            request.NoticeYear,
            request.Position,
            request.NoticeReference,
            request.NoticeUrl,
            request.AppliedOn,
            chosenStatus,
            maxNoticeYear);

        if (candidate.IsFailure)
        {
            return Result.Failure<ExamEditionResponse>(candidate.Error!);
        }

        var organizerId = candidate.Value.OrganizerId;
        if (!await store.OrganizerExistsAsync(organizerId, cancellationToken))
        {
            return Failure(CatalogErrorCodes.OrganizerNotFound, ErrorKind.NotFound);
        }

        var duplicate = await store.IsDuplicateAsync(
            examId,
            candidate.Value.NoticeYear,
            candidate.Value.NormalizedPosition,
            organizerId,
            id,
            cancellationToken);

        if (duplicate)
        {
            return Failure(CatalogErrorCodes.ExamEditionDuplicate, ErrorKind.Conflict);
        }

        var edition = existing ?? candidate.Value;
        if (existing is null)
        {
            store.Add(edition);
        }
        else
        {
            // The same validation ran on the candidate a moment ago, so this one cannot fail.
            edition.Update(
                request.OrganizerId,
                request.NoticeYear,
                request.Position,
                request.NoticeReference,
                request.NoticeUrl,
                request.AppliedOn,
                chosenStatus,
                maxNoticeYear);
        }

        var refused = await store.TrySaveChangesAsync(cancellationToken);
        if (refused is not null)
        {
            return Result.Failure<ExamEditionResponse>(refused);
        }

        // The response carries the board's name, which only the read side knows how to join.
        var saved = await queries.FindAsync(examId, edition.Id, cancellationToken);

        return saved is null
            ? Failure(CatalogErrorCodes.ExamEditionNotFound, ErrorKind.NotFound)
            : Result.Success(saved);
    }

    private static Result<ExamEditionResponse> Failure(string code, ErrorKind kind) =>
        Result.Failure<ExamEditionResponse>(new Error(code, kind));
}
