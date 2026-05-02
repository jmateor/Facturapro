using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Facturapro.Models.Entities;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace Facturapro.Services.PDF
{
    /// <summary>
    /// Servicio para generar representaciones impresas (PDF) de facturas electrónicas
    /// </summary>
    public interface IPdfService
    {
        byte[] GenerarFacturaPDF(Factura factura, ConfiguracionEmpresa empresa);
        byte[] GenerarFacturaPDF(Factura factura, ConfiguracionEmpresa empresa, string? logoPath);
    }

    public class PdfService : IPdfService
    {
        private const string COLOR_PRIMARIO = "#2563eb"; // Azul corporativo
        private const string COLOR_SECUNDARIO = "#64748b"; // Gris
        private const string COLOR_FONDO = "#f8fafc"; // Gris claro

        public byte[] GenerarFacturaPDF(Factura factura, ConfiguracionEmpresa empresa)
        {
            return GenerarFacturaPDF(factura, empresa, null);
        }

        public byte[] GenerarFacturaPDF(Factura factura, ConfiguracionEmpresa empresa, string? logoPath)
        {
            // Generar código QR con la información del comprobante
            var qrCode = GenerarQRCode(factura, empresa);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Element(header => ComposeHeader(header, factura, empresa, logoPath));
                    page.Content().Element(content => ComposeContent(content, factura, empresa));
                    page.Footer().Element(footer => ComposeFooter(footer, factura, empresa, qrCode));
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, Factura factura, ConfiguracionEmpresa empresa, string? logoPath)
        {
            container.Column(column =>
            {
                // Primera fila: Logo y datos de la empresa
                column.Item().Row(row =>
                {
                    // Logo o nombre de la empresa
                    row.RelativeItem().Column(col =>
                    {
                        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                        {
                            try
                            {
                                col.Item().Height(60).Image(logoPath).FitArea();
                            }
                            catch
                            {
                                // Si falla la carga del logo, mostrar nombre
                                col.Item().Text(empresa.NombreComercial ?? empresa.RazonSocialEmisor)
                                    .Bold().FontSize(18).FontColor(COLOR_PRIMARIO);
                            }
                        }
                        else
                        {
                            col.Item().Text(empresa.NombreComercial ?? empresa.RazonSocialEmisor)
                                .Bold().FontSize(18).FontColor(COLOR_PRIMARIO);
                        }

                        col.Item().Text(text =>
                        {
                            text.Span("RNC: ").SemiBold();
                            text.Span(empresa.RNCEmisor);
                        });

                        col.Item().Text(empresa.DireccionEmisor);

                        if (!string.IsNullOrEmpty(empresa.TelefonoEmisor))
                        {
                            col.Item().Text($"Tel: {empresa.TelefonoEmisor}");
                        }

                        if (!string.IsNullOrEmpty(empresa.CorreoEmisor))
                        {
                            col.Item().Text($"Email: {empresa.CorreoEmisor}");
                        }
                    });

                    // Tipo de comprobante y número
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        // Caja del tipo de comprobante
                        col.Item()
                            .Background(COLOR_PRIMARIO)
                            .Padding(10)
                            .AlignCenter()
                            .Text(factura.TipoECFTexto)
                            .Bold().FontSize(12).FontColor(Colors.White);

                        col.Item().Height(5);

                        // e-CF
                        if (!string.IsNullOrEmpty(factura.eNCF))
                        {
                            col.Item()
                                .Border(1)
                                .BorderColor(COLOR_PRIMARIO)
                                .Padding(8)
                                .Column(c =>
                                {
                                    c.Item().Text("e-CF (Número Electrónico)").FontSize(9).SemiBold();
                                    c.Item().Text(factura.eNCF).FontSize(11).Bold().FontColor(COLOR_PRIMARIO);
                                });
                        }
                        else
                        {
                            col.Item()
                                .Border(1)
                                .BorderColor(Colors.Red.Medium)
                                .Padding(8)
                                .AlignCenter()
                                .Text("FACTURA NO FIRMADA")
                                .FontSize(10).Bold().FontColor(Colors.Red.Medium);
                        }
                    });
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(COLOR_SECUNDARIO);
            });
        }

        private void ComposeContent(IContainer container, Factura factura, ConfiguracionEmpresa empresa)
        {
            container.Column(column =>
            {
                // Datos del cliente
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FACTURAR A:").Bold().FontSize(11).FontColor(COLOR_PRIMARIO);
                        col.Item().Height(5);

                        col.Item().Text(factura.Cliente?.Nombre ?? "Consumidor Final").SemiBold();

                        if (!string.IsNullOrEmpty(factura.Cliente?.RNC))
                        {
                            col.Item().Text($"RNC: {factura.Cliente.RNC}");
                        }

                        if (!string.IsNullOrEmpty(factura.Cliente?.Direccion))
                        {
                            col.Item().Text(factura.Cliente.Direccion);
                        }

                        if (!string.IsNullOrEmpty(factura.Cliente?.Telefono))
                        {
                            col.Item().Text($"Tel: {factura.Cliente.Telefono}");
                        }
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("FECHA DE EMISIÓN:").Bold().FontSize(11).FontColor(COLOR_PRIMARIO);
                        col.Item().Height(5);

                        col.Item().Text(factura.FechaEmision.ToString("dd/MM/yyyy HH:mm"));

                        col.Item().Height(10);

                        col.Item().Text("Nº INTERNO:").Bold().FontSize(10).FontColor(COLOR_SECUNDARIO);
                        col.Item().Text(factura.NumeroFactura);

                        col.Item().Height(5);

                        col.Item().Text("VENCE:").Bold().FontSize(10).FontColor(COLOR_SECUNDARIO);
                        col.Item().Text(factura.FechaVencimiento.ToString("dd/MM/yyyy"));
                    });
                });

                column.Item().Height(15);

                // Tabla de líneas
                column.Item().Element(table => ComposeTable(table, factura));

                column.Item().Height(15);

                // Totales
                column.Item().AlignRight().Column(col =>
                {
                    col.Item().Width(250).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                        });

                        // Subtotal
                        table.Cell().Text("Subtotal:").AlignRight().SemiBold();
                        table.Cell().Text($"$ {factura.Subtotal:N2}").AlignRight();

                        // ITBIS
                        table.Cell().Text($"ITBIS ({factura.PorcentajeITBIS}%):").AlignRight().SemiBold();
                        table.Cell().Text($"$ {factura.MontoITBIS:N2}").AlignRight();

                        // Línea separadora
                        table.Cell().ColumnSpan(2).PaddingVertical(5).LineHorizontal(1).LineColor(COLOR_SECUNDARIO);

                        // Total
                        table.Cell().Text("TOTAL:").AlignRight().Bold().FontSize(12).FontColor(COLOR_PRIMARIO);
                        table.Cell().Text($"$ {factura.Total:N2}").AlignRight().Bold().FontSize(12).FontColor(COLOR_PRIMARIO);
                    });
                });

                // Notas
                if (!string.IsNullOrEmpty(factura.Notas))
                {
                    column.Item().Height(20);
                    column.Item().Text("Notas:").SemiBold().FontSize(10);
                    column.Item().Text(factura.Notas).FontSize(9).FontColor(COLOR_SECUNDARIO);
                }

                // Estado DGII
                column.Item().Height(15);
                column.Item().Border(1)
                    .BorderColor(Colors.Grey.Lighten1)
                    .Background(Colors.Grey.Lighten3)
                    .Padding(8)
                    .Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.AutoItem().Text("Estado DGII:").SemiBold();

                            var (bgColor, textColor) = GetEstadoDGIColors(factura.EstadoDGII);
                            row.AutoItem().PaddingLeft(5)
                                .Background(bgColor)
                                .PaddingHorizontal(8)
                                .PaddingVertical(2)
                                .Text(factura.EstadoDGII ?? "Pendiente")
                                .FontColor(textColor).Bold().FontSize(9);
                        });

                        if (!string.IsNullOrEmpty(factura.MensajeDGII))
                        {
                            col.Item().Height(5);
                            col.Item().Text(factura.MensajeDGII).FontSize(9).FontColor(COLOR_SECUNDARIO);
                        }
                    });
            });
        }

        private void ComposeTable(IContainer container, Factura factura)
        {
            container.Table(table =>
            {
                // Definir columnas
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);   // #
                    columns.RelativeColumn(4);    // Descripción
                    columns.RelativeColumn(1.5f); // Cantidad
                    columns.RelativeColumn(2);    // Precio
                    columns.RelativeColumn(1.5f); // Desc
                    columns.RelativeColumn(2);    // Total
                });

                // Encabezados
                table.Header(header =>
                {
                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("#").FontColor(Colors.White).Bold();
                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("DESCRIPCIÓN").FontColor(Colors.White).Bold();
                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).AlignCenter().Text("CANT.").FontColor(Colors.White).Bold();
                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).AlignRight().Text("PRECIO").FontColor(Colors.White).Bold();
                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).AlignRight().Text("DESC.").FontColor(Colors.White).Bold();
                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).AlignRight().Text("TOTAL").FontColor(Colors.White).Bold();
                });

                // Filas de datos
                if (factura.Lineas != null)
                {
                    int rowNum = 0;
                    foreach (var linea in factura.Lineas.OrderBy(l => l.Orden))
                    {
                        var bgColor = rowNum % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;

                        table.Cell().Background(bgColor).Padding(5).Text((rowNum + 1).ToString());
                        table.Cell().Background(bgColor).Padding(5).Text(linea.Descripcion);
                        table.Cell().Background(bgColor).Padding(5).AlignCenter().Text(linea.Cantidad.ToString("N2"));
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"$ {linea.PrecioUnitario:N2}");
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"$ {linea.MontoDescuento:N2}");
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"$ {linea.MontoItem:N2}");

                        rowNum++;
                    }
                }
            });
        }

        private void ComposeFooter(IContainer container, Factura factura, ConfiguracionEmpresa empresa, byte[]? qrCode)
        {
            container.Column(column =>
            {
                // Línea separadora
                column.Item().LineHorizontal(1).LineColor(COLOR_SECUNDARIO);
                column.Item().Height(10);

                // Sección del QR y firmas
                column.Item().Row(row =>
                {
                    // QR Code
                    if (qrCode != null)
                    {
                        row.RelativeItem().Row(innerRow =>
                        {
                            innerRow.AutoItem().Width(80).Height(80).Image(qrCode);
                            innerRow.AutoItem().PaddingLeft(10).Column(col =>
                            {
                                col.Item().Text("Escanea el código QR").SemiBold().FontSize(9);
                                col.Item().Text("para verificar la").FontSize(8).FontColor(COLOR_SECUNDARIO);
                                col.Item().Text("factura electrónica").FontSize(8).FontColor(COLOR_SECUNDARIO);
                                col.Item().Height(5);
                                col.Item().Text("Enlace de verificación:").SemiBold().FontSize(8);
                                col.Item().Text(GenerarEnlaceVerificacion(factura)).FontSize(7).FontColor(COLOR_PRIMARIO);
                            });
                        });
                    }

                    // Firmas
                    row.RelativeItem().AlignRight().PaddingRight(20).Column(col =>
                    {
                        col.Item().Height(40).AlignBottom().AlignCenter()
                            .Width(150).LineHorizontal(1).LineColor(Colors.Black);
                        col.Item().AlignCenter().Text("Firma del Cliente").FontSize(9).SemiBold();
                    });
                });

                column.Item().Height(15);

                // Leyenda legal
                column.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(col =>
                {
                    col.Item().Text("COMPROBANTE AUTORIZADO POR LA DGII").SemiBold().FontSize(9).AlignCenter();

                    col.Item().Height(5);

                    col.Item().AlignCenter().Text("Este comprobante fue generado en cumplimiento con el Reglamento 278-19 del Ministerio de Hacienda. Para verificar su autenticidad, visite https://www.dgii.gov.do")
                        .FontSize(8).FontColor(COLOR_SECUNDARIO);
                });
            });
        }

        private byte[]? GenerarQRCode(Factura factura, ConfiguracionEmpresa empresa)
        {
            try
            {
                // Crear el contenido del QR
                var qrContent = new
                {
                    RNC = empresa.RNCEmisor,
                    eNCF = factura.eNCF,
                    Fecha = factura.FechaEmision.ToString("dd/MM/yyyy"),
                    Total = factura.Total,
                    TrackId = factura.Id.ToString()
                };

                var qrText = $"https://ecf.dgii.gov.do/ConsultaTimbreCF/ConsultaTimbreCF.aspx?RncEmisor={empresa.RNCEmisor}&Ecf={factura.eNCF}";

                // Generar QR
                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrData);
                return qrCode.GetGraphic(20);
            }
            catch
            {
                return null;
            }
        }

        private string GenerarEnlaceVerificacion(Factura factura)
        {
            if (!string.IsNullOrEmpty(factura.eNCF) && !string.IsNullOrEmpty(factura.RNCEmisor))
            {
                return $"https://ecf.dgii.gov.do/ConsultaTimbreCF/ConsultaTimbreCF.aspx";
            }
            return "No disponible";
        }

        private (string bg, string text) GetEstadoDGIColors(string? estado)
        {
            return estado?.ToLower() switch
            {
                "aprobado" => (Colors.Green.Lighten3, Colors.Green.Darken2),
                "rechazado" => (Colors.Red.Lighten3, Colors.Red.Darken2),
                "enviado" => (Colors.Blue.Lighten3, Colors.Blue.Darken2),
                "firmado" => (Colors.Purple.Lighten3, Colors.Purple.Darken2),
                "enproceso" => (Colors.Orange.Lighten3, Colors.Orange.Darken2),
                _ => (Colors.Grey.Lighten3, Colors.Grey.Darken2)
            };
        }

        private string GetTipoECFTexto(string? tipoECF)
        {
            return tipoECF switch
            {
                "31" => "Crédito Fiscal",
                "32" => "Consumo",
                "33" => "Nota Débito",
                "34" => "Nota Crédito",
                "41" => "Compras",
                "43" => "Gastos Menores",
                "44" => "Régimen Especial",
                "45" => "Gubernamental",
                "46" => "Exportaciones",
                "47" => "Pagos al Exterior",
                _ => "Factura"
            };
        }

        /// <summary>
        /// Genera un reporte PDF con la lista de facturas
        /// </summary>
        public byte[] GenerarReportePDF(
            List<Factura> facturas,
            ConfiguracionEmpresa? empresa,
            DateTime fechaDesde,
            DateTime fechaHasta,
            string? tipoReporte)
        {
            var tituloReporte = tipoReporte switch
            {
                "anuladas" => "Reporte de Facturas Anuladas",
                "devoluciones" => "Reporte de Devoluciones",
                _ => "Reporte de Ventas"
            };

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Element(header => ComposeReportHeader(header, empresa, tituloReporte, fechaDesde, fechaHasta));
                    page.Content().Element(content => ComposeReportContent(content, facturas));
                    page.Footer().Element(footer => ComposeReportFooter(footer, fechaDesde, fechaHasta));
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeReportHeader(IContainer container, ConfiguracionEmpresa? empresa, string titulo, DateTime desde, DateTime hasta)
        {
            container.Column(column =>
            {
                // Título de la empresa
                column.Item().Text(empresa?.NombreComercial ?? empresa?.RazonSocialEmisor ?? "Empresa")
                    .Bold().FontSize(16).FontColor(COLOR_PRIMARIO);

                if (!string.IsNullOrEmpty(empresa?.RNCEmisor))
                {
                    column.Item().Text($"RNC: {empresa.RNCEmisor}").FontSize(9);
                }

                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(COLOR_SECUNDARIO);

                // Título del reporte y fechas
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text(titulo).Bold().FontSize(14).FontColor(COLOR_PRIMARIO);
                    row.RelativeItem().AlignRight().Text($"Período: {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}")
                        .FontSize(10).FontColor(COLOR_SECUNDARIO);
                });

                column.Item().PaddingTop(5).LineHorizontal(1).LineColor(COLOR_SECUNDARIO);
            });
        }

        private void ComposeReportContent(IContainer container, List<Factura> facturas)
        {
            container.Column(column =>
            {
                column.Item().PaddingTop(10).Table(table =>
                {
                    // Definir columnas
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);   // Fecha
                        columns.RelativeColumn(2);    // Factura
                        columns.RelativeColumn(2.5f); // Cliente
                        columns.RelativeColumn(1.5f); // Tipo
                        columns.RelativeColumn(1.5f); // Estado
                        columns.RelativeColumn(1.5f); // Subtotal
                        columns.RelativeColumn(1.5f); // ITBIS
                        columns.RelativeColumn(1.5f); // Total
                    });

                    // Encabezados
                    table.Header(header =>
                    {
                        header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("FECHA").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("FACTURA").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("CLIENTE").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("TIPO").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("ESTADO").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(COLOR_PRIMARIO).Padding(5).AlignRight().Text("SUBTOTAL").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(COLOR_PRIMARIO).Padding(5).AlignRight().Text("ITBIS").FontColor(Colors.White).Bold().FontSize(9);
                        header.Cell().Background(COLOR_PRIMARIO).Padding(5).AlignRight().Text("TOTAL").FontColor(Colors.White).Bold().FontSize(9);
                    });

                    // Filas de datos
                    int rowNum = 0;
                    foreach (var factura in facturas.OrderBy(f => f.FechaEmision))
                    {
                        var bgColor = rowNum % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;

                        table.Cell().Background(bgColor).Padding(5).Text(factura.FechaEmision.ToString("dd/MM/yyyy")).FontSize(9);
                        table.Cell().Background(bgColor).Padding(5).Text($"{factura.NumeroFactura}").FontSize(9);
                        table.Cell().Background(bgColor).Padding(5).Text(factura.Cliente?.Nombre ?? "Consumidor Final").FontSize(9);
                        table.Cell().Background(bgColor).Padding(5).Text(GetTipoECFTexto(factura.TipoECF)).FontSize(9);

                        // Estado con color
                        var estadoColor = factura.EstadoDGII?.ToLower() switch
                        {
                            "aprobado" or "firmado" => Colors.Green.Darken2,
                            "rechazado" => Colors.Red.Darken2,
                            "enviado" or "enproceso" => Colors.Orange.Darken2,
                            _ => Colors.Grey.Darken2
                        };
                        table.Cell().Background(bgColor).Padding(5).Text(factura.EstadoDGII ?? "Pendiente")
                            .FontColor(estadoColor).Bold().FontSize(9);

                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"$ {factura.Subtotal:N2}").FontSize(9);
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"$ {factura.MontoITBIS:N2}").FontSize(9);
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"$ {factura.Total:N2}")
                            .Bold().FontColor(COLOR_PRIMARIO).FontSize(9);

                        rowNum++;
                    }
                });

                // Totales
                column.Item().PaddingTop(15).AlignRight().Column(totals =>
                {
                    totals.Item().Width(250).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                        });

                        table.Cell().Text("Total Facturas:").AlignRight().Bold();
                        table.Cell().Text(facturas.Count.ToString()).AlignRight();

                        table.Cell().Text("Subtotal:").AlignRight().Bold();
                        table.Cell().Text($"$ {facturas.Sum(f => f.Subtotal):N2}").AlignRight();

                        table.Cell().Text("ITBIS:").AlignRight().Bold();
                        table.Cell().Text($"$ {facturas.Sum(f => f.MontoITBIS):N2}").AlignRight();

                        table.Cell().ColumnSpan(2).PaddingVertical(3).LineHorizontal(1).LineColor(COLOR_SECUNDARIO);

                        table.Cell().Text("TOTAL:").AlignRight().Bold().FontSize(12).FontColor(COLOR_PRIMARIO);
                        table.Cell().Text($"$ {facturas.Sum(f => f.Total):N2}").AlignRight().Bold().FontSize(12).FontColor(COLOR_PRIMARIO);
                    });
                });
            });
        }

        private void ComposeReportFooter(IContainer container, DateTime desde, DateTime hasta)
        {
            container.Column(column =>
            {
                column.Item().PaddingTop(20).LineHorizontal(1).LineColor(COLOR_SECUNDARIO);

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text($"Reporte generado el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(COLOR_SECUNDARIO);
                    row.RelativeItem().AlignRight().Text("Página 1 de 1").FontSize(8).FontColor(COLOR_SECUNDARIO);
                });
            });
        }
    }
}
