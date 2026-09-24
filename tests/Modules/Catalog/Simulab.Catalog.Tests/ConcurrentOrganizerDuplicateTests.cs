using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Application.Organizers;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Tests;

/// <summary>
/// B-14: two writers that both pass the handler's "is taken" check are arbitrated by the unique index.
/// The store here always answers "free", which is what the loser of the race saw, so the database is the
/// only thing left to refuse the second row, and the answer must still be the 409 the check would give.
/// </summary>
public sealed class ConcurrentOrganizerDuplicateTests : CatalogApiTests
{
    [Fact]
    public async Task Create_NameCommittedAfterTheCheck_AnswersNameTaken()
    {
        var name = UniqueName();
        await SaveAsync(id: null, new SaveOrganizerRequest(name, UniqueAcronym(), OrganizerKind.ExamBoard.ToString()));

        var second = await SaveAsync(id: null, new SaveOrganizerRequest(name, UniqueAcronym(), OrganizerKind.ExamBoard.ToString()));

        AssertConflict(second, CatalogErrorCodes.OrganizerNameTaken);
    }

    [Fact]
    public async Task Create_AcronymCommittedAfterTheCheck_AnswersAcronymTaken()
    {
        var acronym = UniqueAcronym();
        await SaveAsync(id: null, new SaveOrganizerRequest(UniqueName(), acronym, OrganizerKind.ExamBoard.ToString()));

        var second = await SaveAsync(id: null, new SaveOrganizerRequest(UniqueName(), acronym, OrganizerKind.ExamBoard.ToString()));

        AssertConflict(second, CatalogErrorCodes.OrganizerAcronymTaken);
    }

    [Fact]
    public async Task Update_RenamedToANameCommittedAfterTheCheck_AnswersNameTaken()
    {
        var taken = UniqueName();
        await SaveAsync(id: null, new SaveOrganizerRequest(taken, UniqueAcronym(), OrganizerKind.ExamBoard.ToString()));
        var other = await SaveAsync(id: null, new SaveOrganizerRequest(UniqueName(), UniqueAcronym(), OrganizerKind.ExamBoard.ToString()));

        var renamed = await SaveAsync(other.Value.Id, new SaveOrganizerRequest(taken, UniqueAcronym(), OrganizerKind.ExamBoard.ToString()));

        AssertConflict(renamed, CatalogErrorCodes.OrganizerNameTaken);
    }

    [Fact]
    public async Task Update_ChangedToAnAcronymCommittedAfterTheCheck_AnswersAcronymTaken()
    {
        var taken = UniqueAcronym();
        await SaveAsync(id: null, new SaveOrganizerRequest(UniqueName(), taken, OrganizerKind.ExamBoard.ToString()));
        var other = await SaveAsync(id: null, new SaveOrganizerRequest(UniqueName(), UniqueAcronym(), OrganizerKind.ExamBoard.ToString()));

        var changed = await SaveAsync(other.Value.Id, new SaveOrganizerRequest(UniqueName(), taken, OrganizerKind.ExamBoard.ToString()));

        AssertConflict(changed, CatalogErrorCodes.OrganizerAcronymTaken);
    }

    [Fact]
    public async Task Create_AfterARefusedDuplicate_TheStoreStillSavesTheNextOrganizer()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = new SaveOrganizerHandler(new AlwaysFreeStore(scope.ServiceProvider.GetRequiredService<IOrganizerStore>()));
        var name = UniqueName();
        (await handler.HandleAsync(null, new SaveOrganizerRequest(name, UniqueAcronym(), OrganizerKind.ExamBoard.ToString()))).IsSuccess.Should().BeTrue();

        var refused = await handler.HandleAsync(null, new SaveOrganizerRequest(name, UniqueAcronym(), OrganizerKind.ExamBoard.ToString()));
        var next = await handler.HandleAsync(null, new SaveOrganizerRequest(UniqueName(), UniqueAcronym(), OrganizerKind.ExamBoard.ToString()));

        refused.IsFailure.Should().BeTrue();
        next.IsSuccess.Should().BeTrue();
    }

    private static void AssertConflict(Result<OrganizerResponse> result, string code)
    {
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(code);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
    }

    private async Task<Result<OrganizerResponse>> SaveAsync(Guid? id, SaveOrganizerRequest request)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = new SaveOrganizerHandler(new AlwaysFreeStore(scope.ServiceProvider.GetRequiredService<IOrganizerStore>()));

        return await handler.HandleAsync(id, request);
    }

    private static string UniqueName() => $"Banca {Guid.CreateVersion7():N}"[..30];

    private static string UniqueAcronym() => Guid.CreateVersion7().ToString("N")[..12];

    /// <summary>The real store, except that the two "is taken" checks never see the other writer.</summary>
    private sealed class AlwaysFreeStore(IOrganizerStore inner) : IOrganizerStore
    {
        public Task<Organizer?> FindAsync(Guid id, CancellationToken cancellationToken) => inner.FindAsync(id, cancellationToken);

        public Task<bool> NameIsTakenAsync(string normalizedName, Guid? exceptId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> AcronymIsTakenAsync(string normalizedAcronym, Guid? exceptId, CancellationToken cancellationToken) => Task.FromResult(false);

        public void Add(Organizer organizer) => inner.Add(organizer);

        public void Remove(Organizer organizer) => inner.Remove(organizer);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => inner.SaveChangesAsync(cancellationToken);

        public Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken) => inner.TrySaveChangesAsync(cancellationToken);
    }
}
