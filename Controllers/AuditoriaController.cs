using Facturapro.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Controllers
{
    [Authorize(Roles = "Admin,Gerente")]
    public class AuditoriaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditoriaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Auditoria
        public async Task<IActionResult> Index(string? modulo, string? usuario, string? nivel, DateTime? desde, DateTime? hasta)
        {
            var query = _context.LogsAuditoria.AsQueryable();

            if (!string.IsNullOrEmpty(modulo))
                query = query.Where(l => l.Modulo == modulo);

            if (!string.IsNullOrEmpty(usuario))
                query = query.Where(l => l.Usuario.Contains(usuario));

            if (!string.IsNullOrEmpty(nivel))
                query = query.Where(l => l.Nivel == nivel);

            if (desde.HasValue)
                query = query.Where(l => l.Fecha >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(l => l.Fecha <= hasta.Value.AddDays(1));

            var logs = await query
                .OrderByDescending(l => l.Fecha)
                .Take(500)
                .ToListAsync();

            ViewBag.Modulos = await _context.LogsAuditoria.Select(l => l.Modulo).Distinct().ToListAsync();
            ViewBag.Niveles = new List<string> { "Info", "Warning", "Error", "Critical" };

            return View(logs);
        }

        // GET: Auditoria/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var log = await _context.LogsAuditoria.FindAsync(id);
            if (log == null) return NotFound();

            return View(log);
        }
    }
}
