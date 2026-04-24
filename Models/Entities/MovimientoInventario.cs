using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class MovimientoInventario
    {
        public int Id { get; set; }

        [Display(Name = "Producto")]
        public int ProductoId { get; set; }

        [Display(Name = "Producto")]
        public Producto? Producto { get; set; }

        [Display(Name = "Tipo de Movimiento")]
        public TipoMovimiento TipoMovimiento { get; set; }

        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; }

        [Display(Name = "Stock Anterior")]
        public int StockAnterior { get; set; }

        [Display(Name = "Stock Nuevo")]
        public int StockNuevo { get; set; }

        [StringLength(500)]
        [Display(Name = "Motivo / Referencia")]
        public string? Motivo { get; set; }

        [Display(Name = "Compra relacionada")]
        public int? CompraId { get; set; }

        [Display(Name = "Compra relacionada")]
        public Compra? Compra { get; set; }

        [Display(Name = "Factura relacionada")]
        public int? FacturaId { get; set; }

        [Display(Name = "Factura relacionada")]
        public Factura? Factura { get; set; }

        [StringLength(100)]
        [Display(Name = "Usuario")]
        public string? UsuarioRegistro { get; set; }

        [Display(Name = "Fecha del Movimiento")]
        public DateTime FechaMovimiento { get; set; } = DateTime.Now;

        [Display(Name = "Costo Unitario")]
        [DataType(DataType.Currency)]
        public decimal? CostoUnitario { get; set; }
    }

    public enum TipoMovimiento
    {
        [Display(Name = "Entrada por Compra")]
        EntradaCompra,

        [Display(Name = "Entrada por Devolución")]
        EntradaDevolucion,

        [Display(Name = "Entrada por Ajuste")]
        EntradaAjuste,

        [Display(Name = "Salida por Venta")]
        SalidaVenta,

        [Display(Name = "Salida por Daño")]
        SalidaDano,

        [Display(Name = "Salida por Ajuste")]
        SalidaAjuste,

        [Display(Name = "Salida por Consumo")]
        SalidaConsumo
    }
}
