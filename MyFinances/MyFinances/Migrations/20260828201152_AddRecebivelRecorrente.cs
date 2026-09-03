using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddRecebivelRecorrente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "recebivel_recorrente_id",
                table: "conta_receber",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recebivel_recorrente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    valor = table.Column<decimal>(type: "numeric", nullable: false),
                    periodicidade = table.Column<string>(type: "text", nullable: false),
                    dia_vencimento = table.Column<int>(type: "integer", nullable: true),
                    mes_referencia = table.Column<int>(type: "integer", nullable: true),
                    dia_da_semana = table.Column<string>(type: "text", nullable: true),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recebivel_recorrente", x => x.id);
                    table.ForeignKey(
                        name: "FK_recebivel_recorrente_categoria_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conta_receber_recorrente_data_prevista",
                table: "conta_receber",
                columns: new[] { "recebivel_recorrente_id", "data_prevista" },
                unique: true,
                filter: "recebivel_recorrente_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_recebivel_recorrente_categoria_id",
                table: "recebivel_recorrente",
                column: "categoria_id");

            migrationBuilder.AddForeignKey(
                name: "FK_conta_receber_recebivel_recorrente_recebivel_recorrente_id",
                table: "conta_receber",
                column: "recebivel_recorrente_id",
                principalTable: "recebivel_recorrente",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conta_receber_recebivel_recorrente_recebivel_recorrente_id",
                table: "conta_receber");

            migrationBuilder.DropTable(
                name: "recebivel_recorrente");

            migrationBuilder.DropIndex(
                name: "IX_conta_receber_recorrente_data_prevista",
                table: "conta_receber");

            migrationBuilder.DropColumn(
                name: "recebivel_recorrente_id",
                table: "conta_receber");
        }
    }
}
