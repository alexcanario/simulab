using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.Catalog.Api;

/// <summary>
/// The notice PDF of an edition (F-89): upload, removal and the curator's download, under the exams group and so under
/// <c>catalog.manage</c> (BR6); and the student's download under <c>catalog.browse</c>. No answer carries a storage
/// address (BR8): the file is streamed by the Api, after the permission and the edition's status are checked.
/// </summary>
internal static class NoticeDocumentEndpoints
{
    // The 20 MB of the file plus the multipart envelope (BR4): a body past this is cut by the server before it is buffered.
    private const long RequestLimit = CatalogLimits.ExamEditionNoticeDocumentMaxBytes + (64 * 1024);

    private const string PdfContentType = "application/pdf";

    internal static RouteGroupBuilder MapNoticeDocumentEndpoints(this RouteGroupBuilder exams)
    {
        ArgumentNullException.ThrowIfNull(exams);

        exams.MapPut("/{examId:guid}/editions/{id:guid}/notice-document", UploadAsync).WithName("UploadExamEditionNoticeDocument");
        exams.MapDelete("/{examId:guid}/editions/{id:guid}/notice-document", RemoveAsync).WithName("RemoveExamEditionNoticeDocument");
        exams.MapGet("/{examId:guid}/editions/{id:guid}/notice-document", DownloadAsync).WithName("DownloadExamEditionNoticeDocument");

        return exams;
    }

    /// <summary>The student's download (UC4): only a published edition's file; every other case is the same not found.</summary>
    internal static async Task<IResult> DownloadForStudentAsync(
        Guid examId,
        Guid id,
        OpenNoticeDocumentHandler handler,
        CancellationToken cancellationToken) =>
        await OpenAsync(handler, examId, id, publishedOnly: true, cancellationToken);

    // The body is read by hand, not bound, so that every way a file can be wrong has a code: a missing part, a body past
    // the limit and a malformed form all answer a problem details, never a bare 400 or 413.
    private static async Task<IResult> UploadAsync(
        Guid examId,
        Guid id,
        HttpRequest request,
        SaveNoticeDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        if (request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
        {
            limit.MaxRequestBodySize = RequestLimit;
        }

        IFormFile? file;
        try
        {
            file = request.HasFormContentType
                ? (await request.ReadFormAsync(cancellationToken)).Files.GetFile("file")
                : null;
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return Problem(new Error(CatalogErrorCodes.ExamEditionNoticeDocumentTooLarge, ErrorKind.Validation));
        }
        catch (InvalidDataException)
        {
            return Problem(new Error(CatalogErrorCodes.ExamEditionNoticeDocumentNotPdf, ErrorKind.Validation));
        }

        if (file is null)
        {
            return Problem(new Error(CatalogErrorCodes.ExamEditionNoticeDocumentNotPdf, ErrorKind.Validation));
        }

        await using var content = file.OpenReadStream();
        var result = await handler.HandleAsync(examId, id, file.FileName, content, file.Length, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> RemoveAsync(
        Guid examId,
        Guid id,
        RemoveNoticeDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(examId, id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }

    private static async Task<IResult> DownloadAsync(
        Guid examId,
        Guid id,
        OpenNoticeDocumentHandler handler,
        CancellationToken cancellationToken) =>
        await OpenAsync(handler, examId, id, publishedOnly: false, cancellationToken);

    private static async Task<IResult> OpenAsync(
        OpenNoticeDocumentHandler handler,
        Guid examId,
        Guid id,
        bool publishedOnly,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(examId, id, publishedOnly, cancellationToken);

        return result.IsSuccess
            ? Results.File(result.Value.Content, PdfContentType, result.Value.FileName)
            : Problem(result.Error!);
    }
}
