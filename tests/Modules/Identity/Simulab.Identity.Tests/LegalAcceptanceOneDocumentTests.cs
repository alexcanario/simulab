using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-85 AC6, BR2 and design decision D14: a server that can read the terms but not the privacy policy has a
/// configuration problem, not the user's. The unreadable topic is never compared, and an acceptance keeps the version
/// of the user's last acceptance for it (review finding 2). This is the only case where the handler writes a version
/// the page did not show.
/// </summary>
public sealed class LegalAcceptanceOneDocumentTests : IdentityApiTests
{
    private const string OldPrivacy = "2026-v0";
    private readonly DirectoryInfo _termsOnly = Directory.CreateTempSubdirectory("simulab-terms-only-");

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        var real = Path.Combine(AppContext.BaseDirectory, "Content", "Legal");
        foreach (var locale in SupportedLanguages.All)
        {
            var source = Path.Combine(real, locale, "terms");
            var target = Directory.CreateDirectory(Path.Combine(_termsOnly.FullName, locale, "terms"));
            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(target.FullName, Path.GetFileName(file)));
            }
        }

        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Legal:RootPath"] = _termsOnly.FullName,
            }));
    }

    private async Task<(User User, HttpClient Client)> SignedInAsync()
    {
        var user = await Accounts.CreateAsync(Factory.Services);
        return (user, await Accounts.SignedInAsync(Client(), user.Email!));
    }

    private async Task AcceptedAsync(Guid userId, string termsVersion)
    {
        var saved = await QueryAsync(async context =>
        {
            context.ConsentRecords.Add(new ConsentRecord
            {
                UserId = userId,
                TermsVersion = termsVersion,
                PrivacyVersion = OldPrivacy,
                DeclaresAdult = true,
                Locale = "en",
                AcceptedAt = Factory.Clock.GetUtcNow().AddDays(-30),
            });
            return await context.SaveChangesAsync();
        });
        saved.Should().Be(1);
    }

    private Task<List<ConsentRecord>> RecordsOfAsync(Guid userId) =>
        QueryAsync(context => context.ConsentRecords.AsNoTracking().Where(record => record.UserId == userId)
            .OrderBy(record => record.AcceptedAt).ThenBy(record => record.Id).ToListAsync());

    // AC6: no earlier acceptance and one unreadable document is the server's problem, so nothing is owed.
    [Fact]
    public async Task NoEarlierAcceptance_StatusListsOnlyTheReadableDocumentAndOwesNothing_AndThePostWritesNothing()
    {
        var (user, client) = await SignedInAsync();

        var status = (await client.GetFromJsonAsync<LegalAcceptanceStatusResponse>("/api/v1/identity/legal-acceptance", AppJson.Options))!;
        using var post = await client.PostAsJsonAsync(
            "/api/v1/identity/legal-acceptances",
            new AcceptLegalDocumentsRequest(SignUpForm.TermsVersion, "anything", "en"),
            AppJson.Options);

        status.Owes.Should().BeFalse();
        status.Documents.Should().ContainSingle().Which.Topic.Should().Be(LegalTopic.Terms);
        post.StatusCode.Should().Be(HttpStatusCode.OK, await post.Content.ReadAsStringAsync());
        (await RecordsOfAsync(user.Id)).Should().BeEmpty();
    }

    // The readable document changed: owed, and the unreadable one is neither listed nor compared.
    [Fact]
    public async Task EarlierAcceptanceOfAnOlderTerms_StatusOwesAndMarksOnlyTheTermsChanged()
    {
        var (user, client) = await SignedInAsync();
        await AcceptedAsync(user.Id, "2026-v0");

        var status = (await client.GetFromJsonAsync<LegalAcceptanceStatusResponse>("/api/v1/identity/legal-acceptance", AppJson.Options))!;

        status.Owes.Should().BeTrue();
        status.Documents.Should().ContainSingle();
        status.Documents[0].Topic.Should().Be(LegalTopic.Terms);
        status.Documents[0].Changed.Should().BeTrue();
    }

    // D14: the new row keeps the privacy version of the last acceptance; it never invents one.
    [Fact]
    public async Task EarlierAcceptanceOfAnOlderTerms_ThePostKeepsThePrivacyVersionOfTheLastAcceptance()
    {
        var (user, client) = await SignedInAsync();
        await AcceptedAsync(user.Id, "2026-v0");

        using var post = await client.PostAsJsonAsync(
            "/api/v1/identity/legal-acceptances",
            new AcceptLegalDocumentsRequest(SignUpForm.TermsVersion, "whatever the page had", "en"),
            AppJson.Options);

        post.StatusCode.Should().Be(HttpStatusCode.Created, await post.Content.ReadAsStringAsync());
        var records = await RecordsOfAsync(user.Id);
        records.Should().HaveCount(2);
        records[1].TermsVersion.Should().Be(SignUpForm.TermsVersion);
        records[1].PrivacyVersion.Should().Be(OldPrivacy);
    }
}
