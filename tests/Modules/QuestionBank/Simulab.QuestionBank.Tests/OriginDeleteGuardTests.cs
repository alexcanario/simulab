using System.Net;
using Simulab.Catalog.Contracts;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-96 BR8, AC9 and AC10 through HTTP: an edition or a notice subject is refused its delete while a live
/// question points at it. The catalog asks every registered usage check and never references this module.
/// </summary>
public sealed class OriginDeleteGuardTests : OriginApiTests
{
    private static string EditionRoute(Edition edition) => $"{CatalogExams}/{edition.ExamId}/editions/{edition.Id}";

    private static string SubjectRoute(Edition edition, Guid subjectId) =>
        $"{EditionRoute(edition)}/notice-subjects/{subjectId}";

    // AC9
    [Fact]
    public async Task DeleteEdition_WithALiveQuestion_IsRefusedWithHasQuestions_AndSucceedsOnceTheyAreGone()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var question = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 1));

        var refused = await admin.DeleteAsync(EditionRoute(edition));

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionHasQuestions);
        (await admin.GetAsync(EditionRoute(edition))).StatusCode.Should().Be(HttpStatusCode.OK);

        (await admin.DeleteAsync($"{Questions}/{question.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var deleted = await admin.DeleteAsync(EditionRoute(edition));

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleted.Content.ReadAsStringAsync());
    }

    // AC9: any status of the question guards it.
    [Fact]
    public async Task DeleteEdition_WithAPublishedQuestion_IsRefusedWithHasQuestions()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var question = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 1));
        (await admin.PostAsync($"{Questions}/{question.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await admin.DeleteAsync(EditionRoute(edition));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionHasQuestions);
    }

    // AC9: clearing the origin of the question frees the edition too.
    [Fact]
    public async Task DeleteEdition_AfterTheOriginIsCleared_Succeeds()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var question = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 1));
        (await admin.DeleteAsync(EditionRoute(edition))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        await UpdatedAsync(admin, question.Id, Complete(topic.Id));

        var response = await admin.DeleteAsync(EditionRoute(edition));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    // AC10
    [Fact]
    public async Task DeleteNoticeSubject_WithALiveQuestion_IsRefusedWithHasQuestions_AndSucceedsOnceTheSubjectIsCleared()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var subject = await NoticeSubjectAsync(admin, edition);
        var question = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 1, subject.Id));

        var refused = await admin.DeleteAsync(SubjectRoute(edition, subject.Id));

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.NoticeSubjectHasQuestions);

        await UpdatedAsync(admin, question.Id, FromEdition(topic.Id, edition.Id, 1));
        var deleted = await admin.DeleteAsync(SubjectRoute(edition, subject.Id));

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleted.Content.ReadAsStringAsync());
    }

    // AC10
    [Fact]
    public async Task DeleteNoticeSubject_AfterTheQuestionIsDeleted_Succeeds()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var subject = await NoticeSubjectAsync(admin, edition);
        var question = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 1, subject.Id));
        (await admin.DeleteAsync(SubjectRoute(edition, subject.Id))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await admin.DeleteAsync($"{Questions}/{question.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await admin.DeleteAsync(SubjectRoute(edition, subject.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    // BR8: another subject of the same edition, which no question points at, is free to go.
    [Fact]
    public async Task DeleteNoticeSubject_NoQuestionPointsAt_Succeeds_WhileASiblingIsGuarded()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var used = await NoticeSubjectAsync(admin, edition, "Usado");
        var free = await NoticeSubjectAsync(admin, edition, "Livre");
        await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 1, used.Id));

        (await admin.DeleteAsync(SubjectRoute(edition, free.Id))).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.DeleteAsync(SubjectRoute(edition, used.Id))).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
