using Microsoft.AspNetCore.Localization;
using MudBlazor.Services;
using Simulab.Web.Components;
using Simulab.Web.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddLocalization();

// Culture: cookie (set by the language switch), then the browser, then en.
// The user profile becomes the first source when Identity arrives.
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(SupportedCultures.Default);
    options.SupportedCultures = [.. SupportedCultures.All];
    options.SupportedUICultures = [.. SupportedCultures.All];
    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRequestLocalization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapDefaultEndpoints();

// Language switch: stores the culture in a cookie and returns to a page inside the app only.
app.MapGet("/culture/set", (string culture, string? redirectUri, HttpContext context) =>
{
    if (SupportedCultures.IsSupported(culture))
    {
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax, Secure = true });
    }

    return Results.LocalRedirect(SupportedCultures.SafeLocalPath(redirectUri));
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
