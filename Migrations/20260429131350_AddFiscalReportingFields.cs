using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Facturapro.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalReportingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoISRRetenido",
                table: "Facturas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoITBISRetenido",
                table: "Facturas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NCFModificado",
                table: "Facturas",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPago",
                table: "Compras",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoISRRetenido",
                table: "Compras",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoITBISRetenido",
                table: "Compras",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NCF",
                table: "Compras",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoGasto",
                table: "Compras",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoISRRetenido",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "MontoITBISRetenido",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "NCFModificado",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "FechaPago",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "MontoISRRetenido",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "MontoITBISRetenido",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "NCF",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "TipoGasto",
                table: "Compras");
        }
    }
}
