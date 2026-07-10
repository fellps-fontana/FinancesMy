using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations.MyFinancesDb
{
    /// <inheritdoc />
    public partial class AdicionaLancamentoTransferenciaFaturaDoCartao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fatura",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_fechamento = table.Column<DateOnly>(type: "date", nullable: false),
                    data_vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fatura", x => x.id);
                    table.ForeignKey(
                        name: "FK_fatura_Contas_conta_id",
                        column: x => x.conta_id,
                        principalTable: "Contas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transferencia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    conta_origem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_destino_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fatura_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencia", x => x.id);
                    table.ForeignKey(
                        name: "FK_transferencia_Contas_conta_destino_id",
                        column: x => x.conta_destino_id,
                        principalTable: "Contas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transferencia_Contas_conta_origem_id",
                        column: x => x.conta_origem_id,
                        principalTable: "Contas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transferencia_fatura_fatura_id",
                        column: x => x.fatura_id,
                        principalTable: "fatura",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "lancamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pierre_txn_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    manual = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    oculto = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    conta_fixa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conciliado_com = table.Column<Guid>(type: "uuid", nullable: true),
                    transferencia_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fatura_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamento", x => x.id);
                    table.ForeignKey(
                        name: "FK_lancamento_Categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_lancamento_Contas_conta_id",
                        column: x => x.conta_id,
                        principalTable: "Contas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lancamento_fatura_fatura_id",
                        column: x => x.fatura_id,
                        principalTable: "fatura",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_lancamento_lancamento_conciliado_com",
                        column: x => x.conciliado_com,
                        principalTable: "lancamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_lancamento_transferencia_transferencia_id",
                        column: x => x.transferencia_id,
                        principalTable: "transferencia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fatura_conta_aberta",
                table: "fatura",
                columns: new[] { "conta_id", "status" },
                unique: true,
                filter: "status = 'ABERTA'");

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_categoria_id",
                table: "lancamento",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_conciliado_com",
                table: "lancamento",
                column: "conciliado_com");

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_conta_id",
                table: "lancamento",
                column: "conta_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_fatura_id",
                table: "lancamento",
                column: "fatura_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_pierre_txn_id",
                table: "lancamento",
                column: "pierre_txn_id",
                unique: true,
                filter: "pierre_txn_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_transferencia_id",
                table: "lancamento",
                column: "transferencia_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencia_conta_destino_id",
                table: "transferencia",
                column: "conta_destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencia_conta_origem_id",
                table: "transferencia",
                column: "conta_origem_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencia_fatura_id",
                table: "transferencia",
                column: "fatura_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lancamento");

            migrationBuilder.DropTable(
                name: "transferencia");

            migrationBuilder.DropTable(
                name: "fatura");
        }
    }
}
