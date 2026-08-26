using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMesReferenciaForAnualContaFixa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE conta_fixa
                SET mes_referencia = COALESCE(
                    (SELECT EXTRACT(MONTH FROM MIN(l.data))::int
                     FROM lancamento l
                     WHERE l.conta_fixa_id = conta_fixa.id),
                    8)  -- Default to August (month of migration)
                WHERE periodicidade = 'ANUAL' AND mes_referencia IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE conta_fixa
                SET mes_referencia = NULL
                WHERE periodicidade = 'ANUAL'
            ");
        }
    }
}
