using Facturapro.Data;
using Facturapro.Services.DGII;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Facturapro.Controllers
{
    [Authorize]
    public class FiscalController : Controller
    {
        private readonly IFiscalService _fiscalService;
        private readonly ApplicationDbContext _context;

        public FiscalController(IFiscalService fiscalService, ApplicationDbContext context)
        {
            _fiscalService = fiscalService;
            _context = context;
        }

        public async Task<IActionResult> Index(int? mes, int? año)
        {
            int m = mes ?? DateTime.Now.Month;
            int a = año ?? DateTime.Now.Year;

            var summary = await _fiscalService.GetSummaryAsync(m, a);
            ViewBag.Mes = m;
            ViewBag.Año = a;

            return View(summary);
        }

        [HttpPost]
        public async Task<IActionResult> Exportar606(int mes, int año)
        {
            var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
            var rncEmisor = config?.RNCEmisor?.Replace("-", "") ?? "000000000";

            var content = await _fiscalService.Generar606Async(mes, año);
            var bytes = Encoding.UTF8.GetBytes(content);
            var fileName = $"606_{rncEmisor}_{año}{mes:D2}.txt";

            return File(bytes, "text/plain", fileName);
        }

        [HttpPost]
        public async Task<IActionResult> Exportar607(int mes, int año)
        {
            var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
            var rncEmisor = config?.RNCEmisor?.Replace("-", "") ?? "000000000";

            var content = await _fiscalService.Generar607Async(mes, año);
            var bytes = Encoding.UTF8.GetBytes(content);
            var fileName = $"607_{rncEmisor}_{año}{mes:D2}.txt";

            return File(bytes, "text/plain", fileName);
        }
    }
}
