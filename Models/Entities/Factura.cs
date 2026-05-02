using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class Factura
    {
        public int Id { get; set; }

        // Campos originales (para uso interno)
        [Required]
        [StringLength(50)]
        [Display(Name = "Nº Factura Interno")]
        public string NumeroFactura { get; set; } = string.Empty;

        // Campos e-CF DGII
        [StringLength(13)]
        [Display(Name = "e-CF (Número Electrónico)")]
        public string? eNCF { get; set; }

        [StringLength(2)]
        [Display(Name = "Tipo de Comprobante")]
        public string? TipoECF { get; set; } = "31"; // 31=Crédito Fiscal, 32=Consumo, 33=Nota Débito, 34=Nota Crédito

        [StringLength(13)]
        [Display(Name = "NCF Modificado")]
        public string? NCFModificado { get; set; }

        [Display(Name = "Versión del XML")]
        public string? VersionXML { get; set; } = "1.0";

        [Display(Name = "RNC Emisor")]
        [StringLength(11)]
        public string? RNCEmisor { get; set; }

        [Display(Name = "Tipo de Ingresos")]
        public string? TipoIngresos { get; set; } = "01"; // 01=Operaciones, 02=Financieros, etc.

        [Display(Name = "Tipo de Pago")]
        public int TipoPago { get; set; } = 1; // 1=Contado, 2=Crédito, 3=Gratuito

        [StringLength(500)]
        [Display(Name = "Ruta XML Firmado")]
        public string? RutaXMLFirmado { get; set; }

        [Display(Name = "XML Firmado")]
        public string? XMLFirmado { get; set; }

        [StringLength(500)]
        [Display(Name = "Ruta PDF Representación")]
        public string? RutaPDF { get; set; }

        [Display(Name = "Firma Digital")]
        public string? FirmaDigital { get; set; }

        [Display(Name = "Fecha de Firma")]
        public DateTime? FechaHoraFirma { get; set; }

        [Display(Name = "Estado DGII")]
        public string? EstadoDGII { get; set; } = "Pendiente"; // Pendiente, Firmado, Enviado, Aprobado, Rechazado

        [Display(Name = "Mensaje DGII")]
        public string? MensajeDGII { get; set; }

        [Display(Name = "Cliente")]
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        [Display(Name = "Fecha de emisión")]
        [DataType(DataType.Date)]
        public DateTime FechaEmision { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de vencimiento")]
        [DataType(DataType.Date)]
        public DateTime FechaVencimiento { get; set; } = DateTime.Now.AddDays(30);

        [Display(Name = "Subtotal")]
        [DataType(DataType.Currency)]
        public decimal Subtotal { get; set; }

        [Display(Name = "ITBIS (%)"), DisplayFormat(DataFormatString = "{0:F2}")]
        [Range(0, 100)]
        public decimal PorcentajeITBIS { get; set; } = 18; // ITBIS República Dominicana

        [Display(Name = "Monto ITBIS")]
        [DataType(DataType.Currency)]
        public decimal MontoITBIS { get; set; }

        [Display(Name = "Total ITBIS")]
        [DataType(DataType.Currency)]
        public decimal TotalITBIS { get; set; }

        [Display(Name = "ITBIS Retenido")]
        public decimal MontoITBISRetenido { get; set; }

        [Display(Name = "ISR Retenido")]
        public decimal MontoISRRetenido { get; set; }

        [Display(Name = "Total")]
        [DataType(DataType.Currency)]
        public decimal Total { get; set; }

        [StringLength(10)]
        [Display(Name = "Moneda")]
        public string Moneda { get; set; } = "DOP"; // DOP o USD

        [Display(Name = "Tasa de Cambio")]
        public decimal TasaCambio { get; set; } = 1.0m;

        [Display(Name = "Total en DOP (Fiscal)")]
        [DataType(DataType.Currency)]
        public decimal TotalDOP { get; set; }

        [Display(Name = "Monto Efectivo")]
        public decimal MontoEfectivo { get; set; }

        [Display(Name = "Monto Tarjeta")]
        public decimal MontoTarjeta { get; set; }

        [Display(Name = "Monto Transferencia")]
        public decimal MontoTransferencia { get; set; }

        [StringLength(50)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Pendiente";

        [StringLength(1000)]
        [Display(Name = "Notas")]
        public string? Notas { get; set; }

        [StringLength(2)]
        [Display(Name = "Tipo de Anulación (608)")]
        public string? TipoAnulacion { get; set; } // 01-11 según DGII

        [StringLength(200)]
        [Display(Name = "Motivo de Anulación")]
        public string? MotivoAnulacion { get; set; }

        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Relaciones
        public ICollection<FacturaLinea>? Lineas { get; set; }

        // Propiedades de solo lectura
        [Display(Name = "Tipo de Comprobante (Texto)")]
        public string TipoECFTexto => TipoECF switch
        {
            "31" => "Factura de Crédito Fiscal",
            "32" => "Factura de Consumo",
            "33" => "Nota de Débito",
            "34" => "Nota de Crédito",
            "41" => "Comprobante de Compras",
            "43" => "Gastos Menores",
            "44" => "Regímenes Especiales",
            "45" => "Gubernamental",
            "46" => "Exportaciones",
            "47" => "Pagos al Exterior",
            _ => "Desconocido"
        };
    }
}
