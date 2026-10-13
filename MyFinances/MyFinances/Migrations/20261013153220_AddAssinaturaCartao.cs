using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddAssinaturaCartao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assinatura_cartao_id",
                table: "lancamento",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "assinatura_cartao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    dia_referencia = table.Column<int>(type: "integer", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assinatura_cartao", x => x.id);
                    table.ForeignKey(
                        name: "FK_assinatura_cartao_categoria_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_assinatura_cartao_conta_conta_id",
                        column: x => x.conta_id,
                        principalTable: "conta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_assinatura_cartao_id",
                table: "lancamento",
                column: "assinatura_cartao_id");

            migrationBuilder.CreateIndex(
                name: "IX_assinatura_cartao_categoria_id",
                table: "assinatura_cartao",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_assinatura_cartao_conta_id",
                table: "assinatura_cartao",
                column: "conta_id");

            migrationBuilder.AddForeignKey(
                name: "FK_lancamento_assinatura_cartao_assinatura_cartao_id",
                table: "lancamento",
                column: "assinatura_cartao_id",
                principalTable: "assinatura_cartao",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lancamento_assinatura_cartao_assinatura_cartao_id",
                table: "lancamento");

            migrationBuilder.DropTable(
                name: "assinatura_cartao");

            migrationBuilder.DropIndex(
                name: "IX_lancamento_assinatura_cartao_id",
                table: "lancamento");

            migrationBuilder.DropColumn(
                name: "assinatura_cartao_id",
                table: "lancamento");
        }
    }
}
