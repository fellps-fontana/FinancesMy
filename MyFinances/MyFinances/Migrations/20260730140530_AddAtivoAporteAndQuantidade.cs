using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddAtivoAporteAndQuantidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "quantidade",
                table: "ativo",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ativo_aporte",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ativo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    preco_unitario = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ativo_aporte", x => x.id);
                    table.ForeignKey(
                        name: "FK_ativo_aporte_ativo_ativo_id",
                        column: x => x.ativo_id,
                        principalTable: "ativo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ativo_aporte_ativo_id",
                table: "ativo_aporte",
                column: "ativo_id");

            // Migracao de dados: Ativo legado nao tem quantidade real historica
            // (regra-de-negocio.md item 8.1). quantidade=1 transforma o
            // valor_investido inteiro em preco de uma unidade sintetica, e o
            // aporte sintetico correspondente preserva valor_investido/data_compra
            // como registro auditavel, sem perder nenhum dado ja gravado.
            migrationBuilder.Sql("UPDATE ativo SET quantidade = 1;");

            migrationBuilder.Sql(@"
                INSERT INTO ativo_aporte (id, ativo_id, data, quantidade, preco_unitario, criado_em)
                SELECT gen_random_uuid(), id, data_compra, 1, valor_investido, criado_em
                FROM ativo;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ativo_aporte");

            migrationBuilder.DropColumn(
                name: "quantidade",
                table: "ativo");
        }
    }
}
