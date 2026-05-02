using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Facturapro.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertasConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockMinimoAlerta",
                table: "ConfiguracionEmpresas",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "DiasAlertaVencimiento",
                table: "ConfiguracionEmpresas",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<bool>(
                name: "AlertaCreditoMaximo",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AlertaVentaMaxima",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotificacionSonido",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotificacionPopup",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotificacionEmail",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockMinimoAlerta",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "DiasAlertaVencimiento",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "AlertaCreditoMaximo",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "AlertaVentaMaxima",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "NotificacionSonido",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "NotificacionPopup",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "NotificacionEmail",
                table: "ConfiguracionEmpresas");
        }
    }
}
