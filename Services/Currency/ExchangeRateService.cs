using System.Text.RegularExpressions;
using Facturapro.Data;
using Microsoft.EntityFrameworkCore;

namespace Facturapro.Services.Currency
{
    public interface IExchangeRateService
    {
        Task<decimal> GetLatestRateAsync();
        Task<bool> UpdateSystemRateAsync();
    }

    public class ExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExchangeRateService> _logger;

        public ExchangeRateService(HttpClient httpClient, ApplicationDbContext context, ILogger<ExchangeRateService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
            
            // Banco Central requires a User-Agent or it might block
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public async Task<decimal> GetLatestRateAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://www.bancentral.gov.do/");
                
                // Regex para buscar el valor de Venta del Dólar en el HTML del Banco Central
                // El formato suele ser algo como <span id="lblVentaDolar">59.1000</span> 
                // o dentro de una tabla de indicadores.
                
                // Intentamos varios patrones comunes en su sitio
                var patterns = new[] 
                {
                    @"venta.*?(\d{2}\.\d{2,4})", // Genérico: venta seguido de número
                    @"lblVentaUSD.*?(\d{2}\.\d{2,4})",
                    @">(\d{2}\.\d{4})<" // Números con 4 decimales entre tags
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(response, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && decimal.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal rate))
                    {
                        if (rate > 40 && rate < 80) // Validación de rango razonable para RD
                        {
                            return rate;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tasa de cambio del Banco Central");
            }

            // Fallback a una tasa conservadora si falla el scraping
            return 59.00m; 
        }

        public async Task<bool> UpdateSystemRateAsync()
        {
            try
            {
                var newRate = await GetLatestRateAsync();
                var config = await _context.ConfiguracionIntegraciones.FirstOrDefaultAsync();
                
                if (config == null)
                {
                    config = new Models.Entities.ConfiguracionIntegracion();
                    _context.ConfiguracionIntegraciones.Add(config);
                }

                config.TasaUSD = newRate;
                config.FechaActualizacion = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar tasa en el sistema");
                return false;
            }
        }
    }
}
