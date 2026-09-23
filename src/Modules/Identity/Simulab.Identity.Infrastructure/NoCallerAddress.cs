using Simulab.SharedKernel.Security;

namespace Simulab.Identity.Infrastructure;

/// <summary>
/// The fallback for a host that does not know the client's address (F-21, BR2): the job worker, a test that
/// calls a handler directly. The Api host registers the real one before this module's <c>TryAdd</c>.
/// </summary>
public sealed class NoCallerAddress : ICallerAddress
{
    public string? Value => null;
}
