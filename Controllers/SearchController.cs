using Facturapro.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Query(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Json(new { success = true, results = new List<object>() });
            }

            q = q.ToLower().Trim();
            var results = new List<object>();

            // 1. Módulos y Acciones (Comandos rápidos)
            var modules = new List<SearchItem>
            {
                new SearchItem("Nueva Factura", "/Facturas/Create", "ri-file-add-line", "Acción"),
                new SearchItem("Cierre de Caja", "/Caja/Cierre", "ri-safe-2-line", "Módulo"),
                new SearchItem("Ver Inventario", "/Productos/Index", "ri-box-3-line", "Módulo"),
                new SearchItem("Reportes de Venta", "/Reportes/Ventas", "ri-line-chart-line", "Módulo"),
                new SearchItem("Configuración DGII", "/Configuracion/DGII", "ri-settings-line", "Sistema")
            };

            var matchedModules = modules.Where(m => m.Title.ToLower().Contains(q)).ToList();
            if (matchedModules.Any()) results.AddRange(matchedModules);

            // 2. Clientes
            var clientes = await _context.Clientes
                .Where(c => c.Nombre.Contains(q) || c.RNC.Contains(q))
                .Take(5)
                .Select(c => new SearchItem(c.Nombre, $"/Clientes/Details/{c.Id}", "ri-user-line", "Cliente", c.RNC))
                .ToListAsync();
            results.AddRange(clientes);

            // 3. Productos
            var productos = await _context.Productos
                .Where(p => p.Nombre.Contains(q) || p.Codigo.Contains(q))
                .Take(5)
                .Select(p => new SearchItem(p.Nombre, $"/Productos/Details/{p.Id}", "ri-box-3-line", "Producto", $"Código: {p.Codigo}"))
                .ToListAsync();
            results.AddRange(productos);

            // 4. Facturas
            var facturas = await _context.Facturas
                .Where(f => (f.eNCF != null && f.eNCF.Contains(q)) || f.NumeroFactura.Contains(q))
                .Take(5)
                .Select(f => new SearchItem(f.eNCF ?? f.NumeroFactura, $"/Facturas/Details/{f.Id}", "ri-file-list-3-line", "Factura", f.EstadoDGII))
                .ToListAsync();
            results.AddRange(facturas);

            return Json(new { success = true, results });
        }

        private class SearchItem
        {
            public string Title { get; set; }
            public string Url { get; set; }
            public string Icon { get; set; }
            public string Category { get; set; }
            public string? Subtitle { get; set; }

            public SearchItem(string title, string url, string icon, string category, string? subtitle = null)
            {
                Title = title;
                Url = url;
                Icon = icon;
                Category = category;
                Subtitle = subtitle;
            }
        }
    }
}
