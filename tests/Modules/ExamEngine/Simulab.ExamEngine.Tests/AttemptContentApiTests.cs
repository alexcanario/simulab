using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulab.Catalog.Contracts;
using Simulab.ExamEngine.Contracts;
using Simulab.QuestionBank.Application.Images;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Serialization;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// What the content and picture tests of F-169 share: picture files in memory, the upload of a PNG, a base text, a notice
/// subject of an edition, and the two new routes. Every picture the question bank stores lands in <see cref="Files"/>.
/// </summary>
public abstract class AttemptContentApiTests : ExamEngineApiTests
{
    protected const string Images = "/api/v1/question-bank/images";
    protected const string BaseTexts = "/api/v1/question-bank/base-texts";

    protected static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 7, 7, 7, 7, 7, 7, 7, 7];

    /// <summary>The picture files of the host under test.</summary>
    protected InMemoryImageFiles Files { get; } = new();

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IQuestionImageFiles>();
            services.AddSingleton<IQuestionImageFiles>(Files);
        });
    }

    /// <summary>An <c>img</c> the way the editor writes it.</summary>
    protected static string Img(Guid id) => $"<img src=\"/question-images/{id}\" alt=\"A figure\">";

    protected static async Task<Guid> UploadedAsync(HttpClient admin)
    {
        var file = new ByteArrayContent(Png);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        using var form = new MultipartFormDataContent { { file, "file", "figure.png" } };
        using var response = await admin.PostAsync(Images, form);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionImageResponse>(AppJson.Options))!.Id;
    }

    protected static async Task<Guid> BaseTextAsync(HttpClient admin, string html)
    {
        var response = await admin.PostAsJsonAsync(BaseTexts, new SaveBaseTextRequest($"Texto {Marker()}", "pt-BR", html), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<BaseTextResponse>(AppJson.Options))!.Id;
    }

    protected static async Task<Guid> NoticeSubjectAsync(HttpClient admin, CatalogEdition edition, string label)
    {
        var response = await admin.PostAsJsonAsync(
            $"/api/v1/catalog/exams/{edition.ExamId}/editions/{edition.Id}/notice-subjects",
            new SaveNoticeSubjectRequest(Label: label),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<NoticeSubjectResponse>(AppJson.Options))!.Id;
    }

    /// <summary>Creates the question from the request as a draft, then publishes it.</summary>
    protected static async Task<QuestionResponse> PublishedAsync(HttpClient admin, SaveQuestionRequest request)
    {
        var created = await admin.PostAsJsonAsync(Questions, request, AppJson.Options);
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var question = (await created.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
        var published = await admin.PostAsync($"{Questions}/{question.Id}/publish", null);
        published.StatusCode.Should().Be(HttpStatusCode.OK, await published.Content.ReadAsStringAsync());

        return question;
    }

    protected static Task<HttpResponseMessage> ContentAsync(HttpClient client, Guid attemptId) =>
        client.GetAsync($"{Attempts}/{attemptId}/content");

    protected static async Task<AttemptContentResponse> ReadContentAsync(HttpClient client, Guid attemptId)
    {
        var response = await ContentAsync(client, attemptId);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<AttemptContentResponse>(AppJson.Options))!;
    }

    protected static Task<HttpResponseMessage> ImageAsync(HttpClient client, Guid attemptId, Guid imageId) =>
        client.GetAsync($"{Attempts}/{attemptId}/images/{imageId}");

    /// <summary>Question picture files in memory, as the question bank's own tests keep them.</summary>
    protected sealed class InMemoryImageFiles : IQuestionImageFiles
    {
        private readonly ConcurrentDictionary<string, byte[]> _files = new();

        public async Task<Result> SaveAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            _files[blobName] = copy.ToArray();

            return Result.Success();
        }

        public Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(_files.TryGetValue(blobName, out var bytes) ? new MemoryStream(bytes) : null);

        public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
        {
            _files.TryRemove(blobName, out _);
            return Task.CompletedTask;
        }
    }
}
