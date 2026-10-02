using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeExamStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // F-42 BR6: a State exam whose free text names a state without doubt now holds that state's
            // acronym. "Without doubt" is the whole text, accents and case aside, being the state's name, its
            // acronym, or the name and the acronym joined as "name (AC)", "name/AC" or "name - AC". Anything
            // else (Sampa) stays as it was and is fixed at the next edit. Municipal and National rows are not
            // touched. normalized_scope_detail follows, as the entity writes it: the name and the acronym, so
            // the student search keeps finding the exam by either (BR5).
            //
            // The list is the one of BrazilianStates, written out: a migration is history and does not read
            // code that may change. Names are already folded (upper case, no accents), the way the translate
            // step below folds the stored text; under a "C" collation upper() leaves non-ASCII letters alone.
            migrationBuilder.Sql(
                """
                WITH states (acronym, name_key) AS (VALUES
                    ('AC', 'ACRE'), ('AL', 'ALAGOAS'), ('AP', 'AMAPA'), ('AM', 'AMAZONAS'), ('BA', 'BAHIA'),
                    ('CE', 'CEARA'), ('DF', 'DISTRITO FEDERAL'), ('ES', 'ESPIRITO SANTO'), ('GO', 'GOIAS'),
                    ('MA', 'MARANHAO'), ('MT', 'MATO GROSSO'), ('MS', 'MATO GROSSO DO SUL'), ('MG', 'MINAS GERAIS'),
                    ('PA', 'PARA'), ('PB', 'PARAIBA'), ('PR', 'PARANA'), ('PE', 'PERNAMBUCO'), ('PI', 'PIAUI'),
                    ('RJ', 'RIO DE JANEIRO'), ('RN', 'RIO GRANDE DO NORTE'), ('RS', 'RIO GRANDE DO SUL'),
                    ('RO', 'RONDONIA'), ('RR', 'RORAIMA'), ('SC', 'SANTA CATARINA'), ('SP', 'SAO PAULO'),
                    ('SE', 'SERGIPE'), ('TO', 'TOCANTINS')),
                stored AS (
                    SELECT id,
                           upper(translate(
                               regexp_replace(btrim(scope_detail), '\s+', ' ', 'g'),
                               'àáâãäåèéêëìíîïòóôõöùúûüýÿçñÀÁÂÃÄÅÈÉÊËÌÍÎÏÒÓÔÕÖÙÚÛÜÝÇÑ',
                               'aaaaaaeeeeiiiiooooouuuuyycnAAAAAAEEEEIIIIOOOOOUUUUYCN')) AS text_key
                    FROM catalog.exams
                    WHERE scope = 'State' AND scope_detail IS NOT NULL)
                UPDATE catalog.exams AS exam
                SET scope_detail = state.acronym,
                    normalized_scope_detail = state.name_key || ' ' || state.acronym
                FROM stored
                JOIN states AS state ON stored.text_key IN (
                    state.name_key,
                    state.acronym,
                    state.name_key || ' (' || state.acronym || ')',
                    state.name_key || '/' || state.acronym,
                    state.name_key || ' - ' || state.acronym)
                WHERE exam.id = stored.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A state that was written as free text before cannot be told apart from one that was picked, so
            // there is nothing to restore: the acronyms stay, and they are valid text for the old column.
        }
    }
}
