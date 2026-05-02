using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Facturapro.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            await FixInventoryInternal();

            var hoy = DateTime.Now;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            // Estadísticas del dashboard
            var dashboardData = new HomeDashboardViewModel
            {
                // Ingresos del mes (facturas del mes actual) - Usando TotalDOP para precisión bimoneda
                IngresosMes = await _context.Facturas
                    .Where(f => f.FechaEmision >= inicioMes && f.Estado != "Cancelada")
                    .SumAsync(f => (decimal?)f.TotalDOP) ?? 0,

                // Total de facturas del mes
                FacturasMes = await _context.Facturas
                    .CountAsync(f => f.FechaEmision >= inicioMes),

                // Facturas pendientes
                FacturasPendientes = await _context.Facturas
                    .CountAsync(f => f.Estado == "Pendiente" || f.Estado == "Emitida"),

                // Clientes activos (que tienen facturas)
                ClientesActivos = await _context.Clientes
                    .CountAsync(c => c.Facturas.Any()),

                // Total de clientes
                TotalClientes = await _context.Clientes.CountAsync(),

                // Total de productos
                TotalProductos = await _context.Productos.CountAsync(p => p.Activo),

                // Productos con stock bajo
                StockBajo = await _context.Productos
                    .CountAsync(p => p.Activo && p.Stock <= 10),

                // Facturas recientes
                FacturasRecientes = await _context.Facturas
                    .Include(f => f.Cliente)
                    .Where(f => f.Estado != "Cancelada")
                    .OrderByDescending(f => f.FechaEmision)
                    .Take(10)
                    .ToListAsync(),

                // Alertas de NCF
                AlertasNCF = new List<NCFAlert>()
            };

            // Consultar rangos de NCF activos
            var rangos = await _context.RangoNumeraciones
                .Where(r => r.Estado == EstadoRango.Activo)
                .ToListAsync();

            foreach (var rango in rangos)
            {
                var diasParaVencer = (rango.FechaVencimiento - hoy).TotalDays;
                
                // Alerta por fecha
                if (diasParaVencer <= 30)
                {
                    dashboardData.AlertasNCF.Add(new NCFAlert
                    {
                        Tipo = rango.TipoECF,
                        Mensaje = diasParaVencer <= 7 ? $"NCF tipo E{rango.TipoECF} vence en {Math.Max(0, (int)diasParaVencer)} días." : $"NCF tipo E{rango.TipoECF} próximo a vencer.",
                        Nivel = diasParaVencer <= 7 ? "danger" : "warning",
                        FechaVencimiento = rango.FechaVencimiento
                    });
                }

                // Alerta por cantidad
                if (rango.CantidadDisponible <= 200)
                {
                    dashboardData.AlertasNCF.Add(new NCFAlert
                    {
                        Tipo = rango.TipoECF,
                        Mensaje = $"Quedan pocos NCF tipo E{rango.TipoECF} ({rango.CantidadDisponible} disponibles).",
                        Nivel = rango.CantidadDisponible <= 50 ? "danger" : "warning",
                        CantidadRestante = rango.CantidadDisponible
                    });
                }
            }

            return View(dashboardData);
        }

        private async Task FixInventoryInternal()
        {
            try {
                // Asegurar columna de CostoPromedio
                await _context.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Productos' AND COLUMN_NAME = 'CostoPromedio') ALTER TABLE Productos ADD CostoPromedio decimal(18,2) NOT NULL DEFAULT 0");
                
                // 1. Asegurar Sucursal Principal
                var sucursal = await _context.Sucursales.FirstOrDefaultAsync(s => s.EsPrincipal);
                if (sucursal == null)
                {
                    sucursal = new Sucursal { Nombre = "Sucursal Principal", EsPrincipal = true, Activo = true };
                    _context.Sucursales.Add(sucursal);
                    await _context.SaveChangesAsync();
                }

                // 2. Asegurar Almacén Principal
                var almacen = await _context.Almacenes.FirstOrDefaultAsync(a => a.EsPrincipalAlmacen);
                if (almacen == null)
                {
                    almacen = new Almacen { Nombre = "Almacén Central", SucursalId = sucursal.Id, EsPrincipalAlmacen = true, Activo = true };
                    _context.Almacenes.Add(almacen);
                    await _context.SaveChangesAsync();
                }

                // 3. ASEGURAR COLUMNAS DE PERMISOS EN AspNetUsers
                await _context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'PuedeFacturar')
                    ALTER TABLE AspNetUsers ADD PuedeFacturar bit NOT NULL DEFAULT 1;
                    
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'PuedeVerReportes')
                    ALTER TABLE AspNetUsers ADD PuedeVerReportes bit NOT NULL DEFAULT 0;
                    
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'PuedeGestionarInventario')
                    ALTER TABLE AspNetUsers ADD PuedeGestionarInventario bit NOT NULL DEFAULT 0;
                    
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'PuedeConfigurarSistema')
                    ALTER TABLE AspNetUsers ADD PuedeConfigurarSistema bit NOT NULL DEFAULT 0;
                    
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'PuedeAnularFacturas')
                    ALTER TABLE AspNetUsers ADD PuedeAnularFacturas bit NOT NULL DEFAULT 0;
                    
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'PuedeVerCostos')
                    ALTER TABLE AspNetUsers ADD PuedeVerCostos bit NOT NULL DEFAULT 0;
                    
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'PuedeGestionarClientes')
                    ALTER TABLE AspNetUsers ADD PuedeGestionarClientes bit NOT NULL DEFAULT 1;

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'PuedeGestionarUsuarios')
                    ALTER TABLE AspNetUsers ADD PuedeGestionarUsuarios bit NOT NULL DEFAULT 0;
                ");

                // 3. Migrar stock actual si no hay registros en StockAlmacen
                if (!await _context.StocksAlmacen.AnyAsync())
                {
                    var productosStock = await _context.Productos.Where(p => p.Stock > 0).ToListAsync();
                    foreach (var p in productosStock)
                    {
                        _context.StocksAlmacen.Add(new StockAlmacen 
                        { 
                            ProductoId = p.Id, 
                            AlmacenId = almacen.Id, 
                            Cantidad = p.Stock,
                            UltimaActualizacion = DateTime.Now
                        });
                    }
                    await _context.SaveChangesAsync();
                }
            } catch (Exception ex) { 
                _logger.LogError(ex, "Error en migración inicial de sucursales");
            }
        }

        public async Task<IActionResult> FixInventory()
        {
            try {
                await FixInventoryInternal();
                return Content("OK: Inventario inicializado correctamente");
            } catch (Exception ex) {
                return Content("ERROR: " + ex.Message + " | " + ex.InnerException?.Message);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var hoy = DateTime.Now;
                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
                var hace7Dias = hoy.Date.AddDays(-6);

                // Stats básicas con manejo de nulos individual
                decimal ingresosMes = 0;
                int facturasMes = 0;
                int facturasPendientes = 0;
                int totalClientes = 0;
                int stockBajo = 0;

                try {
                    ingresosMes = await _context.Facturas
                        .Where(f => f.FechaEmision >= inicioMes && f.Estado != "Cancelada")
                        .SumAsync(f => (decimal?)f.TotalDOP) ?? 0;
                    
                    facturasMes = await _context.Facturas.CountAsync(f => f.FechaEmision >= inicioMes);
                    facturasPendientes = await _context.Facturas.CountAsync(f => f.Estado == "Pendiente" || f.Estado == "Emitida");
                    totalClientes = await _context.Clientes.CountAsync();
                    stockBajo = await _context.Productos.CountAsync(p => p.Activo && p.Stock <= 10);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al cargar stats básicas");
                }

                // Datos para Gráfico de Tendencia
                var labelsTendencia = new List<string>();
                var dataTendencia = new List<decimal>();
                try {
                    var ventasUltimos7Dias = await _context.Facturas
                        .Where(f => f.FechaEmision >= hace7Dias && f.Estado != "Cancelada")
                        .GroupBy(f => f.FechaEmision.Date)
                        .Select(g => new { Fecha = g.Key, Total = g.Sum(f => (decimal?)f.TotalDOP) })
                        .OrderBy(g => g.Fecha)
                        .ToListAsync();

                    for (int i = 0; i < 7; i++)
                    {
                        var fecha = hace7Dias.AddDays(i);
                        labelsTendencia.Add(fecha.ToString("dd MMM"));
                        dataTendencia.Add((decimal)(ventasUltimos7Dias.FirstOrDefault(v => v.Fecha == fecha.Date)?.Total ?? 0));
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al cargar gráfico de tendencia");
                    for (int i = 0; i < 7; i++) {
                        labelsTendencia.Add(hace7Dias.AddDays(i).ToString("dd MMM"));
                        dataTendencia.Add(0);
                    }
                }

                // Datos para Gráfico de Categorías
                var labelsCat = new List<string> { "Sin Datos" };
                var dataCat = new List<int> { 0 };
                try {
                    var topProductos = await _context.FacturaLineas
                        .Where(fl => fl.Factura.FechaEmision >= inicioMes)
                        .GroupBy(fl => fl.NombreItem)
                        .Select(g => new { Nombre = g.Key ?? "Sin Nombre", Cantidad = g.Count() })
                        .OrderByDescending(g => g.Cantidad)
                        .Take(5)
                        .ToListAsync();

                    if (topProductos.Any()) {
                        labelsCat = topProductos.Select(p => p.Nombre).ToList();
                        dataCat = topProductos.Select(p => p.Cantidad).ToList();
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al cargar gráfico de categorías");
                }

                return Json(new
                {
                    ingresosMes,
                    facturasMes,
                    facturasPendientes,
                    totalClientes,
                    totalProductos = await _context.Productos.CountAsync(p => p.Activo),
                    stockBajo,
                    tendencia = new { labels = labelsTendencia, data = dataTendencia },
                    categorias = new { labels = labelsCat, data = dataCat }
                });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error fatal en GetDashboardStats");
                return Json(new { error = "Ocurrió un error al cargar las estadísticas" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFacturasRecientes()
        {
            try {
                var facturas = await _context.Facturas
                    .Include(f => f.Cliente)
                    .OrderByDescending(f => f.FechaEmision)
                    .Take(5)
                    .ToListAsync();

                return Json(facturas.Select(f => new
                {
                    f.Id,
                    f.NumeroFactura,
                    Cliente = f.Cliente?.Nombre ?? "Consumidor Final",
                    Fecha = f.FechaEmision.ToString("dd/MM/yyyy"),
                    Total = f.Total,
                    f.Estado
                }));
            } catch (Exception ex) {
                _logger.LogError(ex, "Error al cargar facturas recientes");
                return Json(new List<object>());
            }
        }

        public IActionResult Reportes()
        {
            return RedirectToAction("Dashboard", "Reportes");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }

    public class HomeDashboardViewModel
    {
        public decimal IngresosMes { get; set; }
        public int FacturasMes { get; set; }
        public int FacturasPendientes { get; set; }
        public int ClientesActivos { get; set; }
        public int TotalClientes { get; set; }
        public int TotalProductos { get; set; }
        public int StockBajo { get; set; }
        public List<Factura> FacturasRecientes { get; set; } = new();
        public List<NCFAlert> AlertasNCF { get; set; } = new();
    }

    public class NCFAlert
    {
        public string Tipo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Nivel { get; set; } = "warning";
        public long CantidadRestante { get; set; }
        public DateTime FechaVencimiento { get; set; }
    }
}
