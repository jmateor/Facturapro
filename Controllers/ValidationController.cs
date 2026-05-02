using Microsoft.AspNetCore.Mvc;
using Facturapro.Services.DGII;
using Microsoft.AspNetCore.Authorization;

namespace Facturapro.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ValidationController : ControllerBase
    {
        private readonly IRncValidationService _rncService;

        public ValidationController(IRncValidationService rncService)
        {
            _rncService = rncService;
        }

        [HttpGet("rnc/{documento}")]
        public async Task<IActionResult> ValidarRnc(string documento)
        {
            if (string.IsNullOrEmpty(documento))
                return BadRequest("El documento es requerido");

            var result = await _rncService.ConsultarDGIIAsync(documento);
            return Ok(result);
        }
    }
}
