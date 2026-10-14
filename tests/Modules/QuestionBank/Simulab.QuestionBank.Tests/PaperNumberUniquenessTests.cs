using System.Net;
using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Tests;

/// <summary>F-96 BR3, AC6: within one edition a live question's number is unique; a deleted draft frees it.</summary>
public sealed class PaperNumberUniquenessTests : OriginApiTests
{
    [Fact]
    public async Task Create_WithTheNumberOfALiveQuestionOfTheSameEdition_IsRefusedWithPaperNumberTaken()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 12));
        var marker = Marker();

        var response = await CreateAsync(admin, FromEdition(topic.Id, edition.Id, 12, statement: $"<p>{marker}</p>"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionPaperNumberTaken);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithTheSameNumberInAnotherEdition_IsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var first = await EditionAsync(admin);
        var second = await EditionAsync(admin);
        await CreatedAsync(admin, FromEdition(topic.Id, first.Id, 12));

        var created = await CreatedAsync(admin, FromEdition(topic.Id, second.Id, 12));

        created.Origin!.PaperNumber.Should().Be(12);
    }

    [Fact]
    public async Task Create_AfterTheDraftWithThatNumberIsDeleted_IsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var first = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 12));
        (await admin.DeleteAsync($"{Questions}/{first.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var created = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 12));

        created.Origin!.PaperNumber.Should().Be(12);
    }

    [Fact]
    public async Task Update_ToTheNumberOfAnotherLiveQuestion_IsRefusedWithPaperNumberTaken()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 1));
        var second = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 2));

        var response = await UpdateAsync(admin, second.Id, FromEdition(topic.Id, edition.Id, 1));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionPaperNumberTaken);
        (await GetAsync(admin, second.Id)).Origin!.PaperNumber.Should().Be(2);
    }

    // The question keeps its own number when it is saved again: it does not collide with itself.
    [Fact]
    public async Task Update_KeepingItsOwnNumber_Succeeds()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var created = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 8));

        var updated = await UpdatedAsync(
            admin, created.Id, FromEdition(topic.Id, edition.Id, 8, statement: "<p>Outro enunciado</p>"));

        updated.Origin!.PaperNumber.Should().Be(8);
    }
}
