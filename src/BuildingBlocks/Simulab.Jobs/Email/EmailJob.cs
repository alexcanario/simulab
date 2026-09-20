using System.Text.Json;
using Simulab.Email;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Jobs.Email;

/// <summary>
/// The one job type the queue ships with (F-13 BR4): a message already written, in the recipient's own
/// language, by the request that decided to send it. The worker only hands it to <see cref="IEmailSender"/>,
/// so it never needs a localizer or a module's data.
/// </summary>
public static class EmailJob
{
    /// <summary>The value stored in <see cref="Job.Type"/>.</summary>
    public const string Type = "email.send";

    public static string Serialize(EmailMessage message) =>
        JsonSerializer.Serialize(message, AppJson.Options);

    public static EmailMessage Deserialize(string payload) =>
        JsonSerializer.Deserialize<EmailMessage>(payload, AppJson.Options)
        ?? throw new InvalidOperationException("The payload of an email job is not a message.");

    /// <summary>Stages an email on the caller's unit of work. It leaves when that unit of work is saved.</summary>
    public static void EnqueueEmail(this IJobQueue queue, EmailMessage message)
    {
        ArgumentNullException.ThrowIfNull(queue);
        queue.Enqueue(Type, Serialize(message));
    }
}
