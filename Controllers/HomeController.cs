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
            var hoy = DateTime.Now;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            // Estadísticas del dashboard
            var dashboardData = new HomeDashboardViewModel
            {
                // Ingresos del mes (facturas del mes actual)
                IngresosMes = await _context.Facturas
                    .Where(f => f.FechaEmision >= inicioMes && f.Estado != "Cancelada")
                    .SumAsync(f => (decimal?)f.Total) ?? 0,

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
                    .ToListAsync()
            };

            return View(dashboardData);
        }

        public IActionResult Privacy()
        {
            return View();
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
    }
}
