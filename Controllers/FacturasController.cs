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
        private readonly Services.Notifications.IWhatsAppService _whatsappService;

        public FacturasController(
            ApplicationDbContext context,
            RangoNumeracionService rangoService,
            FacturacionElectronicaService facturacionService,
            IFacturacionElectronicaAPIService apiService,
            IPdfService pdfService,
            ILogger<FacturasController> logger,
            Services.Notifications.IWhatsAppService whatsappService)
        {
            _context = context;
            _rangoService = rangoService;
            _facturacionService = facturacionService;
            _apiService = apiService;
            _pdfService = pdfService;
            _logger = logger;
            _whatsappService = whatsappService;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClienteId,TipoECF,FechaEmision,FechaVencimiento,PorcentajeITBIS,TipoPago,Notas")] Factura factura)
        {
            // Remover validaciones de campos que se generan en el backend
            ModelState.Remove("NumeroFactura");
            ModelState.Remove("Cliente");

            if (ModelState.IsValid)
            {
                // Iniciar transacción para asegurar atomicidad entre NCF e Invoice
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // 1. Obtener siguiente número e-CF (Esto bloquea la fila del rango en el DB si se configura correctamente)
                    var resultadoRango = await _rangoService.ObtenerSiguienteNumeroAsync(factura.TipoECF ?? "31");

                    if (!resultadoRango.Exito)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", resultadoRango.Mensaje);
                        ViewData["Clientes"] = new SelectList(_context.Clientes.Where(c => c.Activo), "Id", "Nombre", factura.ClienteId);
                        return View(factura);
                    }

                    // 2. Asignar datos del comprobante
                    factura.eNCF = resultadoRango.Data?.ToString();
                    factura.NumeroFactura = factura.eNCF ?? $"FAC-{DateTime.Now:yyyy}-{factura.Id:D4}";
                    factura.Estado = "Pendiente";
                    factura.EstadoDGII = "Pendiente";
                    factura.TipoIngresos = "01"; // Ingresos por operaciones

                    // 3. Guardar factura
                    _context.Add(factura);
                    await _context.SaveChangesAsync();

                    // 4. Confirmar transacción
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Factura {factura.eNCF} creada. Ahora puede agregar líneas y firmarla.";
                    return RedirectToAction(nameof(Edit), new { id = factura.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error crítico al crear factura transaccional");
                    ModelState.AddModelError("", "Ocurrió un error inesperado al procesar la numeración fiscal. Intente nuevamente.");
                }
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

                // Retornar el archivo para visualización directa en el navegador (inline)
                return File(pdfBytes, "application/pdf");
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

        // POST: Facturas/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string tipoAnulacion, string? motivo)
        {
            var factura = await _context.Facturas.FindAsync(id);
            if (factura == null) return NotFound();

            if (factura.Estado == "Cancelada")
            {
                TempData["ErrorMessage"] = "Esta factura ya está cancelada.";
                return RedirectToAction(nameof(Index));
            }

            // Según la DGII, si ya fue aprobada y se cancela, debe reportarse en el 608.
            // Si no tiene NCF (ej. borrador), simplemente se marca como cancelada.
            
            factura.Estado = "Cancelada";
            factura.TipoAnulacion = tipoAnulacion;
            factura.MotivoAnulacion = motivo;
            
            _context.Update(factura);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Factura {factura.eNCF} cancelada correctamente y marcada para el reporte 608.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Facturas/EmitirNotaCredito/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmitirNotaCredito(int id)
        {
            var original = await _context.Facturas
                .Include(f => f.Lineas)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (original == null || string.IsNullOrEmpty(original.eNCF))
            {
                TempData["ErrorMessage"] = "Factura original no encontrada o no tiene e-CF.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (original.EstadoDGII != "Aprobado" && original.EstadoDGII != "Firmado" && original.EstadoDGII != "Enviado")
            {
                TempData["ErrorMessage"] = "Solo se pueden emitir notas de crédito para facturas válidas ante la DGII.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Verificar rangos para E34 (Nota de Crédito)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var resultadoRango = await _rangoService.ObtenerSiguienteNumeroAsync("34");
                if (!resultadoRango.Exito)
                {
                    TempData["ErrorMessage"] = "No hay secuencias disponibles para Notas de Crédito (E34). Configúrelas primero en Rangos DGII.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var notaCredito = new Factura
                {
                    ClienteId = original.ClienteId,
                    TipoECF = "34",
                    NCFModificado = original.eNCF,
                    FechaEmision = DateTime.Now,
                    FechaVencimiento = DateTime.Now.AddDays(30),
                    eNCF = resultadoRango.Data?.ToString(),
                    Estado = "Pendiente",
                    EstadoDGII = "Pendiente",
                    TipoIngresos = original.TipoIngresos,
                    TipoPago = original.TipoPago,
                    PorcentajeITBIS = original.PorcentajeITBIS,
                    Subtotal = original.Subtotal, // En E34 los montos positivos actúan como crédito
                    MontoITBIS = original.MontoITBIS,
                    TotalITBIS = original.TotalITBIS,
                    Total = original.Total,
                    TotalDOP = original.TotalDOP,
                    Moneda = original.Moneda,
                    TasaCambio = original.TasaCambio,
                    Notas = $"Nota de Crédito que afecta a la factura {original.eNCF}"
                };

                notaCredito.NumeroFactura = notaCredito.eNCF ?? $"NC-{DateTime.Now:yyyyMMdd}-{original.Id}";

                _context.Facturas.Add(notaCredito);
                await _context.SaveChangesAsync();

                // Copiar líneas (idealmente el usuario debería poder borrar las que no aplican, pero las copiamos como base)
                if (original.Lineas != null)
                {
                    foreach (var linea in original.Lineas)
                    {
                        _context.FacturaLineas.Add(new FacturaLinea
                        {
                            FacturaId = notaCredito.Id,
                            NumeroLinea = linea.NumeroLinea,
                            Descripcion = linea.Descripcion,
                            NombreItem = linea.NombreItem,
                            IndicadorFacturacion = linea.IndicadorFacturacion,
                            IndicadorBienoServicio = linea.IndicadorBienoServicio,
                            Cantidad = linea.Cantidad,
                            PrecioUnitario = linea.PrecioUnitario,
                            Subtotal = linea.Subtotal,
                            MontoITBIS = linea.MontoITBIS
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Nota de Crédito {notaCredito.eNCF} creada en borrador. Revísela y fírmela.";
                return RedirectToAction(nameof(Edit), new { id = notaCredito.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al generar Nota de Crédito para factura {Id}", id);
                TempData["ErrorMessage"] = "Ocurrió un error al generar la Nota de Crédito.";
                return RedirectToAction(nameof(Details), new { id });
            }
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

        [HttpGet]
        public async Task<IActionResult> EnviarWhatsApp(int id)
        {
            var factura = await _context.Facturas
                .Include(f => f.Cliente)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            var link = _whatsappService.GenerateWhatsAppLink(factura);
            if (string.IsNullOrEmpty(link))
            {
                TempData["ErrorMessage"] = "El cliente no tiene un número de teléfono válido.";
                return RedirectToAction("Details", new { id = id });
            }

            return Redirect(link);
        }

        private bool FacturaExists(int id)
        {
            return _context.Facturas.Any(e => e.Id == id);
        }
        [HttpGet]
        public async Task<IActionResult> GetFacturasJson(string? tipoECF, string? estadoDGII, int page = 1, int pageSize = 10)
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

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var facturas = await query
                .OrderByDescending(f => f.FechaEmision)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    id = f.Id,
                    numeroFactura = f.NumeroFactura,
                    eNCF = f.eNCF,
                    tipoECF = f.TipoECF,
                    clienteNombre = f.Cliente != null ? f.Cliente.Nombre : "Consumidor Final",
                    clienteTelefono = f.Cliente != null ? f.Cliente.Telefono : null,
                    fechaEmision = f.FechaEmision.ToString("dd/MM/yyyy"),
                    total = f.Total,
                    estadoDGII = f.EstadoDGII,
                    estado = f.Estado,
                    xmlFirmado = f.XMLFirmado
                })
                .ToListAsync();

            return Json(new
            {
                items = facturas,
                totalItems,
                totalPages,
                currentPage = page,
                pageSize
            });
        }
    }
}
