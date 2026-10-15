using Simulab.Catalog.Application.Subjects;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Topics;

/// <summary>
/// Adds a topic to a subject or replaces the fields of an existing one, which may move it to another subject
/// (F-79, UC6, UC7, BR10), together with the other names it goes by (F-77, BR4). The subject must exist and the
/// name and the other names must be free inside it; the unique indexes are the real guarantee. A move checks the
/// topic's stored other names against the target subject too (F-77, BR9). The checks run in this order and the
/// first failure answers: name shape, other names' shape, other name equal to the name, subject, name taken,
/// other name taken.
/// </summary>
public sealed class SaveTopicHandler(ITopicStore store, ISubjectStore subjects, IEnumerable<ITopicUsageCheck> usageChecks)
{
    /// <param name="subjectId">The subject from the route on a create; ignored on an update.</param>
    /// <param name="id">The topic to update, or null to create one.</param>
    /// <param name="request">The name, the other names and, on an update, the subject to move to.</param>
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

        var topicId = existing?.Id ?? candidate.Value.Id;
        IReadOnlyList<TopicAlias> stored = existing is null
            ? []
            : await store.ListAliasesAsync(existing.Id, cancellationToken);

        // The set the topic will hold: the request's, or the stored one when the request says nothing.
        IReadOnlyList<AliasText> wanted = request.Aliases is null
            ? [.. stored.Select(alias => new AliasText(alias.Name, alias.NormalizedName))]
            : AliasRules.Distinct(request.Aliases);
        if (request.Aliases is not null)
        {
            foreach (var text in wanted)
            {
                var shape = TopicAlias.Create(topicId, text.Name);
                if (shape.IsFailure)
                {
                    return Result.Failure<TopicResponse>(shape.Error!);
                }
            }
        }

        var normalizedName = candidate.Value.NormalizedName;
        if (wanted.Any(text => AliasRules.IsSameAsName(text, normalizedName)))
        {
            return Result.Failure<TopicResponse>(
                new Error(CatalogErrorCodes.TopicAliasSameAsName, ErrorKind.Validation));
        }

        if (await subjects.FindAsync(targetSubjectId, cancellationToken) is null)
        {
            return Result.Failure<TopicResponse>(new Error(CatalogErrorCodes.SubjectNotFound, ErrorKind.NotFound));
        }

        // A name is taken by a sibling's name (deleted ones included) or by a sibling's live other name.
        if (await store.NameIsTakenAsync(targetSubjectId, normalizedName, id, cancellationToken)
            || await store.AliasIsTakenAsync(targetSubjectId, normalizedName, id, cancellationToken))
        {
            return Result.Failure<TopicResponse>(new Error(CatalogErrorCodes.TopicNameTaken, ErrorKind.Conflict));
        }

        foreach (var text in wanted)
        {
            if (await store.NameIsTakenAsync(targetSubjectId, text.NormalizedName, id, cancellationToken)
                || await store.AliasIsTakenAsync(targetSubjectId, text.NormalizedName, id, cancellationToken))
            {
                return Result.Failure<TopicResponse>(
                    new Error(CatalogErrorCodes.TopicAliasTaken, ErrorKind.Conflict));
            }
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

        if (request.Aliases is not null)
        {
            ReplaceAliases(topic.Id, stored, wanted);
        }

        var refused = await store.TrySaveChangesAsync(cancellationToken);
        if (refused is not null)
        {
            return Result.Failure<TopicResponse>(refused);
        }

        // A topic a live notice subject maps, or that another module says it uses, is in use: the answer says
        // so, as the list does (BR10; F-95 BR13).
        var inUse = await store.IsMappedAsync(topic.Id, cancellationToken)
            || await TopicUsageChecks.AnyAsync(usageChecks, topic.Id, cancellationToken);

        // Read back, so the answer lists the other names in the order the list does.
        var aliases = await store.ListAliasesAsync(topic.Id, cancellationToken);

        return Result.Success(new TopicResponse(topic.Id, topic.SubjectId, topic.Name, inUse)
        {
            Aliases = [.. aliases.Select(alias => alias.Name)],
        });
    }

    // A diff by normalized form (AliasRules.Diff), never "remove all and add": an alias that stays keeps its row.
    private void ReplaceAliases(Guid topicId, IReadOnlyList<TopicAlias> stored, IReadOnlyList<AliasText> wanted)
    {
        var change = AliasRules.Diff(stored, wanted);

        foreach (var alias in change.Remove)
        {
            store.RemoveAlias(alias);
        }

        foreach (var (alias, name) in change.Rename)
        {
            alias.Rename(name);
        }

        foreach (var text in change.Add)
        {
            store.AddAlias(TopicAlias.Create(topicId, text.Name).Value);
        }
    }
}
