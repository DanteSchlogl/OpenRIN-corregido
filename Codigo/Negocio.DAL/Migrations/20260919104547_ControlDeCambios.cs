using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Negocio.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ControlDeCambios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditoriaCambios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Entidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdRegistro = table.Column<int>(type: "int", nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoCambio = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DatosAnteriores = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatosNuevos = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaCambios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaCambios_Entidad_IdRegistro",
                table: "AuditoriaCambios",
                columns: new[] { "Entidad", "IdRegistro" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriaCambios");
        }
    }
}
