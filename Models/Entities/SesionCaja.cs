using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Facturapro.Models.Entities
{
    public class SesionCaja
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [ForeignKey("UsuarioId")]
        public virtual ApplicationUser? Usuario { get; set; }

        public DateTime FechaApertura { get; set; } = DateTime.Now;
        public DateTime? FechaCierre { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoInicial { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoFinalCalculado { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontoFinalDeclarado { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Diferencia => MontoFinalDeclarado.HasValue ? MontoFinalDeclarado.Value - MontoFinalCalculado : null;

        [StringLength(20)]
        public string Estado { get; set; } = "Abierta"; // Abierta, Cerrada

        public string? Notas { get; set; }
    }
}
