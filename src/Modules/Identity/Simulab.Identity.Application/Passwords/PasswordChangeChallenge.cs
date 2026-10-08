using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Passwords;

/// <summary>
/// What a password-change challenge was issued for (F-53 BR6): the account, the security stamp it saw, and the
/// last factor the user proved, which the sign-in event records when the change completes (BR9).
/// </summary>
public sealed record PasswordChangeChallenge(Guid UserId, string? SecurityStamp, AccountEventMethod Method);
