using System.Text.Json;
using Simulab.Identity.Contracts;

namespace Simulab.ExamEngine.Infrastructure.Persistence;

/// <summary>
/// The <c>examEngine</c> section of the file a user downloads (F-167 BR7-BR9): every attempt of the user, read by the
/// token subject alone, as <see cref="Contracts.ExamEngineDataResponse"/>.
/// </summary>
public sealed class ExamEngineDataExportSection(ExamEngineModuleDbContext context) : IDataExportSection
{
    public const string SectionName = "examEngine";

    public string Name => SectionName;

    public Task<JsonElement> GetAsync(Guid userId, CancellationToken cancellationToken) => throw new NotImplementedException();
}
