using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Facturapro.Migrations
{
    /// <inheritdoc />
    public partial class AddVentasConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ClienteObligatorio",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DecimalesPrecios",
                table: "ConfiguracionEmpresas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "DescuentoCantidad",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DiasPlazoCredito",
                table: "ConfiguracionEmpresas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ImprimirAutomático",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Moneda",
                table: "ConfiguracionEmpresas",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MontoCreditoMaximo",
                table: "ConfiguracionEmpresas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoVentaMaxima",
                table: "ConfiguracionEmpresas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoVentaMinima",
                table: "ConfiguracionEmpresas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "PermitirVentasCredito",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RedondeoTotales",
                table: "ConfiguracionEmpresas",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimboloMoneda",
                table: "ConfiguracionEmpresas",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "StockNegativo",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TipoComprobanteDefecto",
                table: "ConfiguracionEmpresas",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoIngresoPorDefecto",
                table: "ConfiguracionEmpresas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TipoPagoPorDefecto",
                table: "ConfiguracionEmpresas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UsarValoresDefecto",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClienteObligatorio",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "DecimalesPrecios",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "DescuentoCantidad",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "DiasPlazoCredito",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "ImprimirAutomático",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "Moneda",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "MontoCreditoMaximo",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "MontoVentaMaxima",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "MontoVentaMinima",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "PermitirVentasCredito",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "RedondeoTotales",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "SimboloMoneda",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "StockNegativo",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "TipoComprobanteDefecto",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "TipoIngresoPorDefecto",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "TipoPagoPorDefecto",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "UsarValoresDefecto",
                table: "ConfiguracionEmpresas");
        }
    }
}
