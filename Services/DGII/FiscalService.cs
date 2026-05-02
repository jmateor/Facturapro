using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Globalization;

namespace Facturapro.Services.DGII
{
    public class FiscalService : IFiscalService
    {
        private readonly ApplicationDbContext _context;

        public FiscalService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> Generar606Async(int mes, int anio)
        {
            var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
            var rncEmisor = config?.RNCEmisor?.Replace("-", "") ?? "";
            var periodo = $"{anio}{mes:D2}";

            var compras = await _context.Compras
                .Include(c => c.Proveedor)
                .Where(c => c.FechaCompra.Month == mes && c.FechaCompra.Year == anio)
                .ToListAsync();

            var sb = new StringBuilder();
            
            // Header: 606|RNC|Periodo|CantidadRegistros
            sb.AppendLine($"606|{rncEmisor}|{periodo}|{compras.Count}");

            foreach (var c in compras)
            {
                var rncCedula = c.Proveedor?.Documento?.Replace("-", "") ?? "";
                var tipoId = rncCedula.Length == 9 ? "1" : (rncCedula.Length == 11 ? "2" : "3");
                var tipoGasto = c.TipoGasto ?? "01";
                var ncf = c.NCF ?? "";
                var ncfModificado = c.NCFModificado ?? "";
                var fechaComprobante = c.FechaCompra.ToString("yyyyMMdd");
                var fechaPago = c.FechaPago?.ToString("yyyyMMdd") ?? "";
                
                // Formatear montos
                string m(decimal val) => val.ToString("F2", CultureInfo.InvariantCulture);

                // Campos 606 (23 campos estándar según Norma 07-2018)
                var fields = new string[23];
                fields[0] = rncCedula;      // 1. RNC o Cédula
                fields[1] = tipoId;         // 2. Tipo ID
                fields[2] = tipoGasto;      // 3. Tipo de Bienes y Servicios Comprados
                fields[3] = ncf;            // 4. NCF
                fields[4] = ncfModificado;  // 5. NCF o Documento Modificado
                fields[5] = fechaComprobante; // 6. Fecha Comprobante (AAAAMM DD)
                fields[6] = fechaPago;      // 7. Fecha Pago (AAAAMM DD)
                fields[7] = m(c.MontoServicios); // 8. Monto Facturado en Servicios
                fields[8] = m(c.MontoBienes);    // 9. Monto Facturado en Bienes
                fields[9] = m(c.MontoServicios + c.MontoBienes); // 10. Total Monto Facturado
                fields[10] = m(c.ITBIS);     // 11. ITBIS Facturado
                fields[11] = m(c.MontoITBISRetenido); // 12. ITBIS Retenido
                fields[12] = m(c.ITBISProporcionalidad); // 13. ITBIS sujeto a Proporcionalidad
                fields[13] = m(c.ITBISCosto); // 14. ITBIS llevado al Costo
                fields[14] = m(c.ITBIS - c.ITBISCosto); // 15. ITBIS por Adelantar
                fields[15] = m(c.ITBISPercibido); // 16. ITBIS percibido en compras
                fields[16] = c.TipoRetencionISR?.ToString() ?? ""; // 17. Tipo de Retención en ISR
                fields[17] = m(c.MontoISRRetenido); // 18. Monto Retención Renta
                fields[18] = m(c.ISRPercibido); // 19. ISR Percibido en compras
                fields[19] = m(c.ISC); // 20. Impuesto Selectivo al Consumo
                fields[20] = m(c.OtrosImpuestos); // 21. Otros Impuestos/Tasas
                fields[21] = m(c.PropinaLegal); // 22. Monto Propina Legal
                fields[22] = c.FormaPago ?? "01"; // 23. Forma de Pago

                sb.AppendLine(string.Join("|", fields));
            }

            return sb.ToString();
        }

        public async Task<string> Generar607Async(int mes, int anio)
        {
            var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
            var rncEmisor = config?.RNCEmisor?.Replace("-", "") ?? "";
            var periodo = $"{anio}{mes:D2}";

            var facturas = await _context.Facturas
                .Include(f => f.Cliente)
                .Where(f => f.FechaEmision.Month == mes && f.FechaEmision.Year == anio)
                .ToListAsync();

            var sb = new StringBuilder();

            // Header: 607|RNC|Periodo|CantidadRegistros
            sb.AppendLine($"607|{rncEmisor}|{periodo}|{facturas.Count}");

            foreach (var f in facturas)
            {
                var rncCedula = f.Cliente?.RNC?.Replace("-", "") ?? "";
                var tipoId = rncCedula.Length == 9 ? "1" : (rncCedula.Length == 11 ? "2" : "3");
                if (string.IsNullOrEmpty(rncCedula)) tipoId = ""; // Para consumidor final sin RNC

                var ncf = f.eNCF ?? "";
                var ncfModificado = f.NCFModificado ?? "";
                var tipoIngreso = f.TipoIngresos ?? "01";
                var fechaComprobante = f.FechaEmision.ToString("yyyyMMdd");
                
                string m(decimal val) => val.ToString("F2", CultureInfo.InvariantCulture);

                // Campos 607 (23 campos)
                var fields = new string[23];
                fields[0] = rncCedula;
                fields[1] = tipoId;
                fields[2] = ncf;
                fields[3] = ncfModificado;
                fields[4] = tipoIngreso;
                fields[5] = fechaComprobante;
                fields[6] = ""; // Fecha Retención
                fields[7] = m(f.Subtotal); // Monto Facturado
                fields[8] = m(f.MontoITBIS); // ITBIS Facturado
                fields[9] = m(f.MontoITBISRetenido);
                fields[10] = m(0); // ITBIS Percibido
                fields[11] = m(f.MontoISRRetenido);
                fields[12] = m(0); // ISR Percibido
                fields[13] = m(0); // ISC
                fields[14] = m(0); // Otros Impuestos
                fields[15] = m(0); // Propina Legal
                
                // Formas de pago según Norma 07-2018
                fields[16] = m(f.MontoEfectivo); // 17. Efectivo
                fields[17] = m(f.MontoTransferencia); // 18. Cheque/Transferencia/Depósito
                fields[18] = m(f.MontoTarjeta); // 19. Tarjeta Crédito/Débito
                
                // Si es a crédito (TipoPago = 2), el balance pendiente va a Venta a Crédito
                decimal pagado = f.MontoEfectivo + f.MontoTarjeta + f.MontoTransferencia;
                decimal credito = f.TipoPago == 2 ? Math.Max(0, f.Total - pagado) : 0;
                fields[19] = m(credito); // 20. Venta a Crédito
                
                fields[20] = m(0); // 21. Bonos o Certificados de Regalo
                fields[21] = m(0); // 22. Permuta
                fields[22] = m(0); // 23. Otras Formas de Ventas

                sb.AppendLine(string.Join("|", fields));
            }

            return sb.ToString();
        }

        public async Task<string> Generar608Async(int mes, int anio)
        {
            var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
            var rncEmisor = config?.RNCEmisor?.Replace("-", "") ?? "";
            var periodo = $"{anio}{mes:D2}";

            var anuladas = await _context.Facturas
                .Where(f => f.FechaEmision.Month == mes && f.FechaEmision.Year == anio && f.Estado == "Cancelada")
                .ToListAsync();

            var sb = new StringBuilder();

            // Header: 608|RNC|Periodo|CantidadRegistros
            sb.AppendLine($"608|{rncEmisor}|{periodo}|{anuladas.Count}");

            foreach (var f in anuladas)
            {
                var ncf = f.eNCF ?? "";
                var fechaComprobante = f.FechaEmision.ToString("yyyyMMdd");
                var tipoAnulacion = f.TipoAnulacion ?? "05"; // Por defecto 05 - Corrección de Información si no se especifica

                // Formato 608: NCF|Fecha|TipoAnulacion
                sb.AppendLine($"{ncf}|{fechaComprobante}|{tipoAnulacion}");
            }

            return sb.ToString();
        }

        public async Task<FiscalSummaryViewModel> GetSummaryAsync(int mes, int anio)
        {
            var compras = await _context.Compras
                .Where(c => c.FechaCompra.Month == mes && c.FechaCompra.Year == anio)
                .ToListAsync();

            var facturas = await _context.Facturas
                .Where(f => f.FechaEmision.Month == mes && f.FechaEmision.Year == anio)
                .ToListAsync();
            
            var anuladas = await _context.Facturas
                .Where(f => f.FechaEmision.Month == mes && f.FechaEmision.Year == anio && f.Estado == "Cancelada")
                .ToListAsync();

            return new FiscalSummaryViewModel
            {
                Mes = mes,
                Anio = anio,
                CantidadCompras = compras.Count,
                TotalCompras = compras.Sum(c => c.Total),
                TotalITBISCompras = compras.Sum(c => c.ITBIS),
                TotalITBISRetenidoCompras = compras.Sum(c => c.MontoITBISRetenido),
                CantidadVentas = facturas.Count,
                TotalVentas = facturas.Sum(f => f.Total),
                TotalITBISVentas = facturas.Sum(f => f.MontoITBIS),
                TotalITBISRetenidoVentas = facturas.Sum(f => f.MontoITBISRetenido),
                CantidadAnuladas = anuladas.Count,
                Ventas = facturas,
                Compras = compras,
                Anuladas = anuladas
            };
        }
    }
}
