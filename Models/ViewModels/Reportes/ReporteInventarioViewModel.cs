namespace Facturapro.Models.ViewModels.Reportes
{
    /// <summary>
    /// ViewModel para el reporte de inventario
    /// </summary>
    public class ReporteInventarioViewModel
    {
        // Resumen
        public int TotalProductos { get; set; }
        public decimal ValorInventarioTotal { get; set; }
        public int ProductosBajoStock { get; set; }
        public int ProductosSinStock { get; set; }

        // Productos
        public List<ProductoInventarioViewModel> Productos { get; set; } = new();

        // Filtros
        public int? CategoriaId { get; set; }
        public string? EstadoStock { get; set; } // todos, bajo, sin_stock
    }

    public class ProductoInventarioViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? CodigoBarra { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal Costo { get; set; }
        public decimal Precio { get; set; }
        public decimal ValorInventario => StockActual * Costo;
        public string EstadoClass
        {
            get
            {
                if (StockActual <= 0) return "badge-danger";
                if (StockActual <= StockMinimo) return "badge-warning";
                return "badge-success";
            }
        }
        public string EstadoTexto
        {
            get
            {
                if (StockActual <= 0) return "Sin Stock";
                if (StockActual <= StockMinimo) return "Bajo Stock";
                return "OK";
            }
        }
    }
}
