using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Facturapro.Migrations
{
    /// <inheritdoc />
    public partial class AddAnulacionFieldsToFacturaV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "Facturas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoAnulacion",
                table: "Facturas",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "TipoAnulacion",
                table: "Facturas");
        }
    }
}
