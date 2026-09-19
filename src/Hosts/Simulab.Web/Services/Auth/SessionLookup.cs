using Simulab.Identity.Contracts;

namespace Simulab.Web.Services.Auth;

/// <summary>The answer of <see cref="AuthClient.LookUpSessionAsync"/>; <paramref name="Session"/> is set only when <see cref="SessionLookupStatus.Alive"/>.</summary>
public sealed record SessionLookup(SessionLookupStatus Status, SessionInfoResponse? Session);
