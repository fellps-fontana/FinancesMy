using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddIndiceUnicoFaturaContaDataFechamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Aborta se ja existir duplicata (conta_id, data_fechamento): exige limpeza manual antes do indice unico.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM fatura
                        GROUP BY conta_id, data_fechamento
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicatas de fatura (conta_id, data_fechamento). Migration abortada: consolidar manualmente.';
                    END IF;
                END $$;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_fatura_conta_data_fechamento",
                table: "fatura",
                columns: new[] { "conta_id", "data_fechamento" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_fatura_conta_data_fechamento",
                table: "fatura");
        }
    }
}
