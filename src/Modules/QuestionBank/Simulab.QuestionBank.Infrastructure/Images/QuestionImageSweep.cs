namespace Simulab.QuestionBank.Infrastructure.Images;

/// <summary>
/// Deletes the images nobody saved a text with (F-103, BR11): a pending image older than 24 hours, its file and then
/// its row. The hosted worker calls it every hour; a test calls it directly.
/// </summary>
public sealed class QuestionImageSweep
{
    /// <summary>One pass; the number of images removed.</summary>
    public Task<int> RunAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
