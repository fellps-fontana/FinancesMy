using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddContaReceberIdAndMakeContaDestinoIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "conta_destino_id",
                table: "transferencia",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "conta_receber_id",
                table: "transferencia",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "conta_receber_id",
                table: "lancamento",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transferencia_conta_receber_id",
                table: "transferencia",
                column: "conta_receber_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_conta_receber_id",
                table: "lancamento",
                column: "conta_receber_id");

            migrationBuilder.AddForeignKey(
                name: "FK_lancamento_conta_receber_conta_receber_id",
                table: "lancamento",
                column: "conta_receber_id",
                principalTable: "conta_receber",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_transferencia_conta_receber_conta_receber_id",
                table: "transferencia",
                column: "conta_receber_id",
                principalTable: "conta_receber",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lancamento_conta_receber_conta_receber_id",
                table: "lancamento");

            migrationBuilder.DropForeignKey(
                name: "FK_transferencia_conta_receber_conta_receber_id",
                table: "transferencia");

            migrationBuilder.DropIndex(
                name: "IX_transferencia_conta_receber_id",
                table: "transferencia");

            migrationBuilder.DropIndex(
                name: "IX_lancamento_conta_receber_id",
                table: "lancamento");

            migrationBuilder.DropColumn(
                name: "conta_receber_id",
                table: "transferencia");

            migrationBuilder.DropColumn(
                name: "conta_receber_id",
                table: "lancamento");

            migrationBuilder.AlterColumn<Guid>(
                name: "conta_destino_id",
                table: "transferencia",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
