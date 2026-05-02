using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class LogAuditoria
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Usuario")]
        public string Usuario { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Módulo")]
        public string Modulo { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Acción")]
        public string Accion { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "ID Entidad")]
        public string? EntidadId { get; set; }

        [Required]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [StringLength(45)]
        [Display(Name = "Dirección IP")]
        public string? IpAddress { get; set; }

        [Display(Name = "Nivel")]
        public string Nivel { get; set; } = "Info"; // Info, Warning, Error, Critical
    }
}
