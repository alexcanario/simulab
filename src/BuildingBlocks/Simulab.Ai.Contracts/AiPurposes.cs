namespace Simulab.Ai.Contracts;

/// <summary>
/// What a call is for. It is recorded on every row and it is the key a model override is configured
/// under (<c>Ai:Models:&lt;purpose&gt;</c>), so cost can be read per purpose and a purpose can move to
/// another model without touching code (F-41, BR7).
/// </summary>
public static class AiPurposes
{
    /// <summary>The development-only diagnostics page. The only purpose F-41 ships.</summary>
    public const string Diagnostics = "diagnostics";

    /// <summary>Longest purpose the column keeps.</summary>
    public const int MaxLength = 50;
}
