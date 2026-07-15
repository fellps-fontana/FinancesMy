using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddCompraParcelada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "compra_parcelada_id",
                table: "lancamento",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "parcela_numero",
                table: "lancamento",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "compra_parcelada",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantidade_parcelas = table.Column<int>(type: "integer", nullable: false),
                    data_compra = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compra_parcelada", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_compra_parcelada_id",
                table: "lancamento",
                column: "compra_parcelada_id");

            migrationBuilder.AddForeignKey(
                name: "FK_lancamento_compra_parcelada_compra_parcelada_id",
                table: "lancamento",
                column: "compra_parcelada_id",
                principalTable: "compra_parcelada",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lancamento_compra_parcelada_compra_parcelada_id",
                table: "lancamento");

            migrationBuilder.DropTable(
                name: "compra_parcelada");

            migrationBuilder.DropIndex(
                name: "IX_lancamento_compra_parcelada_id",
                table: "lancamento");

            migrationBuilder.DropColumn(
                name: "compra_parcelada_id",
                table: "lancamento");

            migrationBuilder.DropColumn(
                name: "parcela_numero",
                table: "lancamento");
        }
    }
}
