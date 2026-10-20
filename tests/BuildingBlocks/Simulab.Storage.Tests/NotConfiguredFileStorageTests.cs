namespace Simulab.Storage.Tests;

/// <summary>Open point 1 of F-89: a Development host without storage starts, saves answer a stable code, reads find nothing.</summary>
public sealed class NotConfiguredFileStorageTests
{
    private readonly NotConfiguredFileStorage _storage = new();

    [Fact]
    public async Task Save_AnswersTheNotConfiguredCode()
    {
        using var content = new MemoryStream([1, 2, 3]);

        var saved = await _storage.SaveAsync("a.pdf", content, "application/pdf");

        saved.IsFailure.Should().BeTrue();
        saved.Error!.Code.Should().Be(StorageErrorCodes.NotConfigured);
        StorageErrorCodes.NotConfigured.Should().Be("storage.not_configured");
    }

    [Fact]
    public async Task Read_Delete_Exists_FindNothingAndThrowNothing()
    {
        (await _storage.OpenReadAsync("a.pdf")).Should().BeNull();
        (await _storage.DeleteAsync("a.pdf")).Should().BeFalse();
        (await _storage.ExistsAsync("a.pdf")).Should().BeFalse();
    }
}
