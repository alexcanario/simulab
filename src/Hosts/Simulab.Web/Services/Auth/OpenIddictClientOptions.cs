namespace Simulab.Web.Services.Auth;

/// <summary>The confidential client the Web authenticates as when it calls the token endpoint (F-5, decision 2).</summary>
public sealed class OpenIddictClientOptions
{
    public const string SectionName = "Authentication:OpenIddict";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}
