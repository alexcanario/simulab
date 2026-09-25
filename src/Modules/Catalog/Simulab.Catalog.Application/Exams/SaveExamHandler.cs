using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Exams;

/// <summary>
/// Creates an exam (UC2) or replaces the fields of an existing one (UC3). The shape rules belong to the
/// entity (BR15); the two that need the table belong here: the issuing authority must exist (BR11) and the
/// name must be free inside it (BR10). The unique index is the real guarantee; the check is what turns the
/// common case into a clean 409 instead of a database error.
/// </summary>
public sealed class SaveExamHandler(IExamStore store, IExamQueries queries)
{
    public async Task<Result<ExamResponse>> HandleAsync(
        Guid? id,
        SaveExamRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = id is { } existingId ? await store.FindAsync(existingId, cancellationToken) : null;
        if (id is not null && existing is null)
        {
            return Result.Failure<ExamResponse>(new Error(CatalogErrorCodes.ExamNotFound, ErrorKind.NotFound));
        }

        // Shape first, so a blank name is answered as a blank name even when the name is also taken. A value
        // that is not one of the known names stays undefined and the entity answers with its own code (BR6,
        // BR7), the same code a caller gets for any other unreadable value.
        var assessmentType = request.ParseAssessmentType() ?? default;
        var scope = request.ParseScope() ?? default;
        var candidate = Exam.Create(
            request.IssuingAuthorityId,
            request.Name,
            assessmentType,
            scope,
            request.ScopeDetail,
            request.ContentLanguage);

        if (candidate.IsFailure)
        {
            return Result.Failure<ExamResponse>(candidate.Error!);
        }

        // BR11 (v2): a parent that is not in the catalog is the issuing authority's own 404.
        if (!await store.IssuingAuthorityExistsAsync(candidate.Value.IssuingAuthorityId, cancellationToken))
        {
            return Result.Failure<ExamResponse>(new Error(CatalogErrorCodes.IssuingAuthorityNotFound, ErrorKind.NotFound));
        }

        var taken = await store.NameIsTakenAsync(
            candidate.Value.IssuingAuthorityId,
            candidate.Value.NormalizedName,
            id,
            cancellationToken);

        if (taken)
        {
            return Result.Failure<ExamResponse>(new Error(CatalogErrorCodes.ExamNameTaken, ErrorKind.Conflict));
        }

        var exam = existing ?? candidate.Value;
        if (existing is null)
        {
            store.Add(exam);
        }
        else
        {
            // The same validation ran on the candidate a moment ago, so this one cannot fail.
            exam.Update(
                request.IssuingAuthorityId,
                request.Name,
                assessmentType,
                scope,
                request.ScopeDetail,
                request.ContentLanguage);
        }

        // Another writer may have committed the same name since the check above; the unique index then
        // refuses this row, and the answer is the same 409 (the B-14 lesson).
        var refused = await store.TrySaveChangesAsync(cancellationToken);
        if (refused is not null)
        {
            return Result.Failure<ExamResponse>(refused);
        }

        // The response carries the issuing authority's name, which only the read side knows how to join.
        var saved = await queries.FindAsync(exam.Id, cancellationToken);

        return saved is null
            ? Result.Failure<ExamResponse>(new Error(CatalogErrorCodes.ExamNotFound, ErrorKind.NotFound))
            : Result.Success(saved);
    }
}
