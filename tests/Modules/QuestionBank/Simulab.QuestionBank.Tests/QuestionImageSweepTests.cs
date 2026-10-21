using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Simulab.QuestionBank.Infrastructure.Images;

namespace Simulab.QuestionBank.Tests;

/// <summary>F-103 AC9 (T9, BR11): the sweep deletes a pending image older than 24 hours, file first and then the row, and nothing else.</summary>
public sealed class QuestionImageSweepTests : QuestionImageApiTests
{
    [Fact]
    public async Task Sweep_DeletesAPendingImageOlderThan24Hours_AndKeepsTheYoungOneAndTheOwnedOne()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var old = await UploadedAsync(curator);
        var owned = await UploadedAsync(curator);
        var question = await CreatedAsync(curator, WithImages(topic.Id, $"<p>S</p>{Img(owned.Id)}", "<p>A</p>", "<p>B</p>"));
        Factory.Clock.Advance(TimeSpan.FromHours(25));
        curator = await CuratorAsync();
        var young = await UploadedAsync(curator);
        var oldBlobName = (await RowAsync(old.Id))!.BlobName;
        var sweep = Factory.Services.GetRequiredService<QuestionImageSweep>();

        var removed = await sweep.RunAsync();

        removed.Should().Be(1);
        (await RowAsync(old.Id))!.IsDeleted.Should().BeTrue();
        Files.Names.Should().HaveCount(2).And.NotContain(oldBlobName);
        (await RowAsync(young.Id))!.IsDeleted.Should().BeFalse();
        (await RowAsync(owned.Id))!.IsDeleted.Should().BeFalse();
        (await RowAsync(owned.Id))!.OwnerId.Should().Be(question.Id);
        using var gone = await curator.GetAsync($"{Images}/{old.Id}");
        gone.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // BR11: an image that is not yet 24 hours old is left alone.
    [Fact]
    public async Task Sweep_LeavesAPendingImageYoungerThan24Hours()
    {
        var curator = await CuratorAsync();
        var pending = await UploadedAsync(curator);
        Factory.Clock.Advance(TimeSpan.FromHours(23));

        var removed = await Factory.Services.GetRequiredService<QuestionImageSweep>().RunAsync();

        removed.Should().Be(0);
        (await RowAsync(pending.Id))!.IsDeleted.Should().BeFalse();
        Files.Names.Should().ContainSingle();
    }
}
