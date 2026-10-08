namespace Simulab.Identity.Tests;

/// <summary>
/// B-24: this project starts Api or Web hosts synchronously, one per test class at once. Each start blocks a pool
/// thread, so the pool needs a high minimum to keep the continuations and timers of the starts running under load.
/// The minimum is set once in tests/Directory.Build.props; this guard keeps the line from being removed in silence.
/// </summary>
public sealed class ThreadPoolMinimumTests
{
    private const int RequiredMinimumWorkerThreads = 256;

    [Fact]
    public void ThreadPool_MinimumWorkerThreads_IsRaisedForHostStarts()
    {
        ThreadPool.GetMinThreads(out var workerThreads, out _);

        workerThreads.Should().BeGreaterThanOrEqualTo(RequiredMinimumWorkerThreads);
    }
}
