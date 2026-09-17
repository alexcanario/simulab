using Microsoft.AspNetCore.Localization;
using Simulab.Web.Components;
using Simulab.Web.Components.Ui;
using Simulab.Web.Localization;
using Simulab.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddUiKit();
builder.Services.AddLocalization();
builder.Services.AddSingleton(TimeProvider.System);

// First typed client (F-4). The base address comes from service discovery: no host or port in the code.
builder.Services.AddHttpClient<IdentityApiClient>(client => client.BaseAddress = new Uri("https+http://api"));
builder.Services.AddScoped<SignUpFlow>();

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

// Dev-only pages (/dev/...) do not exist outside Development: the status code pages render Not found.
if (!app.Environment.IsDevelopment())
{
    app.Use((context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/dev", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }

        return next(context);
    });
}

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

