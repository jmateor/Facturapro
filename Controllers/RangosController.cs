using Facturapro.Data;
using Facturapro.Models.Entities;
using Facturapro.Services.DGII;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Facturapro.Controllers
{
    [Authorize]
    public class RangosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RangoNumeracionService _rangoService;
        private readonly ILogger<RangosController> _logger;

        public RangosController(
            ApplicationDbContext context,
            RangoNumeracionService rangoService,
            ILogger<RangosController> logger)
        {
            _context = context;
            _rangoService = rangoService;
            _logger = logger;
        }

        // GET: Rangos
        public async Task<IActionResult> Index()
        {
            var estadisticas = await _rangoService.ObtenerEstadisticasAsync();
            return View(estadisticas);
        }

        // GET: Rangos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Rangos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RangoNumeracion rango)
        {
            if (ModelState.IsValid)
            {
                var resultado = await _rangoService.CrearRangoAsync(
                    rango.TipoECF,
                    rango.RangoDesde,
                    rango.RangoHasta,
                    rango.FechaVencimiento,
                    rango.Observaciones
                );

                if (resultado.Exito)
                {
                    TempData["SuccessMessage"] = resultado.Mensaje;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", resultado.Mensaje);
                }
            }

            return View(rango);
        }

        // GET: Rangos/VerificarDisponibilidad/tipoECF
        public async Task<JsonResult> VerificarDisponibilidad(string tipoECF)
        {
            var disponible = await _rangoService.ExisteRangoDisponibleAsync(tipoECF);
            return Json(new { disponible });
        }
    }
}
