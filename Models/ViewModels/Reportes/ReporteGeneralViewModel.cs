using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.ViewModels.Reportes
{
    public class ReporteGeneralViewModel
    {
        // Filtros seleccionados
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public int? ClienteId { get; set; }
        public int? CategoriaId { get; set; }
        public int? ProductoId { get; set; }
        public string? TipoReporte { get; set; }
        public string? Ciudad { get; set; }
        public int? Anio { get; set; }
        public bool IncluirAnuladas { get; set; }
        public bool IncluirDevoluciones { get; set; }

        // Datos del reporte
        public List<FacturaReporteViewModel> Facturas { get; set; } = new();

        // Totales
        public decimal TotalVentas { get; set; }
        public decimal TotalITBIS { get; set; }
        public decimal TotalDescuentos { get; set; }
        public int TotalFacturas { get; set; }
        public int TotalItems { get; set; }
    }

    public class FacturaReporteViewModel
    {
        public int FacturaId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public string? ENCF { get; set; }
        public string TipoECF { get; set; } = string.Empty;
        public string TipoECFTexto { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string? ClienteRNC { get; set; }
        public string? ClienteCiudad { get; set; }
        public string? CategoriaNombre { get; set; }
        public string? ProductoNombre { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ITBIS { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public string EstadoDGII { get; set; } = string.Empty;
        public bool EsAnulada => EstadoDGII == "Rechazado" || EstadoDGII == "Cancelada";
        public bool EsDevolucion => TipoECF == "33" || TipoECF == "34";
    }

    public class ProductosMasVendidosViewModel
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? CodigoBarras { get; set; }
        public string? Categoria { get; set; }
        public decimal CantidadVendida { get; set; }
        public decimal TotalVentas { get; set; }
        public int NumeroFacturas { get; set; }
    }

    public class VentasPorCategoriaViewModel
    {
        public int? CategoriaId { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public decimal TotalVentas { get; set; }
        public int CantidadProductos { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class VentasPorCiudadViewModel
    {
        public string Ciudad { get; set; } = string.Empty;
        public int CantidadClientes { get; set; }
        public int CantidadFacturas { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class VentasPorAnioViewModel
    {
        public int Anio { get; set; }
        public decimal TotalVentas { get; set; }
        public int CantidadFacturas { get; set; }
        public decimal PromedioMensual { get; set; }
    }

    public class DevolucionReporteViewModel
    {
        public int FacturaId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public string? ENCF { get; set; }
        public DateTime FechaEmision { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string? ClienteRNC { get; set; }
        public decimal TotalDevolucion { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
