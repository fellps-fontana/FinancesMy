using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Migrations.MyFinancesDb
{
    /// <inheritdoc />
    public partial class InitialCreateDeParaCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeParaCategorias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaPierre = table.Column<string>(type: "text", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeParaCategorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeParaCategorias_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeParaCategorias_CategoriaId",
                table: "DeParaCategorias",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_DeParaCategorias_CategoriaPierre",
                table: "DeParaCategorias",
                column: "CategoriaPierre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeParaCategorias");
        }
    }
}
