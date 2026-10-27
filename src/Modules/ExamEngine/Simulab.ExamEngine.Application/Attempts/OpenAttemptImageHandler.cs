using Simulab.SharedKernel.Results;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>
/// Opens a picture of an attempt for its owner only (F-169, BR11, BR12): only a picture held by a question version the
/// attempt fixed opens; any other id answers as an attempt that does not exist.
/// </summary>
public sealed class OpenAttemptImageHandler(IAttemptStore store, IAttemptSource source)
{
    public Task<Result<AttemptImage>> HandleAsync(Guid userId, Guid attemptId, Guid imageId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
