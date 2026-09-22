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

        builder.ToTable(JobsDbContext.TableName, JobsDbContext.SchemaName);
        builder.HasKey(job => job.Id);

        builder.Property(job => job.Type).IsRequired().HasMaxLength(100);
        builder.Property(job => job.Payload).IsRequired();
        builder.Property(job => job.Status).IsRequired();
        builder.Property(job => job.Attempts).IsRequired();
        builder.Property(job => job.CreatedAt).IsRequired();
        builder.Property(job => job.RunAfter).IsRequired();
        builder.Property(job => job.LastError).HasMaxLength(LastErrorMaxLength);

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
