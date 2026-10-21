using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Application.Images;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.QuestionBank.Api;

/// <summary>
/// The pictures of the question texts (F-103): the upload, under <c>questions.manage</c> (BR12), and the download, for
/// <c>questions.manage</c> or <c>questions.review</c>. No answer carries a storage address (BR12): the Api streams the file,
/// with the headers that keep a stored file from running in the app's origin (BR13).
/// </summary>
internal static class QuestionImageEndpoints
{
    // The 2 MB of the file plus the multipart envelope: a body past this is cut by the server before it is buffered.
    private const long RequestLimit = QuestionLimits.ImageMaxBytes + (64 * 1024);

    private const string SubjectClaim = "sub";

    internal static RouteGroupBuilder MapQuestionImageEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var images = group.MapGroup("/images");

        images.MapPost(string.Empty, UploadAsync)
            .RequireAuthorization(PermissionPolicy.NameFor(QuestionBankPermissions.Manage))
            .WithName("UploadQuestionImage");

        // Either permission reads; two policies on one route would both have to pass, so the endpoint asks.
        images.MapGet("/{id:guid}", DownloadAsync)
            .RequireAuthorization()
            .WithName("DownloadQuestionImage");

        return group;
    }

    // The body is read by hand, not bound, so that every way a file can be wrong has a code: a missing part, a body past
    // the limit and a malformed form all answer a problem details, never a bare 400 or 413.
    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        UploadQuestionImageHandler handler,
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
            return Problem(new Error(QuestionBankErrorCodes.QuestionImageTooLarge, ErrorKind.Validation));
        }
        catch (InvalidDataException)
        {
            return Problem(new Error(QuestionBankErrorCodes.QuestionImageNotAnImage, ErrorKind.Validation));
        }

        if (file is null)
        {
            return Problem(new Error(QuestionBankErrorCodes.QuestionImageNotAnImage, ErrorKind.Validation));
        }

        await using var content = file.OpenReadStream();
        var result = await handler.HandleAsync(content, file.Length, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/question-bank/images/{result.Value.Id}", result.Value)
            : Problem(result.Error!);
    }

    private static async Task<IResult> DownloadAsync(
        Guid id,
        ClaimsPrincipal user,
        HttpContext context,
        IPermissionQueryService permissions,
        OpenQuestionImageHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(SubjectClaim), out var userId)
            || !(await permissions.GetEffectivePermissionsAsync(userId, cancellationToken))
                .Any(permission => permission is QuestionBankPermissions.Manage or QuestionBankPermissions.Review))
        {
            return Problem(new Error(IdentityErrorCodes.Forbidden, ErrorKind.Forbidden));
        }

        var result = await handler.HandleAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return Problem(result.Error!);
        }

        // BR13: the stored type, never sniffed by the browser, and a stored file that cannot act as a page of this origin.
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.ContentSecurityPolicy = "sandbox";
        context.Response.Headers.CacheControl = "private";

        return Results.Stream(result.Value.Content, result.Value.ContentType);
    }
}
