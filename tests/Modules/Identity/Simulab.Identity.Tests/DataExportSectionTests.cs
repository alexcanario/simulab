using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing;
using Simulab.Testing.ApiHost;

namespace Simulab.Identity.Tests;

/// <summary>What the export tests of the sections share: a signed-in account and the export route.</summary>
public abstract class DataExportSectionHostTests : IdentityApiTests
{
    private const string ExportRoute = "/api/v1/identity/data-exports";

    protected RecordingLoggerProvider Logs { get; } = new();

    protected async Task<(HttpClient Client, Guid UserId)> SignedInAsync()
    {
        var account = await TestAccounts.CreateAsync(Factory.Services, roles: IdentityRoles.Student);
        return (await TestAccounts.SignedInAsync(Client(), account.Email!), account.Id);
    }

    protected static Task<HttpResponseMessage> ExportAsync(HttpClient client) =>
        client.PostAsJsonAsync(ExportRoute, new DataExportRequest(TestAccounts.ValidPassword), AppJson.Options);

    /// <summary>A section that always fails, with a message that must never reach the caller.</summary>
    protected sealed class FailingSection(string name) : IDataExportSection
    {
        public const string Secret = "PLANTED-SECTION-FAILURE";

        public string Name => name;

        public Task<JsonElement> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(Secret);
    }
}

/// <summary>F-167 AC5: [sec] T3. A section that throws fails the whole export, with no detail, no file and no notice.</summary>
public sealed class DataExportFailingSectionTests : DataExportSectionHostTests
{
    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        builder.ConfigureServices(services => services.AddScoped<IDataExportSection>(_ => new FailingSection("failing")));
    }

    [Fact]
    public async Task Export_WhenASectionThrows_AnswersAServerErrorWithoutDetail_NoFile_NoNotice_AndLogsIt()
    {
        var (client, userId) = await SignedInAsync();
        await RunJobsAsync();
        var jobsBefore = (await JobsAsync()).Count;

        using var response = await ExportAsync(client);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        response.Content.Headers.ContentDisposition.Should().BeNull("no file is returned");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(FailingSection.Secret).And.NotContain("InvalidOperationException").And.NotContain("identity\"");
        (await JobsAsync()).Count.Should().Be(jobsBefore, "no notice email is queued");
        Logs.Entries.Should().Contain(entry =>
            entry.Level >= LogLevel.Error
            && entry.Category.EndsWith("ExportDataHandler", StringComparison.Ordinal)
            && entry.Message.Contains("failing", StringComparison.Ordinal)
            && entry.Message.Contains(userId.ToString(), StringComparison.Ordinal));
    }
}

/// <summary>F-167 AC6: a host with no registered section writes the file F-32 wrote, member for member.</summary>
public sealed class DataExportWithoutSectionsTests : DataExportSectionHostTests
{
    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services => services.RemoveAll<IDataExportSection>());

    [Fact]
    public async Task Export_WithNoSection_HasOnlyTheMembersOfBefore()
    {
        var (client, userId) = await SignedInAsync();

        using var response = await ExportAsync(client);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var file = JsonDocument.Parse(body).RootElement;
        file.EnumerateObject().Select(member => member.Name).Should().Equal("format", "version", "exportedAt", "identity");
        file.GetProperty("identity").Deserialize<IdentityDataResponse>(AppJson.Options)!.Account.Id.Should().Be(userId);
    }
}

/// <summary>F-167 BR9 (design guard): a section whose name is a member of the file already fails the export instead of overwriting it.</summary>
public sealed class DataExportSectionNameTests : DataExportSectionHostTests
{
    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        builder.ConfigureServices(services => services.AddScoped<IDataExportSection>(_ => new NamedSection("identity")));
    }

    [Fact]
    public async Task Export_WithASectionNamedLikeAnExistingMember_FailsWithoutAFile()
    {
        var (client, _) = await SignedInAsync();

        using var response = await ExportAsync(client);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentDisposition.Should().BeNull();
        Logs.Entries.Should().Contain(entry => entry.Level >= LogLevel.Error && entry.Message.Contains("identity", StringComparison.Ordinal));
    }

    private sealed class NamedSection(string name) : IDataExportSection
    {
        public string Name => name;

        public Task<JsonElement> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(JsonSerializer.SerializeToElement(new { taken = true }, AppJson.Options));
    }
}
