using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Simulab.Testing;

namespace Simulab.Storage.Tests;

/// <summary>
/// F-89 on a real blob emulator (Azurite): the round trip of the storage block, and the container being private
/// (AC13, BR8). One container for every test; each test works on a name of its own.
/// </summary>
public sealed class BlobFileStorageTests
{
    private static async Task<(BlobFileStorage Storage, BlobServiceClient Service)> OpenAsync()
    {
        var service = new BlobServiceClient(await AzuriteServer.ConnectionStringAsync());
        return (new BlobFileStorage(service), service);
    }

    private static string NewName() => $"tests/{Guid.CreateVersion7():N}.pdf";

    private static MemoryStream Bytes(string text) => new(System.Text.Encoding.UTF8.GetBytes(text));

    private static async Task<string> ReadAsync(Stream? stream)
    {
        stream.Should().NotBeNull();
        await using (stream)
        {
            using var reader = new StreamReader(stream!);
            return await reader.ReadToEndAsync();
        }
    }

    [Fact]
    public async Task Save_ThenOpenRead_ReturnsTheSameBytes()
    {
        var (storage, _) = await OpenAsync();
        var name = NewName();

        var saved = await storage.SaveAsync(name, Bytes("%PDF-1.7 hello"), "application/pdf");

        saved.IsSuccess.Should().BeTrue();
        (await ReadAsync(await storage.OpenReadAsync(name))).Should().Be("%PDF-1.7 hello");
    }

    [Fact]
    public async Task Save_TheSameNameAgain_ReplacesTheContent()
    {
        var (storage, _) = await OpenAsync();
        var name = NewName();
        await storage.SaveAsync(name, Bytes("first"), "application/pdf");

        var saved = await storage.SaveAsync(name, Bytes("second"), "application/pdf");

        saved.IsSuccess.Should().BeTrue();
        (await ReadAsync(await storage.OpenReadAsync(name))).Should().Be("second");
    }

    [Fact]
    public async Task Exists_IsTrueAfterSave_AndFalseAfterDelete()
    {
        var (storage, _) = await OpenAsync();
        var name = NewName();
        await storage.SaveAsync(name, Bytes("x"), "application/pdf");
        (await storage.ExistsAsync(name)).Should().BeTrue();

        var deleted = await storage.DeleteAsync(name);

        deleted.Should().BeTrue();
        (await storage.ExistsAsync(name)).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_AMissingFile_ReturnsFalseAndThrowsNothing()
    {
        var (storage, _) = await OpenAsync();

        var act = () => storage.DeleteAsync(NewName());

        (await act.Should().NotThrowAsync()).Subject.Should().BeFalse();
    }

    [Fact]
    public async Task OpenRead_AMissingFile_ReturnsNull()
    {
        var (storage, _) = await OpenAsync();

        (await storage.OpenReadAsync(NewName())).Should().BeNull();
    }

    // AC13 and BR8: the container is created with anonymous access off, and an anonymous request for a stored file is refused.
    [Fact]
    public async Task Container_IsPrivate_AndAnAnonymousRequestForAFileIsRefused()
    {
        var (storage, service) = await OpenAsync();
        var name = NewName();
        await storage.SaveAsync(name, Bytes("%PDF-1.7 secret"), "application/pdf");
        var container = service.GetBlobContainerClient(BlobFileStorage.ContainerName);

        var policy = await container.GetAccessPolicyAsync();
        using var anonymous = new HttpClient();
        using var response = await anonymous.GetAsync(container.GetBlobClient(name).Uri);

        BlobFileStorage.ContainerName.Should().Be("documents");
        policy.Value.BlobPublicAccess.Should().Be(PublicAccessType.None);
        response.IsSuccessStatusCode.Should().BeFalse();
    }
}
