using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Simulab.Jobs.Persistence.Configurations;

/// <summary>
/// The one table of the queue (F-13 BR1). It is mapped by <see cref="JobsDbContext"/>, which owns its
/// migration, and again by every module context that enqueues, where it is excluded from migrations
/// (<see cref="JobModelBuilderExtensions.AddJobQueue"/>): one table, one owner, many writers.
/// </summary>
public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    /// <summary>The longest failure message that is kept; the rest is cut so one bad job cannot fill the table.</summary>
    public const int LastErrorMaxLength = 2000;

    public void Configure(EntityTypeBuilder<Job> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(JobsDbContext.TableName, JobsDbContext.SchemaName, table => table.HasComment(
            "Work that must happen but must not make a request wait, such as sending an email. A job that succeeds leaves no row."));
        builder.HasKey(job => job.Id);

        builder.Property(job => job.Type).IsRequired().HasMaxLength(100)
            .HasComment("Which handler runs the job. The worker matches it against the handlers registered in the host.");
        builder.Property(job => job.Payload).IsRequired()
            .HasComment("The whole input of the job, as JSON. Cleared when the job is given up on: the message is a live link and an address, not evidence.");
        builder.Property(job => job.Status).IsRequired()
            .HasComment("What the row is doing: 0 waiting for its turn, 1 a worker is running it, 2 every attempt failed. There is no value for success, because a job that succeeds leaves no row.");
        builder.Property(job => job.Attempts).IsRequired()
            .HasComment("How many times a worker has started it. The first attempt makes it 1.");
        builder.Property(job => job.CreatedAt).IsRequired()
            .HasComment("When the job was enqueued, in UTC. The worker takes the oldest first. This is not an audit field: the queue is not audited.");
        builder.Property(job => job.RunAfter).IsRequired()
            .HasComment("Not to be run before this instant, in UTC. Set at enqueue and pushed forward by the retry backoff.");
        builder.Property(job => job.StartedAt)
            .HasComment("When the current attempt started, in UTC. A row stuck here is taken again by another worker.");
        builder.Property(job => job.LastError).HasMaxLength(LastErrorMaxLength)
            .HasComment("The message of the last failure, kept with a failed row. Cut so one bad job cannot fill the table.");

        // BR9: the worker asks for the due rows, oldest first. F-18: the index holds only the rows a claim can
        // take, so the Failed rows kept by BR7 never enter it and the claim's cost follows the active queue.
        // The claim states both statuses as constants, which is what lets the planner prove this filter.
        builder.HasIndex(job => job.CreatedAt)
            .HasFilter(ActiveFilter)
            .HasDatabaseName("ix_jobs_active_created_at");
    }

    /// <summary>The rows <see cref="JobRunner"/> can claim: waiting for their turn, or left running by a worker.</summary>
    public static readonly string ActiveFilter = $"status IN ({(int)JobStatus.Pending}, {(int)JobStatus.Running})";
}
