using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class Almacen
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del almacén es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; }

        public int SucursalId { get; set; }
        public virtual Sucursal Sucursal { get; set; }

        public bool EsPrincipalAlmacen { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Relaciones
        public virtual ICollection<StockAlmacen> Stocks { get; set; }
    }
}
