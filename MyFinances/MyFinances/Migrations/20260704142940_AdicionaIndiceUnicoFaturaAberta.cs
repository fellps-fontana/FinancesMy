using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaIndiceUnicoFaturaAberta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faturas_ContaId",
                table: "Faturas");

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_ContaId",
                table: "Faturas",
                column: "ContaId",
                unique: true,
                filter: "\"Status\" = 'ABERTA'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faturas_ContaId",
                table: "Faturas");

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_ContaId",
                table: "Faturas",
                column: "ContaId");
        }
    }
}
