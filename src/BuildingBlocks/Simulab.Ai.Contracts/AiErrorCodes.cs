namespace Simulab.Ai.Contracts;

/// <summary>The stable codes a caller of the gateway can act on (F-41).</summary>
public static class AiErrorCodes
{
    /// <summary>No API key is configured; nothing was called and nothing was recorded (BR4).</summary>
    public const string NotConfigured = "ai.not_configured";

    /// <summary>Nobody is signed in; a call is always charged to a user (BR2).</summary>
    public const string NoUser = "ai.no_user";

    /// <summary>The user's plan allows no more calls this month (BR3).</summary>
    public const string QuotaExceeded = "ai.quota_exceeded";

    /// <summary>The model answered with an error, or could not be reached (BR8).</summary>
    public const string CallFailed = "ai.call_failed";

    /// <summary>The request carried no prompt, so nothing was called.</summary>
    public const string PromptRequired = "ai.prompt_required";
}
