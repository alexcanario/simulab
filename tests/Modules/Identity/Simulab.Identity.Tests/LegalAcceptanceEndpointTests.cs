using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-85: a signed-in user whose last acceptance is not of the current legal versions owes a new one, and
/// accepting writes a new evidence row (AC1-AC13, AC21, AC22, AC25), through the real HTTP pipeline.
/// </summary>
public sealed class LegalAcceptanceEndpointTests : IdentityApiTests
{
    private const string StatusRoute = "/api/v1/identity/legal-acceptance";
    private const string AcceptRoute = "/api/v1/identity/legal-acceptances";
    private const string ClientIp = "203.0.113.45";

    private static readonly string[] SupportedLocales = ["en", "pt-BR", "pt-PT"];

    private async Task<(User User, HttpClient Client)> SignedInAsync(string locale = "en")
    {
        var user = await Accounts.CreateAsync(Factory.Services);
        var client = await Accounts.SignedInAsync(Client(locale), user.Email!);
        return (user, client);
    }

    /// <summary>One earlier acceptance, written the way sign-up writes it, <paramref name="daysAgo"/> days before the test clock.</summary>
    private async Task AcceptedAsync(Guid userId, string termsVersion, string privacyVersion, int daysAgo = 30, string locale = "en")
    {
        var saved = await QueryAsync(async context =>
        {
            context.ConsentRecords.Add(new ConsentRecord
            {
                UserId = userId,
                TermsVersion = termsVersion,
                PrivacyVersion = privacyVersion,
                DeclaresAdult = true,
                Locale = locale,
                AcceptedAt = Factory.Clock.GetUtcNow().AddDays(-daysAgo),
            });
            return await context.SaveChangesAsync();
        });
        saved.Should().Be(1);
    }

    private Task<List<ConsentRecord>> RecordsOfAsync(Guid userId) =>
        QueryAsync(context => context.ConsentRecords.AsNoTracking().Where(record => record.UserId == userId)
            .OrderBy(record => record.AcceptedAt).ThenBy(record => record.Id).ToListAsync());

    private static async Task<LegalAcceptanceStatusResponse> StatusAsync(HttpClient client)
    {
        using var response = await client.GetAsync(StatusRoute);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<LegalAcceptanceStatusResponse>(AppJson.Options))!;
    }

    private static Task<HttpResponseMessage> AcceptAsync(
        HttpClient client, string termsVersion, string privacyVersion, string locale = "en", string? address = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, AcceptRoute)
        {
            Content = JsonContent.Create(new AcceptLegalDocumentsRequest(termsVersion, privacyVersion, locale), options: AppJson.Options),
        };
        if (address is not null)
        {
            request.Headers.Add(ClientAddressHeaders.Address, address);
            request.Headers.Add(ClientAddressHeaders.Secret, TestClient.ClientSecret);
        }

        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> AcceptCurrentAsync(HttpClient client, string locale = "en", string? address = null) =>
        AcceptAsync(client, SignUpForm.TermsVersion, SignUpForm.PrivacyVersion, locale, address);

    // AC1
    [Fact]
    public async Task Status_LastAcceptanceIsCurrent_DoesNotOwe()
    {
        var (user, client) = await SignedInAsync();
        await AcceptedAsync(user.Id, SignUpForm.TermsVersion, SignUpForm.PrivacyVersion);

        var status = await StatusAsync(client);

        status.Owes.Should().BeFalse();
        status.Documents.Should().OnlyContain(document => !document.Changed);
        (await RecordsOfAsync(user.Id)).Should().HaveCount(1, "reading the status writes nothing");
    }

    // AC2
    [Theory]
    [InlineData("2026-v1", "2026-v1", false, true)]
    [InlineData("2026-v0", "2026-v3", true, false)]
    [InlineData("2026-v0", "2026-v1", true, true)]
    public async Task Status_AnOlderVersion_OwesAndMarksExactlyTheDocumentsThatDiffer(
        string acceptedTerms, string acceptedPrivacy, bool termsChanged, bool privacyChanged)
    {
        var (user, client) = await SignedInAsync();
        await AcceptedAsync(user.Id, acceptedTerms, acceptedPrivacy);

        var status = await StatusAsync(client);

        status.Owes.Should().BeTrue();
        status.Documents.Should().HaveCount(2);
        status.Documents.Single(document => document.Topic == LegalTopic.Terms).Changed.Should().Be(termsChanged);
        status.Documents.Single(document => document.Topic == LegalTopic.Privacy).Changed.Should().Be(privacyChanged);
        status.Documents.Single(document => document.Topic == LegalTopic.Terms).Version.Should().Be(SignUpForm.TermsVersion);
        status.Documents.Single(document => document.Topic == LegalTopic.Privacy).Version.Should().Be(SignUpForm.PrivacyVersion);
    }

    // AC3
    [Fact]
    public async Task Status_NoAcceptanceAtAll_OwesAndEveryDocumentIsChanged()
    {
        var (_, client) = await SignedInAsync();

        var status = await StatusAsync(client);

        status.Owes.Should().BeTrue();
        status.Documents.Should().HaveCount(2).And.OnlyContain(document => document.Changed);
    }

    // AC4
    [Fact]
    public async Task Status_AnOldAndANewerCurrentAcceptance_TheNewestDecidesWhateverTheInsertionOrder()
    {
        var (user, client) = await SignedInAsync();
        await AcceptedAsync(user.Id, SignUpForm.TermsVersion, SignUpForm.PrivacyVersion, daysAgo: 1);
        await AcceptedAsync(user.Id, "2026-v0", "2026-v0", daysAgo: 60);

        (await StatusAsync(client)).Owes.Should().BeFalse();
    }

    // AC7
    [Fact]
    public async Task Status_PortugueseUser_GetsThePortugueseTitlesAndTheSameVersions()
    {
        var (_, client) = await SignedInAsync("pt-PT");
        using var privacy = await client.GetAsync("/api/v1/identity/legal-documents/privacy");
        var portuguese = (await privacy.Content.ReadFromJsonAsync<LegalDocumentResponse>(AppJson.Options))!;

        var status = await StatusAsync(client);

        var document = status.Documents.Single(item => item.Topic == LegalTopic.Privacy);
        document.Title.Should().Be(portuguese.Title);
        document.Version.Should().Be(SignUpForm.PrivacyVersion);
        document.EffectiveDate.Should().Be(portuguese.EffectiveDate);
        portuguese.Locale.Should().Be("pt-PT");
    }

    // AC5, AC21
    [Fact]
    public async Task Accept_WhenOwing_WritesANewRecordAndKeepsTheOlderOnesAndTheExportListsItLast()
    {
        var (user, client) = await SignedInAsync();
        await AcceptedAsync(user.Id, "2026-v0", "2026-v0", locale: "en");
        var before = (await RecordsOfAsync(user.Id)).Single();

        using var response = await AcceptCurrentAsync(client, locale: "pt-BR", address: ClientIp);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var records = await RecordsOfAsync(user.Id);
        records.Should().HaveCount(2);
        records[0].Should().BeEquivalentTo(before, "an older record is never changed");
        var added = records[1];
        added.TermsVersion.Should().Be(SignUpForm.TermsVersion);
        added.PrivacyVersion.Should().Be(SignUpForm.PrivacyVersion);
        added.Locale.Should().Be("pt-BR");
        added.AcceptedAt.Should().Be(Factory.Clock.GetUtcNow());
        added.IpAddress.Should().Be(ClientIp);
        added.DeclaresAdult.Should().Be(user.IsAdultDeclared);
        (await StatusAsync(client)).Owes.Should().BeFalse();

        using var export = await client.PostAsJsonAsync(
            "/api/v1/identity/data-exports", new DataExportRequest(SignUpForm.ValidPassword), AppJson.Options);
        export.StatusCode.Should().Be(HttpStatusCode.OK, await export.Content.ReadAsStringAsync());
        var file = JsonSerializer.Deserialize<DataExportResponse>(await export.Content.ReadAsStringAsync(), AppJson.Options)!;
        file.Identity.Consents.Should().HaveCount(2);
        file.Identity.Consents[^1].Locale.Should().Be("pt-BR");
    }

    // AC8
    [Fact]
    public async Task Accept_WhenNothingIsOwed_AnswersOkAndWritesNothing()
    {
        var (user, client) = await SignedInAsync();
        await AcceptedAsync(user.Id, SignUpForm.TermsVersion, SignUpForm.PrivacyVersion);

        using var response = await AcceptCurrentAsync(client);
        using var again = await AcceptCurrentAsync(client);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        again.StatusCode.Should().Be(HttpStatusCode.OK);
        (await RecordsOfAsync(user.Id)).Should().HaveCount(1);
    }

    // AC13, AC8
    [Fact]
    public async Task Accept_TwentyPostsAtOnce_WriteExactlyOneRow()
    {
        var (user, client) = await SignedInAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => AcceptCurrentAsync(client)));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(19);
        (await RecordsOfAsync(user.Id)).Should().HaveCount(1);
    }

    // AC9, AC12
    [Theory]
    [InlineData("2026-v0", "2026-v3")]
    [InlineData("2026-v1", "2026-v0")]
    [InlineData("2026-v0", "2026-v0")]
    public async Task Accept_VersionsThatAreNotTheCurrentOnes_AreRefusedAndNothingIsWritten(string termsVersion, string privacyVersion)
    {
        var (user, client) = await SignedInAsync();

        using var response = await AcceptAsync(client, termsVersion, privacyVersion);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.LegalAcceptanceVersionOutdated);
        (await RecordsOfAsync(user.Id)).Should().BeEmpty();
    }

    // AC10
    [Fact]
    public async Task BothRoutes_WithoutAToken_AreUnauthorized()
    {
        using var status = await Client().GetAsync(StatusRoute);
        using var accept = await AcceptCurrentAsync(Client());

        status.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        accept.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // AC10
    [Fact]
    public async Task Status_DescribesTheTokensOwnAccountOnly()
    {
        var (accepted, acceptedClient) = await SignedInAsync();
        await AcceptedAsync(accepted.Id, SignUpForm.TermsVersion, SignUpForm.PrivacyVersion);
        var (_, owingClient) = await SignedInAsync();

        (await StatusAsync(acceptedClient)).Owes.Should().BeFalse();
        (await StatusAsync(owingClient)).Owes.Should().BeTrue();
        using var other = await owingClient.GetAsync($"{StatusRoute}?userId={accepted.Id}");
        (await other.Content.ReadFromJsonAsync<LegalAcceptanceStatusResponse>(AppJson.Options))!.Owes
            .Should().BeTrue("a query parameter cannot name another account");
    }

    // AC11
    [Fact]
    public async Task Accept_BodyCarriesAnotherUsersId_TheFieldIsIgnoredAndTheRecordIsTheCallers()
    {
        var (caller, client) = await SignedInAsync();
        var (other, _) = await SignedInAsync();

        using var response = await client.PostAsJsonAsync(
            AcceptRoute,
            new
            {
                userId = other.Id,
                termsVersion = SignUpForm.TermsVersion,
                privacyVersion = SignUpForm.PrivacyVersion,
                locale = "en",
            },
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        (await RecordsOfAsync(caller.Id)).Should().HaveCount(1);
        (await RecordsOfAsync(other.Id)).Should().BeEmpty();
    }

    // AC25
    [Theory]
    [InlineData("xx")]
    [InlineData("../../etc")]
    [InlineData("C:\\temp")]
    [InlineData("pt-BR-aaaaaaaaaaaaaaaaaaaa")]
    [InlineData("")]
    public async Task Accept_ALocaleThatIsNotSupported_IsBadRequestAndNothingIsWritten(string locale)
    {
        var (user, client) = await SignedInAsync();

        using var response = await AcceptAsync(client, SignUpForm.TermsVersion, SignUpForm.PrivacyVersion, locale);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.LegalAcceptanceLocaleInvalid);
        (await RecordsOfAsync(user.Id)).Should().BeEmpty();
    }

    // AC25
    [Fact]
    public async Task Accept_EverySupportedLocale_IsStoredAsSent()
    {
        foreach (var locale in SupportedLocales)
        {
            var (user, client) = await SignedInAsync();

            using var response = await AcceptCurrentAsync(client, locale: locale);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            (await RecordsOfAsync(user.Id)).Single().Locale.Should().Be(locale);
        }
    }

    // AC22
    [Fact]
    public async Task OtherAuthenticatedRoutes_AnswerAsBeforeForAUserWhoOwesAnAcceptance()
    {
        var (_, client) = await SignedInAsync();

        using var session = await client.GetAsync("/api/v1/identity/session");
        using var profile = await client.GetAsync("/api/v1/identity/profile");

        session.StatusCode.Should().Be(HttpStatusCode.OK);
        profile.StatusCode.Should().Be(HttpStatusCode.OK);
    }

}
