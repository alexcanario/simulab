using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Tests;

/// <summary>B-14 AC4: only the two organizer unique indexes are translated; every other database error stays an error.</summary>
public sealed class OrganizerUniqueViolationsTests
{
    [Theory]
    [InlineData(OrganizerUniqueViolations.NameIndex, CatalogErrorCodes.OrganizerNameTaken)]
    [InlineData(OrganizerUniqueViolations.AcronymIndex, CatalogErrorCodes.OrganizerAcronymTaken)]
    public void Translate_UniqueViolationOfAnOrganizerIndex_IsTheConflictOfThatField(string index, string code)
    {
        var error = OrganizerUniqueViolations.Translate(Refused(PostgresErrorCodes.UniqueViolation, index));

        error.Should().Be(new Error(code, ErrorKind.Conflict));
    }

    [Fact]
    public void Translate_UniqueViolationOfAnotherConstraint_IsNotTranslated() =>
        OrganizerUniqueViolations.Translate(Refused(PostgresErrorCodes.UniqueViolation, "pk_organizers")).Should().BeNull();

    [Fact]
    public void Translate_OtherErrorOnAnOrganizerIndex_IsNotTranslated() =>
        OrganizerUniqueViolations.Translate(Refused(PostgresErrorCodes.NotNullViolation, OrganizerUniqueViolations.NameIndex)).Should().BeNull();

    [Fact]
    public void Translate_NotAPostgresError_IsNotTranslated() =>
        OrganizerUniqueViolations.Translate(new DbUpdateException("boom", new InvalidOperationException())).Should().BeNull();

    private static DbUpdateException Refused(string sqlState, string constraint) =>
        new("save failed", new PostgresException("refused", "ERROR", "ERROR", sqlState, constraintName: constraint));
}
