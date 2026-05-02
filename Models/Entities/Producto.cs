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

        // Precio de Compra y Ganancia
        [Display(Name = "Precio de Compra")]
        [DataType(DataType.Currency)]
        public decimal PrecioCompra { get; set; } = 0;

        [Display(Name = "Costo Promedio")]
        [DataType(DataType.Currency)]
        public decimal CostoPromedio { get; set; } = 0;

        [Display(Name = "Ganancia")]
        [DataType(DataType.Currency)]
        public decimal Ganancia => Precio - PrecioCompra;

        [Display(Name = "Margen de Ganancia (%)")]
        public decimal MargenGanancia => Precio > 0 ? ((Precio - PrecioCompra) / Precio) * 100 : 0;

        // Tipo de Producto
        [Display(Name = "Tipo de Producto")]
        public int TipoProducto { get; set; } = 1; // 1=Artículo, 2=Servicio, 3=Libra

        // Campos específicos para productos a granel (Libra)
        [Display(Name = "Stock Mínimo")]
        public int StockMinimo { get; set; } = 0;

        [Display(Name = "Ubicación en Almacén")]
        [StringLength(100)]
        public string? Ubicacion { get; set; }

        [Display(Name = "Proveedor Principal")]
        public int? ProveedorId { get; set; }

        [Display(Name = "Proveedor Principal")]
        public Proveedor? Proveedor { get; set; }

        [Display(Name = "Controla Stock")]
        public bool ControlaStock { get; set; } = true; // Para servicios que no controlan inventario

        [Display(Name = "Fecha de Vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        [Display(Name = "Número de Lote")]
        [StringLength(50)]
        public string? NumeroLote { get; set; }

        [Display(Name = "Peso por Unidad")]
        [DataType(DataType.Currency)]
        public decimal? PesoPorUnidad { get; set; } // Para productos que se venden por peso

        [Display(Name = "Unidad de Medida")]
        [StringLength(20)]
        public string UnidadMedida { get; set; } = "Unidad"; // Unidad, Libra, Kg, Documento, Hora, etc.

        [Display(Name = "Ícono")]
        [StringLength(50)]
        public string? Icono { get; set; } // Clase de ícono RemixIcon (ej: "ri-car-line", "ri-t-shirt-line")
    }
}
