namespace Facturapro.Models.ViewModels.Reportes
{
    /// <summary>
    /// ViewModel para el dashboard principal con estadísticas clave
    /// </summary>
    public class DashboardViewModel
    {
        // Tarjetas de resumen
        public decimal VentasHoy { get; set; }
        public decimal VentasMes { get; set; }
        public decimal VentasAnio { get; set; }
        public int FacturasHoy { get; set; }
        public int FacturasMes { get; set; }
        public int TotalClientes { get; set; }
        public int TotalProductos { get; set; }

        // Comparativas
        public decimal VariacionVentasMes { get; set; } // Porcentaje vs mes anterior
        public decimal VariacionVentasHoy { get; set; } // Porcentaje vs mismo día mes anterior

        // Datos para gráficos
        public List<VentaPorDiaViewModel> VentasUltimos7Dias { get; set; } = new();
        public List<VentaPorMesViewModel> VentasPorMes { get; set; } = new();
        public List<ProductoTopViewModel> TopProductos { get; set; } = new();
        public List<ClienteTopViewModel> TopClientes { get; set; } = new();

        // Facturas por estado
        public List<FacturaPorEstadoViewModel> FacturasPorEstado { get; set; } = new();

        // Alertas
        public List<AlertaViewModel> Alertas { get; set; } = new();
    }

    public class VentaPorDiaViewModel
    {
        public DateTime Fecha { get; set; }
        public string FechaFormateada => Fecha.ToString("dd/MM");
        public string DiaSemana => Fecha.ToString("ddd");
        public decimal Total { get; set; }
        public int CantidadFacturas { get; set; }
    }

    public class VentaPorMesViewModel
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string NombreMes => new DateTime(Anio, Mes, 1).ToString("MMMM");
        public decimal Total { get; set; }
        public int CantidadFacturas { get; set; }
    }

    public class ProductoTopViewModel
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? CodigoBarra { get; set; }
        public decimal CantidadVendida { get; set; }
        public decimal TotalVenta { get; set; }
    }

    public class ClienteTopViewModel
    {
        public int ClienteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? RNC { get; set; }
        public int CantidadFacturas { get; set; }
        public decimal TotalCompras { get; set; }
    }

    public class FacturaPorEstadoViewModel
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
        public string Color => Estado.ToLower() switch
        {
            "aprobado" => "#10b981",
            "firmado" => "#3b82f6",
            "enviado" => "#f59e0b",
            "rechazado" => "#ef4444",
            "pendiente" => "#6b7280",
            _ => "#9ca3af"
        };
    }

    public class AlertaViewModel
    {
        public string Tipo { get; set; } = string.Empty; // warning, danger, info, success
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? AccionUrl { get; set; }
        public string? AccionTexto { get; set; }
    }
}
