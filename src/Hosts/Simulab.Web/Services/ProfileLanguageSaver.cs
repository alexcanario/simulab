using System.Security.Claims;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Services;

/// <summary>
/// F-8 BR6: when a signed-in user picks a language in the header switch, it becomes the preferred
/// language too. Best effort: the screen changes whatever the Api says, and the next sign-in applies
/// the profile value.
/// </summary>
public sealed class ProfileLanguageSaver(
    WebSessionTokenAccessor tokens,
    IdentityApiClient api,
    ILogger<ProfileLanguageSaver> logger)
{
    public async Task SaveAsync(ClaimsPrincipal user, string culture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var accessToken = await tokens.GetAccessTokenAsync(user, cancellationToken);
        var result = accessToken is null
            ? ApiResult.Failed<bool>(Components.Ui.ErrorText.UnexpectedCode)
            : await api.UpdatePreferredLanguageAsync(accessToken, culture, cancellationToken);

        if (!result.IsSuccess)
        {
            logger.LogWarning("The language switch could not save {Culture} as the preferred language ({Code}).", culture, result.ErrorCode);
        }
    }
}
