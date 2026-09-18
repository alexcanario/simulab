namespace Simulab.Identity.Tests;

/// <summary>The confidential client every test's token request authenticates as (F-5, decision 2).</summary>
public static class TestClient
{
    public const string ClientId = "simulab-web";

    public const string ClientSecret = "test-only-client-secret-not-a-real-value";
}
