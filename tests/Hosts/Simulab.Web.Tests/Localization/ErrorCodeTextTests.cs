using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Simulab.Web.Localization;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Localization;

/// <summary>
/// F-22: the real solution against <see cref="ErrorCodeTextCheck"/>. The codes come from reflection over every
/// <c>*ErrorCodes</c> class of the <c>*.Contracts</c> assemblies (BR1), so a module added later is covered with
/// no change here, and the texts come from the resource set <c>ErrorText.For</c> reads (BR2).
/// </summary>
public sealed class ErrorCodeTextTests
{
    /// <summary>
    /// BR3: the codes the app never shows a person, each with the reason. Nothing else is exempt. Adding a line
    /// here is a decision to review: it says this code will never reach a human, in any language.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> NeverShown = new Dictionary<string, string>
    {
        ["identity.forbidden"] = "403 from the API; no page reads it — an admin list shows the table's error state, "
            + "and a page the caller may not open shows the ordinary Not Found page (F-6).",
        ["mfa_required"] = "the password step's answer when two-factor is on; read as a flow signal by "
            + "AuthClient.NeedsTotpCode and turned into the code step, never displayed (F-11 BR9).",
    };

    [Fact]
    public void EveryErrorCode_HasATextInEveryLanguage_AndEveryCodeShapedTextAnswersToACode()
    {
        var codes = ErrorCodes();
        var texts = TextsByCulture();

        var failures = ErrorCodeTextCheck.Run(codes, texts, NeverShown);

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    /// <summary>The rule of presence: an empty scan must fail, not pass (profile: architecture tests).</summary>
    [Fact]
    public void TheScan_FindsTheCodesOfEveryContractsAssembly()
    {
        var classes = ErrorCodeClasses();

        classes.Should().NotBeEmpty("a *.Contracts assembly with an *ErrorCodes class must be found");
        classes.Should().Contain(type => type.Name == "IdentityErrorCodes");
        ErrorCodes().Should().HaveCountGreaterThan(20).And.Contain("identity.invalid_credentials");
    }

    [Fact]
    public void TheTexts_AreReadForTheThreeLanguages()
    {
        var texts = TextsByCulture();

        texts.Keys.Should().BeEquivalentTo(SupportedCultures.All.Select(culture => culture.Name));
        texts.Values.Should().OnlyContain(keys => keys.Count > 0);
    }

    [Fact]
    public void EveryExemption_NamesACodeAndGivesAReason()
    {
        NeverShown.Should().NotBeEmpty();
        NeverShown.Keys.Should().BeSubsetOf(ErrorCodes());
        NeverShown.Values.Should().OnlyContain(reason => reason.Length > 20);
    }

    /// <summary>BR1: every public string constant of every static <c>*ErrorCodes</c> class, by reflection.</summary>
    private static IReadOnlyCollection<string> ErrorCodes() =>
        [.. ErrorCodeClasses()
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(field => field is { IsLiteral: true, IsInitOnly: false, FieldType.FullName: "System.String" })
            .Select(field => (string)field.GetRawConstantValue()!)
            .Distinct(StringComparer.Ordinal)];

    /// <summary>
    /// The contracts assemblies are next to this test's own: the Web references every module's
    /// <c>Contracts</c> project (profile), and this project references the Web.
    /// </summary>
    private static IReadOnlyCollection<Type> ErrorCodeClasses()
    {
        var folder = Path.GetDirectoryName(typeof(ErrorCodeTextTests).Assembly.Location)!;

        return
        [
            .. Directory.EnumerateFiles(folder, "Simulab.*.Contracts.dll")
                .Select(Assembly.LoadFrom)
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => type is { IsClass: true, IsAbstract: true, IsSealed: true }
                    && type.Name.EndsWith("ErrorCodes", StringComparison.Ordinal))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
        ];
    }

    /// <summary>The keys of <see cref="SharedResources"/> per culture, as <c>ResourceParityTests</c> reads them.</summary>
    private static Dictionary<string, IReadOnlyCollection<string>> TextsByCulture()
    {
        var manager = new ResourceManager(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

        return SupportedCultures.All.ToDictionary(
            culture => culture.Name,
            culture => Keys(manager, SetCultureOf(culture)),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// The default language has no file of its own: it is the neutral set (`SharedResources.resx`), as
    /// <c>ResourceParityTests</c> reads it. Asking for "en" with <c>tryParents: false</c> finds nothing.
    /// </summary>
    private static CultureInfo SetCultureOf(CultureInfo culture) =>
        culture.Name == SupportedCultures.Default ? CultureInfo.InvariantCulture : culture;

    private static IReadOnlyCollection<string> Keys(ResourceManager manager, CultureInfo culture)
    {
        var set = manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        set.Should().NotBeNull($"the resource file for '{culture.Name}' must exist");
        return [.. set!.Cast<DictionaryEntry>().Select(entry => (string)entry.Key)];
    }
}
