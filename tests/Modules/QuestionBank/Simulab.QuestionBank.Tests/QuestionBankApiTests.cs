using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Infrastructure.Persistence;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing.ApiHost;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// The shared Api host plus what only this module's tests need: its own context, signed-in accounts for each
/// role that matters to <c>questions.manage</c> (BR14), and a topic to point questions at. Tests of one class
/// share a database, so each marks its own data.
/// </summary>
public abstract class QuestionBankApiTests : ApiHostTests
{
    protected const string Questions = "/api/v1/question-bank/questions";
    private const string Subjects = "/api/v1/catalog/subjects";

    protected static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    /// <summary>Runs a query on the module's own context, as the module itself would see the data.</summary>
    protected async Task<T> QueryAsync<T>(Func<QuestionBankModuleDbContext, Task<T>> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        await using var scope = Factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<QuestionBankModuleDbContext>());
    }

    protected async Task<HttpClient> SignedInAsync(string role)
    {
        var account = await TestAccounts.CreateAsync(Factory.Services, roles: role);
        return await TestAccounts.SignedInAsync(Client(), account.Email!);
    }

    protected Task<HttpClient> AdminAsync() => SignedInAsync(IdentityRoles.Admin);

    protected Task<HttpClient> CuratorAsync() => SignedInAsync(IdentityRoles.Curator);

    protected Task<HttpClient> StudentAsync() => SignedInAsync(IdentityRoles.Student);

    /// <summary>A subject and a topic under it, made through the catalog the way a curator would.</summary>
    protected static async Task<TopicResponse> TopicAsync(HttpClient admin, string? name = null)
    {
        var marker = Marker();
        var subject = await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest($"Português {marker}"), AppJson.Options);
        subject.StatusCode.Should().Be(HttpStatusCode.Created, await subject.Content.ReadAsStringAsync());
        var subjectId = (await subject.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!.Id;

        var topic = await admin.PostAsJsonAsync(
            $"{Subjects}/{subjectId}/topics", new SaveTopicRequest(name ?? $"Crase {marker}"), AppJson.Options);
        topic.StatusCode.Should().Be(HttpStatusCode.Created, await topic.Content.ReadAsStringAsync());

        return (await topic.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!;
    }

    /// <summary>A request that satisfies every rule of a published question; each test changes what it is about.</summary>
    protected static SaveQuestionRequest Complete(
        Guid topicId,
        string statement = "<p>Qual é a capital da França?</p>",
        int? difficulty = null) =>
        new(
            topicId,
            "pt-BR",
            statement,
            null,
            null,
            difficulty,
            [new SaveQuestionOptionRequest("<p>Paris</p>", true), new SaveQuestionOptionRequest("<p>Lyon</p>")]);

    protected static Task<HttpResponseMessage> CreateAsync(HttpClient client, SaveQuestionRequest request) =>
        client.PostAsJsonAsync(Questions, request, AppJson.Options);

    protected static async Task<QuestionResponse> CreatedAsync(HttpClient client, SaveQuestionRequest request)
    {
        var response = await CreateAsync(client, request);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
    }

    protected static async Task<QuestionResponse> PublishedAsync(HttpClient client, Guid topicId)
    {
        var question = await CreatedAsync(client, Complete(topicId));
        var response = await client.PostAsync($"{Questions}/{question.Id}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
    }

    protected static async Task<QuestionResponse> GetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<QuestionResponse>($"{Questions}/{id}", AppJson.Options))!;

    protected static async Task<QuestionPageResponse> ListAsync(HttpClient client, string query = "") =>
        (await client.GetFromJsonAsync<QuestionPageResponse>($"{Questions}{query}", AppJson.Options))!;
}
