using Simulab.Identity.Api;
using Simulab.SharedKernel.Security;

namespace Simulab.Api;

/// <summary>
/// The client address of the request being served (F-21, BR2), read the way the rate limiter reads it: the
/// visitor's address when the Web forwarded it with its client secret, the connection's otherwise (B-4).
/// Outside a request — a background job, a test that calls a handler directly — there is none.
/// </summary>
public sealed class HttpContextCallerAddress(IHttpContextAccessor httpContextAccessor, ClientAddress clientAddress) : ICallerAddress
{
    public string? Value => httpContextAccessor.HttpContext is { } context ? clientAddress.Of(context) : null;
}
