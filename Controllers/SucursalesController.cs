using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Facturapro.Controllers
{
    [Authorize]
    public class SucursalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SucursalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sucursales = await _context.Sucursales
                .Include(s => s.Almacenes)
                .ThenInclude(a => a.Stocks)
                .ToListAsync();
            return View(sucursales);
        }

        public async Task<IActionResult> Gestionar()
        {
            var sucursales = await _context.Sucursales
                .Include(s => s.Almacenes)
                .ToListAsync();
            return View(sucursales);
        }

        [HttpPost]
        public async Task<IActionResult> CrearSucursal(Sucursal sucursal)
        {
            if (ModelState.IsValid)
            {
                _context.Sucursales.Add(sucursal);
                await _context.SaveChangesAsync();
                
                // Crear un almacén por defecto para la nueva sucursal
                var almacen = new Almacen 
                { 
                    Nombre = "Almacén General " + sucursal.Nombre, 
                    SucursalId = sucursal.Id, 
                    EsPrincipalAlmacen = true 
                };
                _context.Almacenes.Add(almacen);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Gestionar));
            }
            return View("Gestionar", await _context.Sucursales.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> TransferirStock(int productoId, int almacenOrigenId, int almacenDestinoId, decimal cantidad)
        {
            var stockOrigen = await _context.StocksAlmacen
                .FirstOrDefaultAsync(s => s.ProductoId == productoId && s.AlmacenId == almacenOrigenId);

            if (stockOrigen == null || stockOrigen.Cantidad < cantidad)
            {
                return Json(new { success = false, message = "Stock insuficiente en el almacén de origen" });
            }

            var stockDestino = await _context.StocksAlmacen
                .FirstOrDefaultAsync(s => s.ProductoId == productoId && s.AlmacenId == almacenDestinoId);

            if (stockDestino == null)
            {
                stockDestino = new StockAlmacen
                {
                    ProductoId = productoId,
                    AlmacenId = almacenDestinoId,
                    Cantidad = 0,
                    UltimaActualizacion = DateTime.Now
                };
                _context.StocksAlmacen.Add(stockDestino);
            }

            // Realizar transferencia
            stockOrigen.Cantidad -= cantidad;
            stockOrigen.UltimaActualizacion = DateTime.Now;
            stockDestino.Cantidad += cantidad;
            stockDestino.UltimaActualizacion = DateTime.Now;

            // Registrar transferencia
            var transferencia = new TransferenciaInventario
            {
                ProductoId = productoId,
                AlmacenOrigenId = almacenOrigenId,
                AlmacenDestinoId = almacenDestinoId,
                Cantidad = cantidad,
                FechaTransferencia = DateTime.Now,
                Estado = "Completado"
            };
            _context.TransferenciasInventario.Add(transferencia);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Transferencia completada con éxito" });
        }

        // GET: Sucursales/Transferencias
        public IActionResult Transferencias()
        {
            return View();
        }

        // API: GET /Sucursales/GetAlmacenes
        public async Task<IActionResult> GetAlmacenes()
        {
            var almacenes = await _context.Almacenes
                .Include(a => a.Sucursal)
                .Where(a => a.Activo)
                .OrderBy(a => a.Nombre)
                .Select(a => new { a.Id, Nombre = a.Nombre, Sucursal = a.Sucursal != null ? a.Sucursal.Nombre : "" })
                .ToListAsync();
            return Json(almacenes);
        }

        // API: GET /Sucursales/GetProductos
        public async Task<IActionResult> GetProductos()
        {
            var productos = await _context.Productos
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new { p.Id, p.Nombre, p.Codigo })
                .ToListAsync();
            return Json(productos);
        }

        // API: GET /Sucursales/GetStockProducto?productoId=X
        public async Task<IActionResult> GetStockProducto(int productoId)
        {
            var stocks = await _context.StocksAlmacen
                .Include(s => s.Almacen)
                .Where(s => s.ProductoId == productoId)
                .Select(s => new
                {
                    AlmacenId = s.AlmacenId,
                    Almacen = s.Almacen != null ? s.Almacen.Nombre : "Desconocido",
                    Cantidad = s.Cantidad
                })
                .ToListAsync();
            return Json(stocks);
        }

        // API: GET /Sucursales/GetHistorialTransferencias
        public async Task<IActionResult> GetHistorialTransferencias()
        {
            var transferencias = await _context.TransferenciasInventario
                .Include(t => t.Producto)
                .Include(t => t.AlmacenOrigen)
                .Include(t => t.AlmacenDestino)
                .OrderByDescending(t => t.FechaTransferencia)
                .Take(50)
                .Select(t => new
                {
                    Producto = t.Producto != null ? t.Producto.Nombre : "—",
                    Origen   = t.AlmacenOrigen != null ? t.AlmacenOrigen.Nombre : "—",
                    Destino  = t.AlmacenDestino != null ? t.AlmacenDestino.Nombre : "—",
                    t.Cantidad,
                    t.Estado,
                    Fecha = t.FechaTransferencia.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();
            return Json(transferencias);
        }
    }
}
