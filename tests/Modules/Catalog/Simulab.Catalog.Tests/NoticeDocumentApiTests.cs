using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>What the notice PDF tests of F-89 share: an exam with an edition, a PDF of any size, and the multipart upload.</summary>
public abstract class NoticeDocumentApiTests : CatalogApiTests
{
    protected const string Exams = "/api/v1/catalog/exams";
    protected const string Published = "/api/v1/catalog/published-exams";
    protected const long TwentyMegabytes = 20L * 1024 * 1024;

    private const string Organizers = "/api/v1/catalog/organizers";
    private const string Authorities = "/api/v1/catalog/issuing-authorities";

    protected static readonly int ThisYear = TimeProvider.System.GetUtcNow().Year;

    /// <summary>The notice PDFs of the host under test, in memory unless <see cref="UsesInMemoryFiles"/> is off.</summary>
    protected InMemoryNoticeDocumentFiles Files { get; } = new();

    /// <summary>Off for the one test class that runs the host with no storage connection at all.</summary>
    protected virtual bool UsesInMemoryFiles => true;

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!UsesInMemoryFiles)
        {
            return;
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<INoticeDocumentFiles>();
            services.AddSingleton<INoticeDocumentFiles>(Files);
        });
    }

    protected static string NoticeOf(Guid exam, Guid edition) => $"{Exams}/{exam}/editions/{edition}/notice-document";

    protected static string StudentNoticeOf(Guid exam, Guid edition) => $"{Published}/{exam}/editions/{edition}/notice-document";

    protected static string Unique(string name)
    {
        var value = $"{name} {Guid.CreateVersion7():N}";
        return value[..Math.Min(value.Length, 40)];
    }

    /// <summary>A PDF of this many bytes: the signature, then a body that is not the same byte twice in a row.</summary>
    protected static byte[] Pdf(long size)
    {
        var bytes = new byte[size];
        var header = "%PDF-1.7\n"u8;
        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] = index < header.Length ? header[index] : (byte)('a' + (index % 26));
        }

        return bytes;
    }

    protected static HttpRequestMessage Upload(string route, string fileName, byte[] bytes)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        var form = new MultipartFormDataContent { { file, "file", fileName } };

        return new HttpRequestMessage(HttpMethod.Put, route) { Content = form };
    }

    protected static async Task<ExamResponse> ExamAsync(HttpClient admin)
    {
        ArgumentNullException.ThrowIfNull(admin);

        var authority = await admin.PostAsJsonAsync(
            Authorities,
            new SaveIssuingAuthorityRequest(Unique("Orgao"), Guid.CreateVersion7().ToString("N")[..12]),
            AppJson.Options);
        authority.StatusCode.Should().Be(HttpStatusCode.Created, await authority.Content.ReadAsStringAsync());
        var authorityId = (await authority.Content.ReadFromJsonAsync<IssuingAuthorityResponse>(AppJson.Options))!.Id;

        var response = await admin.PostAsJsonAsync(
            Exams,
            new SaveExamRequest(authorityId, Unique("Exame"), nameof(AssessmentType.PublicServiceExam), nameof(ExamScope.National), null, "pt-BR"),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamResponse>(AppJson.Options))!;
    }

    protected static async Task<OrganizerResponse> BoardAsync(HttpClient admin)
    {
        ArgumentNullException.ThrowIfNull(admin);

        var response = await admin.PostAsJsonAsync(
            Organizers,
            new SaveOrganizerRequest(Unique("Banca"), Guid.CreateVersion7().ToString("N")[..12], nameof(OrganizerKind.ExamBoard)),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<OrganizerResponse>(AppJson.Options))!;
    }

    protected static SaveExamEditionRequest Fields(Guid board, string? status = null, string? url = null, int? year = null) =>
        new(board, year ?? ThisYear, null, null, url, null, status);

    protected static async Task<ExamEditionResponse> CreateEditionAsync(HttpClient admin, Guid exam, SaveExamEditionRequest request)
    {
        ArgumentNullException.ThrowIfNull(admin);

        var response = await admin.PostAsJsonAsync($"{Exams}/{exam}/editions", request, AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!;
    }

    protected static async Task<ExamEditionResponse> FindEditionAsync(HttpClient admin, Guid exam, Guid edition)
    {
        ArgumentNullException.ThrowIfNull(admin);

        return (await admin.GetFromJsonAsync<ExamEditionResponse>($"{Exams}/{exam}/editions/{edition}", AppJson.Options))!;
    }

    protected static async Task<ExamEditionResponse> UploadedAsync(HttpClient admin, Guid exam, Guid edition, string fileName, byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(admin);

        using var response = await admin.SendAsync(Upload(NoticeOf(exam, edition), fileName, bytes));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!;
    }

    /// <summary>An exam with one edition, as the curator would have it before attaching a file.</summary>
    protected static async Task<(ExamResponse Exam, OrganizerResponse Board, ExamEditionResponse Edition)> SetUpAsync(HttpClient admin, string? status = null)
    {
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var edition = await CreateEditionAsync(admin, exam.Id, Fields(board.Id, status));

        return (exam, board, edition);
    }
}
