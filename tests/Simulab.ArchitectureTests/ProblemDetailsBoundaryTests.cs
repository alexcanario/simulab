using System.Text.RegularExpressions;

namespace Simulab.ArchitectureTests;

/// <summary>
/// F-39, BR6: the general mapping from an <see cref="Simulab.SharedKernel.Results.ErrorKind"/> to an
/// HTTP status lives in <c>Simulab.ApiResults</c> and nowhere else. This is the check that stops the
/// fourth copy: F-33 wrote the second and F-41 the third, both past review.
/// </summary>
/// <remarks>
/// It reads the source on disk rather than the compiled assemblies on purpose. A reflection check
/// would only see the assemblies listed by hand in <see cref="SolutionAssemblies"/>, and F-24 caught
/// that list silently missing a project for four weeks; a file scan sees a new module's file the day
/// it is written.
/// </remarks>
public partial class ProblemDetailsBoundaryTests
{
    private const string Block = "Simulab.ApiResults";

    /// <summary>
    /// Files that decide a status for one kind on purpose and must stay legal: they are not copies of
    /// the mapping, and the detector has to keep telling the difference. No exemption list — the rule
    /// below does not flag them, and <see cref="TheDetector_LeavesAPerEndpointDecisionAlone"/> is what
    /// keeps that true if anyone makes the detector laxer (F-39, v2).
    /// </summary>
    private static readonly Dictionary<string, string> PerEndpointDecisions = new(StringComparer.Ordinal)
    {
        ["Modules/Identity/Simulab.Identity.Api/TotpEndpoints.cs"] =
            "turns NotFound into 401 so the TOTP routes do not reveal whether an account exists",
        ["Modules/Identity/Simulab.Identity.Api/RevocationCheckMiddleware.cs"] =
            "writes a problem document from a constant code, with no Error and no ErrorKind",
        ["Modules/Identity/Simulab.Identity.Api/Authorization/PermissionForbiddenResultHandler.cs"] =
            "writes the 403 body from a constant code, with no Error and no ErrorKind"
    };

    /// <summary>
    /// The table itself: an arm that turns one <c>ErrorKind</c> into a status, in either shape C# writes
    /// it. Merely naming <c>ErrorKind</c> beside a <c>StatusCodes.Status</c> is not it — a caller that
    /// passes a status override for one kind does exactly that, and it is the supported way (BR6).
    /// </summary>
    [GeneratedRegex(@"ErrorKind\.\w+\s*=>\s*StatusCodes\.Status\w+|case\s+ErrorKind\.", RegexOptions.None, 2000)]
    private static partial Regex StatusTable();

    /// <summary>A problem document built out of an <see cref="Simulab.SharedKernel.Results.Error"/>.</summary>
    [GeneratedRegex(@"new ProblemDetails[\s\S]{0,400}?\berror\.(Code|Kind|Detail)\b", RegexOptions.None, 2000)]
    private static partial Regex ProblemFromError();

    private static bool CarriesTheMapping(string text) =>
        StatusTable().IsMatch(text) || ProblemFromError().IsMatch(text);

    private static IEnumerable<(string Path, string Name, string Text)> SourceFiles()
    {
        var root = Path.Combine(SolutionAssemblies.RepositoryRoot(), "src");

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => (Path: Path.GetRelativePath(root, path).Replace('\\', '/'), Name: Path.GetFileName(path), Text: File.ReadAllText(path)));
    }

    [Fact]
    public void OnlyTheBuildingBlock_CarriesTheMapping()
    {
        var files = SourceFiles().ToList();

        files.Should().HaveCountGreaterThan(100, "the scan must have read the real source tree");

        var offenders = files
            .Where(file => !file.Path.StartsWith($"BuildingBlocks/{Block}/", StringComparison.Ordinal))
            .Where(file => CarriesTheMapping(file.Text))
            .Select(file => file.Path)
            .ToList();

        offenders.Should().BeEmpty(
            $"the mapping belongs to {Block}; call ApiProblem.Problem(error) instead, or pass a status override for one kind. Found in: {string.Join(", ", offenders)}");
    }

    /// <summary>The positive control: the scan really does recognise the mapping where it lives.</summary>
    [Fact]
    public void TheScan_RecognisesTheMappingInTheBuildingBlock()
    {
        var block = SourceFiles().Where(file => file.Path.StartsWith($"BuildingBlocks/{Block}/", StringComparison.Ordinal)).ToList();

        block.Should().NotBeEmpty($"{Block} must have source files, or the control proves nothing");
        block.Should().Contain(file => CarriesTheMapping(file.Text), "otherwise the detector matches nothing and the rule above passes for free");
    }

    /// <summary>
    /// AC6b: the detector tells a single endpoint's decision about one kind from a copy of the table.
    /// These three are legal and must stay unflagged; a laxer detector turns this red instead of
    /// quietly exempting them.
    /// </summary>
    [Fact]
    public void TheDetector_LeavesAPerEndpointDecisionAlone()
    {
        var files = SourceFiles().ToDictionary(file => file.Path, file => file.Text, StringComparer.Ordinal);

        PerEndpointDecisions.Should().NotBeEmpty();
        PerEndpointDecisions.Values.Should().OnlyContain(reason => reason.Length > 30);
        PerEndpointDecisions.Keys.Should().BeSubsetOf(files.Keys, "a control over a file that no longer exists proves nothing");

        foreach (var (path, reason) in PerEndpointDecisions)
        {
            CarriesTheMapping(files[path]).Should().BeFalse($"{path} {reason}");
        }
    }

    /// <summary>The block is the only place the three deleted copies could have come back to.</summary>
    [Fact]
    public void TheDeletedCopies_AreGone()
    {
        var withStatusFor = SourceFiles()
            .Where(file => !file.Path.StartsWith($"BuildingBlocks/{Block}/", StringComparison.Ordinal))
            .Where(file => file.Text.Contains("int StatusFor(", StringComparison.Ordinal))
            .Select(file => file.Path)
            .ToList();

        withStatusFor.Should().BeEmpty(string.Join(", ", withStatusFor));
    }
}
