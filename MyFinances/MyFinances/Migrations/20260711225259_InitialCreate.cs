using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ativo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    quantidade = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    preco_medio = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    preco_atual = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ativo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    arquivada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categoria", x => x.id);
                    table.ForeignKey(
                        name: "FK_categoria_categoria_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    origem = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: true),
                    pierre_account_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    saldo_manual = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    dia_fechamento = table.Column<int>(type: "integer", nullable: true),
                    dia_vencimento = table.Column<int>(type: "integer", nullable: true),
                    ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conta", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movimentacao_ativo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ativo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    preco_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimentacao_ativo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "de_para_categoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_pierre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_de_para_categoria", x => x.id);
                    table.ForeignKey(
                        name: "FK_de_para_categoria_categoria_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                        name: "FK_fatura_conta_conta_id",
                        column: x => x.conta_id,
                        principalTable: "conta",
                        principalColumn: "id",
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
                        name: "FK_transferencia_conta_conta_destino_id",
                        column: x => x.conta_destino_id,
                        principalTable: "conta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transferencia_conta_conta_origem_id",
                        column: x => x.conta_origem_id,
                        principalTable: "conta",
                        principalColumn: "id",
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
                        name: "FK_lancamento_categoria_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_lancamento_conta_conta_id",
                        column: x => x.conta_id,
                        principalTable: "conta",
                        principalColumn: "id",
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
                name: "IX_categoria_parent_id",
                table: "categoria",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_de_para_categoria_categoria_id",
                table: "de_para_categoria",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_de_para_categoria_categoria_pierre",
                table: "de_para_categoria",
                column: "categoria_pierre",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_usuario_email",
                table: "usuario",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_username",
                table: "usuario",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ativo");

            migrationBuilder.DropTable(
                name: "de_para_categoria");

            migrationBuilder.DropTable(
                name: "lancamento");

            migrationBuilder.DropTable(
                name: "movimentacao_ativo");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "categoria");

            migrationBuilder.DropTable(
                name: "transferencia");

            migrationBuilder.DropTable(
                name: "fatura");

            migrationBuilder.DropTable(
                name: "conta");
        }
    }
}
