using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Precio")]
        [DataType(DataType.Currency)]
        public decimal Precio { get; set; }

        [Display(Name = "Stock")]
        public int Stock { get; set; } = 0;

        [Display(Name = "Categoría")]
        public int? CategoriaId { get; set; }

        [Display(Name = "Categoría")]
        public Categoria? Categoria { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [StringLength(50)]
        [Display(Name = "Código de Barras")]
        public string? CodigoBarras { get; set; }

        [Display(Name = "URL Imagen Código de Barras")]
        public string? CodigoBarrasImagenUrl { get; set; }
    }
}
