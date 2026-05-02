using System;
using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class TransferenciaInventario
    {
        public int Id { get; set; }

        public int AlmacenOrigenId { get; set; }
        public virtual Almacen AlmacenOrigen { get; set; }

        public int AlmacenDestinoId { get; set; }
        public virtual Almacen AlmacenDestino { get; set; }

        public int ProductoId { get; set; }
        public virtual Producto Producto { get; set; }

        public decimal Cantidad { get; set; }

        public DateTime FechaTransferencia { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string UsuarioId { get; set; }

        [StringLength(50)]
        public string Estado { get; set; } = "Completado"; // Pendiente, Completado, Cancelado

        [StringLength(500)]
        public string Notas { get; set; }
    }
}
