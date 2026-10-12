using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-96 BR9, BR10, BR12: the four read routes the question form and the edition page use. They belong to
/// the question bank and need <c>questions.manage</c> only, never <c>catalog.manage</c>.
/// </summary>
public sealed class EditionLookupEndpointTests : OriginApiTests
{
    private static async Task<List<ExamEditionLookupResponse>> SearchAsync(HttpClient client, string search) =>
        (await client.GetFromJsonAsync<List<ExamEditionLookupResponse>>(
            $"{QuestionBank}/editions?search={Uri.EscapeDataString(search)}", AppJson.Options))!;

    // AC11
    [Fact]
    public async Task Search_FindsEditionsOfAnyStatus_ByExamNameAcronymOrYear()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var draft = await EditionAsync(admin, year: 2015, status: nameof(ExamEditionStatus.Draft), marker: marker);
        var published = await EditionAsync(admin, year: 2016, status: nameof(ExamEditionStatus.Published), marker: Marker());

        var byExam = await SearchAsync(admin, draft.ExamName);
        var byAcronym = await SearchAsync(admin, published.OrganizerAcronym);
        var byYear = await SearchAsync(admin, "2015");

        byExam.Should().ContainSingle().Which.Id.Should().Be(draft.Id);
        byExam[0].Status.Should().Be(nameof(ExamEditionStatus.Draft));
        byExam[0].OrganizerAcronym.Should().Be(draft.OrganizerAcronym);
        byExam[0].NoticeYear.Should().Be(2015);
        byAcronym.Should().ContainSingle().Which.Status.Should().Be(nameof(ExamEditionStatus.Published));
        byYear.Should().Contain(item => item.Id == draft.Id);
        byYear.Count.Should().BeLessThanOrEqualTo(20);
    }

    // AC11: search is case and accent insensitive, like the rest of the catalog.
    [Fact]
    public async Task Search_IgnoresCaseOfTheText()
    {
        var admin = await AdminAsync();
        var edition = await EditionAsync(admin);

        var found = await SearchAsync(admin, edition.ExamName.ToUpperInvariant());

        found.Should().ContainSingle().Which.Id.Should().Be(edition.Id);
    }

    // AC11
    [Fact]
    public async Task Search_ReturnsAtMostTwentyEditions()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        for (var index = 0; index < 21; index++)
        {
            await EditionAsync(admin, year: 2000 + index, marker: $"{marker}{index:00}");
        }

        var found = await SearchAsync(admin, $"Exame {marker}");

        found.Count.Should().Be(20);
    }

    // AC11
    [Fact]
    public async Task FindEdition_ReturnsTheLookupItem_AndAnUnknownOneIs404()
    {
        var admin = await AdminAsync();
        var edition = await EditionAsync(admin, year: 2014, position: "Analista");

        var found = await admin.GetFromJsonAsync<ExamEditionLookupResponse>(
            $"{QuestionBank}/editions/{edition.Id}", AppJson.Options);
        var missing = await admin.GetAsync($"{QuestionBank}/editions/{Guid.CreateVersion7()}");

        found!.Id.Should().Be(edition.Id);
        found.ExamName.Should().Be(edition.ExamName);
        found.Position.Should().Be("Analista");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await missing.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionExamEditionNotFound);
    }

    // AC11
    [Fact]
    public async Task NoticeSubjects_ComeInDisplayOrder_AndAnUnknownEditionIs404()
    {
        var admin = await AdminAsync();
        var edition = await EditionAsync(admin);
        var first = await NoticeSubjectAsync(admin, edition, "Primeira", "Básicos");
        var second = await NoticeSubjectAsync(admin, edition, "Segunda", "Básicos");
        var third = await NoticeSubjectAsync(admin, edition, "Terceira", "Específicos");

        var list = await admin.GetFromJsonAsync<List<NoticeSubjectLookupResponse>>(
            $"{QuestionBank}/editions/{edition.Id}/notice-subjects", AppJson.Options);
        var missing = await admin.GetAsync($"{QuestionBank}/editions/{Guid.CreateVersion7()}/notice-subjects");

        list.Should().NotBeNull();
        list!.Select(item => item.Id).Should().Equal(first.Id, second.Id, third.Id);
        list[0].Label.Should().Be("Primeira");
        list[0].Group.Should().Be("Básicos");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // AC12
    [Fact]
    public async Task EditionQuestions_ArePagedInPaperOrder_WithoutTheDeletedOnes()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var subject = await NoticeSubjectAsync(admin, edition, "Matemática");
        var deleted = Guid.Empty;
        // Created out of order: the route sorts by number, not by creation.
        foreach (var number in Enumerable.Range(1, 31).Reverse())
        {
            var question = await CreatedAsync(
                admin, FromEdition(topic.Id, edition.Id, number, number == 5 ? subject.Id : null, $"<p>Enunciado {number}</p>"));
            if (number == 31)
            {
                deleted = question.Id;
            }

            if (number <= 10)
            {
                (await admin.PostAsync($"{Questions}/{question.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        (await admin.DeleteAsync($"{Questions}/{deleted}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var first = await EditionQuestionsAsync(admin, edition.Id, "?page=0&pageSize=25");
        var second = await EditionQuestionsAsync(admin, edition.Id, "?page=1&pageSize=25");

        first.Total.Should().Be(30);
        first.Items.Select(item => item.PaperNumber).Should().Equal(Enumerable.Range(1, 25));
        second.Items.Select(item => item.PaperNumber).Should().Equal(Enumerable.Range(26, 5));
        first.Items.Should().OnlyContain(item => item.ContentLanguage == "pt-BR");
        first.Items.Single(item => item.PaperNumber == 5).NoticeSubjectLabel.Should().Be("Matemática");
        first.Items.Single(item => item.PaperNumber == 5).NoticeSubjectId.Should().Be(subject.Id);
        first.Items.Single(item => item.PaperNumber == 1).Status.Should().Be(QuestionStatus.Published);
        first.Items.Single(item => item.PaperNumber == 20).Status.Should().Be(QuestionStatus.Draft);
        first.Items[0].StatementExcerpt.Should().Be("Enunciado 1");
    }

    // AC12
    [Fact]
    public async Task EditionQuestions_OfAnUnknownEdition_Is404()
    {
        var admin = await AdminAsync();

        var response = await admin.GetAsync($"{QuestionBank}/editions/{Guid.CreateVersion7()}/questions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionExamEditionNotFound);
    }

    // AC13
    [Fact]
    public async Task EveryRoute_WithoutTheManagePermission_IsForbidden()
    {
        var student = await StudentAsync();
        var id = Guid.CreateVersion7();

        var responses = new[]
        {
            await student.GetAsync($"{QuestionBank}/editions"),
            await student.GetAsync($"{QuestionBank}/editions/{id}"),
            await student.GetAsync($"{QuestionBank}/editions/{id}/notice-subjects"),
            await student.GetAsync($"{QuestionBank}/editions/{id}/questions")
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    // AC13: a Curator holds questions.manage and not catalog.manage, and still fills the origin.
    [Fact]
    public async Task EveryRoute_ForACuratorWithNoCatalogPermission_Answers200()
    {
        var admin = await AdminAsync();
        var curator = await CuratorAsync();
        var edition = await EditionAsync(admin);
        await NoticeSubjectAsync(admin, edition);

        var responses = new[]
        {
            await curator.GetAsync($"{QuestionBank}/editions?search={Uri.EscapeDataString(edition.ExamName)}"),
            await curator.GetAsync($"{QuestionBank}/editions/{edition.Id}"),
            await curator.GetAsync($"{QuestionBank}/editions/{edition.Id}/notice-subjects"),
            await curator.GetAsync($"{QuestionBank}/editions/{edition.Id}/questions")
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        }
    }
}
