using System.Text.Json;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Infrastructure.Account;

/// <summary>The <c>account.erased</c> job (F-59): its type and its payload, which carries no personal data.</summary>
public static class AccountErasedJob
{
    /// <summary>The value stored in the job's type column.</summary>
    public const string Type = "account.erased";

    public static string Serialize(Guid userId, DateTimeOffset erasedAt) =>
        JsonSerializer.Serialize(new Payload(userId, erasedAt), AppJson.Options);

    public static (Guid UserId, DateTimeOffset ErasedAt) Deserialize(string payload)
    {
        var parsed = JsonSerializer.Deserialize<Payload>(payload, AppJson.Options)
            ?? throw new InvalidOperationException("The payload of an account.erased job is not a payload.");
        return (parsed.UserId, parsed.ErasedAt);
    }

    private sealed record Payload(Guid UserId, DateTimeOffset ErasedAt);
}
