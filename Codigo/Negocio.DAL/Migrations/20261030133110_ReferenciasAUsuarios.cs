using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Negocio.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ReferenciasAUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Turnos_IdUsuarioMedico",
                table: "Turnos",
                column: "IdUsuarioMedico");

            migrationBuilder.CreateIndex(
                name: "IX_Seguimientos_IdUsuarioDecision",
                table: "Seguimientos",
                column: "IdUsuarioDecision");

            migrationBuilder.CreateIndex(
                name: "IX_Seguimientos_IdUsuarioRegistro",
                table: "Seguimientos",
                column: "IdUsuarioRegistro");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_IdUsuarioPortal",
                table: "Pacientes",
                column: "IdUsuarioPortal");

            migrationBuilder.CreateIndex(
                name: "IX_MedicionesRIN_IdUsuarioRegistro",
                table: "MedicionesRIN",
                column: "IdUsuarioRegistro");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAdversos_IdUsuarioRegistro",
                table: "EventosAdversos",
                column: "IdUsuarioRegistro");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_IdUsuarioResolucion",
                table: "Alertas",
                column: "IdUsuarioResolucion");

            migrationBuilder.AddForeignKey(
                name: "FK_Alertas_Usuarios_IdUsuarioResolucion",
                table: "Alertas",
                column: "IdUsuarioResolucion",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EventosAdversos_Usuarios_IdUsuarioRegistro",
                table: "EventosAdversos",
                column: "IdUsuarioRegistro",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicionesRIN_Usuarios_IdUsuarioRegistro",
                table: "MedicionesRIN",
                column: "IdUsuarioRegistro",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pacientes_Usuarios_IdUsuarioPortal",
                table: "Pacientes",
                column: "IdUsuarioPortal",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Seguimientos_Usuarios_IdUsuarioDecision",
                table: "Seguimientos",
                column: "IdUsuarioDecision",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Seguimientos_Usuarios_IdUsuarioRegistro",
                table: "Seguimientos",
                column: "IdUsuarioRegistro",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Usuarios_IdUsuarioMedico",
                table: "Turnos",
                column: "IdUsuarioMedico",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alertas_Usuarios_IdUsuarioResolucion",
                table: "Alertas");

            migrationBuilder.DropForeignKey(
                name: "FK_EventosAdversos_Usuarios_IdUsuarioRegistro",
                table: "EventosAdversos");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicionesRIN_Usuarios_IdUsuarioRegistro",
                table: "MedicionesRIN");

            migrationBuilder.DropForeignKey(
                name: "FK_Pacientes_Usuarios_IdUsuarioPortal",
                table: "Pacientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Seguimientos_Usuarios_IdUsuarioDecision",
                table: "Seguimientos");

            migrationBuilder.DropForeignKey(
                name: "FK_Seguimientos_Usuarios_IdUsuarioRegistro",
                table: "Seguimientos");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Usuarios_IdUsuarioMedico",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_IdUsuarioMedico",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Seguimientos_IdUsuarioDecision",
                table: "Seguimientos");

            migrationBuilder.DropIndex(
                name: "IX_Seguimientos_IdUsuarioRegistro",
                table: "Seguimientos");

            migrationBuilder.DropIndex(
                name: "IX_Pacientes_IdUsuarioPortal",
                table: "Pacientes");

            migrationBuilder.DropIndex(
                name: "IX_MedicionesRIN_IdUsuarioRegistro",
                table: "MedicionesRIN");

            migrationBuilder.DropIndex(
                name: "IX_EventosAdversos_IdUsuarioRegistro",
                table: "EventosAdversos");

            migrationBuilder.DropIndex(
                name: "IX_Alertas_IdUsuarioResolucion",
                table: "Alertas");
        }
    }
}
