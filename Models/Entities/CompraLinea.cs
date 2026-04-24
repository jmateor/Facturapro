using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class CompraLinea
    {
        public int Id { get; set; }

        [Display(Name = "Compra")]
        public int CompraId { get; set; }

        [Display(Name = "Compra")]
        public Compra? Compra { get; set; }

        [Display(Name = "Producto")]
        public int ProductoId { get; set; }

        [Display(Name = "Producto")]
        public Producto? Producto { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; }

        [Display(Name = "Precio Unitario")]
        [DataType(DataType.Currency)]
        public decimal PrecioUnitario { get; set; }

        [Display(Name = "Descuento")]
        [DataType(DataType.Currency)]
        public decimal DescuentoLinea { get; set; }

        [Display(Name = "ITBIS %")]
        public decimal PorcentajeITBIS { get; set; } = 18;

        [Display(Name = "Total")]
        [DataType(DataType.Currency)]
        public decimal TotalLinea { get; set; }
    }
}
