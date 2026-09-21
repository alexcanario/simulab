using Simulab.DocGen;

namespace Simulab.ArchitectureTests.DocGen;

/// <summary>F-15: stale detection behind `--check` and the idempotent write (AC5, AC6, AC7).</summary>
public sealed class GeneratedDocsTests : IDisposable
{
    private readonly string _outDir = Path.Combine(Path.GetTempPath(), "simulab-docgen-" + Guid.NewGuid().ToString("N"));

    private static readonly Dictionary<string, string> Generated = new(StringComparer.Ordinal)
    {
        ["README.md"] = "# index\n",
        ["Identity/entities.md"] = "# Identity\n"
    };

    public void Dispose()
    {
        if (Directory.Exists(_outDir))
        {
            Directory.Delete(_outDir, recursive: true);
        }
    }

    [Fact]
    public void StaleFiles_OnAnEmptyFolder_ListsEveryFile() =>
        GeneratedDocs.StaleFiles(_outDir, Generated).Should().Equal("Identity/entities.md", "README.md");

    [Fact]
    public void StaleFiles_AfterWrite_IsEmpty()
    {
        GeneratedDocs.Write(_outDir, Generated, GeneratedDocs.StaleFiles(_outDir, Generated));

        GeneratedDocs.StaleFiles(_outDir, Generated).Should().BeEmpty();
    }

    [Fact]
    public void StaleFiles_IgnoresWindowsLineEndings()
    {
        GeneratedDocs.Write(_outDir, Generated, GeneratedDocs.StaleFiles(_outDir, Generated));
        File.WriteAllText(Path.Combine(_outDir, "README.md"), "# index\r\n");

        GeneratedDocs.StaleFiles(_outDir, Generated).Should().BeEmpty();
    }

    [Fact]
    public void StaleFiles_NamesTheFileThatDiffersFromTheCode()
    {
        GeneratedDocs.Write(_outDir, Generated, GeneratedDocs.StaleFiles(_outDir, Generated));
        var changed = new Dictionary<string, string>(Generated) { ["Identity/entities.md"] = "# Identity\nnew column\n" };

        GeneratedDocs.StaleFiles(_outDir, changed).Should().Equal("Identity/entities.md");
    }

    [Fact]
    public void StaleFiles_NamesAFileTheGeneratorNoLongerProduces_AndWriteRemovesIt()
    {
        GeneratedDocs.Write(_outDir, Generated, GeneratedDocs.StaleFiles(_outDir, Generated));
        File.WriteAllText(Path.Combine(_outDir, "notes.md"), "hand-written");

        var stale = GeneratedDocs.StaleFiles(_outDir, Generated);
        stale.Should().Equal("notes.md");

        GeneratedDocs.Write(_outDir, Generated, stale);
        File.Exists(Path.Combine(_outDir, "notes.md")).Should().BeFalse();
        GeneratedDocs.StaleFiles(_outDir, Generated).Should().BeEmpty();
    }
}
