using System.Net;
using System.Net.Http.Json;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Services;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>F-103: the typed client sends the picture as one multipart part named file, reads the stored picture's answer and the refusals by code.</summary>
public sealed class QuestionBankApiClientImageTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path, string? Authorization, string? PartName, string? PartFileName, byte[] Bytes)> Received { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? name = null;
            string? fileName = null;
            var bytes = Array.Empty<byte>();
            if (request.Content is MultipartFormDataContent form)
            {
                var part = form.Single();
                name = part.Headers.ContentDisposition!.Name?.Trim('"');
                fileName = part.Headers.ContentDisposition.FileName?.Trim('"');
                bytes = await part.ReadAsByteArrayAsync(cancellationToken);
            }

            Received.Add((request.Method, request.RequestUri!.AbsolutePath, request.Headers.Authorization?.ToString(), name, fileName, bytes));

            return answer(request);
        }
    }

    private static (QuestionBankApiClient Client, Handler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        var handler = new Handler(answer);

        return (new QuestionBankApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }), handler);
    }

    [Fact]
    public async Task UploadImage_PostsTheFileAsThePartNamedFile_WithTheToken_AndReadsTheStoredPicture()
    {
        var id = Guid.Parse("0199a3b4-1111-7222-8333-444455556666");
        var (client, handler) = Create(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new QuestionImageResponse(id, $"/question-images/{id}", "image/png", 3), options: AppJson.Options)
        });

        var result = await client.UploadImageAsync("token-1", "map.png", "image/png", [1, 2, 3]);

        result.Value.Should().Be(new QuestionImageResponse(id, $"/question-images/{id}", "image/png", 3));
        var call = handler.Received.Single();
        call.Method.Should().Be(HttpMethod.Post);
        call.Path.Should().Be("/api/v1/question-bank/images");
        call.Authorization.Should().Be("Bearer token-1");
        call.PartName.Should().Be("file");
        call.PartFileName.Should().Be("map.png");
        call.Bytes.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task UploadImage_WhenTheApiRefuses_ReturnsTheStableCode()
    {
        var (client, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(
                new Dictionary<string, object> { ["status"] = 400, ["code"] = QuestionBankErrorCodes.QuestionImageNotAnImage },
                options: AppJson.Options)
        });

        var result = await client.UploadImageAsync("token-1", "a.png", "image/png", [1]);

        result.Value.Should().BeNull();
        result.ErrorCode.Should().Be(QuestionBankErrorCodes.QuestionImageNotAnImage);
    }
}
