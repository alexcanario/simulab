using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Totp;

/// <summary>
/// The account a code step signed in, and which code it used (F-11 BR9). The token endpoint records the
/// method on the sign-in event (F-21 BR3).
/// </summary>
public sealed record TotpSignIn(User User, SecondFactorMethod Method);
