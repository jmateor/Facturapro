using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Facturapro.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegracionesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionIntegraciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmailHabilitado = table.Column<bool>(type: "bit", nullable: false),
                    SmtpServer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    SmtpUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SmtpPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpUseSSL = table.Column<bool>(type: "bit", nullable: false),
                    WhatsAppHabilitado = table.Column<bool>(type: "bit", nullable: false),
                    WhatsAppApiKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WhatsAppPhoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DgiiValidacionHabilitada = table.Column<bool>(type: "bit", nullable: false),
                    GoogleDriveHabilitado = table.Column<bool>(type: "bit", nullable: false),
                    PasarelaPagoHabilitada = table.Column<bool>(type: "bit", nullable: false),
                    PasarelaProveedor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionIntegraciones", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionIntegraciones");
        }
    }
}
