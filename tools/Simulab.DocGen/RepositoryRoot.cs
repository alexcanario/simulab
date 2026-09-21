namespace Simulab.DocGen;

/// <summary>The folder that holds the solution file, found by walking up from the tool's output folder.</summary>
internal static class RepositoryRoot
{
    public static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !directory.EnumerateFiles("*.slnx").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("No solution file found above the tool.");
    }
}
