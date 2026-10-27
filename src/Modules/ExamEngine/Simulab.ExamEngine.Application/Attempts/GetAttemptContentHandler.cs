using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>
/// Reads the content of an attempt for its owner only (F-169, BR2, BR11): anyone else, an <c>Admin</c> included, gets the
/// answer of an id that does not exist.
/// </summary>
public sealed class GetAttemptContentHandler(IAttemptStore store, IAttemptSource source)
{
    public Task<Result<AttemptContentResponse>> HandleAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
