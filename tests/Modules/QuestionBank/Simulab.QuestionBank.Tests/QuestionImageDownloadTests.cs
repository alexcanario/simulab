using System.Net;
using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Tests;

/// <summary>F-103 through HTTP: who reads an image (AC5), what the response carries (AC6) and an image that is not there.</summary>
public sealed class QuestionImageDownloadTests : QuestionImageApiTests
{
    // AC5 (T5, BR12): questions.manage reads, and gets the bytes it uploaded.
    [Fact]
    public async Task Download_ByAHolderOfManage_AnswersTheBytes()
    {
        var curator = await CuratorAsync();
        var image = await UploadedAsync(curator, "jpeg");

        using var response = await curator.GetAsync($"{Images}/{image.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(ImageBytes("jpeg"));
    }

    // AC5 (T5, BR12): questions.review alone reads too; the first any-of route of the module.
    [Fact]
    public async Task Download_ByAHolderOfReviewOnly_AnswersTheBytes()
    {
        var image = await UploadedAsync(await CuratorAsync(), "gif");
        var reviewer = await HoldingOnlyAsync(QuestionBankPermissions.Review);

        using var response = await reviewer.GetAsync($"{Images}/{image.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(ImageBytes("gif"));
    }

    // AC5 (T5): neither permission is a 403; no token is a 401.
    [Fact]
    public async Task Download_ByAStudent_Answers403()
    {
        var image = await UploadedAsync(await CuratorAsync());

        using var response = await (await StudentAsync()).GetAsync($"{Images}/{image.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Download_WithoutAToken_Answers401()
    {
        var image = await UploadedAsync(await CuratorAsync());

        using var response = await Client().GetAsync($"{Images}/{image.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // AC5 (T5, BR12): no response names the storage account or container.
    [Fact]
    public async Task Download_AnswerCarriesNoStorageAddress()
    {
        var curator = await CuratorAsync();
        var image = await UploadedAsync(curator);

        using var response = await curator.GetAsync($"{Images}/{image.Id}");

        var everything = string.Join(' ', response.Headers.Select(h => $"{h.Key}={string.Join(',', h.Value)}"))
            + string.Join(' ', response.Content.Headers.Select(h => $"{h.Key}={string.Join(',', h.Value)}"));
        everything.ToLowerInvariant().Should().NotContain("blob.core").And.NotContain("devstoreaccount").And.NotContain("question-images/");
        response.Headers.Location.Should().BeNull();
    }

    // AC6 (T6, BR13): stored content type, nosniff, sandbox, private, inline.
    [Fact]
    public async Task Download_AnswersTheStoredTypeAndTheHardeningHeaders()
    {
        var curator = await CuratorAsync();
        var image = await UploadedAsync(curator, "webp");

        using var response = await curator.GetAsync($"{Images}/{image.Id}");

        response.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("Content-Security-Policy").Should().Equal("sandbox");
        response.Headers.CacheControl!.Private.Should().BeTrue();
        response.Headers.CacheControl.NoStore.Should().BeFalse();
        response.Content.Headers.ContentDisposition.Should().BeNull("the picture is shown inline, never offered as a download");
    }

    // BR12: an id nobody uploaded is a 404 with the stable code.
    [Fact]
    public async Task Download_OfAnUnknownImage_AnswersNotFound()
    {
        var curator = await CuratorAsync();

        using var response = await curator.GetAsync($"{Images}/{Guid.CreateVersion7()}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionImageNotFound);
    }
}
