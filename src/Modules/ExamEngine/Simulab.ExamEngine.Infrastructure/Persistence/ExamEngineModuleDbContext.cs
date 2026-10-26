using Microsoft.EntityFrameworkCore;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;

namespace Simulab.ExamEngine.Infrastructure.Persistence;

/// <summary>
/// The ExamEngine module's context: its own schema, its own migrations (ADR-0001, decision 6). It holds the attempts
/// of students and the answers to their questions (F-166).
/// </summary>
public sealed class ExamEngineModuleDbContext(
    DbContextOptions<ExamEngineModuleDbContext> options,
    ICurrentTenant currentTenant)
    : ModuleDbContext(options, currentTenant)
{
    public const string SchemaName = "exam_engine";

    protected override string Schema => SchemaName;
}
