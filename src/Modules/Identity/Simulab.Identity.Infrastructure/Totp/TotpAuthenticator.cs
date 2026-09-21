using OtpNet;
using QRCoder;
using Simulab.Identity.Application.Totp;

namespace Simulab.Identity.Infrastructure.Totp;

/// <summary>
/// RFC 6238 with the parameters every authenticator app uses: SHA-1, six digits, 30-second steps. Imported from
/// Simulae's <c>TotpService</c> (F-11); the check now returns the matched step so the replay rule can use it (BR4).
/// </summary>
public sealed class TotpAuthenticator : ITotpAuthenticator
{
    public const string Issuer = "Simulab";

    private const int SecretBytes = 20; // 160 bits, as RFC 4226 recommends.
    private const int StepSeconds = 30;
    private const int Digits = 6;
    private const int QrPixelsPerModule = 5;

    public string GenerateSecret() => Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(SecretBytes));

    public string OtpAuthUri(string email, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var label = Uri.EscapeDataString($"{Issuer}:{email}");
        return $"otpauth://totp/{label}?secret={secret}&issuer={Uri.EscapeDataString(Issuer)}&digits={Digits}&period={StepSeconds}";
    }

    public string QrCodeDataUri(string otpAuthUri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(QrPixelsPerModule);
        return $"data:image/png;base64,{Convert.ToBase64String(png)}";
    }

    public long? MatchStep(string secret, string code, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var totp = new OtpNet.Totp(Base32Encoding.ToBytes(secret), step: StepSeconds, mode: OtpHashMode.Sha1, totpSize: Digits);
        return totp.VerifyTotp(at.UtcDateTime, code.Trim(), out var step, new VerificationWindow(previous: 1, future: 1))
            ? step
            : null;
    }
}
