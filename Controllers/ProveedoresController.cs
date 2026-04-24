using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Controllers
{
    [Authorize]
    public class ProveedoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Proveedores
        public async Task<IActionResult> Index(string buscar)
        {
            var query = _context.Proveedores.AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(p => p.Nombre.Contains(buscar) ||
                                       p.Email!.Contains(buscar) ||
                                       p.Documento!.Contains(buscar));
            }

            return View(await query.OrderBy(p => p.Nombre).ToListAsync());
        }

        // GET: Proveedores/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedores
                .Include(p => p.Compras)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (proveedor == null)
            {
                return NotFound();
            }

            return View(proveedor);
        }

        // GET: Proveedores/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Proveedores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Documento,Direccion,Email,Telefono,PersonaContacto,Activo")] Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                // Validar que el nombre no exista
                if (await _context.Proveedores.AnyAsync(p => p.Nombre == proveedor.Nombre))
                {
                    ModelState.AddModelError("Nombre", "Ya existe un proveedor con este nombre");
                    return View(proveedor);
                }

                proveedor.FechaCreacion = DateTime.Now;
                _context.Add(proveedor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Proveedor creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(proveedor);
        }

        // GET: Proveedores/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
            {
                return NotFound();
            }
            return View(proveedor);
        }

        // POST: Proveedores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Documento,Direccion,Email,Telefono,PersonaContacto,Activo")] Proveedor proveedor)
        {
            if (id != proveedor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Validar que el nombre no exista en otro proveedor
                if (await _context.Proveedores.AnyAsync(p => p.Nombre == proveedor.Nombre && p.Id != proveedor.Id))
                {
                    ModelState.AddModelError("Nombre", "Ya existe un proveedor con este nombre");
                    return View(proveedor);
                }

                try
                {
                    _context.Update(proveedor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Proveedor actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProveedorExists(proveedor.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(proveedor);
        }

        // GET: Proveedores/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedores
                .Include(p => p.Compras)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (proveedor == null)
            {
                return NotFound();
            }

            return View(proveedor);
        }

        // POST: Proveedores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await _context.Proveedores
                .Include(p => p.Compras)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proveedor != null)
            {
                if (proveedor.Compras.Any())
                {
                    TempData["ErrorMessage"] = "No se puede eliminar el proveedor porque tiene compras registradas.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Proveedores.Remove(proveedor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Proveedor eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProveedorExists(int id)
        {
            return _context.Proveedores.Any(e => e.Id == id);
        }
    }
}
