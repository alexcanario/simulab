using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Text;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-102 on the entity alone: what a base text is made of (BR1, AC1) and when its language may change (BR3, AC5).
/// The rules answer with a <see cref="Result"/>; a refused edit leaves the entity as it was.
/// </summary>
public sealed class BaseTextTests
{
    private static SanitizedHtml Text(string html = "<p>O rio corria devagar.</p>", string plain = "O rio corria devagar.") =>
        new(html, plain);

    private static BaseText Created(string title = "Texto I", string language = "pt-BR") =>
        BaseText.Create(title, language, Text()).Value!;

    // AC1, BR1: the title is trimmed, the language canonical, the title searchable without case or accents.
    [Fact]
    public void Create_ValidContent_TrimsTheTitle_CanonicalizesTheLanguage_AndDerivesTheSearchForm()
    {
        var result = BaseText.Create("  Fundação do Brasil  ", "pt-br", Text());

        result.IsSuccess.Should().BeTrue();
        var baseText = result.Value!;
        baseText.Title.Should().Be("Fundação do Brasil");
        baseText.NormalizedTitle.Should().Be(ComparableText.Normalize("Fundação do Brasil"));
        baseText.ContentLanguage.Should().Be("pt-BR");
        baseText.TextHtml.Should().Be("<p>O rio corria devagar.</p>");
    }

    // AC1, BR1: a blank title is refused; nothing is truncated.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankTitle_IsRefusedWithTitleRequired(string? title)
    {
        var result = BaseText.Create(title, "pt-BR", Text());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.BaseTextTitleRequired);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public void Create_TitleOfExactlyTheLimitAfterTrimming_IsAccepted_AndOneMoreIsRefused()
    {
        var atLimit = new string('a', QuestionLimits.BaseTextTitleMaxLength);

        BaseText.Create($"  {atLimit}  ", "pt-BR", Text()).IsSuccess.Should().BeTrue();
        BaseText.Create(atLimit + "a", "pt-BR", Text()).Error!.Code
            .Should().Be(QuestionBankErrorCodes.BaseTextTitleTooLong);
    }

    // AC1, BR1: the text must have a body once its tags are removed.
    [Fact]
    public void Create_TextWithNoBody_IsRefusedWithTextRequired()
    {
        var result = BaseText.Create("Texto I", "pt-BR", SanitizedHtml.Empty);

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.BaseTextTextRequired);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public void Create_TextOfExactlyTheLimit_IsAccepted_AndOneMoreIsRefusedWithTextTooLong()
    {
        var atLimit = new string('a', QuestionLimits.BaseTextMaxLength);

        BaseText.Create("Texto I", "pt-BR", new SanitizedHtml(atLimit, "a")).IsSuccess.Should().BeTrue();
        BaseText.Create("Texto I", "pt-BR", new SanitizedHtml(atLimit + "a", "a")).Error!.Code
            .Should().Be(QuestionBankErrorCodes.BaseTextTextTooLong);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("xx")]
    [InlineData("fr")]
    public void Create_ALanguageTheAppDoesNotKnow_IsRefusedWithContentLanguageInvalid(string? language)
    {
        var result = BaseText.Create("Texto I", language, Text());

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.BaseTextContentLanguageInvalid);
    }

    // AC5, BR3: the language of a text that questions use is locked; the same language is not a change.
    [Fact]
    public void Update_ChangingTheLanguage_WhileQuestionsLinkIt_IsRefusedWithLanguageInUse_AndNothingChanges()
    {
        var baseText = Created();

        var result = baseText.Update("Texto II", "en", Text("<p>The river.</p>", "The river."), hasLinkedQuestions: true);

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.BaseTextLanguageInUse);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
        baseText.Title.Should().Be("Texto I");
        baseText.ContentLanguage.Should().Be("pt-BR");
        baseText.TextHtml.Should().Be("<p>O rio corria devagar.</p>");
    }

    [Fact]
    public void Update_KeepingTheLanguage_WhileQuestionsLinkIt_ReplacesTitleAndText()
    {
        var baseText = Created();

        var result = baseText.Update("Texto II", "pt-br", Text("<p>Outro.</p>", "Outro."), hasLinkedQuestions: true);

        result.IsSuccess.Should().BeTrue();
        baseText.Title.Should().Be("Texto II");
        baseText.NormalizedTitle.Should().Be(ComparableText.Normalize("Texto II"));
        baseText.TextHtml.Should().Be("<p>Outro.</p>");
    }

    [Fact]
    public void Update_ChangingTheLanguage_WhenNoQuestionLinksIt_Changes()
    {
        var baseText = Created();

        var result = baseText.Update("Texto I", "en", Text(), hasLinkedQuestions: false);

        result.IsSuccess.Should().BeTrue();
        baseText.ContentLanguage.Should().Be("en");
    }

    // AC1: an invalid edit answers the shape error before the lock, and leaves the entity as it was.
    [Fact]
    public void Update_WithABlankTitleAndALanguageChange_AnswersTheShapeErrorFirst_AndNothingChanges()
    {
        var baseText = Created();

        var result = baseText.Update("  ", "en", Text(), hasLinkedQuestions: true);

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.BaseTextTitleRequired);
        baseText.ContentLanguage.Should().Be("pt-BR");
        baseText.Title.Should().Be("Texto I");
    }
}
