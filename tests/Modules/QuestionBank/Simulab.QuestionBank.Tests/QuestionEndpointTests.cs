using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-95 through HTTP, on a real PostgreSQL: authoring, sanitizing, editing, deleting, listing and the
/// permission. The publication flow has its own class.
/// </summary>
public sealed class QuestionEndpointTests : QuestionBankApiTests
{
    // AC1: a question saved with topic, language and statement is a draft, listed with topic and subject.
    [Fact]
    public async Task Create_TopicLanguageAndStatement_IsStoredAsDraft_AndListedWithItsTopicAndSubject()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, new SaveQuestionRequest(topic.Id, "pt-BR", "<p>O que é <strong>crase</strong>?</p>"));

        question.Status.Should().Be(QuestionStatus.Draft);
        question.Type.Should().Be(QuestionType.SingleChoice);
        question.StatementHtml.Should().Be("<p>O que é <strong>crase</strong>?</p>");
        question.TopicName.Should().Be(topic.Name);
        question.Options.Should().BeEmpty();

        var listed = (await ListAsync(admin, "?pageSize=100")).Items.Single(item => item.Id == question.Id);
        listed.TopicId.Should().Be(topic.Id);
        listed.TopicName.Should().Be(topic.Name);
        listed.SubjectName.Should().NotBeNullOrEmpty();
        listed.Status.Should().Be(QuestionStatus.Draft);
        listed.StatementExcerpt.Should().Be("O que é crase?");
    }

    // AC2: an empty statement is 400; a draft with no options and no key is accepted.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<p><br></p>")]
    public async Task Create_EmptyStatement_IsRefused(string? statement)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(admin, new SaveQuestionRequest(topic.Id, "pt-BR", statement));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionStatementRequired);
    }

    [Fact]
    public async Task Create_DraftWithNoOptionsAndNoKey_IsAccepted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, new SaveQuestionRequest(topic.Id, "pt-BR", "<p>Rascunho</p>"));

        question.Options.Should().BeEmpty();
        question.Status.Should().Be(QuestionStatus.Draft);
    }

    // AC3: the four text fields are sanitized on the way in; what is returned and what is stored are the same.
    [Fact]
    public async Task Create_HostileHtmlInEveryField_IsStrippedAndTheAllowedMarkupSurvives()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        const string hostile =
            "<p onclick=\"x()\">ok <sup>2</sup></p><script>alert(1)</script>" +
            "<a href=\"javascript:alert(1)\">bad</a><a href=\"https://example.com\">good</a>" +
            "<img src=\"data:image/png;base64,AAAA\"><table><tbody><tr><td>cell</td></tr></tbody></table>";
        var request = new SaveQuestionRequest(
            topic.Id,
            "pt-BR",
            hostile,
            null,
            hostile,
            null,
            [new SaveQuestionOptionRequest(hostile, true), new SaveQuestionOptionRequest("<p>B</p>")]);

        var created = await CreatedAsync(admin, request);
        var stored = await GetAsync(admin, created.Id);

        foreach (var question in new[] { created, stored })
        {
            var fields = new[]
            {
                question.StatementHtml, question.ExplanationHtml!, question.Options[0].TextHtml
            };
            foreach (var field in fields)
            {
                field.Should().NotContain("script").And.NotContain("onclick").And.NotContain("javascript:")
                    .And.NotContain("data:").And.NotContain("<img");
                field.Should().Contain("<sup>2</sup>").And.Contain("https://example.com").And.Contain("<td>cell</td>");
            }
        }
    }

    // AC4: a sixth option is refused.
    [Fact]
    public async Task Create_SixOptions_IsRefusedWithOptionsCount()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var options = Enumerable.Range(0, 6).Select(index => new SaveQuestionOptionRequest($"<p>{index}</p>")).ToList();

        var response = await CreateAsync(admin, new SaveQuestionRequest(topic.Id, "pt-BR", "<p>Q</p>", Options: options));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionOptionsCount);
    }

    // AC7: editing a published question keeps it published; an edit that breaks it is refused and nothing changes.
    [Fact]
    public async Task Update_Published_StatementChange_StaysPublishedWithTheNewSanitizedText()
    {
        var admin = await AdminAsync();
        var published = await PublishedAsync(admin, (await TopicAsync(admin)).Id);

        var response = await admin.PutAsJsonAsync(
            $"{Questions}/{published.Id}",
            Complete(published.TopicId, "<p>Nova <script>x</script>pergunta</p>"),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updated = (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
        updated.Status.Should().Be(QuestionStatus.Published);
        updated.StatementHtml.Should().Be("<p>Nova pergunta</p>");
        (await GetAsync(admin, published.Id)).StatementHtml.Should().Be("<p>Nova pergunta</p>");
    }

    [Fact]
    public async Task Update_Published_LeavingOneOption_IsRefused_AndTheStoredQuestionIsUnchanged()
    {
        var admin = await AdminAsync();
        var published = await PublishedAsync(admin, (await TopicAsync(admin)).Id);
        var broken = Complete(published.TopicId, "<p>Quebrada</p>") with
        {
            Options = [new SaveQuestionOptionRequest("<p>Paris</p>", true)]
        };

        var response = await admin.PutAsJsonAsync($"{Questions}/{published.Id}", broken, AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionOptionsCount);
        var stored = await GetAsync(admin, published.Id);
        stored.StatementHtml.Should().Be(published.StatementHtml);
        stored.Options.Should().HaveCount(2);
        stored.Status.Should().Be(QuestionStatus.Published);
    }

    [Fact]
    public async Task Update_ReplacesTheOptionsInTheOrderSent()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Complete(topic.Id));
        var reordered = Complete(topic.Id) with
        {
            Options =
            [
                new SaveQuestionOptionRequest("<p>Lyon</p>"),
                new SaveQuestionOptionRequest("<p>Nice</p>"),
                new SaveQuestionOptionRequest("<p>Paris</p>", true)
            ]
        };

        var response = await admin.PutAsJsonAsync($"{Questions}/{question.Id}", reordered, AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var stored = await GetAsync(admin, question.Id);
        stored.Options.Select(option => option.TextHtml).Should().Equal("<p>Lyon</p>", "<p>Nice</p>", "<p>Paris</p>");
        stored.Options.Select(option => option.Position).Should().Equal(0, 1, 2);
        stored.Options.Single(option => option.IsCorrect).Position.Should().Be(2);
    }

    // AC8: a draft is deleted and leaves the list, its rows stay flagged; a published one is refused.
    [Fact]
    public async Task Delete_Draft_LeavesTheList_AndSoftDeletesTheQuestionAndItsOptions()
    {
        var admin = await AdminAsync();
        var question = await CreatedAsync(admin, Complete((await TopicAsync(admin)).Id));

        var response = await admin.DeleteAsync($"{Questions}/{question.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListAsync(admin, "?pageSize=100")).Items.Should().NotContain(item => item.Id == question.Id);
        (await admin.GetAsync($"{Questions}/{question.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var deleted = await QueryAsync(async context => new
        {
            Question = await context.Questions.IgnoreQueryFilters().Where(row => row.Id == question.Id)
                .Select(row => row.IsDeleted).SingleAsync(),
            Options = await context.QuestionOptions.IgnoreQueryFilters().Where(row => row.QuestionId == question.Id)
                .Select(row => row.IsDeleted).ToListAsync()
        });
        deleted.Question.Should().BeTrue();
        deleted.Options.Should().HaveCount(2).And.OnlyContain(isDeleted => isDeleted);
    }

    [Fact]
    public async Task Delete_Published_IsRefusedWithDeleteRequiresDraft()
    {
        var admin = await AdminAsync();
        var published = await PublishedAsync(admin, (await TopicAsync(admin)).Id);

        var response = await admin.DeleteAsync($"{Questions}/{published.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
        (await GetAsync(admin, published.Id)).Status.Should().Be(QuestionStatus.Published);
    }

    // AC10, BR14: no permission, no endpoint; Curator and Admin hold it.
    [Fact]
    public async Task EveryEndpoint_WithoutThePermission_AnswersForbidden()
    {
        var student = await StudentAsync();
        var id = Guid.CreateVersion7();

        var answers = new[]
        {
            await student.GetAsync(Questions),
            await student.GetAsync($"{Questions}/{id}"),
            await student.PostAsJsonAsync(Questions, new SaveQuestionRequest(), AppJson.Options),
            await student.PutAsJsonAsync($"{Questions}/{id}", new SaveQuestionRequest(), AppJson.Options),
            await student.PostAsync($"{Questions}/{id}/publish", null),
            await student.PostAsync($"{Questions}/{id}/unpublish", null),
            await student.DeleteAsync($"{Questions}/{id}")
        };

        answers.Select(answer => answer.StatusCode).Should().OnlyContain(status => status == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ACurator_HasThePermission_WithoutBeingAnAdmin()
    {
        var curator = await CuratorAsync();
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(curator, Complete(topic.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        (await curator.GetAsync(Questions)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AC11, BR10, BR15: status filter and statement search ignoring case and accents, paged, newest first.
    [Fact]
    public async Task List_FiltersByStatusAndSearchesTheStatementIgnoringCaseAndAccents()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var draft = await CreatedAsync(admin, Complete(topic.Id, $"<p>Dúvida {marker}</p>"));
        var published = await CreatedAsync(admin, Complete(topic.Id, $"<p>Outra DUVIDA {marker}</p>"));
        await admin.PostAsync($"{Questions}/{published.Id}/publish", null);
        await CreatedAsync(admin, Complete(topic.Id, $"<p>Sem relação {marker}</p>"));

        var search = await ListAsync(admin, $"?search={Uri.EscapeDataString("duvida")}%20{marker}");
        var byStatus = await ListAsync(admin, $"?status=Published&search={marker}");

        search.Items.Select(item => item.Id).Should().BeEquivalentTo([draft.Id, published.Id]);
        byStatus.Items.Select(item => item.Id).Should().Equal(published.Id);
        byStatus.Total.Should().Be(1);
    }

    [Fact]
    public async Task List_IsPagedNewestFirst()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var first = await CreatedAsync(admin, Complete(topic.Id, $"<p>Primeira {marker}</p>"));
        var second = await CreatedAsync(admin, Complete(topic.Id, $"<p>Segunda {marker}</p>"));
        var third = await CreatedAsync(admin, Complete(topic.Id, $"<p>Terceira {marker}</p>"));

        var page0 = await ListAsync(admin, $"?search={marker}&page=0&pageSize=2");
        var page1 = await ListAsync(admin, $"?search={marker}&page=1&pageSize=2");

        page0.Total.Should().Be(3);
        page0.Items.Select(item => item.Id).Should().Equal(third.Id, second.Id);
        page1.Items.Select(item => item.Id).Should().Equal(first.Id);
    }

    [Fact]
    public async Task List_ExcerptIsCutAtTheLimit()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(
            admin,
            Complete(topic.Id, $"<p>{new string('x', QuestionLimits.ExcerptLength + 50)}</p>"));

        var listed = (await ListAsync(admin, "?pageSize=100")).Items.Single(item => item.Id == question.Id);

        listed.StatementExcerpt.Length.Should().Be(QuestionLimits.ExcerptLength);
    }

    [Fact]
    public async Task List_UnknownStatus_IsIgnored()
    {
        var admin = await AdminAsync();

        var response = await admin.GetAsync($"{Questions}?status=Banana");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AC13, BR6.
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Create_DifficultyOutsideTheRange_IsRefused(int difficulty)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(admin, Complete(topic.Id, difficulty: difficulty));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionDifficultyInvalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task Create_DifficultyInsideTheRangeOrEmpty_IsAccepted(int? difficulty)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, Complete(topic.Id, difficulty: difficulty));

        question.Difficulty.Should().Be(difficulty);
    }

    // AC14, BR7.
    [Fact]
    public async Task Create_UnsupportedLanguage_IsRefused_AndALowercaseOneIsStoredCanonical()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var refused = await CreateAsync(admin, Complete(topic.Id) with { ContentLanguage = "fr" });
        var created = await CreatedAsync(admin, Complete(topic.Id) with { ContentLanguage = "pt-br" });

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionContentLanguageInvalid);
        created.ContentLanguage.Should().Be("pt-BR");
    }

    // AC15, BR9: each field has its own code, and nothing is truncated.
    [Fact]
    public async Task Create_OverTheLimit_IsRefusedWithEachFieldsOwnCode()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var statement = $"<p>{new string('a', QuestionLimits.StatementMaxLength)}</p>";
        var explanation = $"<p>{new string('a', QuestionLimits.ExplanationMaxLength)}</p>";
        var option = $"<p>{new string('a', QuestionLimits.OptionMaxLength)}</p>";

        (string Code, SaveQuestionRequest Request)[] cases =
        [
            (QuestionBankErrorCodes.QuestionStatementTooLong, Complete(topic.Id, statement)),
            (QuestionBankErrorCodes.QuestionExplanationTooLong, Complete(topic.Id) with { ExplanationHtml = explanation }),
            (QuestionBankErrorCodes.QuestionOptionTooLong, Complete(topic.Id) with
            {
                Options = [new SaveQuestionOptionRequest(option, true), new SaveQuestionOptionRequest("<p>B</p>")]
            })
        ];

        foreach (var (code, request) in cases)
        {
            var response = await CreateAsync(admin, request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, code);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(code);
        }
    }

    [Fact]
    public async Task Create_TextAtTheLimit_IsStoredWhole()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var text = new string('a', QuestionLimits.StatementMaxLength - "<p></p>".Length);

        var question = await CreatedAsync(admin, Complete(topic.Id, $"<p>{text}</p>"));

        (await GetAsync(admin, question.Id)).StatementHtml.Should().Be($"<p>{text}</p>");
    }

    // The option the refusal is about travels on the problem (decision of 2026-10-10).
    [Fact]
    public async Task Create_OptionTooLong_NamesItsPositionOnTheProblem()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var request = Complete(topic.Id) with
        {
            Options =
            [
                new SaveQuestionOptionRequest("<p>Fine</p>", true),
                new SaveQuestionOptionRequest("<p>Fine too</p>"),
                new SaveQuestionOptionRequest($"<p>{new string('a', QuestionLimits.OptionMaxLength)}</p>")
            ]
        };

        var response = await CreateAsync(admin, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        problem.GetProperty("code").GetString().Should().Be(QuestionBankErrorCodes.QuestionOptionTooLong);
        problem.GetProperty("position").GetInt32().Should().Be(2);
    }

    // BR16.
    [Fact]
    public async Task UnknownIds_Answer404WithTheirOwnCodes()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var missing = Guid.CreateVersion7();

        var question = await admin.GetAsync($"{Questions}/{missing}");
        var update = await admin.PutAsJsonAsync($"{Questions}/{missing}", Complete(topic.Id), AppJson.Options);
        var unknownTopic = await CreateAsync(admin, Complete(missing));
        var noTopic = await CreateAsync(admin, Complete(topic.Id) with { TopicId = null });

        foreach (var (response, code) in new[]
                 {
                     (question, QuestionBankErrorCodes.QuestionNotFound),
                     (update, QuestionBankErrorCodes.QuestionNotFound),
                     (unknownTopic, QuestionBankErrorCodes.QuestionTopicNotFound),
                     (noTopic, QuestionBankErrorCodes.QuestionTopicNotFound)
                 })
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound, code);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(code);
        }
    }

    [Fact]
    public async Task Update_ToADeletedTopic_IsRefusedWithTopicNotFound()
    {
        var admin = await AdminAsync();
        var kept = await TopicAsync(admin);
        var doomed = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Complete(kept.Id));
        (await admin.DeleteAsync($"/api/v1/catalog/topics/{doomed.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await admin.PutAsJsonAsync($"{Questions}/{question.Id}", Complete(doomed.Id), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionTopicNotFound);
    }
}
