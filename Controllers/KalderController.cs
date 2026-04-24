using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Controllers
{
    /// <summary>
    /// Módulo Kalder - Sistema de Gestión de Almacén e Inventario
    /// </summary>
    [Authorize]
    public class KalderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KalderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Kalder
        public async Task<IActionResult> Index()
        {
            // Estadísticas para el dashboard
            var totalProductos = await _context.Productos.CountAsync();
            var productosBajoStock = await _context.Productos
                .Where(p => p.Stock <= 5 && p.Activo)
                .Include(p => p.Categoria)
                .ToListAsync();
            var stockTotal = await _context.Productos.SumAsync(p => (int?)p.Stock) ?? 0;
            var movimientosHoy = await _context.MovimientosInventario
                .CountAsync(m => m.FechaMovimiento.Date == DateTime.Today);

            // Valor del inventario
            var valorInventario = await _context.Productos
                .SumAsync(p => (decimal?)(p.Stock * p.Precio)) ?? 0;

            // Movimientos recientes
            var movimientosRecientes = await _context.MovimientosInventario
                .Include(m => m.Producto)
                .OrderByDescending(m => m.FechaMovimiento)
                .Take(10)
                .ToListAsync();

            ViewData["TotalProductos"] = totalProductos;
            ViewData["ProductosBajoStock"] = productosBajoStock;
            ViewData["StockTotal"] = stockTotal;
            ViewData["MovimientosHoy"] = movimientosHoy;
            ViewData["ValorInventario"] = valorInventario;
            ViewData["MovimientosRecientes"] = movimientosRecientes;

            return View();
        }

        // GET: Kalder/Inventario
        public async Task<IActionResult> Inventario(int? categoriaId, string buscar)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .AsQueryable();

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId);
            }

            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(p => p.Nombre.Contains(buscar) ||
                                       p.Codigo.Contains(buscar) ||
                                       (p.CodigoBarras != null && p.CodigoBarras.Contains(buscar)));
            }

            ViewData["Categorias"] = await _context.Categorias
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var productos = await query
                .OrderBy(p => p.Stock <= 5 ? 0 : 1)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return View(productos);
        }

        // GET: Kalder/Entrada/5
        public async Task<IActionResult> Entrada(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // POST: Kalder/Entrada/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entrada(int id, int cantidad, string? motivo, string? proveedor, string? numeroFacturaProveedor)
        {
            if (cantidad <= 0)
            {
                TempData["ErrorMessage"] = "La cantidad debe ser mayor a cero";
                return RedirectToAction(nameof(Entrada), new { id });
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            var stockAnterior = producto.Stock;
            producto.Stock += cantidad;

            // Registrar movimiento
            var motivoCompleto = !string.IsNullOrEmpty(motivo) ? motivo : "Entrada de mercancía";
            if (!string.IsNullOrEmpty(proveedor))
            {
                motivoCompleto += $" | Proveedor: {proveedor}";
            }
            if (!string.IsNullOrEmpty(numeroFacturaProveedor))
            {
                motivoCompleto += $" | Factura: {numeroFacturaProveedor}";
            }

            _context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = id,
                TipoMovimiento = TipoMovimiento.EntradaCompra,
                Cantidad = cantidad,
                StockAnterior = stockAnterior,
                StockNuevo = producto.Stock,
                Motivo = motivoCompleto,
                FechaMovimiento = DateTime.Now,
                UsuarioRegistro = User.Identity?.Name ?? "Kalder"
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Entrada registrada: {cantidad} unidades de {producto.Nombre}";
            return RedirectToAction(nameof(Inventario));
        }

        // GET: Kalder/Salida/5
        public async Task<IActionResult> Salida(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // POST: Kalder/Salida/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salida(int id, int cantidad, string? motivo)
        {
            if (cantidad <= 0)
            {
                TempData["ErrorMessage"] = "La cantidad debe ser mayor a cero";
                return RedirectToAction(nameof(Salida), new { id });
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            if (producto.Stock < cantidad)
            {
                TempData["ErrorMessage"] = $"Stock insuficiente. Disponible: {producto.Stock}";
                return RedirectToAction(nameof(Salida), new { id });
            }

            var stockAnterior = producto.Stock;
            producto.Stock -= cantidad;

            // Registrar movimiento
            _context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = id,
                TipoMovimiento = TipoMovimiento.SalidaAjuste,
                Cantidad = cantidad,
                StockAnterior = stockAnterior,
                StockNuevo = producto.Stock,
                Motivo = !string.IsNullOrEmpty(motivo) ? motivo : "Salida de mercancía",
                FechaMovimiento = DateTime.Now,
                UsuarioRegistro = User.Identity?.Name ?? "Kalder"
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Salida registrada: {cantidad} unidades de {producto.Nombre}";
            return RedirectToAction(nameof(Inventario));
        }

        // GET: Kalder/Ajuste/5
        public async Task<IActionResult> Ajuste(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // POST: Kalder/Ajuste/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ajuste(int id, int nuevoStock, string? motivo)
        {
            if (nuevoStock < 0)
            {
                TempData["ErrorMessage"] = "El stock no puede ser negativo";
                return RedirectToAction(nameof(Ajuste), new { id });
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            var stockAnterior = producto.Stock;
            producto.Stock = nuevoStock;

            // Determinar tipo de movimiento
            var tipoMovimiento = nuevoStock > stockAnterior
                ? TipoMovimiento.EntradaAjuste
                : TipoMovimiento.SalidaAjuste;

            // Registrar movimiento
            _context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = id,
                TipoMovimiento = tipoMovimiento,
                Cantidad = Math.Abs(nuevoStock - stockAnterior),
                StockAnterior = stockAnterior,
                StockNuevo = producto.Stock,
                Motivo = !string.IsNullOrEmpty(motivo) ? motivo : "Ajuste de inventario",
                FechaMovimiento = DateTime.Now,
                UsuarioRegistro = User.Identity?.Name ?? "Kalder"
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Ajuste realizado: Stock de {producto.Nombre} ajustado de {stockAnterior} a {nuevoStock}";
            return RedirectToAction(nameof(Inventario));
        }

        // GET: Kalder/Historial
        public async Task<IActionResult> Historial(int? productoId, DateTime? fechaDesde, DateTime? fechaHasta, string? tipoMovimiento)
        {
            var query = _context.MovimientosInventario
                .Include(m => m.Producto)
                .AsQueryable();

            if (productoId.HasValue)
            {
                query = query.Where(m => m.ProductoId == productoId);
            }

            if (fechaDesde.HasValue)
            {
                query = query.Where(m => m.FechaMovimiento >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                query = query.Where(m => m.FechaMovimiento <= fechaHasta.Value.AddDays(1));
            }

            if (!string.IsNullOrEmpty(tipoMovimiento) && Enum.TryParse<TipoMovimiento>(tipoMovimiento, out var tipo))
            {
                query = query.Where(m => m.TipoMovimiento == tipo);
            }

            var movimientos = await query
                .OrderByDescending(m => m.FechaMovimiento)
                .Take(100)
                .ToListAsync();

            ViewData["Productos"] = await _context.Productos
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewData["TiposMovimiento"] = Enum.GetValues<TipoMovimiento>();

            return View(movimientos);
        }

        // GET: Kalder/ReporteStock
        public async Task<IActionResult> ReporteStock(string? estado)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .AsQueryable();

            if (estado == "bajo")
            {
                query = query.Where(p => p.Stock <= 5);
            }
            else if (estado == "agotado")
            {
                query = query.Where(p => p.Stock == 0);
            }
            else if (estado == "sobre")
            {
                query = query.Where(p => p.Stock > 100);
            }

            var productos = await query.OrderBy(p => p.Nombre).ToListAsync();
            ViewData["Estado"] = estado;

            return View(productos);
        }
    }
}
