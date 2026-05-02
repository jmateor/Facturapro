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

        public async Task<IActionResult> Index()
        {
            // Obtener el almacén principal
            var almacen = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);

            var totalProductos = await _context.Productos.CountAsync(p => p.Activo);
            var movimientosHoy = await _context.MovimientosInventario
                .CountAsync(m => m.FechaMovimiento.Date == DateTime.Today);

            List<Producto> productosBajoStock;
            int stockTotal;
            decimal valorInventario;

            if (almacen != null)
            {
                // Usar datos del almacén
                var stocksAlmacen = await _context.StocksAlmacen
                    .Include(s => s.Producto).ThenInclude(p => p.Categoria)
                    .Where(s => s.AlmacenId == almacen.Id && s.Producto.Activo)
                    .ToListAsync();

                productosBajoStock = stocksAlmacen
                    .Where(s => s.Cantidad <= 5)
                    .Select(s => s.Producto)
                    .ToList();

                stockTotal = (int)stocksAlmacen.Sum(s => s.Cantidad);
                valorInventario = stocksAlmacen.Sum(s => s.Cantidad * s.Producto.Precio);
            }
            else
            {
                // Fallback a stock global
                productosBajoStock = await _context.Productos
                    .Where(p => p.Stock <= 5 && p.Activo)
                    .Include(p => p.Categoria)
                    .ToListAsync();
                stockTotal = await _context.Productos.SumAsync(p => (int?)p.Stock) ?? 0;
                valorInventario = await _context.Productos
                    .SumAsync(p => (decimal?)(p.Stock * p.Precio)) ?? 0;
            }

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
            ViewData["AlmacenNombre"] = almacen?.Nombre ?? "Global";

            return View();
        }

        public async Task<IActionResult> Inventario(int? categoriaId, string buscar)
        {
            var almacen = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);

            List<Producto> productos;

            if (almacen != null)
            {
                var stockQuery = _context.StocksAlmacen
                    .Include(s => s.Producto).ThenInclude(p => p.Categoria)
                    .Where(s => s.AlmacenId == almacen.Id && s.Producto.Activo);

                if (categoriaId.HasValue)
                    stockQuery = stockQuery.Where(s => s.Producto.CategoriaId == categoriaId);

                if (!string.IsNullOrEmpty(buscar))
                    stockQuery = stockQuery.Where(s =>
                        s.Producto.Nombre.Contains(buscar) ||
                        s.Producto.Codigo.Contains(buscar) ||
                        (s.Producto.CodigoBarras != null && s.Producto.CodigoBarras.Contains(buscar)));

                var stocks = await stockQuery
                    .OrderBy(s => s.Cantidad <= 5 ? 0 : 1)
                    .ThenBy(s => s.Producto.Nombre)
                    .ToListAsync();

                // Proyectar el stock del almacén al campo Stock del producto para compatibilidad con la vista
                productos = stocks.Select(s => {
                    s.Producto.Stock = (int)s.Cantidad;
                    return s.Producto;
                }).ToList();
            }
            else
            {
                var query = _context.Productos.Include(p => p.Categoria).AsQueryable();
                if (categoriaId.HasValue) query = query.Where(p => p.CategoriaId == categoriaId);
                if (!string.IsNullOrEmpty(buscar))
                    query = query.Where(p => p.Nombre.Contains(buscar) || p.Codigo.Contains(buscar));
                productos = await query.OrderBy(p => p.Stock <= 5 ? 0 : 1).ThenBy(p => p.Nombre).ToListAsync();
            }

            ViewData["Categorias"] = await _context.Categorias
                .Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync();
            ViewData["AlmacenNombre"] = almacen?.Nombre ?? "Global";

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

            // Sincronizar StockAlmacen
            var almacenEntrada = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);
            if (almacenEntrada != null)
            {
                var stockAlmacen = await _context.StocksAlmacen
                    .FirstOrDefaultAsync(s => s.ProductoId == id && s.AlmacenId == almacenEntrada.Id);
                if (stockAlmacen != null)
                {
                    stockAlmacen.Cantidad += cantidad;
                    stockAlmacen.UltimaActualizacion = DateTime.Now;
                }
                else
                {
                    _context.StocksAlmacen.Add(new StockAlmacen
                    {
                        ProductoId = id,
                        AlmacenId = almacenEntrada.Id,
                        Cantidad = producto.Stock,
                        UltimaActualizacion = DateTime.Now
                    });
                }
            }

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

            // Sincronizar StockAlmacen
            var almacenSalida = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);
            if (almacenSalida != null)
            {
                var stockAlmacen = await _context.StocksAlmacen
                    .FirstOrDefaultAsync(s => s.ProductoId == id && s.AlmacenId == almacenSalida.Id);
                if (stockAlmacen != null)
                {
                    stockAlmacen.Cantidad = Math.Max(0, stockAlmacen.Cantidad - cantidad);
                    stockAlmacen.UltimaActualizacion = DateTime.Now;
                }
            }

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

            // Sincronizar StockAlmacen
            var almacenAjuste = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen && a.Activo);
            if (almacenAjuste != null)
            {
                var stockAlmacen = await _context.StocksAlmacen
                    .FirstOrDefaultAsync(s => s.ProductoId == id && s.AlmacenId == almacenAjuste.Id);
                if (stockAlmacen != null)
                {
                    stockAlmacen.Cantidad = nuevoStock;
                    stockAlmacen.UltimaActualizacion = DateTime.Now;
                }
                else
                {
                    _context.StocksAlmacen.Add(new StockAlmacen
                    {
                        ProductoId = id,
                        AlmacenId = almacenAjuste.Id,
                        Cantidad = nuevoStock,
                        UltimaActualizacion = DateTime.Now
                    });
                }
            }

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

        // GET: Kalder/Kardex/5
        public async Task<IActionResult> Kardex(int id, DateTime? desde, DateTime? hasta)
        {
            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null) return NotFound();

            var query = _context.MovimientosInventario
                .Include(m => m.Factura)
                .Include(m => m.Compra)
                .Where(m => m.ProductoId == id);

            if (desde.HasValue) query = query.Where(m => m.FechaMovimiento >= desde.Value);
            if (hasta.HasValue) query = query.Where(m => m.FechaMovimiento <= hasta.Value.AddDays(1));

            var movimientos = await query
                .OrderBy(m => m.FechaMovimiento)
                .ToListAsync();

            ViewData["Producto"] = producto;
            ViewData["Desde"] = desde;
            ViewData["Hasta"] = hasta;

            return View(movimientos);
        }
    }
}
