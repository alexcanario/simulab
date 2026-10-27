namespace Simulab.Web.Services;

/// <summary>
/// The Web's typed client for the ExamEngine endpoints (F-169). The base address comes from service discovery (rule:
/// api-contracts) and every call carries the visitor's language. The result carries the stable error code, never a
/// message: the page localizes by code.
/// </summary>
public sealed class ExamEngineApiClient(HttpClient http)
{
    private const string Base = "/api/v1/exam-engine/attempts";

    /// <summary>
    /// F-169 BR12: one picture of an attempt, streamed. The caller disposes the response; it reads the status, the type and
    /// the body itself, because the body is a file and not JSON.
    /// </summary>
    public Task<HttpResponseMessage> OpenImageAsync(
        string accessToken,
        Guid attemptId,
        Guid imageId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
