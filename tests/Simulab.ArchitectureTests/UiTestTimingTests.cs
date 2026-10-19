namespace Simulab.ArchitectureTests;

/// <summary>
/// B-23: <c>Click()</c> can return before the handler has run, so a test that reads the URL on the statement after
/// a click can fail under load (the race of B-11 and B-22). A text rule cannot tell which handlers await, so it
/// forbids the shape: a statement ending in <c>.Click();</c> followed, after blank lines only, by a statement that
/// reads <c>.Uri</c> outside <c>WaitForAssertion(</c>. This reads the test sources.
/// </summary>
public class UiTestTimingTests
{
    internal static IReadOnlyList<string> FindClickThenUriRead(string testsRoot) =>
        [.. TestSources(testsRoot).SelectMany(path => Hits(File.ReadAllLines(path)).Select(line => $"{Path.GetRelativePath(testsRoot, path)}:{line}"))];

    /// <summary>B-30: the lines where a test source calls <c>AdvanceDebounce(...)</c> as a statement of its own.</summary>
    internal static IReadOnlyList<string> FindBareAdvanceDebounce(string testsRoot) =>
        [.. TestSources(testsRoot).SelectMany(path => BareAdvanceLines(File.ReadAllLines(path)).Select(line => $"{Path.GetRelativePath(testsRoot, path)}:{line}"))];

    private static IEnumerable<string> TestSources(string testsRoot)
    {
        var generated = new[] { $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}" };

        return Directory.EnumerateFiles(testsRoot, "*Tests.cs", SearchOption.AllDirectories)
            .Where(path => !generated.Any(segment => path.Contains(segment, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<int> BareAdvanceLines(string[] lines)
    {
        for (var index = 0; index < lines.Length; index++)
        {
            var statement = lines[index].Trim();
            if (statement.StartsWith("AdvanceDebounce(", StringComparison.Ordinal) && statement.EndsWith(';'))
            {
                yield return index + 1;
            }
        }
    }

    private static IEnumerable<int> Hits(string[] lines)
    {
        for (var index = 0; index < lines.Length; index++)
        {
            if (!lines[index].TrimEnd().EndsWith(".Click();", StringComparison.Ordinal))
            {
                continue;
            }

            var next = index + 1;
            while (next < lines.Length && string.IsNullOrWhiteSpace(lines[next]))
            {
                next++;
            }

            if (next < lines.Length
                && lines[next].Contains(".Uri", StringComparison.Ordinal)
                && !lines[next].Contains("WaitForAssertion(", StringComparison.Ordinal))
            {
                yield return next + 1;
            }
        }
    }

    [Fact]
    public void ClickThenUriRead_InTestSources_IsNotFound()
    {
        var testsRoot = Path.Combine(SolutionAssemblies.RepositoryRoot(), "tests");

        var hits = FindClickThenUriRead(testsRoot);

        hits.Should().BeEmpty("a click can return before its handler ran: read the URL inside WaitForAssertion (B-23). Found: {0}", string.Join("; ", hits));
    }

    [Fact]
    public void AdvanceDebounceAlone_InTestSources_IsNotFound()
    {
        var testsRoot = Path.Combine(SolutionAssemblies.RepositoryRoot(), "tests");

        var hits = FindBareAdvanceDebounce(testsRoot);

        hits.Should().BeEmpty("a keystroke can reach its debounce after the clock moved, so one advance can miss it: wait with WaitForAfterDebounce, which moves the clock on every try (B-30). Found: {0}", string.Join("; ", hits));
    }

    [Fact]
    public void FindBareAdvanceDebounce_AdvanceAsAStatement_NamesTheFileAndLine()
    {
        var root = Directory.CreateTempSubdirectory("simulab-advance-");
        try
        {
            File.WriteAllLines(Path.Combine(root.FullName, "AloneTests.cs"), ["page.Find(\"input\").Input(\"x\");", "        AdvanceDebounce();", "", "page.WaitForAssertion(() => Api.Received.Should().NotBeEmpty());"]);
            File.WriteAllLines(Path.Combine(root.FullName, "IntervalTests.cs"), ["field.Find(\"input\").Input(\"x\");", "AdvanceDebounce(500);"]);

            var hits = FindBareAdvanceDebounce(root.FullName);

            hits.Should().BeEquivalentTo(["AloneTests.cs:2", "IntervalTests.cs:2"]);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void FindBareAdvanceDebounce_TheHelperAndOtherMentions_AreLeftAlone()
    {
        var root = Directory.CreateTempSubdirectory("simulab-advance-");
        try
        {
            File.WriteAllLines(Path.Combine(root.FullName, "HelperTests.cs"), ["WaitForAfterDebounce(page, () => Api.Received.Should().NotBeEmpty());", "// AdvanceDebounce() once is not enough", "Clock.Advance(TimeSpan.FromMilliseconds(300));"]);

            FindBareAdvanceDebounce(root.FullName).Should().BeEmpty();
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void FindClickThenUriRead_UriReadAfterAClick_NamesTheFileAndLine()
    {
        var root = Directory.CreateTempSubdirectory("simulab-click-uri-");
        try
        {
            File.WriteAllLines(Path.Combine(root.FullName, "BlankTests.cs"), ["page.Find(\"a\").Click();", "", "navigation.Uri.Should().EndWith(\"/x\");"]);
            File.WriteAllLines(Path.Combine(root.FullName, "TightTests.cs"), ["button.Click();", "Navigation.Uri.Should().EndWith(\"/x\");"]);

            var hits = FindClickThenUriRead(root.FullName);

            hits.Should().BeEquivalentTo(["BlankTests.cs:3", "TightTests.cs:2"]);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void FindClickThenUriRead_UriReadInsideWaitForAssertion_IsLeftAlone()
    {
        var root = Directory.CreateTempSubdirectory("simulab-click-uri-");
        try
        {
            File.WriteAllLines(Path.Combine(root.FullName, "WaitedTests.cs"), ["page.Find(\"a\").Click();", "", "page.WaitForAssertion(() => navigation.Uri.Should().EndWith(\"/x\"));"]);
            File.WriteAllLines(Path.Combine(root.FullName, "OtherTests.cs"), ["page.Find(\"a\").Click();", "page.Markup.Should().Contain(\"x\");", "navigation.Uri.Should().EndWith(\"/x\");"]);

            FindClickThenUriRead(root.FullName).Should().BeEmpty();
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}
