using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class Sucursal
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(200)]
        public string Direccion { get; set; }

        [StringLength(50)]
        public string Telefono { get; set; }

        [StringLength(11)]
        public string RNC_Especifico { get; set; }

        public bool EsPrincipal { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Relaciones
        public virtual ICollection<Almacen> Almacenes { get; set; }
    }
}
