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

        [Display(Name = "Estado")]
        public EstadoCompra Estado { get; set; } = EstadoCompra.Pendiente;

        [Display(Name = "Subtotal")]
        [DataType(DataType.Currency)]
        public decimal SubTotal { get; set; }

        [Display(Name = "ITBIS")]
        [DataType(DataType.Currency)]
        public decimal ITBIS { get; set; }

        [Display(Name = "Descuento")]
        [DataType(DataType.Currency)]
        public decimal Descuento { get; set; }

        [Display(Name = "Total")]
        [DataType(DataType.Currency)]
        public decimal Total { get; set; }

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
