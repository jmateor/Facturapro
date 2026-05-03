using System.Collections.Generic;

namespace Facturapro.Models.ViewModels.Reportes
{
    public class EstadoResultadosViewModel
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string MesNombre { get; set; } = string.Empty;

        // Ingresos
        public decimal TotalIngresosVentas { get; set; }
        public decimal TotalIngresosOtros { get; set; }
        public decimal TotalIngresos => TotalIngresosVentas + TotalIngresosOtros;

        // Costos y Gastos (Egresos)
        public decimal CostoVentas { get; set; }
        public decimal GastosOperativos { get; set; }
        public decimal GastosNomina { get; set; }
        public decimal GastosFinancieros { get; set; }
        public decimal OtrosGastos { get; set; }
        
        public decimal TotalEgresos => CostoVentas + GastosOperativos + GastosNomina + GastosFinancieros + OtrosGastos;

        // Utilidad
        public decimal UtilidadBruta => TotalIngresos - CostoVentas;
        public decimal UtilidadOperativa => UtilidadBruta - GastosOperativos - GastosNomina;
        public decimal UtilidadNeta => UtilidadOperativa - GastosFinancieros - OtrosGastos;

        // Detalles para desglose
        public List<DetalleCuenta> DetalleIngresos { get; set; } = new();
        public List<DetalleCuenta> DetalleEgresos { get; set; } = new();
    }

    public class DetalleCuenta
    {
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Categoria { get; set; } = string.Empty;
    }
}
