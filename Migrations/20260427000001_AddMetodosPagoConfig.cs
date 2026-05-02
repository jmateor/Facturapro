using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Facturapro.Migrations
{
    /// <inheritdoc />
    public partial class AddMetodosPagoConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AceptarEfectivo",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AceptarTarjeta",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AceptarTransferencia",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AceptarSinpe",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AceptarCredito",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AceptarMixto",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "MetodoPagoPorDefecto",
                table: "ConfiguracionEmpresas",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "MostrarOpcionesPago",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PermitirCambio",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreguntarCambio",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoMaximoCambio",
                table: "ConfiguracionEmpresas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AceptarEfectivo",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "AceptarTarjeta",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "AceptarTransferencia",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "AceptarSinpe",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "AceptarCredito",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "AceptarMixto",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "MetodoPagoPorDefecto",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "MostrarOpcionesPago",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "PermitirCambio",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "PreguntarCambio",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "MontoMaximoCambio",
                table: "ConfiguracionEmpresas");
        }
    }
}
