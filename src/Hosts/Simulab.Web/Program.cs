using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Simulab.Identity.Contracts;
using Simulab.Web.Components;
using Simulab.Web.Components.Ui;
using Simulab.Web.Localization;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddUiKit();
builder.Services.AddLocalization();
builder.Services.AddSingleton(TimeProvider.System);

// First typed client (F-4). The base address comes from service discovery: no host or port in the code.
builder.Services.AddHttpClient<IdentityApiClient>(client => client.BaseAddress = new Uri("https+http://api"));
builder.Services.AddScoped<ProfileLanguageSaver>();
builder.Services.AddScoped<SignUpFlow>();

// Sign-in and sign-out (F-5). The cookie is what keeps a visitor signed in across page loads; it also
// carries the access and refresh tokens (never the browser, never JavaScript - see WebAuthClaims).
builder.Services.AddHttpClient<AuthClient>(client => client.BaseAddress = new Uri("https+http://api"));
builder.Services.AddSingleton<SignInTicketStore>();

// B-3: the tokens live on the server, one entry per signed-in browser; the cookie only points at it.
// Through the Aspire client integration (project rule): it trusts the local Redis container's TLS certificate.
builder.AddRedisClient("redis");
builder.Services.AddSingleton<IWebSessionStore, RedisWebSessionStore>();
builder.Services.AddSingleton<SessionRefreshGate>();
builder.Services.AddScoped<WebSessionTokenAccessor>();
builder.Services.AddScoped<AuthenticationStateProvider, SessionRevalidatingStateProvider>();
builder.Services.AddOptions<OpenIddictClientOptions>().Bind(builder.Configuration.GetSection(OpenIddictClientOptions.SectionName));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "simulab.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TokenLifetimes.RefreshToken;
        options.SlidingExpiration = false;
        options.LoginPath = "/sign-in";
        options.Events = new SessionCookieEvents();
    });
// F-6, BR5-BR7: a policy per permission claim. UI comfort only - the Api still enforces every call.
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in IdentityPermissions.All)
    {
        options.AddPolicy(PermissionPolicy.NameFor(permission), policy => policy.RequireClaim(WebAuthClaims.Permission, permission));
    }
});
builder.Services.AddCascadingAuthenticationState();

// Culture: cookie, then the browser, then en. For a signed-in user the cookie is written from the
// profile at sign-in and after each change (F-8 BR5), so the profile comes first without an Api call per page.
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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapDefaultEndpoints();
app.MapAccountEndpoints();

// Language switch: stores the culture in a cookie and returns to a page inside the app only.
// F-8 BR6: a signed-in user's choice is also saved as the preferred language (best effort), but only when
// the switch itself asked: a link from another site (the auth cookie is SameSite=Lax) changes the screen only.
app.MapGet("/culture/set", async (string culture, string? redirectUri, HttpContext context, ProfileLanguageSaver saver) =>
{
    if (SupportedCultures.IsSupported(culture))
    {
        CultureCookie.Write(context, culture);
        if (context.Request.Headers["Sec-Fetch-Site"] == "same-origin")
        {
            await saver.SaveAsync(context.User, culture, context.RequestAborted);
        }
    }

    return Results.LocalRedirect(SupportedCultures.SafeLocalPath(redirectUri));
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

