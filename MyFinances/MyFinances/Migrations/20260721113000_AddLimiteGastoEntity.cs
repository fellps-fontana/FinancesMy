using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddLimiteGastoEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "limite_gasto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor_limite = table.Column<decimal>(type: "numeric", nullable: false),
                    periodo = table.Column<string>(type: "text", nullable: false, defaultValue: "MENSAL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_limite_gasto", x => x.id);
                    table.ForeignKey(
                        name: "FK_limite_gasto_categoria_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_limite_gasto_categoria_id",
                table: "limite_gasto",
                column: "categoria_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "limite_gasto");
        }
    }
}
