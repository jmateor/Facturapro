using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Facturapro.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalFieldsToCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormaPago",
                table: "Compras",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ISC",
                table: "Compras",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISRPercibido",
                table: "Compras",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ITBISCosto",
                table: "Compras",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ITBISPercibido",
                table: "Compras",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ITBISProporcionalidad",
                table: "Compras",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoBienes",
                table: "Compras",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoServicios",
                table: "Compras",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NCFModificado",
                table: "Compras",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtrosImpuestos",
                table: "Compras",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PropinaLegal",
                table: "Compras",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TipoRetencionISR",
                table: "Compras",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormaPago",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ISC",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ISRPercibido",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ITBISCosto",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ITBISPercibido",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ITBISProporcionalidad",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "MontoBienes",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "MontoServicios",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "NCFModificado",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "OtrosImpuestos",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "PropinaLegal",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "TipoRetencionISR",
                table: "Compras");
        }
    }
}
