using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations
{
    /// <inheritdoc />
    public partial class AddContaIdToAtivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "instituicao",
                table: "ativo",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<Guid>(
                name: "conta_id",
                table: "ativo",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ativo_conta_id",
                table: "ativo",
                column: "conta_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ativo_conta_conta_id",
                table: "ativo",
                column: "conta_id",
                principalTable: "conta",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ativo_conta_conta_id",
                table: "ativo");

            migrationBuilder.DropIndex(
                name: "IX_ativo_conta_id",
                table: "ativo");

            migrationBuilder.DropColumn(
                name: "conta_id",
                table: "ativo");

            migrationBuilder.AlterColumn<string>(
                name: "instituicao",
                table: "ativo",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
