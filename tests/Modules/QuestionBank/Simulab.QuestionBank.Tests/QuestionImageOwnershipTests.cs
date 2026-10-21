using System.Net;
using System.Net.Http.Json;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-103 through HTTP, on a real PostgreSQL: what a save of a question or a base text does with the images in its HTML —
/// it owns them (AC10, BR6), refuses another owner's and unknown ids (AC8), counts them (AC3), demands the alternative
/// text (AC11), strips what the sanitizer does not allow (AC7) and counts an image as content (AC12).
/// </summary>
public sealed class QuestionImageOwnershipTests : QuestionImageApiTests
{
    // AC10 (UC1, BR3, BR6): the statement and two options hold three images; the saved question owns all three.
    [Fact]
    public async Task Save_AQuestionWithAnImageInTheStatementAndInTwoOptions_StoresThemAndOwnsThemByTheQuestion()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var images = await UploadedManyAsync(curator, 3);

        var question = await CreatedAsync(curator, WithImages(
            topic.Id,
            $"<p>Look at the map:</p>{Img(images[0].Id, "The map")}",
            $"<p>North</p>{Img(images[1].Id, "An arrow")}",
            Img(images[2].Id, "Another arrow")));

        question.StatementHtml.Should().Contain($"/question-images/{images[0].Id}");
        question.Options.Select(option => option.TextHtml).Should().Contain(html => html.Contains(images[1].Id.ToString()))
            .And.Contain(html => html.Contains(images[2].Id.ToString()));
        foreach (var image in images)
        {
            var row = (await RowAsync(image.Id))!;
            row.OwnerKind.Should().Be("Question");
            row.OwnerId.Should().Be(question.Id);
        }
    }

    // AC10 (BR6): the base text owns its own images.
    [Fact]
    public async Task Save_ABaseTextWithAnImage_OwnsItByTheBaseText()
    {
        var curator = await CuratorAsync();
        var image = await UploadedAsync(curator);

        var text = await CreatedTextAsync(curator, $"<p>Read:</p>{Img(image.Id, "A chart")}");

        var row = (await RowAsync(image.Id))!;
        row.OwnerKind.Should().Be("BaseText");
        row.OwnerId.Should().Be(text.Id);
    }

    // BR6: an image the question already owns can be kept by the next save.
    [Fact]
    public async Task Save_AgainWithTheImageThatTheQuestionOwns_IsAccepted()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        var question = await CreatedAsync(curator, WithImages(topic.Id, $"<p>One</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"));

        var response = await PutQuestionAsync(curator, question.Id, WithImages(topic.Id, $"<p>Two</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await RowAsync(image.Id))!.OwnerId.Should().Be(question.Id);
    }

    // AC8 (T8, BR6): an image owned by question A named by question B is refused and B stays as it was.
    [Fact]
    public async Task Save_AQuestionNamingAnImageOfAnotherQuestion_AnswersImageNotFound_AndStaysUnchanged()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        var owner = await CreatedAsync(curator, WithImages(topic.Id, $"<p>A</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"));
        var other = await CreatedAsync(curator, WithImages(topic.Id, "<p>Original statement</p>", "<p>A</p>", "<p>B</p>"));

        var response = await PutQuestionAsync(curator, other.Id, WithImages(topic.Id, $"<p>Stolen</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"));

        await AssertProblemAsync(response, HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionImageNotFoundOnSave);
        (await GetAsync(curator, other.Id)).StatementHtml.Should().Be("<p>Original statement</p>");
        (await RowAsync(image.Id))!.OwnerId.Should().Be(owner.Id);
    }

    // AC8 (T8): a new question and a base text are refused the same way; so is an id that does not exist.
    [Fact]
    public async Task Save_ANewQuestionNamingAnImageOfAnotherQuestion_AnswersImageNotFound()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        await CreatedAsync(curator, WithImages(topic.Id, $"<p>A</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"));

        var response = await CreateAsync(curator, WithImages(topic.Id, $"<p>B</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"));

        await AssertProblemAsync(response, HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionImageNotFoundOnSave);
    }

    [Fact]
    public async Task Save_ABaseTextNamingAnImageOfAQuestion_AnswersImageNotFound()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        await CreatedAsync(curator, WithImages(topic.Id, $"<p>A</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"));

        var response = await PostTextAsync(curator, $"<p>Passage</p>{Img(image.Id)}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, QuestionBankErrorCodes.BaseTextImageNotFound);
    }

    [Fact]
    public async Task Save_ABaseTextNamingAnImageOfAnotherBaseText_AnswersImageNotFound()
    {
        var curator = await CuratorAsync();
        var image = await UploadedAsync(curator);
        await CreatedTextAsync(curator, $"<p>First</p>{Img(image.Id)}");

        var response = await PostTextAsync(curator, $"<p>Second</p>{Img(image.Id)}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, QuestionBankErrorCodes.BaseTextImageNotFound);
    }

    [Fact]
    public async Task Save_NamingAnImageThatDoesNotExist_AnswersImageNotFound_ForAQuestionAndForABaseText()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var missing = Guid.CreateVersion7();

        await AssertProblemAsync(
            await CreateAsync(curator, WithImages(topic.Id, $"<p>A</p>{Img(missing)}", "<p>A</p>", "<p>B</p>")),
            HttpStatusCode.NotFound,
            QuestionBankErrorCodes.QuestionImageNotFoundOnSave);
        await AssertProblemAsync(
            await PostTextAsync(curator, $"<p>A</p>{Img(missing)}"),
            HttpStatusCode.NotFound,
            QuestionBankErrorCodes.BaseTextImageNotFound);
    }

    // AC3 (T3, BR7): ten images in the statement and options together are accepted, eleven are refused.
    [Fact]
    public async Task Save_AQuestionWithTenImages_IsAccepted_AndWithElevenIsRefused_CountingTheOptionsToo()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var images = await UploadedManyAsync(curator, 11);
        string Group(int from, int count) => string.Concat(images.Skip(from).Take(count).Select(image => Img(image.Id)));

        var accepted = await CreateAsync(curator, WithImages(topic.Id, $"<p>S</p>{Group(0, 5)}", $"<p>A</p>{Group(5, 3)}", $"<p>B</p>{Group(8, 2)}"));
        var refused = await CreateAsync(curator, WithImages(topic.Id, $"<p>S</p>{Group(0, 5)}", $"<p>A</p>{Group(5, 3)}", $"<p>B</p>{Group(8, 3)}"));

        accepted.StatusCode.Should().Be(HttpStatusCode.Created, await accepted.Content.ReadAsStringAsync());
        await AssertProblemAsync(refused, HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionImagesTooMany);
    }

    [Fact]
    public async Task Save_ABaseTextWithTenImages_IsAccepted_AndWithElevenIsRefused()
    {
        var curator = await CuratorAsync();
        var images = await UploadedManyAsync(curator, 11);
        string Group(int count) => string.Concat(images.Take(count).Select(image => Img(image.Id)));

        var refused = await PostTextAsync(curator, $"<p>P</p>{Group(11)}");
        var accepted = await PostTextAsync(curator, $"<p>P</p>{Group(10)}");

        await AssertProblemAsync(refused, HttpStatusCode.BadRequest, QuestionBankErrorCodes.BaseTextImagesTooMany);
        accepted.StatusCode.Should().Be(HttpStatusCode.Created, await accepted.Content.ReadAsStringAsync());
    }

    // AC11 (BR4): no alt is refused, an empty alt (decorative) is accepted, an alt over 250 characters is refused.
    [Fact]
    public async Task Save_AQuestionImageWithNoAlt_AnswersAltRequired_WithAnEmptyAltIsAccepted_AndWithALongAltIsRefused()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);

        await AssertProblemAsync(
            await CreateAsync(curator, WithImages(topic.Id, $"<p>A</p>{Img(image.Id, alt: null)}", "<p>A</p>", "<p>B</p>")),
            HttpStatusCode.BadRequest,
            QuestionBankErrorCodes.QuestionImageAltRequired);
        await AssertProblemAsync(
            await CreateAsync(curator, WithImages(topic.Id, $"<p>A</p>{Img(image.Id, new string('a', 251))}", "<p>A</p>", "<p>B</p>")),
            HttpStatusCode.BadRequest,
            QuestionBankErrorCodes.QuestionImageAltTooLong);
        var decorative = await CreateAsync(curator, WithImages(topic.Id, $"<p>A</p>{Img(image.Id, string.Empty)}", "<p>A</p>", "<p>B</p>"));
        decorative.StatusCode.Should().Be(HttpStatusCode.Created, await decorative.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Save_ABaseTextImageWithNoAlt_AnswersAltRequired_WithAnEmptyAltIsAccepted_AndWithALongAltIsRefused()
    {
        var curator = await CuratorAsync();
        var image = await UploadedAsync(curator);

        await AssertProblemAsync(
            await PostTextAsync(curator, $"<p>A</p>{Img(image.Id, alt: null)}"),
            HttpStatusCode.BadRequest,
            QuestionBankErrorCodes.BaseTextImageAltRequired);
        await AssertProblemAsync(
            await PostTextAsync(curator, $"<p>A</p>{Img(image.Id, new string('a', 251))}"),
            HttpStatusCode.BadRequest,
            QuestionBankErrorCodes.BaseTextImageAltTooLong);
        var decorative = await PostTextAsync(curator, $"<p>A</p>{Img(image.Id, string.Empty)}");
        decorative.StatusCode.Should().Be(HttpStatusCode.Created, await decorative.Content.ReadAsStringAsync());
    }

    // AC7 (T7, BR5): through the Api the three foreign addresses are gone and the stored one keeps only src and alt.
    [Fact]
    public async Task Save_AStatementWithForeignAddressesAndAStoredImageWithAnEventAttribute_KeepsOnlyTheCleanStoredImage()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        var statement = "<p>Look</p><img src=\"https://x.example/y.png\" alt=\"a\"><img src=\"data:image/png;base64,iVBORw0KGgo=\" alt=\"a\">"
            + "<img src=\"javascript:alert(1)\" alt=\"a\">"
            + $"<img src=\"/question-images/{image.Id}\" alt=\"Chart\" onerror=\"steal()\">";

        var question = await CreatedAsync(curator, WithImages(topic.Id, statement, "<p>A</p>", "<p>B</p>"));

        question.StatementHtml.Should().Contain($"<img src=\"/question-images/{image.Id}\" alt=\"Chart\">")
            .And.NotContain("x.example").And.NotContain("data:").And.NotContain("javascript").And.NotContain("onerror");
    }

    [Fact]
    public async Task Save_AnOptionAndABaseTextWithForeignAddresses_DropThemToo()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var kept = await UploadedManyAsync(curator, 2);

        var question = await CreatedAsync(curator, WithImages(
            topic.Id, "<p>S</p>", $"<p>A</p><img src=\"https://x.example/y.png\" alt=\"a\">{Img(kept[0].Id)}", "<p>B</p>"));
        var text = await CreatedTextAsync(
            curator, $"<p>P</p><img src=\"https://x.example/y.png\" alt=\"a\"><img src=\"data:image/png;base64,AAAA\" alt=\"a\">{Img(kept[1].Id)}");

        question.Options[0].TextHtml.Should().Contain(kept[0].Id.ToString()).And.NotContain("x.example");
        text.TextHtml.Should().Contain(kept[1].Id.ToString()).And.NotContain("x.example").And.NotContain("data:");
    }

    // BR1: the explanation takes no image, whatever the HTML says.
    [Fact]
    public async Task Save_AnExplanationWithAStoredImage_DropsTheImage()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);

        var question = await CreatedAsync(
            curator,
            WithImages(topic.Id, "<p>S</p>", "<p>A</p>", "<p>B</p>") with { ExplanationHtml = $"<p>Because</p>{Img(image.Id)}" });

        question.ExplanationHtml.Should().Be("<p>Because</p>");
        (await RowAsync(image.Id))!.OwnerId.Should().BeNull("an image only the explanation named is still pending");
    }

    // AC12 (BR8): two options that hold only an image let the question be published.
    [Fact]
    public async Task Publish_AQuestionWhoseOptionsAreOnlyImages_IsAccepted()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var images = await UploadedManyAsync(curator, 2);
        var draft = await CreatedAsync(curator, WithImages(topic.Id, "<p>Which drawing is a circle?</p>", Img(images[0].Id, "A circle"), Img(images[1].Id, "A square")));

        var response = await curator.PostAsync($"{Questions}/{draft.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var published = (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
        published.Status.Should().Be(QuestionStatus.Published);
    }
}
