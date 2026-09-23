namespace Simulab.Web.Services.Auth;

/// <summary>The two-factor challenge a Google sign-in got from the Api (F-20 BR6), waiting for the code step of <c>/sign-in</c>.</summary>
public sealed record CodeStepTicket(string Challenge);
