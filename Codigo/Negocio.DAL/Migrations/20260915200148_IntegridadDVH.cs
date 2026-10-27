using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Negocio.DAL.Migrations
{
    /// <inheritdoc />
    public partial class IntegridadDVH : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Usuarios",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Turnos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Seguimientos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "ReportesEstadisticos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Pacientes",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "ObrasSociales",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "MedicionesRIN",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "HistoriasClinicas",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "EventosAdversos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Diagnosticos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Alertas",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DigitosVerificadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreTabla = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DVV = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FechaCalculo = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitosVerificadores", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Diagnosticos",
                keyColumn: "Id",
                keyValue: 1,
                column: "DVH",
                value: null);

            migrationBuilder.UpdateData(
                table: "Diagnosticos",
                keyColumn: "Id",
                keyValue: 2,
                column: "DVH",
                value: null);

            migrationBuilder.UpdateData(
                table: "Diagnosticos",
                keyColumn: "Id",
                keyValue: 3,
                column: "DVH",
                value: null);

            migrationBuilder.UpdateData(
                table: "Diagnosticos",
                keyColumn: "Id",
                keyValue: 4,
                column: "DVH",
                value: null);

            migrationBuilder.UpdateData(
                table: "Diagnosticos",
                keyColumn: "Id",
                keyValue: 5,
                column: "DVH",
                value: null);

            migrationBuilder.UpdateData(
                table: "ObrasSociales",
                keyColumn: "Id",
                keyValue: 1,
                column: "DVH",
                value: null);

            migrationBuilder.UpdateData(
                table: "ObrasSociales",
                keyColumn: "Id",
                keyValue: 2,
                column: "DVH",
                value: null);

            migrationBuilder.UpdateData(
                table: "ObrasSociales",
                keyColumn: "Id",
                keyValue: 3,
                column: "DVH",
                value: null);

            migrationBuilder.UpdateData(
                table: "ObrasSociales",
                keyColumn: "Id",
                keyValue: 4,
                column: "DVH",
                value: null);

            migrationBuilder.UpdateData(
                table: "ObrasSociales",
                keyColumn: "Id",
                keyValue: 5,
                column: "DVH",
                value: null);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "DVH",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_DigitosVerificadores_NombreTabla",
                table: "DigitosVerificadores",
                column: "NombreTabla",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigitosVerificadores");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Seguimientos");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "ReportesEstadisticos");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "ObrasSociales");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "MedicionesRIN");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "HistoriasClinicas");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "EventosAdversos");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Diagnosticos");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Alertas");
        }
    }
}
