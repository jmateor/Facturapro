using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.Entities
{
    public class Proveedor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "RNC/Cédula")]
        public string? Documento { get; set; }

        [StringLength(200)]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [StringLength(150)]
        [Display(Name = "Correo Electrónico")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string? Email { get; set; }

        [StringLength(50)]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [StringLength(100)]
        [Display(Name = "Contacto")]
        public string? PersonaContacto { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Propiedades de navegación
        public ICollection<Compra> Compras { get; set; } = new List<Compra>();
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
