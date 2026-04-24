using Facturapro.Models.DGII;
using Facturapro.Models.Entities;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Facturapro.Services.DGII
{
    /// <summary>
    /// Servicio para integración con la API de la DGII - Facturación Electrónica
    /// </summary>
    public interface IDGIIService
    {
        Task<ApiResult<SemillaResponse>> ObtenerSemillaAsync();
        Task<ApiResult<TokenResponse>> ObtenerTokenAsync();
        Task<ApiResult<EnvioComprobanteResponse>> EnviarComprobanteAsync(Factura factura, string xmlFirmado);
        Task<ApiResult<EnvioFCResponse>> EnviarFacturaConsumoAsync(Factura factura, string xmlFirmado);
        Task<ApiResult<EstadoComprobanteResponse>> ConsultarEstadoAsync(string trackId);
        Task<ApiResult<TimbreFiscalResponse>> ObtenerTimbreAsync(string rncEmisor, string rncComprador, string encf, DateTime fechaEmision, decimal montoTotal);
        Task<ApiResult<EstatusServiciosResponse>> VerificarEstatusServiciosAsync();
        Task<ApiResult<List<ContribuyenteDGII>>> ObtenerDirectorioAsync();
        Task<ApiResult<ContribuyenteDGII?>> BuscarContribuyentePorRNCAsync(string rnc);
    }

    public class DGIIService : IDGIIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DGIIService> _logger;
        private readonly ConfiguracionAPIDGII _config;

        public DGIIService(
            IHttpClientFactory httpClientFactory,
            ILogger<DGIIService> logger,
            ConfiguracionAPIDGII configuracion)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = configuracion;
        }

        #region Autenticación

        /// <summary>
        /// Obtiene la semilla de autenticación de la DGII
        /// </summary>
        public async Task<ApiResult<SemillaResponse>> ObtenerSemillaAsync()
        {
            try
            {
                var url = $"{_config.GetUrlBase()}autenticacion/api/autenticacion/semilla";
                _logger.LogInformation("Solicitando semilla a DGII: {Url}", url);

                using var httpClient = _httpClientFactory.CreateClient("DGII");
                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error al obtener semilla. Status: {Status}, Response: {Response}",
                        response.StatusCode, content);
                    return ApiResult<SemillaResponse>.Failure($"Error {response.StatusCode}: {content}");
                }

                // Parsear XML de respuesta
                var doc = XDocument.Parse(content);
                var semillaValue = doc.Descendants("Semilla").FirstOrDefault()?.Value;
                var fechaGen = doc.Descendants("FechaGeneracion").FirstOrDefault()?.Value;

                if (string.IsNullOrEmpty(semillaValue))
                {
                    return ApiResult<SemillaResponse>.Failure("No se pudo extraer la semilla del XML");
                }

                var semilla = new SemillaResponse
                {
                    Valor = semillaValue,
                    FechaGeneracion = DateTime.TryParse(fechaGen, out var fecha) ? fecha : DateTime.Now,
                    XmlContent = content
                };

                _logger.LogInformation("Semilla obtenida exitosamente. Fecha: {Fecha}", semilla.FechaGeneracion);
                return ApiResult<SemillaResponse>.CreateSuccess(semilla);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener semilla de autenticación");
                return ApiResult<SemillaResponse>.Failure($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene el token JWT firmando la semilla con el certificado
        /// </summary>
        public async Task<ApiResult<TokenResponse>> ObtenerTokenAsync()
        {
            try
            {
                // 1. Obtener semilla
                var semillaResult = await ObtenerSemillaAsync();
                if (!semillaResult.Success)
                    return ApiResult<TokenResponse>.Failure(semillaResult.Message);

                // 2. Firmar semilla con certificado
                var semillaFirmada = FirmarSemilla(semillaResult.Data!.XmlContent);
                if (string.IsNullOrEmpty(semillaFirmada))
                    return ApiResult<TokenResponse>.Failure("No se pudo firmar la semilla");

                // 3. Enviar semilla firmada
                var url = $"{_config.GetUrlBase()}autenticacion/api/autenticacion/validarsemilla";
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("xml", semillaFirmada)
                });

                _logger.LogInformation("Enviando semilla firmada para obtener token...");

                using var httpClient = _httpClientFactory.CreateClient("DGII");
                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error al validar semilla. Status: {Status}, Response: {Response}",
                        response.StatusCode, responseContent);
                    return ApiResult<TokenResponse>.Failure($"Error {response.StatusCode}: {responseContent}");
                }

                // 4. Parsear respuesta JSON
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
                {
                    return ApiResult<TokenResponse>.Failure("No se pudo obtener el token");
                }

                _logger.LogInformation("Token obtenido exitosamente. Expira: {Expira}", tokenResponse.Expira);
                return ApiResult<TokenResponse>.CreateSuccess(tokenResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener token");
                return ApiResult<TokenResponse>.Failure($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Firma el XML de semilla con el certificado P12
        /// </summary>
        private string FirmarSemilla(string xmlSemilla)
        {
            try
            {
                // Cargar certificado
                var certificado = new X509Certificate2(
                    _config.RutaCertificadoP12,
                    _config.PasswordCertificado,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

                // Crear documento XML
                var doc = new XmlDocument();
                doc.LoadXml(xmlSemilla);

                // Firmar el documento
                var signedXml = new SignedXml(doc)
                {
                    SigningKey = certificado.GetRSAPrivateKey()
                };

                // Crear referencia
                var reference = new Reference
                {
                    Uri = "",
                    DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256"
                };

                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signedXml.AddReference(reference);

                // Agregar información del certificado
                var keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(certificado));
                signedXml.KeyInfo = keyInfo;

                // Firmar
                signedXml.ComputeSignature();

                // Insertar firma en el documento
                var xmlSignature = signedXml.GetXml();
                doc.DocumentElement?.AppendChild(doc.ImportNode(xmlSignature, true));

                return doc.OuterXml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al firmar semilla con certificado");
                return string.Empty;
            }
        }

        #endregion

        #region Envío de Comprobantes

        /// <summary>
        /// Envía un comprobante electrónico a la DGII
        /// </summary>
        public async Task<ApiResult<EnvioComprobanteResponse>> EnviarComprobanteAsync(Factura factura, string xmlFirmado)
        {
            try
            {
                // 1. Obtener token
                var tokenResult = await ObtenerTokenAsync();
                if (!tokenResult.Success)
                    return ApiResult<EnvioComprobanteResponse>.Failure($"Error de autenticación: {tokenResult.Message}");

                // 2. Configurar request
                var url = $"{_config.GetUrlBase()}recepcion/api/facturaselectronicas";

                using var content = new MultipartFormDataContent();
                var xmlContent = new StringContent(xmlFirmado);
                xmlContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                content.Add(xmlContent, "xml", $"{_config.RncEmisor}{factura.eNCF}.xml");

                _logger.LogInformation("Enviando comprobante {eNCF} a DGII...", factura.eNCF);

                // 3. Enviar
                using var httpClient = _httpClientFactory.CreateClient("DGII");
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenResult.Data!.Token);

                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error al enviar comprobante. Status: {Status}, Response: {Response}",
                        response.StatusCode, responseContent);
                    return ApiResult<EnvioComprobanteResponse>.Failure($"Error {response.StatusCode}: {responseContent}");
                }

                // 4. Parsear respuesta
                var resultado = JsonSerializer.Deserialize<EnvioComprobanteResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (resultado == null)
                {
                    return ApiResult<EnvioComprobanteResponse>.Failure("No se pudo parsear la respuesta");
                }

                if (!resultado.Exitoso)
                {
                    _logger.LogWarning("Comprobante rechazado: {Error} - {Mensaje}", resultado.Error, resultado.Mensaje);
                    return ApiResult<EnvioComprobanteResponse>.Failure($"{resultado.Error}: {resultado.Mensaje}", resultado);
                }

                _logger.LogInformation("Comprobante enviado exitosamente. TrackId: {TrackId}", resultado.TrackId);
                return ApiResult<EnvioComprobanteResponse>.CreateSuccess(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar comprobante {eNCF}", factura.eNCF);
                return ApiResult<EnvioComprobanteResponse>.Failure($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Envía una factura de consumo (< RD$250,000) a la API de FC
        /// </summary>
        public async Task<ApiResult<EnvioFCResponse>> EnviarFacturaConsumoAsync(Factura factura, string xmlFirmado)
        {
            try
            {
                // 1. Obtener token
                var tokenResult = await ObtenerTokenAsync();
                if (!tokenResult.Success)
                    return ApiResult<EnvioFCResponse>.Failure($"Error de autenticación: {tokenResult.Message}");

                // 2. Configurar request
                var url = $"{_config.GetUrlBaseFC()}recepcionfc/api/recepcion/ecf";

                using var content = new MultipartFormDataContent();
                var xmlContent = new StringContent(xmlFirmado);
                xmlContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                content.Add(xmlContent, "xml", $"{_config.RncEmisor}{factura.eNCF}.xml");

                _logger.LogInformation("Enviando factura de consumo {eNCF} a DGII...", factura.eNCF);

                // 3. Enviar
                using var httpClient = _httpClientFactory.CreateClient("DGII");
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenResult.Data!.Token);

                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error al enviar FC. Status: {Status}, Response: {Response}",
                        response.StatusCode, responseContent);
                    return ApiResult<EnvioFCResponse>.Failure($"Error {response.StatusCode}: {responseContent}");
                }

                // 4. Parsear respuesta
                var resultado = JsonSerializer.Deserialize<EnvioFCResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (resultado == null)
                {
                    return ApiResult<EnvioFCResponse>.Failure("No se pudo parsear la respuesta");
                }

                if (!resultado.Exitoso)
                {
                    var mensajes = string.Join(", ", resultado.Mensajes.Select(m => $"{m.Codigo}: {m.Valor}"));
                    _logger.LogWarning("FC rechazada: {Mensajes}", mensajes);
                    return ApiResult<EnvioFCResponse>.Failure(mensajes, resultado);
                }

                _logger.LogInformation("Factura de consumo enviada exitosamente. eNCF: {eNCF}", resultado.ENCF);
                return ApiResult<EnvioFCResponse>.CreateSuccess(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar factura de consumo {eNCF}", factura.eNCF);
                return ApiResult<EnvioFCResponse>.Failure($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Consultas

        /// <summary>
        /// Consulta el estado de un comprobante enviado
        /// </summary>
        public async Task<ApiResult<EstadoComprobanteResponse>> ConsultarEstadoAsync(string trackId)
        {
            try
            {
                // 1. Obtener token
                var tokenResult = await ObtenerTokenAsync();
                if (!tokenResult.Success)
                    return ApiResult<EstadoComprobanteResponse>.Failure($"Error de autenticación: {tokenResult.Message}");

                // 2. Configurar request
                var url = $"{_config.GetUrlBase()}consultaresultado/api/consultas/estado?trackid={Uri.EscapeDataString(trackId)}";

                _logger.LogInformation("Consultando estado para TrackId: {TrackId}", trackId);

                // 3. Consultar
                using var httpClient = _httpClientFactory.CreateClient("DGII");
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenResult.Data!.Token);

                var response = await httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<EstadoComprobanteResponse>.Failure($"Error {response.StatusCode}: {responseContent}");
                }

                // 4. Parsear respuesta
                var estado = JsonSerializer.Deserialize<EstadoComprobanteResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (estado == null)
                {
                    return ApiResult<EstadoComprobanteResponse>.Failure("No se pudo parsear la respuesta");
                }

                estado.TrackId = trackId;
                estado.FechaConsulta = DateTime.Now;

                _logger.LogInformation("Estado consultado: {Estado}", estado.Estado);
                return ApiResult<EstadoComprobanteResponse>.CreateSuccess(estado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar estado para TrackId {TrackId}", trackId);
                return ApiResult<EstadoComprobanteResponse>.Failure($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene el timbre fiscal (QR) de un comprobante
        /// </summary>
        public async Task<ApiResult<TimbreFiscalResponse>> ObtenerTimbreAsync(
            string rncEmisor, string rncComprador, string encf,
            DateTime fechaEmision, decimal montoTotal)
        {
            try
            {
                // El código de seguridad se calcula o se obtiene de la factura
                var codigoSeguridad = GenerarCodigoSeguridad(encf, fechaEmision, montoTotal);

                var url = $"{_config.GetUrlBase()}consultatimbre?" +
                    $"rncemisor={Uri.EscapeDataString(rncEmisor)}" +
                    $"&rnccomprador={Uri.EscapeDataString(rncComprador)}" +
                    $"&encf={Uri.EscapeDataString(encf)}" +
                    $"&fechaemision={fechaEmision:dd-MM-yyyy}" +
                    $"&montototal={montoTotal:F2}" +
                    $"&codigoseguridad={Uri.EscapeDataString(codigoSeguridad)}";

                _logger.LogInformation("Consultando timbre para eNCF: {eNCF}", encf);

                using var httpClient = _httpClientFactory.CreateClient("DGII");
                var response = await httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TimbreFiscalResponse>.Failure($"Error {response.StatusCode}: {responseContent}");
                }

                // El timbre puede venir como imagen o JSON según implementación
                var timbre = new TimbreFiscalResponse
                {
                    RncEmisor = rncEmisor,
                    RncComprador = rncComprador,
                    ENCF = encf,
                    FechaEmision = fechaEmision,
                    MontoTotal = montoTotal,
                    CodigoSeguridad = codigoSeguridad,
                    Validado = true
                };

                return ApiResult<TimbreFiscalResponse>.CreateSuccess(timbre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener timbre para eNCF {eNCF}", encf);
                return ApiResult<TimbreFiscalResponse>.Failure($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica el estado de los servicios de la DGII
        /// </summary>
        public async Task<ApiResult<EstatusServiciosResponse>> VerificarEstatusServiciosAsync()
        {
            try
            {
                var url = "https://statusecf.dgii.gov.do/api/estatusservicios/obtenerestatus";

                using var httpClient = _httpClientFactory.CreateClient("DGII");
                var response = await httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<EstatusServiciosResponse>.Failure($"Error {response.StatusCode}");
                }

                // Parsear respuesta (estructura puede variar)
                var estatus = new EstatusServiciosResponse
                {
                    AutenticacionDisponible = responseContent.Contains("true"),
                    RecepcionDisponible = true,
                    ConsultasDisponible = true,
                    EnMantenimiento = responseContent.Contains("mantenimiento", StringComparison.OrdinalIgnoreCase)
                };

                return ApiResult<EstatusServiciosResponse>.CreateSuccess(estatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar estatus de servicios");
                return ApiResult<EstatusServiciosResponse>.CreateSuccess(new EstatusServiciosResponse
                {
                    AutenticacionDisponible = false,
                    RecepcionDisponible = false,
                    ConsultasDisponible = false,
                    EnMantenimiento = true
                });
            }
        }

        /// <summary>
        /// Obtiene el directorio de contribuyentes electrónicos
        /// </summary>
        public async Task<ApiResult<List<ContribuyenteDGII>>> ObtenerDirectorioAsync()
        {
            try
            {
                var tokenResult = await ObtenerTokenAsync();
                if (!tokenResult.Success)
                    return ApiResult<List<ContribuyenteDGII>>.Failure($"Error de autenticación: {tokenResult.Message}");

                var url = $"{_config.GetUrlBase()}consultadirectorio/api/consultas/listado";

                using var httpClient = _httpClientFactory.CreateClient("DGII");
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenResult.Data!.Token);

                var response = await httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<List<ContribuyenteDGII>>.Failure($"Error {response.StatusCode}: {responseContent}");
                }

                // Parsear respuesta (puede venir en diferentes formatos)
                var contribuyentes = new List<ContribuyenteDGII>();

                return ApiResult<List<ContribuyenteDGII>>.CreateSuccess(contribuyentes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener directorio");
                return ApiResult<List<ContribuyenteDGII>>.Failure($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Busca un contribuyente por RNC en el directorio DGII
        /// </summary>
        public async Task<ApiResult<ContribuyenteDGII?>> BuscarContribuyentePorRNCAsync(string rnc)
        {
            try
            {
                var tokenResult = await ObtenerTokenAsync();
                if (!tokenResult.Success)
                    return ApiResult<ContribuyenteDGII?>.Failure($"Error de autenticación: {tokenResult.Message}");

                var url = $"{_config.GetUrlBase()}consultadirectorio/api/consultas/obtenerdirectorioporrnc?rnc={Uri.EscapeDataString(rnc)}";

                using var httpClient = _httpClientFactory.CreateClient("DGII");
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenResult.Data!.Token);

                var response = await httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return ApiResult<ContribuyenteDGII?>.CreateSuccess(null);
                }

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<ContribuyenteDGII?>.Failure($"Error {response.StatusCode}: {responseContent}");
                }

                var contribuyente = new ContribuyenteDGII
                {
                    RNC = rnc,
                    EmiteECF = responseContent.Contains("true", StringComparison.OrdinalIgnoreCase)
                };

                return ApiResult<ContribuyenteDGII?>.CreateSuccess(contribuyente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar contribuyente {RNC}", rnc);
                return ApiResult<ContribuyenteDGII?>.Failure($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Utilidades

        /// <summary>
        /// Genera el código de seguridad para el timbre
        /// </summary>
        private string GenerarCodigoSeguridad(string encf, DateTime fechaEmision, decimal montoTotal)
        {
            // Formato: Hash de eNCF + Fecha + Monto
            var data = $"{encf}|{fechaEmision:yyyyMMdd}|{montoTotal:F2}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToHexString(hash).Substring(0, 16);
        }

        #endregion
    }

    /// <summary>
    /// Resultado estandarizado de operaciones API
    /// </summary>
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResult<T> CreateSuccess(T data)
        {
            return new ApiResult<T> { Success = true, Data = data };
        }

        public static ApiResult<T> Failure(string message, T? data = default)
        {
            return new ApiResult<T> { Success = false, Message = message, Data = data };
        }
    }
}
