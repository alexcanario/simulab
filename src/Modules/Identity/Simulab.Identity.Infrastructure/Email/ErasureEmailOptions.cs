namespace Simulab.Identity.Infrastructure.Email;

/// <summary>Where the farewell email points (F-10 BR12). Bound from configuration section <c>Identity</c>.</summary>
public sealed class ErasureEmailOptions
{
    public const string SectionName = "Identity";

    /// <summary>Absolute address of the sign-up page, offered because the erased address is free again (BR7).</summary>
    public string SignUpUrl { get; set; } = "https://localhost:7125/sign-up";
}
