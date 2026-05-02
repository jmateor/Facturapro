using System;
using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class StockAlmacen
    {
        public int Id { get; set; }

        public int ProductoId { get; set; }
        public virtual Producto Producto { get; set; }

        public int AlmacenId { get; set; }
        public virtual Almacen Almacen { get; set; }

        public decimal Cantidad { get; set; }

        public DateTime UltimaActualizacion { get; set; } = DateTime.Now;
    }
}
