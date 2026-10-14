using System.Globalization;
using System.Resources;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-114 AC13: every text of the field kinds exists in pt-BR, pt-PT and en (rule: i18n).</summary>
public sealed class SelectFieldResourcesTests
{
    private static readonly ResourceManager Manager =
        new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string> KeysByCulture()
    {
        string[] keys =
        [
            "SelectField.Expand", "SelectField.Collapse", "SelectField.NoResults", "SelectField.Results",
            "SelectField.Loading", "Gallery.Section.FieldKinds", "Gallery.FieldKinds.Hint",
            "Gallery.FieldKinds.Select", "Gallery.FieldKinds.Lookup", "Gallery.FieldKinds.Text",
            "Gallery.FieldKinds.State.Empty", "Gallery.FieldKinds.State.WithValue", "Gallery.FieldKinds.State.Disabled",
            "Gallery.FieldKinds.State.Error", "Gallery.FieldKinds.State.LongText", "Gallery.FieldKinds.State.Loading",
            "Gallery.FieldKinds.State.Open", "Gallery.FieldKinds.State.NoResults", "Gallery.FieldKinds.State.LeadingIcon",
            "Gallery.FieldKinds.State.NotApplicable"
        ];
        var data = new TheoryData<string, string>();
        foreach (var culture in new[] { "en", "pt-BR", "pt-PT" })
        {
            foreach (var key in keys)
            {
                data.Add(culture, key);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(KeysByCulture))]
    public void Key_HasATextInTheLanguage(string culture, string key)
    {
        var text = Manager.GetString(key, new CultureInfo(culture));

        text.Should().NotBeNullOrWhiteSpace($"'{key}' must exist in {culture}");
    }
}
