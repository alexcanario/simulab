using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Security;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-166 BR10 through HTTP, with a host whose current tenant is a real one: the rows of an attempt carry the tenant of
/// <see cref="ICurrentTenant"/> (null in v1), and the student who made them still reads and changes them through the
/// tenant filter. Its own class: the tenant is a setting of the host.
/// </summary>
public sealed class AttemptTenantTests : ExamEngineApiTests
{
    private static readonly Guid Tenant = Guid.Parse("0198f370-0099-7000-8000-000000000001");

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICurrentTenant>();
            services.AddScoped<ICurrentTenant>(_ => new FixedTenant(Tenant));
        });
    }

    // AC19
    [Fact]
    public async Task EveryRowOfAnAttempt_CarriesTheTenantOfTheHost_AndTheStudentStillReadsAndChangesIt()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await StudentAsync();

        var attempt = await StartedAsync(student, edition.Id);
        await SavedAsync(student, attempt.Id, questions[0].Id, new SingleChoicePayload(1));

        (await ReadAsync(student, attempt.Id)).Questions[0].Answer.Should().Be(new SingleChoicePayload(1));
        (await CountAsync($"SELECT count(*) FROM exam_engine.attempts WHERE id = '{attempt.Id}' AND tenant_id = '{Tenant}'")).Should().Be(1);
        (await CountAsync($"SELECT count(*) FROM exam_engine.attempt_answers WHERE attempt_id = '{attempt.Id}' AND tenant_id = '{Tenant}'")).Should().Be(8);
        (await SubmitAsync(student, attempt.Id)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AC19: an attempt of another tenant is invisible, whoever asks; the answer is the one of an unknown id.
    [Fact]
    public async Task AnAttemptOfAnotherTenant_Answers404ToEveryRoute_AndNothingChanges()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var (student, studentId) = await SignedInWithIdAsync(Simulab.Identity.Contracts.IdentityRoles.Student);
        var attempt = await StartedAsync(student, edition.Id);
        var elsewhere = Guid.Parse("0198f370-0099-7000-8000-000000000002");
        await ValuesAsync($"UPDATE exam_engine.attempts SET tenant_id = '{elsewhere}' WHERE id = '{attempt.Id}' RETURNING id");
        await ValuesAsync($"UPDATE exam_engine.attempt_answers SET tenant_id = '{elsewhere}' WHERE attempt_id = '{attempt.Id}' RETURNING id");

        var responses = new[]
        {
            await student.GetAsync($"{Attempts}/{attempt.Id}"),
            await SaveAsync(student, attempt.Id, questions[0].Id, new SingleChoicePayload(1)),
            await ClearAsync(student, attempt.Id, questions[0].Id),
            await SubmitAsync(student, attempt.Id)
        };

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.NotFound);
        (await CountAsync($"SELECT count(*) FROM exam_engine.attempts WHERE id = '{attempt.Id}' AND status = 'InProgress' AND student_id = '{studentId}'")).Should().Be(1);
        (await CountAsync($"SELECT count(*) FROM exam_engine.attempt_answers WHERE attempt_id = '{attempt.Id}' AND answer IS NOT NULL")).Should().Be(0);
    }

    private sealed class FixedTenant(Guid tenantId) : ICurrentTenant
    {
        public Guid? TenantId => tenantId;
    }
}
