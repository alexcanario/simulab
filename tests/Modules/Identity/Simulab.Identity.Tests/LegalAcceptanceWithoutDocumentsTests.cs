using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-85 AC6 (BR2): a server that cannot read its own legal documents has a configuration problem, not the
/// user's, so nobody is locked out by it and no acceptance is recorded for text nobody can show.
/// </summary>
public sealed class LegalAcceptanceWithoutDocumentsTests : IdentityApiTests
{
    private readonly DirectoryInfo _emptyContent = Directory.CreateTempSubdirectory("simulab-no-legal-");

    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Legal:RootPath"] = _emptyContent.FullName,
            }));

    [Fact]
    public async Task Status_NoDocumentOnTheServer_DoesNotOweAndListsNothing()
    {
        var user = await Accounts.CreateAsync(Factory.Services);
        var client = await Accounts.SignedInAsync(Client(), user.Email!);

        using var response = await client.GetAsync("/api/v1/identity/legal-acceptance");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var status = (await response.Content.ReadFromJsonAsync<LegalAcceptanceStatusResponse>(AppJson.Options))!;
        status.Owes.Should().BeFalse();
        status.Documents.Should().BeEmpty();
        (await QueryAsync(context => context.ConsentRecords.CountAsync(record => record.UserId == user.Id))).Should().Be(0);
    }
}
