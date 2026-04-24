using Facturapro.Data;
using Facturapro.Models.DGII;
using Facturapro.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Services.DGII
{
    /// <summary>
    /// Servicio de orquestación para envío de comprobantes a la DGII usando la API real
    /// </summary>
    public interface IFacturacionElectronicaAPIService
    {
        Task<ResultadoEnvio> EnviarFacturaADGIIAsync(int facturaId);
        Task<ResultadoEnvio> EnviarFacturaConsumoADGIIAsync(int facturaId);
        Task ConsultarEstadoPendientesAsync();
        Task<bool> ValidarCertificadoAsync();
        Task<bool> VerificarServiciosDGIIAsync();
    }

    public class FacturacionElectronicaAPIService : IFacturacionElectronicaAPIService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDGIIService _dgiiService;
        private readonly FacturacionElectronicaService _xmlService;
        private readonly ILogger<FacturacionElectronicaAPIService> _logger;

        public FacturacionElectronicaAPIService(
            ApplicationDbContext context,
            IDGIIService dgiiService,
            FacturacionElectronicaService xmlService,
            ILogger<FacturacionElectronicaAPIService> logger)
        {
            _context = context;
            _dgiiService = dgiiService;
            _xmlService = xmlService;
            _logger = logger;
        }

        /// <summary>
        /// Envía una factura a la DGII mediante la API
        /// </summary>
        public async Task<ResultadoEnvio> EnviarFacturaADGIIAsync(int facturaId)
        {
            try
            {
                _logger.LogInformation("Iniciando envío de factura {FacturaId} a DGII", facturaId);

                // 1. Obtener factura completa
                var factura = await _context.Facturas
                    .Include(f => f.Cliente)
                    .Include(f => f.Lineas)
                    .FirstOrDefaultAsync(f => f.Id == facturaId);

                if (factura == null)
                    return ResultadoEnvio.Fallido("Factura no encontrada");

                if (factura.EstadoDGII == "Aceptado")
                    return ResultadoEnvio.CrearExitoso("Factura ya fue aceptada por la DGII", factura.eNCF ?? "");

                if (string.IsNullOrEmpty(factura.eNCF))
                    return ResultadoEnvio.Fallido("La factura no tiene número e-CF asignado");

                // 2. Generar XML
                var xml = _xmlService.GenerarXMLECF(factura, factura.Cliente!);
                var xmlString = xml.ToString();

                // 3. Firmar XML con certificado
                var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
                if (config == null || string.IsNullOrEmpty(config.RutaCertificado))
                    return ResultadoEnvio.Fallido("No hay certificado configurado");

                var xmlFirmado = await FirmarXMLAsync(xmlString, config);
                if (string.IsNullOrEmpty(xmlFirmado))
                    return ResultadoEnvio.Fallido("Error al firmar el XML");

                // 4. Enviar a DGII
                factura.EstadoDGII = "Enviado";
                await _context.SaveChangesAsync();

                // Determinar si es Factura de Consumo (< 250,000)
                if (factura.Total < 250000 && factura.TipoECF == "32")
                {
                    var resultadoFC = await _dgiiService.EnviarFacturaConsumoAsync(factura, xmlFirmado);
                    // Mapear resultado de FC al tipo estándar
                    if (!resultadoFC.Success)
                    {
                        factura.EstadoDGII = "Rechazado";
                        factura.MensajeDGII = resultadoFC.Message;
                        await _context.SaveChangesAsync();

                        _logger.LogWarning("Factura {eNCF} rechazada: {Mensaje}", factura.eNCF, resultadoFC.Message);
                        return ResultadoEnvio.Fallido(resultadoFC.Message);
                    }

                    // Actualizar factura con TrackId
                    factura.EstadoDGII = "EnProceso";
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Factura de consumo {eNCF} enviada exitosamente.",
                        factura.eNCF);

                    return ResultadoEnvio.CrearExitoso(
                        $"Factura de consumo enviada a DGII correctamente.",
                        factura.eNCF,
                        null,
                        factura.eNCF);
                }

                var resultado = await _dgiiService.EnviarComprobanteAsync(factura, xmlFirmado);

                // 5. Procesar respuesta
                if (!resultado.Success)
                {
                    factura.EstadoDGII = "Rechazado";
                    factura.MensajeDGII = resultado.Message;
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Factura {eNCF} rechazada: {Mensaje}", factura.eNCF, resultado.Message);
                    return ResultadoEnvio.Fallido(resultado.Message);
                }

                // 6. Actualizar factura con TrackId
                factura.EstadoDGII = "EnProceso";
                factura.MensajeDGII = $"TrackId: {resultado.Data!.TrackId}";
                factura.XMLFirmado = xmlFirmado;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Factura {eNCF} enviada exitosamente. TrackId: {TrackId}",
                    factura.eNCF, resultado.Data.TrackId);

                return ResultadoEnvio.CrearExitoso(
                    "Factura enviada exitosamente",
                    factura.eNCF,
                    resultado.Data.TrackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar factura {FacturaId} a DGII", facturaId);
                return ResultadoEnvio.Fallido($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Envía específicamente una factura de consumo (< RD$250,000)
        /// </summary>
        public async Task<ResultadoEnvio> EnviarFacturaConsumoADGIIAsync(int facturaId)
        {
            try
            {
                var factura = await _context.Facturas
                    .Include(f => f.Cliente)
                    .Include(f => f.Lineas)
                    .FirstOrDefaultAsync(f => f.Id == facturaId);

                if (factura == null)
                    return ResultadoEnvio.Fallido("Factura no encontrada");

                // Generar XML específico para FC
                var xml = _xmlService.GenerarXMLECF(factura, factura.Cliente!);
                var xmlString = xml.ToString();

                var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
                var xmlFirmado = await FirmarXMLAsync(xmlString, config!);

                factura.EstadoDGII = "Enviado";
                await _context.SaveChangesAsync();

                var resultado = await _dgiiService.EnviarFacturaConsumoAsync(factura, xmlFirmado);

                if (!resultado.Success)
                {
                    factura.EstadoDGII = "Rechazado";
                    factura.MensajeDGII = resultado.Message;
                    await _context.SaveChangesAsync();
                    return ResultadoEnvio.Fallido(resultado.Message);
                }

                factura.EstadoDGII = "Aceptado"; // FC suele ser aceptada inmediatamente
                factura.MensajeDGII = $"eNCF: {resultado.Data!.ENCF}";
                await _context.SaveChangesAsync();

                return ResultadoEnvio.CrearExitoso(
                    "Factura de consumo enviada exitosamente",
                    factura.eNCF,
                    null,
                    resultado.Data.ENCF);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar FC {FacturaId}", facturaId);
                return ResultadoEnvio.Fallido($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Consulta el estado de las facturas pendientes
        /// </summary>
        public async Task ConsultarEstadoPendientesAsync()
        {
            try
            {
                var facturasPendientes = await _context.Facturas
                    .Where(f => f.EstadoDGII == "EnProceso" || f.EstadoDGII == "Enviado")
                    .Where(f => !string.IsNullOrEmpty(f.MensajeDGII))
                    .ToListAsync();

                _logger.LogInformation("Consultando estado de {Cantidad} facturas pendientes", facturasPendientes.Count);

                foreach (var factura in facturasPendientes)
                {
                    // Extraer TrackId del mensaje (formato: "TrackId: xxxxxx")
                    var trackId = ExtraerTrackId(factura.MensajeDGII);
                    if (string.IsNullOrEmpty(trackId))
                        continue;

                    var resultado = await _dgiiService.ConsultarEstadoAsync(trackId);

                    if (resultado.Success && resultado.Data != null)
                    {
                        var estadoAnterior = factura.EstadoDGII;
                        factura.EstadoDGII = resultado.Data.Estado;

                        if (resultado.Data.Mensajes?.Any() == true)
                        {
                            factura.MensajeDGII = string.Join("; ",
                                resultado.Data.Mensajes.Select(m => $"{m.Codigo}: {m.Valor}"));
                        }

                        _logger.LogInformation(
                            "Factura {eNCF} cambió de estado: {Anterior} -> {Nuevo}",
                            factura.eNCF, estadoAnterior, factura.EstadoDGII);
                    }

                    // Esperar un poco entre consultas para no saturar la API
                    await Task.Delay(1000);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar estados pendientes");
            }
        }

        /// <summary>
        /// Valida que el certificado esté configurado y sea válido
        /// </summary>
        public async Task<bool> ValidarCertificadoAsync()
        {
            try
            {
                var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
                if (config == null || string.IsNullOrEmpty(config.RutaCertificado))
                    return false;

                if (!File.Exists(config.RutaCertificado))
                    return false;

                // Intentar cargar el certificado
                var certificado = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                    config.RutaCertificado,
                    config.PasswordCertificado);

                return certificado.HasPrivateKey && certificado.NotAfter > DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar certificado");
                return false;
            }
        }

        /// <summary>
        /// Verifica si los servicios de la DGII están disponibles
        /// </summary>
        public async Task<bool> VerificarServiciosDGIIAsync()
        {
            try
            {
                var resultado = await _dgiiService.VerificarEstatusServiciosAsync();
                return resultado.Success && resultado.Data?.AutenticacionDisponible == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar servicios DGII");
                return false;
            }
        }

        #region Métodos Privados

        /// <summary>
        /// Firma el XML con el certificado digital
        /// </summary>
        private async Task<string?> FirmarXMLAsync(string xmlContent, ConfiguracionEmpresa config)
        {
            try
            {
                if (string.IsNullOrEmpty(config.RutaCertificado) || !File.Exists(config.RutaCertificado))
                    return null;

                // Usar el servicio existente para firmar
                var xmlDoc = System.Xml.Linq.XDocument.Parse(xmlContent);

                return await _xmlService.FirmarXMLAsync(
                    xmlDoc,
                    config.RutaCertificado,
                    config.PasswordCertificado ?? "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al firmar XML");
                return null;
            }
        }

        /// <summary>
        /// Extrae el TrackId del mensaje de respuesta
        /// </summary>
        private string? ExtraerTrackId(string? mensaje)
        {
            if (string.IsNullOrEmpty(mensaje))
                return null;

            // Formato: "TrackId: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
            var match = System.Text.RegularExpressions.Regex.Match(mensaje, @"TrackId:\s*([\w-]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        #endregion
    }

    /// <summary>
    /// Resultado de una operación de envío
    /// </summary>
    public class ResultadoEnvio
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string? ENCF { get; set; }
        public string? TrackId { get; set; }
        public string? ENCFAsignado { get; set; }

        public static ResultadoEnvio CrearExitoso(string mensaje, string encf, string? trackId = null, string? encfAsignado = null)
        {
            return new ResultadoEnvio
            {
                Exitoso = true,
                Mensaje = mensaje,
                ENCF = encf,
                TrackId = trackId,
                ENCFAsignado = encfAsignado
            };
        }

        public static ResultadoEnvio Fallido(string mensaje)
        {
            return new ResultadoEnvio
            {
                Exitoso = false,
                Mensaje = mensaje
            };
        }
    }
}
