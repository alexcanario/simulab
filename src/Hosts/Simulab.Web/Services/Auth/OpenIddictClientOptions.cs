using System.ComponentModel.DataAnnotations;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// The confidential client the Web authenticates as when it calls the token endpoint (F-5, decision 2). F-94 BR6: both
/// values are required and checked when the host starts, so a deploy that forgot one stops there with the key named
/// instead of answering every sign-in with a 401 <c>invalid_client</c> (the F-64 staging).
/// </summary>
public sealed class OpenIddictClientOptions
{
    public const string SectionName = "Authentication:OpenIddict";

    [Required(ErrorMessage = "Authentication:OpenIddict:ClientId is missing (docs/infra.md: the deploy sets it; Development has it in appsettings.Development.json).")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Authentication:OpenIddict:ClientSecret is missing (docs/infra.md: the deploy parameter openiddict-client-secret).")]
    public string ClientSecret { get; set; } = string.Empty;
}
