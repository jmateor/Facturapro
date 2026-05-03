using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Facturapro.Controllers
{
    [Authorize]
    public class CobrosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CobrosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cobros
        public async Task<IActionResult> Index()
        {
            // Obtener las facturas a crédito que no estén pagadas ni canceladas
            var facturasPendientes = await _context.Facturas
                .Include(f => f.Cliente)
                .Where(f => f.TipoPago == 2 && f.Estado != "Pagada" && f.Estado != "Cancelada")
                .OrderBy(f => f.FechaVencimiento)
                .ToListAsync();

            // Calcular el balance pendiente de cada factura
            var recibos = await _context.RecibosPago.ToListAsync();
            
            var viewModels = facturasPendientes.Select(f => new FacturaPendienteViewModel
            {
                FacturaId = f.Id,
                NumeroFactura = f.NumeroFactura ?? string.Empty,
                ENCF = f.eNCF,
                ClienteNombre = f.Cliente?.Nombre ?? "Consumidor Final",
                FechaEmision = f.FechaEmision,
                FechaVencimiento = f.FechaVencimiento,
                Total = f.Total,
                Abonado = recibos.Where(r => r.FacturaId == f.Id).Sum(r => r.MontoTotal),
                Balance = f.Total - recibos.Where(r => r.FacturaId == f.Id).Sum(r => r.MontoTotal),
                DiasVencidos = (DateTime.Now - f.FechaVencimiento).Days
            }).ToList();

            return View(viewModels);
        }

        // GET: Cobros/Registrar/5
        public async Task<IActionResult> Registrar(int id)
        {
            var factura = await _context.Facturas
                .Include(f => f.Cliente)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            var abonado = await _context.RecibosPago
                .Where(r => r.FacturaId == id)
                .SumAsync(r => r.MontoEfectivo + r.MontoTarjeta + r.MontoTransferencia);

            var balance = factura.Total - abonado;

            ViewBag.Factura = factura;
            ViewBag.Abonado = abonado;
            ViewBag.Balance = balance;

            var recibo = new ReciboPago
            {
                FacturaId = factura.Id,
                ClienteId = factura.ClienteId,
                MontoEfectivo = balance > 0 ? balance : 0 // Sugerir pagar el balance total en efectivo por defecto
            };

            return View(recibo);
        }

        // POST: Cobros/Registrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar([Bind("FacturaId,ClienteId,MontoEfectivo,MontoTarjeta,MontoTransferencia,Referencia,Notas")] ReciboPago recibo)
        {
            var factura = await _context.Facturas.FindAsync(recibo.FacturaId);
            if (factura == null) return NotFound();

            if (recibo.MontoEfectivo < 0 || recibo.MontoTarjeta < 0 || recibo.MontoTransferencia < 0)
            {
                ModelState.AddModelError("", "Los montos no pueden ser negativos.");
            }

            if (recibo.MontoTotal <= 0)
            {
                ModelState.AddModelError("", "Debe ingresar un monto mayor a cero.");
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                recibo.UsuarioId = userId ?? string.Empty;
                recibo.FechaPago = DateTime.Now;

                _context.RecibosPago.Add(recibo);
                
                // Actualizar campos en factura
                factura.MontoEfectivo += recibo.MontoEfectivo;
                factura.MontoTarjeta += recibo.MontoTarjeta;
                factura.MontoTransferencia += recibo.MontoTransferencia;

                var abonadoHistorico = await _context.RecibosPago
                    .Where(r => r.FacturaId == factura.Id)
                    .SumAsync(r => r.MontoEfectivo + r.MontoTarjeta + r.MontoTransferencia);

                var nuevoAbonadoTotal = abonadoHistorico + recibo.MontoTotal;

                if (nuevoAbonadoTotal >= factura.Total)
                {
                    factura.Estado = "Pagada";
                }
                else
                {
                    factura.Estado = "Pago Parcial";
                }

                _context.Facturas.Update(factura);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Pago de {recibo.MontoTotal:C} registrado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            var abonado = await _context.RecibosPago
                .Where(r => r.FacturaId == factura.Id)
                .SumAsync(r => r.MontoEfectivo + r.MontoTarjeta + r.MontoTransferencia);
            var balance = factura.Total - abonado;

            ViewBag.Factura = factura;
            ViewBag.Abonado = abonado;
            ViewBag.Balance = balance;

            return View(recibo);
        }

        // GET: Cobros/Historial/5
        public async Task<IActionResult> Historial(int id)
        {
            var recibos = await _context.RecibosPago
                .Include(r => r.Usuario)
                .Where(r => r.FacturaId == id)
                .OrderByDescending(r => r.FechaPago)
                .ToListAsync();

            ViewBag.FacturaId = id;
            return PartialView("_HistorialPagos", recibos);
        }
    }

    public class FacturaPendienteViewModel
    {
        public int FacturaId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public string? ENCF { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal Total { get; set; }
        public decimal Abonado { get; set; }
        public decimal Balance { get; set; }
        public int DiasVencidos { get; set; }
    }
}
