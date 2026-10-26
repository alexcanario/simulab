using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Simulab.QuestionBank.Infrastructure.Images;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-110 AC9 (BR8): the files of the three seeded images are written by the Api at start-up, right after the migrations,
/// from the bytes embedded in the module; a blob that is already there is never written again, and the file of a deleted
/// image row is not written. The test host turns the step on, which the shared host leaves off.
/// </summary>
public sealed class SeedQuestionImageFilesTests : QuestionImageApiTests
{
    // SHA-256 of the files the owner extracted from the paper (cartogram 1, cartogram 2, climogram).
    private static readonly Dictionary<Guid, string> Hashes = new()
    {
        [SeedManausGuardQuestionsTests.Id(14, 1)] = "c0f60a1957612e050858461148b7a5ddbc883318d6bf2673352859ad449c67f6",
        [SeedManausGuardQuestionsTests.Id(14, 2)] = "1c17f8168f1f190657e0e14e51d0699f4e153881faffd96ef805d0cb7e95055a",
        [SeedManausGuardQuestionsTests.Id(14, 3)] = "f0d374f16e3bf9533a480c49593ec50f65f1bd9f134c609cfae6562a2228c92e",
    };

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        base.ConfigureHost(builder);
        builder!.UseSetting("QuestionBank:SeedImagesOnStart", "true");
    }

    [Fact]
    public async Task Start_WritesTheThreeSeededImagesWithTheEmbeddedBytes()
    {
        _ = Client();

        Files.Names.Should().BeEquivalentTo(Hashes.Keys.Select(BlobName));
        foreach (var (id, hash) in Hashes)
        {
            (await HashAsync(BlobName(id))).Should().Be(hash, id.ToString());
        }
    }

    [Fact]
    public async Task ASecondStart_WritesNothing()
    {
        _ = Client();
        var before = await Task.WhenAll(Hashes.Keys.Select(id => HashAsync(BlobName(id))));

        var written = await Seeder().WriteMissingAsync();

        written.Should().Be(0);
        (await Task.WhenAll(Hashes.Keys.Select(id => HashAsync(BlobName(id))))).Should().Equal(before);
    }

    [Fact]
    public async Task AMissingBlob_IsWrittenAgain_ButNotTheBlobOfADeletedRow()
    {
        _ = Client();
        var deleted = SeedManausGuardQuestionsTests.Id(14, 1);
        var missing = SeedManausGuardQuestionsTests.Id(14, 2);
        await using (var connection = new NpgsqlConnection(Factory.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"UPDATE question_bank.question_images SET is_deleted = true, deleted_at = now() WHERE id = '{deleted}'", connection);
            (await command.ExecuteNonQueryAsync()).Should().Be(1);
        }

        await Files.DeleteAsync(BlobName(deleted));
        await Files.DeleteAsync(BlobName(missing));

        var written = await Seeder().WriteMissingAsync();

        written.Should().Be(1, "the live row's file is written again");
        Files.Names.Should().Contain(BlobName(missing)).And.NotContain(BlobName(deleted));
        (await HashAsync(BlobName(missing))).Should().Be(Hashes[missing]);
    }

    private SeedQuestionImageFiles Seeder() => Factory.Services.GetRequiredService<SeedQuestionImageFiles>();

    private static string BlobName(Guid id) => $"question-images/{id}";

    private async Task<string> HashAsync(string blobName)
    {
        await using var stream = await Files.OpenReadAsync(blobName);
        stream.Should().NotBeNull(blobName);

        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream!));
    }
}
