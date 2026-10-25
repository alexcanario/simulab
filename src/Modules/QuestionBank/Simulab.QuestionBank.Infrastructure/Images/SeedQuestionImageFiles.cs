namespace Simulab.QuestionBank.Infrastructure.Images;

/// <summary>
/// F-110 (BR8): writes the files of the images the <c>SeedManausGuardQuestions</c> migration seeded.
/// </summary>
public sealed class SeedQuestionImageFiles
{
    /// <summary>Writes each missing file of a live seeded image row; the number of files written.</summary>
    public Task<int> WriteMissingAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
