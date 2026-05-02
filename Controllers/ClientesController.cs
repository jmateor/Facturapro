using Facturapro.Data;
using Facturapro.Models.Entities;
using Facturapro.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Controllers
{
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Clientes
        public async Task<IActionResult> Index()
        {
            var clientes = await _context.Clientes
                .Include(c => c.Facturas)
                .Select(c => new ClienteViewModel
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Email = c.Email,
                    NIF = c.NIF,
                    Telefono = c.Telefono,
                    Ciudad = c.Ciudad,
                    Activo = c.Activo,
                    TotalFacturas = c.Facturas != null ? c.Facturas.Count : 0,
                    TotalFacturado = c.Facturas != null ? c.Facturas.Where(f => f.Estado == "Pagada").Sum(f => f.Total) : 0
                })
                .ToListAsync();

            return View(clientes);
        }

        // GET: Clientes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .Include(c => c.Facturas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        // GET: Clientes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clientes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Email,NIF,Direccion,Ciudad,CodigoPostal,Pais,Telefono")] Cliente cliente)
        {
            if (ModelState.IsValid)
            {
                cliente.FechaAlta = DateTime.Now;
                _context.Add(cliente);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cliente creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(cliente);
        }

        // GET: Clientes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound();
            }
            return View(cliente);
        }

        // POST: Clientes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Email,NIF,Direccion,Ciudad,CodigoPostal,Pais,Telefono,Activo")] Cliente cliente)
        {
            if (id != cliente.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cliente);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cliente actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClienteExists(cliente.Id))
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
            return View(cliente);
        }

        // GET: Clientes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        // POST: Clientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cliente eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ClienteExists(int id)
        {
            return _context.Clientes.Any(e => e.Id == id);
        }

        // POST: Clientes/CrearRapido (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearRapido([Bind("Nombre,Email,NIF,Direccion,Ciudad,Telefono")] Cliente cliente)
        {
            if (string.IsNullOrEmpty(cliente.Nombre))
            {
                return BadRequest(new { success = false, message = "El nombre es obligatorio" });
            }

            cliente.FechaAlta = DateTime.Now;
            cliente.Activo = true;

            _context.Add(cliente);
            await _context.SaveChangesAsync();

            return Ok(new {
                success = true,
                message = "Cliente creado correctamente",
                cliente = new {
                    Id = cliente.Id,
                    Nombre = cliente.Nombre,
                    RNC = cliente.NIF,
                    Direccion = cliente.Direccion,
                    Telefono = cliente.Telefono,
                    Email = cliente.Email
                }
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetClientesJson(string q, string estado)
        {
            var query = _context.Clientes
                .Include(c => c.Facturas)
                .AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                q = q.ToLower();
                query = query.Where(c => c.Nombre.ToLower().Contains(q) || 
                                       (c.NIF != null && c.NIF.ToLower().Contains(q)) || 
                                       (c.Email != null && c.Email.ToLower().Contains(q)));
            }

            if (!string.IsNullOrEmpty(estado))
            {
                bool activo = estado == "activo";
                query = query.Where(c => c.Activo == activo);
            }

            var clientes = await query
                .Select(c => new ClienteViewModel
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Email = c.Email,
                    NIF = c.NIF,
                    Telefono = c.Telefono,
                    Ciudad = c.Ciudad,
                    Activo = c.Activo,
                    TotalFacturas = c.Facturas != null ? c.Facturas.Count : 0,
                    TotalFacturado = c.Facturas != null ? c.Facturas.Where(f => f.Estado == "Pagada").Sum(f => f.Total) : 0
                })
                .ToListAsync();

            return Json(clientes);
        }
    }
}
