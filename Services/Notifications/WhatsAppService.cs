using Facturapro.Models.Entities;
using System.Net;

namespace Facturapro.Services.Notifications
{
    public interface IWhatsAppService
    {
        string GenerateWhatsAppLink(Factura factura);
        Task<bool> SendInvoiceNotificationAsync(Factura factura);
    }

    public class WhatsAppService : IWhatsAppService
    {
        private readonly ILogger<WhatsAppService> _logger;
        private readonly Data.ApplicationDbContext _context;

        public WhatsAppService(ILogger<WhatsAppService> logger, Data.ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public string GenerateWhatsAppLink(Factura factura)
        {
            var telefono = factura.Cliente?.Telefono?.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");
            if (string.IsNullOrEmpty(telefono)) return string.Empty;

            // Asegurar código de país (RD +1)
            if (telefono.Length == 10) telefono = "1" + telefono;

            var mensaje = $"Hola *{factura.Cliente?.Nombre}*, gracias por tu compra en *FacturaPro*.\n\n" +
                          $"*Factura:* #{factura.NumeroFactura}\n" +
                          $"*Monto:* {factura.Total.ToString("C", new System.Globalization.CultureInfo("es-DO"))}\n" +
                          $"*Fecha:* {factura.FechaEmision:dd/MM/yyyy}\n\n" +
                          $"Puedes ver tu factura aquí: {GeneratePublicInvoiceUrl(factura)}\n\n" +
                          $"¡Gracias por preferirnos!";

            return $"https://wa.me/{telefono}?text={WebUtility.UrlEncode(mensaje)}";
        }

        public async Task<bool> SendInvoiceNotificationAsync(Factura factura)
        {
            // Aquí se implementaría la lógica de API (Twilio, UltraMsg, etc.)
            // Por ahora, simulamos que si está habilitado en config, se intenta
            var config = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_context.ConfiguracionIntegraciones);
            
            if (config == null || !config.WhatsAppHabilitado || string.IsNullOrEmpty(config.WhatsAppApiKey))
            {
                return false;
            }

            try
            {
                // Lógica de envío automático vía API externa
                _logger.LogInformation($"Enviando notificación automática WhatsApp para factura {factura.NumeroFactura}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando WhatsApp automático");
                return false;
            }
        }

        private string GeneratePublicInvoiceUrl(Factura factura)
        {
            // En una app real, esto sería una URL pública con un token único
            return $"https://facturapro.com/v/{(Guid.NewGuid().ToString().Substring(0, 8))}";
        }
    }
}
