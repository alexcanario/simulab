using Simulab.Catalog.Application.Subjects;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Topics;

/// <summary>
/// Adds a topic to a subject or replaces the fields of an existing one, which may move it to another subject
/// (F-79, UC6, UC7, BR10). The subject must exist and the name must be free inside it; the unique index is the
/// real guarantee.
/// </summary>
public sealed class SaveTopicHandler(ITopicStore store, ISubjectStore subjects)
{
    /// <param name="subjectId">The subject from the route on a create; ignored on an update.</param>
    /// <param name="id">The topic to update, or null to create one.</param>
    /// <param name="request">The name and, on an update, the subject to move to.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    public async Task<Result<TopicResponse>> HandleAsync(
        Guid? subjectId,
        Guid? id,
        SaveTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = id is { } existingId ? await store.FindAsync(existingId, cancellationToken) : null;
        if (id is not null && existing is null)
        {
            return Result.Failure<TopicResponse>(new Error(CatalogErrorCodes.TopicNotFound, ErrorKind.NotFound));
        }

        // A create sits under the route's subject; an update keeps its subject unless the body names another.
        var targetSubjectId = existing is null
            ? subjectId ?? Guid.Empty
            : request.SubjectId ?? existing.SubjectId;

        var candidate = Topic.Create(targetSubjectId, request.Name);
        if (candidate.IsFailure)
        {
            return Result.Failure<TopicResponse>(candidate.Error!);
        }

        if (await subjects.FindAsync(targetSubjectId, cancellationToken) is null)
        {
            return Result.Failure<TopicResponse>(new Error(CatalogErrorCodes.SubjectNotFound, ErrorKind.NotFound));
        }

        if (await store.NameIsTakenAsync(targetSubjectId, candidate.Value.NormalizedName, id, cancellationToken))
        {
            return Result.Failure<TopicResponse>(new Error(CatalogErrorCodes.TopicNameTaken, ErrorKind.Conflict));
        }

        var topic = existing ?? candidate.Value;
        if (existing is null)
        {
            store.Add(topic);
        }
        else
        {
            // The same validation ran on the candidate a moment ago, so this one cannot fail.
            topic.Update(targetSubjectId, request.Name);
        }

        var refused = await store.TrySaveChangesAsync(cancellationToken);
        if (refused is not null)
        {
            return Result.Failure<TopicResponse>(refused);
        }

        // A topic mapped by a live notice subject is in use: the answer says so, as the list does (BR10).
        return Result.Success(new TopicResponse(
            topic.Id,
            topic.SubjectId,
            topic.Name,
            await store.IsMappedAsync(topic.Id, cancellationToken)));
    }
}
