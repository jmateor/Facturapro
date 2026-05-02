using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Facturapro.Migrations
{
    /// <inheritdoc />
    public partial class AddDispositivosAndAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_FechaMovimiento",
                table: "MovimientosInventario");

            migrationBuilder.CreateTable(
                name: "ConfiguracionDispositivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HabilitarImpresora = table.Column<bool>(type: "bit", nullable: false),
                    AnchoPapel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorteAutomatico = table.Column<bool>(type: "bit", nullable: false),
                    AbrirCajon = table.Column<bool>(type: "bit", nullable: false),
                    ImprimirCopia = table.Column<bool>(type: "bit", nullable: false),
                    HabilitarLector = table.Column<bool>(type: "bit", nullable: false),
                    ModoEscaneo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SufijoLectura = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SonidoEscaneo = table.Column<bool>(type: "bit", nullable: false),
                    HabilitarPantallaCliente = table.Column<bool>(type: "bit", nullable: false),
                    PuertoPantallaCliente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionDispositivos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogsAuditoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Usuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Modulo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntidadId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Nivel = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsAuditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_Fecha",
                table: "LogsAuditoria",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_Modulo",
                table: "LogsAuditoria",
                column: "Modulo");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_Usuario",
                table: "LogsAuditoria",
                column: "Usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionDispositivos");

            migrationBuilder.DropTable(
                name: "LogsAuditoria");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_FechaMovimiento",
                table: "MovimientosInventario",
                column: "FechaMovimiento");
        }
    }
}
