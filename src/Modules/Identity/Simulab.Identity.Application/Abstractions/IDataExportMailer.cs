namespace Simulab.Identity.Application.Abstractions;

/// <summary>The notice that the user's data was downloaded, in the recipient's own language (F-16 BR7).</summary>
public interface IDataExportMailer
{
    /// <summary>Stages the email on the caller's unit of work; the caller saves it.</summary>
    Task SendDataExportedAsync(string email, DateTimeOffset exportedAt, string locale, CancellationToken cancellationToken = default);
}
