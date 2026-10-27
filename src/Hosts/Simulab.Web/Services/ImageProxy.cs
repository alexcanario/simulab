namespace Simulab.Web.Services;

/// <summary>
/// The part every picture route of the Web shares once it has the Api's answer: pass a failure on and stream a success with
/// the F-103 headers, so the hardening of the question bank's pictures and the student's pictures cannot drift apart.
/// </summary>
internal static class ImageProxy
{
    internal static async Task<IResult> StreamAsync(HttpResponseMessage response, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(context);

        context.Response.RegisterForDispose(response);

        if (!response.IsSuccessStatusCode)
        {
            // The Api's answer, unchanged: a 404 stays a 404 and a 403 a 403. A 401 means the session ended.
            return response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? Results.Challenge()
                : Results.StatusCode((int)response.StatusCode);
        }

        // F-103 BR13: the type the Api stored, never sniffed, and a stored file that cannot act as a page of this origin.
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.ContentSecurityPolicy = "sandbox";
        context.Response.Headers.CacheControl = "private";

        return Results.Stream(
            await response.Content.ReadAsStreamAsync(context.RequestAborted),
            response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }
}
