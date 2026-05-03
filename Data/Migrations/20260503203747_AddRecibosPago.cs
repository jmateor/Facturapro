using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Facturapro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecibosPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecibosPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacturaId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoEfectivo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoTarjeta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoTransferencia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecibosPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecibosPago_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecibosPago_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecibosPago_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecibosPago_ClienteId",
                table: "RecibosPago",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_RecibosPago_FacturaId",
                table: "RecibosPago",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_RecibosPago_UsuarioId",
                table: "RecibosPago",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecibosPago");
        }
    }
}
