using System.Diagnostics.CodeAnalysis;

namespace Simulab.Jobs;

/// <summary>
/// Puts work on the queue (F-13 BR2). <see cref="Enqueue"/> only stages the row on the caller's unit of
/// work: it is written by the <c>SaveChanges</c> that also writes the data justifying it, so there is no
/// state where that data exists and the job does not. A caller with no save of its own calls
/// <see cref="FlushAsync"/>.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "The job queue is a queue; 'Queue' is the word the feature file and the owner use (F-13).")]
public interface IJobQueue
{
    /// <summary>Stages a job. Nothing reaches the database until the caller's unit of work is saved.</summary>
    void Enqueue(string type, string payload);

    /// <summary>Writes what is staged on the caller's unit of work.</summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
