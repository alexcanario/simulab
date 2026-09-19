using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Security;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Application.Passwords;

/// <summary>Says whether a reset link still works, without using it (F-7: the page checks on load).</summary>
public sealed class CheckPasswordResetTokenHandler(IPasswordResetTokenStore tokenStore, TimeProvider timeProvider)
{
    public async Task<PasswordResetTokenStatus> HandleAsync(string? rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return PasswordResetTokenStatus.Invalid;
        }

        var token = await tokenStore.FindByHashAsync(SecureToken.Hash(rawToken), cancellationToken);
        if (token is null || token.IsConsumed)
        {
            return PasswordResetTokenStatus.Invalid;
        }

        return token.IsExpiredAt(timeProvider.GetUtcNow()) ? PasswordResetTokenStatus.Expired : PasswordResetTokenStatus.Valid;
    }
}
