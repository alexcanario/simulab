using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing.ApiHost;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// What the review tests of F-105 share: the seven routes as helpers, a signed-in account whose only permissions
/// are the ones asked for (the least-privileged role is the one to test with, B-28), and the entries as stored.
/// </summary>
public abstract class QuestionReviewApiTests : QuestionBankApiTests
{
    protected const string ReviewQueue = "/api/v1/question-bank/review-queue";

    /// <summary>The host clock is fixed: a test that cares about the order of two events moves it between them.</summary>
    protected void Tick() => Factory.Clock.Advance(TimeSpan.FromMinutes(1));

    protected static Task<HttpResponseMessage> SubmitAsync(HttpClient client, Guid id) =>
        client.PostAsync($"{Questions}/{id}/submit", null);

    protected static Task<HttpResponseMessage> WithdrawAsync(HttpClient client, Guid id) =>
        client.PostAsync($"{Questions}/{id}/withdraw", null);

    protected static Task<HttpResponseMessage> ApproveAsync(HttpClient client, Guid id, string? note, DateTimeOffset? submittedAt = null) =>
        note is null && submittedAt is null
            ? client.PostAsync($"{ReviewQueue}/{id}/approve", null)
            : client.PostAsJsonAsync($"{ReviewQueue}/{id}/approve", new ApproveQuestionRequest(note, submittedAt), AppJson.Options);

    protected static Task<HttpResponseMessage> RejectAsync(HttpClient client, Guid id, string? reason, DateTimeOffset? submittedAt = null) =>
        reason is null && submittedAt is null
            ? client.PostAsync($"{ReviewQueue}/{id}/reject", null)
            : client.PostAsJsonAsync($"{ReviewQueue}/{id}/reject", new RejectQuestionRequest(reason, submittedAt), AppJson.Options);

    protected static async Task<QuestionResponse> AsQuestionAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
    }

    protected static async Task<QuestionResponse> SubmittedAsync(HttpClient client, Guid id) =>
        await AsQuestionAsync(await SubmitAsync(client, id));

    protected static async Task<QuestionResponse> InReviewAsync(HttpClient client, Guid topicId, string statement = "<p>Qual é a capital da França?</p>")
    {
        var draft = await CreatedAsync(client, Complete(topicId, statement));

        return await SubmittedAsync(client, draft.Id);
    }

    protected static async Task<IReadOnlyList<QuestionReviewEntryResponse>> ReviewsAsync(HttpClient client, Guid id)
    {
        var response = await client.GetAsync($"{Questions}/{id}/reviews");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<List<QuestionReviewEntryResponse>>(AppJson.Options))!;
    }

    protected static async Task<ReviewQueuePageResponse> QueueAsync(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"{ReviewQueue}{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ReviewQueuePageResponse>(AppJson.Options))!;
    }

    protected static async Task AssertRefusedAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, body);
        CodeOf(body).Should().Be(code);
    }

    /// <summary>An account whose role holds exactly these permissions and no others.</summary>
    protected async Task<HttpClient> HoldingOnlyAsync(params string[] permissions)
    {
        var admin = await AdminAsync();
        var role = await admin.PostAsJsonAsync(
            "/api/v1/identity/roles",
            new SaveRoleRequest($"Only {string.Join('+', permissions)} {Marker()}", [.. permissions]),
            AppJson.Options);
        role.StatusCode.Should().Be(HttpStatusCode.Created, await role.Content.ReadAsStringAsync());
        var roleName = (await role.Content.ReadFromJsonAsync<RoleResponse>(AppJson.Options))!.Name;
        var account = await TestAccounts.CreateAsync(Factory.Services, roles: roleName);

        return await TestAccounts.SignedInAsync(Client(), account.Email!);
    }

    protected Task<List<QuestionReviewEntry>> StoredEntriesAsync(Guid questionId) =>
        QueryAsync(context => context.QuestionReviewEntries
            .AsNoTracking()
            .Where(entry => entry.QuestionId == questionId)
            .OrderBy(entry => entry.CreatedAt)
            .ToListAsync());

    protected Task<string> StatusOfAsync(Guid questionId) =>
        QueryAsync(context => context.Database
            .SqlQuery<string>($"SELECT status AS \"Value\" FROM question_bank.questions WHERE id = {questionId}")
            .SingleAsync());
}
