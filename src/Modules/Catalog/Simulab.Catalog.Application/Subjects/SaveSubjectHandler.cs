using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Subjects;

/// <summary>
/// Creates a subject or replaces the fields of an existing one (F-79, UC2, UC3), together with the other names
/// it goes by (F-77, BR4). The shape rules belong to the entities; the ones that need the tables — the area
/// exists, the name and the other names are free — belong here. The unique indexes are the real guarantee; the
/// checks turn the common case into a clean answer. The checks run in this order and the first failure answers:
/// name shape, other names' shape, other name equal to the name, area, name taken, other name taken.
/// </summary>
public sealed class SaveSubjectHandler(ISubjectStore store, ISubjectQueries queries)
{
    public async Task<Result<SubjectResponse>> HandleAsync(
        Guid? id,
        SaveSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = id is { } existingId ? await store.FindAsync(existingId, cancellationToken) : null;
        if (id is not null && existing is null)
        {
            return Result.Failure<SubjectResponse>(new Error(CatalogErrorCodes.SubjectNotFound, ErrorKind.NotFound));
        }

        // Shape first, then the area and the name against the tables.
        var candidate = Subject.Create(request.Name, request.AreaId);
        if (candidate.IsFailure)
        {
            return Result.Failure<SubjectResponse>(candidate.Error!);
        }

        var subjectId = existing?.Id ?? candidate.Value.Id;
        IReadOnlyList<SubjectAlias> stored = existing is null
            ? []
            : await store.ListAliasesAsync(existing.Id, cancellationToken);

        // The set the subject will hold: the request's, or the stored one when the request says nothing.
        var wanted = WantedSet(request.Aliases, stored);
        if (request.Aliases is not null)
        {
            foreach (var text in wanted)
            {
                var shape = SubjectAlias.Create(subjectId, text.Name);
                if (shape.IsFailure)
                {
                    return Result.Failure<SubjectResponse>(shape.Error!);
                }
            }
        }

        var normalizedName = candidate.Value.NormalizedName;
        if (wanted.Any(text => AliasRules.IsSameAsName(text, normalizedName)))
        {
            return Result.Failure<SubjectResponse>(
                new Error(CatalogErrorCodes.SubjectAliasSameAsName, ErrorKind.Validation));
        }

        if (request.AreaId is { } areaId && !await store.AreaExistsAsync(areaId, cancellationToken))
        {
            return Result.Failure<SubjectResponse>(new Error(CatalogErrorCodes.SubjectAreaInvalid, ErrorKind.Validation));
        }

        // A name is taken by another subject's name (deleted ones included) or by another subject's live alias.
        if (await store.NameIsTakenAsync(normalizedName, id, cancellationToken)
            || await store.AliasIsTakenAsync(normalizedName, id, cancellationToken))
        {
            return Result.Failure<SubjectResponse>(new Error(CatalogErrorCodes.SubjectNameTaken, ErrorKind.Conflict));
        }

        foreach (var text in wanted)
        {
            if (await store.NameIsTakenAsync(text.NormalizedName, id, cancellationToken)
                || await store.AliasIsTakenAsync(text.NormalizedName, id, cancellationToken))
            {
                return Result.Failure<SubjectResponse>(
                    new Error(CatalogErrorCodes.SubjectAliasTaken, ErrorKind.Conflict));
            }
        }

        var subject = existing ?? candidate.Value;
        if (existing is null)
        {
            store.Add(subject);
        }
        else
        {
            // The same validation ran on the candidate a moment ago, so this one cannot fail.
            subject.Update(request.Name, request.AreaId);
        }

        if (request.Aliases is not null)
        {
            ReplaceAliases(subject.Id, stored, wanted);
        }

        // Another writer may have committed the same name since the checks above (B-14).
        var refused = await store.TrySaveChangesAsync(cancellationToken);
        if (refused is not null)
        {
            return Result.Failure<SubjectResponse>(refused);
        }

        // The answer carries the area code, the topic count and the other names, which the entity does not hold.
        var saved = await queries.FindAsync(subject.Id, cancellationToken);

        return Result.Success(saved ?? new SubjectResponse(subject.Id, subject.Name, subject.AreaId, null, 0)
        {
            Aliases = [.. wanted.OrderBy(text => text.NormalizedName, StringComparer.Ordinal).Select(text => text.Name)],
        });
    }

    private static IReadOnlyList<AliasText> WantedSet(IReadOnlyList<string>? requested, IReadOnlyList<SubjectAlias> stored) =>
        requested is null
            ? [.. stored.Select(alias => new AliasText(alias.Name, alias.NormalizedName))]
            : AliasRules.Distinct(requested);

    // A diff by normalized form (AliasRules.Diff), never "remove all and add": an alias that stays keeps its row.
    private void ReplaceAliases(Guid subjectId, IReadOnlyList<SubjectAlias> stored, IReadOnlyList<AliasText> wanted)
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
            store.AddAlias(SubjectAlias.Create(subjectId, text.Name).Value);
        }
    }
}
