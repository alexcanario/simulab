using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Simulab.Identity.Tests;

/// <summary>F-11 AC14: with the feature on, the Api does not start without a valid encryption key (BR3).</summary>
public sealed class TotpKeyValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bm90LWEtMjU2LWJpdC1rZXk=")] // base64, but 15 bytes.
    [InlineData("not base64 at all")]
    public async Task Start_FeatureOnWithoutAValidKey_FailsNamingTheSetting(string? key)
    {
        using var factory = new IdentityApiFactory();
        await factory.PrepareAsync($"totp_key_{Guid.NewGuid():N}");
        factory.ConfigureHost = builder =>
        {
            builder.UseSetting("Identity:TotpEnabled", "true");
            builder.UseSetting("Identity:TotpEncryptionKey", key);
        };

        var start = () => factory.CreateClient();

        start.Should().Throw<OptionsValidationException>().WithMessage("*Identity:TotpEncryptionKey*");
    }

    [Fact]
    public async Task Start_FeatureOffWithoutAKey_Starts()
    {
        using var factory = new IdentityApiFactory();
        await factory.PrepareAsync($"totp_key_off_{Guid.NewGuid():N}");
        factory.ConfigureHost = builder =>
        {
            builder.UseSetting("Identity:TotpEnabled", "false");
            builder.UseSetting("Identity:TotpEncryptionKey", string.Empty);
        };

        using var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
