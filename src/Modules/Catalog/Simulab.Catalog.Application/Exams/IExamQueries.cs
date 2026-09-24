using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Application.Exams;

/// <summary>The read side of the exam list and of the form page (F-34, UC1 and UC3).</summary>
public interface IExamQueries
{
    Task<ExamPageResponse> ListAsync(ExamListQuery query, CancellationToken cancellationToken);

    /// <summary>One exam with its issuing authority's name, or null when it does not exist or was deleted.</summary>
    Task<ExamResponse?> FindAsync(Guid id, CancellationToken cancellationToken);
}
