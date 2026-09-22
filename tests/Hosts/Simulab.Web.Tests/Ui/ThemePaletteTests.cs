using System.Reflection;
using MudBlazor;
using MudBlazor.Utilities;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-17: every colour of both palettes has one declared role, and the role's pairs are measured in both themes
/// (rule: ui). A screen check only sees the pairs that screen renders; the dark error red failed for months
/// before F-10 rendered it. A token without a role — one MudBlazor adds in an upgrade, say — fails by name.
/// </summary>
public sealed class ThemePaletteTests
{
    private static readonly MudTheme Theme = SimulabTheme.Create();

    private static readonly string[] Pages = ["Surface", "Background"];

    /// <summary>The colours painted as a filled background with their own contrast text on top.</summary>
    private static readonly string[] Fills = ["Primary", "Secondary", "Tertiary", "Info", "Success", "Warning", "Error", "Dark"];

    private static readonly IReadOnlyDictionary<string, Role> Roles = BuildRoles();

    [Fact]
    public void EveryPaletteColour_HasADeclaredRole()
    {
        var colours = ColourProperties().Select(property => property.Name).ToList();

        colours.Except(Roles.Keys).Should().BeEmpty("every palette colour needs a role in ThemePaletteTests (BR1)");
        Roles.Keys.Except(colours).Should().BeEmpty("a role must name a colour the palette has");
    }

    [Fact]
    public void EveryPairOfARole_NamesColoursThePaletteHas()
    {
        var colours = ColourProperties().Select(property => property.Name).ToHashSet();

        Pairs().Select(pair => (string)pair[1]).Concat(Pairs().Select(pair => (string)pair[2]))
            .Should().OnlyContain(name => colours.Contains(name));
    }

    public static TheoryData<string, string, string, double> Pairs()
    {
        var data = new TheoryData<string, string, string, double>();
        foreach (var theme in new[] { "light", "dark" })
        {
            foreach (var (token, role) in Roles.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                foreach (var (foreground, background, minimum) in role.PairsOf(token))
                {
                    data.Add(theme, foreground, background, minimum);
                }
            }
        }

        return data;
    }

    /// <summary>Text 4.5:1 on its surfaces (BR2), a fill's contrast text 4.5:1 at rest and on hover (BR3), non-text 3:1 (BR4).</summary>
    [Theory]
    [MemberData(nameof(Pairs))]
    public void Pair_ReadsAtTheMinimumOfItsRole(string theme, string foreground, string background, double minimum)
    {
        var palette = theme == "light" ? (Palette)Theme.PaletteLight : Theme.PaletteDark;

        var ratio = ColourContrast.Ratio(ValueOf(palette, foreground), ValueOf(palette, background));

        ratio.Should().BeGreaterThanOrEqualTo(minimum, $"{foreground} on {background} in the {theme} theme reads at {ratio:0.00}:1");
    }

    [Fact]
    public void Pairs_CoverEveryRoleThatIsMeasured()
    {
        var measured = Pairs().Select(pair => (string)pair[1]).Concat(Pairs().Select(pair => (string)pair[2])).ToHashSet();

        Roles.Where(entry => entry.Value is not Exempt).Select(entry => entry.Key)
            .Should().OnlyContain(name => measured.Contains(name), "a colour with a role other than exempt is measured in at least one pair");
    }

    private static IEnumerable<PropertyInfo> ColourProperties() =>
        typeof(Palette).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType == typeof(MudColor) || property.PropertyType == typeof(string));

    private static string ValueOf(Palette palette, string token) =>
        typeof(Palette).GetProperty(token)!.GetValue(palette)!.ToString()!;

    private static Dictionary<string, Role> BuildRoles()
    {
        const string disabled = "a disabled control: WCAG 2.2 1.4.3 and 1.4.11 exempt inactive components";
        const string line = "a decorative line: no control and no information depends on it";
        const string named = "a MudBlazor named colour: no kit component paints with it";
        const string lighten = "a light tone of a fill: no kit component paints with it";

        var roles = new Dictionary<string, Role>(StringComparer.Ordinal)
        {
            ["TextPrimary"] = new Text([.. Pages, "PrimaryLighten"]),
            ["TextSecondary"] = new Text(Pages),
            ["AppbarText"] = new Text(["AppbarBackground"]),
            ["DrawerText"] = new Text(["DrawerBackground"]),

            // B-10: every icon-only button and menu icon. F-17: the border of every outlined field (AppTextField).
            ["ActionDefault"] = new NonText(Pages),
            ["LinesInputs"] = new NonText(Pages),
            ["DrawerIcon"] = new NonText(["DrawerBackground"]),

            ["Surface"] = new Backdrop(),
            ["Background"] = new Backdrop(),
            ["AppbarBackground"] = new Backdrop(),
            ["DrawerBackground"] = new Backdrop(),

            // The role badge (app.css .app-badge) puts the primary text on this tint.
            ["PrimaryLighten"] = new Backdrop(),

            ["TextDisabled"] = new Exempt(disabled),
            ["ActionDisabled"] = new Exempt(disabled),
            ["ActionDisabledBackground"] = new Exempt(disabled),
            ["Divider"] = new Exempt(line),
            ["DividerLight"] = new Exempt(line),
            ["TableLines"] = new Exempt(line),
            ["LinesDefault"] = new Exempt(line),
            ["TableHover"] = new Exempt("a row tint under the text: AppDataTable does not stripe, and the hover tint is 4% or less"),
            ["TableStriped"] = new Exempt("a row tint under the text: AppDataTable does not stripe"),
            ["Skeleton"] = new Exempt("a loading placeholder: it carries no information"),
            ["OverlayDark"] = new Exempt("the scrim behind a dialog: the dialog has its own surface"),
            ["OverlayLight"] = new Exempt("the scrim behind a dialog: the dialog has its own surface"),
            ["BackgroundGray"] = new Exempt(named),
            ["Black"] = new Exempt(named),
            ["White"] = new Exempt(named),
            ["GrayDefault"] = new Exempt(named),
            ["GrayLight"] = new Exempt(named),
            ["GrayLighter"] = new Exempt(named),
            ["GrayDark"] = new Exempt(named),
            ["GrayDarker"] = new Exempt(named),
        };

        foreach (var fill in Fills)
        {
            // BR5: only the primary and the error colours are also text; the other fills are never text (F-17 Q2).
            roles[fill] = new Fill(AlsoText: fill is "Primary" or "Error");
            roles[fill + "ContrastText"] = new PartOfFill(fill);
            roles[fill + "Darken"] = new PartOfFill(fill);
            roles.TryAdd(fill + "Lighten", new Exempt(lighten));
        }

        return roles;
    }

    private abstract record Role
    {
        public virtual IEnumerable<(string Foreground, string Background, double Minimum)> PairsOf(string token) => [];
    }

    /// <summary>Text on each of the listed surfaces.</summary>
    private sealed record Text(string[] On) : Role
    {
        public override IEnumerable<(string, string, double)> PairsOf(string token) =>
            On.Select(background => (token, background, ColourContrast.MinimumForText));
    }

    /// <summary>A filled background: its contrast text on it at rest and on its hover tone; for primary and error also text on the page.</summary>
    private sealed record Fill(bool AlsoText) : Role
    {
        public override IEnumerable<(string, string, double)> PairsOf(string token) =>
        [
            (token + "ContrastText", token, ColourContrast.MinimumForText),
            (token + "ContrastText", token + "Darken", ColourContrast.MinimumForText),
            .. AlsoText ? Pages.Select(background => (token, background, ColourContrast.MinimumForText)) : [],
        ];
    }

    /// <summary>An icon or a control boundary on each of the listed surfaces (WCAG 2.2 1.4.11).</summary>
    private sealed record NonText(string[] On) : Role
    {
        public override IEnumerable<(string, string, double)> PairsOf(string token) =>
            On.Select(background => (token, background, ColourContrast.MinimumForNonText));
    }

    /// <summary>A surface something else is measured on.</summary>
    private sealed record Backdrop : Role;

    /// <summary>The contrast text or the hover tone of a fill, measured with it.</summary>
    private sealed record PartOfFill(string Fill) : Role;

    private sealed record Exempt(string Reason) : Role;
}
