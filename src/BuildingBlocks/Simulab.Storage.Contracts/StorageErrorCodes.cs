namespace Simulab.Storage.Contracts;

/// <summary>The stable codes the storage block answers with (F-89); the UI localizes by code.</summary>
public static class StorageErrorCodes
{
    /// <summary>The host has no file storage connection (Development only; any other environment refuses to start).</summary>
    public const string NotConfigured = "storage.not_configured";
}
