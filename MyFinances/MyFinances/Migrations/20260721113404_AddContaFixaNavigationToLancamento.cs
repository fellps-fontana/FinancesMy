using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddContaFixaNavigationToLancamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lancamento_conta_fixa_conta_fixa_id",
                table: "lancamento");

            migrationBuilder.AddForeignKey(
                name: "FK_lancamento_conta_fixa_conta_fixa_id",
                table: "lancamento",
                column: "conta_fixa_id",
                principalTable: "conta_fixa",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lancamento_conta_fixa_conta_fixa_id",
                table: "lancamento");

            migrationBuilder.AddForeignKey(
                name: "FK_lancamento_conta_fixa_conta_fixa_id",
                table: "lancamento",
                column: "conta_fixa_id",
                principalTable: "conta_fixa",
                principalColumn: "id");
        }
    }
}
