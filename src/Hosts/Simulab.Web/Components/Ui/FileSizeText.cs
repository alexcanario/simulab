using System.Globalization;

namespace Simulab.Web.Components.Ui;

/// <summary>The size of a file as a reader says it (F-89): bytes, KB or MB, one decimal, in the reader's culture.</summary>
public static class FileSizeText
{
    private const long Kilobyte = 1024;
    private const long Megabyte = 1024 * Kilobyte;

    public static string Format(long bytes, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        return bytes switch
        {
            >= Megabyte => $"{(bytes / (double)Megabyte).ToString("0.#", culture)} MB",
            >= Kilobyte => $"{(bytes / (double)Kilobyte).ToString("0.#", culture)} KB",
            _ => $"{bytes.ToString(culture)} B"
        };
    }
}
