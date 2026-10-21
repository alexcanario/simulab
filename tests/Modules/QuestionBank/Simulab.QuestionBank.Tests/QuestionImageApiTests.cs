using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulab.QuestionBank.Application.Images;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// What the image tests of F-103 share: the files in memory, real image bytes of each format, the multipart upload,
/// the image row as stored (read with SQL, so the tests do not depend on the entity's shape) and the HTML an editor
/// would hand over.
/// </summary>
public abstract class QuestionImageApiTests : QuestionReviewApiTests
{
    protected const string Images = "/api/v1/question-bank/images";
    protected const string BaseTexts = "/api/v1/question-bank/base-texts";

    /// <summary>The image files of the host under test.</summary>
    protected InMemoryQuestionImageFiles Files { get; } = new();

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IQuestionImageFiles>();
            services.AddSingleton<IQuestionImageFiles>(Files);
        });
    }

    protected sealed class ImageRow
    {
        public Guid Id { get; set; }

        public string BlobName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public string? OwnerKind { get; set; }

        public Guid? OwnerId { get; set; }

        public bool IsDeleted { get; set; }
    }

    /// <summary>The kinds of image the upload accepts, with the type the Api stores for each.</summary>
    public static TheoryData<string, string> Formats => new()
    {
        { "png", "image/png" },
        { "jpeg", "image/jpeg" },
        { "webp", "image/webp" },
        { "gif", "image/gif" }
    };

    /// <summary>A file of this format and size: the signature of the format, then filler.</summary>
    protected static byte[] ImageBytes(string format = "png", int size = 64)
    {
        var bytes = new byte[size];
        var header = format switch
        {
            "png" => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
            "jpeg" => [0xFF, 0xD8, 0xFF, 0xE0],
            "gif" => "GIF89a"u8.ToArray(),
            "webp" => [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBP"u8],
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
        header.CopyTo(bytes, 0);
        for (var index = header.Length; index < bytes.Length; index++)
        {
            bytes[index] = (byte)(index % 251);
        }

        return bytes;
    }

    protected static HttpRequestMessage UploadRequest(string fileName, byte[] bytes, string contentType = "image/png")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        var form = new MultipartFormDataContent { { file, "file", fileName } };

        return new HttpRequestMessage(HttpMethod.Post, Images) { Content = form };
    }

    protected static Task<HttpResponseMessage> UploadAsync(HttpClient client, byte[] bytes, string fileName = "figure.png", string contentType = "image/png") =>
        client.SendAsync(UploadRequest(fileName, bytes, contentType));

    protected static async Task<QuestionImageResponse> UploadedAsync(HttpClient client, string format = "png", int size = 64)
    {
        using var response = await UploadAsync(client, ImageBytes(format, size));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionImageResponse>(AppJson.Options))!;
    }

    protected static async Task<List<QuestionImageResponse>> UploadedManyAsync(HttpClient client, int count)
    {
        var images = new List<QuestionImageResponse>();
        for (var index = 0; index < count; index++)
        {
            images.Add(await UploadedAsync(client));
        }

        return images;
    }

    /// <summary>The row of this image, whether or not it was soft-deleted; null when there is none.</summary>
    protected async Task<ImageRow?> RowAsync(Guid id) =>
        (await QueryAsync(context => context.Database
            .SqlQuery<ImageRow>($"""
                select id as "Id", blob_name as "BlobName", content_type as "ContentType", owner_kind as "OwnerKind",
                       owner_id as "OwnerId", is_deleted as "IsDeleted"
                from question_bank.question_images where id = {id}
                """)
            .ToListAsync())).SingleOrDefault();

    /// <summary>An <c>img</c> the way the editor writes it; a null <paramref name="alt"/> leaves the attribute out.</summary>
    protected static string Img(Guid id, string? alt = "A figure") =>
        alt is null
            ? $"<img src=\"/question-images/{id}\">"
            : $"<img src=\"/question-images/{id}\" alt=\"{alt}\">";

    protected static SaveQuestionRequest WithImages(Guid topicId, string statement, params string[] options) =>
        new(
            topicId,
            "pt-BR",
            statement,
            null,
            null,
            null,
            [.. options.Select((html, index) => new SaveQuestionOptionRequest(html, index == 0))]);

    protected static Task<HttpResponseMessage> PutQuestionAsync(HttpClient client, Guid id, SaveQuestionRequest request) =>
        client.PutAsJsonAsync($"{Questions}/{id}", request, AppJson.Options);

    protected static Task<HttpResponseMessage> PostTextAsync(HttpClient client, string html) =>
        client.PostAsJsonAsync(BaseTexts, new SaveBaseTextRequest($"Texto {Marker()}", "pt-BR", html), AppJson.Options);

    protected static async Task<BaseTextResponse> CreatedTextAsync(HttpClient client, string html)
    {
        var response = await PostTextAsync(client, html);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<BaseTextResponse>(AppJson.Options))!;
    }

    protected static Task<HttpResponseMessage> PutTextAsync(HttpClient client, Guid id, string title, string html) =>
        client.PutAsJsonAsync($"{BaseTexts}/{id}", new SaveBaseTextRequest(title, "pt-BR", html), AppJson.Options);

    protected static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, body);
        CodeOf(body).Should().Be(code);
    }
}
