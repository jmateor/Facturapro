using System.ComponentModel.DataAnnotations;

namespace Facturapro.Models.ViewModels
{
    public class ClienteViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        [Display(Name = "Nombre / Razón Social")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Display(Name = "NIF/CIF")]
        public string? NIF { get; set; }

        [StringLength(200)]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [StringLength(50)]
        [Display(Name = "Ciudad")]
        public string? Ciudad { get; set; }

        [StringLength(10)]
        [Display(Name = "Código Postal")]
        public string? CodigoPostal { get; set; }

        [StringLength(50)]
        [Display(Name = "País")]
        public string? Pais { get; set; } = "España";

        [StringLength(20)]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Total Facturas")]
        public int TotalFacturas { get; set; }

        [Display(Name = "Total Facturado")]
        [DataType(DataType.Currency)]
        public decimal TotalFacturado { get; set; }
    }
}
