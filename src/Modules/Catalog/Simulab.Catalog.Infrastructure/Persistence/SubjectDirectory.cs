using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Text;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// What another module may ask about subjects and their topics (B-28): live rows only, over the catalog's own
/// context and its normalized names, so the search ignores case and accents the way the subject list does.
/// The global soft-delete filters leave out a deleted subject and a deleted topic.
/// </summary>
public sealed class SubjectDirectory(CatalogModuleDbContext context) : ISubjectDirectory
{
    public async Task<IReadOnlyList<SubjectDirectoryEntry>> SearchAsync(
        string? search,
        int take,
        CancellationToken cancellationToken)
    {
        var text = ComparableText.Normalize(search);
        var subjects = context.Subjects.AsNoTracking();

        if (text.Length > 0)
        {
            subjects = subjects.Where(subject => subject.NormalizedName.Contains(text));
        }

        return await subjects
            .OrderBy(subject => subject.NormalizedName)
            .ThenBy(subject => subject.Id)
            .Take(Math.Max(take, 0))
            .Select(subject => new SubjectDirectoryEntry(subject.Id, subject.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TopicDirectoryEntry>?> ListTopicsAsync(
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        var subjectName = await context.Subjects
            .AsNoTracking()
            .Where(subject => subject.Id == subjectId)
            .Select(subject => subject.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (subjectName is null)
        {
            return null;
        }

        return await context.Topics
            .AsNoTracking()
            .Where(topic => topic.SubjectId == subjectId)
            .OrderBy(topic => topic.NormalizedName)
            .ThenBy(topic => topic.Id)
            .Select(topic => new TopicDirectoryEntry(topic.Id, topic.Name, subjectId, subjectName))
            .ToListAsync(cancellationToken);
    }
}
