using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations.MyFinancesDb
{
    /// <inheritdoc />
    public partial class RemoveIndiceFixLancamentoStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_lancamento_conta_fatura_aberta",
                table: "lancamento");

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_conta_id",
                table: "lancamento",
                column: "conta_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_lancamento_conta_id",
                table: "lancamento");

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_conta_fatura_aberta",
                table: "lancamento",
                columns: new[] { "conta_id", "fatura_id", "status" },
                unique: true,
                filter: "fatura_id IS NOT NULL AND status = 'ABERTA'");
        }
    }
}
