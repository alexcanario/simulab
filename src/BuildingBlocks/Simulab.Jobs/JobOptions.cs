namespace Simulab.Jobs;

/// <summary>Bound from configuration section <c>Jobs</c> (F-13 BR13).</summary>
public sealed class JobOptions
{
    public const string SectionName = "Jobs";

    /// <summary>Whether the hosted worker polls. The test host sets it to false and runs the jobs itself.</summary>
    public bool WorkerEnabled { get; set; } = true;
}
