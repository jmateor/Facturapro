using Facturapro.Data;
using Facturapro.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Controllers
{
    [Authorize(Roles = "Admin,Gerente")]
    public class IntegracionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IntegracionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Integraciones
        public async Task<IActionResult> Index()
        {
            var config = await _context.ConfiguracionIntegraciones.FirstOrDefaultAsync();
            if (config == null)
            {
                config = new ConfiguracionIntegracion();
                _context.ConfiguracionIntegraciones.Add(config);
                await _context.SaveChangesAsync();
            }
            return View(config);
        }

        // POST: Integraciones/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(ConfiguracionIntegracion config)
        {
            if (ModelState.IsValid)
            {
                var dbConfig = await _context.ConfiguracionIntegraciones.FirstOrDefaultAsync();
                if (dbConfig == null)
                {
                    _context.ConfiguracionIntegraciones.Add(config);
                }
                else
                {
                    // Email
                    dbConfig.EmailHabilitado = config.EmailHabilitado;
                    dbConfig.SmtpServer = config.SmtpServer;
                    dbConfig.SmtpPort = config.SmtpPort;
                    dbConfig.SmtpUser = config.SmtpUser;
                    if (!string.IsNullOrEmpty(config.SmtpPassword))
                        dbConfig.SmtpPassword = config.SmtpPassword;
                    dbConfig.SmtpUseSSL = config.SmtpUseSSL;

                    // WhatsApp
                    dbConfig.WhatsAppHabilitado = config.WhatsAppHabilitado;
                    dbConfig.WhatsAppApiKey = config.WhatsAppApiKey;
                    dbConfig.WhatsAppPhoneId = config.WhatsAppPhoneId;

                    // DGII
                    dbConfig.DgiiValidacionHabilitada = config.DgiiValidacionHabilitada;

                    dbConfig.FechaActualizacion = DateTime.Now;
                    _context.Update(dbConfig);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Configuración de integraciones actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View("Index", config);
        }
    }
}
