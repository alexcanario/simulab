namespace Simulab.Email.Tests;

/// <summary>An address in a captured message.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Address">Email address.</param>
public sealed record MailpitAddress(string Name, string Address);
