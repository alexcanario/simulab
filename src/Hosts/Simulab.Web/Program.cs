using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Simulab.Identity.Contracts;
using Simulab.ServiceDefaults;
using Simulab.Web.Components;
using Simulab.Web.Components.Ui;
using Simulab.Web.Localization;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// F-64 BR4: the key ring of a deployed host lives in a blob container, not in the container's file system.
builder.AddAppDataProtection("simulab-web");
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddUiKit();
builder.Services.AddLocalization();
builder.Services.AddSingleton(TimeProvider.System);

// First typed client (F-4). The base address comes from service discovery: no host or port in the code.
builder.Services.AddHttpClient<IdentityApiClient>(client => client.BaseAddress = new Uri("https+http://api"));
// F-33: the Catalog module's own typed client, same base address from service discovery.
builder.Services.AddHttpClient<CatalogApiClient>(client => client.BaseAddress = new Uri("https+http://api"));
// F-41 (v2): the Development-only diagnostics route. The client is registered everywhere; the route it
// calls is mapped only in Development, and the page that uses it renders only there (BR9).
builder.Services.AddHttpClient<AiApiClient>(client => client.BaseAddress = new Uri("https+http://api"));
builder.Services.AddScoped<ProfileLanguageSaver>();
builder.Services.AddScoped<VisitorContext>();
builder.Services.AddScoped<UserTimeZone>();
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
// F-94 BR6: a host without its client id or secret refuses to start and names the key.
builder.Services.AddOpenIddictClientOptions(builder.Configuration);
var authentication = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
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
builder.Services.AddScoped<SignInHandOff>();

// F-20 BR1: Google sign-in exists only while it is on. This host owns the round trip because it is the only one the
// browser reaches; the Api still checks the ID token itself (BR2).
var google = GoogleSignInSettings.From(builder.Configuration);
builder.Services.AddSingleton(google);
if (google.Enabled)
{
    authentication
        .AddCookie(GoogleSignInSettings.ExternalScheme, options =>
        {
            // Holds Google's answer only between the callback and /account/google/complete, which clears it.
            options.Cookie.Name = "simulab.google";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        })
        .AddOpenIdConnect(GoogleSignInSettings.Scheme, options =>
        {
            options.Authority = GoogleSignInSettings.Authority;
            options.ClientId = google.ClientId;
            options.ClientSecret = google.ClientSecret;
            options.ResponseType = "code";
            options.CallbackPath = GoogleSignInSettings.CallbackPath;
            options.SignInScheme = GoogleSignInSettings.ExternalScheme;
            options.SaveTokens = true;
            options.MapInboundClaims = false;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("email");
            options.Scope.Add("profile");
            options.Events.OnRemoteFailure = context =>
            {
                // The visitor said no at Google, or the round trip broke: back to sign-in, never an error page.
                var denied = context.Request.Query["error"] == "access_denied";
                context.Response.Redirect(denied ? "/sign-in" : $"/sign-in?error={IdentityErrorCodes.GoogleSignInExpired}");
                context.HandleResponse();
                return Task.CompletedTask;
            };
        });
}

// Always registered: the pages that read them decide by the switch, and an empty store costs nothing.
builder.Services.AddSingleton<GoogleSignUpTickets>();
// F-29 BR2: the link attempts in flight on this host.
builder.Services.AddSingleton<GoogleLinkTickets>();
builder.Services.AddSingleton<CodeStepTickets>();
// F-6, BR5-BR7: a policy per permission claim. UI comfort only - the Api still enforces every call.
builder.Services.AddAuthorization(options =>
{
    // F-33 BR2: one policy per permission every module declares, not only Identity's.
    foreach (var permission in WebPermissions.All)
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

// B-4 BR5, F-64 BR6: X-Forwarded-For and X-Forwarded-Proto only from the proxies the configuration lists (none in dev).
var forwarded = new ForwardedHeadersOptions();
var behindTrustedProxy = TrustedProxies.Configure(forwarded, builder.Configuration);

var app = builder.Build();

if (behindTrustedProxy)
{
    app.UseUnlistedSenderLog(forwarded);
    app.UseForwardedHeaders(forwarded);
}
else if (!app.Environment.IsDevelopment())
{
    app.UseUnlistedProxyLog();
}

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
if (google.Enabled)
{
    app.MapGoogleAccountEndpoints();
}

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

