namespace Simulab.Web.Services.Auth;

/// <summary>
/// F-20 BR6: a Google sign-in that needs the two-factor code hands its challenge to the <c>/sign-in</c> code step. The
/// challenge itself never travels in the URL; it lives as long as the Api keeps it (F-11 BR9, five minutes).
/// </summary>
public sealed class CodeStepTickets(TimeProvider timeProvider)
    : SingleUseTickets<CodeStepTicket>(timeProvider, TimeSpan.FromMinutes(5));
