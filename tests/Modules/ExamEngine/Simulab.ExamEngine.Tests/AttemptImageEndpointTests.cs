using System.Net;
using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-169 BR12 through HTTP: a picture opens through an attempt only for its owner and only when a question version the
/// attempt fixed holds it (T3, T6), with the headers of F-103 (T4).
/// </summary>
public sealed class AttemptImageEndpointTests : AttemptContentApiTests
{
    private sealed record Paper(CatalogEdition Edition, Guid InStatement, Guid InBaseText, Guid InOption, Guid InDraft);

    // AC12 (Api), AC15: [sec] T4. The owner opens a picture of the statement, of the base text and of an option, each
    // with its stored type, nosniff, sandbox and private.
    [Fact]
    public async Task Image_HeldByAFixedVersion_OpensForTheOwner_WithTheStoredTypeAndTheHardeningHeaders()
    {
        var admin = await AdminAsync();
        var paper = await PaperAsync(admin);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, paper.Edition.Id);

        foreach (var imageId in new[] { paper.InStatement, paper.InBaseText, paper.InOption })
        {
            using var response = await ImageAsync(student, attempt.Id, imageId);

            response.StatusCode.Should().Be(HttpStatusCode.OK, imageId.ToString());
            (await response.Content.ReadAsByteArrayAsync()).Should().Equal(Png);
            response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
            response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
            response.Headers.GetValues("Content-Security-Policy").Should().Equal("sandbox");
            response.Headers.CacheControl!.Private.Should().BeTrue();
        }
    }

    // AC15: [sec] T3. A picture of a draft question of the same edition, and an id that is no picture, answer 404.
    [Fact]
    public async Task Image_NotHeldByAFixedVersionOfTheAttempt_Answers404()
    {
        var admin = await AdminAsync();
        var paper = await PaperAsync(admin);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, paper.Edition.Id);

        foreach (var imageId in new[] { paper.InDraft, Guid.CreateVersion7() })
        {
            using var response = await ImageAsync(student, attempt.Id, imageId);

            CodeOf(await BodyAsync(response, HttpStatusCode.NotFound)).Should().Be(ExamAttemptErrorCodes.NotFound);
        }

        using var held = await ImageAsync(student, attempt.Id, paper.InStatement);
        held.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AC15: [sec] T3, T6. Another student and an Admin get 404 through A's attempt; an anonymous caller gets 401.
    [Fact]
    public async Task Image_ThroughAnotherStudentsAttempt_Answers404_EvenForAnAdmin_AndAnonymousGets401()
    {
        var admin = await AdminAsync();
        var paper = await PaperAsync(admin);
        var owner = await StudentAsync();
        var other = await StudentAsync();
        var attempt = await StartedAsync(owner, paper.Edition.Id);
        await StartedAsync(other, paper.Edition.Id);

        foreach (var reader in new[] { other, admin })
        {
            using var response = await ImageAsync(reader, attempt.Id, paper.InStatement);

            CodeOf(await BodyAsync(response, HttpStatusCode.NotFound)).Should().Be(ExamAttemptErrorCodes.NotFound);
        }

        using var anonymous = await ImageAsync(Client(), attempt.Id, paper.InStatement);
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var own = await ImageAsync(owner, attempt.Id, paper.InStatement);
        own.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // One published question with a picture in its statement, its base text and an option, and a draft question of the
    // same edition with a picture of its own.
    private static async Task<Paper> PaperAsync(HttpClient admin)
    {
        var topicId = await TopicAsync(admin);
        var edition = await EditionAsync(admin, publish: false);
        var inStatement = await UploadedAsync(admin);
        var inBaseText = await UploadedAsync(admin);
        var inOption = await UploadedAsync(admin);
        var inDraft = await UploadedAsync(admin);
        var baseTextId = await BaseTextAsync(admin, $"<p>Leia.</p>{Img(inBaseText)}");
        await PublishedAsync(admin, QuestionOfType(topicId, edition.Id, 1, AttemptQuestionType.SingleChoice) with
        {
            StatementHtml = $"<p>Observe.</p>{Img(inStatement)}",
            BaseTextId = baseTextId,
            Options = [new($"<p>Esta</p>{Img(inOption)}", true), new("<p>Lyon</p>"), new("<p>Nice</p>")]
        });
        var draft = await admin.PostAsync(
            Questions,
            System.Net.Http.Json.JsonContent.Create(
                QuestionOfType(topicId, edition.Id, 2, AttemptQuestionType.SingleChoice) with { StatementHtml = $"<p>Rascunho.</p>{Img(inDraft)}" },
                options: AppJson.Options));
        draft.StatusCode.Should().Be(HttpStatusCode.Created, await draft.Content.ReadAsStringAsync());
        await PublishEditionAsync(admin, edition);

        return new Paper(edition, inStatement, inBaseText, inOption, inDraft);
    }
}
