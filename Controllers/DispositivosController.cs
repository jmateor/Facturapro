using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Controllers
{
    [Authorize(Roles = "Admin,Gerente")]
    public class DispositivosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DispositivosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Dispositivos
        public async Task<IActionResult> Index()
        {
            var config = await _context.ConfiguracionDispositivos.FirstOrDefaultAsync();
            if (config == null)
            {
                config = new ConfiguracionDispositivo();
                _context.ConfiguracionDispositivos.Add(config);
                await _context.SaveChangesAsync();
            }
            return View(config);
        }

        // POST: Dispositivos/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(ConfiguracionDispositivo config)
        {
            if (ModelState.IsValid)
            {
                var dbConfig = await _context.ConfiguracionDispositivos.FirstOrDefaultAsync();
                if (dbConfig == null)
                {
                    _context.ConfiguracionDispositivos.Add(config);
                }
                else
                {
                    dbConfig.HabilitarImpresora = config.HabilitarImpresora;
                    dbConfig.AnchoPapel = config.AnchoPapel;
                    dbConfig.CorteAutomatico = config.CorteAutomatico;
                    dbConfig.AbrirCajon = config.AbrirCajon;
                    dbConfig.ImprimirCopia = config.ImprimirCopia;
                    dbConfig.HabilitarLector = config.HabilitarLector;
                    dbConfig.ModoEscaneo = config.ModoEscaneo;
                    dbConfig.SufijoLectura = config.SufijoLectura;
                    dbConfig.SonidoEscaneo = config.SonidoEscaneo;
                    dbConfig.FechaActualizacion = DateTime.Now;
                    _context.Update(dbConfig);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Configuración de dispositivos actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View("Index", config);
        }
    }
}
