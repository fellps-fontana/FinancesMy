using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanoMensal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plano_mensal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ano = table.Column<int>(type: "integer", nullable: false),
                    mes = table.Column<int>(type: "integer", nullable: false),
                    renda_prevista = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    percentual_investimento = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plano_mensal", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plano_mensal_sublimite",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plano_mensal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plano_mensal_sublimite", x => x.id);
                    table.ForeignKey(
                        name: "FK_plano_mensal_sublimite_categoria_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plano_mensal_sublimite_plano_mensal_plano_mensal_id",
                        column: x => x.plano_mensal_id,
                        principalTable: "plano_mensal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plano_mensal_ano_mes",
                table: "plano_mensal",
                columns: new[] { "ano", "mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plano_mensal_sublimite_categoria_id",
                table: "plano_mensal_sublimite",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_plano_mensal_sublimite_plano_categoria",
                table: "plano_mensal_sublimite",
                columns: new[] { "plano_mensal_id", "categoria_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plano_mensal_sublimite");

            migrationBuilder.DropTable(
                name: "plano_mensal");
        }
    }
}
