using Facturapro.Data;
using Facturapro.Models;
using Facturapro.Models.DGII;
using Facturapro.Models.Entities;
using Facturapro.Services.DGII;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Facturapro.Controllers
{
    [Authorize]
    public class ConfiguracionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ConfiguracionController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;

        public ConfiguracionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ConfiguracionController> logger,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _env = env;
            _httpClientFactory = httpClientFactory;
        }

        // GET: Configuracion
        public IActionResult Index()
        {
            return View();
        }

        // GET: Configuracion/Empresa
        public async Task<IActionResult> Empresa()
        {
            var config = await _context.ConfiguracionEmpresas
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                config = new ConfiguracionEmpresa
                {
                    RNCEmisor = "",
                    RazonSocialEmisor = "",
                    DireccionEmisor = ""
                };
            }

            return View(config);
        }

        // GET: Configuracion/Ventas
        public async Task<IActionResult> Ventas()
        {
            var config = await _context.ConfiguracionEmpresas
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                config = new ConfiguracionEmpresa
                {
                    PermitirVentasCredito = true,
                    ImprimirAutomático = true,
                    ClienteObligatorio = false,
                    DescuentoCantidad = true,
                    StockNegativo = false,
                    UsarValoresDefecto = true,
                    DecimalesPrecios = 2,
                    RedondeoTotales = "none",
                    Moneda = "DOP",
                    SimboloMoneda = "prefix",
                    TipoComprobanteDefecto = "E31",
                    TipoIngresoPorDefecto = "01",
                    TipoPagoPorDefecto = 1,
                    ITBISPorDefecto = 18,
                    MontoVentaMinima = 0,
                    MontoVentaMaxima = 0,
                    MontoCreditoMaximo = 0,
                    DiasPlazoCredito = 30
                };
            }

            return View(config);
        }

        // POST: Configuracion/Ventas
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ventas(ConfiguracionEmpresa configuracion)
        {
            if (ModelState.IsValid)
            {
                var existente = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();

                if (existente != null)
                {
                    // Actualizar solo los campos de configuración de ventas
                    existente.PermitirVentasCredito = configuracion.PermitirVentasCredito;
                    existente.ImprimirAutomático = configuracion.ImprimirAutomático;
                    existente.ClienteObligatorio = configuracion.ClienteObligatorio;
                    existente.DescuentoCantidad = configuracion.DescuentoCantidad;
                    existente.StockNegativo = configuracion.StockNegativo;
                    existente.UsarValoresDefecto = configuracion.UsarValoresDefecto;
                    existente.DecimalesPrecios = configuracion.DecimalesPrecios;
                    existente.RedondeoTotales = configuracion.RedondeoTotales ?? "none";
                    existente.Moneda = configuracion.Moneda ?? "DOP";
                    existente.SimboloMoneda = configuracion.SimboloMoneda ?? "prefix";
                    existente.TipoComprobanteDefecto = configuracion.TipoComprobanteDefecto ?? "E31";
                    existente.TipoIngresoPorDefecto = configuracion.TipoIngresoPorDefecto ?? "01";
                    existente.TipoPagoPorDefecto = configuracion.TipoPagoPorDefecto;
                    existente.ITBISPorDefecto = configuracion.ITBISPorDefecto;
                    existente.MontoVentaMinima = configuracion.MontoVentaMinima;
                    existente.MontoVentaMaxima = configuracion.MontoVentaMaxima;
                    existente.MontoCreditoMaximo = configuracion.MontoCreditoMaximo;
                    existente.DiasPlazoCredito = configuracion.DiasPlazoCredito;
                    existente.FechaActualizacion = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Configuración de ventas guardada exitosamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "No hay configuración de empresa. Configure primero los datos de la empresa.";
                }
            }

            return RedirectToAction(nameof(Ventas));
        }

        // GET: Configuracion/Impresion
        public async Task<IActionResult> Impresion()
        {
            var config = await _context.ConfiguracionEmpresas
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                config = new ConfiguracionEmpresa
                {
                    TipoTicket = "thermal",
                    MostrarLogo = true,
                    MostrarDescripcion = true,
                    MostrarCodigoBarras = true,
                    MostrarImpuestos = true,
                    MostrarNCF = false,
                    PiePagina = "¡Gracias por su compra!",
                    TamanoFuentePie = "medium"
                };
            }

            return View(config);
        }

        // POST: Configuracion/Impresion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Impresion(ConfiguracionEmpresa configuracion)
        {
            if (ModelState.IsValid)
            {
                var existente = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();

                if (existente != null)
                {
                    // Actualizar solo los campos de impresión
                    existente.TipoTicket = configuracion.TipoTicket ?? "thermal";
                    existente.MostrarLogo = configuracion.MostrarLogo;
                    existente.MostrarDescripcion = configuracion.MostrarDescripcion;
                    existente.MostrarCodigoBarras = configuracion.MostrarCodigoBarras;
                    existente.MostrarNCF = configuracion.MostrarNCF;
                    existente.MostrarImpuestos = configuracion.MostrarImpuestos;
                    existente.PiePagina = configuracion.PiePagina;
                    existente.TamanoFuentePie = configuracion.TamanoFuentePie ?? "medium";
                    existente.FechaActualizacion = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Configuración de impresión guardada exitosamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "No hay configuración de empresa. Configure primero los datos de la empresa.";
                }
            }

            return RedirectToAction(nameof(Impresion));
        }

        // GET: Configuracion/Backup
        public IActionResult Backup()
        {
            return View();
        }

        // GET: Configuracion/Usuarios
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = _userManager.Users.ToList();
            return View(usuarios);
        }

        // GET: Configuracion/DGII
        public async Task<IActionResult> DGII()
        {
            var config = await _context.ConfiguracionEmpresas
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                config = new ConfiguracionEmpresa
                {
                    RNCEmisor = "",
                    RazonSocialEmisor = "",
                    DireccionEmisor = ""
                };
            }

            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Empresa(ConfiguracionEmpresa configuracion)
        {
            if (ModelState.IsValid)
            {
                var existente = await _context.ConfiguracionEmpresas
                    .FirstOrDefaultAsync();

                if (existente != null)
                {
                    existente.RNCEmisor = configuracion.RNCEmisor;
                    existente.RazonSocialEmisor = configuracion.RazonSocialEmisor;
                    existente.NombreComercial = configuracion.NombreComercial;
                    existente.DireccionEmisor = configuracion.DireccionEmisor;
                    existente.Municipio = configuracion.Municipio;
                    existente.Provincia = configuracion.Provincia;
                    existente.TelefonoEmisor = configuracion.TelefonoEmisor;
                    existente.CorreoEmisor = configuracion.CorreoEmisor;
                    existente.ActividadEconomica = configuracion.ActividadEconomica;
                    existente.Multimoneda = configuracion.Multimoneda;
                    existente.UsarCaja = configuracion.UsarCaja;
                    existente.CuentaIngresosEfectivo = configuracion.CuentaIngresosEfectivo;
                    existente.CuentaSalidasEfectivo = configuracion.CuentaSalidasEfectivo;
                    existente.FechaActualizacion = DateTime.Now;
                }
                else
                {
                    _context.ConfiguracionEmpresas.Add(configuracion);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Configuración de empresa guardada exitosamente.";
            }

            return RedirectToAction(nameof(Empresa));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarConfiguracion(ConfiguracionEmpresa configuracion)
        {
            if (ModelState.IsValid)
            {
                var existente = await _context.ConfiguracionEmpresas
                    .FirstOrDefaultAsync();

                if (existente != null)
                {
                    existente.RNCEmisor = configuracion.RNCEmisor;
                    existente.RazonSocialEmisor = configuracion.RazonSocialEmisor;
                    existente.NombreComercial = configuracion.NombreComercial;
                    existente.DireccionEmisor = configuracion.DireccionEmisor;
                    existente.Municipio = configuracion.Municipio;
                    existente.Provincia = configuracion.Provincia;
                    existente.TelefonoEmisor = configuracion.TelefonoEmisor;
                    existente.CorreoEmisor = configuracion.CorreoEmisor;
                    existente.ActividadEconomica = configuracion.ActividadEconomica;
                    existente.FechaActualizacion = DateTime.Now;
                }
                else
                {
                    _context.ConfiguracionEmpresas.Add(configuracion);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Configuración guardada exitosamente.";
            }

            return RedirectToAction(nameof(DGII));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirCertificado(IFormFile certificado, string passwordCertificado)
        {
            if (certificado == null || certificado.Length == 0)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un archivo de certificado.";
                return RedirectToAction(nameof(DGII));
            }

            if (string.IsNullOrEmpty(passwordCertificado))
            {
                TempData["ErrorMessage"] = "Debe proporcionar la contraseña del certificado.";
                return RedirectToAction(nameof(DGII));
            }

            try
            {
                // Crear directorio para certificados FUERA de wwwroot (seguridad)
                var certPath = Path.Combine(_env.ContentRootPath, "App_Data", "certificados");
                Directory.CreateDirectory(certPath);

                // Guardar archivo
                var fileName = $"certificado_{DateTime.Now:yyyyMMddHHmmss}.pfx";
                var filePath = Path.Combine(certPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await certificado.CopyToAsync(stream);
                }

                // Validar certificado
                var x509 = new X509Certificate2(filePath, passwordCertificado);

                if (!x509.HasPrivateKey)
                {
                    System.IO.File.Delete(filePath);
                    TempData["ErrorMessage"] = "El certificado no contiene la clave privada.";
                    return RedirectToAction(nameof(DGII));
                }

                // Generar huella digital
                var huella = x509.Thumbprint;

                // Actualizar configuración
                var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
                if (config != null)
                {
                    // Eliminar certificado anterior si existe
                    if (!string.IsNullOrEmpty(config.RutaCertificado) && System.IO.File.Exists(config.RutaCertificado))
                    {
                        System.IO.File.Delete(config.RutaCertificado);
                    }

                    config.RutaCertificado = filePath;
                    config.PasswordCertificado = passwordCertificado; // En producción, encriptar
                    config.HuellaCertificado = huella;
                    config.FechaVencimientoCertificado = x509.NotAfter;
                    config.FechaActualizacion = DateTime.Now;

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Certificado subido exitosamente. Válido hasta: {x509.NotAfter:dd/MM/yyyy}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir certificado");
                TempData["ErrorMessage"] = "Error al procesar el certificado. Verifique la contraseña.";
            }

            return RedirectToAction(nameof(DGII));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarModo(bool modoPruebas)
        {
            var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
            if (config != null)
            {
                config.ModoPruebas = modoPruebas;
                config.FechaActualizacion = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = modoPruebas
                    ? "Sistema configurado en MODO PRUEBAS. Los comprobantes no tienen validez fiscal."
                    : "Sistema configurado en MODO PRODUCCIÓN. Los comprobantes son reales.";
            }

            return RedirectToAction(nameof(DGII));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarImpuestos(ConfiguracionEmpresa configuracion)
        {
            var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
            if (config != null)
            {
                config.ITBISPorDefecto = configuracion.ITBISPorDefecto;
                config.TipoIngresosPorDefecto = configuracion.TipoIngresosPorDefecto;
                config.FechaActualizacion = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Configuración de impuestos actualizada.";
            }

            return RedirectToAction(nameof(DGII));
        }

        // GET: Configuracion/VerificarServiciosDGII
        [HttpGet]
        public async Task<IActionResult> VerificarServiciosDGII()
        {
            try
            {
                var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
                if (config == null)
                {
                    return Json(new { disponible = false, mensaje = "Configuración no encontrada" });
                }

                // Crear instancia temporal del servicio DGII
                var dgiiConfig = new ConfiguracionAPIDGII
                {
                    Ambiente = config.ModoPruebas ? AmbienteDGII.Test : AmbienteDGII.Produccion,
                    RncEmisor = config.RNCEmisor,
                    RutaCertificadoP12 = config.RutaCertificado ?? string.Empty,
                    PasswordCertificado = config.PasswordCertificado ?? string.Empty
                };

                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var dgiiLogger = loggerFactory.CreateLogger<DGIIService>();
                var dgiiService = new DGIIService(_httpClientFactory, dgiiLogger, dgiiConfig);

                var resultado = await dgiiService.VerificarEstatusServiciosAsync();

                return Json(new
                {
                    disponible = resultado.Success && resultado.Data?.AutenticacionDisponible == true,
                    autenticacion = resultado.Data?.AutenticacionDisponible ?? false,
                    recepcion = resultado.Data?.RecepcionDisponible ?? false,
                    mensaje = resultado.Success ? "Servicios disponibles" : "Servicios no disponibles"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar servicios DGII");
                return Json(new { disponible = false, mensaje = ex.Message });
            }
        }

        // GET: Configuracion/ValidarCertificado
        [HttpGet]
        public async Task<IActionResult> ValidarCertificado()
        {
            try
            {
                var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
                if (config == null || string.IsNullOrEmpty(config.RutaCertificado))
                {
                    return Json(new { valido = false, mensaje = "No hay certificado configurado" });
                }

                if (!System.IO.File.Exists(config.RutaCertificado))
                {
                    return Json(new { valido = false, mensaje = "El archivo de certificado no existe" });
                }

                var certificado = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                    config.RutaCertificado,
                    config.PasswordCertificado);

                var valido = certificado.HasPrivateKey && certificado.NotAfter > DateTime.Now;

                return Json(new
                {
                    valido = valido,
                    huella = certificado.Thumbprint,
                    emisor = certificado.Issuer,
                    sujeto = certificado.Subject,
                    vigenteDesde = certificado.NotBefore,
                    vigenteHasta = certificado.NotAfter,
                    diasRestantes = (certificado.NotAfter - DateTime.Now).Days,
                    mensaje = valido ? "Certificado válido" : "Certificado inválido o expirado"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar certificado");
                return Json(new { valido = false, mensaje = $"Error: {ex.Message}" });
            }
        }
    }
}
