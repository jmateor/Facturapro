using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class FacturaLinea
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Factura")]
        public int FacturaId { get; set; }
        public Factura? Factura { get; set; }

        [Display(Name = "Número de Línea")]
        public int NumeroLinea { get; set; }

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [StringLength(80)]
        [Display(Name = "Nombre del Ítem")]
        public string? NombreItem { get; set; }

        [Display(Name = "Indicador Facturación")]
        public int IndicadorFacturacion { get; set; } = 1; // 1=ITBIS 18%, 2=ITBIS 16%, 3=ITBIS 0%, 4=Exento

        [Display(Name = "Tipo Ítem")]
        public int IndicadorBienoServicio { get; set; } = 1; // 1=Bien, 2=Servicio

        [Display(Name = "Cantidad")]
        public decimal Cantidad { get; set; } = 1;

        [Display(Name = "Unidad de Medida")]
        [StringLength(20)]
        public string? UnidadMedida { get; set; }

        [Display(Name = "Precio unitario")]
        [DataType(DataType.Currency)]
        public decimal PrecioUnitario { get; set; }

        [Display(Name = "Descuento (%)")]
        [Range(0, 100)]
        public decimal Descuento { get; set; } = 0;

        [Display(Name = "Monto Descuento")]
        [DataType(DataType.Currency)]
        public decimal MontoDescuento { get; set; }

        [Display(Name = "ITBIS")]
        [DataType(DataType.Currency)]
        public decimal MontoITBIS { get; set; }

        [Display(Name = "Subtotal")]
        [DataType(DataType.Currency)]
        public decimal Subtotal { get; set; }

        [Display(Name = "Orden")]
        public int Orden { get; set; }

        // Propiedad de solo lectura para monto total de línea
        [Display(Name = "Monto Total Línea")]
        public decimal MontoItem => Subtotal + MontoITBIS - MontoDescuento;
    }
}
