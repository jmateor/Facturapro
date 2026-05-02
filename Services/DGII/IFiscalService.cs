using Facturapro.Models.Entities;

namespace Facturapro.Services.DGII
{
    public interface IFiscalService
    {
        Task<string> Generar606Async(int mes, int anio);
        Task<string> Generar607Async(int mes, int anio);
        Task<string> Generar608Async(int mes, int anio);
        Task<FiscalSummaryViewModel> GetSummaryAsync(int mes, int anio);
    }

    public class FiscalSummaryViewModel
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public int CantidadCompras { get; set; }
        public decimal TotalCompras { get; set; }
        public decimal TotalITBISCompras { get; set; }
        public int CantidadVentas { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal TotalITBISVentas { get; set; }
        public decimal TotalITBISRetenidoVentas { get; set; }
        public decimal TotalITBISRetenidoCompras { get; set; }
        public int CantidadAnuladas { get; set; }
        
        // Listas detalladas para la vista
        public List<Factura> Ventas { get; set; } = new List<Factura>();
        public List<Compra> Compras { get; set; } = new List<Compra>();
        public List<Factura> Anuladas { get; set; } = new List<Factura>();

        // Cálculos proyectados
        public decimal BalanceITBIS => TotalITBISVentas - TotalITBISCompras;
        public decimal ITBISAPagar => Math.Max(0, BalanceITBIS - TotalITBISRetenidoVentas);
    }
}
