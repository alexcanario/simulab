using System.Diagnostics;

namespace Simulab.AppHost.Tests;

/// <summary>
/// F-81: what the workflow tests share: the repository root and a bash that runs one step of a workflow. The same
/// two helpers are private in <c>DeployWorkflowTests</c> (F-65); that file is left as it is.
/// </summary>
internal static class WorkflowTestSupport
{
    public static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Directory.Packages.props not found above the test output.");
    }

    public static async Task<(int ExitCode, string Output)> RunBashAsync(string script, IReadOnlyDictionary<string, string> environment)
    {
        var start = new ProcessStartInfo(BashPath())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(script);
        foreach (var (name, value) in environment)
        {
            start.Environment[name] = value;
        }

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(timeout.Token);
        return (process.ExitCode, await output + await error);
    }

    /// <summary>On Windows the bash on the PATH may be the WSL launcher; the one that ships with Git is the one that runs a step.</summary>
    private static string BashPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "bash";
        }

        var gitBash = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "bash.exe");
        return File.Exists(gitBash) ? gitBash : throw new InvalidOperationException("Git for Windows (bash.exe) is needed to run a workflow step.");
    }
}
