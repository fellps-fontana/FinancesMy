using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AlteraFaturaTransferenciaParaUmParaMuitos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faturas_Transferencias_TransferenciaId",
                table: "Faturas");

            migrationBuilder.DropIndex(
                name: "IX_Faturas_TransferenciaId",
                table: "Faturas");

            migrationBuilder.DropColumn(
                name: "TransferenciaId",
                table: "Faturas");

            migrationBuilder.AddColumn<Guid>(
                name: "FaturaId",
                table: "Transferencias",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_FaturaId",
                table: "Transferencias",
                column: "FaturaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transferencias_Faturas_FaturaId",
                table: "Transferencias",
                column: "FaturaId",
                principalTable: "Faturas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transferencias_Faturas_FaturaId",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "IX_Transferencias_FaturaId",
                table: "Transferencias");

            migrationBuilder.DropColumn(
                name: "FaturaId",
                table: "Transferencias");

            migrationBuilder.AddColumn<Guid>(
                name: "TransferenciaId",
                table: "Faturas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_TransferenciaId",
                table: "Faturas",
                column: "TransferenciaId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Faturas_Transferencias_TransferenciaId",
                table: "Faturas",
                column: "TransferenciaId",
                principalTable: "Transferencias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
