using Bunit;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-183 AC1, BR1: the e-mail field cleans what the user typed when they leave it.</summary>
public sealed class AppEmailFieldTests : KitTestContext
{
    private string? _changed;

    private IRenderedComponent<AppEmailField> RenderField(string? value = null) =>
        Render<AppEmailField>(parameters => parameters
            .Add(field => field.Id, "email")
            .Add(field => field.Label, "E-mail")
            .Add(field => field.Autocomplete, "username")
            .Add(field => field.Value, value)
            .Add(field => field.ValueChanged, (string? changed) => _changed = changed));

    [Fact]
    public void Leave_TrimsAndLowerCasesTheValue()
    {
        var field = RenderField();

        field.Find("#email").Change(" Ana@Exemplo.COM ");

        _changed.Should().Be("ana@exemplo.com");
    }

    [Fact]
    public void Input_CarriesTheEmailAttributes()
    {
        var input = RenderField().Find(".app-email-field #email");

        input.GetAttribute("type").Should().Be("email");
        input.GetAttribute("inputmode").Should().Be("email");
        input.GetAttribute("autocomplete").Should().Be("username");
        input.GetAttribute("spellcheck").Should().Be("false");
    }
}

/// <summary>F-183 AC2, BR2: the one-time code field takes six digits and a pasted code whole.</summary>
public sealed class AppOtpFieldTests : KitTestContext
{
    private string? _changed;

    private IRenderedComponent<AppOtpField> RenderField() =>
        Render<AppOtpField>(parameters => parameters
            .Add(field => field.Id, "code")
            .Add(field => field.Label, "Code")
            .Add(field => field.ValueChanged, (string? changed) => _changed = changed));

    [Fact]
    public void LettersAndASeventhDigit_AreNotTaken()
    {
        var field = RenderField();

        field.Find("#code").Change("12a3b4567");

        _changed.Should().Be("123456");
    }

    [Theory]
    [InlineData("123 456")]
    [InlineData("123-456")]
    [InlineData(" 123456 ")]
    public void PastedCode_FillsTheFieldWhole(string pasted)
    {
        var field = RenderField();

        field.Find("#code").Change(pasted);

        _changed.Should().Be("123456");
    }

    [Fact]
    public void Input_AsksForDigitsAndTheOneTimeCode()
    {
        var input = RenderField().Find(".app-otp-field #code");

        input.GetAttribute("inputmode").Should().Be("numeric");
        input.GetAttribute("autocomplete").Should().Be("one-time-code");
        input.GetAttribute("pattern").Should().Be("[0-9]*");
        // Room for a pasted "123 456": the separator is dropped after the paste, not cut before it.
        int.Parse(input.GetAttribute("maxlength")!, System.Globalization.CultureInfo.InvariantCulture).Should().BeGreaterThan(AppOtpField.TotpLength);
    }
}

/// <summary>F-183 AC3, BR3: the code field drops spaces and shows the characters in monospace.</summary>
public sealed class AppCodeFieldTests : KitTestContext
{
    private string? _changed;

    private IRenderedComponent<AppCodeField> RenderField(bool upperCase = false) =>
        Render<AppCodeField>(parameters => parameters
            .Add(field => field.Id, "code")
            .Add(field => field.Label, "Recovery code")
            .Add(field => field.Autocomplete, "one-time-code")
            .Add(field => field.UpperCase, upperCase)
            .Add(field => field.ValueChanged, (string? changed) => _changed = changed));

    [Fact]
    public void Spaces_AreRemoved()
    {
        var field = RenderField();

        field.Find("#code").Change("abcd efgh");

        _changed.Should().Be("abcdefgh");
    }

    [Fact]
    public void UpperCase_On_RaisesTheLetters()
    {
        var field = RenderField(upperCase: true);

        field.Find("#code").Change("abcd-efgh");

        _changed.Should().Be("ABCD-EFGH");
    }

    [Fact]
    public void Input_IsMonospaceWithNoSpellCheck()
    {
        var input = RenderField().Find(".app-code-field #code");

        input.GetAttribute("spellcheck").Should().Be("false");
        input.GetAttribute("autocapitalize").Should().Be("off");
        input.GetAttribute("autocomplete").Should().Be("one-time-code");
    }
}

/// <summary>F-183 AC4, BR5: the text area grows between two line counts and always shows the counter.</summary>
public sealed class AppTextAreaFieldTests : KitTestContext
{
    private IRenderedComponent<AppTextAreaField> RenderField(string? value) =>
        Render<AppTextAreaField>(parameters => parameters
            .Add(field => field.Id, "description")
            .Add(field => field.Label, "Description")
            .Add(field => field.MaxLength, 200)
            .Add(field => field.Value, value));

    [Fact]
    public void Renders_ATextAreaOfTheMinimumLines()
    {
        var area = RenderField(null).Find(".app-text-area-field textarea#description");

        area.GetAttribute("rows").Should().Be("3");
    }

    [Fact]
    public void Grows_UpToTheMaximumLines()
    {
        var input = RenderField(null).FindComponent<MudTextField<string>>().Instance;

        input.Sizing.Should().Be(InputSizing.Auto);
        input.MaxLines.Should().Be(10);
    }

    [Fact]
    public void Counter_IsAlwaysOn()
    {
        var field = RenderField("hello");

        field.Find(".app-field-counter").TextContent.Trim().Should().Be("5/200");
    }
}

/// <summary>F-183 AC5, BR6: the URL field completes the scheme, refuses another one and opens the link.</summary>
public sealed class AppUrlFieldTests : KitTestContext
{
    private string? _changed;

    private IRenderedComponent<AppUrlField> RenderField(string? value = null, string? error = null) =>
        Render<AppUrlField>(parameters => parameters
            .Add(field => field.Id, "site")
            .Add(field => field.Label, "Website")
            .Add(field => field.Autocomplete, "url")
            .Add(field => field.Value, value)
            .Add(field => field.Error, error)
            .Add(field => field.ValueChanged, (string? changed) => _changed = changed));

    [Theory]
    [InlineData("exemplo.gov.br", "https://exemplo.gov.br")]
    [InlineData("  https://exemplo.gov.br/edital  ", "https://exemplo.gov.br/edital")]
    [InlineData("http://exemplo.gov.br", "http://exemplo.gov.br")]
    public void Leave_TrimsAndAddsTheSchemeWhenMissing(string typed, string expected)
    {
        var field = RenderField();

        field.Find("#site").Change(typed);

        _changed.Should().Be(expected);
    }

    [Fact]
    public void ValidValue_ShowsTheOpenLinkInANewTab()
    {
        var link = RenderField("https://exemplo.gov.br").Find(".app-url-field a.app-url-open");

        link.GetAttribute("href").Should().Be("https://exemplo.gov.br");
        link.GetAttribute("target").Should().Be("_blank");
        link.GetAttribute("rel").Should().Be("noopener noreferrer");
        link.GetAttribute("aria-label").Should().Be("Open the link in a new tab");
    }

    [Fact]
    public void OtherScheme_ShowsTheErrorAndKeepsTheText()
    {
        var field = RenderField("ftp://x");

        field.Find(".app-field-error-text").TextContent.Should().Be("Enter a web address that starts with http:// or https://.");
        field.Find("#site").GetAttribute("value").Should().Be("ftp://x");
        field.FindAll("a.app-url-open").Should().BeEmpty();
    }

    [Fact]
    public void PageError_WinsOverTheFieldsOwn()
    {
        var field = RenderField("ftp://x", error: "The notice link is too long.");

        field.Find(".app-field-error-text").TextContent.Should().Be("The notice link is too long.");
    }

    [Fact]
    public void Input_AsksForAWebAddress()
    {
        var input = RenderField().Find("#site");

        input.GetAttribute("inputmode").Should().Be("url");
        input.GetAttribute("spellcheck").Should().Be("false");
        input.GetAttribute("autocomplete").Should().Be("url");
        RenderField().FindAll("a.app-url-open").Should().BeEmpty();
    }

    [Theory]
    [InlineData("https://exemplo.gov.br", true)]
    [InlineData("http://exemplo.gov.br/a?b=1", true)]
    [InlineData("ftp://exemplo.gov.br", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("https://", false)]
    public void IsValid_TakesOnlyHttpAndHttps(string value, bool expected) =>
        AppUrlField.IsValid(value).Should().Be(expected);
}

/// <summary>F-183 AC6, BR7: one search box for the tables and any later search.</summary>
public sealed class AppSearchFieldTests : KitTestContext
{
    [Fact]
    public void Renders_AMagnifierAndAClearButton()
    {
        var field = Render<AppSearchField>(parameters => parameters.Add(p => p.Value, "abc"));

        var input = field.Find(".app-search-field input");
        input.GetAttribute("aria-label").Should().Be("Search");
        input.GetAttribute("placeholder").Should().Be("Search");
        field.FindAll(".app-search-field .mud-input-adornment-start").Should().ContainSingle();
        field.FindAll(".app-search-field .mud-input-clear-button").Should().ContainSingle();
    }

    [Fact]
    public void DataTable_SearchesWithTheSearchField()
    {
        Render<MudPopoverProvider>();
        var source = new GallerySource(number => new GallerySample(number, $"Item {number}", "Description", number, number,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)), TimeSpan.Zero);

        var table = Render<AppDataTable<GallerySample>>(parameters => parameters
            .Add(p => p.LoadAsync, source.LoadAsync)
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<PropertyColumn<GallerySample, string>>(0);
                builder.AddComponentParameter(1, nameof(PropertyColumn<GallerySample, string>.Property),
                    (System.Linq.Expressions.Expression<Func<GallerySample, string>>)(x => x.Name));
                builder.CloseComponent();
            }));

        table.FindComponents<AppSearchField>().Should().ContainSingle();
        table.FindAll(".app-search-field.app-table-search input").Should().ContainSingle();
    }
}
