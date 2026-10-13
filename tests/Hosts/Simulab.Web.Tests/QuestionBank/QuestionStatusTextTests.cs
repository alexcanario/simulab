using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Resources;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-101 (review finding 3): the words and the tone of a status are written over every value of the enum, so a
/// status added later cannot fall back to the draft's label and chip without this test going red
/// (rule: enums are read from <see cref="Enum.GetValues{TEnum}()"/>, never counted).
/// </summary>
public sealed class QuestionStatusTextTests : KitTestContext
{
    [Fact]
    public void EveryStatus_HasItsOwnName_AndOnlyTheDraftIsNeutral()
    {
        var localizer = Services.GetRequiredService<IStringLocalizer<SharedResources>>();
        var statuses = Enum.GetValues<QuestionStatus>();

        var names = statuses.ToDictionary(status => status, status => QuestionText.StatusName(localizer, status));

        names.Values.Should().OnlyHaveUniqueItems("a status that reads as another one's label is a missing arm");
        names.Values.Should().NotContain(name => string.IsNullOrWhiteSpace(name) || name.StartsWith("Questions.Status.", StringComparison.Ordinal));
        statuses.Where(status => QuestionText.StatusTone(status) == Simulab.Web.Components.Ui.AppStatusTone.Neutral)
            .Should().Equal(QuestionStatus.Draft);
        statuses.Select(QuestionText.StatusTone).Should().OnlyHaveUniqueItems();
    }
}
