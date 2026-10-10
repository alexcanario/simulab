using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Simulab.QuestionBank.Api;

/// <summary>
/// The QuestionBank module's routes (F-95). The host calls this once, under <c>/api/v1</c>.
/// </summary>
public static class QuestionBankEndpoints
{
    public static IEndpointRouteBuilder MapQuestionBankEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/question-bank").WithTags("QuestionBank");

        group.MapQuestionEndpoints();

        return endpoints;
    }
}
