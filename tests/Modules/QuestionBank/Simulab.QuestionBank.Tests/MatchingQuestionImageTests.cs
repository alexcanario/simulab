using System.Net;
using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-106 AC11, BR7 (system-design R2, R3): a picture in a left item belongs to the question like one in an option,
/// and a published question's version keeps its file when the text drops it.
/// </summary>
public sealed class MatchingQuestionImageTests : QuestionImageApiTests
{
    private static SaveQuestionRequest Matching(Guid topicId, string firstLeft) =>
        new(
            topicId,
            "pt-BR",
            "<p>Associe as figuras.</p>",
            Type: QuestionType.Matching,
            Options: [new("<p>Círculo</p>"), new("<p>Quadrado</p>")],
            MatchItems: [new(firstLeft, 0), new("<p>Quatro lados</p>", 1)]);

    [Fact]
    public async Task Save_APictureInALeftItem_IsOwnedByTheQuestion()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);

        var question = await CreatedAsync(curator, Matching(topic.Id, $"<p>Redondo</p>{Img(image.Id, "A circle")}"));

        var row = (await RowAsync(image.Id))!;
        row.OwnerKind.Should().Be("Question");
        row.OwnerId.Should().Be(question.Id);
    }

    [Fact]
    public async Task Save_APublishedMatchingWithoutThePictureOfALeftItem_KeepsTheFileBecauseTheVersionHoldsIt()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        var draft = await CreatedAsync(curator, Matching(topic.Id, $"<p>Redondo</p>{Img(image.Id, "A circle")}"));
        (await curator.PostAsync($"{Questions}/{draft.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await PutQuestionAsync(curator, draft.Id, Matching(topic.Id, "<p>Redondo</p>"));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Files.Names.Should().ContainSingle();
        (await RowAsync(image.Id))!.IsDeleted.Should().BeFalse();
    }
}
