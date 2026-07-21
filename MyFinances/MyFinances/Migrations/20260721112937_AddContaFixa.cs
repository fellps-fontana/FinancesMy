using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddContaFixa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conta_fixa",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    valor = table.Column<decimal>(type: "numeric", nullable: false),
                    dia_vencimento = table.Column<int>(type: "integer", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conta_fixa", x => x.id);
                    table.ForeignKey(
                        name: "FK_conta_fixa_categoria_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_conta_fixa_conta_conta_id",
                        column: x => x.conta_id,
                        principalTable: "conta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_conta_fixa_id",
                table: "lancamento",
                column: "conta_fixa_id");

            migrationBuilder.CreateIndex(
                name: "IX_conta_fixa_categoria_id",
                table: "conta_fixa",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_conta_fixa_conta_id",
                table: "conta_fixa",
                column: "conta_id");

            migrationBuilder.AddForeignKey(
                name: "FK_lancamento_conta_fixa_conta_fixa_id",
                table: "lancamento",
                column: "conta_fixa_id",
                principalTable: "conta_fixa",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lancamento_conta_fixa_conta_fixa_id",
                table: "lancamento");

            migrationBuilder.DropTable(
                name: "conta_fixa");

            migrationBuilder.DropIndex(
                name: "IX_lancamento_conta_fixa_id",
                table: "lancamento");
        }
    }
}
