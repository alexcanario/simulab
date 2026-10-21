using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>F-103 through HTTP: who may upload (AC1), what is accepted (AC2), the size (AC3) and the name it is stored under (AC4).</summary>
public sealed class QuestionImageUploadTests : QuestionImageApiTests
{
    // AC1 (T1): no token.
    [Fact]
    public async Task Upload_WithoutAToken_Answers401_AndStoresNothing()
    {
        using var response = await UploadAsync(Client(), ImageBytes());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        Files.Names.Should().BeEmpty();
    }

    // AC1 (T1): a signed-in account without questions.manage, a student and a reviewer who only reviews.
    [Fact]
    public async Task Upload_ByAStudent_Answers403_AndStoresNothing()
    {
        using var response = await UploadAsync(await StudentAsync(), ImageBytes());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Files.Names.Should().BeEmpty();
    }

    [Fact]
    public async Task Upload_ByAHolderOfReviewOnly_Answers403_AndStoresNothing()
    {
        var reviewer = await HoldingOnlyAsync(QuestionBankPermissions.Review);

        using var response = await UploadAsync(reviewer, ImageBytes());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Files.Names.Should().BeEmpty();
    }

    // AC2 (T2, BR2): a real image of each format is accepted, whatever the sent type says.
    [Theory]
    [MemberData(nameof(Formats))]
    public async Task Upload_OfEachAcceptedFormat_Answers201_WithTheTypeOfItsBytes(string format, string contentType)
    {
        var curator = await CuratorAsync();

        using var response = await UploadAsync(curator, ImageBytes(format), $"figure.{format}", "application/octet-stream");

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var image = (await response.Content.ReadFromJsonAsync<QuestionImageResponse>(AppJson.Options))!;
        image.ContentType.Should().Be(contentType);
        image.SizeBytes.Should().Be(64);
        image.Url.Should().Be($"/question-images/{image.Id}");
        response.Headers.Location!.ToString().Should().EndWith($"{Images}/{image.Id}");
        Files.Names.Should().ContainSingle();
        (await RowAsync(image.Id))!.OwnerId.Should().BeNull("an image no saved text holds yet is pending (BR3)");
    }

    // AC2 (T2, BR2): a file named a.png whose content is HTML, SVG or text is refused, whatever the sent type says.
    [Theory]
    [InlineData("<html><body><script>alert(1)</script></body></html>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>")]
    [InlineData("just some text pretending to be a picture")]
    [InlineData("")]
    public async Task Upload_OfAFileNamedPngThatIsNotAnImage_AnswersNotAnImage_AndStoresNothing(string content)
    {
        var curator = await CuratorAsync();

        using var response = await UploadAsync(curator, System.Text.Encoding.UTF8.GetBytes(content), "a.png", "image/png");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionImageNotAnImage);
        Files.Names.Should().BeEmpty();
    }

    // AC2: a request with no file part is the same refusal.
    [Fact]
    public async Task Upload_WithNoFilePart_AnswersNotAnImage()
    {
        var curator = await CuratorAsync();

        using var response = await curator.PostAsync(Images, new MultipartFormDataContent { { new StringContent("x"), "other" } });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionImageNotAnImage);
        Files.Names.Should().BeEmpty();
    }

    // AC3 (T3, BR2): exactly 2 MB is accepted.
    [Fact]
    public async Task Upload_OfExactlyTwoMegabytes_IsAccepted()
    {
        var curator = await CuratorAsync();

        var image = await UploadedAsync(curator, "png", (int)QuestionLimits.ImageMaxBytes);

        image.SizeBytes.Should().Be(QuestionLimits.ImageMaxBytes);
    }

    // AC3 (T3, BR2): one byte more is refused before it is stored.
    [Fact]
    public async Task Upload_OfOneByteOverTwoMegabytes_AnswersTooLarge_AndStoresNothing()
    {
        var curator = await CuratorAsync();

        using var response = await UploadAsync(curator, ImageBytes("png", (int)QuestionLimits.ImageMaxBytes + 1));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionImageTooLarge);
        Files.Names.Should().BeEmpty();
    }

    // AC4 (T4, BR13): the name the user chose appears nowhere in the stored name.
    [Fact]
    public async Task Upload_OfAFileNamedWithATraversal_StoresItUnderAGeneratedName()
    {
        var curator = await CuratorAsync();

        using var response = await UploadAsync(curator, ImageBytes(), "../../x.png");

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var image = (await response.Content.ReadFromJsonAsync<QuestionImageResponse>(AppJson.Options))!;
        var stored = Files.Names.Should().ContainSingle().Subject;
        stored.Should().MatchRegex(new Regex("^question-images/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$"));
        stored.Should().NotContain("x.png").And.NotContain("..");
        (await RowAsync(image.Id))!.BlobName.Should().Be(stored);
    }
}
