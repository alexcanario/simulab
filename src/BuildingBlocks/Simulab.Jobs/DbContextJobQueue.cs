using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Simulab.Jobs;

/// <summary>
/// The queue over a module's own <c>DbContext</c> (F-13 BR2). The module maps the job table with
/// <c>AddJobQueue()</c>; staging a job here puts it in the same change tracker, and therefore in the
/// same transaction, as the token or account row the email is about.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "It is an IJobQueue over a DbContext; the suffix names what it is (F-13).")]
public sealed class DbContextJobQueue<TContext>(TContext context, TimeProvider timeProvider) : IJobQueue
    where TContext : DbContext
{
    public void Enqueue(string type, string payload)
    {
        var now = timeProvider.GetUtcNow();
        context.Set<Job>().Add(new Job
        {
            Type = type,
            Payload = payload,
            CreatedAt = now,
            RunAfter = now
        });
    }

    public Task FlushAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
