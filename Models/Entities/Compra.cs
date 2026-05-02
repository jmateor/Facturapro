using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class Compra
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El número de factura es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Número de Factura")]
        public string NumeroFactura { get; set; } = string.Empty;

        [Display(Name = "Proveedor")]
        public int ProveedorId { get; set; }

        [Display(Name = "Proveedor")]
        public Proveedor? Proveedor { get; set; }

        [Display(Name = "Fecha de Compra")]
        [DataType(DataType.Date)]
        public DateTime FechaCompra { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Recepción")]
        [DataType(DataType.Date)]
        public DateTime? FechaRecepcion { get; set; }

        [StringLength(13)]
        [Display(Name = "NCF")]
        public string? NCF { get; set; }

        [StringLength(2)]
        [Display(Name = "Tipo de Gasto")]
        public string? TipoGasto { get; set; } = "01"; // 01=Gastos de Personal, 02=Gastos por Trabajos, etc.

        [Display(Name = "Fecha de Pago")]
        public DateTime? FechaPago { get; set; }

        [Display(Name = "Estado")]
        public EstadoCompra Estado { get; set; } = EstadoCompra.Pendiente;

        [Display(Name = "Subtotal")]
        [DataType(DataType.Currency)]
        public decimal SubTotal { get; set; }

        [Display(Name = "Monto en Servicios")]
        public decimal MontoServicios { get; set; }

        [Display(Name = "Monto en Bienes")]
        public decimal MontoBienes { get; set; }

        [Display(Name = "ITBIS")]
        [DataType(DataType.Currency)]
        public decimal ITBIS { get; set; }

        [Display(Name = "Descuento")]
        [DataType(DataType.Currency)]
        public decimal Descuento { get; set; }

        [Display(Name = "Total")]
        [DataType(DataType.Currency)]
        public decimal Total { get; set; }

        [Display(Name = "ITBIS Retenido")]
        public decimal MontoITBISRetenido { get; set; }

        [Display(Name = "ISR Retenido")]
        public decimal MontoISRRetenido { get; set; }

        [StringLength(13)]
        [Display(Name = "NCF Modificado")]
        public string? NCFModificado { get; set; }

        [Display(Name = "ITBIS Proporcionalidad")]
        public decimal ITBISProporcionalidad { get; set; }

        [Display(Name = "ITBIS al Costo")]
        public decimal ITBISCosto { get; set; }

        [Display(Name = "ITBIS Percibido")]
        public decimal ITBISPercibido { get; set; }

        [Display(Name = "Tipo Retención ISR")]
        public int? TipoRetencionISR { get; set; } // 1-9

        [Display(Name = "ISR Percibido")]
        public decimal ISRPercibido { get; set; }

        [Display(Name = "ISC")]
        public decimal ISC { get; set; }

        [Display(Name = "Otros Impuestos")]
        public decimal OtrosImpuestos { get; set; }

        [Display(Name = "Propina Legal")]
        public decimal PropinaLegal { get; set; }

        [StringLength(2)]
        [Display(Name = "Forma de Pago")]
        public string FormaPago { get; set; } = "01"; // 01=Efectivo, 02=Cheques, etc.

        [StringLength(500)]
        [Display(Name = "Notas")]
        public string? Notas { get; set; }

        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Propiedades de navegación
        public ICollection<CompraLinea> Lineas { get; set; } = new List<CompraLinea>();
    }

    public enum EstadoCompra
    {
        [Display(Name = "Pendiente")]
        Pendiente,

        [Display(Name = "Recibida")]
        Recibida,

        [Display(Name = "Cancelada")]
        Cancelada
    }
}
