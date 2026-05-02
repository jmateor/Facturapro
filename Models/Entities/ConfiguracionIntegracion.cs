using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class ConfiguracionIntegracion
    {
        public int Id { get; set; }

        // Correo Electrónico (SMTP) - Habilitado
        [Display(Name = "Habilitar Envío por Correo")]
        public bool EmailHabilitado { get; set; } = true;

        [StringLength(100)]
        public string? SmtpServer { get; set; }

        public int SmtpPort { get; set; } = 587;

        [StringLength(100)]
        public string? SmtpUser { get; set; }

        [DataType(DataType.Password)]
        public string? SmtpPassword { get; set; }

        public bool SmtpUseSSL { get; set; } = true;

        // WhatsApp - Habilitado (Base)
        [Display(Name = "Habilitar Notificaciones WhatsApp")]
        public bool WhatsAppHabilitado { get; set; } = false;

        [StringLength(200)]
        public string? WhatsAppApiKey { get; set; }

        [StringLength(100)]
        public string? WhatsAppPhoneId { get; set; }

        // DGII RNC Validator - Habilitado
        [Display(Name = "Validación RNC en Tiempo Real")]
        public bool DgiiValidacionHabilitada { get; set; } = true;

        // Google Drive - Próximamente
        public bool GoogleDriveHabilitado { get; set; } = false;

        // Pasarelas de Pago - Próximamente
        public bool PasarelaPagoHabilitada { get; set; } = false;
        
        public string? PasarelaProveedor { get; set; } // Azul, CardNet

        // Configuración Moneda
        [Display(Name = "Tasa de Cambio USD (vs DOP)")]
        [Range(1, 200)]
        public decimal TasaUSD { get; set; } = 58.50m; // Valor por defecto actual aproximado

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}
