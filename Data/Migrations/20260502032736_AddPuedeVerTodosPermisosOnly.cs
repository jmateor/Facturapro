using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Facturapro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPuedeVerTodosPermisosOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PuedeAnularFacturas",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeConfigurarSistema",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeFacturar",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeGestionarClientes",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeGestionarInventario",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeGestionarUsuarios",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeVerCostos",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeVerReportes",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PuedeVerTodosPermisos",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PuedeAnularFacturas",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PuedeConfigurarSistema",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PuedeFacturar",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PuedeGestionarClientes",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PuedeGestionarInventario",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PuedeGestionarUsuarios",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PuedeVerCostos",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PuedeVerReportes",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PuedeVerTodosPermisos",
                table: "AspNetUsers");
        }
    }
}
