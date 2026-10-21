namespace Simulab.QuestionBank.Application.Images;

/// <summary>
/// Decides what an uploaded file is from its first bytes (F-103, BR2): the name, the extension and the content type the
/// client sent are never trusted. No imaging library: the signature is enough to refuse SVG, HTML and anything else.
/// </summary>
public static class QuestionImageFormat
{
    /// <summary>The most bytes <see cref="Detect"/> needs.</summary>
    public const int SignatureLength = 12;

    /// <summary>The content type of a PNG, JPEG, WebP or GIF file; null for anything else.</summary>
    public static string? Detect(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(PngSignature))
        {
            return "image/png";
        }

        if (header.StartsWith(JpegSignature))
        {
            return "image/jpeg";
        }

        if (header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8))
        {
            return "image/gif";
        }

        // RIFF, a four-byte size, then WEBP.
        return header.Length >= SignatureLength && header[..4].SequenceEqual("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8)
            ? "image/webp"
            : null;
    }

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];
}
