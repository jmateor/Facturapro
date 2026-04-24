using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models
{
    /// <summary>
    /// Modelo de usuario extendido para ASP.NET Core Identity
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Apellido")]
        public string? Apellido { get; set; }

        [Display(Name = "Nombre Completo")]
        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();

        [StringLength(20)]
        [Display(Name = "Rol")]
        public string Rol { get; set; } = "Vendedor"; // Admin, Cajero, Vendedor, Gerente

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Fecha de registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Display(Name = "Último acceso")]
        public DateTime? UltimoAcceso { get; set; }

        // Propiedades de navegación para auditoría
        public virtual ICollection<FacturaAudit> FacturasCreadas { get; set; } = new List<FacturaAudit>();
    }

    /// <summary>
    /// Tabla de auditoría para facturas (quién creó/qué cuándo)
    /// </summary>
    public class FacturaAudit
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty; // Crear, Modificar, EnviarDGII
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string? Detalles { get; set; }
    }
}
