using System.Net;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-96 through HTTP: a question records the past edition it came from, its number in the paper and,
/// optionally, its notice subject (UC1, UC2, BR1 to BR6).
/// </summary>
public sealed class QuestionOriginEndpointTests : OriginApiTests
{
    // AC2
    [Fact]
    public async Task Create_WithEditionNumberAndNoticeSubject_StoresItAndReturnsTheOriginParts()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin, year: 2024, position: "Analista");
        var subject = await NoticeSubjectAsync(admin, edition, "Língua Portuguesa");
        await PublishEditionAsync(admin, edition);

        var created = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 12, subject.Id));

        created.Origin.Should().NotBeNull();
        created.Origin!.ExamEditionId.Should().Be(edition.Id);
        created.Origin.ExamId.Should().Be(edition.ExamId);
        created.Origin.ExamName.Should().Be(edition.ExamName);
        created.Origin.OrganizerAcronym.Should().Be(edition.OrganizerAcronym);
        created.Origin.OrganizerName.Should().Be(edition.OrganizerName);
        created.Origin.NoticeYear.Should().Be(2024);
        created.Origin.Position.Should().Be("Analista");
        created.Origin.PaperNumber.Should().Be(12);
        created.Origin.NoticeSubjectId.Should().Be(subject.Id);
        created.Origin.NoticeSubjectLabel.Should().Be("Língua Portuguesa");
        (await GetAsync(admin, created.Id)).Origin.Should().BeEquivalentTo(created.Origin);
    }

    // AC2 and BR4: the list carries the same origin, built from the edition on every read.
    [Fact]
    public async Task List_CarriesTheOriginOfAQuestionFromAnEdition()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var marker = Marker();
        await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 7, statement: $"<p>Lista {marker}</p>"));

        var page = await ListAsync(admin, $"?search={marker}");

        var item = page.Items.Should().ContainSingle().Subject;
        item.Origin.Should().NotBeNull();
        item.Origin!.ExamEditionId.Should().Be(edition.Id);
        item.Origin.PaperNumber.Should().Be(7);
        item.Origin.OrganizerAcronym.Should().Be(edition.OrganizerAcronym);
    }

    // AC3
    [Fact]
    public async Task Create_WithNoOriginFields_StoresAnAuthoredQuestion()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var marker = Marker();

        var authored = await CreatedAsync(admin, Complete(topic.Id, $"<p>Autoral {marker}</p>"));
        var fromEdition = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 1, statement: $"<p>Prova {marker}</p>"));

        // Only the question that was given an edition has an origin; the other one stays authored.
        fromEdition.Origin.Should().NotBeNull();
        authored.Origin.Should().BeNull();
        (await GetAsync(admin, authored.Id)).Origin.Should().BeNull();
        var items = (await ListAsync(admin, $"?search={marker}")).Items;
        items.Should().HaveCount(2);
        items.Single(item => item.Id == authored.Id).Origin.Should().BeNull();
        items.Single(item => item.Id == fromEdition.Id).Origin.Should().NotBeNull();
    }

    // AC4
    [Fact]
    public async Task Create_WithAnEditionAndNoNumber_IsRefusedWithPaperNumberRequired()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var marker = Marker();

        var response = await CreateAsync(admin, FromEdition(topic.Id, edition.Id, null, statement: $"<p>{marker}</p>"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionPaperNumberRequired);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    // AC4
    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    [InlineData(-3)]
    public async Task Create_WithANumberOutsideOneToFiveHundred_IsRefusedWithPaperNumberInvalid(int number)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);

        var response = await CreateAsync(admin, FromEdition(topic.Id, edition.Id, number));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionPaperNumberInvalid);
    }

    // AC4
    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    public async Task Create_WithTheBoundaryNumbers_IsStored(int number)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);

        var created = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, number));

        created.Origin!.PaperNumber.Should().Be(number);
    }

    // AC4
    [Fact]
    public async Task Create_WithANumberAndNoEdition_IsRefusedWithOriginRequiresEdition()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(admin, FromEdition(topic.Id, null, 5));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionOriginRequiresEdition);
    }

    // AC4
    [Fact]
    public async Task Create_WithANoticeSubjectAndNoEdition_IsRefusedWithOriginRequiresEdition()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var subject = await NoticeSubjectAsync(admin, edition);

        var response = await CreateAsync(admin, FromEdition(topic.Id, null, null, subject.Id));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionOriginRequiresEdition);
    }

    // AC5
    [Fact]
    public async Task Create_WithAnUnknownEdition_IsRefusedWithExamEditionNotFound()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(admin, FromEdition(topic.Id, Guid.CreateVersion7(), 1));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionExamEditionNotFound);
    }

    // AC5
    [Fact]
    public async Task Create_WithADeletedEdition_IsRefusedWithExamEditionNotFound()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        (await admin.DeleteAsync($"{CatalogExams}/{edition.ExamId}/editions/{edition.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await CreateAsync(admin, FromEdition(topic.Id, edition.Id, 1));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionExamEditionNotFound);
    }

    // AC5
    [Fact]
    public async Task Create_WithANoticeSubjectOfAnotherEdition_IsRefusedWithNoticeSubjectNotFound()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var other = await EditionAsync(admin);
        var foreign = await NoticeSubjectAsync(admin, other);
        var marker = Marker();

        var response = await CreateAsync(
            admin, FromEdition(topic.Id, edition.Id, 3, foreign.Id, statement: $"<p>{marker}</p>"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionNoticeSubjectNotFound);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    // AC5
    [Fact]
    public async Task Create_WithADeletedNoticeSubject_IsRefusedWithNoticeSubjectNotFound()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var subject = await NoticeSubjectAsync(admin, edition);
        (await admin.DeleteAsync(
                $"{CatalogExams}/{edition.ExamId}/editions/{edition.Id}/notice-subjects/{subject.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await CreateAsync(admin, FromEdition(topic.Id, edition.Id, 3, subject.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionNoticeSubjectNotFound);
    }

    // AC7
    [Fact]
    public async Task Update_OfAPublishedQuestion_ChangesTheNumberMovesTheEditionAndClearsTheOrigin()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var first = await EditionAsync(admin);
        var second = await EditionAsync(admin);
        var created = await CreatedAsync(admin, FromEdition(topic.Id, first.Id, 12));
        var published = await admin.PostAsync($"{Questions}/{created.Id}/publish", null);
        published.StatusCode.Should().Be(HttpStatusCode.OK);

        var renumbered = await UpdatedAsync(admin, created.Id, FromEdition(topic.Id, first.Id, 13));
        var moved = await UpdatedAsync(admin, created.Id, FromEdition(topic.Id, second.Id, 4));
        var authored = await UpdatedAsync(admin, created.Id, Complete(topic.Id));

        renumbered.Status.Should().Be(QuestionStatus.Published);
        renumbered.Origin!.PaperNumber.Should().Be(13);
        moved.Status.Should().Be(QuestionStatus.Published);
        moved.Origin!.ExamEditionId.Should().Be(second.Id);
        authored.Status.Should().Be(QuestionStatus.Published);
        authored.Origin.Should().BeNull();

        // The number the question held is free again for another one in the first edition.
        var next = await CreatedAsync(admin, FromEdition(topic.Id, first.Id, 13));
        next.Origin!.PaperNumber.Should().Be(13);
    }

    // AC7: the notice subject can be cleared without clearing the edition.
    [Fact]
    public async Task Update_ClearingOnlyTheNoticeSubject_KeepsTheEditionAndTheNumber()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var subject = await NoticeSubjectAsync(admin, edition);
        var created = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 9, subject.Id));

        var updated = await UpdatedAsync(admin, created.Id, FromEdition(topic.Id, edition.Id, 9));

        updated.Origin!.PaperNumber.Should().Be(9);
        updated.Origin.NoticeSubjectId.Should().BeNull();
        updated.Origin.NoticeSubjectLabel.Should().BeNull();
    }

    // AC8 and BR5: a published question may point at a draft edition.
    [Fact]
    public async Task PublishQuestion_FromADraftEdition_Succeeds_AndKeepsTheOrigin()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin, status: nameof(ExamEditionStatus.Draft));
        var created = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 2));

        var response = await admin.PostAsync($"{Questions}/{created.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var published = (await GetAsync(admin, created.Id));
        published.Status.Should().Be(QuestionStatus.Published);
        published.Origin!.ExamEditionId.Should().Be(edition.Id);
    }

    // AC13
    [Fact]
    public async Task Create_WithAnOriginAsACuratorWithNoCatalogPermission_Succeeds()
    {
        var admin = await AdminAsync();
        var curator = await CuratorAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);

        var created = await CreatedAsync(curator, FromEdition(topic.Id, edition.Id, 21));

        created.Origin!.PaperNumber.Should().Be(21);
    }

    // BR11 and AC-less rule: an edition removed behind the module's back leaves the ids and no names.
    [Fact]
    public async Task Get_WhenTheEditionNoLongerResolves_KeepsTheIdsAndHasNoNames()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var created = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 30));
        await QueryAsync(async context =>
        {
            var row = context.Questions.Single(question => question.Id == created.Id);
            context.Entry(row).Property(nameof(row.ExamEditionId)).CurrentValue = Guid.CreateVersion7();
            await context.SaveChangesAsync();
            return 0;
        });

        var read = await GetAsync(admin, created.Id);

        read.Origin.Should().NotBeNull();
        read.Origin!.PaperNumber.Should().Be(30);
        read.Origin.ExamName.Should().BeEmpty();
        read.Origin.NoticeYear.Should().BeNull();
    }
}
