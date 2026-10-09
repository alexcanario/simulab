using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain;

/// <summary>
/// The rules of a notice subject's mapping that need no database (F-75): the shape of each entry and the
/// size of the list (BR1), the repeated entry stored once (BR5), and the overlap of a topic with its own whole
/// subject (BR4). Whether the targets exist (BR3) needs the tables, so the handler asks the store for it
/// between <see cref="Normalize"/> and <see cref="CheckOverlap"/>. Pure, in the style of
/// <see cref="NoticeSubjectOrder"/>.
/// </summary>
public static class NoticeSubjectMappingRules
{
    /// <summary>
    /// A request longer than twice the limit is refused without being read: the ceiling exists to stop a runaway
    /// request, and duplicates are only collapsed after the shape check.
    /// </summary>
    private const int RawLimit = CatalogLimits.NoticeSubjectMappingMax * 2;

    /// <summary>
    /// Checks every entry has exactly one id (BR1), collapses repeats (BR5, order of first appearance kept) and
    /// refuses more than <see cref="CatalogLimits.NoticeSubjectMappingMax"/> distinct entries (BR1). Null is empty.
    /// </summary>
    public static Result<IReadOnlyList<MappingEntry>> Normalize(IReadOnlyList<NoticeSubjectMappingRequest>? requested)
    {
        if (requested is null or { Count: 0 })
        {
            return Result.Success<IReadOnlyList<MappingEntry>>([]);
        }

        if (requested.Count > RawLimit)
        {
            return Failure(CatalogErrorCodes.NoticeSubjectMappingTooMany);
        }

        var distinct = new List<MappingEntry>(requested.Count);
        var seen = new HashSet<MappingEntry>();
        foreach (var item in requested)
        {
            if (item is null
                || (item.SubjectId is null) == (item.TopicId is null)
                || item.SubjectId == Guid.Empty
                || item.TopicId == Guid.Empty)
            {
                return Failure(CatalogErrorCodes.NoticeSubjectMappingInvalid);
            }

            var entry = new MappingEntry(item.SubjectId, item.TopicId);
            if (seen.Add(entry))
            {
                distinct.Add(entry);
            }
        }

        return distinct.Count > CatalogLimits.NoticeSubjectMappingMax
            ? Failure(CatalogErrorCodes.NoticeSubjectMappingTooMany)
            : Result.Success<IReadOnlyList<MappingEntry>>(distinct);
    }

    /// <summary>
    /// Refuses a topic mapped together with its own whole subject (BR4), unless the saved mapping already held
    /// both: "there was no such overlap before the save". That keeps an overlap a later topic move created (F-79
    /// BR10) from blocking the save of the row. <paramref name="topicSubjects"/> maps each requested topic to its
    /// current subject.
    /// </summary>
    public static Result CheckOverlap(
        IReadOnlyList<MappingEntry> entries,
        IReadOnlyDictionary<Guid, Guid> topicSubjects,
        IReadOnlyList<MappingEntry> saved)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(topicSubjects);
        ArgumentNullException.ThrowIfNull(saved);

        var wholeSubjects = entries.Where(entry => entry.SubjectId is not null).Select(entry => entry.SubjectId!.Value)
            .ToHashSet();
        var savedWhole = saved.Where(entry => entry.SubjectId is not null).Select(entry => entry.SubjectId!.Value)
            .ToHashSet();
        var savedTopics = saved.Where(entry => entry.TopicId is not null).Select(entry => entry.TopicId!.Value)
            .ToHashSet();

        foreach (var entry in entries.Where(entry => entry.TopicId is not null))
        {
            var topicId = entry.TopicId!.Value;
            if (!topicSubjects.TryGetValue(topicId, out var subjectId) || !wholeSubjects.Contains(subjectId))
            {
                continue;
            }

            var heldBefore = savedWhole.Contains(subjectId) && savedTopics.Contains(topicId);
            if (!heldBefore)
            {
                return Result.Failure(new Error(CatalogErrorCodes.NoticeSubjectMappingOverlap, ErrorKind.Validation));
            }
        }

        return Result.Success();
    }

    private static Result<IReadOnlyList<MappingEntry>> Failure(string code) =>
        Result.Failure<IReadOnlyList<MappingEntry>>(new Error(code, ErrorKind.Validation));
}
