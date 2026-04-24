using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Facturapro.Migrations
{
    /// <inheritdoc />
    public partial class AddImpresionConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Agregar columnas de configuración de impresión a ConfiguracionEmpresas
            migrationBuilder.AddColumn<string>(
                name: "TipoTicket",
                table: "ConfiguracionEmpresas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "thermal");

            migrationBuilder.AddColumn<bool>(
                name: "MostrarLogo",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "MostrarDescripcion",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "MostrarCodigoBarras",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "MostrarNCF",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MostrarImpuestos",
                table: "ConfiguracionEmpresas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PiePagina",
                table: "ConfiguracionEmpresas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TamanoFuentePie",
                table: "ConfiguracionEmpresas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "medium");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoTicket",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "MostrarLogo",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "MostrarDescripcion",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "MostrarCodigoBarras",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "MostrarNCF",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "MostrarImpuestos",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "PiePagina",
                table: "ConfiguracionEmpresas");

            migrationBuilder.DropColumn(
                name: "TamanoFuentePie",
                table: "ConfiguracionEmpresas");
        }
    }
}
