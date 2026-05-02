using System.Text.RegularExpressions;

namespace Facturapro.Services.Intelligence
{
    public interface IOcrService
    {
        Task<OcrResult> ProcessInvoiceAsync(Stream imageStream);
    }

    public class OcrResult
    {
        public string? RNC { get; set; }
        public string? NCF { get; set; }
        public decimal? Total { get; set; }
        public decimal? ITBIS { get; set; }
        public DateTime? Fecha { get; set; }
        public string? RawText { get; set; }
        public bool Success { get; set; }
    }

    public class OcrService : IOcrService
    {
        private readonly ILogger<OcrService> _logger;

        public OcrService(ILogger<OcrService> logger)
        {
            _logger = logger;
        }

        public async Task<OcrResult> ProcessInvoiceAsync(Stream imageStream)
        {
            // En una implementación real, aquí llamaríamos a Azure Computer Vision, Google Vision AI o Tesseract.
            // Para este prototipo, simularemos la extracción basada en un "escaneo" exitoso.
            
            _logger.LogInformation("Procesando imagen con OCR...");
            
            // Simulamos un delay de procesamiento
            await Task.Delay(1500);

            // Mock de datos extraídos (en una app real esto vendría del motor de OCR)
            // Aquí podríamos integrar Tesseract.NET si estuviera disponible el paquete.
            
            return new OcrResult
            {
                Success = true,
                RNC = "131062019", // Ejemplo RNC Banco Popular
                NCF = "B0100000001",
                Total = 1180.00m,
                ITBIS = 180.00m,
                Fecha = DateTime.Now,
                RawText = "FACTURA DE CRÉDITO FISCAL\nRNC: 131062019\nNCF: B0100000001\nFECHA: 01/05/2026\nSUBTOTAL: 1,000.00\nITBIS: 180.00\nTOTAL: 1,180.00"
            };
        }

        // Método de utilidad para parsear texto si ya tenemos el OCR crudo
        public OcrResult ParseText(string text)
        {
            var result = new OcrResult { RawText = text, Success = true };

            // Buscar RNC (9 o 11 dígitos)
            var rncMatch = Regex.Match(text, @"RNC[:\s]+(\d{9,11})", RegexOptions.IgnoreCase);
            if (rncMatch.Success) result.RNC = rncMatch.Groups[1].Value;

            // Buscar NCF (B o E seguido de 10 dígitos)
            var ncfMatch = Regex.Match(text, @"(B|E)\d{10}", RegexOptions.IgnoreCase);
            if (ncfMatch.Success) result.NCF = ncfMatch.Value;

            // Buscar Montos
            var totalMatch = Regex.Match(text, @"TOTAL[:\s]+([\d,.]+)", RegexOptions.IgnoreCase);
            if (totalMatch.Success)
            {
                if (decimal.TryParse(totalMatch.Groups[1].Value.Replace(",", ""), out decimal total))
                    result.Total = total;
            }

            return result;
        }
    }
}
