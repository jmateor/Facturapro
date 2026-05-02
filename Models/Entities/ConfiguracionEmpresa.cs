using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    /// <summary>
    /// Configuración de la empresa emisora de e-CF
    /// </summary>
    public class ConfiguracionEmpresa
    {
        public int Id { get; set; }

        // Datos del Emisor (Obligatorios para e-CF)
        [Required]
        [StringLength(11)]
        [Display(Name = "RNC Emisor")]
        public string RNCEmisor { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Razón Social")]
        public string RazonSocialEmisor { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Dirección")]
        public string DireccionEmisor { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Municipio")]
        public string? Municipio { get; set; }

        [StringLength(50)]
        [Display(Name = "Provincia")]
        public string? Provincia { get; set; }

        [StringLength(50)]
        [Display(Name = "Teléfono")]
        public string? TelefonoEmisor { get; set; }

        [StringLength(150)]
        [EmailAddress]
        [Display(Name = "Correo Electrónico")]
        public string? CorreoEmisor { get; set; }

        [StringLength(50)]
        [Display(Name = "Nombre Comercial")]
        public string? NombreComercial { get; set; }

        [StringLength(50)]
        [Display(Name = "Actividad Económica")]
        public string? ActividadEconomica { get; set; }

        // Configuración de Certificado Digital
        [StringLength(500)]
        [Display(Name = "Ruta Certificado PFX")]
        public string? RutaCertificado { get; set; }

        [Display(Name = "Contraseña Certificado")]
        public string? PasswordCertificado { get; set; }

        [Display(Name = "Certificado Válido Hasta")]
        public DateTime? FechaVencimientoCertificado { get; set; }

        [Display(Name = "Huella Digital Certificado")]
        public string? HuellaCertificado { get; set; }

        // Configuración de API DGII (si se usa proveedor)
        [StringLength(500)]
        [Display(Name = "URL API Producción")]
        public string? UrlAPIProduccion { get; set; }

        [StringLength(500)]
        [Display(Name = "URL API Pruebas")]
        public string? UrlAPIPruebas { get; set; }

        [StringLength(500)]
        [Display(Name = "API Key")]
        public string? APIKey { get; set; }

        [StringLength(500)]
        [Display(Name = "API Secret")]
        public string? APISecret { get; set; }

        [Display(Name = "Modo Pruebas")]
        public bool ModoPruebas { get; set; } = true;

        // Configuración de Funcionalidades
        [Display(Name = "Usar Multimoneda")]
        public bool Multimoneda { get; set; } = false;

        [Display(Name = "Usar Caja")]
        public bool UsarCaja { get; set; } = true;

        [StringLength(100)]
        [Display(Name = "Cuenta Ingresos Efectivo")]
        public string? CuentaIngresosEfectivo { get; set; }

        [StringLength(100)]
        [Display(Name = "Cuenta Salidas Efectivo")]
        public string? CuentaSalidasEfectivo { get; set; }

        // Configuración de Impuestos por Defecto
        [Display(Name = "ITBIS por Defecto")]
        [Range(0, 100)]
        public decimal ITBISPorDefecto { get; set; } = 18;

        [Display(Name = "Tipo Ingresos por Defecto")]
        public string TipoIngresosPorDefecto { get; set; } = "01"; // 01=Ingresos por operaciones

        // Configuración de Impresión
        [StringLength(20)]
        [Display(Name = "Tipo de Ticket")]
        public string TipoTicket { get; set; } = "thermal"; // thermal, half, letter

        [Display(Name = "Mostrar Logotipo")]
        public bool MostrarLogo { get; set; } = true;

        [Display(Name = "Mostrar Descripción")]
        public bool MostrarDescripcion { get; set; } = true;

        [Display(Name = "Mostrar Código de Barras")]
        public bool MostrarCodigoBarras { get; set; } = true;

        [Display(Name = "Mostrar NCF")]
        public bool MostrarNCF { get; set; } = false;

        [Display(Name = "Mostrar Impuestos")]
        public bool MostrarImpuestos { get; set; } = true;

        [StringLength(200)]
        [Display(Name = "Pie de Página")]
        public string? PiePagina { get; set; }

        [StringLength(20)]
        [Display(Name = "Tamaño Fuente Pie")]
        public string TamanoFuentePie { get; set; } = "medium"; // small, medium, large

        // Configuración de Ventas
        [Display(Name = "Permitir Ventas a Crédito")]
        public bool PermitirVentasCredito { get; set; } = true;

        [Display(Name = "Imprimir Automáticamente")]
        public bool ImprimirAutomático { get; set; } = true;

        [Display(Name = "Cliente Obligatorio")]
        public bool ClienteObligatorio { get; set; } = false;

        [Display(Name = "Descuento por Cantidad")]
        public bool DescuentoCantidad { get; set; } = true;

        [Display(Name = "Permitir Stock Negativo")]
        public bool StockNegativo { get; set; } = false;

        [Display(Name = "Usar Valores por Defecto")]
        public bool UsarValoresDefecto { get; set; } = true;

        [Display(Name = "Decimales en Precios")]
        public int DecimalesPrecios { get; set; } = 2;

        [StringLength(10)]
        [Display(Name = "Redondeo de Totales")]
        public string? RedondeoTotales { get; set; } = "none";

        [StringLength(3)]
        [Display(Name = "Moneda")]
        public string Moneda { get; set; } = "DOP";

        [StringLength(10)]
        [Display(Name = "Símbolo de Moneda")]
        public string SimboloMoneda { get; set; } = "prefix";

        [StringLength(3)]
        [Display(Name = "Tipo de Comprobante por Defecto")]
        public string TipoComprobanteDefecto { get; set; } = "E31";

        [Display(Name = "Tipo de Ingreso por Defecto")]
        public string TipoIngresoPorDefecto { get; set; } = "01";

        [Display(Name = "Tipo de Pago por Defecto")]
        public int TipoPagoPorDefecto { get; set; } = 1; // 1=Contado, 2=Crédito, 3=Mixto

        [Display(Name = "Monto Mínimo de Venta")]
        [Range(0, double.MaxValue)]
        public decimal MontoVentaMinima { get; set; } = 0;

        [Display(Name = "Monto Máximo de Venta")]
        [Range(0, double.MaxValue)]
        public decimal MontoVentaMaxima { get; set; } = 0;

        [Display(Name = "Monto Máximo de Crédito")]
        [Range(0, double.MaxValue)]
        public decimal MontoCreditoMaximo { get; set; } = 0;

        [Display(Name = "Días de Plazo de Crédito")]
        [Range(1, 365)]
        public int DiasPlazoCredito { get; set; } = 30;

        // Configuración de Numeración
        [Display(Name = "Vencimiento Rangos (días)")]
        public int DiasVencimientoRango { get; set; } = 365;

        // Configuración de Alertas y Notificaciones
        [Display(Name = "Alerta de Stock Bajo (%)")]
        [Range(1, 100)]
        public int PorcentajeAlertaAgotamiento { get; set; } = 80;

        [Display(Name = "Alerta Stock Mínimo (unidades)")]
        [Range(1, 1000)]
        public int StockMinimoAlerta { get; set; } = 5;

        [Display(Name = "Alerta Productos Vencidos (días)")]
        [Range(1, 365)]
        public int DiasAlertaVencimiento { get; set; } = 30;

        [Display(Name = "Alerta Crédito Máximo")]
        public bool AlertaCreditoMaximo { get; set; } = true;

        [Display(Name = "Alerta Venta Máxima")]
        public bool AlertaVentaMaxima { get; set; } = true;

        [Display(Name = "Notificar Sonido")]
        public bool NotificacionSonido { get; set; } = true;

        [Display(Name = "Notificar Popup")]
        public bool NotificacionPopup { get; set; } = true;

        [Display(Name = "Notificar Email")]
        public bool NotificacionEmail { get; set; } = false;

        // Configuración de Métodos de Pago
        [Display(Name = "Aceptar Efectivo")]
        public bool AceptarEfectivo { get; set; } = true;

        [Display(Name = "Aceptar Tarjeta de Crédito/Débito")]
        public bool AceptarTarjeta { get; set; } = true;

        [Display(Name = "Aceptar Transferencia")]
        public bool AceptarTransferencia { get; set; } = true;

        [Display(Name = "Aceptar Sinpe Móvil")]
        public bool AceptarSinpe { get; set; } = false;

        [Display(Name = "Aceptar Crédito")]
        public bool AceptarCredito { get; set; } = true;

        [Display(Name = "Aceptar Mixto")]
        public bool AceptarMixto { get; set; } = true;

        [Display(Name = "Método de Pago por Defecto")]
        public int MetodoPagoPorDefecto { get; set; } = 1; // 1=Efectivo, 2=Tarjeta, 3=Transferencia, 4=Sinpe, 5=Crédito, 6=Mixto

        [Display(Name = "Mostrar Opciones de Pago")]
        public bool MostrarOpcionesPago { get; set; } = true;

        [Display(Name = "Permitir Cambio")]
        public bool PermitirCambio { get; set; } = true;

        [Display(Name = "Preguntar por Cambio")]
        public bool PreguntarCambio { get; set; } = false;

        [Display(Name = "Monto Máximo para Cambio")]
        [Range(0, double.MaxValue)]
        public decimal MontoMaximoCambio { get; set; } = 0;

        // Campos de control
        [Display(Name = "Última Actualización")]
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        public bool EsValidoParaEmitir()
        {
            return !string.IsNullOrEmpty(RNCEmisor) &&
                   !string.IsNullOrEmpty(RazonSocialEmisor) &&
                   RNCEmisor.Length >= 9 &&
                   RNCEmisor.Length <= 11;
        }
    }
}
