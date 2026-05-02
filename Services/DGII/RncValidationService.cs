using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Facturapro.Services.DGII
{
    public interface IRncValidationService
    {
        bool ValidarFormatoRnc(string rnc);
        bool ValidarFormatoCedula(string cedula);
        Task<RncValidationResult> ConsultarDGIIAsync(string documento);
    }

    public class RncValidationResult
    {
        public bool EsValido { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string NombreComercial { get; set; } = string.Empty;
        public string Actividad { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Regimen { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; // RNC o CEDULA
        public string Mensaje { get; set; } = string.Empty;
    }

    public class RncValidationService : IRncValidationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RncValidationService> _logger;

        public RncValidationService(IHttpClientFactory httpClientFactory, ILogger<RncValidationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public bool ValidarFormatoRnc(string rnc)
        {
            if (string.IsNullOrEmpty(rnc)) return false;
            rnc = Regex.Replace(rnc, @"[^\d]", "");
            if (rnc.Length != 9) return false;

            int[] pesos = { 7, 9, 8, 6, 5, 4, 3, 2 };
            int suma = 0;

            for (int i = 0; i < 8; i++)
            {
                suma += (rnc[i] - '0') * pesos[i];
            }

            int residuo = suma % 11;
            int digitoVerificador;

            if (residuo == 0) digitoVerificador = 2;
            else if (residuo == 1) digitoVerificador = 1;
            else digitoVerificador = 11 - residuo;

            return digitoVerificador == (rnc[8] - '0');
        }

        public bool ValidarFormatoCedula(string cedula)
        {
            if (string.IsNullOrEmpty(cedula)) return false;
            cedula = Regex.Replace(cedula, @"[^\d]", "");
            if (cedula.Length != 11) return false;

            int suma = 0;
            int[] pesos = { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 };

            for (int i = 0; i < 10; i++)
            {
                int mult = (cedula[i] - '0') * pesos[i];
                if (mult > 9) mult = (mult / 10) + (mult % 10);
                suma += mult;
            }

            int proximoDiez = (int)Math.Ceiling(suma / 10.0) * 10;
            int digitoVerificador = proximoDiez - suma;

            return digitoVerificador == (cedula[10] - '0');
        }

        public async Task<RncValidationResult> ConsultarDGIIAsync(string documento)
        {
            documento = Regex.Replace(documento, @"[^\d]", "");
            var result = new RncValidationResult { Documento = documento };

            if (documento.Length == 9)
            {
                result.Tipo = "RNC";
                result.EsValido = ValidarFormatoRnc(documento);
            }
            else if (documento.Length == 11)
            {
                result.Tipo = "CEDULA";
                result.EsValido = ValidarFormatoCedula(documento);
            }
            else
            {
                result.Mensaje = "Longitud de documento no válida";
                return result;
            }

            if (!result.EsValido)
            {
                result.Mensaje = "Dígito verificador incorrecto";
                return result;
            }

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                
                // 1. INTENTO VÍA NUEVA API (Dominican Technology) - RECOMENDADA POR EL USUARIO
                var apiUrl = $"https://api-dgii.dominicantechnology.com/api/v1/rnc/{documento}";
                var apiResponse = await client.GetAsync(apiUrl);
                
                if (apiResponse.IsSuccessStatusCode)
                {
                    var content = await apiResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        if (data.TryGetProperty("razon_social", out var razonSocial))
                            result.Nombre = razonSocial.GetString() ?? "";
                        
                        if (data.TryGetProperty("nombre_comercial", out var nombreComercial))
                            result.NombreComercial = nombreComercial.GetString() ?? "";
                        
                        if (data.TryGetProperty("estado", out var estado))
                            result.Estado = estado.GetString() ?? "";

                        if (!string.IsNullOrEmpty(result.Nombre))
                        {
                            result.EsValido = true;
                            result.Mensaje = "Encontrado (API Premium)";
                            return result;
                        }
                    }
                }

                // 2. FALLBACK VÍA HANDLER DGII (Si la API anterior falla)
                var urlHandler = $"https://dgii.gov.do/app/WebApps/ConsultasWeb/Handler/RNC.ashx?rnc={documento}";
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                var handlerResponse = await client.GetStringAsync(urlHandler);
                
                if (!string.IsNullOrEmpty(handlerResponse) && handlerResponse.Contains("|"))
                {
                    var parts = handlerResponse.Split('|');
                    if (parts.Length > 1) result.Nombre = parts[1].Trim();
                    if (parts.Length > 2) result.NombreComercial = parts[2].Trim();
                    if (parts.Length > 5) result.Estado = parts[5].Trim();
                    
                    if (!string.IsNullOrEmpty(result.Nombre))
                    {
                        result.EsValido = true;
                        result.Mensaje = "Encontrado (DGII Directo)";
                        return result;
                    }
                }

                result.Mensaje = "Documento válido pero no se encontraron datos en DGII";
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error en consulta de RNC: {Message}", ex.Message);
                result.Mensaje = "Validado localmente (Sin conexión a DGII)";
            }

            return result;
        }
    }
}
