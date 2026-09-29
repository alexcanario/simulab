using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Application.ExamEditions;

/// <summary>The read side of the editions section and of the edition page (F-35, UC1 and UC3).</summary>
public interface IExamEditionQueries
{
    /// <summary>
    /// Every edition of the exam, deleted ones excluded, newest notice year first, then position, then the
    /// board's name (BR14). Not paged: an exam has a handful.
    /// </summary>
    Task<IReadOnlyList<ExamEditionResponse>> ListAsync(Guid examId, CancellationToken cancellationToken);

    /// <summary>One edition of this exam with its board's name, or null when it does not exist or was deleted.</summary>
    Task<ExamEditionResponse?> FindAsync(Guid examId, Guid id, CancellationToken cancellationToken);
}
