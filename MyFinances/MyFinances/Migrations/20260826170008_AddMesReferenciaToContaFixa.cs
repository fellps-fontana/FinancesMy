using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddMesReferenciaToContaFixa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "mes_referencia",
                table: "conta_fixa",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mes_referencia",
                table: "conta_fixa");
        }
    }
}
