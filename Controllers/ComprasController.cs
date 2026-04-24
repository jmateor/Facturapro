using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Controllers
{
    [Authorize]
    public class ComprasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComprasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Compras
        public async Task<IActionResult> Index(string buscar, EstadoCompra? estado)
        {
            var query = _context.Compras
                .Include(c => c.Proveedor)
                .AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(c => c.NumeroFactura.Contains(buscar) ||
                                        c.Proveedor!.Nombre.Contains(buscar));
            }

            if (estado.HasValue)
            {
                query = query.Where(c => c.Estado == estado.Value);
            }

            ViewData["Estados"] = Enum.GetValues(typeof(EstadoCompra))
                .Cast<EstadoCompra>()
                .ToList();

            return View(await query.OrderByDescending(c => c.FechaCompra).ToListAsync());
        }

        // GET: Compras/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var compra = await _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Lineas)
                    .ThenInclude(l => l.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (compra == null)
            {
                return NotFound();
            }

            return View(compra);
        }

        // GET: Compras/Create
        public async Task<IActionResult> Create()
        {
            await CargarProveedoresAsync();
            await CargarProductosAsync();
            return View();
        }

        // POST: Compras/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NumeroFactura,ProveedorId,FechaCompra,Notas,SubTotal,ITBIS,Descuento,Total")] Compra compra, List<CompraLineaViewModel> lineas)
        {
            if (await _context.Compras.AnyAsync(c => c.NumeroFactura == compra.NumeroFactura))
            {
                ModelState.AddModelError("NumeroFactura", "Ya existe una compra con este número de factura");
            }

            if (lineas == null || !lineas.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto a la compra");
            }

            if (ModelState.IsValid)
            {
                compra.Estado = EstadoCompra.Pendiente;
                compra.FechaCreacion = DateTime.Now;

                _context.Add(compra);
                await _context.SaveChangesAsync();

                // Agregar líneas
                foreach (var lineaVm in lineas)
                {
                    var linea = new CompraLinea
                    {
                        CompraId = compra.Id,
                        ProductoId = lineaVm.ProductoId,
                        Descripcion = lineaVm.Descripcion,
                        Cantidad = lineaVm.Cantidad,
                        PrecioUnitario = lineaVm.PrecioUnitario,
                        DescuentoLinea = lineaVm.Descuento,
                        PorcentajeITBIS = lineaVm.ITBIS,
                        TotalLinea = lineaVm.Total
                    };
                    _context.CompraLineas.Add(linea);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Compra registrada correctamente.";
                return RedirectToAction(nameof(Details), new { id = compra.Id });
            }

            await CargarProveedoresAsync(compra.ProveedorId);
            await CargarProductosAsync();
            return View(compra);
        }

        // POST: Compras/Recibir/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recibir(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.Lineas)
                    .ThenInclude(l => l.Producto)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (compra == null)
            {
                return NotFound();
            }

            if (compra.Estado != EstadoCompra.Pendiente)
            {
                TempData["ErrorMessage"] = "Solo se pueden recibir compras pendientes.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Recibir la compra y actualizar inventario
            compra.Estado = EstadoCompra.Recibida;
            compra.FechaRecepcion = DateTime.Now;

            foreach (var linea in compra.Lineas)
            {
                if (linea.Producto != null)
                {
                    var stockAnterior = linea.Producto.Stock;
                    linea.Producto.Stock += linea.Cantidad;

                    // Registrar movimiento de inventario
                    var movimiento = new MovimientoInventario
                    {
                        ProductoId = linea.ProductoId,
                        TipoMovimiento = TipoMovimiento.EntradaCompra,
                        Cantidad = linea.Cantidad,
                        StockAnterior = stockAnterior,
                        StockNuevo = linea.Producto.Stock,
                        Motivo = $"Compra #{compra.NumeroFactura}",
                        CompraId = compra.Id,
                        CostoUnitario = linea.PrecioUnitario,
                        FechaMovimiento = DateTime.Now,
                        UsuarioRegistro = User.Identity?.Name ?? "Sistema"
                    };
                    _context.MovimientosInventario.Add(movimiento);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Compra recibida correctamente. El inventario ha sido actualizado.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Compras/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.Lineas)
                    .ThenInclude(l => l.Producto)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (compra == null)
            {
                return NotFound();
            }

            if (compra.Estado == EstadoCompra.Cancelada)
            {
                TempData["ErrorMessage"] = "La compra ya está cancelada.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Si ya fue recibida, devolver el stock
            if (compra.Estado == EstadoCompra.Recibida)
            {
                foreach (var linea in compra.Lineas)
                {
                    if (linea.Producto != null)
                    {
                        var stockAnterior = linea.Producto.Stock;
                        linea.Producto.Stock -= linea.Cantidad;

                        // Registrar movimiento de inventario (salida)
                        var movimiento = new MovimientoInventario
                        {
                            ProductoId = linea.ProductoId,
                            TipoMovimiento = TipoMovimiento.SalidaAjuste,
                            Cantidad = linea.Cantidad,
                            StockAnterior = stockAnterior,
                            StockNuevo = linea.Producto.Stock,
                            Motivo = $"Cancelación de compra #{compra.NumeroFactura}",
                            CompraId = compra.Id,
                            FechaMovimiento = DateTime.Now,
                            UsuarioRegistro = User.Identity?.Name ?? "Sistema"
                        };
                        _context.MovimientosInventario.Add(movimiento);
                    }
                }
            }

            compra.Estado = EstadoCompra.Cancelada;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Compra cancelada correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task CargarProveedoresAsync(int? proveedorSeleccionado = null)
        {
            var proveedores = await _context.Proveedores
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewData["ProveedorId"] = new SelectList(proveedores, "Id", "Nombre", proveedorSeleccionado);
        }

        private async Task CargarProductosAsync()
        {
            var productos = await _context.Productos
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewData["Productos"] = productos;
        }
    }

    public class CompraLineaViewModel
    {
        public int ProductoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal ITBIS { get; set; }
        public decimal Total { get; set; }
    }
}
