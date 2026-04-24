using Facturapro.Data;
using Facturapro.Models.Entities;
using Facturapro.Services.DGII;
using Facturapro.Services.PDF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace Facturapro.Controllers
{
    [Authorize]
    public class FacturasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RangoNumeracionService _rangoService;
        private readonly FacturacionElectronicaService _facturacionService;
        private readonly IFacturacionElectronicaAPIService _apiService;
        private readonly IPdfService _pdfService;
        private readonly ILogger<FacturasController> _logger;

        public FacturasController(
            ApplicationDbContext context,
            RangoNumeracionService rangoService,
            FacturacionElectronicaService facturacionService,
            IFacturacionElectronicaAPIService apiService,
            IPdfService pdfService,
            ILogger<FacturasController> logger)
        {
            _context = context;
            _rangoService = rangoService;
            _facturacionService = facturacionService;
            _apiService = apiService;
            _pdfService = pdfService;
            _logger = logger;
        }

        // GET: Facturas
        public async Task<IActionResult> Index(string? tipoECF, string? estadoDGII)
        {
            var query = _context.Facturas
                .Include(f => f.Cliente)
                .AsQueryable();

            if (!string.IsNullOrEmpty(tipoECF))
            {
                query = query.Where(f => f.TipoECF == tipoECF);
            }

            if (!string.IsNullOrEmpty(estadoDGII))
            {
                query = query.Where(f => f.EstadoDGII == estadoDGII);
            }

            var facturas = await query.OrderByDescending(f => f.FechaEmision).ToListAsync();
            return View(facturas);
        }

        // GET: Facturas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Lineas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (factura == null) return NotFound();

            return View(factura);
        }

        // GET: Facturas/Create
        public async Task<IActionResult> Create()
        {
            // Verificar si hay rangos disponibles
            var rangos31 = await _rangoService.ExisteRangoDisponibleAsync("31");
            var rangos32 = await _rangoService.ExisteRangoDisponibleAsync("32");

            if (!rangos31 && !rangos32)
            {
                TempData["ErrorMessage"] = "No hay rangos de numeración disponibles. Configure rangos autorizados por la DGII primero.";
                return RedirectToAction("Index", "Rangos");
            }

            ViewData["Clientes"] = new SelectList(_context.Clientes.Where(c => c.Activo), "Id", "Nombre");

            var factura = new Factura
            {
                FechaEmision = DateTime.Now,
                FechaVencimiento = DateTime.Now.AddDays(30),
                PorcentajeITBIS = 18,
                TipoECF = "31" // Por defecto Factura de Crédito Fiscal
            };

            return View(factura);
        }

        // POST: Facturas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClienteId,TipoECF,FechaEmision,FechaVencimiento,PorcentajeITBIS,TipoPago,Notas")] Factura factura)
        {
            if (ModelState.IsValid)
            {
                // Obtener siguiente número e-CF
                var resultadoRango = await _rangoService.ObtenerSiguienteNumeroAsync(factura.TipoECF ?? "31");

                if (!resultadoRango.Exito)
                {
                    ModelState.AddModelError("", resultadoRango.Mensaje);
                    ViewData["Clientes"] = new SelectList(_context.Clientes.Where(c => c.Activo), "Id", "Nombre", factura.ClienteId);
                    return View(factura);
                }

                factura.eNCF = resultadoRango.Data?.ToString();
                factura.NumeroFactura = factura.eNCF ?? $"FAC-{DateTime.Now:yyyy}-{factura.Id:D4}";
                factura.Estado = "Pendiente";
                factura.EstadoDGII = "Pendiente";
                factura.TipoIngresos = "01"; // Ingresos por operaciones

                _context.Add(factura);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Factura {factura.eNCF} creada. Ahora puede agregar líneas y firmarla.";
                return RedirectToAction(nameof(Edit), new { id = factura.Id });
            }

            ViewData["Clientes"] = new SelectList(_context.Clientes.Where(c => c.Activo), "Id", "Nombre", factura.ClienteId);
            return View(factura);
        }

        // GET: Facturas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Lineas)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            ViewData["Clientes"] = new SelectList(_context.Clientes.Where(c => c.Activo), "Id", "Nombre", factura.ClienteId);
            return View(factura);
        }

        // POST: Facturas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClienteId,TipoECF,FechaEmision,FechaVencimiento,Estado,TipoPago,PorcentajeITBIS,Notas")] Factura factura)
        {
            if (id != factura.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(factura);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Factura actualizada correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FacturaExists(factura.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["Clientes"] = new SelectList(_context.Clientes.Where(c => c.Activo), "Id", "Nombre", factura.ClienteId);
            return View(factura);
        }

        // GET: Facturas/Firmar/5
        public async Task<IActionResult> Firmar(int id)
        {
            var factura = await _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Lineas)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            if (factura.EstadoDGII != "Pendiente" && factura.EstadoDGII != "Rechazado")
            {
                TempData["ErrorMessage"] = "Esta factura ya ha sido procesada.";
                return RedirectToAction(nameof(Index));
            }

            if (factura.Lineas == null || !factura.Lineas.Any())
            {
                TempData["ErrorMessage"] = "La factura debe tener al menos una línea para firmar.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            // Verificar certificado configurado
            var certValido = await _apiService.ValidarCertificadoAsync();
            if (!certValido)
            {
                TempData["ErrorMessage"] = "No hay certificado digital válido configurado. Configure el certificado en Configuración > DGII.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Generar XML y firmar
                var xml = _facturacionService.GenerarXMLECF(factura, factura.Cliente!);

                var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
                if (config != null && !string.IsNullOrEmpty(config.RutaCertificado))
                {
                    var xmlFirmado = await _facturacionService.FirmarXMLAsync(xml, config.RutaCertificado, config.PasswordCertificado ?? "");

                    factura.XMLFirmado = xmlFirmado;
                    factura.FechaHoraFirma = DateTime.Now;
                    factura.EstadoDGII = "Firmado";

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Factura {factura.eNCF} firmada digitalmente. Lista para enviar a la DGII.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error: No se encontró certificado configurado.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al firmar factura {FacturaId}", id);
                TempData["ErrorMessage"] = $"Error al firmar: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Facturas/EnviarDGII/5
        public async Task<IActionResult> EnviarDGII(int id)
        {
            var factura = await _context.Facturas
                .Include(f => f.Cliente)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            if (factura.EstadoDGII != "Firmado" && factura.EstadoDGII != "Rechazado")
            {
                TempData["ErrorMessage"] = "La factura debe estar firmada o en estado Rechazado para reenviar.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar servicios DGII disponibles
            var serviciosDisponibles = await _apiService.VerificarServiciosDGIIAsync();
            if (!serviciosDisponibles)
            {
                TempData["ErrorMessage"] = "Los servicios de la DGII no están disponibles en este momento. Intente más tarde.";
                return RedirectToAction(nameof(Index));
            }

            var resultado = await _apiService.EnviarFacturaADGIIAsync(id);

            if (resultado.Exitoso)
            {
                TempData["SuccessMessage"] = $"Factura {resultado.ENCF} enviada a la DGII exitosamente. TrackId: {resultado.TrackId}";
            }
            else
            {
                TempData["ErrorMessage"] = $"Error al enviar: {resultado.Mensaje}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Facturas/ConsultarEstado/5
        public async Task<IActionResult> ConsultarEstado(int id)
        {
            await _apiService.ConsultarEstadoPendientesAsync();

            var factura = await _context.Facturas.FindAsync(id);
            if (factura != null)
            {
                TempData["SuccessMessage"] = $"Estado actualizado: {factura.EstadoDGII}. {factura.MensajeDGII}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Facturas/ConsultarEstadoPendientes
        public async Task<IActionResult> ConsultarEstadoPendientes()
        {
            await _apiService.ConsultarEstadoPendientesAsync();

            TempData["SuccessMessage"] = "Estados de facturas pendientes actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Facturas/DescargarXML/5
        public async Task<IActionResult> DescargarXML(int id)
        {
            var factura = await _context.Facturas.FindAsync(id);
            if (factura == null || string.IsNullOrEmpty(factura.XMLFirmado))
            {
                return NotFound("XML no disponible");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(factura.XMLFirmado);
            return File(bytes, "application/xml", $"{factura.eNCF}.xml");
        }

        // GET: Facturas/DescargarPDF/5
        public async Task<IActionResult> DescargarPDF(int id)
        {
            var factura = await _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Lineas)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            // Obtener configuración de la empresa
            var empresa = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
            if (empresa == null)
            {
                TempData["ErrorMessage"] = "No se ha configurado la empresa emisora. Configure la empresa en el módulo de Configuración.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Generar PDF con la representación impresa
                var pdfBytes = _pdfService.GenerarFacturaPDF(factura, empresa);

                // Nombre del archivo
                var fileName = $"{factura.eNCF ?? factura.NumeroFactura}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF para factura {FacturaId}", id);
                TempData["ErrorMessage"] = "Error al generar el PDF. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Facturas/AgregarLinea
        [HttpPost]
        public async Task<IActionResult> AgregarLinea(int facturaId, string descripcion, string? nombreItem, decimal cantidad, decimal precioUnitario, int indicadorFacturacion)
        {
            var factura = await _context.Facturas
                .Include(f => f.Lineas)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null) return NotFound();

            // Calcular ITBIS por línea
            var itbisRate = indicadorFacturacion == 1 ? 0.18m : 0m;
            var subtotal = cantidad * precioUnitario;
            var montoITBIS = subtotal * itbisRate;

            var linea = new FacturaLinea
            {
                FacturaId = facturaId,
                NumeroLinea = (factura.Lineas?.Count ?? 0) + 1,
                Descripcion = descripcion,
                NombreItem = nombreItem ?? descripcion,
                IndicadorFacturacion = indicadorFacturacion,
                IndicadorBienoServicio = 2, // Servicio por defecto
                Cantidad = cantidad,
                PrecioUnitario = precioUnitario,
                Subtotal = subtotal,
                MontoITBIS = montoITBIS
            };

            _context.FacturaLineas.Add(linea);

            // Recalcular totales
            await RecalcularTotales(facturaId);

            return RedirectToAction(nameof(Edit), new { id = facturaId });
        }

        // POST: Facturas/EliminarLinea
        [HttpPost]
        public async Task<IActionResult> EliminarLinea(int id)
        {
            var linea = await _context.FacturaLineas.FindAsync(id);
            if (linea == null) return NotFound();

            var facturaId = linea.FacturaId;
            _context.FacturaLineas.Remove(linea);

            await RecalcularTotales(facturaId);

            return RedirectToAction(nameof(Edit), new { id = facturaId });
        }

        // GET: Facturas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturas
                .Include(f => f.Cliente)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (factura == null) return NotFound();

            return View(factura);
        }

        // POST: Facturas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var factura = await _context.Facturas.FindAsync(id);
            if (factura != null)
            {
                // Solo permitir eliminar si no está firmada
                if (factura.EstadoDGII == "Aprobado")
                {
                    TempData["ErrorMessage"] = "No se puede eliminar una factura aprobada por la DGII.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Facturas.Remove(factura);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Factura eliminada correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task RecalcularTotales(int facturaId)
        {
            var factura = await _context.Facturas
                .Include(f => f.Lineas)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura != null)
            {
                var subtotal = factura.Lineas?.Sum(l => l.Subtotal) ?? 0;
                var totalITBIS = factura.Lineas?.Sum(l => l.MontoITBIS) ?? 0;

                factura.Subtotal = subtotal;
                factura.TotalITBIS = totalITBIS;
                factura.MontoITBIS = totalITBIS;
                factura.Total = subtotal + totalITBIS;

                await _context.SaveChangesAsync();
            }
        }

        private bool FacturaExists(int id)
        {
            return _context.Facturas.Any(e => e.Id == id);
        }
    }
}
