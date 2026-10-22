using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-90 AC9, BR9 through HTTP: a second module that references topics registers its own
/// <see cref="ITopicUsageCheck"/> (a test double here), and the topic's count adds its answer to the question
/// bank's with no change to the catalog.
/// </summary>
public sealed class TopicWhereUsedSecondModuleTests : QuestionBankApiTests
{
    private const int SecondModuleCount = 4;

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices(services => services.AddScoped<ITopicUsageCheck, SecondModule>());
    }

    [Fact]
    public async Task TopicCount_AddsTheSecondModulesAnswerToTheQuestions()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        await CreatedAsync(admin, Complete(topic.Id));
        await CreatedAsync(admin, Complete(topic.Id));

        var whereUsed = (await admin.GetFromJsonAsync<WhereUsedResponse>(
            $"/api/v1/catalog/topics/{topic.Id}/where-used", AppJson.Options))!;

        whereUsed.QuestionCount.Should().Be(2 + SecondModuleCount);
    }

    /// <summary>A module that uses every topic with the same number of rows.</summary>
    private sealed class SecondModule : ITopicUsageCheck
    {
        public Task<IReadOnlySet<Guid>> InUseAsync(IReadOnlyCollection<Guid> topicIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<Guid>>(topicIds.ToHashSet());

        public Task<int> CountAsync(Guid topicId, CancellationToken cancellationToken) => Task.FromResult(SecondModuleCount);
    }
}
