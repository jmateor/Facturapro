namespace Facturapro.Models.ViewModels.Reportes
{
    /// <summary>
    /// ViewModel para el reporte de ventas por período
    /// </summary>
    public class ReporteVentasViewModel
    {
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public string? TipoECF { get; set; }
        public int? ClienteId { get; set; }
        public string? EstadoDGII { get; set; }

        // Resumen
        public decimal TotalVentas { get; set; }
        public decimal TotalITBIS { get; set; }
        public int TotalFacturas { get; set; }
        public decimal PromedioFactura { get; set; }

        // Detalle
        public List<VentaDetalleViewModel> Facturas { get; set; } = new();

        // Agrupaciones
        public List<VentaPorDiaViewModel> VentasPorDia { get; set; } = new();
        public List<VentaPorTipoViewModel> VentasPorTipo { get; set; } = new();

        // Filtros para la vista
        public List<ClienteFilterViewModel> Clientes { get; set; } = new();
    }

    public class VentaDetalleViewModel
    {
        public int FacturaId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public string? ENCF { get; set; }
        public string TipoECF { get; set; } = string.Empty;
        public string TipoECFTexto { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string? ClienteRNC { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ITBIS { get; set; }
        public decimal Total { get; set; }
        public string EstadoDGII { get; set; } = string.Empty;
        public string EstadoClass => EstadoDGII?.ToLower() switch
        {
            "aprobado" => "badge-success",
            "firmado" => "badge-primary",
            "enviado" => "badge-warning",
            "rechazado" => "badge-danger",
            _ => "badge-secondary"
        };
    }

    public class VentaPorTipoViewModel
    {
        public string TipoECF { get; set; } = string.Empty;
        public string TipoECFTexto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class ClienteFilterViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? RNC { get; set; }
    }
}
