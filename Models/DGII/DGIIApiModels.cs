using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.DGII
{
    /// <summary>
    /// Respuesta de la API al obtener semilla de autenticación
    /// </summary>
    public class SemillaResponse
    {
        public string Valor { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
        public string XmlContent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta de la API al validar semilla (obtener token)
    /// </summary>
    public class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expedido { get; set; }
        public DateTime Expira { get; set; }
        public bool Exitoso => !string.IsNullOrEmpty(Token);
    }

    /// <summary>
    /// Respuesta de la API al enviar un comprobante electrónico
    /// </summary>
    public class EnvioComprobanteResponse
    {
        public string TrackId { get; set; } = string.Empty;
        public string? Error { get; set; }
        public string? Mensaje { get; set; }
        public bool Exitoso => !string.IsNullOrEmpty(TrackId) && string.IsNullOrEmpty(Error);
    }

    /// <summary>
    /// Respuesta de la API de Factura de Consumo (FC < RD$250,000)
    /// </summary>
    public class EnvioFCResponse
    {
        public int Codigo { get; set; }
        public string Estado { get; set; } = string.Empty;
        public List<MensajeDGII> Mensajes { get; set; } = new();
        public string? ENCF { get; set; }
        public bool SecuenciaUtilizada { get; set; }
        public bool Exitoso => Codigo == 1 && Estado == "Aceptado";
    }

    public class MensajeDGII
    {
        public string Codigo { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }

    /// <summary>
    /// Estado de validación de un comprobante enviado
    /// </summary>
    public class EstadoComprobanteResponse
    {
        public string TrackId { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty; // Aceptado, Rechazado, EnProceso
        public List<MensajeDGII> Mensajes { get; set; } = new();
        public DateTime? FechaConsulta { get; set; }
    }

    /// <summary>
    /// Información del timbre fiscal (QR)
    /// </summary>
    public class TimbreFiscalResponse
    {
        public string RncEmisor { get; set; } = string.Empty;
        public string RncComprador { get; set; } = string.Empty;
        public string ENCF { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public decimal MontoTotal { get; set; }
        public DateTime FechaFirma { get; set; }
        public string CodigoSeguridad { get; set; } = string.Empty;
        public string? ImagenQR { get; set; }
        public bool Validado { get; set; }
    }

    /// <summary>
    /// Estado de los servicios de la DGII
    /// </summary>
    public class EstatusServiciosResponse
    {
        public bool AutenticacionDisponible { get; set; }
        public bool RecepcionDisponible { get; set; }
        public bool ConsultasDisponible { get; set; }
        public bool EnMantenimiento { get; set; }
        public DateTime? InicioMantenimiento { get; set; }
        public DateTime? FinMantenimiento { get; set; }
    }

    /// <summary>
    /// Información de un contribuyente en el directorio DGII
    /// </summary>
    public class ContribuyenteDGII
    {
        public string RNC { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string? NombreComercial { get; set; }
        public string? ActividadEconomica { get; set; }
        public bool EmiteECF { get; set; }
        public DateTime? FechaRegistro { get; set; }
    }

    /// <summary>
    /// Tipos de ambiente para la API DGII
    /// </summary>
    public enum AmbienteDGII
    {
        Test = 1,           // TesteCF - Pruebas iniciales
        Certificacion = 2,  // CerteCF - Certificación formal
        Produccion = 3      // eCF - Producción real
    }

    /// <summary>
    /// Estados posibles de un comprobante ante la DGII
    /// </summary>
    public enum EstadoComprobanteDGII
    {
        Pendiente,
        Enviado,
        EnProceso,
        Aceptado,
        Rechazado,
        Anulado
    }

    /// <summary>
    /// Configuración de la API DGII
    /// </summary>
    public class ConfiguracionAPIDGII
    {
        [Required]
        public AmbienteDGII Ambiente { get; set; } = AmbienteDGII.Test;

        [Required]
        [StringLength(11)]
        public string RncEmisor { get; set; } = string.Empty;

        [Required]
        public string RutaCertificadoP12 { get; set; } = string.Empty;

        [Required]
        public string PasswordCertificado { get; set; } = string.Empty;

        /// <summary>
        /// URL base según el ambiente
        /// </summary>
        public string GetUrlBase()
        {
            return Ambiente switch
            {
                AmbienteDGII.Test => "https://ecf.dgii.gov.do/testecf/",
                AmbienteDGII.Certificacion => "https://ecf.dgii.gov.do/certecf/",
                AmbienteDGII.Produccion => "https://ecf.dgii.gov.do/ecf/",
                _ => "https://ecf.dgii.gov.do/testecf/"
            };
        }

        /// <summary>
        /// URL base para Facturas de Consumo
        /// </summary>
        public string GetUrlBaseFC()
        {
            return Ambiente switch
            {
                AmbienteDGII.Test => "https://fc.dgii.gov.do/testecf/",
                AmbienteDGII.Certificacion => "https://fc.dgii.gov.do/certecf/",
                AmbienteDGII.Produccion => "https://fc.dgii.gov.do/ecf/",
                _ => "https://fc.dgii.gov.do/testecf/"
            };
        }
    }
}
